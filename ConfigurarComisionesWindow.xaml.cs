using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace costbenefi.Views
{
    public partial class ConfigurarComisionesWindow : Window
    {
        public decimal PorcentajeComisionPredeterminado { get; private set; }
        public bool ComisionActivaPorDefecto { get; private set; }
        public string NombreBanco { get; private set; }
        public string TipoTerminal { get; private set; }
        public bool TerminalCobraIVA { get; private set; }
        public decimal PorcentajeIVA { get; private set; }

        private TextBox TxtPorcentajeComision;
        private CheckBox ChkActivarPorDefecto;
        private CheckBox ChkTerminalCobraIVA;
        private TextBox TxtPorcentajeIVA;
        private TextBox TxtNombreBanco;
        private ComboBox CmbTipoTerminal;
        private TextBlock TxtEjemplosComision;
        private Button BtnGuardar;

        // Evento para notificar cambios en la configuración
        public event EventHandler<ComisionConfig> ConfiguracionActualizada;

        public ConfigurarComisionesWindow()
        {
            InitializeComponent();
            CargarConfiguracionActual();
        }

        private void InitializeComponent()
        {
            Title = "🏦 Configurar Comisiones de Terminal";
            Width = 700;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20, 20, 20, 20);

            for (int i = 0; i < 10; i++)
            {
                if (i == 8)
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                else
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var headerPanel = CreateHeaderPanel();
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            var infoPanel = CreateInfoPanel();
            Grid.SetRow(infoPanel, 1);
            mainGrid.Children.Add(infoPanel);

            var sep1 = new Separator { Margin = new Thickness(0, 20, 0, 20) };
            Grid.SetRow(sep1, 2);
            mainGrid.Children.Add(sep1);

            var configPanel = CreateConfigPanel();
            Grid.SetRow(configPanel, 3);
            mainGrid.Children.Add(configPanel);

            var sep2 = new Separator { Margin = new Thickness(0, 15, 0, 15) };
            Grid.SetRow(sep2, 4);
            mainGrid.Children.Add(sep2);

            var ivaPanel = CreateIVAPanel();
            Grid.SetRow(ivaPanel, 5);
            mainGrid.Children.Add(ivaPanel);

            var ivaPorcentajePanel = CreateIVAPorcentajePanel();
            Grid.SetRow(ivaPorcentajePanel, 6);
            mainGrid.Children.Add(ivaPorcentajePanel);

            var ejemplosPanel = CreateEjemplosPanel();
            Grid.SetRow(ejemplosPanel, 7);
            mainGrid.Children.Add(ejemplosPanel);

            var sep3 = new Separator { Margin = new Thickness(0, 20, 0, 20) };
            Grid.SetRow(sep3, 8);
            mainGrid.Children.Add(sep3);

            var botonesPanel = CreateBotonesPanel();
            Grid.SetRow(botonesPanel, 9);
            mainGrid.Children.Add(botonesPanel);

            Content = mainGrid;
        }

        private UIElement CreateHeaderPanel()
        {
            var headerPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = "🏦",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };

            var title = new TextBlock
            {
                Text = "Configuración de Comisiones de Terminal",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 600
            };

            headerStack.Children.Add(icon);
            headerStack.Children.Add(title);
            headerPanel.Child = headerStack;

            return headerPanel;
        }

        private UIElement CreateInfoPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 15, 15, 15)
            };

            var stack = new StackPanel();

            var titulo = new TextBlock
            {
                Text = "ℹ️ Información Importante",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)),
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 600
            };

            var info = new TextBlock
            {
                Text = "Configure las comisiones que cobra su banco o procesador de pagos por cada transacción con tarjeta, incluyendo el IVA aplicable. " +
                       "Esta configuración se aplicará automáticamente en el procesamiento de pagos.",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)),
                LineHeight = 18,
                MaxWidth = 600
            };

            stack.Children.Add(titulo);
            stack.Children.Add(info);
            panel.Child = stack;

            return panel;
        }

        private UIElement CreateConfigPanel()
        {
            var panel = new StackPanel();

            ChkActivarPorDefecto = new CheckBox
            {
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };
            ChkActivarPorDefecto.Content = new TextBlock
            {
                Text = "Activar cálculo de comisiones por defecto en todas las ventas",
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 550
            };
            ChkActivarPorDefecto.Checked += OnConfigChanged;
            ChkActivarPorDefecto.Unchecked += OnConfigChanged;
            panel.Children.Add(ChkActivarPorDefecto);

            var bancoGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            bancoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            bancoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblBanco = new TextBlock
            {
                Text = "Banco/Procesador:",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 120
            };
            Grid.SetColumn(lblBanco, 0);

            TxtNombreBanco = new TextBox
            {
                Height = 32,
                FontSize = 12,
                Padding = new Thickness(10, 6, 10, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1, 1, 1, 1)
            };
            Grid.SetColumn(TxtNombreBanco, 1);

            bancoGrid.Children.Add(lblBanco);
            bancoGrid.Children.Add(TxtNombreBanco);
            panel.Children.Add(bancoGrid);

            var terminalGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            terminalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            terminalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblTerminal = new TextBlock
            {
                Text = "Tipo de Terminal:",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 120
            };
            Grid.SetColumn(lblTerminal, 0);

            CmbTipoTerminal = new ComboBox
            {
                Height = 32,
                FontSize = 12,
                Padding = new Thickness(10, 6, 10, 6)
            };
            CmbTipoTerminal.Items.Add("Terminal Física (POS)");
            CmbTipoTerminal.Items.Add("Terminal Virtual");
            CmbTipoTerminal.Items.Add("Aplicación Móvil");
            CmbTipoTerminal.Items.Add("Lector de Tarjetas");
            CmbTipoTerminal.Items.Add("Otro");
            CmbTipoTerminal.SelectedIndex = 0;
            Grid.SetColumn(CmbTipoTerminal, 1);

            terminalGrid.Children.Add(lblTerminal);
            terminalGrid.Children.Add(CmbTipoTerminal);
            panel.Children.Add(terminalGrid);

            var comisionGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            comisionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            comisionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            comisionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblComision = new TextBlock
            {
                Text = "% Comisión:",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 120
            };
            Grid.SetColumn(lblComision, 0);

            TxtPorcentajeComision = new TextBox
            {
                Height = 32,
                FontSize = 12,
                Padding = new Thickness(10, 6, 10, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2, 2, 2, 2),
                Text = "3.50"
            };
            TxtPorcentajeComision.TextChanged += OnConfigChanged;
            TxtPorcentajeComision.PreviewTextInput += NumericTextBox_PreviewTextInput;
            TxtPorcentajeComision.LostFocus += FormatPercentageTextBox;
            Grid.SetColumn(TxtPorcentajeComision, 1);

            var lblPorcentaje = new TextBlock
            {
                Text = "% (Ej: 3.50 para 3.50%)",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 200
            };
            Grid.SetColumn(lblPorcentaje, 2);

            comisionGrid.Children.Add(lblComision);
            comisionGrid.Children.Add(TxtPorcentajeComision);
            comisionGrid.Children.Add(lblPorcentaje);
            panel.Children.Add(comisionGrid);

            return panel;
        }

        private UIElement CreateIVAPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(253, 246, 178)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 15, 15, 15)
            };

            var stack = new StackPanel();

            var titulo = new TextBlock
            {
                Text = "🧮 Configuración de IVA sobre Comisión",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 600
            };

            ChkTerminalCobraIVA = new CheckBox
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            ChkTerminalCobraIVA.Content = new TextBlock
            {
                Text = "El terminal cobra IVA adicional sobre la comisión",
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 550
            };
            ChkTerminalCobraIVA.Checked += OnConfigChanged;
            ChkTerminalCobraIVA.Unchecked += OnConfigChanged;
            ChkTerminalCobraIVA.Checked += (s, e) => TxtPorcentajeIVA.IsEnabled = ChkTerminalCobraIVA.IsChecked == true;
            ChkTerminalCobraIVA.Unchecked += (s, e) => TxtPorcentajeIVA.IsEnabled = ChkTerminalCobraIVA.IsChecked == true;
            stack.Children.Add(titulo);
            stack.Children.Add(ChkTerminalCobraIVA);

            var info = new TextBlock
            {
                Text = "Si está activado, se aplicará el porcentaje de IVA ingresado sobre el monto de la comisión.",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14)),
                LineHeight = 16,
                MaxWidth = 600,
                Opacity = 0.8
            };
            stack.Children.Add(info);

            panel.Child = stack;
            return panel;
        }

        private UIElement CreateIVAPorcentajePanel()
        {
            var panel = new Grid { Margin = new Thickness(0, 15, 0, 15) };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblIVA = new TextBlock
            {
                Text = "% IVA:",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 120
            };
            Grid.SetColumn(lblIVA, 0);

            TxtPorcentajeIVA = new TextBox
            {
                Height = 32,
                FontSize = 12,
                Padding = new Thickness(10, 6, 10, 6),
                Text = "16.00",
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2, 2, 2, 2),
                IsEnabled = false
            };
            TxtPorcentajeIVA.TextChanged += OnConfigChanged;
            TxtPorcentajeIVA.PreviewTextInput += NumericTextBox_PreviewTextInput;
            TxtPorcentajeIVA.LostFocus += FormatPercentageTextBox;
            Grid.SetColumn(TxtPorcentajeIVA, 1);

            var lblIVAInfo = new TextBlock
            {
                Text = "% (Ej: 16 para 16%)",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 200
            };
            Grid.SetColumn(lblIVAInfo, 2);

            panel.Children.Add(lblIVA);
            panel.Children.Add(TxtPorcentajeIVA);
            panel.Children.Add(lblIVAInfo);

            return panel;
        }

        private UIElement CreateEjemplosPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 15, 15, 15),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var stack = new StackPanel();

            var titulo = new TextBlock
            {
                Text = "📊 Ejemplos de Cálculo",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 600
            };

            TxtEjemplosComision = new TextBlock
            {
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                LineHeight = 16,
                Text = "Calculando ejemplos...",
                MaxWidth = 600
            };

            stack.Children.Add(titulo);
            stack.Children.Add(TxtEjemplosComision);
            panel.Child = stack;

            return panel;
        }

        private UIElement CreateBotonesPanel()
        {
            var panel = new Grid();
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var btnCancelar = new Button
            {
                Content = "❌ Cancelar",
                Height = 40,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnCancelar.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(btnCancelar, 0);

            BtnGuardar = new Button
            {
                Content = "💾 Guardar Configuración",
                Height = 40,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(10, 0, 0, 0)
            };
            BtnGuardar.Click += BtnGuardar_Click;
            Grid.SetColumn(BtnGuardar, 1);

            panel.Children.Add(btnCancelar);
            panel.Children.Add(BtnGuardar);

            return panel;
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !IsValidDecimal(text);
        }

        private bool IsValidDecimal(string text)
        {
            return decimal.TryParse(text, out decimal result) && result >= 0 && result <= 50;
        }

        private void FormatPercentageTextBox(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (decimal.TryParse(textBox.Text, out decimal value))
            {
                textBox.Text = value.ToString("F2");
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            }
            else
            {
                textBox.BorderBrush = Brushes.Red;
            }
            ActualizarEjemplos();
        }

        private void CargarConfiguracionActual()
        {
            try
            {
                var config = ComisionConfig.Cargar();

                if (TxtPorcentajeComision != null) TxtPorcentajeComision.Text = config.PorcentajeComisionPredeterminado.ToString("F2");
                if (ChkActivarPorDefecto != null) ChkActivarPorDefecto.IsChecked = config.ComisionActivaPorDefecto;
                if (ChkTerminalCobraIVA != null) ChkTerminalCobraIVA.IsChecked = config.TerminalCobraIVA;
                if (TxtPorcentajeIVA != null)
                {
                    TxtPorcentajeIVA.Text = config.PorcentajeIVA.ToString("F2");
                    TxtPorcentajeIVA.IsEnabled = config.TerminalCobraIVA;
                }
                if (TxtNombreBanco != null) TxtNombreBanco.Text = config.NombreBanco ?? "";
                if (CmbTipoTerminal != null && !string.IsNullOrEmpty(config.TipoTerminal))
                {
                    for (int i = 0; i < CmbTipoTerminal.Items.Count; i++)
                    {
                        if (CmbTipoTerminal.Items[i].ToString().Contains(config.TipoTerminal))
                        {
                            CmbTipoTerminal.SelectedIndex = i;
                            break;
                        }
                    }
                }

                ActualizarEjemplos();
            }
            catch (Exception)
            {
                ActualizarEjemplos();
            }
        }

        private void OnConfigChanged(object sender, RoutedEventArgs e)
        {
            ActualizarEjemplos();
        }

        private void OnConfigChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarEjemplos();
        }

        private void ActualizarEjemplos()
        {
            try
            {
                if (TxtEjemplosComision == null || TxtPorcentajeComision == null || TxtPorcentajeIVA == null) return;

                if (!decimal.TryParse(TxtPorcentajeComision.Text, out decimal porcentaje) || porcentaje < 0 || porcentaje > 20)
                {
                    TxtPorcentajeComision.BorderBrush = Brushes.Red;
                    TxtEjemplosComision.Text = "Ingrese un porcentaje de comisión válido (0-20%).";
                    return;
                }
                TxtPorcentajeComision.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));

                if (!decimal.TryParse(TxtPorcentajeIVA.Text, out decimal ivaPorcentaje) || ivaPorcentaje < 0 || ivaPorcentaje > 50)
                {
                    TxtPorcentajeIVA.BorderBrush = Brushes.Red;
                    TxtEjemplosComision.Text = "Ingrese un porcentaje de IVA válido (0-50%).";
                    return;
                }
                TxtPorcentajeIVA.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));

                bool cobraIVA = ChkTerminalCobraIVA?.IsChecked == true;

                var ejemplos = new[]
                {
                    new { Venta = 100m },
                    new { Venta = 500m },
                    new { Venta = 1000m },
                    new { Venta = 2500m }
                };

                string textoEjemplos = $"Con {porcentaje:F2}% de comisión";
                if (cobraIVA) textoEjemplos += $" + {ivaPorcentaje:F2}% IVA";
                textoEjemplos += ":\n\n";

                foreach (var ejemplo in ejemplos)
                {
                    var comisionBase = ejemplo.Venta * (porcentaje / 100);
                    var ivaComision = cobraIVA ? comisionBase * (ivaPorcentaje / 100) : 0;
                    var comisionTotal = comisionBase + ivaComision;
                    var neto = ejemplo.Venta - comisionTotal;

                    textoEjemplos += $"• Venta {ejemplo.Venta:C0} → ";
                    if (cobraIVA)
                    {
                        textoEjemplos += $"Comisión: {comisionBase:C2} + IVA: {ivaComision:C2} = {comisionTotal:C2} → Neto: {neto:C2}\n";
                    }
                    else
                    {
                        textoEjemplos += $"Comisión: {comisionTotal:C2} → Neto: {neto:C2}\n";
                    }
                }

                textoEjemplos += $"\nEn un día con $10,000 en ventas con tarjeta:\n";
                var comisionDia = 10000 * (porcentaje / 100);
                var ivaDia = cobraIVA ? comisionDia * (ivaPorcentaje / 100) : 0;
                var totalDia = comisionDia + ivaDia;

                if (cobraIVA)
                {
                    textoEjemplos += $"Comisión: {comisionDia:C2} + IVA: {ivaDia:C2} = Total: {totalDia:C2}";
                }
                else
                {
                    textoEjemplos += $"Comisión total: {totalDia:C2}";
                }

                TxtEjemplosComision.Text = textoEjemplos;
            }
            catch (Exception)
            {
                TxtEjemplosComision.Text = "Error al calcular ejemplos.";
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(TxtPorcentajeComision.Text, out decimal porcentaje) || porcentaje < 0 || porcentaje > 20)
                {
                    MessageBox.Show("Ingrese un porcentaje de comisión válido entre 0 y 20%.", "Porcentaje Inválido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPorcentajeComision.Focus();
                    return;
                }

                if (!decimal.TryParse(TxtPorcentajeIVA.Text, out decimal ivaPorcentaje) || ivaPorcentaje < 0 || ivaPorcentaje > 50)
                {
                    MessageBox.Show("Ingrese un porcentaje de IVA válido entre 0 y 50%.", "IVA Inválido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPorcentajeIVA.Focus();
                    return;
                }

                PorcentajeComisionPredeterminado = porcentaje;
                ComisionActivaPorDefecto = ChkActivarPorDefecto.IsChecked == true;
                TerminalCobraIVA = ChkTerminalCobraIVA.IsChecked == true;
                PorcentajeIVA = ivaPorcentaje;
                NombreBanco = TxtNombreBanco.Text.Trim();
                TipoTerminal = CmbTipoTerminal.SelectedItem?.ToString() ?? "";

                var config = new ComisionConfig
                {
                    PorcentajeComisionPredeterminado = porcentaje,
                    ComisionActivaPorDefecto = ComisionActivaPorDefecto,
                    TerminalCobraIVA = TerminalCobraIVA,
                    PorcentajeIVA = PorcentajeIVA,
                    NombreBanco = NombreBanco,
                    TipoTerminal = TipoTerminal
                };
                config.Guardar();

                // Notificar cambios
                ConfiguracionActualizada?.Invoke(this, config);

                string mensaje = "✅ Configuración guardada exitosamente!\n\n";
                mensaje += $"Banco/Procesador: {NombreBanco}\n";
                mensaje += $"Tipo de terminal: {TipoTerminal}\n";
                mensaje += $"Comisión: {porcentaje:F2}%\n";
                mensaje += $"Terminal cobra IVA: {(TerminalCobraIVA ? $"Sí ({ivaPorcentaje:F2}%)" : "No")}\n";
                mensaje += $"Activar por defecto: {(ComisionActivaPorDefecto ? "Sí" : "No")}";

                MessageBox.Show(mensaje, "Configuración Guardada",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ComisionConfig
    {
        public decimal PorcentajeComisionPredeterminado { get; set; } = 3.5m;
        public bool ComisionActivaPorDefecto { get; set; } = false;
        public bool TerminalCobraIVA { get; set; } = false;
        public decimal PorcentajeIVA { get; set; } = 16m;
        public string NombreBanco { get; set; } = "";
        public string TipoTerminal { get; set; } = "";

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CostBenefi",
            "comisiones.json"
        );

        public void Guardar()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar configuración: {ex.Message}");
            }
        }

        public static ComisionConfig Cargar()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<ComisionConfig>(json) ?? new ComisionConfig();
                }
            }
            catch (Exception)
            {
                // Si hay error al cargar, usar configuración por defecto
            }

            return new ComisionConfig();
        }
    }
}