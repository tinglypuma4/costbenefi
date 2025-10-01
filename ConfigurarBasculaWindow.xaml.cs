using System;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace costbenefi.Views
{
    public partial class ConfigurarBasculaWindow : Window
    {
        private readonly AppDbContext _context;
        private ConfiguracionBascula _configuracionActual;
        private BasculaService _servicioTemporal;

        public bool ConfiguracionGuardada { get; private set; } = false;

        public ConfigurarBasculaWindow(AppDbContext context)
        {
            _context = context;
            InitializeComponent();
            CargarDatosIniciales();
        }

        private async void CargarDatosIniciales()
        {
            try
            {
                TxtStatus.Text = "⏳ Cargando configuración...";

                // ✅ MEJORADO: Cargar puertos COM con diagnóstico
                await CargarPuertosCOM();

                // Cargar velocidades comunes
                CmbBaudRate.ItemsSource = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
                CmbBaudRate.SelectedValue = 9600;

                // ✅ NUEVO: Cargar configuraciones de parámetros serie
                CargarParametrosSerial();

                // ✅ MEJORADO: Cargar configuraciones predefinidas con RHINO
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Genérica Universal", Tag = "generica" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "RHINO BAR-8RS", Tag = "rhino" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "OHAUS", Tag = "ohaus" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Mettler Toledo", Tag = "mettler" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Torrey", Tag = "torrey" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "EXCELL", Tag = "excell" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Toledo", Tag = "toledo" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Personalizada", Tag = "custom" });
                CmbConfiguracion.SelectedIndex = 0;

                // Cargar configuración existente
                await CargarConfiguracionExistenteAsync();

                TxtStatus.Text = "✅ Configuración cargada";
                BtnProbarConexion.IsEnabled = true;
                BtnDiagnosticar.IsEnabled = true;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al cargar: {ex.Message}";
                MessageBox.Show($"Error al cargar configuración: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ NUEVO: Método para cargar puertos COM con diagnóstico
        private async Task CargarPuertosCOM()
        {
            try
            {
                TxtStatus.Text = "🔍 Detectando puertos COM...";

                var puertos = await Task.Run(() => BasculaService.ObtenerPuertosDisponibles());
                CmbPuerto.ItemsSource = puertos;

                if (puertos.Length > 0)
                {
                    CmbPuerto.SelectedIndex = 0;
                    TxtStatus.Text = $"✅ {puertos.Length} puertos COM detectados";
                }
                else
                {
                    TxtStatus.Text = "⚠️ No se detectaron puertos COM - Revise conexiones";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error detectando puertos: {ex.Message}";
                // Cargar puertos por defecto como fallback
                CmbPuerto.ItemsSource = new[] { "COM1", "COM2", "COM3", "COM4", "COM5" };
                CmbPuerto.SelectedIndex = 0;
            }
        }

        // ✅ NUEVO: Cargar parámetros serie
        private void CargarParametrosSerial()
        {
            // Bits de datos
            CmbDataBits.Items.Add(new ComboBoxItem { Content = "7", Tag = 7 });
            CmbDataBits.Items.Add(new ComboBoxItem { Content = "8", Tag = 8 });
            CmbDataBits.SelectedIndex = 1; // 8 bits por defecto

            // Paridad
            CmbParidad.Items.Add(new ComboBoxItem { Content = "Ninguna", Tag = Parity.None });
            CmbParidad.Items.Add(new ComboBoxItem { Content = "Par", Tag = Parity.Even });
            CmbParidad.Items.Add(new ComboBoxItem { Content = "Impar", Tag = Parity.Odd });
            CmbParidad.SelectedIndex = 0; // Ninguna por defecto

            // Bits de parada
            CmbBitsParada.Items.Add(new ComboBoxItem { Content = "1", Tag = StopBits.One });
            CmbBitsParada.Items.Add(new ComboBoxItem { Content = "2", Tag = StopBits.Two });
            CmbBitsParada.SelectedIndex = 0; // 1 bit por defecto

            // Control de flujo
            CmbControlFlujo.Items.Add(new ComboBoxItem { Content = "Ninguno", Tag = Handshake.None });
            CmbControlFlujo.Items.Add(new ComboBoxItem { Content = "Hardware", Tag = Handshake.RequestToSend });
            CmbControlFlujo.Items.Add(new ComboBoxItem { Content = "Software", Tag = Handshake.XOnXOff });
            CmbControlFlujo.SelectedIndex = 0; // Ninguno por defecto
        }

        private async Task CargarConfiguracionExistenteAsync()
        {
            try
            {
                _configuracionActual = await _context.Set<ConfiguracionBascula>()
                    .FirstOrDefaultAsync(c => c.EsConfiguracionActiva);

                if (_configuracionActual != null)
                {
                    // Cargar valores en la interfaz
                    TxtNombre.Text = _configuracionActual.Nombre;
                    CmbPuerto.SelectedValue = _configuracionActual.Puerto;
                    CmbBaudRate.SelectedValue = _configuracionActual.BaudRate;

                    // ✅ NUEVO: Cargar parámetros serie adicionales
                    SeleccionarEnComboBox(CmbDataBits, _configuracionActual.DataBits);
                    SeleccionarEnComboBox(CmbParidad, _configuracionActual.Parity);
                    SeleccionarEnComboBox(CmbBitsParada, _configuracionActual.StopBits);
                    SeleccionarEnComboBox(CmbControlFlujo, _configuracionActual.Handshake);

                    TxtTimeoutLectura.Text = _configuracionActual.TimeoutLectura.ToString();
                    TxtIntervaloLectura.Text = _configuracionActual.IntervaloLectura.ToString();
                    TxtUnidadPeso.Text = _configuracionActual.UnidadPeso;
                    ChkRequiereSolicitud.IsChecked = _configuracionActual.RequiereSolicitudPeso;
                    TxtComandoSolicitar.Text = _configuracionActual.ComandoSolicitarPeso;
                    TxtComandoTara.Text = _configuracionActual.ComandoTara;
                    TxtPatronExtraccion.Text = _configuracionActual.PatronExtraccion;
                    TxtTerminadorLinea.Text = _configuracionActual.TerminadorLinea ?? "\r\n";

                    // Determinar tipo de configuración
                    DeterminarTipoConfiguracion();
                }
                else
                {
                    // Aplicar configuración por defecto
                    AplicarConfiguracionPredefinida("generica");
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al cargar configuración existente: {ex.Message}";
            }
        }

        // ✅ NUEVO: Helper para seleccionar items en ComboBox
        private void SeleccionarEnComboBox(ComboBox comboBox, object valor)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag.Equals(valor))
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void DeterminarTipoConfiguracion()
        {
            if (_configuracionActual == null) return;

            // ✅ NUEVO: Verificar RHINO
            var rhino = ConfiguracionBascula.ConfiguracionRhino();
            if (ConfiguracionesCoinciden(_configuracionActual, rhino))
            {
                CmbConfiguracion.SelectedIndex = 1; // RHINO
                return;
            }

            // Verificar si coincide con OHAUS
            var ohaus = ConfiguracionBascula.ConfiguracionOhaus();
            if (ConfiguracionesCoinciden(_configuracionActual, ohaus))
            {
                CmbConfiguracion.SelectedIndex = 2; // OHAUS
                return;
            }

            // Verificar si coincide con Mettler
            var mettler = ConfiguracionBascula.ConfiguracionMettler();
            if (ConfiguracionesCoinciden(_configuracionActual, mettler))
            {
                CmbConfiguracion.SelectedIndex = 3; // Mettler
                return;
            }

            // ✅ NUEVO: Verificar otras marcas
            var torrey = ConfiguracionBascula.ConfiguracionTorrey();
            if (ConfiguracionesCoinciden(_configuracionActual, torrey))
            {
                CmbConfiguracion.SelectedIndex = 4; // Torrey
                return;
            }

            var excell = ConfiguracionBascula.ConfiguracionExcell();
            if (ConfiguracionesCoinciden(_configuracionActual, excell))
            {
                CmbConfiguracion.SelectedIndex = 5; // EXCELL
                return;
            }

            // Si no coincide con ninguna, es personalizada
            CmbConfiguracion.SelectedIndex = CmbConfiguracion.Items.Count - 1; // Personalizada
        }

        // ✅ NUEVO: Método para comparar configuraciones
        private bool ConfiguracionesCoinciden(ConfiguracionBascula config1, ConfiguracionBascula config2)
        {
            return config1.BaudRate == config2.BaudRate &&
                   config1.DataBits == config2.DataBits &&
                   config1.Parity == config2.Parity &&
                   config1.StopBits == config2.StopBits &&
                   config1.ComandoSolicitarPeso == config2.ComandoSolicitarPeso &&
                   config1.PatronExtraccion == config2.PatronExtraccion;
        }

        private void CmbConfiguracion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbConfiguracion.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                AplicarConfiguracionPredefinida(tag);
            }
        }

        // ✅ MEJORADO: Configuraciones predefinidas actualizadas
        private void AplicarConfiguracionPredefinida(string tipo)
        {
            ConfiguracionBascula config = tipo switch
            {
                "rhino" => ConfiguracionBascula.ConfiguracionRhino(),
                "ohaus" => ConfiguracionBascula.ConfiguracionOhaus(),
                "mettler" => ConfiguracionBascula.ConfiguracionMettler(),
                "torrey" => ConfiguracionBascula.ConfiguracionTorrey(),
                "excell" => ConfiguracionBascula.ConfiguracionExcell(),
                "toledo" => ConfiguracionBascula.ConfiguracionToledo(),
                "generica" => ConfiguracionBascula.ConfiguracionGenerica(),
                _ => new ConfiguracionBascula()
            };

            // Mantener puerto y nombre si ya están configurados
            var puertoActual = CmbPuerto.SelectedValue?.ToString();
            var nombreActual = TxtNombre.Text;

            // Aplicar configuración
            TxtNombre.Text = string.IsNullOrEmpty(nombreActual) ? config.Nombre : nombreActual;
            if (!string.IsNullOrEmpty(puertoActual)) config.Puerto = puertoActual;

            CmbBaudRate.SelectedValue = config.BaudRate;
            SeleccionarEnComboBox(CmbDataBits, config.DataBits);
            SeleccionarEnComboBox(CmbParidad, config.Parity);
            SeleccionarEnComboBox(CmbBitsParada, config.StopBits);
            SeleccionarEnComboBox(CmbControlFlujo, config.Handshake);

            TxtTimeoutLectura.Text = config.TimeoutLectura.ToString();
            TxtIntervaloLectura.Text = config.IntervaloLectura.ToString();
            TxtUnidadPeso.Text = config.UnidadPeso;
            ChkRequiereSolicitud.IsChecked = config.RequiereSolicitudPeso;
            TxtComandoSolicitar.Text = config.ComandoSolicitarPeso;
            TxtComandoTara.Text = config.ComandoTara;
            TxtPatronExtraccion.Text = config.PatronExtraccion;
            TxtTerminadorLinea.Text = config.TerminadorLinea ?? "\r\n";

            // Habilitar/deshabilitar campos según el tipo
            var esPersonalizada = tipo == "custom";
            PanelComandos.IsEnabled = esPersonalizada || ChkRequiereSolicitud.IsChecked == true;
            PanelAvanzado.IsEnabled = esPersonalizada;

            TxtStatus.Text = $"✅ Configuración {config.Nombre} aplicada";
        }

        private void ChkRequiereSolicitud_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelComandos != null)
                PanelComandos.IsEnabled = ChkRequiereSolicitud.IsChecked == true;
        }

        private void ChkRequiereSolicitud_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PanelComandos != null)
                PanelComandos.IsEnabled = false;
        }

        // ✅ MEJORADO: Prueba de conexión con diagnóstico detallado
        private async void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnProbarConexion.IsEnabled = false;
                BtnProbarConexion.Content = "⏳ Probando...";
                TxtStatus.Text = "🔄 Probando conexión con báscula...";

                var config = CrearConfiguracionDesdeInterfaz();
                if (config == null) return;

                // Mostrar información de la configuración
                TxtStatus.Text = $"🔧 Configuración: {config.ObtenerInfoDebug()}";
                await Task.Delay(1000);

                _servicioTemporal = new BasculaService(_context);

                var resultado = await _servicioTemporal.ProbarConexionAsync(config);

                if (resultado.Exitoso)
                {
                    TxtStatus.Text = "✅ ¡Conexión exitosa! Báscula responde correctamente";
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));

                    // Mostrar datos adicionales si están disponibles
                    if (!string.IsNullOrEmpty(resultado.DatosRecibidos))
                    {
                        TxtStatus.Text += $"\n📊 Datos: {resultado.DatosRecibidos}";
                    }

                    if (resultado.PesoDetectado.HasValue)
                    {
                        TxtStatus.Text += $"\n⚖️ Peso: {resultado.PesoDetectado:F3} {config.UnidadPeso}";
                    }

                    BtnGuardar.IsEnabled = true;
                }
                else
                {
                    TxtStatus.Text = $"❌ Error: {resultado.MensajeError}";
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));

                    MessageBox.Show($"No se pudo conectar con la báscula.\n\n" +
                                  $"Error: {resultado.MensajeError}\n\n" +
                                  $"Verifique:\n" +
                                  $"• Puerto COM correcto ({config.Puerto})\n" +
                                  $"• Báscula encendida y conectada\n" +
                                  $"• Velocidad: {config.BaudRate} bps\n" +
                                  $"• Configuración: {config.DataBits}-{config.Parity}-{config.StopBits}",
                                  "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error: {ex.Message}";
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));

                MessageBox.Show($"Error al probar conexión: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnProbarConexion.IsEnabled = true;
                BtnProbarConexion.Content = "🔍 Probar Conexión";

                if (_servicioTemporal != null)
                {
                    await _servicioTemporal.DesconectarAsync();
                    _servicioTemporal.Dispose();
                    _servicioTemporal = null;
                }
            }
        }

        // ✅ NUEVO: Botón de diagnóstico
        private async void BtnDiagnosticar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnDiagnosticar.IsEnabled = false;
                BtnDiagnosticar.Content = "🔍 Diagnosticando...";
                TxtStatus.Text = "🔍 Ejecutando diagnóstico completo...";

                var diagnostico = await Task.Run(() => BasculaService.DiagnosticarSistemaAsync());

                var ventanaDiagnostico = new Window
                {
                    Title = "🔍 Diagnóstico del Sistema",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = diagnostico,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 12,
                            Padding = new Thickness(15),
                            TextWrapping = TextWrapping.Wrap,
                            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
                        }
                    }
                };

                ventanaDiagnostico.ShowDialog();
                TxtStatus.Text = "✅ Diagnóstico completado";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en diagnóstico: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error en diagnóstico";
            }
            finally
            {
                BtnDiagnosticar.IsEnabled = true;
                BtnDiagnosticar.Content = "🔍 Diagnosticar";
            }
        }

        // ✅ MÉTODO CORREGIDO BtnGuardar_Click
        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnGuardar.IsEnabled = false;
                TxtStatus.Text = "💾 Guardando configuración...";

                // ✅ VALIDACIONES PRIMERO
                if (string.IsNullOrEmpty(TxtNombre.Text))
                {
                    MessageBox.Show("Ingrese un nombre para la configuración.", "Campo Requerido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNombre.Focus();
                    return;
                }

                if (CmbPuerto.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione un puerto COM.", "Campo Requerido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ DESACTIVAR TODAS LAS CONFIGURACIONES EXISTENTES
                var configuracionesExistentes = await _context.ConfiguracionesBascula.ToListAsync();
                foreach (var conf in configuracionesExistentes)
                {
                    conf.EsConfiguracionActiva = false;
                }

                // ✅ DECIDIR: ACTUALIZAR EXISTENTE O CREAR NUEVA
                if (_configuracionActual != null && _configuracionActual.Id > 0)
                {
                    // ✅ ACTUALIZAR CONFIGURACIÓN EXISTENTE (MÉTODO CORRECTO)
                    _configuracionActual.Nombre = TxtNombre.Text.Trim();
                    _configuracionActual.Puerto = CmbPuerto.SelectedValue.ToString();
                    _configuracionActual.BaudRate = (int)CmbBaudRate.SelectedValue;
                    _configuracionActual.DataBits = (int)((ComboBoxItem)CmbDataBits.SelectedItem).Tag;
                    _configuracionActual.Parity = (Parity)((ComboBoxItem)CmbParidad.SelectedItem).Tag;
                    _configuracionActual.StopBits = (StopBits)((ComboBoxItem)CmbBitsParada.SelectedItem).Tag;
                    _configuracionActual.Handshake = (Handshake)((ComboBoxItem)CmbControlFlujo.SelectedItem).Tag;
                    _configuracionActual.TimeoutLectura = int.Parse(TxtTimeoutLectura.Text);
                    _configuracionActual.IntervaloLectura = int.Parse(TxtIntervaloLectura.Text);
                    _configuracionActual.UnidadPeso = TxtUnidadPeso.Text.Trim();
                    _configuracionActual.RequiereSolicitudPeso = ChkRequiereSolicitud.IsChecked == true;
                    _configuracionActual.ComandoSolicitarPeso = TxtComandoSolicitar.Text.Trim();
                    _configuracionActual.ComandoTara = TxtComandoTara.Text.Trim();
                    _configuracionActual.PatronExtraccion = TxtPatronExtraccion.Text.Trim();
                    _configuracionActual.TerminadorLinea = TxtTerminadorLinea.Text.Trim();
                    _configuracionActual.EsConfiguracionActiva = true;
                    _configuracionActual.FechaActualizacion = DateTime.Now;

                    // Entity Framework detectará los cambios automáticamente
                }
                else
                {
                    // ✅ CREAR NUEVA CONFIGURACIÓN (MÉTODO CORRECTO)
                    var nuevaConfig = new ConfiguracionBascula
                    {
                        Nombre = TxtNombre.Text.Trim(),
                        Puerto = CmbPuerto.SelectedValue.ToString(),
                        BaudRate = (int)CmbBaudRate.SelectedValue,
                        DataBits = (int)((ComboBoxItem)CmbDataBits.SelectedItem).Tag,
                        Parity = (Parity)((ComboBoxItem)CmbParidad.SelectedItem).Tag,
                        StopBits = (StopBits)((ComboBoxItem)CmbBitsParada.SelectedItem).Tag,
                        Handshake = (Handshake)((ComboBoxItem)CmbControlFlujo.SelectedItem).Tag,
                        TimeoutLectura = int.Parse(TxtTimeoutLectura.Text),
                        IntervaloLectura = int.Parse(TxtIntervaloLectura.Text),
                        UnidadPeso = TxtUnidadPeso.Text.Trim(),
                        RequiereSolicitudPeso = ChkRequiereSolicitud.IsChecked == true,
                        ComandoSolicitarPeso = TxtComandoSolicitar.Text.Trim(),
                        ComandoTara = TxtComandoTara.Text.Trim(),
                        PatronExtraccion = TxtPatronExtraccion.Text.Trim(),
                        TerminadorLinea = TxtTerminadorLinea.Text.Trim(),
                        EsConfiguracionActiva = true,
                        FechaCreacion = DateTime.Now,
                        FechaActualizacion = DateTime.Now,
                        UsuarioCreacion = "Usuario" // O el usuario actual
                    };

                    // Agregar al contexto
                    _context.ConfiguracionesBascula.Add(nuevaConfig);
                    _configuracionActual = nuevaConfig;
                }

                // ✅ GUARDAR CAMBIOS
                await _context.SaveChangesAsync();

                ConfiguracionGuardada = true;
                TxtStatus.Text = "✅ Configuración guardada exitosamente";

                MessageBox.Show($"✅ Configuración de báscula guardada correctamente!\n\n" +
                              $"Configuración: {_configuracionActual.Nombre}\n" +
                              $"Puerto: {_configuracionActual.Puerto}\n" +
                              $"Velocidad: {_configuracionActual.BaudRate} bps\n" +
                              $"Formato: {_configuracionActual.DataBits}-{_configuracionActual.Parity}-{_configuracionActual.StopBits}\n\n" +
                              $"La nueva configuración se aplicará automáticamente.",
                              "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al guardar: {ex.Message}";
                MessageBox.Show($"Error al guardar configuración: {ex.Message}\n\nDetalles: {ex.InnerException?.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Debug info
                System.Diagnostics.Debug.WriteLine($"Error completo: {ex}");
            }
            finally
            {
                BtnGuardar.IsEnabled = true;
            }
        }

        // ✅ MEJORADO: Crear configuración con parámetros serie completos
        private ConfiguracionBascula CrearConfiguracionDesdeInterfaz()
        {
            try
            {
                // Validaciones
                if (string.IsNullOrEmpty(TxtNombre.Text))
                {
                    MessageBox.Show("Ingrese un nombre para la configuración.", "Campo Requerido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNombre.Focus();
                    return null;
                }

                if (CmbPuerto.SelectedValue == null)
                {
                    MessageBox.Show("Seleccione un puerto COM.", "Campo Requerido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                var config = new ConfiguracionBascula
                {
                    Nombre = TxtNombre.Text.Trim(),
                    Puerto = CmbPuerto.SelectedValue.ToString(),
                    BaudRate = (int)CmbBaudRate.SelectedValue,

                    // ✅ NUEVO: Parámetros serie completos
                    DataBits = (int)((ComboBoxItem)CmbDataBits.SelectedItem).Tag,
                    Parity = (Parity)((ComboBoxItem)CmbParidad.SelectedItem).Tag,
                    StopBits = (StopBits)((ComboBoxItem)CmbBitsParada.SelectedItem).Tag,
                    Handshake = (Handshake)((ComboBoxItem)CmbControlFlujo.SelectedItem).Tag,

                    TimeoutLectura = int.Parse(TxtTimeoutLectura.Text),
                    IntervaloLectura = int.Parse(TxtIntervaloLectura.Text),
                    UnidadPeso = TxtUnidadPeso.Text.Trim(),
                    RequiereSolicitudPeso = ChkRequiereSolicitud.IsChecked == true,
                    ComandoSolicitarPeso = TxtComandoSolicitar.Text.Trim(),
                    ComandoTara = TxtComandoTara.Text.Trim(),
                    PatronExtraccion = TxtPatronExtraccion.Text.Trim(),
                    TerminadorLinea = TxtTerminadorLinea.Text.Trim()
                };

                // Validar patrón regex
                try
                {
                    var regex = new Regex(config.PatronExtraccion);
                }
                catch
                {
                    MessageBox.Show("El patrón de extracción no es un regex válido.", "Error de Validación",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPatronExtraccion.Focus();
                    return null;
                }

                return config;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en configuración: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ✅ MEJORADO: Refrescar puertos con diagnóstico
        private async void BtnRefrescarPuertos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnRefrescarPuertos.IsEnabled = false;
                TxtStatus.Text = "🔄 Actualizando puertos COM...";

                var puertoSeleccionado = CmbPuerto.SelectedValue;

                await Task.Delay(500); // Pausa para mostrar el estado
                var puertos = await Task.Run(() => BasculaService.ObtenerPuertosDisponibles());

                CmbPuerto.ItemsSource = puertos;

                if (puertos.Contains(puertoSeleccionado))
                {
                    CmbPuerto.SelectedValue = puertoSeleccionado;
                }
                else if (puertos.Length > 0)
                {
                    CmbPuerto.SelectedIndex = 0;
                }

                TxtStatus.Text = $"✅ Actualizado: {puertos.Length} puertos disponibles";

                if (puertos.Length == 0)
                {
                    TxtStatus.Text += " - ⚠️ No se detectaron puertos COM";
                    MessageBox.Show("No se detectaron puertos COM.\n\n" +
                                  "Posibles causas:\n" +
                                  "• Báscula no conectada\n" +
                                  "• Drivers USB-Serie faltantes\n" +
                                  "• Puerto en uso por otra aplicación\n\n" +
                                  "Use el botón 'Diagnosticar' para más información.",
                                  "Sin Puertos COM", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al actualizar puertos: {ex.Message}";
                MessageBox.Show($"Error al actualizar puertos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnRefrescarPuertos.IsEnabled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_servicioTemporal != null)
            {
                _servicioTemporal.DesconectarAsync().Wait();
                _servicioTemporal.Dispose();
            }
            base.OnClosed(e);
        }

        #region UI Creation - ✅ MEJORADO

        private void InitializeComponent()
        {
            Title = "⚖️ Configuración de Báscula Digital";
            Width = 750;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(20)
            };

            var mainStack = new StackPanel();

            // Header
            var header = new TextBlock
            {
                Text = "⚖️ Configuración Universal de Báscula Digital",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 59, 78))
            };
            mainStack.Children.Add(header);

            // Configuración básica
            mainStack.Children.Add(CreateSection("🔧 Configuración Básica", CreateBasicConfigPanel()));

            // Configuración de comunicación serie
            mainStack.Children.Add(CreateSection("📡 Comunicación Serie", CreateSerialConfigPanel()));

            // Configuración de protocolo
            mainStack.Children.Add(CreateSection("📋 Protocolo de Comunicación", CreateProtocolPanel()));

            // Configuración avanzada
            mainStack.Children.Add(CreateSection("⚙️ Configuración Avanzada", CreateAdvancedPanel()));

            // Botones
            mainStack.Children.Add(CreateButtonPanel());

            // Status
            TxtStatus = new TextBlock
            {
                Text = "💡 Configure los parámetros de conexión con su báscula digital",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            mainStack.Children.Add(TxtStatus);

            scrollViewer.Content = mainStack;
            Content = scrollViewer;
        }

        // Campos para controles
        private TextBox TxtNombre;
        private ComboBox CmbPuerto;
        private ComboBox CmbBaudRate;
        private ComboBox CmbConfiguracion;
        private ComboBox CmbDataBits; // ✅ NUEVO
        private ComboBox CmbParidad; // ✅ NUEVO
        private ComboBox CmbBitsParada; // ✅ NUEVO
        private ComboBox CmbControlFlujo; // ✅ NUEVO
        private TextBox TxtTimeoutLectura;
        private TextBox TxtIntervaloLectura;
        private TextBox TxtUnidadPeso;
        private CheckBox ChkRequiereSolicitud;
        private TextBox TxtComandoSolicitar;
        private TextBox TxtComandoTara;
        private TextBox TxtPatronExtraccion;
        private TextBox TxtTerminadorLinea; // ✅ NUEVO
        private StackPanel PanelComandos;
        private StackPanel PanelAvanzado; // ✅ NUEVO
        private Button BtnProbarConexion;
        private Button BtnGuardar;
        private Button BtnDiagnosticar; // ✅ NUEVO
        private Button BtnRefrescarPuertos;
        private TextBlock TxtStatus;

        // ✅ NUEVO: Panel de configuración serie
        private Panel CreateSerialConfigPanel()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            // Primera fila: Bits de datos y Paridad
            var dataBitsPanel = CreateComboPanel("Bits de Datos:", out CmbDataBits);
            Grid.SetRow(dataBitsPanel, 0);
            Grid.SetColumn(dataBitsPanel, 0);
            grid.Children.Add(dataBitsPanel);

            var paridadPanel = CreateComboPanel("Paridad:", out CmbParidad);
            Grid.SetRow(paridadPanel, 0);
            Grid.SetColumn(paridadPanel, 1);
            grid.Children.Add(paridadPanel);

            // Segunda fila: Bits de parada y Control de flujo
            var bitsParadaPanel = CreateComboPanel("Bits de Parada:", out CmbBitsParada);
            Grid.SetRow(bitsParadaPanel, 1);
            Grid.SetColumn(bitsParadaPanel, 0);
            grid.Children.Add(bitsParadaPanel);

            var controlFlujoPanel = CreateComboPanel("Control de Flujo:", out CmbControlFlujo);
            Grid.SetRow(controlFlujoPanel, 1);
            Grid.SetColumn(controlFlujoPanel, 1);
            grid.Children.Add(controlFlujoPanel);

            return grid;
        }

        // ✅ RENOMBRADO: Panel de protocolo (antes comunicación)
        private Panel CreateProtocolPanel()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            // Timeout lectura
            var timeoutPanel = CreateFieldPanel("Timeout lectura (ms):", out TxtTimeoutLectura);
            TxtTimeoutLectura.Text = "2000";
            Grid.SetRow(timeoutPanel, 0);
            Grid.SetColumn(timeoutPanel, 0);
            grid.Children.Add(timeoutPanel);

            // Intervalo lectura
            var intervaloPanel = CreateFieldPanel("Intervalo lectura (ms):", out TxtIntervaloLectura);
            TxtIntervaloLectura.Text = "1000";
            Grid.SetRow(intervaloPanel, 0);
            Grid.SetColumn(intervaloPanel, 1);
            grid.Children.Add(intervaloPanel);

            // Unidad de peso
            var unidadPanel = CreateFieldPanel("Unidad de peso:", out TxtUnidadPeso);
            TxtUnidadPeso.Text = "kg";
            Grid.SetRow(unidadPanel, 1);
            Grid.SetColumn(unidadPanel, 0);
            grid.Children.Add(unidadPanel);

            // ✅ NUEVO: Terminador de línea
            var terminadorPanel = CreateFieldPanel("Terminador de línea:", out TxtTerminadorLinea);
            TxtTerminadorLinea.Text = "\\r\\n";
            TxtTerminadorLinea.ToolTip = "Ejemplo: \\r\\n, \\n, \\r";
            Grid.SetRow(terminadorPanel, 1);
            Grid.SetColumn(terminadorPanel, 1);
            grid.Children.Add(terminadorPanel);

            // Requiere solicitud
            ChkRequiereSolicitud = new CheckBox
            {
                Content = "Requiere comando para leer peso",
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = true
            };
            ChkRequiereSolicitud.Checked += ChkRequiereSolicitud_Checked;
            ChkRequiereSolicitud.Unchecked += ChkRequiereSolicitud_Unchecked;
            Grid.SetRow(ChkRequiereSolicitud, 2);
            Grid.SetColumnSpan(ChkRequiereSolicitud, 2);
            grid.Children.Add(ChkRequiereSolicitud);

            return grid;
        }

        // Panel básico actualizado
        private Panel CreateBasicConfigPanel()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            // Nombre
            var nombrePanel = CreateFieldPanel("Nombre:", out TxtNombre);
            TxtNombre.Text = "Mi Báscula Digital";
            Grid.SetRow(nombrePanel, 0);
            Grid.SetColumn(nombrePanel, 0);
            grid.Children.Add(nombrePanel);

            // Puerto COM con botón refrescar
            var puertoStack = new StackPanel { Margin = new Thickness(5) };
            puertoStack.Children.Add(new TextBlock { Text = "Puerto COM:", Margin = new Thickness(0, 0, 0, 5), FontSize = 12 });

            var puertoPanel = new StackPanel { Orientation = Orientation.Horizontal };
            CmbPuerto = new ComboBox { Width = 100, Margin = new Thickness(0, 0, 5, 0), FontSize = 12 };

            BtnRefrescarPuertos = new Button
            {
                Content = "🔄",
                Width = 30,
                Height = 25,
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "Refrescar puertos COM"
            };
            BtnRefrescarPuertos.Click += BtnRefrescarPuertos_Click;

            puertoPanel.Children.Add(CmbPuerto);
            puertoPanel.Children.Add(BtnRefrescarPuertos);
            puertoStack.Children.Add(puertoPanel);

            Grid.SetRow(puertoStack, 0);
            Grid.SetColumn(puertoStack, 1);
            grid.Children.Add(puertoStack);

            // Configuración predefinida
            var configPanel = CreateComboPanel("Configuración:", out CmbConfiguracion);
            CmbConfiguracion.SelectionChanged += CmbConfiguracion_SelectionChanged;
            Grid.SetRow(configPanel, 1);
            Grid.SetColumn(configPanel, 0);
            grid.Children.Add(configPanel);

            // Velocidad
            var baudPanel = CreateComboPanel("Velocidad (baud):", out CmbBaudRate);
            Grid.SetRow(baudPanel, 1);
            Grid.SetColumn(baudPanel, 1);
            grid.Children.Add(baudPanel);

            return grid;
        }

        // ✅ MEJORADO: Panel avanzado
        private Panel CreateAdvancedPanel()
        {
            PanelAvanzado = new StackPanel();

            // Panel comandos
            PanelComandos = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                IsEnabled = true
            };

            var solicitarPanel = CreateFieldPanel("Comando solicitar:", out TxtComandoSolicitar);
            solicitarPanel.Width = 150;
            TxtComandoSolicitar.Text = "P";
            TxtComandoSolicitar.ToolTip = "Comando ASCII para solicitar peso (ej: P, W)";
            PanelComandos.Children.Add(solicitarPanel);

            var tararPanel = CreateFieldPanel("Comando tarar:", out TxtComandoTara);
            tararPanel.Width = 150;
            TxtComandoTara.Text = "T";
            TxtComandoTara.ToolTip = "Comando para tarar la báscula";
            PanelComandos.Children.Add(tararPanel);

            PanelAvanzado.Children.Add(PanelComandos);

            // Patrón de extracción
            var patronPanel = CreateFieldPanel("Patrón de extracción (regex):", out TxtPatronExtraccion);
            TxtPatronExtraccion.Text = @"(\d+\.?\d*)";
            TxtPatronExtraccion.ToolTip = "Expresión regular para extraer el peso de la respuesta";
            PanelAvanzado.Children.Add(patronPanel);

            var ayuda = new TextBlock
            {
                Text = "💡 Patrones comunes:\n" +
                       "• (\\d+\\.?\\d*) - Para números simples: 123.45\n" +
                       "• ST,GS,\\+?\\s*(\\d+\\.?\\d*) - Para protocolo Toledo: ST,GS,+0012.34\n" +
                       "• Weight:\\s*(\\d+\\.?\\d*) - Para texto: Weight: 12.34\n" +
                       "• [+-]?\\d+\\.?\\d* - Para números con signo: +12.34",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 5, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                Padding = new Thickness(10)
            };
            PanelAvanzado.Children.Add(ayuda);

            return PanelAvanzado;
        }

        // ✅ MEJORADO: Panel de botones
        private Panel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            BtnProbarConexion = CreateButton("🔍 Probar Conexión", Color.FromRgb(59, 130, 246));
            BtnProbarConexion.Click += BtnProbarConexion_Click;
            BtnProbarConexion.IsEnabled = false;

            // ✅ NUEVO: Botón de diagnóstico
            BtnDiagnosticar = CreateButton("🔍 Diagnosticar", Color.FromRgb(139, 92, 246));
            BtnDiagnosticar.Click += BtnDiagnosticar_Click;
            BtnDiagnosticar.IsEnabled = false;

            BtnGuardar = CreateButton("💾 Guardar", Color.FromRgb(34, 197, 94));
            BtnGuardar.Click += BtnGuardar_Click;
            BtnGuardar.IsEnabled = false;

            var btnCancelar = CreateButton("❌ Cancelar", Color.FromRgb(108, 117, 125));
            btnCancelar.Click += BtnCancelar_Click;

            panel.Children.Add(BtnProbarConexion);
            panel.Children.Add(BtnDiagnosticar);
            panel.Children.Add(BtnGuardar);
            panel.Children.Add(btnCancelar);

            return panel;
        }

        private Border CreateSection(string titulo, Panel content)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(20)
            };

            var stack = new StackPanel();

            var tituloText = new TextBlock
            {
                Text = titulo,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };
            stack.Children.Add(tituloText);
            stack.Children.Add(content);

            border.Child = stack;
            return border;
        }

        private StackPanel CreateFieldPanel(string label, out TextBox textBox)
        {
            var stack = new StackPanel { Margin = new Thickness(5) };
            stack.Children.Add(new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 12
            });

            textBox = new TextBox
            {
                Padding = new Thickness(8),
                FontSize = 12
            };
            stack.Children.Add(textBox);

            return stack;
        }

        private StackPanel CreateComboPanel(string label, out ComboBox comboBox)
        {
            var stack = new StackPanel { Margin = new Thickness(5) };
            stack.Children.Add(new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 12
            });

            comboBox = new ComboBox
            {
                Padding = new Thickness(8),
                FontSize = 12
            };
            stack.Children.Add(comboBox);

            return stack;
        }

        private Button CreateButton(string content, Color backgroundColor)
        {
            return new Button
            {
                Content = content,
                Width = 150,
                Height = 40,
                Margin = new Thickness(10, 0, 10, 0),
                Background = new SolidColorBrush(backgroundColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
        }

        #endregion
    }
}