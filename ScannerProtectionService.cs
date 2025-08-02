


using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio avanzado de protección anti-autoclick para escáneres
    /// Versión mejorada con configuración flexible y mejor rendimiento
    /// </summary>
    public class ScannerProtectionService : IDisposable
    {
        #region Configuración
        public class ConfiguracionProteccion
        {
            public int TiempoProteccionMs { get; set; } = 3000;        // 3 segundos por defecto
            public int TiempoBloqueoBotonMs { get; set; } = 2000;      // 2 segundos por defecto
            public int TiempoEscaneoRecienteMs { get; set; } = 5000;   // 5 segundos por defecto
            public bool LogDebugActivo { get; set; } = true;
            public List<string> BotonesCriticos { get; set; } = new List<string>
            {
                "BtnCerrarSesionPOS",
                "BtnSalirSistema"
            };
        }
        #endregion

        #region Variables Privadas
        private readonly Window _ventanaPrincipal;
        private readonly ConfiguracionProteccion _config;
        private readonly object _lock = new object();

        private DateTime _ultimoEscaneo = DateTime.MinValue;
        private bool _escaneando = false;
        private bool _disposed = false;

        private DispatcherTimer _timerProteccion;
        private readonly Dictionary<string, DispatcherTimer> _timersReactivacion = new();

        // Estadísticas para debugging
        private int _escaneosTotales = 0;
        private int _accionesBloqueadas = 0;
        private int _clicksAutomaticosDetectados = 0;
        #endregion

        #region Constructor y Inicialización
        public ScannerProtectionService(Window ventanaPrincipal, ConfiguracionProteccion config = null)
        {
            _ventanaPrincipal = ventanaPrincipal ?? throw new ArgumentNullException(nameof(ventanaPrincipal));
            _config = config ?? new ConfiguracionProteccion();

            InicializarTimer();
            LogDebug("✅ ScannerProtectionService inicializado");
        }

        private void InicializarTimer()
        {
            _timerProteccion = new DispatcherTimer();
            _timerProteccion.Interval = TimeSpan.FromMilliseconds(_config.TiempoProteccionMs);
            _timerProteccion.Tick += (s, e) =>
            {
                lock (_lock)
                {
                    _escaneando = false;
                    _timerProteccion.Stop();
                    LogDebug("✅ Protección de escáner desactivada automáticamente");
                }
            };
        }
        #endregion

        #region Métodos Públicos Principales
        /// <summary>
        /// Método principal - llamar cuando se detecta un escaneo
        /// </summary>
        public void OnProductoEscaneado(string codigoBarras = "", string nombreProducto = "")
        {
            if (_disposed) return;

            try
            {
                lock (_lock)
                {
                    _escaneosTotales++;
                    _ultimoEscaneo = DateTime.Now;
                    _escaneando = true;

                    LogDebug($"🔍 Escaneo #{_escaneosTotales}: {codigoBarras} - {nombreProducto}");
                    LogDebug("🛡️ Protección ACTIVADA");

                    // Bloquear botones críticos
                    BloquearBotonesCriticos();

                    // Reiniciar timer de protección
                    _timerProteccion.Stop();
                    _timerProteccion.Start();

                    // Actualizar status
                    MostrarStatusProtegido($"Producto escaneado: {nombreProducto}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error en OnProductoEscaneado: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida si una acción debe ser bloqueada (thread-safe)
        /// </summary>
        public bool DebeBloquearAccion(string accion = "")
        {
            if (_disposed) return false;

            try
            {
                lock (_lock)
                {
                    // Si está escaneando activamente
                    if (_escaneando)
                    {
                        _accionesBloqueadas++;
                        LogDebug($"🛡️ BLOQUEADO [{_accionesBloqueadas}]: {accion} - Escáner activo");
                        return true;
                    }

                    // Si fue escaneo muy reciente
                    var tiempoTranscurrido = (DateTime.Now - _ultimoEscaneo).TotalMilliseconds;
                    if (tiempoTranscurrido < _config.TiempoEscaneoRecienteMs && _ultimoEscaneo != DateTime.MinValue)
                    {
                        _accionesBloqueadas++;
                        LogDebug($"🛡️ BLOQUEADO [{_accionesBloqueadas}]: {accion} - Escaneo reciente ({tiempoTranscurrido:F0}ms)");
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error validando bloqueo: {ex.Message}");
                return false; // En caso de error, permitir acción por seguridad
            }
        }

        /// <summary>
        /// Valida si un click es automático - versión mejorada
        /// </summary>
        public bool EsClickAutomatico(Button boton, bool verificarFocoEstricto = false)
        {
            if (_disposed || boton == null) return true;

            try
            {
                // Verificación básica - debe tener interacción real
                bool tieneMouseOver = boton.IsMouseOver;
                bool tieneFoco = boton.IsFocused;
                bool estaEnabled = boton.IsEnabled;
                bool esVisible = boton.IsVisible;

                // Logs para debugging
                LogDebug($"🔍 Validando click: MouseOver={tieneMouseOver}, Foco={tieneFoco}, Enabled={estaEnabled}, Visible={esVisible}");

                // Validaciones básicas
                if (!estaEnabled || !esVisible)
                {
                    LogDebug("🛡️ Click bloqueado - Botón deshabilitado o invisible");
                    return true;
                }

                // Validación de interacción real
                bool tieneInteraccionReal = tieneMouseOver || tieneFoco;

                if (verificarFocoEstricto)
                {
                    // Modo estricto - requiere ambos
                    tieneInteraccionReal = tieneMouseOver && tieneFoco;
                }

                if (!tieneInteraccionReal)
                {
                    _clicksAutomaticosDetectados++;
                    LogDebug($"🛡️ Click automático detectado [{_clicksAutomaticosDetectados}] - Sin interacción real");
                    return true;
                }

                LogDebug("✅ Click validado como manual");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error detectando click automático: {ex.Message}");
                return true; // En caso de error, considerar automático por seguridad
            }
        }

        /// <summary>
        /// Fuerza la limpieza de protección (para casos especiales)
        /// </summary>
        public void LimpiarProteccion(string motivo = "Manual")
        {
            if (_disposed) return;

            try
            {
                lock (_lock)
                {
                    _escaneando = false;
                    _timerProteccion?.Stop();

                    // Limpiar timers de reactivación
                    foreach (var timer in _timersReactivacion.Values.ToList())
                    {
                        timer?.Stop();
                    }
                    _timersReactivacion.Clear();

                    LogDebug($"🧹 Protección limpiada - Motivo: {motivo}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error limpiando protección: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene estadísticas del servicio
        /// </summary>
        public string ObtenerEstadisticas()
        {
            if (_disposed) return "Servicio disposed";

            lock (_lock)
            {
                var tiempoUltimoEscaneo = _ultimoEscaneo == DateTime.MinValue ?
                    "Nunca" :
                    $"{(DateTime.Now - _ultimoEscaneo).TotalSeconds:F1}s atrás";

                return $"📊 ESTADÍSTICAS DEL SCANNER PROTECTION\n" +
                       $"🔍 Escaneos totales: {_escaneosTotales}\n" +
                       $"🛡️ Acciones bloqueadas: {_accionesBloqueadas}\n" +
                       $"🖱️ Clicks automáticos: {_clicksAutomaticosDetectados}\n" +
                       $"⏱️ Último escaneo: {tiempoUltimoEscaneo}\n" +
                       $"🚨 Estado actual: {(_escaneando ? "PROTEGIENDO" : "INACTIVO")}\n" +
                       $"⚙️ Tiempo protección: {_config.TiempoProteccionMs}ms";
            }
        }
        #endregion

        #region Métodos Privados
        private void BloquearBotonesCriticos()
        {
            try
            {
                foreach (var nombreBoton in _config.BotonesCriticos)
                {
                    BloquearBotonIndividual(nombreBoton);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error bloqueando botones críticos: {ex.Message}");
            }
        }

        private void BloquearBotonIndividual(string nombreBoton)
        {
            try
            {
                var boton = _ventanaPrincipal.FindName(nombreBoton) as Button;
                if (boton == null)
                {
                    LogDebug($"⚠️ Botón {nombreBoton} no encontrado");
                    return;
                }

                // Deshabilitar botón
                boton.IsEnabled = false;
                LogDebug($"🔒 Botón {nombreBoton} bloqueado");

                // Limpiar timer anterior si existe
                if (_timersReactivacion.ContainsKey(nombreBoton))
                {
                    _timersReactivacion[nombreBoton]?.Stop();
                    _timersReactivacion.Remove(nombreBoton);
                }

                // Crear timer de reactivación
                var timerReactivar = new DispatcherTimer();
                timerReactivar.Interval = TimeSpan.FromMilliseconds(_config.TiempoBloqueoBotonMs);
                timerReactivar.Tick += (s, e) =>
                {
                    try
                    {
                        boton.IsEnabled = true;
                        timerReactivar.Stop();
                        _timersReactivacion.Remove(nombreBoton);
                        LogDebug($"🔓 Botón {nombreBoton} reactivado");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error reactivando botón {nombreBoton}: {ex.Message}");
                    }
                };

                _timersReactivacion[nombreBoton] = timerReactivar;
                timerReactivar.Start();
            }
            catch (Exception ex)
            {
                LogError($"Error bloqueando botón {nombreBoton}: {ex.Message}");
            }
        }

        private void MostrarStatusProtegido(string mensaje)
        {
            if (_disposed) return;

            try
            {
                _ventanaPrincipal.Dispatcher.BeginInvoke(() =>
                {
                    var txtStatus = _ventanaPrincipal.FindName("TxtStatusPOS") as TextBlock;
                    if (txtStatus != null)
                    {
                        txtStatus.Text = $"🛡️ {mensaje}";
                    }
                });
            }
            catch (Exception ex)
            {
                LogError($"Error mostrando status: {ex.Message}");
            }
        }

        private void LogDebug(string mensaje)
        {
            if (_config.LogDebugActivo)
            {
                Debug.WriteLine($"[ScannerProtection] {mensaje}");
            }
        }

        private void LogError(string mensaje)
        {
            Debug.WriteLine($"[ScannerProtection] ❌ {mensaje}");
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                lock (_lock)
                {
                    _disposed = true;

                    // Detener timer principal
                    _timerProteccion?.Stop();
                    _timerProteccion = null;

                    // Detener y limpiar timers de reactivación
                    foreach (var timer in _timersReactivacion.Values)
                    {
                        timer?.Stop();
                    }
                    _timersReactivacion.Clear();

                    LogDebug($"🗑️ ScannerProtectionService disposed - Estadísticas finales: {_escaneosTotales} escaneos, {_accionesBloqueadas} bloqueados");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en dispose: {ex.Message}");
            }
        }
        #endregion
    }

    #region Extensiones y Helpers
    /// <summary>
    /// Extensiones para facilitar el uso del servicio
    /// </summary>
    public static class ScannerProtectionExtensions
    {
        /// <summary>
        /// Ejecuta una acción solo si no está bloqueada por el escáner
        /// </summary>
        public static bool EjecutarSiNoEstaBloqueado(this ScannerProtectionService service,
            Action accion, string nombreAccion = "")
        {
            if (service?.DebeBloquearAccion(nombreAccion) == true)
            {
                return false;
            }

            try
            {
                accion?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error ejecutando acción {nombreAccion}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Valida un click de botón y ejecuta la acción si es válido
        /// </summary>
        public static bool ValidarYEjecutarClick(this ScannerProtectionService service,
            Button boton, Action accion, string nombreAccion = "", bool focoEstricto = false)
        {
            if (service == null || boton == null || accion == null) return false;

            // Verificar si debe ser bloqueado
            if (service.DebeBloquearAccion(nombreAccion))
                return false;

            // Verificar si es click automático
            if (service.EsClickAutomatico(boton, focoEstricto))
                return false;

            try
            {
                accion.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error ejecutando click {nombreAccion}: {ex.Message}");
                return false;
            }
        }
    }
    #endregion
}