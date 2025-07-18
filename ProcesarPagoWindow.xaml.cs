using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class ProcesarPagoWindow : Window
    {
        private decimal _totalVenta;
        private decimal _pagoEfectivo = 0;
        private decimal _pagoTarjeta = 0;
        private decimal _pagoTransferencia = 0;
        private decimal _efectivoRecibido = 0;

        // Propiedades para comisiones
        private decimal _porcentajeComisionTarjeta = 0;
        private decimal _porcentajeIVA = 0;
        private decimal _comisionTarjeta = 0;
        private decimal _ivaComision = 0;
        private bool _tieneComisionTarjeta = false;
        private bool _terminalCobraIVA = false;

        public bool PagoConfirmado { get; private set; } = false;
        public string FormaPagoFinal { get; private set; } = "";
        public string DetallesPago { get; private set; } = "";
        public decimal CambioADar { get; private set; } = 0;
        public string NombreCliente { get; private set; }
        public decimal Monto { get; private set; }

        // Propiedades públicas para comisiones
        public decimal MontoEfectivo => _pagoEfectivo > 0 ? _pagoEfectivo : _efectivoRecibido;
        public decimal MontoTarjeta => _pagoTarjeta;
        public decimal MontoTransferencia => _pagoTransferencia;
        public decimal ComisionTarjeta => _comisionTarjeta;
        public decimal IVAComision => _ivaComision;
        public decimal PorcentajeComisionTarjeta => _porcentajeComisionTarjeta;
        public decimal PorcentajeIVA => _porcentajeIVA;

        // Controles
        private TextBox TxtCliente;
        private TextBlock TxtTotalVenta;
        private TextBox TxtEfectivoRecibido;
        private Button BtnPagoCompleto;
        private TextBox TxtPagoEfectivo;
        private TextBox TxtPagoTarjeta;
        private TextBox TxtPagoTransferencia;
        private TextBlock TxtTotalPagado;
        private TextBlock TxtTotalPendiente;
        private TextBlock TxtCambio;
        private Button BtnConfirmar;
        private CheckBox ChkComisionTarjeta;
        private TextBlock TxtComisionCalculada;
        private TextBlock TxtIVAComision;
        private TextBlock TxtTotalRealRecibido;

        public ProcesarPagoWindow(decimal totalVenta, string cliente)
        {
            InitializeComponent();
            _totalVenta = totalVenta;
            NombreCliente = cliente?.Trim() ?? "";

            // Configurar interfaz
            TxtCliente.Text = NombreCliente;
            TxtTotalVenta.Text = _totalVenta.ToString("C2");
            TxtTotalPendiente.Text = _totalVenta.ToString("C2");

            // Cargar configuración de comisiones
            CargarConfiguracionComisiones();

            // Enfocar en efectivo
            TxtEfectivoRecibido.Focus();

            // Configurar eventos
            TxtEfectivoRecibido.TextChanged += OnPagoChanged;
            TxtEfectivoRecibido.PreviewTextInput += NumericTextBox_PreviewTextInput;
            TxtEfectivoRecibido.LostFocus += FormatMontoTextBox;
            TxtPagoEfectivo.TextChanged += OnPagoChanged;
            TxtPagoTarjeta.TextChanged += OnPagoChanged;
            TxtPagoTransferencia.TextChanged += OnPagoChanged;
            ChkComisionTarjeta.Checked += OnComisionChanged;
            ChkComisionTarjeta.Unchecked += OnComisionChanged;

            UpdateCalculos();
        }

        private void CargarConfiguracionComisiones()
        {
            try
            {
                var config = ComisionConfig.Cargar();
                ChkComisionTarjeta.IsChecked = config.ComisionActivaPorDefecto;
                _porcentajeComisionTarjeta = config.PorcentajeComisionPredeterminado;
                _terminalCobraIVA = config.TerminalCobraIVA;
                _porcentajeIVA = config.PorcentajeIVA;
            }
            catch (Exception)
            {
                ChkComisionTarjeta.IsChecked = false;
                _porcentajeComisionTarjeta = 3.50m;
                _terminalCobraIVA = false;
                _porcentajeIVA = 16m;
            }
        }

        private void InitializeComponent()
        {
            Title = "💰 Procesar Pago";
            Width = 650;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                Padding = new Thickness(20, 20, 20, 20)
            };

            var mainGrid = new Grid();
            for (int i = 0; i < 12; i++)
            {
                mainGrid.RowDefinitions.Add(i == 10 ? new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } : new RowDefinition { Height = GridLength.Auto });
            }

            // ===== MÉTODO CORREGIDO SIN .Last() =====
            var headerPanel = CreateHeaderPanel();
            mainGrid.Children.Add(headerPanel);
            Grid.SetRow(headerPanel, 0);

            var infoPanel = CreateInfoPanel();
            mainGrid.Children.Add(infoPanel);
            Grid.SetRow(infoPanel, 1);

            var separator1 = new Separator { Margin = new Thickness(0, 10, 0, 10) };
            mainGrid.Children.Add(separator1);
            Grid.SetRow(separator1, 2);

            var pagoSimplePanel = CreatePagoSimplePanel();
            mainGrid.Children.Add(pagoSimplePanel);
            Grid.SetRow(pagoSimplePanel, 3);

            var separator2 = new Separator { Margin = new Thickness(0, 10, 0, 10) };
            mainGrid.Children.Add(separator2);
            Grid.SetRow(separator2, 4);

            var pagoCombinadoPanel = CreatePagoCombinadoPanel();
            mainGrid.Children.Add(pagoCombinadoPanel);
            Grid.SetRow(pagoCombinadoPanel, 5);

            var separator3 = new Separator { Margin = new Thickness(0, 10, 0, 10) };
            mainGrid.Children.Add(separator3);
            Grid.SetRow(separator3, 6);

            var comisionPanel = CreateComisionPanel();
            mainGrid.Children.Add(comisionPanel);
            Grid.SetRow(comisionPanel, 7);

            var separator4 = new Separator { Margin = new Thickness(0, 10, 0, 10) };
            mainGrid.Children.Add(separator4);
            Grid.SetRow(separator4, 8);

            var totalesPanel = CreateTotalesPanel();
            mainGrid.Children.Add(totalesPanel);
            Grid.SetRow(totalesPanel, 9);

            var botonesPanel = CreateBotonesPanel();
            mainGrid.Children.Add(botonesPanel);
            Grid.SetRow(botonesPanel, 11);

            scrollViewer.Content = mainGrid;
            Content = scrollViewer;
        }

        private UIElement CreateHeaderPanel()
        {
            var headerPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(5, 150, 105)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 12, 20, 12),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(new TextBlock { Text = "💰", FontSize = 20, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
            headerStack.Children.Add(new TextBlock { Text = "Procesar Pago", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            headerPanel.Child = headerStack;

            return headerPanel;
        }

        private UIElement CreateInfoPanel()
        {
            var panel = new StackPanel();
            var clienteGrid = new Grid();
            clienteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            clienteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var clienteLabel = new TextBlock { Text = "Cliente:", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), FontSize = 12 };
            Grid.SetColumn(clienteLabel, 0);

            TxtCliente = new TextBox { Height = 28, FontSize = 12, Padding = new Thickness(8, 4, 8, 4), IsReadOnly = true, Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)) };
            Grid.SetColumn(TxtCliente, 1);

            clienteGrid.Children.Add(clienteLabel);
            clienteGrid.Children.Add(TxtCliente);

            var totalGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var totalLabel = new TextBlock { Text = "Total:", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), FontSize = 12 };
            Grid.SetColumn(totalLabel, 0);

            TxtTotalVenta = new TextBlock { FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(TxtTotalVenta, 1);

            totalGrid.Children.Add(totalLabel);
            totalGrid.Children.Add(TxtTotalVenta);

            panel.Children.Add(clienteGrid);
            panel.Children.Add(totalGrid);

            return panel;
        }

        private UIElement CreatePagoSimplePanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "💵 Pago Simple (Efectivo)", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)), Margin = new Thickness(0, 0, 0, 8) });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var label = new TextBlock { Text = "Efectivo recibido:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), FontSize = 11 };
            Grid.SetColumn(label, 0);

            TxtEfectivoRecibido = new TextBox { Height = 30, FontSize = 14, Padding = new Thickness(8, 4, 8, 4), BorderBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105)), BorderThickness = new Thickness(2, 2, 2, 2), Text = "0" };
            Grid.SetColumn(TxtEfectivoRecibido, 1);

            BtnPagoCompleto = new Button { Content = "Exacto", Height = 30, FontSize = 11, Background = new SolidColorBrush(Color.FromRgb(99, 102, 241)), Foreground = Brushes.White, BorderThickness = new Thickness(0, 0, 0, 0), Margin = new Thickness(5, 0, 0, 0) };
            BtnPagoCompleto.Click += BtnPagoCompleto_Click;
            Grid.SetColumn(BtnPagoCompleto, 2);

            grid.Children.Add(label);
            grid.Children.Add(TxtEfectivoRecibido);
            grid.Children.Add(BtnPagoCompleto);
            panel.Children.Add(grid);

            return panel;
        }

        private UIElement CreatePagoCombinadoPanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "💳 Pago Combinado", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)), Margin = new Thickness(0, 0, 0, 8) });

            var efectivoGrid = CreatePagoRow("💵 Efectivo:", out TxtPagoEfectivo, out var btnEfectivo);
            btnEfectivo.Click += (s, e) => BtnFaltanteEfectivo_Click();
            panel.Children.Add(efectivoGrid);

            var tarjetaGrid = CreatePagoRow("💳 Tarjeta:", out TxtPagoTarjeta, out var btnTarjeta);
            btnTarjeta.Click += (s, e) => BtnFaltanteTarjeta_Click();
            panel.Children.Add(tarjetaGrid);

            var transferenciaGrid = CreatePagoRow("📱 Transferencia:", out TxtPagoTransferencia, out var btnTransferencia);
            btnTransferencia.Click += (s, e) => BtnFaltanteTransferencia_Click();
            panel.Children.Add(transferenciaGrid);

            return panel;
        }

        private UIElement CreatePagoRow(string label, out TextBox textBox, out Button button)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var lblText = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), FontSize = 11 };
            Grid.SetColumn(lblText, 0);

            textBox = new TextBox { Height = 26, FontSize = 12, Padding = new Thickness(6, 3, 6, 3), Text = "0" };
            textBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
            textBox.LostFocus += FormatMontoTextBox;
            Grid.SetColumn(textBox, 1);

            button = new Button { Content = "Faltante", Height = 26, FontSize = 10, Background = new SolidColorBrush(Color.FromRgb(156, 163, 175)), Foreground = Brushes.White, BorderThickness = new Thickness(0, 0, 0, 0), Margin = new Thickness(5, 0, 0, 0) };
            Grid.SetColumn(button, 2);

            grid.Children.Add(lblText);
            grid.Children.Add(textBox);
            grid.Children.Add(button);

            return grid;
        }

        private UIElement CreateComisionPanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "🏦 Comisiones de Terminal", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)), Margin = new Thickness(0, 0, 0, 8) });

            ChkComisionTarjeta = new CheckBox { Content = "Activar comisiones", FontSize = 12, Margin = new Thickness(0, 0, 0, 8), Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)) };
            panel.Children.Add(ChkComisionTarjeta);

            var infoGrid = new Grid { Margin = new Thickness(20, 0, 0, 0) };
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lblComision = new TextBlock { Text = "Comisión a pagar:", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(lblComision, 0);
            Grid.SetRow(lblComision, 0);

            TxtComisionCalculada = new TextBlock { Text = "$0.00", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(TxtComisionCalculada, 1);
            Grid.SetRow(TxtComisionCalculada, 0);

            var lblIVA = new TextBlock { Text = $"IVA sobre comisión ({_porcentajeIVA:F2}%):", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
            Grid.SetColumn(lblIVA, 0);
            Grid.SetRow(lblIVA, 1);

            TxtIVAComision = new TextBlock { Text = "$0.00", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
            Grid.SetColumn(TxtIVAComision, 1);
            Grid.SetRow(TxtIVAComision, 1);

            infoGrid.Children.Add(lblComision);
            infoGrid.Children.Add(TxtComisionCalculada);
            infoGrid.Children.Add(lblIVA);
            infoGrid.Children.Add(TxtIVAComision);
            panel.Children.Add(infoGrid);

            return panel;
        }

        private UIElement CreateTotalesPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 12, 12, 12)
            };

            var stack = new StackPanel();

            var totalPagadoGrid = new Grid();
            totalPagadoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalPagadoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var totalPagadoLabel = new TextBlock { Text = "Total pagado:", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
            Grid.SetColumn(totalPagadoLabel, 0);

            TxtTotalPagado = new TextBlock { FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)), VerticalAlignment = VerticalAlignment.Center, Text = "$0.00" };
            Grid.SetColumn(TxtTotalPagado, 1);

            totalPagadoGrid.Children.Add(totalPagadoLabel);
            totalPagadoGrid.Children.Add(TxtTotalPagado);

            var pendienteGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            pendienteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pendienteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pendienteLabel = new TextBlock { Text = "Pendiente:", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
            Grid.SetColumn(pendienteLabel, 0);

            TxtTotalPendiente = new TextBlock { FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Text = "$0.00" };
            Grid.SetColumn(TxtTotalPendiente, 1);

            pendienteGrid.Children.Add(pendienteLabel);
            pendienteGrid.Children.Add(TxtTotalPendiente);

            var cambioGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            cambioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cambioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cambioLabel = new TextBlock { Text = "Cambio a dar:", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
            Grid.SetColumn(cambioLabel, 0);

            TxtCambio = new TextBlock { FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Text = "$0.00" };
            Grid.SetColumn(TxtCambio, 1);

            cambioGrid.Children.Add(cambioLabel);
            cambioGrid.Children.Add(TxtCambio);

            var realGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            realGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            realGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var realLabel = new TextBlock { Text = "Total real recibido:", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)), FontSize = 12 };
            Grid.SetColumn(realLabel, 0);

            TxtTotalRealRecibido = new TextBlock { FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Text = "$0.00", Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)) };
            Grid.SetColumn(TxtTotalRealRecibido, 1);

            realGrid.Children.Add(realLabel);
            realGrid.Children.Add(TxtTotalRealRecibido);

            stack.Children.Add(totalPagadoGrid);
            stack.Children.Add(pendienteGrid);
            stack.Children.Add(cambioGrid);
            stack.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
            stack.Children.Add(realGrid);

            panel.Child = stack;
            return panel;
        }

        private UIElement CreateBotonesPanel()
        {
            var panel = new Grid();
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var btnCancelar = new Button { Content = "❌ Cancelar", Height = 40, FontSize = 13, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)), Foreground = Brushes.White, BorderThickness = new Thickness(0, 0, 0, 0), Margin = new Thickness(0, 0, 5, 0) };
            btnCancelar.Click += (s, e) => { PagoConfirmado = false; Close(); };
            Grid.SetColumn(btnCancelar, 0);

            BtnConfirmar = new Button { Content = "✅ Confirmar Pago", Height = 40, FontSize = 13, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(5, 150, 105)), Foreground = Brushes.White, BorderThickness = new Thickness(0, 0, 0, 0), Margin = new Thickness(5, 0, 0, 0), IsEnabled = false };
            BtnConfirmar.Click += BtnConfirmar_Click;
            Grid.SetColumn(BtnConfirmar, 1);

            panel.Children.Add(btnCancelar);
            panel.Children.Add(BtnConfirmar);

            return panel;
        }

        // Validación y formato
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !IsValidDecimal(text);
        }

        private bool IsValidDecimal(string text)
        {
            return decimal.TryParse(text, out decimal result) && result >= 0 && result <= 1000000;
        }

        private void FormatMontoTextBox(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (decimal.TryParse(textBox.Text, out decimal value))
            {
                textBox.Text = value.ToString("F2");
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            }
            else
            {
                textBox.BorderBrush = Brushes.Red;
            }
            UpdateCalculos();
        }

        // Métodos para botones de faltante
        private void BtnFaltanteEfectivo_Click()
        {
            try
            {
                ActualizarValoresActuales();
                decimal faltante = Math.Max(0, _totalVenta - _pagoTarjeta - _pagoTransferencia);
                TxtPagoEfectivo.Text = faltante.ToString("F2");
                TxtEfectivoRecibido.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular faltante: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFaltanteTarjeta_Click()
        {
            try
            {
                ActualizarValoresActuales();
                decimal faltante = Math.Max(0, _totalVenta - _pagoEfectivo - _pagoTransferencia);
                TxtPagoTarjeta.Text = faltante.ToString("F2");
                TxtEfectivoRecibido.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular faltante: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFaltanteTransferencia_Click()
        {
            try
            {
                ActualizarValoresActuales();
                decimal faltante = Math.Max(0, _totalVenta - _pagoEfectivo - _pagoTarjeta);
                TxtPagoTransferencia.Text = faltante.ToString("F2");
                TxtEfectivoRecibido.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular faltante: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarValoresActuales()
        {
            decimal.TryParse(TxtPagoEfectivo?.Text ?? "0", out _pagoEfectivo);
            decimal.TryParse(TxtPagoTarjeta?.Text ?? "0", out _pagoTarjeta);
            decimal.TryParse(TxtPagoTransferencia?.Text ?? "0", out _pagoTransferencia);
            decimal.TryParse(TxtEfectivoRecibido?.Text ?? "0", out _efectivoRecibido);

            // Actualizar comisiones con IVA
            _tieneComisionTarjeta = ChkComisionTarjeta?.IsChecked == true;
            if (_tieneComisionTarjeta)
            {
                _comisionTarjeta = _pagoTarjeta * (_porcentajeComisionTarjeta / 100);
                _ivaComision = _terminalCobraIVA ? _comisionTarjeta * (_porcentajeIVA / 100) : 0;
            }
            else
            {
                _comisionTarjeta = 0;
                _ivaComision = 0;
            }
        }

        // Eventos y cálculos
        private void OnPagoChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCalculos();
        }

        private void OnComisionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCalculos();
        }

        private void UpdateCalculos()
        {
            try
            {
                ActualizarValoresActuales();

                decimal totalPagado = Math.Max(_efectivoRecibido, _pagoEfectivo + _pagoTarjeta + _pagoTransferencia);
                decimal pendiente = Math.Max(0, _totalVenta - totalPagado);
                decimal cambio = Math.Max(0, totalPagado - _totalVenta);
                decimal comisionTotal = _comisionTarjeta + _ivaComision;
                decimal totalRealRecibido = totalPagado - comisionTotal;

                // Actualizar interfaz
                if (TxtTotalPagado != null) TxtTotalPagado.Text = totalPagado.ToString("C2");
                if (TxtTotalPendiente != null)
                {
                    TxtTotalPendiente.Text = pendiente.ToString("C2");
                    TxtTotalPendiente.Foreground = pendiente > 0 ?
                        new SolidColorBrush(Color.FromRgb(239, 68, 68)) :
                        new SolidColorBrush(Color.FromRgb(5, 150, 105));
                }
                if (TxtCambio != null)
                {
                    TxtCambio.Text = cambio.ToString("C2");
                    TxtCambio.Foreground = cambio > 0 ?
                        new SolidColorBrush(Color.FromRgb(245, 158, 11)) :
                        new SolidColorBrush(Color.FromRgb(156, 163, 175));
                }

                if (TxtComisionCalculada != null)
                {
                    TxtComisionCalculada.Text = _comisionTarjeta.ToString("C2");
                }

                if (TxtIVAComision != null)
                {
                    TxtIVAComision.Text = _ivaComision.ToString("C2");
                    TxtIVAComision.Visibility = _terminalCobraIVA ? Visibility.Visible : Visibility.Collapsed;
                    var parent = TxtIVAComision.Parent as Grid;
                    if (parent != null)
                    {
                        foreach (UIElement child in parent.Children)
                        {
                            if (child is TextBlock tb && tb.Text.Contains("IVA sobre"))
                            {
                                tb.Text = $"IVA sobre comisión ({_porcentajeIVA:F2}%):";
                                tb.Visibility = _terminalCobraIVA ? Visibility.Visible : Visibility.Collapsed;
                                break;
                            }
                        }
                    }
                }

                if (TxtTotalRealRecibido != null)
                {
                    TxtTotalRealRecibido.Text = totalRealRecibido.ToString("C2");
                }

                if (BtnConfirmar != null)
                {
                    BtnConfirmar.IsEnabled = totalPagado >= _totalVenta;
                }

                CambioADar = cambio;
                Monto = totalPagado;
            }
            catch (Exception)
            {
                // Manejar errores silenciosamente
            }
        }

        private void BtnPagoCompleto_Click(object sender, RoutedEventArgs e)
        {
            TxtEfectivoRecibido.Text = _totalVenta.ToString("F2");
            TxtPagoEfectivo.Text = "0";
            TxtPagoTarjeta.Text = "0";
            TxtPagoTransferencia.Text = "0";
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ActualizarValoresActuales();
                decimal totalPagado = Math.Max(_efectivoRecibido, _pagoEfectivo + _pagoTarjeta + _pagoTransferencia);

                if (totalPagado < _totalVenta)
                {
                    MessageBox.Show($"El pago es insuficiente.\n\nTotal: {_totalVenta:C2}\nPagado: {totalPagado:C2}\nFaltante: {(_totalVenta - totalPagado):C2}",
                        "Pago Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                NombreCliente = TxtCliente.Text.Trim();
                if (NombreCliente.Length > 100)
                {
                    MessageBox.Show("El nombre del cliente no puede exceder 100 caracteres.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Monto = totalPagado;

                if (_efectivoRecibido >= _totalVenta)
                {
                    FormaPagoFinal = "💵 Efectivo";
                    DetallesPago = $"Efectivo recibido: {_efectivoRecibido:C2}";
                    if (CambioADar > 0)
                    {
                        DetallesPago += $" | Cambio: {CambioADar:C2}";
                    }
                }
                else if (_pagoEfectivo + _pagoTarjeta + _pagoTransferencia >= _totalVenta)
                {
                    var metodos = new List<string>();
                    if (_pagoEfectivo > 0) metodos.Add($"Efectivo: {_pagoEfectivo:C2}");
                    if (_pagoTarjeta > 0) metodos.Add($"Tarjeta: {_pagoTarjeta:C2}");
                    if (_pagoTransferencia > 0) metodos.Add($"Transferencia: {_pagoTransferencia:C2}");

                    FormaPagoFinal = "💳 Pago Combinado";
                    DetallesPago = string.Join(" | ", metodos);

                    if (_comisionTarjeta > 0)
                    {
                        DetallesPago += $" | Comisión: {_comisionTarjeta:C2} ({_porcentajeComisionTarjeta:F2}%)";
                        if (_ivaComision > 0)
                        {
                            DetallesPago += $" | IVA: {_ivaComision:C2} ({_porcentajeIVA:F2}%)";
                        }
                    }
                }

                PagoConfirmado = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al confirmar pago: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}