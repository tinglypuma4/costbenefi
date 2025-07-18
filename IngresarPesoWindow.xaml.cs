using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public decimal PesoIngresado { get; private set; }

        public IngresarPesoWindow(AppDbContext context, RawMaterial producto, BasculaService basculaService)
        {
            _context = context;
            _producto = producto;
            _basculaService = basculaService;

            InitializeComponent();
            ConfigurarVentana();
        }

        private void InitializeComponent()
        {
            Title = "Ingresar Peso del Producto";
            Width = 450;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var grid = new Grid { Margin = new Thickness(20) };

            // Definir filas
            for (int i = 0; i < 8; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Título
            var titulo = new TextBlock
            {
                Text = "⚖️ Ingresar Peso del Producto",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 59, 78))
            };
            Grid.SetRow(titulo, 0);
            grid.Children.Add(titulo);

            // Información del producto
            var infoProducto = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackInfo = new StackPanel();
            stackInfo.Children.Add(new TextBlock
            {
                Text = $"Producto: {_producto.NombreArticulo}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = $"Precio: {_producto.PrecioVentaFinal:C2} por {_producto.UnidadMedida}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = $"Stock disponible: {_producto.StockTotal:F3} {_producto.UnidadMedida}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            });

            infoProducto.Child = stackInfo;
            Grid.SetRow(infoProducto, 1);
            grid.Children.Add(infoProducto);

            // Peso desde báscula
            var labelBascula = new TextBlock
            {
                Text = "Peso desde báscula:",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(labelBascula, 2);
            grid.Children.Add(labelBascula);

            var panelBascula = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var txtPesoBascula = new TextBox
            {
                Name = "TxtPesoBascula",
                Width = 150,
                Height = 32,
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Text = "0.000",
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var btnLeerBascula = new Button
            {
                Content = "📏 Leer Báscula",
                Width = 120,
                Height = 32,
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            btnLeerBascula.Click += BtnLeerBascula_Click;

            var btnTarar = new Button
            {
                Content = "🔄 Tarar",
                Width = 80,
                Height = 32,
                Margin = new Thickness(5, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12
            };
            btnTarar.Click += (s, e) =>
            {
                _basculaService?.Tarar();
                txtPesoBascula.Text = "0.000";
            };

            panelBascula.Children.Add(txtPesoBascula);
            panelBascula.Children.Add(btnLeerBascula);
            panelBascula.Children.Add(btnTarar);

            Grid.SetRow(panelBascula, 3);
            grid.Children.Add(panelBascula);

            // Peso manual
            var labelManual = new TextBlock
            {
                Text = "O ingrese peso manualmente:",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(labelManual, 4);
            grid.Children.Add(labelManual);

            var panelManual = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var txtPesoManual = new TextBox
            {
                Name = "TxtPesoManual",
                Width = 150,
                Height = 32,
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var labelUnidad = new TextBlock
            {
                Text = _producto.UnidadMedida,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };

            panelManual.Children.Add(txtPesoManual);
            panelManual.Children.Add(labelUnidad);

            Grid.SetRow(panelManual, 5);
            grid.Children.Add(panelManual);

            // Cálculo del precio
            var precioBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(209, 250, 229)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackPrecio = new StackPanel();
            stackPrecio.Children.Add(new TextBlock
            {
                Text = "💰 Cálculo del Precio",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var lblPrecioTotal = new TextBlock
            {
                Name = "LblPrecioTotal",
                Text = "Precio total: $0.00",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPrecio.Children.Add(lblPrecioTotal);

            precioBorder.Child = stackPrecio;
            Grid.SetRow(precioBorder, 6);
            grid.Children.Add(precioBorder);

            // Botones finales
            var panelBotones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnCancelar = new Button
            {
                Content = "❌ Cancelar",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12
            };
            btnCancelar.Click += (s, e) => { DialogResult = false; Close(); };

            var btnAceptar = new Button
            {
                Content = "✅ Agregar al Carrito",
                Width = 140,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            btnAceptar.Click += (s, e) => ProcesarPeso(txtPesoBascula, txtPesoManual);

            panelBotones.Children.Add(btnCancelar);
            panelBotones.Children.Add(btnAceptar);

            Grid.SetRow(panelBotones, 7);
            grid.Children.Add(panelBotones);

            Content = grid;

            // Configurar eventos de báscula
            if (_basculaService != null)
            {
                _basculaService.PesoRecibido += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtPesoBascula.Text = e.Peso.ToString("F3");
                        ActualizarPrecioTotal(e.Peso, lblPrecioTotal);
                    });
                };
            }

            // Eventos para actualizar precio en tiempo real
            txtPesoManual.TextChanged += (s, e) =>
            {
                if (decimal.TryParse(txtPesoManual.Text, out decimal peso))
                {
                    ActualizarPrecioTotal(peso, lblPrecioTotal);
                }
            };
        }

        private void ConfigurarVentana()
        {
            // Configuración adicional si es necesaria
        }

        private async void BtnLeerBascula_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_basculaService != null)
                {
                    var peso = await _basculaService.LeerPesoAsync();
                    // El peso se actualiza automáticamente por el evento
                }
                else
                {
                    MessageBox.Show("Báscula no disponible", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer báscula: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarPrecioTotal(decimal peso, TextBlock lblPrecio)
        {
            try
            {
                var precioTotal = peso * _producto.PrecioVentaFinal;
                lblPrecio.Text = $"Precio total: {precioTotal:C2}";
            }
            catch
            {
                lblPrecio.Text = "Precio total: $0.00";
            }
        }

        private void ProcesarPeso(TextBox txtBascula, TextBox txtManual)
        {
            try
            {
                decimal peso = 0;

                // Priorizar peso manual si está ingresado
                if (!string.IsNullOrWhiteSpace(txtManual.Text))
                {
                    if (!decimal.TryParse(txtManual.Text, out peso))
                    {
                        MessageBox.Show("Ingrese un peso válido", "Peso Inválido",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(txtBascula.Text))
                {
                    if (!decimal.TryParse(txtBascula.Text, out peso))
                    {
                        MessageBox.Show("Peso de báscula inválido", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (peso <= 0)
                {
                    MessageBox.Show("El peso debe ser mayor a cero", "Peso Inválido",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (peso > _producto.StockTotal)
                {
                    MessageBox.Show(
                        $"Peso excede el stock disponible.\n" +
                        $"Solicitado: {peso:F3} {_producto.UnidadMedida}\n" +
                        $"Disponible: {_producto.StockTotal:F3} {_producto.UnidadMedida}",
                        "Stock Insuficiente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                PesoIngresado = peso;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar peso: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}   