using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
namespace costbenefi.Services
{
    /// <summary>
    /// Servicio unificado que maneja TODOS los tipos de escáneres:
    /// - USB (como teclado HID) - Como tu Steren Com-596
    /// - Serie/COM (puerto serie)
    /// - Cualquier otro tipo
    /// </summary>
    public class UnifiedScannerService : IDisposable
    {
        private readonly Window _mainWindow;
        private readonly ScannerProtectionService _protection;

        // Escáneres serie (puerto COM)
        private GlobalScannerService _serialScanner;

        // Escáneres USB (teclado)
        private StringBuilder _keyboardBuffer = new StringBuilder();
        private DateTime _lastKeyTime = DateTime.MinValue;
        private DispatcherTimer _keyboardTimer;
        private const int KEYBOARD_TIMEOUT_MS = 500;

        // Estado
        private ScannerContext _currentContext = ScannerContext.Ninguno;
        private bool _isEnabled = true;
        private bool _disposed = false;

        // Estadísticas
        private int _serialScansDetected = 0;
        private int _keyboardScansDetected = 0;

        public event EventHandler<CodigoEscaneadoEventArgs> CodigoDetectado;
        public event EventHandler<string> EstadoCambiado;

        public UnifiedScannerService(Window mainWindow, ScannerProtectionService protection)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _protection = protection ?? throw new ArgumentNullException(nameof(protection));

            InitializeServices();
            Debug.WriteLine("✅ UnifiedScannerService inicializado - Soporta USB + Serie");
        }

        private void InitializeServices()
        {
            try
            {
                // ✅ 1. INICIALIZAR ESCÁNER SERIE (para escáneres COM)
                InitializeSerialScanner();

                // ✅ 2. INICIALIZAR CAPTURA DE TECLADO (para escáneres USB)
                InitializeKeyboardCapture();

                Debug.WriteLine("🎯 Servicios híbridos inicializados correctamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error inicializando servicios: {ex.Message}");
            }
        }

        private void InitializeSerialScanner()
        {
            try
            {
                _serialScanner = new GlobalScannerService(_mainWindow.Dispatcher);

                _serialScanner.CodigoEscaneado += (sender, e) =>
                {
                    _serialScansDetected++;
                    Debug.WriteLine($"📡 Escáner SERIE detectado: {e.CodigoBarras} (Total serie: {_serialScansDetected})");
                    OnCodigoDetectado(e.CodigoBarras, "Serie/COM");
                };

                _serialScanner.ScannerConectado += (sender, puerto) =>
                {
                    Debug.WriteLine($"✅ Escáner SERIE conectado en {puerto}");
                    EstadoCambiado?.Invoke(this, $"Escáner serie conectado: {puerto}");
                };

                _serialScanner.ErrorDetectado += (sender, error) =>
                {
                    Debug.WriteLine($"⚠️ Error escáner serie: {error}");
                    // No es crítico - continuamos con USB
                };

                Debug.WriteLine("📡 Servicio de escáner SERIE inicializado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error en escáner serie (continuando con USB): {ex.Message}");
            }
        }

