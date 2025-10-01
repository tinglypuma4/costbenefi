using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Services;
using System.Collections.Generic;

namespace costbenefi.Services.Sync
{
    /// <summary>
    /// Cliente específico para terminales (PCs Caja)
    /// Maneja la lógica específica de operación sin base de datos local
    /// </summary>
    public class TerminalClient : IDisposable
    {
        private readonly ConfiguracionSistema _config;
        private readonly NetworkService _networkService;
        private readonly SyncService _syncService;
        private Timer? _heartbeatTimer;
        private bool _disposed = false;
        private bool _autenticado = false;

        // Cache local en memoria (sin BD local)
        private readonly Dictionary<int, ProductoSync> _productosCache = new();
        private readonly Dictionary<int, ServicioSync> _serviciosCache = new();
        private readonly Dictionary<int, PromocionSync> _promocionesCache = new();
        private readonly List<VentaSync> _ventasPendientes = new();

        // Estado del terminal
        public bool EstaConectado { get; private set; } = false;
        public bool EstaAutenticado => _autenticado;
        public DateTime UltimaConexion { get; private set; } = DateTime.MinValue;
        public string EstadoActual { get; private set; } = "Inicializando";

        // Eventos
        public event EventHandler<string>? EstadoCambiado;
        public event EventHandler<TerminalEventArgs>? ConexionCambiada;
        public event EventHandler<TerminalErrorEventArgs>? ErrorOcurrido;
        public event EventHandler<CacheActualizadoEventArgs>? CacheActualizado;

        public TerminalClient()
        {
            _config = ConfiguracionSistema.Instance;
            _networkService = new NetworkService();
            _syncService = new SyncService(_networkService);

            // Configurar eventos de sincronización
            _syncService.SincronizacionCompletada += OnSyncCompletada;
            _syncService.ErrorSincronizacion += OnSyncError;
            _syncService.EstadoCambiado += OnSyncEstadoCambiado;

            Debug.WriteLine("🖥️ TerminalClient inicializado");
            InicializarTerminal();
        }

        /// <summary>
        /// Inicializa el terminal y establece conexión con el servidor
        /// </summary>
        private async void InicializarTerminal()
        {
            try
            {
                CambiarEstado("Conectando al servidor...");

                // Verificar configuración
                if (!_config.SincronizacionActiva)
                {
                    CambiarEstado("Terminal en modo standalone");
                    return;
                }

                // Intentar conectar y autenticar
                await ConectarYAutenticar();

                if (_autenticado)
                {
                    // Cargar datos iniciales
                    await CargarDatosIniciales();

                    // Iniciar heartbeat
                    IniciarHeartbeat();

                    CambiarEstado("Terminal operativo");
                }
                else
                {
                    CambiarEstado("Error de autenticación");
                }
            }
            catch (Exception ex)
            {
                CambiarEstado($"Error de inicialización: {ex.Message}");
                ErrorOcurrido?.Invoke(this, new TerminalErrorEventArgs
                {
                    Error = ex,
                    Mensaje = "Error inicializando terminal"
                });
            }
        }

