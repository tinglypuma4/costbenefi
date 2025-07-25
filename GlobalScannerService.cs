using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace costbenefi.Services
{
    public enum ScannerContext
    {
        MateriaPrima,
        PuntoVenta,
        Ninguno
    }

    public class CodigoEscaneadoEventArgs : EventArgs
    {
        public string CodigoBarras { get; set; }
        public ScannerContext Contexto { get; set; }
        public DateTime FechaEscaneo { get; set; } = DateTime.Now;
    }

    public class GlobalScannerService : IDisposable
    {
        private readonly List<SerialPortScanner> _scanners = new();
        private readonly Timer _detectionTimer;
        private readonly Dispatcher _dispatcher;
        private ScannerContext _currentContext = ScannerContext.Ninguno;
        private bool _isEnabled = true;

        public event EventHandler<CodigoEscaneadoEventArgs> CodigoEscaneado;
        public event EventHandler<string> ErrorDetectado;
        public event EventHandler<string> ScannerConectado;
        public event EventHandler<string> ScannerDesconectado;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                foreach (var scanner in _scanners)
                {
                    scanner.IsEnabled = value;
                }
            }
        }

        public ScannerContext CurrentContext
        {
            get => _currentContext;
            set => _currentContext = value;
        }

        public int ConnectedScanners => _scanners.Count(s => s.IsConnected);

        public GlobalScannerService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            // Timer para detección automática cada 5 segundos
            _detectionTimer = new Timer(DetectScanners, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private async void DetectScanners(object state)
        {
            if (!_isEnabled) return;

            try
            {
                await Task.Run(() =>
                {
                    var availablePorts = SerialPort.GetPortNames();
                    var currentPorts = _scanners.Select(s => s.PortName).ToList();

                    // Detectar nuevos puertos
                    foreach (var port in availablePorts)
                    {
                        if (!currentPorts.Contains(port))
                        {
                            TryConnectToPort(port);
                        }
                    }

                    // Remover puertos desconectados
                    var disconnectedScanners = _scanners.Where(s => !availablePorts.Contains(s.PortName) || !s.IsConnected).ToList();
                    foreach (var scanner in disconnectedScanners)
                    {
                        RemoveScanner(scanner);
                    }
                });
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorDetectado?.Invoke(this, $"Error en detección: {ex.Message}"));
            }
        }

        private void TryConnectToPort(string portName)
        {
            try
            {
                var scanner = new SerialPortScanner(portName, _dispatcher);
                scanner.CodigoDetectado += OnCodigoDetectado;
                scanner.ErrorOcurrido += OnScannerError;
                scanner.EstadoConexionCambiado += OnScannerConnectionChanged;

                if (scanner.TryConnect())
                {
                    _scanners.Add(scanner);
                    _dispatcher.BeginInvoke(() => ScannerConectado?.Invoke(this, $"Scanner conectado en {portName}"));
                }
                else
                {
                    scanner.Dispose();
                }
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorDetectado?.Invoke(this, $"Error conectando a {portName}: {ex.Message}"));
            }
        }

        private void RemoveScanner(SerialPortScanner scanner)
        {
            try
            {
                _scanners.Remove(scanner);
                _dispatcher.BeginInvoke(() => ScannerDesconectado?.Invoke(this, $"Scanner desconectado de {scanner.PortName}"));
                scanner.Dispose();
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorDetectado?.Invoke(this, $"Error removiendo scanner: {ex.Message}"));
            }
        }

        private void OnCodigoDetectado(object sender, string codigo)
        {
            if (!_isEnabled || string.IsNullOrWhiteSpace(codigo)) return;

            _dispatcher.BeginInvoke(() =>
            {
                CodigoEscaneado?.Invoke(this, new CodigoEscaneadoEventArgs
                {
                    CodigoBarras = codigo.Trim(),
                    Contexto = _currentContext
                });
            });
        }

        private void OnScannerError(object sender, string error)
        {
            _dispatcher.BeginInvoke(() => ErrorDetectado?.Invoke(this, error));
        }

        private void OnScannerConnectionChanged(object sender, bool isConnected)
        {
            if (sender is SerialPortScanner scanner)
            {
                if (!isConnected)
                {
                    RemoveScanner(scanner);
                }
            }
        }

        public void SetContext(ScannerContext context)
        {
            _currentContext = context;
        }

        public string GetStatusInfo()
        {
            if (!_isEnabled) return "Scanner deshabilitado";

            var connectedCount = ConnectedScanners;
            if (connectedCount == 0) return "Sin scanners conectados";

            var contextText = _currentContext switch
            {
                ScannerContext.MateriaPrima => "Materia Prima",
                ScannerContext.PuntoVenta => "Punto de Venta",
                _ => "Inactivo"
            };

            return $"{connectedCount} scanner(s) - Contexto: {contextText}";
        }

        public void Dispose()
        {
            try
            {
                _detectionTimer?.Dispose();

                foreach (var scanner in _scanners.ToList())
                {
                    scanner.Dispose();
                }

                _scanners.Clear();
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorDetectado?.Invoke(this, $"Error en dispose: {ex.Message}"));
            }
        }
    }

    public class SerialPortScanner : IDisposable
    {
        private SerialPort _serialPort;
        private readonly string _portName;
        private readonly Dispatcher _dispatcher;
        private string _barcodeBuffer = "";
        private DateTime _lastDataReceived = DateTime.MinValue;
        private readonly Timer _bufferTimer;
        private const int BUFFER_TIMEOUT_MS = 200;

        public event EventHandler<string> CodigoDetectado;
        public event EventHandler<string> ErrorOcurrido;
        public event EventHandler<bool> EstadoConexionCambiado;

        public string PortName => _portName;
        public bool IsConnected => _serialPort?.IsOpen == true;
        public bool IsEnabled { get; set; } = true;

        public SerialPortScanner(string portName, Dispatcher dispatcher)
        {
            _portName = portName;
            _dispatcher = dispatcher;
            _bufferTimer = new Timer(ProcessBuffer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public bool TryConnect()
        {
            try
            {
                _serialPort = new SerialPort(_portName)
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.ErrorReceived += OnErrorReceived;

                _serialPort.Open();

                // Test básico: enviar comando de prueba
                TestScanner();

                return true;
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorOcurrido?.Invoke(this, $"Error conectando {_portName}: {ex.Message}"));
                return false;
            }
        }

        private void TestScanner()
        {
            try
            {
                // Algunos scanners responden a comandos de configuración
                // Esto es opcional, muchos scanners no necesitan configuración
                if (_serialPort.IsOpen)
                {
                    // Comando básico para verificar conexión (específico del fabricante)
                    // Por ahora solo verificamos que el puerto esté abierto
                }
            }
            catch
            {
                // Ignorar errores de test, el scanner puede funcionar sin comandos especiales
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!IsEnabled) return;

            try
            {
                var data = _serialPort.ReadExisting();
                if (string.IsNullOrEmpty(data)) return;

                _lastDataReceived = DateTime.Now;
                _barcodeBuffer += data;

                // Reiniciar timer para procesar buffer
                _bufferTimer.Change(BUFFER_TIMEOUT_MS, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorOcurrido?.Invoke(this, $"Error leyendo datos: {ex.Message}"));
            }
        }

        private void ProcessBuffer(object state)
        {
            try
            {
                if (string.IsNullOrEmpty(_barcodeBuffer)) return;

                // Limpiar y procesar el código
                var codigo = _barcodeBuffer.Trim('\r', '\n', ' ', '\t');

                if (codigo.Length >= 4) // Mínimo 4 caracteres para un código válido
                {
                    _dispatcher.BeginInvoke(() => CodigoDetectado?.Invoke(this, codigo));
                }

                _barcodeBuffer = "";
            }
            catch (Exception ex)
            {
                _dispatcher.BeginInvoke(() => ErrorOcurrido?.Invoke(this, $"Error procesando buffer: {ex.Message}"));
            }
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            _dispatcher.BeginInvoke(() =>
            {
                ErrorOcurrido?.Invoke(this, $"Error en puerto {_portName}: {e.EventType}");
                EstadoConexionCambiado?.Invoke(this, false);
            });
        }

        public void Dispose()
        {
            try
            {
                _bufferTimer?.Dispose();

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                }
            }
            catch
            {
                // Ignorar errores en dispose
            }
        }
    }
}