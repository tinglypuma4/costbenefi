using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;
using System.Globalization;

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
            if (RbPorCantidad != null)
            {
                RbPorCantidad.IsChecked = true; // Empezar en modo cantidad
            }

            // Configurar visibilidad inicial
            if (PanelCantidad != null && PanelDinero != null)
            {
                PanelCantidad.Visibility = Visibility.Visible;
                PanelDinero.Visibility = Visibility.Collapsed;
            }

            // Configurar etiqueta inicial
            if (LabelDinamico != null)
            {
                LabelDinamico.Text = ObtenerEtiquetaUnidad();
            }

            // Configurar info inicial
            if (TxtConversionInfo != null)
            {
                TxtConversionInfo.Text = "💡 Seleccione modo de ingreso arriba";
            }

            System.Diagnostics.Debug.WriteLine($"✅ Ventana configurada para {ObtenerTipoUnidad(_producto.UnidadMedida)}: {_producto.UnidadMedida}");

        }

        private void ConfigurarEventos()
        {
            // Evento peso recibido de báscula
            _basculaService.PesoRecibido += BasculaService_PesoRecibido;  // ✅ CORRECTO
            _basculaService.ErrorOcurrido += BasculaService_ErrorOcurrido;  // ✅ CORRECTO
            _basculaService.EstadoConexionCambiado += BasculaService_EstadoConexionCambiado;  // ✅ CORRECTO

            // ✅ AGREGAR: Configurar eventos de radio buttons
            if (RbPorCantidad != null)
            {
                RbPorCantidad.Checked += RbPorCantidad_Checked;
            }
            if (RbPorDinero != null)
            {
                RbPorDinero.Checked += RbPorDinero_Checked;
            }
            System.Diagnostics.Debug.WriteLine("✅ Eventos de modos configurados correctamente");
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
                decimal cantidadFinal = 0;
                decimal dineroFinal = 0;
                bool esModoDinero = RbPorDinero?.IsChecked == true;

                if (esModoDinero)
                {
                    // 💰 MODO POR DINERO
                    if (decimal.TryParse(TxtDineroIngresado?.Text.Replace(",", "."),
                     NumberStyles.Number,
                     CultureInfo.InvariantCulture,
                     out decimal dineroIngresado) && dineroIngresado > 0)
                    {
                        // Calcular cantidad basada en dinero
                        if (_producto.PrecioVentaFinal > 0)
                        {
                            cantidadFinal = dineroIngresado / _producto.PrecioVentaFinal;
                            dineroFinal = dineroIngresado;

                            // Actualizar campo de cantidad automáticamente
                            if (TxtPesoManual != null)
                            {
                                TxtPesoManual.Text = cantidadFinal.ToString("F3");
                            }

                            // Mostrar conversión
                            if (TxtConversionInfo != null)
                            {
                                TxtConversionInfo.Text = $"💰 ${dineroIngresado:F2} = {cantidadFinal:F3} {_producto.UnidadMedida}";
                                TxtConversionInfo.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                            }
                        }
                        else
                        {
                            TxtStatus.Text = "❌ Producto sin precio configurado";
                            BtnConfirmar.IsEnabled = false;
                            return;
                        }
                    }
                    else
                    {
                        LimpiarCalculos("Ingrese un monto válido");
                        return;
                    }
                }
                else
                {
                    // ⚖️ MODO POR CANTIDAD (original)
                    if (decimal.TryParse(TxtPesoManual?.Text.Replace(",", "."),
                     NumberStyles.Number,
                     CultureInfo.InvariantCulture,
                     out decimal cantidad) && cantidad > 0)
                    {
                        cantidadFinal = cantidad;
                        dineroFinal = cantidad * _producto.PrecioVentaFinal;

                        // Actualizar campo de dinero automáticamente
                        if (TxtDineroIngresado != null)
                        {
                            TxtDineroIngresado.Text = dineroFinal.ToString("F2");
                        }

                        // Mostrar conversión
                        if (TxtConversionInfo != null)
                        {
                            TxtConversionInfo.Text = $"⚖️ {cantidadFinal:F3} {_producto.UnidadMedida} = ${dineroFinal:F2}";
                            TxtConversionInfo.Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                        }
                    }
                    else
                    {
                        LimpiarCalculos($"Ingrese una cantidad válida de {_producto.UnidadMedida}");
                        return;
                    }
                }

                // ✅ VALIDACIONES COMUNES
                if (cantidadFinal > _producto.StockTotal)
                {
                    TxtStatus.Text = $"⚠️ Cantidad excede el stock disponible ({_producto.StockTotal:F3})";
                    TxtSubtotal.Text = "$0.00";
                    TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    BtnConfirmar.IsEnabled = false;
                    return;
                }

                // ✅ CALCULAR TOTALES
                var subtotal = cantidadFinal * _producto.PrecioVentaFinal;
                TxtSubtotal.Text = subtotal.ToString("C2");
                TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));

                // Calcular ganancia
                var ganancia = cantidadFinal * (_producto.PrecioVentaFinal - _producto.PrecioConIVA);
                TxtGanancia.Text = $"Ganancia: {ganancia:C2}";

                // Habilitar confirmar
                BtnConfirmar.IsEnabled = true;

                var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);
                TxtStatus.Text = $"✅ {tipoUnidad} válido: {cantidadFinal:F3} {_producto.UnidadMedida} = ${subtotal:F2}";

            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error en cálculos: {ex.Message}";
                BtnConfirmar.IsEnabled = false;
                System.Diagnostics.Debug.WriteLine($"Error ActualizarCalculos: {ex}");
            }
        }
        private void LimpiarCalculos(string mensaje = "")
        {
            TxtSubtotal.Text = "$0.00";
            TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            TxtGanancia.Text = "Ganancia: $0.00";

            if (TxtConversionInfo != null)
            {
                TxtConversionInfo.Text = "";
            }

            BtnConfirmar.IsEnabled = false;

            if (!string.IsNullOrEmpty(mensaje))
            {
                TxtStatus.Text = $"⚠️ {mensaje}";
            }
        }

        // ✅ AGREGAR estos métodos en IngresarPesoWindow.cs (después de otros event handlers, línea ~180)

        private void RbPorCantidad_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PanelCantidad != null && PanelDinero != null)
                {
                    // Mostrar panel cantidad, ocultar panel dinero
                    PanelCantidad.Visibility = Visibility.Visible;
                    PanelDinero.Visibility = Visibility.Collapsed;

                    // Cambiar etiqueta
                    if (LabelDinamico != null)
                    {
                        LabelDinamico.Text = ObtenerEtiquetaUnidad();
                    }

                    // Enfocar campo cantidad
                    TxtPesoManual?.Focus();

                    TxtStatus.Text = $"📏 Modo por cantidad - Ingrese {_producto.UnidadMedida}";
                    ActualizarCalculos();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RbPorCantidad_Checked: {ex.Message}");
            }
        }

        private void RbPorDinero_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PanelCantidad != null && PanelDinero != null)
                {
                    // Ocultar panel cantidad, mostrar panel dinero
                    PanelCantidad.Visibility = Visibility.Collapsed;
                    PanelDinero.Visibility = Visibility.Visible;

                    // Cambiar etiqueta  
                    if (LabelDinamico != null)
                    {
                        LabelDinamico.Text = "Dinero:";
                    }

                    // Enfocar campo dinero
                    TxtDineroIngresado?.Focus();

                    TxtStatus.Text = "💰 Modo por dinero - Ingrese cantidad en pesos";
                    ActualizarCalculos();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RbPorDinero_Checked: {ex.Message}");
            }
        }

        private void TxtDineroIngresado_TextChanged(object sender, TextChangedEventArgs e)
        {
            // ✅ ACTUALIZACIÓN EN TIEMPO REAL para modo dinero
            if (RbPorDinero?.IsChecked == true)
            {
                ActualizarCalculos();
            }
        }

        private void TxtDineroIngresado_KeyDown(object sender, KeyEventArgs e)
        {
            // Permitir Enter para confirmar
            if (e.Key == Key.Enter && BtnConfirmar.IsEnabled)
            {
                BtnConfirmar_Click(sender, new RoutedEventArgs());
                return;
            }

            // Permitir solo números, punto decimal y teclas de control (mismo que cantidad)
            if (!IsValidKey(e.Key))
            {
                e.Handled = true;
            }
        }
        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal cantidadFinal = 0;
                bool esModoDinero = RbPorDinero?.IsChecked == true;

                if (esModoDinero)
                {
                    // 💰 MODO DINERO - Calcular cantidad desde dinero
                    if (decimal.TryParse(TxtDineroIngresado?.Text.Replace(",", "."),
                     NumberStyles.Number,
                     CultureInfo.InvariantCulture,
                     out decimal dinero) && dinero > 0)
                    {
                        if (_producto.PrecioVentaFinal > 0)
                        {
                            cantidadFinal = dinero / _producto.PrecioVentaFinal;
                        }
                        else
                        {
                            MessageBox.Show("El producto no tiene precio configurado.",
                                          "Error de Precio", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ingrese un monto válido mayor a cero.",
                                      "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtDineroIngresado?.Focus();
                        return;
                    }
                }
                else
                {
                    // ⚖️ MODO CANTIDAD - Usar cantidad directamente
                    if (decimal.TryParse(TxtPesoManual?.Text.Replace(",", "."),
                     NumberStyles.Number,
                     CultureInfo.InvariantCulture,
                     out decimal cantidad) && cantidad > 0)
                    {
                        cantidadFinal = cantidad;
                    }
                    else
                    {
                        var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);
                        MessageBox.Show($"Ingrese una cantidad válida de {tipoUnidad.ToLower()}.",
                                      "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtPesoManual?.Focus();
                        return;
                    }
                }

                // ✅ VALIDACIÓN DE STOCK COMÚN
                if (cantidadFinal > _producto.StockTotal)
                {
                    MessageBox.Show($"La cantidad ingresada ({cantidadFinal:F3}) excede el stock disponible ({_producto.StockTotal:F3}).",
                                  "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ CONFIRMAR CON INFORMACIÓN COMPLETA
                var tipoConfirmacion = ObtenerTipoUnidad(_producto.UnidadMedida);
                var dineroTotal = cantidadFinal * _producto.PrecioVentaFinal;
                var modoUsado = esModoDinero ? "💰 por dinero" : "⚖️ por cantidad";

                var mensaje = $"✅ CONFIRMAR VENTA ({modoUsado})\n\n" +
                             $"📦 Producto: {_producto.NombreArticulo}\n" +
                             $"📏 {tipoConfirmacion}: {cantidadFinal:F3} {_producto.UnidadMedida}\n" +
                             $"💰 Total: ${dineroTotal:F2}\n\n" +
                             $"¿Proceder con esta cantidad?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Cantidad",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    PesoIngresado = cantidadFinal; // ✅ Devolver la cantidad final calculada
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al confirmar: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error BtnConfirmar_Click: {ex}");
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

        // ✅ REEMPLAZAR MÉTODO COMPLETO InitializeComponent() en IngresarPesoWindow.cs
        private void InitializeComponent()
        {
            var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);
            Title = $"⚖️ Ingresar {tipoUnidad}";
            Width = 700;   // Más ancho
            Height = 650;  // Más alto para mejor visibilidad
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20);

            // ✅ DEFINIR FILAS AMPLIADAS (más filas para nuevos controles)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0 - Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1 - Info producto
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2 - Estado báscula
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3 - Controles báscula
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4 - Selector modo
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5 - Panel dinero
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 6 - Peso manual (cantidad)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 7 - Conversión info
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 8 - Cálculos
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 9 - Spacer
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 10 - Status
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 11 - Botones

            // ✅ HEADER DINÁMICO
            var header = new TextBlock
            {
                Text = $"⚖️ Ingreso de {tipoUnidad}",
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

            // ✅ NUEVO: Selector de modo (Por Cantidad vs Por Dinero)
            var selectorBorder = CreateInfoSection("🎯 Modo de Ingreso", 4);
            var selectorGrid = new Grid();
            selectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            selectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            RbPorCantidad = new RadioButton
            {
                Content = $"⚖️ Por {tipoUnidad}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                GroupName = "ModoIngreso",
                IsChecked = true,
                Margin = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center
            };
           

            RbPorDinero = new RadioButton
            {
                Content = "💰 Por Dinero",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                GroupName = "ModoIngreso",
                Margin = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center
            };
            

            Grid.SetColumn(RbPorCantidad, 0);
            Grid.SetColumn(RbPorDinero, 1);
            selectorGrid.Children.Add(RbPorCantidad);
            selectorGrid.Children.Add(RbPorDinero);
            selectorBorder.Child = selectorGrid;
            mainGrid.Children.Add(selectorBorder);

            // ✅ NUEVO: Panel para ingreso por dinero
            var dineroPanel = CreateInfoSection("💰 Ingreso por Dinero", 5);
            var dineroGrid = new Grid();
            dineroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            dineroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dineroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dineroLabel = new TextBlock
            {
                Text = "Dinero:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(dineroLabel, 0);
            dineroGrid.Children.Add(dineroLabel);

            TxtDineroIngresado = new TextBox
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10),
                Text = "0.00",
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(254, 249, 195)), // Fondo amarillo claro
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                BorderThickness = new Thickness(2)
            };
            TxtDineroIngresado.TextChanged += TxtDineroIngresado_TextChanged;
            TxtDineroIngresado.KeyDown += TxtDineroIngresado_KeyDown;
            Grid.SetColumn(TxtDineroIngresado, 1);
            dineroGrid.Children.Add(TxtDineroIngresado);

            var simboloPesoLabel = new TextBlock
            {
                Text = "$",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94))
            };
            Grid.SetColumn(simboloPesoLabel, 2);
            dineroGrid.Children.Add(simboloPesoLabel);

            PanelDinero = new StackPanel();
            PanelDinero.Children.Add(dineroGrid);
            dineroPanel.Child = PanelDinero;
            mainGrid.Children.Add(dineroPanel);

            // ✅ MODIFICADO: Peso/Cantidad manual con label dinámico
            var pesoPanel = CreateInfoSection($"✏️ {tipoUnidad} Manual", 6);
            var pesoGrid = new Grid();
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ✅ LABEL DINÁMICO
            LabelDinamico = new TextBlock
            {
                Text = ObtenerEtiquetaUnidad(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(LabelDinamico, 0);
            pesoGrid.Children.Add(LabelDinamico);

            TxtPesoManual = new TextBox
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10),
                Text = "0.000",
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)), // Fondo azul claro
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2)
            };
            TxtPesoManual.TextChanged += TxtPesoManual_TextChanged;
            TxtPesoManual.KeyDown += TxtPesoManual_KeyDown;
            Grid.SetColumn(TxtPesoManual, 1);
            pesoGrid.Children.Add(TxtPesoManual);

            var unidadLabel = new TextBlock
            {
                Text = _producto?.UnidadMedida ?? "kg",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
            };
            Grid.SetColumn(unidadLabel, 2);
            pesoGrid.Children.Add(unidadLabel);

            PanelCantidad = new StackPanel();
            PanelCantidad.Children.Add(pesoGrid);
            pesoPanel.Child = PanelCantidad;
            mainGrid.Children.Add(pesoPanel);

            // ✅ NUEVO: Info de conversión en tiempo real  
            var conversionBorder = CreateInfoSection("🔄 Conversión Automática", 7);
            TxtConversionInfo = new TextBlock
            {
                Text = "💡 Seleccione modo de ingreso arriba",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Padding = new Thickness(15, 10, 15, 10),  // ✅ CORRECTO (left, top, right, bottom)
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            conversionBorder.Child = TxtConversionInfo;
            mainGrid.Children.Add(conversionBorder);

            // ✅ AJUSTADO: Cálculos en fila 8
            var calculosPanel = CreateInfoSection("💰 Cálculos", 8);
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

            // ✅ AJUSTADO: Status en fila 10
            TxtStatus = new TextBlock
            {
                Text = $"💡 Ingrese la cantidad de {tipoUnidad.ToLower()} del producto o use la báscula",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 15, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(TxtStatus, 10);
            mainGrid.Children.Add(TxtStatus);

            // ✅ AJUSTADO: Botones en fila 11
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

            Grid.SetRow(botonesPanel, 11);
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

        private RadioButton RbPorCantidad;
        private RadioButton RbPorDinero;
        private TextBox TxtDineroIngresado;
        private TextBlock TxtConversionInfo;
        private StackPanel PanelCantidad;
        private StackPanel PanelDinero;
        private TextBlock LabelDinamico; // Para cambiar "Peso:" / "Dinero:"

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

        // ✅ AGREGAR en IngresarPesoWindow.cs (después de otros métodos privados, línea ~280)
        private string ObtenerTipoUnidad(string unidadMedida)
        {
            if (string.IsNullOrEmpty(unidadMedida)) return "Cantidad";

            var unidad = unidadMedida.ToLower().Trim();

            if (unidad.Contains("kg") || unidad.Contains("gr") || unidad.Contains("gramos") ||
                unidad.Contains("kilogramos") || unidad.Contains("lb") || unidad.Contains("libras"))
                return "Peso";

            if (unidad.Contains("litros") || unidad.Contains("lt") || unidad.Contains("l") ||
                unidad.Contains("ml") || unidad.Contains("mililitros") || unidad.Contains("galones"))
                return "Volumen";

            if (unidad.Contains("metros") || unidad.Contains("mts") || unidad.Contains("m") ||
                unidad.Contains("cm") || unidad.Contains("centímetros"))
                return "Longitud";

            return "Cantidad";
        }

        private string ObtenerEtiquetaUnidad()
        {
            var tipo = ObtenerTipoUnidad(_producto.UnidadMedida);
            return tipo switch
            {
                "Peso" => "Peso:",
                "Volumen" => "Volumen:",
                "Longitud" => "Longitud:",
                _ => "Cantidad:"
            };
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