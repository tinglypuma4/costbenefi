using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio de sincronización entre servidor y terminales
    /// </summary>
    public class SyncService : IDisposable
    {
        private readonly NetworkService _networkService;
        private readonly ConfiguracionSistema _config;
        private Timer? _syncTimer;
        private bool _syncEnProceso = false;
        private bool _disposed = false;
        private DateTime _ultimaSincronizacion = DateTime.MinValue;

        // Eventos
        public event EventHandler<SyncEventArgs>? SincronizacionCompletada;
        public event EventHandler<SyncErrorEventArgs>? ErrorSincronizacion;
        public event EventHandler<string>? EstadoCambiado;

        public SyncService(NetworkService networkService)
        {
            _networkService = networkService;
            _config = ConfiguracionSistema.Instance;

            Debug.WriteLine("🔄 SyncService inicializado");
        }

        /// <summary>
        /// Inicia la sincronización automática
        /// </summary>
        public void IniciarSincronizacionAutomatica()
        {
            if (!_config.SincronizacionActiva) return;

            var intervalo = TimeSpan.FromMinutes(_config.IntervaloSincronizacionMinutos);

            _syncTimer = new Timer(async _ => await SincronizarPeriodicamente(),
                                  null,
                                  TimeSpan.FromSeconds(10), // Primer sync en 10 segundos
                                  intervalo);

            EstadoCambiado?.Invoke(this, "Sincronización automática iniciada");
            Debug.WriteLine($"🔄 Sync automático cada {_config.IntervaloSincronizacionMinutos} minutos");
        }

        /// <summary>
        /// Detiene la sincronización automática
        /// </summary>
        public void DetenerSincronizacionAutomatica()
        {
            _syncTimer?.Dispose();
            _syncTimer = null;

            EstadoCambiado?.Invoke(this, "Sincronización automática detenida");
            Debug.WriteLine("⏸️ Sync automático detenido");
        }

        /// <summary>
        /// Sincronización periódica automática
        /// </summary>
        private async Task SincronizarPeriodicamente()
        {
            try
            {
                if (_syncEnProceso)
                {
                    Debug.WriteLine("⏳ Sync ya en proceso, omitiendo...");
                    return;
                }

                await SincronizarAhora();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en sync periódico: {ex.Message}");

                ErrorSincronizacion?.Invoke(this, new SyncErrorEventArgs
                {
                    Error = ex,
                    Mensaje = "Error en sincronización periódica"
                });
            }
        }

        /// <summary>
        /// Fuerza una sincronización inmediata
        /// </summary>
        public async Task<bool> SincronizarAhora()
        {
            if (_syncEnProceso)
            {
                Debug.WriteLine("⏳ Sincronización ya en proceso");
                return false;
            }

            _syncEnProceso = true;
            EstadoCambiado?.Invoke(this, "Sincronizando...");

            try
            {
                Debug.WriteLine("🔄 Iniciando sincronización manual...");

                // Verificar conectividad
                var conectividad = await _networkService.ProbarConectividad();
                if (!conectividad)
                {
                    throw new Exception("Sin conectividad al servidor");
                }

                // Solicitar cambios del servidor
                var solicitud = new SolicitudCambios
                {
                    TerminalId = _config.NombreTerminal,
                    UltimaSync = _ultimaSincronizacion,
                    TiposRequeridos = new System.Collections.Generic.List<string>
                    {
                        "productos", "servicios", "promociones"
                    }
                };

                var response = await _networkService.PostAsync<CambiosServidor>(
                    "/api/sync/cambios", solicitud);

                if (response.IsSuccess && response.Data != null)
                {
                    var cambios = response.Data;

                    Debug.WriteLine($"📦 Recibidos {cambios.TotalCambios} cambios del servidor");

                    // Actualizar timestamp
                    _ultimaSincronizacion = DateTime.Now;

                    // Notificar éxito
                    SincronizacionCompletada?.Invoke(this, new SyncEventArgs
                    {
                        Exitosa = true,
                        CambiosRecibidos = cambios.TotalCambios,
                        UltimaSincronizacion = _ultimaSincronizacion,
                        Mensaje = $"Sincronización completada: {cambios.TotalCambios} cambios"
                    });

                    EstadoCambiado?.Invoke(this, $"Última sync: {DateTime.Now:HH:mm:ss}");

                    return true;
                }
                else
                {
                    throw new Exception($"Error obteniendo cambios: {response.Error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en sincronización: {ex.Message}");

                ErrorSincronizacion?.Invoke(this, new SyncErrorEventArgs
                {
                    Error = ex,
                    Mensaje = ex.Message
                });

                EstadoCambiado?.Invoke(this, $"Error: {ex.Message}");

                return false;
            }
            finally
            {
                _syncEnProceso = false;
            }
        }

        /// <summary>
        /// Envía cambios locales al servidor
        /// </summary>
        public async Task<bool> EnviarCambiosLocales(CambiosLocales cambios)
        {
            try
            {
                Debug.WriteLine($"📤 Enviando {cambios.TotalCambios} cambios al servidor...");

                var response = await _networkService.PostAsync<RespuestaSincronizacion>(
                    "/api/sync/recibir-cambios", cambios);

                if (response.IsSuccess && response.Data?.Exitosa == true)
                {
                    Debug.WriteLine($"✅ Cambios enviados exitosamente");
                    return true;
                }
                else
                {
                    var mensaje = response.Data?.Mensaje ?? response.Error;
                    Debug.WriteLine($"❌ Error enviando cambios: {mensaje}");

                    ErrorSincronizacion?.Invoke(this, new SyncErrorEventArgs
                    {
                        Error = new Exception(mensaje),
                        Mensaje = "Error enviando cambios al servidor"
                    });

                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error enviando cambios: {ex.Message}");

                ErrorSincronizacion?.Invoke(this, new SyncErrorEventArgs
                {
                    Error = ex,
                    Mensaje = "Error enviando cambios al servidor"
                });

                return false;
            }
        }

        /// <summary>
        /// Obtiene estadísticas del servidor
        /// </summary>
        public async Task<object?> ObtenerEstadisticasServidor()
        {
            try
            {
                var response = await _networkService.GetAsync<object>("/api/sync/estadisticas");
                return response.IsSuccess ? response.Data : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error obteniendo estadísticas: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Propiedades de estado
        /// </summary>
        public bool SyncEnProceso => _syncEnProceso;
        public DateTime UltimaSincronizacion => _ultimaSincronizacion;
        public bool SincronizacionAutomaticaActiva => _syncTimer != null;

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            DetenerSincronizacionAutomatica();

            Debug.WriteLine("🗑️ SyncService disposed");
        }
    }

    #region Event Args

    public class SyncEventArgs : EventArgs
    {
        public bool Exitosa { get; set; }
        public int CambiosRecibidos { get; set; }
        public int CambiosEnviados { get; set; }
        public DateTime UltimaSincronizacion { get; set; }
        public string Mensaje { get; set; } = "";
    }

    public class SyncErrorEventArgs : EventArgs
    {
        public Exception Error { get; set; } = new();
        public string Mensaje { get; set; } = "";
        public DateTime FechaError { get; set; } = DateTime.Now;
    }

    #endregion
}