using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Services;
using System.Net.Http;
using System.Diagnostics;

namespace costbenefi.Views
{
    public partial class ConfiguracionRedWindow : Window
    {
        private readonly ConfiguracionSistema _config;
        private bool _configuracionCambiada = false;

        public bool ConfiguracionGuardada { get; private set; } = false;

        public ConfiguracionRedWindow()
        {
            InitializeComponent();
            _config = ConfiguracionSistema.Instance;

            Loaded += ConfiguracionRedWindow_Loaded;
        }

        private async void ConfiguracionRedWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cargar configuración actual
                CargarConfiguracionActual();

                // Detectar IP local
                await DetectarIPLocal();

                // Configurar nombre de terminal por defecto
                if (string.IsNullOrEmpty(TxtNombreTerminal.Text))
                {
                    TxtNombreTerminal.Text = Environment.MachineName;
                }

                // Actualizar URLs
                ActualizarUrls();

                System.Diagnostics.Debug.WriteLine("✅ Ventana de configuración de red cargada");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarConfiguracionActual()
        {
            try
            {
                // Seleccionar radio button según configuración actual
                switch (_config.Tipo)
                {
                    case TipoInstalacion.Servidor:
                        RbServidor.IsChecked = true;
                        break;
                    case TipoInstalacion.Terminal:
                        RbTerminal.IsChecked = true;
                        break;
                    case TipoInstalacion.Standalone:
                    default:
                        RbStandalone.IsChecked = true;
                        break;
                }

                // Cargar datos de configuración
                TxtIPServidor.Text = _config.Tipo == TipoInstalacion.Servidor ? _config.ServidorIP : "0.0.0.0";
                TxtPuertoServidor.Text = _config.ServidorPuerto.ToString();

                TxtIPServidorTerminal.Text = _config.Tipo == TipoInstalacion.Terminal ? _config.ServidorIP : "192.168.1.100";
                TxtPuertoTerminal.Text = _config.ServidorPuerto.ToString();
                TxtNombreTerminal.Text = _config.NombreTerminal;

                ChkSincronizacionAutomatica.IsChecked = _config.SincronizacionActiva;
                SliderIntervalo.Value = _config.IntervaloSincronizacionMinutos * 60; // Convertir a segundos

                // Actualizar estado actual
                ActualizarEstadoActual();

                System.Diagnostics.Debug.WriteLine($"✅ Configuración cargada: {_config.Tipo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando configuración: {ex.Message}");
            }
        }

        private void ActualizarEstadoActual()
        {
            try
            {
                // Mostrar configuración actual
                string configTexto = _config.Tipo switch
                {
                    TipoInstalacion.Servidor => $"🖥️ Servidor (Puerto: {_config.ServidorPuerto})",
                    TipoInstalacion.Terminal => $"💻 Terminal → {_config.ServidorIP}:{_config.ServidorPuerto}",
                    TipoInstalacion.Standalone => "🔒 Standalone (Sin red)",
                    _ => "❓ Sin configurar"
                };

                TxtConfigActual.Text = configTexto;

                // Estado de sincronización
                if (_config.SincronizacionActiva && _config.Tipo != TipoInstalacion.Standalone)
                {
                    TxtEstado.Text = "🟢 Sincronización activa";
                }
                else
                {
                    TxtEstado.Text = "🔴 Sin sincronización";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando estado: {ex.Message}");
            }
        }

        private async Task DetectarIPLocal()
        {
            try
            {
                // Obtener IP local
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var localIP = host.AddressList
                    .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .FirstOrDefault(ip => !IPAddress.IsLoopback(ip));

                if (localIP != null)
                {
                    TxtIPLocal.Text = localIP.ToString();
                    System.Diagnostics.Debug.WriteLine($"🌐 IP local detectada: {localIP}");
                }
                else
                {
                    TxtIPLocal.Text = "No detectada";
                    System.Diagnostics.Debug.WriteLine("⚠️ No se pudo detectar IP local");
                }
            }
            catch (Exception ex)
            {
                TxtIPLocal.Text = "Error al detectar";
                System.Diagnostics.Debug.WriteLine($"❌ Error detectando IP local: {ex.Message}");
            }
        }

        private void RbServidor_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            GbConfigServidor.IsEnabled = true;
            GbConfigTerminal.IsEnabled = false;
            GbConfigSync.IsEnabled = true;

            _configuracionCambiada = true;
            ActualizarUrls();
        }

        private void RbTerminal_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            GbConfigServidor.IsEnabled = false;
            GbConfigTerminal.IsEnabled = true;
            GbConfigSync.IsEnabled = true;

            _configuracionCambiada = true;
            ActualizarUrls();
        }

        private void RbStandalone_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            GbConfigServidor.IsEnabled = false;
            GbConfigTerminal.IsEnabled = false;
            GbConfigSync.IsEnabled = false;

