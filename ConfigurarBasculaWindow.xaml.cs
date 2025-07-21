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

                // Cargar puertos COM disponibles
                var puertos = BasculaService.ObtenerPuertosDisponibles();
                CmbPuerto.ItemsSource = puertos;

                if (puertos.Length > 0)
                {
                    CmbPuerto.SelectedIndex = 0;
                }

                // Cargar velocidades comunes
                CmbBaudRate.ItemsSource = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
                CmbBaudRate.SelectedValue = 9600;

                // Cargar configuraciones predefinidas
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Genérica", Tag = "generica" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "OHAUS", Tag = "ohaus" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Mettler Toledo", Tag = "mettler" });
                CmbConfiguracion.Items.Add(new ComboBoxItem { Content = "Personalizada", Tag = "custom" });
                CmbConfiguracion.SelectedIndex = 0;

                // Cargar configuración existente
                await CargarConfiguracionExistenteAsync();

                TxtStatus.Text = "✅ Configuración cargada";
                BtnProbarConexion.IsEnabled = true;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al cargar: {ex.Message}";
                MessageBox.Show($"Error al cargar configuración: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    TxtTimeoutLectura.Text = _configuracionActual.TimeoutLectura.ToString();
                    TxtIntervaloLectura.Text = _configuracionActual.IntervaloLectura.ToString();
                    TxtUnidadPeso.Text = _configuracionActual.UnidadPeso;
                    ChkRequiereSolicitud.IsChecked = _configuracionActual.RequiereSolicitudPeso;
                    TxtComandoSolicitar.Text = _configuracionActual.ComandoSolicitarPeso;
                    TxtComandoTara.Text = _configuracionActual.ComandoTara;
                    TxtPatronExtraccion.Text = _configuracionActual.PatronExtraccion;

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

        private void DeterminarTipoConfiguracion()
        {
            if (_configuracionActual == null) return;

            // Verificar si coincide con OHAUS
            var ohaus = ConfiguracionBascula.ConfiguracionOhaus();
            if (_configuracionActual.BaudRate == ohaus.BaudRate &&
                _configuracionActual.ComandoSolicitarPeso == ohaus.ComandoSolicitarPeso &&
                _configuracionActual.PatronExtraccion == ohaus.PatronExtraccion)
            {
                CmbConfiguracion.SelectedIndex = 1; // OHAUS
                return;
            }

            // Verificar si coincide con Mettler
            var mettler = ConfiguracionBascula.ConfiguracionMettler();
            if (_configuracionActual.BaudRate == mettler.BaudRate &&
                _configuracionActual.ComandoSolicitarPeso == mettler.ComandoSolicitarPeso &&
                _configuracionActual.PatronExtraccion == mettler.PatronExtraccion)
            {
                CmbConfiguracion.SelectedIndex = 2; // Mettler
                return;
            }

            // Si no coincide con ninguna, es personalizada
            CmbConfiguracion.SelectedIndex = 3; // Personalizada
        }

        private void CmbConfiguracion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbConfiguracion.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                AplicarConfiguracionPredefinida(tag);
            }
        }

        private void AplicarConfiguracionPredefinida(string tipo)
        {
            ConfiguracionBascula config = tipo switch
            {
                "ohaus" => ConfiguracionBascula.ConfiguracionOhaus(),
                "mettler" => ConfiguracionBascula.ConfiguracionMettler(),
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
            TxtTimeoutLectura.Text = config.TimeoutLectura.ToString();
            TxtIntervaloLectura.Text = config.IntervaloLectura.ToString();
            TxtUnidadPeso.Text = config.UnidadPeso;
            ChkRequiereSolicitud.IsChecked = config.RequiereSolicitudPeso;
            TxtComandoSolicitar.Text = config.ComandoSolicitarPeso;
            TxtComandoTara.Text = config.ComandoTara;
            TxtPatronExtraccion.Text = config.PatronExtraccion;

            // Habilitar/deshabilitar campos según el tipo
            var esPersonalizada = tipo == "custom";
            PanelComandos.IsEnabled = esPersonalizada || ChkRequiereSolicitud.IsChecked == true;
            TxtPatronExtraccion.IsEnabled = esPersonalizada;

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

        private async void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnProbarConexion.IsEnabled = false;
                BtnProbarConexion.Content = "⏳ Probando...";
                TxtStatus.Text = "🔄 Probando conexión con báscula...";

                var config = CrearConfiguracionDesdeInterfaz();
                if (config == null) return;

                _servicioTemporal = new BasculaService(_context);

                var resultado = await _servicioTemporal.ProbarConexionAsync(config);

                if (resultado)
                {
                    TxtStatus.Text = "✅ Conexión exitosa! Báscula responde correctamente";
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));

                    // Intentar leer peso
                    var peso = await _servicioTemporal.LeerPesoAsync();
                    if (peso >= 0)
                    {
                        TxtStatus.Text = $"✅ Conexión exitosa! Peso actual: {peso:F3} {config.UnidadPeso}";
                    }

                    BtnGuardar.IsEnabled = true;
                }
                else
                {
                    TxtStatus.Text = "❌ No se pudo conectar con la báscula";
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));

                    MessageBox.Show("No se pudo establecer conexión con la báscula.\n\n" +
                                  "Verifique:\n" +
                                  "• Puerto COM correcto\n" +
                                  "• Báscula encendida y conectada\n" +
                                  "• Configuración de comunicación",
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

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnGuardar.IsEnabled = false;
                TxtStatus.Text = "💾 Guardando configuración...";

                var config = CrearConfiguracionDesdeInterfaz();
                if (config == null) return;

                // Desactivar configuraciones anteriores
                var configuracionesExistentes = await _context.Set<ConfiguracionBascula>()
                    .ToListAsync();

                foreach (var conf in configuracionesExistentes)
                {
                    conf.EsConfiguracionActiva = false;
                }

                // Agregar o actualizar configuración actual
                if (_configuracionActual != null)
                {
                    _context.Entry(_configuracionActual).CurrentValues.SetValues(config);
                    _configuracionActual.EsConfiguracionActiva = true;
                    _configuracionActual.FechaActualizacion = DateTime.Now;
                }
                else
                {
                    config.EsConfiguracionActiva = true;
                    _context.Set<ConfiguracionBascula>().Add(config);
                }

                await _context.SaveChangesAsync();

                ConfiguracionGuardada = true;
                TxtStatus.Text = "✅ Configuración guardada exitosamente";

                MessageBox.Show("✅ Configuración de báscula guardada correctamente!\n\n" +
                              "La nueva configuración se aplicará automáticamente.",
                              "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al guardar: {ex.Message}";
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                BtnGuardar.IsEnabled = true;
            }
        }

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
                    TimeoutLectura = int.Parse(TxtTimeoutLectura.Text),
                    IntervaloLectura = int.Parse(TxtIntervaloLectura.Text),
                    UnidadPeso = TxtUnidadPeso.Text.Trim(),
                    RequiereSolicitudPeso = ChkRequiereSolicitud.IsChecked == true,
                    ComandoSolicitarPeso = TxtComandoSolicitar.Text.Trim(),
                    ComandoTara = TxtComandoTara.Text.Trim(),
                    PatronExtraccion = TxtPatronExtraccion.Text.Trim()
                };

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

        private void BtnRefrescarPuertos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var puertoSeleccionado = CmbPuerto.SelectedValue;
                var puertos = BasculaService.ObtenerPuertosDisponibles();
                CmbPuerto.ItemsSource = puertos;

                if (puertos.Contains(puertoSeleccionado))
                {
                    CmbPuerto.SelectedValue = puertoSeleccionado;
                }
                else if (puertos.Length > 0)
                {
                    CmbPuerto.SelectedIndex = 0;
                }

                TxtStatus.Text = $"✅ Puertos actualizados: {puertos.Length} disponibles";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al actualizar puertos: {ex.Message}";
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

        private void InitializeComponent()
        {
            Title = "⚖️ Configuración de Báscula Digital";
            Width = 700;
            Height = 650;
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
                Text = "⚖️ Configuración de Báscula Digital",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 59, 78))
            };
            mainStack.Children.Add(header);

            // Configuración básica
            mainStack.Children.Add(CreateSection("🔧 Configuración Básica", CreateBasicConfigPanel()));

            // Configuración de comunicación
            mainStack.Children.Add(CreateSection("📡 Comunicación", CreateCommunicationPanel()));

            // Configuración avanzada
            mainStack.Children.Add(CreateSection("⚙️ Configuración Avanzada", CreateAdvancedPanel()));

            // Botones
            mainStack.Children.Add(CreateButtonPanel());

            // Status
            TxtStatus = new TextBlock
            {
                Text = "💡 Configure los parámetros de conexión con su báscula",
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
        private TextBox TxtTimeoutLectura;
        private TextBox TxtIntervaloLectura;
        private TextBox TxtUnidadPeso;
        private CheckBox ChkRequiereSolicitud;
        private TextBox TxtComandoSolicitar;
        private TextBox TxtComandoTara;
        private TextBox TxtPatronExtraccion;
        private StackPanel PanelComandos;
        private Button BtnProbarConexion;
        private Button BtnGuardar;
        private TextBlock TxtStatus;

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

        private Panel CreateBasicConfigPanel()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            // Nombre
            var nombrePanel = CreateFieldPanel("Nombre:", out TxtNombre);
            TxtNombre.Text = "Mi Báscula";
            Grid.SetRow(nombrePanel, 0);
            Grid.SetColumn(nombrePanel, 0);
            grid.Children.Add(nombrePanel);

            // Puerto COM
            var puertoStack = new StackPanel { Margin = new Thickness(5) };
            puertoStack.Children.Add(new TextBlock { Text = "Puerto COM:", Margin = new Thickness(0, 0, 0, 5) });

            var puertoPanel = new StackPanel { Orientation = Orientation.Horizontal };
            CmbPuerto = new ComboBox { Width = 120, Margin = new Thickness(0, 0, 5, 0) };
            var btnRefresh = new Button
            {
                Content = "🔄",
                Width = 30,
                Height = 25,
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnRefresh.Click += BtnRefrescarPuertos_Click;

            puertoPanel.Children.Add(CmbPuerto);
            puertoPanel.Children.Add(btnRefresh);
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

        private Panel CreateCommunicationPanel()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            // Timeout lectura
            var timeoutPanel = CreateFieldPanel("Timeout lectura (ms):", out TxtTimeoutLectura);
            TxtTimeoutLectura.Text = "1000";
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

            // Requiere solicitud
            ChkRequiereSolicitud = new CheckBox
            {
                Content = "Requiere comando para leer peso",
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            ChkRequiereSolicitud.Checked += ChkRequiereSolicitud_Checked;
            ChkRequiereSolicitud.Unchecked += ChkRequiereSolicitud_Unchecked;
            Grid.SetRow(ChkRequiereSolicitud, 1);
            Grid.SetColumn(ChkRequiereSolicitud, 1);
            grid.Children.Add(ChkRequiereSolicitud);

            // Panel comandos
            PanelComandos = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                IsEnabled = false
            };

            var solicitarPanel = CreateFieldPanel("Comando solicitar:", out TxtComandoSolicitar);
            solicitarPanel.Width = 150;
            TxtComandoSolicitar.Text = "P";
            PanelComandos.Children.Add(solicitarPanel);

            var tararPanel = CreateFieldPanel("Comando tarar:", out TxtComandoTara);
            tararPanel.Width = 150;
            TxtComandoTara.Text = "T";
            PanelComandos.Children.Add(tararPanel);

            Grid.SetRow(PanelComandos, 2);
            Grid.SetColumnSpan(PanelComandos, 2);
            grid.Children.Add(PanelComandos);

            return grid;
        }

        private Panel CreateAdvancedPanel()
        {
            var stack = new StackPanel();

            var patronPanel = CreateFieldPanel("Patrón de extracción (regex):", out TxtPatronExtraccion);
            TxtPatronExtraccion.Text = @"(\d+\.?\d*)";
            stack.Children.Add(patronPanel);

            var ayuda = new TextBlock
            {
                Text = "💡 Patrón regex para extraer el peso de la respuesta de la báscula.\n" +
                       "Ejemplos: (\\d+\\.?\\d*) para números, ST,GS,\\+?\\s*(\\d+\\.?\\d*) para protocolo estándar",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5, 5, 5, 0)
            };
            stack.Children.Add(ayuda);

            return stack;
        }

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

            BtnGuardar = CreateButton("💾 Guardar", Color.FromRgb(34, 197, 94));
            BtnGuardar.Click += BtnGuardar_Click;
            BtnGuardar.IsEnabled = false;

            var btnCancelar = CreateButton("❌ Cancelar", Color.FromRgb(108, 117, 125));
            btnCancelar.Click += BtnCancelar_Click;

            panel.Children.Add(BtnProbarConexion);
            panel.Children.Add(BtnGuardar);
            panel.Children.Add(btnCancelar);

            return panel;
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
    }
}