using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using System.Management;

namespace costbenefi.Services
{
    public class BasculaService : IDisposable
    {
        private readonly AppDbContext _context;
        private SerialPort _serialPort;
        private ConfiguracionBascula _configuracion;
        private bool _conectado = false;
        private DateTime _ultimoEventoDataReceived = DateTime.MinValue;
        private readonly object _lockDataReceived = new object();
        private readonly object _lockObject = new object();

        // Eventos
        public event EventHandler<PesoRecibidoEventArgs> PesoRecibido;
        public event EventHandler<string> ErrorOcurrido;
        public event EventHandler<string> DatosRecibidos;

        // ✅ PROPIEDADES PÚBLICAS AGREGADAS
        /// <summary>
        /// Indica si la báscula está conectada
        /// </summary>
        public bool EstaConectada => _conectado && _serialPort?.IsOpen == true;

        /// <summary>
        /// Obtiene la configuración actual de la báscula
        /// </summary>
        public ConfiguracionBascula ConfiguracionActual => _configuracion;

        public BasculaService(AppDbContext context)
        {
            _context = context;
        }

        #region Métodos Estáticos de Utilidad

        /// <summary>
        /// ✅ MÉTODO MEJORADO: Obtiene todos los puertos COM disponibles con información detallada
        /// </summary>
        public static string[] ObtenerPuertosDisponibles()
        {
            try
            {
                Debug.WriteLine("🔍 === INICIANDO DETECCIÓN DE PUERTOS COM ===");

                var puertosEncontrados = new List<string>();

                // Método 1: SerialPort.GetPortNames() - Básico pero confiable
                try
                {
                    var puertosSistema = SerialPort.GetPortNames();
                    Debug.WriteLine($"📋 Método 1 - GetPortNames(): {puertosSistema.Length} puertos");

                    if (puertosSistema.Any())
                    {
                        puertosEncontrados.AddRange(puertosSistema);
                        foreach (var puerto in puertosSistema)
                        {
                            Debug.WriteLine($"   • {puerto}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error en GetPortNames(): {ex.Message}");
                }

                // Método 2: WMI Query - Más información detallada
                try
                {
                    var puertosWMI = ObtenerPuertosViaWMI();
                    Debug.WriteLine($"📋 Método 2 - WMI Query: {puertosWMI.Count} puertos");

                    foreach (var info in puertosWMI)
                    {
                        Debug.WriteLine($"   • {info.Key}: {info.Value}");
                        if (!puertosEncontrados.Contains(info.Key))
                        {
                            puertosEncontrados.Add(info.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error en WMI Query: {ex.Message}");
                }

                // Si no se encontraron puertos, agregar puertos comunes como fallback
                if (!puertosEncontrados.Any())
                {
                    Debug.WriteLine("⚠️ No se detectaron puertos - Agregando puertos comunes como fallback");
                    puertosEncontrados.AddRange(new[] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8" });
                }

                // Ordenar puertos numéricamente
                var puertosOrdenados = puertosEncontrados
                    .Distinct()
                    .OrderBy(puerto =>
                    {
                        if (int.TryParse(puerto.Replace("COM", ""), out int num))
                            return num;
                        return 999;
                    })
                    .ToArray();

                Debug.WriteLine($"✅ RESULTADO FINAL: {puertosOrdenados.Length} puertos disponibles");
                Debug.WriteLine($"   Puertos: [{string.Join(", ", puertosOrdenados)}]");
                Debug.WriteLine("🔍 === FIN DETECCIÓN DE PUERTOS COM ===\n");

                return puertosOrdenados;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 ERROR CRÍTICO en ObtenerPuertosDisponibles: {ex}");

                // Fallback absoluto
                return new[] { "COM1", "COM2", "COM3", "COM4", "COM5" };
            }
        }

        /// <summary>
        /// Obtiene puertos COM usando WMI para información más detallada
        /// </summary>
        private static Dictionary<string, string> ObtenerPuertosViaWMI()
        {
            var puertos = new Dictionary<string, string>();

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string caption = obj["Caption"]?.ToString() ?? "";
                        string deviceId = obj["DeviceID"]?.ToString() ?? "";

                        // Extraer número de puerto COM del caption
                        var match = Regex.Match(caption, @"COM(\d+)");
                        if (match.Success)
                        {
                            string puerto = match.Value;
                            puertos[puerto] = caption;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en WMI query: {ex.Message}");
            }

            return puertos;
        }

        /// <summary>
        /// ✅ NUEVO: Diagnóstico completo del sistema
        /// </summary>
        public static async Task<string> DiagnosticarSistemaAsync()
        {
            var diagnostico = new StringBuilder();
            diagnostico.AppendLine("🔍 === DIAGNÓSTICO COMPLETO DEL SISTEMA DE BÁSCULA ===");
            diagnostico.AppendLine($"📅 Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            diagnostico.AppendLine();

            try
            {
                // 1. Información del sistema
                diagnostico.AppendLine("🖥️ INFORMACIÓN DEL SISTEMA:");
                diagnostico.AppendLine($"   • Sistema Operativo: {Environment.OSVersion}");
                diagnostico.AppendLine($"   • Versión .NET: {Environment.Version}");
                diagnostico.AppendLine($"   • Arquitectura: {Environment.Is64BitOperatingSystem} bits");
                diagnostico.AppendLine();

                // 2. Detección de puertos COM
                diagnostico.AppendLine("📋 DETECCIÓN DE PUERTOS COM:");
                var puertos = ObtenerPuertosDisponibles();

                if (puertos.Length == 0)
                {
                    diagnostico.AppendLine("   ❌ NO SE DETECTARON PUERTOS COM");
                    diagnostico.AppendLine("   💡 Posibles causas:");
                    diagnostico.AppendLine("      • No hay dispositivos serie conectados");
                    diagnostico.AppendLine("      • Drivers USB-Serie no instalados");
                    diagnostico.AppendLine("      • Problemas de hardware");
                }
                else
                {
                    diagnostico.AppendLine($"   ✅ {puertos.Length} puertos detectados:");

                    foreach (var puerto in puertos)
                    {
                        var estadoPuerto = await ProbrarDisponibilidadPuerto(puerto);
                        diagnostico.AppendLine($"      • {puerto}: {estadoPuerto}");
                    }
                }
                diagnostico.AppendLine();

                // 3. Información detallada WMI
                diagnostico.AppendLine("🔍 INFORMACIÓN DETALLADA DE DISPOSITIVOS:");
                try
                {
                    var puertosWMI = ObtenerPuertosViaWMI();
                    if (puertosWMI.Any())
                    {
                        foreach (var puerto in puertosWMI)
                        {
                            diagnostico.AppendLine($"   • {puerto.Key}: {puerto.Value}");
                        }
                    }
                    else
                    {
                        diagnostico.AppendLine("   ⚠️ No se encontraron dispositivos serie en WMI");
                    }
                }
                catch (Exception ex)
                {
                    diagnostico.AppendLine($"   ❌ Error consultando WMI: {ex.Message}");
                }
                diagnostico.AppendLine();

                // 4. Configuraciones predefinidas disponibles
                diagnostico.AppendLine("⚙️ CONFIGURACIONES PREDEFINIDAS DISPONIBLES:");
                var configuraciones = new[]
                {
                    ("RHINO BAR-8RS", ConfiguracionBascula.ConfiguracionRhino()),
                    ("OHAUS", ConfiguracionBascula.ConfiguracionOhaus()),
                    ("Mettler Toledo", ConfiguracionBascula.ConfiguracionMettler()),
                    ("Torrey", ConfiguracionBascula.ConfiguracionTorrey()),
                    ("EXCELL", ConfiguracionBascula.ConfiguracionExcell()),
                    ("Toledo", ConfiguracionBascula.ConfiguracionToledo()),
                    ("Genérica", ConfiguracionBascula.ConfiguracionGenerica())
                };

                foreach (var (nombre, config) in configuraciones)
                {
                    diagnostico.AppendLine($"   • {nombre}:");
                    diagnostico.AppendLine($"     - Velocidad: {config.BaudRate} bps");
                    diagnostico.AppendLine($"     - Formato: {config.DataBits}-{config.Parity}-{config.StopBits}");
                    diagnostico.AppendLine($"     - Comando: '{config.ComandoSolicitarPeso}'");
                    diagnostico.AppendLine($"     - Patrón: {config.PatronExtraccion}");
                }
                diagnostico.AppendLine();

                // 5. Recomendaciones
                diagnostico.AppendLine("💡 RECOMENDACIONES:");
                if (puertos.Length == 0)
                {
                    diagnostico.AppendLine("   🔧 ACCIONES RECOMENDADAS:");
                    diagnostico.AppendLine("      1. Verificar que la báscula esté conectada físicamente");
                    diagnostico.AppendLine("      2. Revisar el Administrador de Dispositivos de Windows");
                    diagnostico.AppendLine("      3. Instalar drivers USB-Serie si es necesario");
                    diagnostico.AppendLine("      4. Probar con otro cable USB/Serie");
                    diagnostico.AppendLine("      5. Reiniciar la báscula y el sistema");
                }
                else
                {
                    diagnostico.AppendLine("   ✅ Sistema preparado para configuración");
                    diagnostico.AppendLine("   🎯 Próximos pasos:");
                    diagnostico.AppendLine("      1. Seleccionar el puerto COM correcto");
                    diagnostico.AppendLine("      2. Elegir la configuración predefinida de su báscula");
                    diagnostico.AppendLine("      3. Usar 'Probar Conexión' para validar");
                    diagnostico.AppendLine("      4. Ajustar parámetros si es necesario");
                }

            }
            catch (Exception ex)
            {
                diagnostico.AppendLine($"💥 ERROR EN DIAGNÓSTICO: {ex}");
            }

            diagnostico.AppendLine();
            diagnostico.AppendLine("🔍 === FIN DEL DIAGNÓSTICO ===");

            return diagnostico.ToString();
        }

        /// <summary>
        /// Prueba la disponibilidad de un puerto específico
        /// </summary>
        private static async Task<string> ProbrarDisponibilidadPuerto(string puerto)
        {
            try
            {
                using (var serialPort = new SerialPort(puerto))
                {
                    serialPort.BaudRate = 9600;
                    serialPort.DataBits = 8;
                    serialPort.Parity = Parity.None;
                    serialPort.StopBits = StopBits.One;
                    serialPort.ReadTimeout = 500;
                    serialPort.WriteTimeout = 500;

                    serialPort.Open();
                    await Task.Delay(100);

                    bool disponible = serialPort.IsOpen;
                    serialPort.Close();

                    return disponible ? "✅ Disponible" : "❌ No disponible";
                }
            }
            catch (UnauthorizedAccessException)
            {
                return "⚠️ En uso por otra aplicación";
            }
            catch (ArgumentException)
            {
                return "❌ Puerto no válido";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }

        #endregion

        #region Métodos de Conexión

        /// <summary>
        /// ✅ MÉTODO MEJORADO: Prueba la conexión con diagnóstico detallado
        /// </summary>
        public async Task<ResultadoPruebaConexion> ProbarConexionAsync(ConfiguracionBascula config)
        {
            var stopwatch = Stopwatch.StartNew();
            SerialPort testPort = null;

            try
            {
                Debug.WriteLine("🔧 === INICIANDO PRUEBA DE CONEXIÓN ===");
                Debug.WriteLine($"   📋 Configuración: {config.ObtenerInfoDebug()}");
                Debug.WriteLine($"   🔗 Puerto: {config.Puerto}");
                Debug.WriteLine($"   ⚡ Velocidad: {config.BaudRate} bps");
                Debug.WriteLine($"   📊 Formato: {config.DataBits}-{config.Parity}-{config.StopBits}");
                Debug.WriteLine($"   🎛️ Control flujo: {config.Handshake}");
                Debug.WriteLine($"   📤 Comando: '{config.ComandoSolicitarPeso}'");
                Debug.WriteLine($"   🔍 Patrón: {config.PatronExtraccion}");

                // Validar configuración
                if (!config.ValidarConfiguracion())
                {
                    return ResultadoPruebaConexion.Error("Configuración inválida",
                        "Verifique que todos los campos estén correctamente configurados");
                }

                // Verificar que el puerto existe
                var puertosDisponibles = ObtenerPuertosDisponibles();
                if (!puertosDisponibles.Contains(config.Puerto))
                {
                    return ResultadoPruebaConexion.Error($"Puerto {config.Puerto} no disponible",
                        $"Puertos disponibles: {string.Join(", ", puertosDisponibles)}");
                }

                Debug.WriteLine("🔌 Configurando puerto serie...");

                // Configurar puerto serie
                testPort = new SerialPort
                {
                    PortName = config.Puerto,
                    BaudRate = config.BaudRate,
                    DataBits = config.DataBits,
                    Parity = config.Parity,
                    StopBits = config.StopBits,
                    Handshake = config.Handshake,
                    ReadTimeout = config.TimeoutLectura,
                    WriteTimeout = config.TimeoutLectura,
                    NewLine = config.ObtenerTerminadorReal()
                };

                Debug.WriteLine("📡 Abriendo puerto...");
                testPort.Open();

                if (!testPort.IsOpen)
                {
                    return ResultadoPruebaConexion.Error("No se pudo abrir el puerto",
                        "El puerto puede estar en uso por otra aplicación");
                }

                Debug.WriteLine("✅ Puerto abierto exitosamente");

                // Limpiar buffers
                testPort.DiscardInBuffer();
                testPort.DiscardOutBuffer();
                await Task.Delay(500); // Tiempo de estabilización

                string respuesta = "";
                decimal? pesoDetectado = null;

                // Si requiere solicitud de peso
                if (config.RequiereSolicitudPeso && !string.IsNullOrEmpty(config.ComandoSolicitarPeso))
                {
                    Debug.WriteLine($"📤 Enviando comando: '{config.ComandoSolicitarPeso}'");

                    // Enviar comando
                    var comandoBytes = config.ObtenerComandoComoBytes();
                    testPort.Write(comandoBytes, 0, comandoBytes.Length);

                    // Agregar terminador si es necesario
                    testPort.Write(config.ObtenerTerminadorReal());

                    Debug.WriteLine("⏳ Esperando respuesta...");
                    await Task.Delay(Math.Min(config.TimeoutLectura, 3000));

                    if (testPort.BytesToRead > 0)
                    {
                        respuesta = testPort.ReadExisting();
                        Debug.WriteLine($"📥 Respuesta recibida ({respuesta.Length} chars): '{respuesta}'");
                        Debug.WriteLine($"📥 Respuesta (hex): {string.Join(" ", respuesta.Select(c => ((int)c).ToString("X2")))}");
                    }
                    else
                    {
                        Debug.WriteLine("❌ No se recibió respuesta");
                        return ResultadoPruebaConexion.Error("La báscula no respondió al comando",
                            $"Comando enviado: '{config.ComandoSolicitarPeso}' - Sin respuesta en {config.TimeoutLectura}ms");
                    }
                }
                else
                {
                    Debug.WriteLine("👂 Escuchando datos automáticos...");
                    await Task.Delay(3000); // Esperar datos automáticos

                    if (testPort.BytesToRead > 0)
                    {
                        respuesta = testPort.ReadExisting();
                        Debug.WriteLine($"📥 Datos automáticos recibidos: '{respuesta}'");
                    }
                    else
                    {
                        Debug.WriteLine("❌ No se recibieron datos automáticos");
                        return ResultadoPruebaConexion.Error("No se recibieron datos de la báscula",
                            "La báscula debe enviar datos automáticamente o configurar comando de solicitud");
                    }
                }

                // Validar respuesta
                if (string.IsNullOrEmpty(respuesta))
                {
                    return ResultadoPruebaConexion.Error("Respuesta vacía de la báscula",
                        "Verifique la configuración de comunicación de la báscula");
                }

                // Extraer peso usando patrón regex
                try
                {
                    var regex = new Regex(config.PatronExtraccion);
                    var match = regex.Match(respuesta);

                    if (match.Success && match.Groups.Count > 1)
                    {
                        string pesoTexto = match.Groups[1].Value;
                        Debug.WriteLine($"🎯 Peso extraído: '{pesoTexto}'");

                        if (decimal.TryParse(pesoTexto, out decimal peso))
                        {
                            pesoDetectado = peso;
                            Debug.WriteLine($"⚖️ Peso parseado: {peso:F3} {config.UnidadPeso}");
                        }
                        else
                        {
                            Debug.WriteLine($"⚠️ No se pudo parsear el peso: '{pesoTexto}'");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ El patrón '{config.PatronExtraccion}' no coincidió con '{respuesta}'");
                        return ResultadoPruebaConexion.Error("El patrón no coincide con la respuesta",
                            $"Respuesta: '{respuesta}' - Patrón: '{config.PatronExtraccion}'");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error procesando patrón regex: {ex.Message}");
                    return ResultadoPruebaConexion.Error("Error en el patrón de extracción",
                        $"Verifique que el patrón regex sea válido: {ex.Message}");
                }

                stopwatch.Stop();
                Debug.WriteLine($"✅ Prueba exitosa en {stopwatch.ElapsedMilliseconds}ms");
                Debug.WriteLine("🔧 === FIN PRUEBA DE CONEXIÓN ===\n");

                return ResultadoPruebaConexion.Exito(respuesta, pesoDetectado, stopwatch.Elapsed);
            }
            catch (UnauthorizedAccessException)
            {
                return ResultadoPruebaConexion.Error($"Puerto {config.Puerto} en uso",
                    "El puerto está siendo utilizado por otra aplicación");
            }
            catch (ArgumentException ex)
            {
                return ResultadoPruebaConexion.Error("Error en parámetros de configuración", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ResultadoPruebaConexion.Error("Error de operación en puerto serie", ex.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 ERROR INESPERADO: {ex}");
                return ResultadoPruebaConexion.Error("Error inesperado", ex.Message);
            }
            finally
            {
                try
                {
                    testPort?.Close();
                    testPort?.Dispose();
                    Debug.WriteLine("🔐 Puerto de prueba cerrado y liberado");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Error cerrando puerto de prueba: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Conecta con la configuración activa
        /// </summary>
        public async Task<bool> ConectarAsync()
        {
            try
            {
                _configuracion = await _context.Set<ConfiguracionBascula>()
                    .FirstOrDefaultAsync(c => c.EsConfiguracionActiva);

                if (_configuracion == null)
                {
                    Debug.WriteLine("❌ No hay configuración activa de báscula");
                    return false;
                }

                return Conectar();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error conectando: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Conecta con la configuración cargada
        /// </summary>
        public bool Conectar()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_conectado)
                        Desconectar();

                    if (_configuracion == null)
                    {
                        Debug.WriteLine("❌ No hay configuración de báscula");
                        return false;
                    }

                    Debug.WriteLine($"🔗 Conectando a báscula: {_configuracion.ObtenerInfoDebug()}");

                    _serialPort = new SerialPort
                    {
                        PortName = _configuracion.Puerto,
                        BaudRate = _configuracion.BaudRate,
                        DataBits = _configuracion.DataBits,
                        Parity = _configuracion.Parity,
                        StopBits = _configuracion.StopBits,
                        Handshake = _configuracion.Handshake,
                        ReadTimeout = _configuracion.TimeoutLectura,
                        WriteTimeout = _configuracion.TimeoutLectura,
                        NewLine = _configuracion.ObtenerTerminadorReal()
                    };

                    _serialPort.DataReceived += SerialPort_DataReceived;
                    _serialPort.ErrorReceived += SerialPort_ErrorReceived;

                    _serialPort.Open();
                    _conectado = _serialPort.IsOpen;

                    if (_conectado)
                    {
                        Debug.WriteLine("✅ Báscula conectada exitosamente");
                        // Limpiar buffers
                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();
                    }

                    return _conectado;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error conectando báscula: {ex.Message}");
                    _conectado = false;
                    return false;
                }
            }
        }

        /// <summary>
        /// Desconecta la báscula
        /// </summary>
        public async Task DesconectarAsync()
        {
            await Task.Run(() => Desconectar());
        }

        public void Desconectar()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_serialPort != null)
                    {
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Close();
                        }
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                    _conectado = false;
                    Debug.WriteLine("🔐 Báscula desconectada");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Error desconectando: {ex.Message}");
                }
            }
        }

        #endregion

        #region Métodos de Lectura

        /// <summary>
        /// Lee el peso de la báscula
        /// </summary>
        /// <summary>
        /// ✅ MÉTODO MEJORADO: Lee el peso con validación robusta
        /// </summary>
        public async Task<decimal> LeerPesoAsync()
        {
            try
            {
                if (!_conectado || _serialPort == null || !_serialPort.IsOpen)
                {
                    Debug.WriteLine("❌ Báscula no conectada");
                    return 0; // ✅ CAMBIO: Devolver 0 en lugar de -1
                }

                // ✅ LIMPIAR BUFFERS ANTES DE LEER
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                if (_configuracion.RequiereSolicitudPeso)
                {
                    // Enviar comando para solicitar peso
                    var comando = _configuracion.ObtenerComandoComoBytes();
                    _serialPort.Write(comando, 0, comando.Length);
                    _serialPort.Write(_configuracion.ObtenerTerminadorReal());

                    Debug.WriteLine($"📤 Comando enviado: '{_configuracion.ComandoSolicitarPeso}'");
                }

                // Esperar respuesta (ajustar según tu báscula)
                await Task.Delay(Math.Max(_configuracion.IntervaloLectura, 200));

                if (_serialPort.BytesToRead > 0)
                {
                    string respuesta = _serialPort.ReadExisting();

                    // ✅ TOMAR SOLO LA ÚLTIMA LÍNEA (en caso de múltiples respuestas)
                    if (respuesta.Contains('\n'))
                    {
                        var lineas = respuesta.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        respuesta = lineas.LastOrDefault() ?? respuesta;
                        Debug.WriteLine($"📥 Múltiples líneas detectadas, usando última: '{respuesta}'");
                    }

                    // ✅ LOG DETALLADO
                    Debug.WriteLine($"📥 === RESPUESTA DE BÁSCULA ===");
                    Debug.WriteLine($"   📄 Texto: '{respuesta}'");
                    Debug.WriteLine($"   📊 Longitud: {respuesta.Length} caracteres");
                    Debug.WriteLine($"   🔢 Hex: {string.Join(" ", respuesta.Select(c => ((int)c).ToString("X2")))}");

                    decimal peso = ExtraerPesoMejorado(respuesta);

                    Debug.WriteLine($"   ⚖️ Peso extraído: {peso:F3} kg");
                    Debug.WriteLine($"📥 === FIN RESPUESTA ===\n");

                    // ✅ LIMPIAR BUFFER DESPUÉS DE LEER
                    _serialPort.DiscardInBuffer();

                    return peso;
                }

                Debug.WriteLine("⚠️ No hay datos disponibles para leer");
                return 0; // ✅ CAMBIO: Devolver 0 en lugar de -1
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error leyendo peso: {ex.Message}");
                Debug.WriteLine($"Stack: {ex.StackTrace}");
                ErrorOcurrido?.Invoke(this, ex.Message);
                return 0; // ✅ CAMBIO: Devolver 0 en lugar de -1
            }
        }

        /// <summary>
        /// ✅ NUEVO MÉTODO: Extracción de peso con limpieza robusta
        /// </summary>
        private decimal ExtraerPesoMejorado(string respuesta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(respuesta) || _configuracion == null)
                {
                    Debug.WriteLine("⚠️ Respuesta vacía o sin configuración");
                    return 0;
                }

                Debug.WriteLine($"🔍 === INICIANDO EXTRACCIÓN DE PESO ===");
                Debug.WriteLine($"   📄 Respuesta original: '{respuesta}'");
                Debug.WriteLine($"   🎯 Patrón regex: '{_configuracion.PatronExtraccion}'");

                // ✅ PASO 1: Intentar con el patrón configurado
                var regex = new Regex(_configuracion.PatronExtraccion);
                var match = regex.Match(respuesta);

                if (match.Success && match.Groups.Count > 1)
                {
                    string pesoTexto = match.Groups[1].Value.Trim();
                    Debug.WriteLine($"   ✅ Match exitoso: '{pesoTexto}'");

                    // ✅ PASO 2: Limpiar el texto extraído
                    string pesoLimpio = LimpiarTextoNumerico(pesoTexto);
                    Debug.WriteLine($"   🧹 Texto limpio: '{pesoLimpio}'");

                    // ✅ PASO 3: Parsear a decimal
                    if (decimal.TryParse(pesoLimpio,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal peso))
                    {
                        // ✅ PASO 4: VALIDACIONES CRÍTICAS
                        peso = Math.Abs(peso); // Siempre positivo

                        // ✅ DETECTAR SI ESTÁ EN GRAMOS (valor muy alto)
                        if (peso > 1000)
                        {
                            Debug.WriteLine($"   ⚠️ Peso sospechoso ({peso}), probablemente en gramos");
                            Debug.WriteLine($"   🔄 Convirtiendo a kilogramos: {peso} / 1000 = {peso / 1000m}");
                            peso = peso / 1000m;
                        }

                        // ✅ VALIDAR RANGO RAZONABLE
                        if (peso > 500) // Máximo 500 kg (ajustar según tu báscula)
                        {
                            Debug.WriteLine($"   ❌ Peso excede límite razonable (500 kg): {peso}");
                            return 0;
                        }

                        // ✅ VALIDAR MÍNIMO RAZONABLE
                        if (peso < 0.001m) // Menor a 1 gramo
                        {
                            Debug.WriteLine($"   ⚠️ Peso muy pequeño: {peso}");
                            return 0;
                        }

                        Debug.WriteLine($"   ✅ PESO VÁLIDO: {peso:F3} {_configuracion.UnidadPeso}");
                        Debug.WriteLine($"🔍 === FIN EXTRACCIÓN (EXITOSA) ===");
                        return peso;
                    }
                    else
                    {
                        Debug.WriteLine($"   ❌ No se pudo parsear: '{pesoLimpio}'");
                    }
                }
                else
                {
                    Debug.WriteLine($"   ⚠️ Patrón no coincidió");
                    Debug.WriteLine($"   🔄 Intentando extracción alternativa...");

                    // ✅ PASO ALTERNATIVO: Buscar cualquier número en el string
                    return ExtraerPesoAlternativo(respuesta);
                }

                Debug.WriteLine($"🔍 === FIN EXTRACCIÓN (SIN ÉXITO) ===");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error extrayendo peso: {ex.Message}");
                Debug.WriteLine($"Stack: {ex.StackTrace}");
                return 0;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Limpia texto numérico de caracteres no deseados
        /// </summary>
        private string LimpiarTextoNumerico(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return "0";

            // Limpiar caracteres comunes
            texto = texto
                .Replace("kg", "", StringComparison.OrdinalIgnoreCase)
                .Replace("g", "", StringComparison.OrdinalIgnoreCase)
                .Replace("lb", "", StringComparison.OrdinalIgnoreCase)
                .Replace("oz", "", StringComparison.OrdinalIgnoreCase)
                .Replace("ST", "", StringComparison.OrdinalIgnoreCase)  // Stable
                .Replace("US", "", StringComparison.OrdinalIgnoreCase)  // Unstable
                .Replace("NET", "", StringComparison.OrdinalIgnoreCase)
                .Replace("GS", "", StringComparison.OrdinalIgnoreCase)
                .Replace("NT", "", StringComparison.OrdinalIgnoreCase)
                .Replace("+", "")
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "")
                .Replace(",", ".") // ✅ CRÍTICO: Reemplazar coma por punto
                .Trim();

            return texto;
        }

        /// <summary>
        /// ✅ NUEVO: Método alternativo si el patrón principal falla
        /// </summary>
        private decimal ExtraerPesoAlternativo(string respuesta)
        {
            try
            {
                Debug.WriteLine($"   🔄 Extracción alternativa iniciada");

                // Limpiar todo el texto
                string limpio = LimpiarTextoNumerico(respuesta);
                Debug.WriteLine($"   🧹 Texto limpio completo: '{limpio}'");

                // Buscar cualquier patrón numérico
                var regexNumero = new Regex(@"[-+]?\d+\.?\d*");
                var matchNumero = regexNumero.Match(limpio);

                if (matchNumero.Success)
                {
                    string numeroStr = matchNumero.Value;
                    Debug.WriteLine($"   🔢 Número encontrado: '{numeroStr}'");

                    if (decimal.TryParse(numeroStr,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal peso))
                    {
                        peso = Math.Abs(peso);

                        // Validar si está en gramos
                        if (peso > 1000)
                        {
                            Debug.WriteLine($"   🔄 Convirtiendo de gramos: {peso} / 1000");
                            peso = peso / 1000m;
                        }

                        if (peso > 0 && peso <= 500)
                        {
                            Debug.WriteLine($"   ✅ Peso alternativo válido: {peso:F3}");
                            return peso;
                        }
                    }
                }

                Debug.WriteLine($"   ❌ Extracción alternativa sin éxito");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"   ❌ Error en extracción alternativa: {ex.Message}");
                return 0;
            }
        }
        /// <summary>
        /// ✅ MÉTODO AGREGADO: Tarar la báscula
        /// </summary>
        public async Task<bool> TararAsync()
        {
            try
            {
                if (!EstaConectada || _configuracion == null)
                {
                    Debug.WriteLine("❌ Báscula no conectada para tarar");
                    return false;
                }

                Debug.WriteLine($"⚖️ Enviando comando de tarado: '{_configuracion.ComandoTara}'");

                // ✅ CORRECCIÓN: Crear bytes del comando de tarar manualmente
                byte[] comandoBytes;
                var comandoTara = _configuracion.ComandoTara ?? "T";

                // Detectar si el comando es hexadecimal, decimal o ASCII
                if (comandoTara.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // Comando hexadecimal
                    string hex = comandoTara.Substring(2);
                    comandoBytes = Convert.FromHexString(hex);
                }
                else if (int.TryParse(comandoTara, out int valorDecimal))
                {
                    // Comando decimal
                    comandoBytes = new byte[] { (byte)valorDecimal };
                }
                else
                {
                    // Comando ASCII
                    comandoBytes = Encoding.ASCII.GetBytes(comandoTara);
                }

                _serialPort.Write(comandoBytes, 0, comandoBytes.Length);
                _serialPort.Write(_configuracion.ObtenerTerminadorReal());

                // Esperar que se complete el tarado
                await Task.Delay(1500);

                Debug.WriteLine("✅ Comando de tarado enviado");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al tarar báscula: {ex.Message}");
                ErrorOcurrido?.Invoke(this, $"Error al tarar: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extrae el peso de una respuesta usando el patrón configurado
        /// </summary>
        private decimal ExtraerPeso(string respuesta)
        {
            return ExtraerPesoMejorado(respuesta);
        }

        #endregion

        #region Event Handlers

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_lockDataReceived)
            {
                try
                {
                    // ✅ EVITAR PROCESAMIENTO MÚLTIPLE EN CORTO TIEMPO
                    var ahora = DateTime.Now;
                    if ((ahora - _ultimoEventoDataReceived).TotalMilliseconds < 300)
                    {
                        Debug.WriteLine($"⏭️ Evento DataReceived ignorado (muy rápido)");
                        return;
                    }
                    _ultimoEventoDataReceived = ahora;

                    if (_serialPort?.IsOpen != true || _serialPort.BytesToRead <= 0)
                        return;

                    // ✅ LEER TODO EL BUFFER DISPONIBLE
                    string datos = _serialPort.ReadExisting();

                    // ✅ TOMAR SOLO LA ÚLTIMA LÍNEA (dato más reciente)
                    string datoActual = datos;
                    if (datos.Contains('\n'))
                    {
                        var lineas = datos.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        datoActual = lineas.LastOrDefault() ?? datos;
                        Debug.WriteLine($"📥 Múltiples líneas recibidas, usando última: '{datoActual}'");
                    }
                    else
                    {
                        Debug.WriteLine($"📥 Datos recibidos: '{datoActual}'");
                    }

                    // ✅ LIMPIAR BUFFER DESPUÉS DE LEER
                    _serialPort.DiscardInBuffer();

                    // Notificar datos crudos
                    DatosRecibidos?.Invoke(this, datoActual);

                    // ✅ Extraer peso actual (NO acumulado)
                    decimal peso = ExtraerPeso(datoActual);

                    if (peso > 0) // ✅ CAMBIO: Solo emitir si es mayor a 0
                    {
                        Debug.WriteLine($"⚖️ Peso actual emitido: {peso:F3} kg");
                        PesoRecibido?.Invoke(this, new PesoRecibidoEventArgs(peso));
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ Peso inválido o cero: {peso}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error procesando datos recibidos: {ex.Message}");
                    ErrorOcurrido?.Invoke(this, ex.Message);
                }
            }
        }
        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Debug.WriteLine($"❌ Error en puerto serie: {e.EventType}");
            ErrorOcurrido?.Invoke(this, $"Error de comunicación: {e.EventType}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Desconectar();
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    // ✅ CLASES DE EVENTOS
    public class PesoRecibidoEventArgs : EventArgs
    {
        public decimal Peso { get; }

        public PesoRecibidoEventArgs(decimal peso)
        {
            Peso = peso;
        }
    }
}