            _configuracionCambiada = true;
        }

        private void SliderIntervalo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || TxtIntervaloValor == null) return;

            int segundos = (int)e.NewValue;
            TxtIntervaloValor.Text = $"{segundos} seg";
            _configuracionCambiada = true;
        }

        private void ActualizarUrls()
        {
            if (!IsLoaded) return;

            try
            {
                if (RbServidor.IsChecked == true)
                {
                    var ip = TxtIPServidor.Text.Trim();
                    var puerto = TxtPuertoServidor.Text.Trim();
                    TxtUrlServidor.Text = $"http://{ip}:{puerto}";
                }

                if (RbTerminal.IsChecked == true)
                {
                    var ip = TxtIPServidorTerminal.Text.Trim();
                    var puerto = TxtPuertoTerminal.Text.Trim();
                    TxtUrlConexion.Text = $"http://{ip}:{puerto}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando URLs: {ex.Message}");
            }
        }

        private async void BtnDetectarIP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnDetectarIP.IsEnabled = false;
                BtnDetectarIP.Content = "🔄 Buscando...";

                // Buscar servidores en la red local
                var servidoresEncontrados = await BuscarServidoresEnRed();

                if (servidoresEncontrados.Any())
                {
                    var primerServidor = servidoresEncontrados.First();
                    TxtIPServidorTerminal.Text = primerServidor;
                    ActualizarUrls();

                    MessageBox.Show($"✅ Servidor encontrado!\n\nIP: {primerServidor}\n\nSe ha configurado automáticamente.",
                                  "Servidor Detectado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("❌ No se encontraron servidores en la red local.\n\nIngrese la IP manualmente.",
                                  "Sin Servidores", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al detectar servidores: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnDetectarIP.IsEnabled = true;
                BtnDetectarIP.Content = "🔍 Detectar";
            }
        }

        private async Task<string[]> BuscarServidoresEnRed()
        {
            try
            {
                var servidores = new System.Collections.Generic.List<string>();
                var ipLocal = TxtIPLocal.Text;

                if (string.IsNullOrEmpty(ipLocal) || ipLocal == "No detectada")
                    return servidores.ToArray();

                // Obtener rango de red (ej: 192.168.1.x)
                var ipParts = ipLocal.Split('.');
                if (ipParts.Length != 4) return servidores.ToArray();

                var baseIP = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.";

                System.Diagnostics.Debug.WriteLine($"🔍 Buscando servidores en rango: {baseIP}x");

                // Probar IPs comunes (1-20, 100-110)
                var ipsAProbar = new System.Collections.Generic.List<int>();
                for (int i = 1; i <= 20; i++) ipsAProbar.Add(i);
                for (int i = 100; i <= 110; i++) ipsAProbar.Add(i);

                var tareas = ipsAProbar.Select(async ip =>
                {
                    var ipAProbar = $"{baseIP}{ip}";
                    if (ipAProbar == ipLocal) return null; // No probar la IP local

                    try
                    {
                        using var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(2);

                        var response = await client.GetAsync($"http://{ipAProbar}:5000/api/sync/ping");
                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Servidor encontrado en: {ipAProbar}");
                            return ipAProbar;
                        }
                    }
                    catch
                    {
                        // Ignorar errores de conexión
                    }
                    return null;
                });

                var resultados = await Task.WhenAll(tareas);
                return resultados.Where(r => r != null).ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error buscando servidores: {ex.Message}");
                return new string[0];
            }
        }

        private async void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnProbarConexion.IsEnabled = false;
                BtnProbarConexion.Content = "🔄 Probando...";

                var ip = TxtIPServidorTerminal.Text.Trim();
                var puerto = TxtPuertoTerminal.Text.Trim();

                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(puerto))
                {
                    MessageBox.Show("❌ Ingrese IP y puerto válidos.", "Datos Requeridos",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var url = $"http://{ip}:{puerto}/api/sync/ping";
                System.Diagnostics.Debug.WriteLine($"🧪 Probando conexión a: {url}");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"✅ Conexión exitosa!\n\nServidor: {ip}:{puerto}\nRespuesta: {content}",
                                  "Conexión OK", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtUltimaConexion.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                }
                else
                {
                    MessageBox.Show($"❌ Error de conexión\n\nCódigo: {response.StatusCode}\nDetalles: {content}",
                                  "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al conectar:\n\n{ex.Message}\n\nVerifique:\n• IP y puerto correctos\n• Servidor ejecutándose\n• Firewall configurado",
                              "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnProbarConexion.IsEnabled = true;
                BtnProbarConexion.Content = "🧪 Probar";
            }
        }
        private async void BtnProbarConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnProbarConfiguracion.IsEnabled = false;
                BtnProbarConfiguracion.Content = "🔄 Probando...";

                var resultado = ValidarConfiguracion();
                if (!resultado.esValida)
                {
                    MessageBox.Show($"❌ Configuración inválida:\n\n{resultado.mensaje}",
                                  "Configuración Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Probar según el tipo seleccionado
                if (RbTerminal.IsChecked == true)
                {
                    // ✅ CORRECCIÓN: Llamar al método de lógica, no al event handler
                    await ProbarConexionServidor();
                }
                else if (RbServidor.IsChecked == true)
                {
                    MessageBox.Show($"✅ Configuración de servidor válida!\n\n" +
                                  $"🌐 URL: {TxtUrlServidor.Text}\n" +
                                  $"🔄 Intervalo: {TxtIntervaloValor.Text}",
                                  "Configuración OK", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("✅ Configuración standalone válida!\n\nFuncionará sin sincronización de red.",
                                  "Configuración OK", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al probar configuración: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnProbarConfiguracion.IsEnabled = true;
                BtnProbarConfiguracion.Content = "🧪 Probar Config";
            }
        }

        /// <summary>
        /// ✅ NUEVO: Método separado para la lógica de conexión que SÍ retorna Task
        /// </summary>
        private async Task ProbarConexionServidor()
        {
            try
            {
                var ip = TxtIPServidorTerminal.Text.Trim();
                var puerto = TxtPuertoTerminal.Text.Trim();

                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(puerto))
                {
                    MessageBox.Show("❌ Ingrese IP y puerto válidos.", "Datos Requeridos",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var url = $"http://{ip}:{puerto}/api/sync/ping";
                System.Diagnostics.Debug.WriteLine($"🧪 Probando conexión a: {url}");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"✅ Conexión exitosa!\n\nServidor: {ip}:{puerto}\nRespuesta: {content}",
                                  "Conexión OK", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtUltimaConexion.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                }
                else
                {
                    MessageBox.Show($"❌ Error de conexión\n\nCódigo: {response.StatusCode}\nDetalles: {content}",
                                  "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al conectar:\n\n{ex.Message}\n\nVerifique:\n• IP y puerto correctos\n• Servidor ejecutándose\n• Firewall configurado",
                              "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       


        private (bool esValida, string mensaje) ValidarConfiguracion()
        {
            try
            {
                if (RbServidor.IsChecked == true)
                {
                    if (!int.TryParse(TxtPuertoServidor.Text, out int puerto) || puerto <= 0 || puerto > 65535)
                        return (false, "Puerto del servidor debe ser un número entre 1 y 65535");
                }
                else if (RbTerminal.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(TxtIPServidorTerminal.Text))
                        return (false, "IP del servidor es requerida");

                    if (!int.TryParse(TxtPuertoTerminal.Text, out int puerto) || puerto <= 0 || puerto > 65535)
                        return (false, "Puerto debe ser un número entre 1 y 65535");

                    if (string.IsNullOrWhiteSpace(TxtNombreTerminal.Text))
                        return (false, "Nombre del terminal es requerido");
                }

                return (true, "Configuración válida");
            }
            catch (Exception ex)
            {
                return (false, $"Error al validar: {ex.Message}");
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar configuración
                var resultado = ValidarConfiguracion();
                if (!resultado.esValida)
                {
                    MessageBox.Show($"❌ No se puede guardar:\n\n{resultado.mensaje}",
                                  "Configuración Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Guardar configuración
                GuardarConfiguracion();

                MessageBox.Show("✅ Configuración guardada exitosamente!\n\nLos cambios se aplicarán al reiniciar la aplicación.",
                              "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                ConfiguracionGuardada = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuardarConfiguracion()
        {
            try
            {
                if (RbServidor.IsChecked == true)
                {
                    var ip = TxtIPServidor.Text.Trim();
                    var puerto = int.Parse(TxtPuertoServidor.Text);
                    _config.ConfigurarComoServidor(ip, puerto);
                }
                else if (RbTerminal.IsChecked == true)
                {
                    var ip = TxtIPServidorTerminal.Text.Trim();
                    var puerto = int.Parse(TxtPuertoTerminal.Text);
                    var nombre = TxtNombreTerminal.Text.Trim();
                    _config.ConfigurarComoTerminal(ip, puerto, nombre);
                }
                else
                {
                    _config.Tipo = TipoInstalacion.Standalone;
                    _config.SincronizacionActiva = false;
                }

                // Configurar sincronización
                _config.SincronizacionActiva = ChkSincronizacionAutomatica.IsChecked == true;
                _config.IntervaloSincronizacionMinutos = (int)(SliderIntervalo.Value / 60);

                System.Diagnostics.Debug.WriteLine($"✅ Configuración guardada: {_config.Tipo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error guardando configuración: {ex.Message}");
                throw;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_configuracionCambiada)
            {
                var resultado = MessageBox.Show("¿Descartar los cambios realizados?",
                                              "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultado == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }

        // Event handlers para actualizar URLs en tiempo real
        private void TxtIPServidor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _configuracionCambiada = true;
            ActualizarUrls();
        }

        private void TxtPuertoServidor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _configuracionCambiada = true;
            ActualizarUrls();
        }

        private void TxtIPServidorTerminal_TextChanged(object sender, TextChangedEventArgs e)
        {
            _configuracionCambiada = true;
            ActualizarUrls();
        }

        private void TxtPuertoTerminal_TextChanged(object sender, TextChangedEventArgs e)
        {
            _configuracionCambiada = true;
            ActualizarUrls();
        }
    }
}