        private void InitializeKeyboardCapture()
        {
            try
            {
                // Timer para procesar códigos de teclado
                _keyboardTimer = new DispatcherTimer();
                _keyboardTimer.Interval = TimeSpan.FromMilliseconds(KEYBOARD_TIMEOUT_MS);
                _keyboardTimer.Tick += ProcessKeyboardBuffer;

                // Capturar eventos de teclado de la ventana principal
                _mainWindow.KeyDown += OnWindowKeyDown;

                Debug.WriteLine("⌨️ Servicio de escáner USB/TECLADO inicializado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error inicializando captura de teclado: {ex.Message}");
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // ✅ DECLARAR 'now' UNA SOLA VEZ AL INICIO
                DateTime now = DateTime.Now;

                // ✅ NUEVA LÓGICA: PRIORIDAD PARA TECLA "+"
                if (_currentContext == ScannerContext.PuntoVenta && _isEnabled)
                {
                    // 🎯 DETECTAR TECLA "+" ESPECÍFICAMENTE
                    if (e.Key == Key.Add || e.Key == Key.OemPlus)
                    {
                        bool escaneoActivo = _keyboardBuffer.Length > 0;
                        bool entradaRapidaReciente = _lastKeyTime != DateTime.MinValue &&
                                                   (now - _lastKeyTime).TotalMilliseconds < 300;

                        // ✅ SI NO ESTAMOS ESCANEANDO, PERMITIR QUE PASE LA TECLA "+"
                        if (!escaneoActivo && !entradaRapidaReciente)
                        {
                            Debug.WriteLine("➕ TECLA '+' PERMITIDA - No hay escaneo activo");
                            // NO marcar e.Handled = true, permitir que llegue a MainWindow
                            return;
                        }
                        else
                        {
                            Debug.WriteLine("➕ TECLA '+' BLOQUEADA - Escaneo activo detectado");
                            e.Handled = true;
                            return;
                        }
                    }

                    // ✅ CAPTURAR TODOS LOS ENTER EN CONTEXTO POS INMEDIATAMENTE
                    if (e.Key == Key.Enter || e.Key == Key.Return ||
                        e.Key == Key.LineFeed || e.SystemKey == Key.Enter ||
                        e.Key == Key.System)
                    {
                        Debug.WriteLine($"🛡️ ENTER INTERCEPTADO en contexto POS (Tecla: {e.Key})");
                        e.Handled = true;

                        if (_keyboardBuffer.Length > 0)
                        {
                            Debug.WriteLine($"🎯 Procesando buffer por ENTER: '{_keyboardBuffer}'");
                            ProcessKeyboardBuffer(null, null);
                        }
                        return;
                    }

                    // ✅ FALLBACK: Si llega una tecla no reconocida Y hay buffer, procesarlo
                    if (_keyboardBuffer.Length > 0)
                    {
                        string fallbackChar = ConvertKeyToChar(e.Key, e.KeyboardDevice.Modifiers);
                        if (string.IsNullOrEmpty(fallbackChar))
                        {
                            Debug.WriteLine($"🎯 TECLA NO RECONOCIDA CON BUFFER - Forzando procesamiento: {e.Key}");
                            ProcessKeyboardBuffer(null, null);
                            e.Handled = true;
                            return;
                        }
                    }

                    // ✅ TAMBIÉN PROCESAR SI EL BUFFER ESTÁ MUY LARGO (fallback)
                    if (_keyboardBuffer.Length >= 20)
                    {
                        Debug.WriteLine($"🚨 BUFFER MUY LARGO ({_keyboardBuffer.Length}) - Forzando procesamiento");
                        ProcessKeyboardBuffer(null, null);
                        e.Handled = true;
                        return;
                    }
                }

                // ✅ DEBUG: Mostrar TODAS las teclas que llegan
                Debug.WriteLine($"🔑 TECLA RECIBIDA: {e.Key} - Contexto: {_currentContext} - Enabled: {_isEnabled}");

                // ✅ FALLBACK AUTOMÁTICO: Si el buffer tiene 13+ caracteres, procesarlo inmediatamente
                if (_keyboardBuffer.Length >= 13)
                {
                    Debug.WriteLine($"🚨 FORZANDO PROCESAMIENTO - Buffer largo: '{_keyboardBuffer}' (longitud: {_keyboardBuffer.Length})");
                    ProcessKeyboardBuffer(null, null);
                    e.Handled = true;
                    return;
                }

                // Solo procesar en contexto POS
                if (_currentContext != ScannerContext.PuntoVenta)
                {
                    Debug.WriteLine($"❌ Ignorado - Contexto incorrecto: {_currentContext}");
                    return;
                }

                if (!_isEnabled)
                {
                    Debug.WriteLine($"❌ Ignorado - Servicio deshabilitado");
                    return;
                }

                // Convertir tecla a carácter
                string character = ConvertKeyToChar(e.Key, e.KeyboardDevice.Modifiers);
                if (string.IsNullOrEmpty(character))
                {
                    Debug.WriteLine($"❌ No se pudo convertir tecla: {e.Key}");
                    return;
                }

                // ✅ REUSAR LA VARIABLE 'now' YA DECLARADA
                // DateTime now = DateTime.Now;  ← ELIMINAR ESTA LÍNEA

                // ✅ DETECTAR ENTRADA DE ESCÁNER
                bool isRapidInput = _lastKeyTime != DateTime.MinValue &&
                                   (now - _lastKeyTime).TotalMilliseconds < 300;

                bool isAccumulating = _keyboardBuffer.Length > 0;

                if (isRapidInput || isAccumulating)
                {
                    // Estamos capturando un código de escáner
                    _keyboardBuffer.Append(character);
                    _lastKeyTime = now;

                    // Reiniciar timer
                    _keyboardTimer.Stop();
                    _keyboardTimer.Start();

                    // ✅ ¡CRUCIAL! BLOQUEAR TECLA para que NO active otros controles
                    e.Handled = true;

                    Debug.WriteLine($"⌨️ ESCÁNER CAPTURANDO: '{character}' - Buffer: '{_keyboardBuffer}' (longitud: {_keyboardBuffer.Length}) - TECLA BLOQUEADA");
                }
                else if (char.IsLetterOrDigit(character[0]) || char.IsSymbol(character[0]) || char.IsPunctuation(character[0]))
                {
                    // Primera tecla de posible código de escáner
                    _keyboardBuffer.Clear();
                    _keyboardBuffer.Append(character);
                    _lastKeyTime = now;
                    _keyboardTimer.Start();

                    // ✅ BLOQUEAR DESDE LA PRIMERA TECLA
                    e.Handled = true;

                    Debug.WriteLine($"🎯 INICIANDO captura escáner: '{character}' - TECLA BLOQUEADA");
                }
                else
                {
                    Debug.WriteLine($"❓ Carácter no procesable: '{character}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 ERROR en OnWindowKeyDown: {ex.Message}");
            }
        }
        public void LimpiarBuffer()
        {
            try
            {
                _keyboardBuffer.Clear();
                _lastKeyTime = DateTime.MinValue;
                _keyboardTimer?.Stop();

                Debug.WriteLine($"🧹 Buffer del escáner limpiado - Enabled: {_isEnabled}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error limpiando buffer: {ex.Message}");
            }
        }
        private void ProcessKeyboardBuffer(object sender, EventArgs e)
        {
            try
            {
                _keyboardTimer.Stop();

                // ✅ VERIFICAR SI ESTÁ HABILITADO ANTES DE PROCESAR
                if (!_isEnabled)
                {
                    Debug.WriteLine("🚫 Buffer ignorado - Escáner deshabilitado");
                    _keyboardBuffer.Clear();
                    return;
                }

                Debug.WriteLine($"🕐 TIMER ACTIVADO - Buffer length: {_keyboardBuffer.Length}");

                if (_keyboardBuffer.Length == 0)
                {
                    Debug.WriteLine("❌ Buffer vacío - no hay nada que procesar");
                    return;
                }

                string code = _keyboardBuffer.ToString().Trim();
                _keyboardBuffer.Clear();

                Debug.WriteLine($"🔍 PROCESANDO CÓDIGO COMPLETO: '{code}' (longitud: {code.Length})");

                // ✅ VALIDACIÓN MÁS FLEXIBLE - códigos de 3 a 50 caracteres
                if (code.Length >= 3 && code.Length <= 50)
                {
                    _keyboardScansDetected++;
                    Debug.WriteLine($"✅ CÓDIGO USB VÁLIDO DETECTADO: '{code}' (Total USB: {_keyboardScansDetected})");
                    OnCodigoDetectado(code, "USB/Teclado");
                }
                else
                {
                    Debug.WriteLine($"❌ Código USB INVÁLIDO: '{code}' (longitud: {code.Length}) - Debe ser entre 3-50 caracteres");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 ERROR procesando buffer: {ex.Message}");
            }
        }

        private string ConvertKeyToChar(Key key, ModifierKeys modifiers)
        {
            try
            {
                // Números del teclado principal
                if (key >= Key.D0 && key <= Key.D9)
                {
                    if ((modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                        return ((int)(key - Key.D0)).ToString();
                    else
                    {
                        // Caracteres especiales con Shift en números
                        return key switch
                        {
                            Key.D1 => "!",
                            Key.D2 => "@",
                            Key.D3 => "#",
                            Key.D4 => "$",
                            Key.D5 => "%",
                            Key.D6 => "^",
                            Key.D7 => "&",
                            Key.D8 => "*",
                            Key.D9 => "(",
                            Key.D0 => ")",
                            _ => ""
                        };
                    }
                }

                // Números del teclado numérico
                if (key >= Key.NumPad0 && key <= Key.NumPad9)
                    return ((int)(key - Key.NumPad0)).ToString();

                // Letras - SIMPLIFICADO SIN CAPSLOCK
                if (key >= Key.A && key <= Key.Z)
                {
                    char letter = (char)('A' + (key - Key.A));
                    // ✅ SIMPLIFICADO: Solo considerar Shift (los escáneres no dependen de CapsLock)
                    bool shouldUppercase = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                    return shouldUppercase ? letter.ToString() : letter.ToString().ToLower();
                }

                // ✅ CARACTERES ESPECIALES COMUNES EN CÓDIGOS DE BARRAS
                switch (key)
                {
                    case Key.OemMinus: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "_" : "-";
                    case Key.OemPeriod: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? ">" : ".";
                    case Key.OemComma: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "<" : ",";
                    case Key.OemQuestion: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "?" : "/";
                    case Key.OemSemicolon: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? ":" : ";";
                    case Key.OemQuotes: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "\"" : "'";
                    case Key.OemBackslash: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "|" : "\\";
                    case Key.OemOpenBrackets: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "{" : "[";
                    case Key.OemCloseBrackets: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "}" : "]";
                    case Key.OemPlus: return (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? "+" : "=";
                    case Key.Space: return " ";
                    case Key.Tab: return "\t";
                    default:
                        Debug.WriteLine($"❓ Tecla no reconocida: {key}");
                        return "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error convirtiendo tecla {key}: {ex.Message}");
                return "";
            }
        }
        private void OnCodigoDetectado(string codigo, string tipo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    return;

                // ✅ VERIFICACIÓN ADICIONAL: Solo procesar si está habilitado
                if (!_isEnabled)
                {
                    Debug.WriteLine($"🚫 Código ignorado - Escáner deshabilitado: '{codigo}'");
                    return;
                }

                Debug.WriteLine($"🎯 CÓDIGO DETECTADO [{tipo}]: '{codigo}'");

                // Activar protección inmediatamente
                _protection?.OnProductoEscaneado(codigo, $"Escaneado por {tipo}");

                // Notificar a los suscriptores
                var eventArgs = new CodigoEscaneadoEventArgs
                {
                    CodigoBarras = codigo.Trim(),
                    Contexto = _currentContext,
                    FechaEscaneo = DateTime.Now
                };

                CodigoDetectado?.Invoke(this, eventArgs);
                EstadoCambiado?.Invoke(this, $"Código detectado por {tipo}: {codigo}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error notificando código: {ex.Message}");
            }
        }

        public void SetContext(ScannerContext context)
        {
            _currentContext = context;
            _serialScanner?.SetContext(context);
            Debug.WriteLine($"🔄 Contexto unificado cambiado a: {context}");
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;

            // Si se desactiva, limpiar buffer inmediatamente
            if (!enabled)
            {
                LimpiarBuffer();
                Debug.WriteLine("🧹 Buffer limpiado al desactivar escáner");
            }

            // Propagar a escáner serie
            if (_serialScanner != null)
                _serialScanner.IsEnabled = enabled;

            Debug.WriteLine($"⚙️ Escáner unificado {(enabled ? "HABILITADO" : "DESHABILITADO")}");
        }

        public string GetStatusInfo()
        {
            string statusBase = $"Escáneres detectados - USB: {_keyboardScansDetected}, Serie: {_serialScansDetected} | Contexto: {_currentContext}";

            if (!_isEnabled)
            {
                return $"🚫 DESHABILITADO | {statusBase}";
            }
            else
            {
                return $"✅ ACTIVO | {statusBase}";
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _keyboardTimer?.Stop();
                _keyboardTimer = null;

                if (_mainWindow != null)
                    _mainWindow.KeyDown -= OnWindowKeyDown;

                _serialScanner?.Dispose();

                _disposed = true;
                Debug.WriteLine($"🗑️ UnifiedScannerService disposed - USB: {_keyboardScansDetected}, Serie: {_serialScansDetected}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en dispose: {ex.Message}");
            }
        }
    }
}