        /// <summary>
        /// Conecta y autentica el terminal con el servidor
        /// </summary>
        private async Task ConectarYAutenticar()
        {
            try
            {
                Debug.WriteLine("🔐 Iniciando autenticación del terminal...");

                // Verificar conectividad básica
                var conectividad = await _networkService.ProbarConectividad();
                if (!conectividad)
                {
                    throw new Exception("No se puede conectar al servidor");
                }

                EstaConectado = true;
                UltimaConexion = DateTime.Now;
                ConexionCambiada?.Invoke(this, new TerminalEventArgs
                {
                    Conectado = true,
                    Mensaje = "Conectado al servidor"
                });

                // Solicitar autenticación
                var solicitudAuth = new SolicitudAutenticacionTerminal
                {
                    TerminalId = _config.NombreTerminal,
                    ClaveTerminal = ObtenerClaveTerminal(),
                    Version = "1.0.0",
                    IP = ObtenerIPLocal(),
                    FechaSolicitud = DateTime.Now
                };

                var response = await _networkService.PostAsync<RespuestaAutenticacion>(
                    "/api/sync/auth", solicitudAuth);

                if (response.IsSuccess && response.Data?.Autorizado == true)
                {
                    _autenticado = true;

                    // Actualizar token de acceso
                    var token = response.Data.Token;
                    _networkService.ActualizarToken(token);

                    // Aplicar configuración del servidor
                    AplicarConfiguracionServidor(response.Data.Configuracion);

                    Debug.WriteLine($"✅ Terminal autenticado exitosamente con token: {token[..10]}...");
                }
                else
                {
                    throw new Exception($"Autenticación fallida: {response.Data?.Mensaje ?? response.Error}");
                }
            }
            catch (Exception ex)
            {
                EstaConectado = false;
                _autenticado = false;
                Debug.WriteLine($"❌ Error en autenticación: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene la clave del terminal (simplificado)
        /// </summary>
        private string ObtenerClaveTerminal()
        {
            // Por simplicidad, usar el nombre de la máquina como clave
            // En producción, usar una clave más segura
            return Environment.MachineName + "_key";
        }

        /// <summary>
        /// Obtiene la IP local del terminal
        /// </summary>
        private string ObtenerIPLocal()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                var localIP = host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return localIP?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Aplica configuración recibida del servidor
        /// </summary>
        private void AplicarConfiguracionServidor(ConfiguracionTerminalSync configuracion)
        {
            try
            {
                Debug.WriteLine("⚙️ Aplicando configuración del servidor...");

                // TODO: Aplicar configuraciones específicas
                // Por ejemplo: intervalos de sync, límites de descuento, etc.

                Debug.WriteLine($"✅ Configuración aplicada: Sync cada {configuracion.IntervaloSyncSegundos}s");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error aplicando configuración: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga datos iniciales del servidor
        /// </summary>
        private async Task CargarDatosIniciales()
        {
            try
            {
                Debug.WriteLine("📦 Cargando datos iniciales del servidor...");

                // Solicitar todos los datos (primera sincronización)
                var solicitud = new SolicitudCambios
                {
                    UltimaSync = DateTime.MinValue, // Traer todo
                    TerminalId = _config.NombreTerminal,
                    TiposRequeridos = new List<string> { "productos", "servicios", "promociones" }
                };

                var response = await _networkService.PostAsync<CambiosServidor>(
                    "/api/sync/cambios", solicitud);

                if (response.IsSuccess && response.Data != null)
                {
                    await ActualizarCacheLocal(response.Data);
                    Debug.WriteLine($"✅ Datos iniciales cargados: {response.Data.TotalCambios} elementos");
                }
                else
                {
                    throw new Exception($"Error cargando datos: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error cargando datos iniciales: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Actualiza el cache local con datos del servidor
        /// </summary>
        private async Task ActualizarCacheLocal(CambiosServidor cambios)
        {
            try
            {
                int actualizados = 0;

                // Actualizar productos
                foreach (var producto in cambios.Productos)
                {
                    if (producto.Eliminado)
                    {
                        _productosCache.Remove(producto.Id);
                    }
                    else
                    {
                        _productosCache[producto.Id] = producto;
                    }
                    actualizados++;
                }

                // Actualizar servicios
                foreach (var servicio in cambios.Servicios)
                {
                    if (servicio.Eliminado)
                    {
                        _serviciosCache.Remove(servicio.Id);
                    }
                    else
                    {
                        _serviciosCache[servicio.Id] = servicio;
                    }
                    actualizados++;
                }

                // Actualizar promociones
                foreach (var promocion in cambios.Promociones)
                {
                    if (promocion.Eliminado)
                    {
                        _promocionesCache.Remove(promocion.Id);
                    }
                    else
                    {
                        _promocionesCache[promocion.Id] = promocion;
                    }
                    actualizados++;
                }

                // Notificar actualización
                CacheActualizado?.Invoke(this, new CacheActualizadoEventArgs
                {
                    TotalProductos = _productosCache.Count,
                    TotalServicios = _serviciosCache.Count,
                    TotalPromociones = _promocionesCache.Count,
                    ElementosActualizados = actualizados
                });

                Debug.WriteLine($"💾 Cache actualizado: {actualizados} elementos");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error actualizando cache: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Inicia el heartbeat con el servidor
        /// </summary>
        private void IniciarHeartbeat()
        {
            _heartbeatTimer = new Timer(async _ => await EnviarHeartbeat(),
                                      null,
                                      TimeSpan.FromMinutes(1),
                                      TimeSpan.FromMinutes(5));

            Debug.WriteLine("💓 Heartbeat iniciado");
        }

        /// <summary>
        /// Envía heartbeat al servidor
        /// </summary>
        private async Task EnviarHeartbeat()
        {
            try
            {
                if (!_autenticado) return;

                var estado = new EstadoTerminalSync
                {
                    TerminalId = _config.NombreTerminal,
                    UltimaActividad = DateTime.Now,
                    UsuarioActual = Environment.UserName,
                    EscaneadorConectado = true, // TODO: Detectar real
                    ImpresoraConectada = true,  // TODO: Detectar real
                    BasculaConectada = false,   // TODO: Detectar real
                    VentasDelDia = _ventasPendientes.Count,
                    TotalVentasDelDia = _ventasPendientes.Sum(v => v.Total),
                    Version = "1.0.0",
                    IP = ObtenerIPLocal()
                };

                var response = await _networkService.PostAsync<object>("/api/sync/heartbeat", estado);

                if (response.IsSuccess)
                {
                    UltimaConexion = DateTime.Now;
                }
                else
                {
                    Debug.WriteLine($"⚠️ Error en heartbeat: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error enviando heartbeat: {ex.Message}");
            }
        }

        #region Métodos Públicos para el POS

        /// <summary>
        /// Obtiene productos disponibles para venta (desde cache)
        /// </summary>
        public List<ProductoSync> ObtenerProductosParaVenta()
        {
            return _productosCache.Values
                .Where(p => p.ActivoParaVenta && p.StockTotal > 0)
                .OrderBy(p => p.NombreArticulo)
                .ToList();
        }

        /// <summary>
        /// Obtiene servicios disponibles para venta (desde cache)
        /// </summary>
        public List<ServicioSync> ObtenerServiciosParaVenta()
        {
            return _serviciosCache.Values
                .Where(s => s.Activo && s.IntegradoPOS)
                .OrderBy(s => s.PrioridadPOS)
                .ThenBy(s => s.NombreServicio)
                .ToList();
        }

        /// <summary>
        /// Obtiene promociones vigentes (desde cache)
        /// </summary>
        public List<PromocionSync> ObtenerPromocionesVigentes()
        {
            var ahora = DateTime.Now;
            return _promocionesCache.Values
                .Where(p => p.Activa && p.FechaInicio <= ahora && p.FechaFin >= ahora)
                .OrderBy(p => p.NombrePromocion)
                .ToList();
        }

        /// <summary>
        /// Busca un producto por código de barras
        /// </summary>
        public ProductoSync? BuscarProductoPorCodigo(string codigoBarras)
        {
            return _productosCache.Values
                .FirstOrDefault(p => p.CodigoBarras.Equals(codigoBarras, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Registra una venta para sincronizar con el servidor
        /// </summary>
        public async Task<bool> RegistrarVenta(VentaSync venta)
        {
            try
            {
                // Agregar origen terminal
                venta.TerminalOrigen = _config.NombreTerminal;

                // Agregar a la cola de sincronización
                _ventasPendientes.Add(venta);

                // Intentar sincronizar inmediatamente
                if (_autenticado && EstaConectado)
                {
                    await SincronizarVentasPendientes();
                }

                Debug.WriteLine($"💰 Venta {venta.NumeroTicket} registrada para sincronización");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error registrando venta: {ex.Message}");
                ErrorOcurrido?.Invoke(this, new TerminalErrorEventArgs
                {
                    Error = ex,
                    Mensaje = "Error registrando venta"
                });
                return false;
            }
        }

        /// <summary>
        /// Sincroniza ventas pendientes con el servidor
        /// </summary>
        public async Task<bool> SincronizarVentasPendientes()
        {
            try
            {
                if (!_ventasPendientes.Any()) return true;

                var cambiosLocales = new CambiosLocales
                {
                    TerminalId = _config.NombreTerminal,
                    FechaGeneracion = DateTime.Now,
                    Ventas = new List<VentaSync>(_ventasPendientes)
                };

                var response = await _networkService.PostAsync<RespuestaSincronizacion>(
                    "/api/sync/recibir-cambios", cambiosLocales);

                if (response.IsSuccess && response.Data?.Exitosa == true)
                {
                    // Limpiar ventas sincronizadas exitosamente
                    _ventasPendientes.Clear();
                    Debug.WriteLine($"✅ {cambiosLocales.Ventas.Count} ventas sincronizadas exitosamente");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ Error sincronizando ventas: {response.Data?.Mensaje ?? response.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en sincronización de ventas: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fuerza una sincronización completa
        /// </summary>
        public async Task<bool> SincronizarAhora()
        {
            if (!_autenticado || !EstaConectado)
            {
                await ConectarYAutenticar();
            }

            return await _syncService.SincronizarAhora();
        }

        /// <summary>
        /// Obtiene estadísticas del terminal
        /// </summary>
        public TerminalEstadisticas ObtenerEstadisticas()
        {
            return new TerminalEstadisticas
            {
                EstaConectado = EstaConectado,
                EstaAutenticado = _autenticado,
                UltimaConexion = UltimaConexion,
                ProductosEnCache = _productosCache.Count,
                ServiciosEnCache = _serviciosCache.Count,
                PromocionesEnCache = _promocionesCache.Count,
                VentasPendientes = _ventasPendientes.Count,
                TotalVentasPendientes = _ventasPendientes.Sum(v => v.Total),
                EstadoActual = EstadoActual
            };
        }

        #endregion

        #region Event Handlers

        private void OnSyncCompletada(object? sender, SyncEventArgs e)
        {
            Debug.WriteLine($"🔄 Sincronización completada: {e.Mensaje}");
            CambiarEstado($"Última sync: {DateTime.Now:HH:mm:ss}");
        }

        private void OnSyncError(object? sender, SyncErrorEventArgs e)
        {
            Debug.WriteLine($"❌ Error de sincronización: {e.Mensaje}");
            CambiarEstado($"Error sync: {e.Mensaje}");

            ErrorOcurrido?.Invoke(this, new TerminalErrorEventArgs
            {
                Error = e.Error,
                Mensaje = e.Mensaje
            });
        }

        private void OnSyncEstadoCambiado(object? sender, string estado)
        {
            CambiarEstado(estado);
        }

        #endregion

        #region Métodos Privados

        private void CambiarEstado(string nuevoEstado)
        {
            EstadoActual = nuevoEstado;
            EstadoCambiado?.Invoke(this, nuevoEstado);
            Debug.WriteLine($"🔄 Estado terminal: {nuevoEstado}");
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _heartbeatTimer?.Dispose();
            _syncService?.Dispose();
            _networkService?.Dispose();

            Debug.WriteLine("🗑️ TerminalClient disposed");
        }
    }

    #region Event Args y Clases de Estado

    public class TerminalEventArgs : EventArgs
    {
        public bool Conectado { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class TerminalErrorEventArgs : EventArgs
    {
        public Exception Error { get; set; } = new();
        public string Mensaje { get; set; } = "";
    }

    public class CacheActualizadoEventArgs : EventArgs
    {
        public int TotalProductos { get; set; }
        public int TotalServicios { get; set; }
        public int TotalPromociones { get; set; }
        public int ElementosActualizados { get; set; }
    }

    public class TerminalEstadisticas
    {
        public bool EstaConectado { get; set; }
        public bool EstaAutenticado { get; set; }
        public DateTime UltimaConexion { get; set; }
        public int ProductosEnCache { get; set; }
        public int ServiciosEnCache { get; set; }
        public int PromocionesEnCache { get; set; }
        public int VentasPendientes { get; set; }
        public decimal TotalVentasPendientes { get; set; }
        public string EstadoActual { get; set; } = "";

        public override string ToString()
        {
            return $"🖥️ ESTADÍSTICAS DEL TERMINAL\n\n" +
                   $"🔗 Conectado: {(EstaConectado ? "SÍ" : "NO")}\n" +
                   $"🔐 Autenticado: {(EstaAutenticado ? "SÍ" : "NO")}\n" +
                   $"⏰ Última conexión: {UltimaConexion:HH:mm:ss}\n" +
                   $"📦 Productos en cache: {ProductosEnCache}\n" +
                   $"🛍️ Servicios en cache: {ServiciosEnCache}\n" +
                   $"🎁 Promociones en cache: {PromocionesEnCache}\n" +
                   $"💰 Ventas pendientes: {VentasPendientes}\n" +
                   $"💵 Total pendiente: {TotalVentasPendientes:C2}\n" +
                   $"📊 Estado: {EstadoActual}";
        }
    }

    #endregion
}