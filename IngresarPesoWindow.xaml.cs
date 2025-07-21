using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class IngresarPesoWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly RawMaterial _producto;
        private readonly BasculaService _basculaService;

        public decimal PesoIngresado { get; private set; } = 0;

        public IngresarPesoWindow(AppDbContext context, RawMaterial producto, BasculaService basculaService)
        {
            _context = context;
            _producto = producto;
            _basculaService = basculaService;

            InitializeComponent();
            ConfigurarVentana();
            ConfigurarEventos();
        }

        private void ConfigurarVentana()
        {
            // Configurar información del producto
            TxtNombreProducto.Text = _producto.NombreArticulo;
            TxtUnidadMedida.Text = _producto.UnidadMedida;
            TxtStockDisponible.Text = $"{_producto.StockTotal:F3} {_producto.UnidadMedida}";
            TxtPrecioUnitario.Text = _producto.PrecioVentaFinal.ToString("C2");

            // Configurar estado báscula
            ActualizarEstadoBascula();

            // Enfocar textbox de peso
            TxtPesoManual.Focus();
            TxtPesoManual.SelectAll();
        }

        private void ConfigurarEventos()
        {
            // Evento peso recibido de báscula
            _basculaService.PesoRecibido += BasculaService_PesoRecibido;
            _basculaService.ErrorOcurrido += BasculaService_ErrorOcurrido;
            _basculaService.EstadoConexionCambiado += BasculaService_EstadoConexionCambiado;
        }

        private void ActualizarEstadoBascula()
        {
            if (_basculaService.EstaConectada)
            {
                TxtEstadoBascula.Text = "✅ Conectada";
                TxtEstadoBascula.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                BtnLeerBascula.IsEnabled = true;
                BtnTarar.IsEnabled = true;
            }
            else
            {
                TxtEstadoBascula.Text = "❌ Desconectada";
                TxtEstadoBascula.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                BtnLeerBascula.IsEnabled = false;
                BtnTarar.IsEnabled = false;
            }

            TxtNombreBascula.Text = _basculaService.NombreBascula ?? "Sin configurar";
            TxtPuertoBascula.Text = _basculaService.PuertoActual ?? "N/A";
        }

        private async void BtnLeerBascula_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnLeerBascula.IsEnabled = false;
                BtnLeerBascula.Content = "⏳ Leyendo...";
                TxtStatus.Text = "📖 Leyendo peso de báscula...";

                var peso = await _basculaService.LeerPesoAsync();

                if (peso > 0)
                {
                    TxtPesoBascula.Text = $"{peso:F3}";
                    TxtPesoManual.Text = $"{peso:F3}";
                    TxtStatus.Text = $"✅ Peso leído: {peso:F3} {_producto.UnidadMedida}";
                    ActualizarCalculos();
                }
                else
                {
                    TxtStatus.Text = "⚠️ No se pudo leer peso válido";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error al leer báscula: {ex.Message}", "Error Báscula",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnLeerBascula.IsEnabled = true;
                BtnLeerBascula.Content = "📖 Leer Báscula";
            }
        }

        private async void BtnTarar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnTarar.IsEnabled = false;
                BtnTarar.Content = "⏳";
                TxtStatus.Text = "⚖️ Tarando báscula...";

                var resultado = await _basculaService.TararAsync();

                if (resultado)
                {
                    TxtStatus.Text = "✅ Báscula tarada correctamente";
                    TxtPesoBascula.Text = "0.000";
                }
                else
                {
                    TxtStatus.Text = "❌ Error al tarar báscula";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error al tarar: {ex.Message}";
            }
            finally
            {
                BtnTarar.IsEnabled = true;
                BtnTarar.Content = "⚖️ Tarar";
            }
        }

        private void TxtPesoManual_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarCalculos();
        }

        private void ActualizarCalculos()
        {
            try
            {
                if (decimal.TryParse(TxtPesoManual.Text, out decimal peso) && peso > 0)
                {
                    // Validar que no exceda el stock
                    if (peso > _producto.StockTotal)
                    {
                        TxtStatus.Text = $"⚠️ Cantidad excede el stock disponible ({_producto.StockTotal:F3})";
                        TxtSubtotal.Text = "$0.00";
                        TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        BtnConfirmar.IsEnabled = false;
                        return;
                    }

                    // Calcular subtotal
                    var subtotal = peso * _producto.PrecioVentaFinal;
                    TxtSubtotal.Text = subtotal.ToString("C2");
                    TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));

                    // Calcular ganancia
                    var ganancia = peso * (_producto.PrecioVentaFinal - _producto.PrecioConIVA);
                    TxtGanancia.Text = $"Ganancia: {ganancia:C2}";

                    // Habilitar confirmar
                    BtnConfirmar.IsEnabled = true;
                    TxtStatus.Text = $"✅ Peso válido: {peso:F3} {_producto.UnidadMedida}";
                }
                else
                {
                    TxtSubtotal.Text = "$0.00";
                    TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                    TxtGanancia.Text = "Ganancia: $0.00";
                    BtnConfirmar.IsEnabled = false;

                    if (!string.IsNullOrEmpty(TxtPesoManual.Text))
                    {
                        TxtStatus.Text = "⚠️ Ingrese un peso válido";
                    }
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error en cálculos: {ex.Message}";
                BtnConfirmar.IsEnabled = false;
            }
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(TxtPesoManual.Text, out decimal peso) && peso > 0)
                {
                    if (peso > _producto.StockTotal)
                    {
                        MessageBox.Show($"La cantidad ingresada ({peso:F3}) excede el stock disponible ({_producto.StockTotal:F3}).",
                                      "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    PesoIngresado = peso;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ingrese un peso válido mayor a cero.", "Peso Inválido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPesoManual.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al confirmar peso: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtPesoManual_KeyDown(object sender, KeyEventArgs e)
        {
            // Permitir Enter para confirmar
            if (e.Key == Key.Enter && BtnConfirmar.IsEnabled)
            {
                BtnConfirmar_Click(sender, new RoutedEventArgs());
                return;
            }

            // Permitir solo números, punto decimal y teclas de control
            if (!IsValidKey(e.Key))
            {
                e.Handled = true;
            }
        }

        private bool IsValidKey(Key key)
        {
            return key >= Key.D0 && key <= Key.D9 ||
                   key >= Key.NumPad0 && key <= Key.NumPad9 ||
                   key == Key.Decimal || key == Key.OemPeriod ||
                   key == Key.Back || key == Key.Delete ||
                   key == Key.Left || key == Key.Right ||
                   key == Key.Tab || key == Key.Enter;
        }

        private void BasculaService_PesoRecibido(object sender, PesoEventArgs e)
        {
            // Ejecutar en UI thread
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtPesoBascula.Text = $"{e.Peso:F3}";
                TxtPesoManual.Text = $"{e.Peso:F3}";
                TxtStatus.Text = $"📡 Peso automático: {e.PesoFormateado}";
                ActualizarCalculos();
            }));
        }

        private void BasculaService_ErrorOcurrido(object sender, string e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtStatus.Text = $"❌ Error báscula: {e}";
            }));
        }

        private void BasculaService_EstadoConexionCambiado(object sender, bool conectada)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ActualizarEstadoBascula();
            }));
        }

        protected override void OnClosed(EventArgs e)
        {
            // Desuscribir eventos
            _basculaService.PesoRecibido -= BasculaService_PesoRecibido;
            _basculaService.ErrorOcurrido -= BasculaService_ErrorOcurrido;
            _basculaService.EstadoConexionCambiado -= BasculaService_EstadoConexionCambiado;

            base.OnClosed(e);
        }

        private void InitializeComponent()
        {
            Title = "⚖️ Ingresar Peso";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20);

            // Definir filas
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Info producto
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Estado báscula
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Controles báscula
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Peso manual
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Cálculos
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Spacer
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones

            // Header
            var header = new TextBlock
            {
                Text = "⚖️ Ingreso de Peso por Báscula",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 59, 78))
            };
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Info del producto
            var productoBorder = CreateInfoSection("🛍️ Información del Producto", 1);
            var productoGrid = new Grid();
            productoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            productoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            productoGrid.RowDefinitions.Add(new RowDefinition());
            productoGrid.RowDefinitions.Add(new RowDefinition());

            TxtNombreProducto = CreateInfoLabel("Producto:", "", 0, 0, productoGrid);
            TxtUnidadMedida = CreateInfoLabel("Unidad:", "", 0, 1, productoGrid);
            TxtStockDisponible = CreateInfoLabel("Stock:", "", 1, 0, productoGrid);
            TxtPrecioUnitario = CreateInfoLabel("Precio:", "", 1, 1, productoGrid);

            productoBorder.Child = productoGrid;
            mainGrid.Children.Add(productoBorder);

            // Estado báscula
            var basculaBorder = CreateInfoSection("⚖️ Estado de la Báscula", 2);
            var basculaGrid = new Grid();
            basculaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            basculaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            basculaGrid.RowDefinitions.Add(new RowDefinition());
            basculaGrid.RowDefinitions.Add(new RowDefinition());

            TxtEstadoBascula = CreateInfoLabel("Estado:", "", 0, 0, basculaGrid);
            TxtNombreBascula = CreateInfoLabel("Báscula:", "", 0, 1, basculaGrid);
            TxtPuertoBascula = CreateInfoLabel("Puerto:", "", 1, 0, basculaGrid);
            TxtPesoBascula = CreateInfoLabel("Último peso:", "0.000", 1, 1, basculaGrid);

            basculaBorder.Child = basculaGrid;
            mainGrid.Children.Add(basculaBorder);

            // Controles báscula
            var controlesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 20)
            };

            BtnLeerBascula = CreateButton("📖 Leer Báscula", Color.FromRgb(34, 197, 94));
            BtnLeerBascula.Click += BtnLeerBascula_Click;
            BtnTarar = CreateButton("⚖️ Tarar", Color.FromRgb(249, 115, 22));
            BtnTarar.Click += BtnTarar_Click;

            controlesPanel.Children.Add(BtnLeerBascula);
            controlesPanel.Children.Add(BtnTarar);

            Grid.SetRow(controlesPanel, 3);
            mainGrid.Children.Add(controlesPanel);

            // Peso manual
            var pesoPanel = CreateInfoSection("✏️ Peso Manual", 4);
            var pesoGrid = new Grid();
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pesoLabel = new TextBlock { Text = "Peso:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            Grid.SetColumn(pesoLabel, 0);
            pesoGrid.Children.Add(pesoLabel);

            TxtPesoManual = new TextBox
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10),
                Text = "0.000",
                TextAlignment = TextAlignment.Center
            };
            TxtPesoManual.TextChanged += TxtPesoManual_TextChanged;
            TxtPesoManual.KeyDown += TxtPesoManual_KeyDown;
            Grid.SetColumn(TxtPesoManual, 1);
            pesoGrid.Children.Add(TxtPesoManual);

            var unidadLabel = new TextBlock { Text = _producto?.UnidadMedida ?? "kg", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(unidadLabel, 2);
            pesoGrid.Children.Add(unidadLabel);

            pesoPanel.Child = pesoGrid;
            mainGrid.Children.Add(pesoPanel);

            // Cálculos
            var calculosPanel = CreateInfoSection("💰 Cálculos", 5);
            var calculosGrid = new Grid();
            calculosGrid.RowDefinitions.Add(new RowDefinition());
            calculosGrid.RowDefinitions.Add(new RowDefinition());
            calculosGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            calculosGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TxtSubtotal = new TextBlock
            {
                Text = "$0.00",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94))
            };
            Grid.SetRow(TxtSubtotal, 0);
            Grid.SetColumnSpan(TxtSubtotal, 2);
            calculosGrid.Children.Add(TxtSubtotal);

            TxtGanancia = new TextBlock
            {
                Text = "Ganancia: $0.00",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(TxtGanancia, 1);
            Grid.SetColumnSpan(TxtGanancia, 2);
            calculosGrid.Children.Add(TxtGanancia);

            calculosPanel.Child = calculosGrid;
            mainGrid.Children.Add(calculosPanel);

            // Status
            TxtStatus = new TextBlock
            {
                Text = "💡 Ingrese el peso del producto o use la báscula",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 20, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(TxtStatus, 7);
            mainGrid.Children.Add(TxtStatus);

            // Botones
            var botonesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var btnCancelar = CreateButton("❌ Cancelar", Color.FromRgb(108, 117, 125));
            btnCancelar.Click += BtnCancelar_Click;

            BtnConfirmar = CreateButton("✅ Confirmar", Color.FromRgb(34, 197, 94));
            BtnConfirmar.Click += BtnConfirmar_Click;
            BtnConfirmar.IsEnabled = false;

            botonesPanel.Children.Add(btnCancelar);
            botonesPanel.Children.Add(BtnConfirmar);

            Grid.SetRow(botonesPanel, 8);
            mainGrid.Children.Add(botonesPanel);

            Content = mainGrid;
        }

        // Campos para controles
        private TextBlock TxtNombreProducto;
        private TextBlock TxtUnidadMedida;
        private TextBlock TxtStockDisponible;
        private TextBlock TxtPrecioUnitario;
        private TextBlock TxtEstadoBascula;
        private TextBlock TxtNombreBascula;
        private TextBlock TxtPuertoBascula;
        private TextBlock TxtPesoBascula;
        private Button BtnLeerBascula;
        private Button BtnTarar;
        private TextBox TxtPesoManual;
        private TextBlock TxtSubtotal;
        private TextBlock TxtGanancia;
        private TextBlock TxtStatus;
        private Button BtnConfirmar;

        private Border CreateInfoSection(string titulo, int row)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(5),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 5, 0, 10)
            };

            var stack = new StackPanel();

            var tituloText = new TextBlock
            {
                Text = titulo,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };
            stack.Children.Add(tituloText);

            Grid.SetRow(border, row);
            return border;
        }

        private TextBlock CreateInfoLabel(string label, string value, int row, int col, Grid parent)
        {
            var stack = new StackPanel { Margin = new Thickness(5) };

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };

            stack.Children.Add(labelText);
            stack.Children.Add(valueText);

            Grid.SetRow(stack, row);
            Grid.SetColumn(stack, col);
            parent.Children.Add(stack);

            return valueText;
        }

        private Button CreateButton(string content, Color backgroundColor)
        {
            return new Button
            {
                Content = content,
                Width = 130,
                Height = 35,
                Margin = new Thickness(5, 0, 5, 0),
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