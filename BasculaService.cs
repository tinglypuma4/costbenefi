using System;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio principal para manejo de b�scula digital
    /// Compatible con m�ltiples marcas y protocolos
    /// </summary>
    public class BasculaService : IDisposable
    {
        private SerialPort? _serialPort;
        private ConfiguracionBascula? _configuracion;
        private bool _isConnected = false;
        private bool _isReading = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly AppDbContext _context;

        // Eventos
        public event EventHandler<PesoEventArgs>? PesoRecibido;
        public event EventHandler<string>? ErrorOcurrido;
        public event EventHandler<bool>? EstadoConexionCambiado;

        // Propiedades
        public bool EstaConectada => _isConnected && _serialPort?.IsOpen == true;
        public bool EstaLeyendo => _isReading;
        public string? PuertoActual => _configuracion?.Puerto;
        public string? NombreBascula => _configuracion?.Nombre;

        public BasculaService(AppDbContext context)
        {
            _context = context;
        }

        public BasculaService() : this(new AppDbContext())
        {
        }

        /// <summary>
        /// Conecta autom�ticamente usando la configuraci�n activa
        /// </summary>
        public bool Conectar()
        {
            try
            {
                return ConectarAsync().Result;
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al conectar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Conecta con la b�scula usando configuraci�n autom�tica
        /// </summary>
        public async Task<bool> ConectarAsync()
        {
            try
            {
                // Cargar configuraci�n activa desde base de datos
                await CargarConfiguracionActivaAsync();

                if (_configuracion == null)
                {
                    // Crear configuraci�n predeterminada si no existe
                    await CrearConfiguracionPredeterminadaAsync();
                }

                if (_configuracion == null)
                {
                    ErrorOcurrido?.Invoke(this, "No se pudo cargar configuraci�n de b�scula");
                    return false;
                }

                return await ConectarConConfiguracionAsync(_configuracion);
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al conectar b�scula: {ex.Message}");
                EstadoConexionCambiado?.Invoke(this, false);
                return false;
            }
        }

        /// <summary>
        /// Conecta usando una configuraci�n espec�fica
        /// </summary>
        public async Task<bool> ConectarConConfiguracionAsync(ConfiguracionBascula config)
        {
            try
            {
                if (_isConnected)
                {
                    await DesconectarAsync();
                }

                _configuracion = config;

                _serialPort = new SerialPort(
                    config.Puerto,
                    config.BaudRate,
                    (Parity)config.Paridad,
                    config.DataBits,
                    (StopBits)config.StopBits
                )
                {
                    ReadTimeout = config.TimeoutLectura,
                    WriteTimeout = config.TimeoutEscritura,
                    Handshake = (Handshake)config.ControlFlujo,
                    RtsEnable = true,
                    DtrEnable = true
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.ErrorReceived += SerialPort_ErrorReceived;

                _serialPort.Open();
                _isConnected = true;

                // Comando de inicializaci�n
                if (!string.IsNullOrEmpty(config.ComandoInicializacion))
                {
                    await EnviarComandoAsync(config.ComandoInicializacion);
                }

                // Iniciar lectura autom�tica si no requiere solicitud
                if (!config.RequiereSolicitudPeso)
                {
                    await IniciarLecturaAutomaticaAsync();
                }

                EstadoConexionCambiado?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                ErrorOcurrido?.Invoke(this, $"Error al conectar: {ex.Message}");
                EstadoConexionCambiado?.Invoke(this, false);
                return false;
            }
        }

        /// <summary>
        /// Desconecta de la b�scula
        /// </summary>
        public async Task DesconectarAsync()
        {
            await DetenerLecturaAsync();

            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.Close();
                }
                catch (Exception ex)
                {
                    ErrorOcurrido?.Invoke(this, $"Error al desconectar: {ex.Message}");
                }
            }

            _serialPort?.Dispose();
            _serialPort = null;
            _isConnected = false;

            EstadoConexionCambiado?.Invoke(this, false);
        }

        /// <summary>
        /// Lee peso una sola vez
        /// </summary>
        public async Task<decimal> LeerPesoAsync()
        {
            if (!_isConnected || _configuracion == null)
            {
                throw new InvalidOperationException("B�scula no conectada");
            }

            try
            {
                if (_configuracion.RequiereSolicitudPeso)
                {
                    await EnviarComandoAsync(_configuracion.ComandoSolicitarPeso);
                }

                // Esperar respuesta (m�ximo 3 segundos)
                var timeout = DateTime.Now.AddSeconds(3);
                decimal pesoRecibido = 0;
                bool pesoObtenido = false;

                // Suscribirse temporalmente al evento
                EventHandler<PesoEventArgs> handlerTemporal = (s, e) =>
                {
                    pesoRecibido = e.Peso;
                    pesoObtenido = true;
                };

                PesoRecibido += handlerTemporal;

                try
                {
                    // Esperar hasta obtener peso o timeout
                    while (!pesoObtenido && DateTime.Now < timeout)
                    {
                        await Task.Delay(100);
                    }

                    return pesoObtenido ? pesoRecibido : 0;
                }
                finally
                {
                    PesoRecibido -= handlerTemporal;
                }
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al leer peso: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Tarar la b�scula
        /// </summary>
        public async Task<bool> TararAsync()
        {
            if (!_isConnected || _configuracion == null || string.IsNullOrEmpty(_configuracion.ComandoTara))
                return false;

            try
            {
                await EnviarComandoAsync(_configuracion.ComandoTara);
                await Task.Delay(1000); // Esperar que la b�scula procese
                return true;
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al tarar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Inicia lectura autom�tica continua
        /// </summary>
        private async Task IniciarLecturaAutomaticaAsync()
        {
            if (_isReading || _configuracion == null) return;

            _isReading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            if (_configuracion.RequiereSolicitudPeso)
            {
                // Modo por solicitud - enviar comando peri�dicamente
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            await EnviarComandoAsync(_configuracion.ComandoSolicitarPeso);
                            await Task.Delay(_configuracion.IntervaloLectura, _cancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancelaci�n normal
                    }
                    catch (Exception ex)
                    {
                        ErrorOcurrido?.Invoke(this, $"Error en lectura autom�tica: {ex.Message}");
                    }
                }, _cancellationTokenSource.Token);
            }
            // Si no requiere solicitud, los datos llegan autom�ticamente v�a DataReceived
        }

        /// <summary>
        /// Detiene la lectura autom�tica
        /// </summary>
        private async Task DetenerLecturaAsync()
        {
            _isReading = false;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            await Task.Delay(100);
        }

        /// <summary>
        /// Env�a comando a la b�scula
        /// </summary>
        private async Task EnviarComandoAsync(string comando)
        {
            if (_serialPort?.IsOpen != true || string.IsNullOrEmpty(comando))
                return;

            try
            {
                var terminador = _configuracion?.TerminadorComando ?? "\r\n";
                var bytes = Encoding.ASCII.GetBytes(comando + terminador);
                _serialPort.Write(bytes, 0, bytes.Length);
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al enviar comando: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja datos recibidos del puerto serie
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort?.IsOpen != true)
                    return;

                var data = _serialPort.ReadExisting();
                if (string.IsNullOrEmpty(data))
                    return;

                var peso = ProcesarDatosRecibidos(data);
                if (peso.HasValue && peso.Value >= 0)
                {
                    PesoRecibido?.Invoke(this, new PesoEventArgs
                    {
                        Peso = peso.Value,
                        Unidad = _configuracion?.UnidadPeso ?? "kg",
                        Timestamp = DateTime.Now,
                        DatosOriginales = data.Trim(),
                        EsEstable = true
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al procesar datos: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja errores del puerto serie
        /// </summary>
        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorOcurrido?.Invoke(this, $"Error de puerto serie: {e.EventType}");
        }

        /// <summary>
        /// Procesa datos recibidos y extrae el peso
        /// </summary>
        private decimal? ProcesarDatosRecibidos(string datos)
        {
            try
            {
                datos = datos.Trim();

                // Usar patr�n personalizado si est� configurado
                if (_configuracion != null && !string.IsNullOrEmpty(_configuracion.PatronExtraccion))
                {
                    var match = Regex.Match(datos, _configuracion.PatronExtraccion);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (decimal.TryParse(match.Groups[1].Value.Replace(',', '.'),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out decimal peso))
                        {
                            return peso;
                        }
                    }
                }

                // Patrones est�ndar para b�sculas comunes
                var patronesEstandar = new[]
                {
                    @"ST,GS,\+?\s*(\d+\.?\d*)",    // Protocolo est�ndar
                    @"(\d+\.?\d*)\s*kg",           // Formato: "1.5 kg"
                    @"(\d+\.?\d*)\s*g",            // Formato: "1500 g"
                    @"(\d+\.?\d*)\s*lb",           // Formato: "3.3 lb"
                    @"W:\s*(\d+\.?\d*)",           // Formato: "W: 1.5"
                    @"WT\s*(\d+\.?\d*)",           // Formato: "WT 1.5"
                    @"NET\s*(\d+\.?\d*)",          // Formato: "NET 1.5"
                    @"[+-]?\s*(\d+\.?\d*)",        // Solo n�meros con signo
                };

                foreach (var patron in patronesEstandar)
                {
                    var match = Regex.Match(datos, patron, RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (decimal.TryParse(match.Groups[1].Value.Replace(',', '.'),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out decimal peso))
                        {
                            return peso;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Carga configuraci�n activa desde base de datos
        /// </summary>
        private async Task CargarConfiguracionActivaAsync()
        {
            try
            {
                _configuracion = await _context.Set<ConfiguracionBascula>()
                    .FirstOrDefaultAsync(c => c.EsConfiguracionActiva);
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al cargar configuraci�n: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea configuraci�n predeterminada si no existe
        /// </summary>
        private async Task CrearConfiguracionPredeterminadaAsync()
        {
            try
            {
                var puertosDisponibles = SerialPort.GetPortNames();
                if (puertosDisponibles.Length == 0)
                {
                    ErrorOcurrido?.Invoke(this, "No se encontraron puertos COM disponibles");
                    return;
                }

                var configPredeterminada = ConfiguracionBascula.ConfiguracionGenerica();
                configPredeterminada.Puerto = puertosDisponibles[0]; // Usar primer puerto disponible
                configPredeterminada.EsConfiguracionActiva = true;

                _context.Set<ConfiguracionBascula>().Add(configPredeterminada);
                await _context.SaveChangesAsync();

                _configuracion = configPredeterminada;
            }
            catch (Exception ex)
            {
                ErrorOcurrido?.Invoke(this, $"Error al crear configuraci�n predeterminada: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene lista de puertos COM disponibles
        /// </summary>
        public static string[] ObtenerPuertosDisponibles()
        {
            try
            {
                return SerialPort.GetPortNames();
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Obtiene lista de configuraciones guardadas
        /// </summary>
        public async Task<ConfiguracionBascula[]> ObtenerConfiguracionesAsync()
        {
            try
            {
                return await _context.Set<ConfiguracionBascula>()
                    .OrderBy(c => c.Nombre)
                    .ToArrayAsync();
            }
            catch
            {
                return new ConfiguracionBascula[0];
            }
        }

        /// <summary>
        /// Prueba conexi�n con configuraci�n espec�fica
        /// </summary>
        public async Task<bool> ProbarConexionAsync(ConfiguracionBascula config)
        {
            var servicioTemporal = new BasculaService(_context);
            try
            {
                var resultado = await servicioTemporal.ConectarConConfiguracionAsync(config);
                if (resultado)
                {
                    await Task.Delay(1000); // Esperar 1 segundo
                    var peso = await servicioTemporal.LeerPesoAsync();
                    return peso >= 0; // Peso v�lido (incluso 0)
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                await servicioTemporal.DesconectarAsync();
                servicioTemporal.Dispose();
            }
        }

        public void Dispose()
        {
            DesconectarAsync().Wait();
            _context?.Dispose();
        }
    }

    /// <summary>
    /// Argumentos del evento de peso recibido
    /// </summary>
    public class PesoEventArgs : EventArgs
    {
        public decimal Peso { get; set; }
        public string Unidad { get; set; } = "kg";
        public DateTime Timestamp { get; set; }
        public string DatosOriginales { get; set; } = "";
        public bool EsEstable { get; set; } = true;
        public bool EsPositivo => Peso >= 0;
        public string PesoFormateado => $"{Peso:F3} {Unidad}";
    }
}