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
using System.Windows.Threading;

namespace costbenefi.Views
{
    public partial class IngresarPesoWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly RawMaterial _producto;
        private readonly BasculaService _basculaService;

        // ✅ Timer para lectura continua
        private DispatcherTimer _timerLecturaContinua;
        private DateTime _ultimaLectura = DateTime.MinValue;
        private decimal _ultimoPesoEstable = 0;
        private bool _lecturaAutomaticaActiva = false;

        public decimal PesoIngresado { get; private set; } = 0;

        public IngresarPesoWindow(AppDbContext context, RawMaterial producto, BasculaService basculaService)
        {
            _context = context;
            _producto = producto;
            _basculaService = basculaService;

            InitializeComponent();
            ConfigurarVentana();
            ConfigurarEventos();

            // ✅ Iniciar lectura automática
            IniciarLecturaAutomatica();
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
                RbPorCantidad.IsChecked = true;
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

            // ✅ Configurar vista previa inicial
            if (TxtVistaPrevia != null)
            {
                TxtVistaPrevia.Text = "🔄 Esperando lectura de báscula...";
                TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            }

            if (TxtConversionInfo != null)
            {
                TxtConversionInfo.Text = "💡 Seleccione modo de ingreso arriba";
            }

            System.Diagnostics.Debug.WriteLine($"✅ Ventana configurada para {ObtenerTipoUnidad(_producto.UnidadMedida)}: {_producto.UnidadMedida}");
        }

        private void ConfigurarEventos()
        {
            // ✅ CRÍTICO: COMENTAR PesoRecibido PARA EVITAR DUPLICACIÓN
            // _basculaService.PesoRecibido += BasculaService_PesoRecibido;

            _basculaService.ErrorOcurrido += BasculaService_ErrorOcurrido;
            _basculaService.DatosRecibidos += BasculaService_DatosRecibidos;

            // Configurar eventos de radio buttons
            if (RbPorCantidad != null)
            {
                RbPorCantidad.Checked += RbPorCantidad_Checked;
            }
            if (RbPorDinero != null)
            {
                RbPorDinero.Checked += RbPorDinero_Checked;
            }
            System.Diagnostics.Debug.WriteLine("✅ Eventos configurados (SIN PesoRecibido para evitar duplicación)");
        }

        // ✅ Método para iniciar lectura automática
        private void IniciarLecturaAutomatica()
        {
            try
            {
                if (!_basculaService.EstaConectada)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Báscula no conectada - No se puede iniciar lectura automática");
                    TxtStatus.Text = "⚠️ Báscula no conectada - Use ingreso manual";
                    return;
                }

                // ✅ Configurar timer para lectura continua cada 1 segundo (ajustado de 500ms)
                _timerLecturaContinua = new DispatcherTimer();
                _timerLecturaContinua.Interval = TimeSpan.FromMilliseconds(1000);
                _timerLecturaContinua.Tick += async (s, e) => await LeerPesoAutomatico();

                _lecturaAutomaticaActiva = true;
                _timerLecturaContinua.Start();

                // ✅ Actualizar UI para indicar lectura automática
                BtnLecturaAutomatica.Content = "🔴 LECTURA ACTIVA";
                BtnLecturaAutomatica.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));

                TxtStatus.Text = "📡 Lectura automática activada - Vista previa en tiempo real";

                System.Diagnostics.Debug.WriteLine("✅ Lectura automática iniciada (cada 1 segundo)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error iniciando lectura automática: {ex.Message}");
                TxtStatus.Text = "❌ Error en lectura automática - Use botones manuales";
            }
        }

        // ✅ Método para lectura automática
        private async Task LeerPesoAutomatico()
        {
            try
            {
                if (!_lecturaAutomaticaActiva || !_basculaService.EstaConectada)
                    return;

                System.Diagnostics.Debug.WriteLine($"📡 === LECTURA AUTOMÁTICA {DateTime.Now:HH:mm:ss.fff} ===");

                var peso = await _basculaService.LeerPesoAsync();

                System.Diagnostics.Debug.WriteLine($"   📊 Peso recibido: {peso:F3} kg");

                // ✅ VALIDACIÓN ROBUSTA
                if (peso <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("   ⚠️ Peso cero o inválido - ignorando");
                    return;
                }

                if (peso > _producto.StockTotal * 10)
                {
                    System.Diagnostics.Debug.WriteLine($"   ⚠️ Peso muy alto ignorado: {peso:F3}");

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TxtStatus.Text = $"⚠️ Lectura anormal: {peso:F3} (verificar báscula)";
                    }));
                    return;
                }

                // ✅ Solo actualizar si cambió significativamente (10g mínimo)
                if (Math.Abs(peso - _ultimoPesoEstable) >= 0.010m)
                {
                    System.Diagnostics.Debug.WriteLine($"   ✅ Cambio significativo: {_ultimoPesoEstable:F3} → {peso:F3}");

                    _ultimoPesoEstable = peso;
                    _ultimaLectura = DateTime.Now;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ActualizarVistaPreviaBascula(peso);
                    }));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"   ⏭️ Cambio insignificante - ignorando");
                }

                System.Diagnostics.Debug.WriteLine($"📡 === FIN LECTURA AUTOMÁTICA ===\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en lectura automática: {ex.Message}");
            }
        }

        // ✅ Actualizar vista previa con datos de báscula
        private void ActualizarVistaPreviaBascula(decimal peso)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🖼️ === ACTUALIZANDO VISTA PREVIA ===");
                System.Diagnostics.Debug.WriteLine($"   ⚖️ Peso a mostrar: {peso:F3} kg");

                // ✅ Actualizar peso en display
                TxtPesoBascula.Text = $"{peso:F3}";

                // ✅ Solo actualizar campo manual si no está siendo editado por el usuario
                if (!TxtPesoManual.IsFocused && !TxtPesoManual.IsKeyboardFocused)
                {
                    TxtPesoManual.Text = $"{peso:F3}";
                    System.Diagnostics.Debug.WriteLine($"   ✅ Campo manual actualizado: {peso:F3}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"   ⏭️ Campo manual tiene foco - no actualizar");
                }

                // ✅ Calcular y mostrar vista previa
                if (peso > 0)
                {
                    var total = peso * _producto.PrecioVentaFinal;
                    var ganancia = peso * (_producto.PrecioVentaFinal - _producto.PrecioConIVA);

                    // ✅ Actualizar vista previa prominente
                    TxtVistaPrevia.Text = $"📊 VISTA PREVIA: {peso:F3} {_producto.UnidadMedida} = {total:C2}";

                    // ✅ Cambiar color según si es válido
                    if (peso <= _producto.StockTotal)
                    {
                        TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Verde
                        BtnConfirmar.IsEnabled = true;
                    }
                    else
                    {
                        TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Rojo
                        TxtVistaPrevia.Text += " ⚠️ EXCEDE STOCK";
                        BtnConfirmar.IsEnabled = false;
                    }

                    // ✅ Actualizar cálculos detallados
                    ActualizarCalculos();

                    // ✅ Mostrar timestamp de última lectura
                    TxtUltimaLectura.Text = $"🕐 {DateTime.Now:HH:mm:ss}";

                    System.Diagnostics.Debug.WriteLine($"   💰 Dinero calculado: {total:C2}");
                    System.Diagnostics.Debug.WriteLine($"   💵 Ganancia: {ganancia:C2}");
                }
                else
                {
                    TxtVistaPrevia.Text = "⚖️ Coloque el producto en la báscula";
                    TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                    BtnConfirmar.IsEnabled = false;
                }

                System.Diagnostics.Debug.WriteLine($"🖼️ === FIN ACTUALIZACIÓN ===\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando vista previa: {ex.Message}");
            }
        }

        // ✅ Botón de Ingreso Rápido Manual
        private void BtnIngresoRapido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new IngresoRapidoDialog(_producto);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    var pesoIngresado = dialog.PesoIngresado;

                    if (pesoIngresado > _producto.StockTotal)
                    {
                        MessageBox.Show($"La cantidad ingresada ({pesoIngresado:F3}) excede el stock disponible ({_producto.StockTotal:F3}).",
                                      "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var total = pesoIngresado * _producto.PrecioVentaFinal;
                    var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);

                    var mensaje = $"✅ INGRESO RÁPIDO\n\n" +
                                 $"📦 Producto: {_producto.NombreArticulo}\n" +
                                 $"📏 {tipoUnidad}: {pesoIngresado:F3} {_producto.UnidadMedida}\n" +
                                 $"💰 Total: {total:C2}\n\n" +
                                 $"¿Agregar al carrito?";

                    var resultado = MessageBox.Show(mensaje, "Confirmar Ingreso Rápido",
                                                  MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        _lecturaAutomaticaActiva = false;
                        _timerLecturaContinua?.Stop();

                        PesoIngresado = pesoIngresado;
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en ingreso rápido: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ Botón para controlar lectura automática
        private void BtnLecturaAutomatica_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_lecturaAutomaticaActiva)
                {
                    _lecturaAutomaticaActiva = false;
                    _timerLecturaContinua?.Stop();

                    BtnLecturaAutomatica.Content = "▶️ ACTIVAR LECTURA";
                    BtnLecturaAutomatica.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));

                    TxtStatus.Text = "⏸️ Lectura automática pausada";
                    TxtVistaPrevia.Text = "⏸️ Lectura automática desactivada";
                    TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                }
                else
                {
                    IniciarLecturaAutomatica();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error controlando lectura automática: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarEstadoBascula()
        {
            if (_basculaService.EstaConectada)
            {
                TxtEstadoBascula.Text = "✅ Conectada";
                TxtEstadoBascula.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                BtnLeerBascula.IsEnabled = true;
                BtnTarar.IsEnabled = true;
                BtnLecturaAutomatica.IsEnabled = true;
            }
            else
            {
                TxtEstadoBascula.Text = "❌ Desconectada";
                TxtEstadoBascula.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                BtnLeerBascula.IsEnabled = false;
                BtnTarar.IsEnabled = false;
                BtnLecturaAutomatica.IsEnabled = false;
            }

            if (_basculaService.ConfiguracionActual != null)
            {
                TxtNombreBascula.Text = _basculaService.ConfiguracionActual.Nombre ?? "Sin configurar";
                TxtPuertoBascula.Text = _basculaService.ConfiguracionActual.Puerto ?? "N/A";
            }
            else
            {
                TxtNombreBascula.Text = "Sin configurar";
                TxtPuertoBascula.Text = "N/A";
            }
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
                    ActualizarVistaPreviaBascula(peso);
                    TxtStatus.Text = $"✅ Peso leído manualmente: {peso:F3} {_producto.UnidadMedida}";
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
                BtnLeerBascula.Content = "📖 Leer Manual";
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
                    TxtVistaPrevia.Text = "✅ Báscula tarada - Coloque el producto";
                    TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
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
            if (TxtPesoManual.IsFocused)
            {
                ActualizarCalculos();
            }
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
                    if (decimal.TryParse(TxtDineroIngresado?.Text.Replace(",", "."),
                     NumberStyles.Number,
                     CultureInfo.InvariantCulture,
                     out decimal dineroIngresado) && dineroIngresado > 0)
                    {
                        if (_producto.PrecioVentaFinal > 0)
                        {
                            cantidadFinal = dineroIngresado / _producto.PrecioVentaFinal;
                            dineroFinal = dineroIngresado;

                            if (TxtPesoManual != null && !TxtPesoManual.IsFocused)
                            {
                                TxtPesoManual.Text = cantidadFinal.ToString("F3");
                            }

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
                    if (decimal.TryParse(TxtPesoManual?.Text.Replace(",", "."),
                     NumberStyles.Number,
                     CultureInfo.InvariantCulture,
                     out decimal cantidad) && cantidad > 0)
                    {
                        cantidadFinal = cantidad;
                        dineroFinal = cantidad * _producto.PrecioVentaFinal;

                        if (TxtDineroIngresado != null && !TxtDineroIngresado.IsFocused)
                        {
                            TxtDineroIngresado.Text = dineroFinal.ToString("F2");
                        }

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

                if (cantidadFinal > _producto.StockTotal)
                {
                    TxtStatus.Text = $"⚠️ Cantidad excede el stock disponible ({_producto.StockTotal:F3})";
                    TxtSubtotal.Text = "$0.00";
                    TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    BtnConfirmar.IsEnabled = false;
                    return;
                }

                var subtotal = cantidadFinal * _producto.PrecioVentaFinal;
                TxtSubtotal.Text = subtotal.ToString("C2");
                TxtSubtotal.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));

                var ganancia = cantidadFinal * (_producto.PrecioVentaFinal - _producto.PrecioConIVA);
                TxtGanancia.Text = $"Ganancia: {ganancia:C2}";

                BtnConfirmar.IsEnabled = true;

                var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);

                if (!_lecturaAutomaticaActiva)
                {
                    TxtStatus.Text = $"✅ {tipoUnidad} válido: {cantidadFinal:F3} {_producto.UnidadMedida} = ${subtotal:F2}";
                }
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

            if (!string.IsNullOrEmpty(mensaje) && !_lecturaAutomaticaActiva)
            {
                TxtStatus.Text = $"⚠️ {mensaje}";
            }
        }

        private void RbPorCantidad_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PanelCantidad != null && PanelDinero != null)
                {
                    PanelCantidad.Visibility = Visibility.Visible;
                    PanelDinero.Visibility = Visibility.Collapsed;

                    if (LabelDinamico != null)
                    {
                        LabelDinamico.Text = ObtenerEtiquetaUnidad();
                    }

                    TxtPesoManual?.Focus();

                    if (!_lecturaAutomaticaActiva)
                    {
                        TxtStatus.Text = $"📏 Modo por cantidad - Ingrese {_producto.UnidadMedida}";
                    }
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
                    PanelCantidad.Visibility = Visibility.Collapsed;
                    PanelDinero.Visibility = Visibility.Visible;

                    if (LabelDinamico != null)
                    {
                        LabelDinamico.Text = "Dinero:";
                    }

                    TxtDineroIngresado?.Focus();

                    if (!_lecturaAutomaticaActiva)
                    {
                        TxtStatus.Text = "💰 Modo por dinero - Ingrese cantidad en pesos";
                    }
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
            if (RbPorDinero?.IsChecked == true && TxtDineroIngresado.IsFocused)
            {
                ActualizarCalculos();
            }
        }

        private void TxtDineroIngresado_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnConfirmar.IsEnabled)
            {
                BtnConfirmar_Click(sender, new RoutedEventArgs());
                return;
            }

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

                if (cantidadFinal > _producto.StockTotal)
                {
                    MessageBox.Show($"La cantidad ingresada ({cantidadFinal:F3}) excede el stock disponible ({_producto.StockTotal:F3}).",
                                  "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var tipoConfirmacion = ObtenerTipoUnidad(_producto.UnidadMedida);
                var dineroTotal = cantidadFinal * _producto.PrecioVentaFinal;
                var modoUsado = esModoDinero ? "💰 por dinero" : "⚖️ por cantidad";
                var origenDatos = _lecturaAutomaticaActiva ? "📡 báscula automática" : "✏️ ingreso manual";

                var mensaje = $"✅ AGREGAR AL CARRITO ({modoUsado})\n\n" +
                             $"📦 Producto: {_producto.NombreArticulo}\n" +
                             $"📏 {tipoConfirmacion}: {cantidadFinal:F3} {_producto.UnidadMedida}\n" +
                             $"💰 Total: ${dineroTotal:F2}\n" +
                             $"📊 Origen: {origenDatos}\n\n" +
                             $"¿Agregar este producto al carrito?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Agregar al Carrito",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _lecturaAutomaticaActiva = false;
                    _timerLecturaContinua?.Stop();

                    PesoIngresado = cantidadFinal;
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
            _lecturaAutomaticaActiva = false;
            _timerLecturaContinua?.Stop();

            DialogResult = false;
            Close();
        }

        private void TxtPesoManual_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnConfirmar.IsEnabled)
            {
                BtnConfirmar_Click(sender, new RoutedEventArgs());
                return;
            }

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

        // ❌ MÉTODO COMENTADO PARA EVITAR DUPLICACIÓN
        /*
        private void BasculaService_PesoRecibido(object sender, PesoRecibidoEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ActualizarVistaPreviaBascula(e.Peso);
                System.Diagnostics.Debug.WriteLine($"📡 Peso recibido automáticamente: {e.Peso:F3}");
            }));
        }
        */

        private void BasculaService_ErrorOcurrido(object sender, string error)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtStatus.Text = $"❌ Error báscula: {error}";
                TxtVistaPrevia.Text = $"❌ Error: {error}";
                TxtVistaPrevia.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            }));
        }

        private void BasculaService_DatosRecibidos(object sender, string datos)
        {
            System.Diagnostics.Debug.WriteLine($"📥 Datos báscula: {datos}");
        }

        protected override void OnClosed(EventArgs e)
        {
            _lecturaAutomaticaActiva = false;
            _timerLecturaContinua?.Stop();
            _timerLecturaContinua = null;

            // ✅ Desuscribir SOLO los eventos que fueron suscritos
            // _basculaService.PesoRecibido -= BasculaService_PesoRecibido; // ❌ Ya comentado arriba
            _basculaService.ErrorOcurrido -= BasculaService_ErrorOcurrido;
            _basculaService.DatosRecibidos -= BasculaService_DatosRecibidos;

            base.OnClosed(e);
        }

        // ===== INICIALIZACIÓN DE UI =====
        private void InitializeComponent()
        {
            var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);
            Title = $"⚖️ Ingresar {tipoUnidad} - Vista Previa en Tiempo Real";
            Width = 700;
            Height = 750;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20);

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0 - Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1 - Info producto
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2 - Estado báscula
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3 - Controles báscula
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4 - Vista previa
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5 - Selector modo
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 6 - Panel dinero
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 7 - Peso manual
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 8 - Conversión info
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 9 - Cálculos
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 10 - Spacer
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 11 - Status
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 12 - Botón cancelar

            // Header
            var header = new TextBlock
            {
                Text = $"⚖️ {tipoUnidad} con Vista Previa en Tiempo Real",
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
            TxtPesoBascula = CreateInfoLabel("Peso actual:", "0.000", 1, 1, basculaGrid);

            basculaBorder.Child = basculaGrid;
            mainGrid.Children.Add(basculaBorder);

            // Controles báscula + Botón Agregar
            var controlesCompletosGrid = new Grid();
            controlesCompletosGrid.RowDefinitions.Add(new RowDefinition());
            controlesCompletosGrid.RowDefinitions.Add(new RowDefinition());
            controlesCompletosGrid.Margin = new Thickness(0, 10, 0, 20);

            var controlesBasculaPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            BtnLeerBascula = CreateButton("📖 Leer Manual", Color.FromRgb(34, 197, 94));
            BtnLeerBascula.Click += BtnLeerBascula_Click;

            BtnTarar = CreateButton("⚖️ Tarar", Color.FromRgb(249, 115, 22));
            BtnTarar.Click += BtnTarar_Click;

            BtnLecturaAutomatica = CreateButton("▶️ ACTIVAR LECTURA", Color.FromRgb(34, 197, 94));
            BtnLecturaAutomatica.Click += BtnLecturaAutomatica_Click;
            BtnLecturaAutomatica.Width = 150;

            BtnIngresoRapido = CreateButton("📝 INGRESO RÁPIDO", Color.FromRgb(138, 43, 226));
            BtnIngresoRapido.Click += BtnIngresoRapido_Click;
            BtnIngresoRapido.Width = 150;

            controlesBasculaPanel.Children.Add(BtnLeerBascula);
            controlesBasculaPanel.Children.Add(BtnTarar);
            controlesBasculaPanel.Children.Add(BtnLecturaAutomatica);
            controlesBasculaPanel.Children.Add(BtnIngresoRapido);

            Grid.SetRow(controlesBasculaPanel, 0);
            controlesCompletosGrid.Children.Add(controlesBasculaPanel);

            var accionPrincipalPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            BtnConfirmar = new Button
            {
                Content = "🛒 AGREGAR AL CARRITO",
                Width = 200,
                Height = 45,
                Background = new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                IsEnabled = false
            };
            BtnConfirmar.Click += BtnConfirmar_Click;

            accionPrincipalPanel.Children.Add(BtnConfirmar);

            Grid.SetRow(accionPrincipalPanel, 1);
            controlesCompletosGrid.Children.Add(accionPrincipalPanel);

            Grid.SetRow(controlesCompletosGrid, 3);
            mainGrid.Children.Add(controlesCompletosGrid);

            // Vista previa
            var vistaPreviaBorder = CreateInfoSection("📊 Vista Previa en Tiempo Real", 4);
            var vistaGrid = new Grid();
            vistaGrid.RowDefinitions.Add(new RowDefinition());
            vistaGrid.RowDefinitions.Add(new RowDefinition());

            TxtVistaPrevia = new TextBlock
            {
                Text = "🔄 Esperando lectura de báscula...",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Padding = new Thickness(20, 15, 20, 10),
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(TxtVistaPrevia, 0);
            vistaGrid.Children.Add(TxtVistaPrevia);

            TxtUltimaLectura = new TextBlock
            {
                Text = "🕐 Sin lecturas",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(TxtUltimaLectura, 1);
            vistaGrid.Children.Add(TxtUltimaLectura);

            vistaPreviaBorder.Child = vistaGrid;
            mainGrid.Children.Add(vistaPreviaBorder);

            // Selector modo
            var selectorBorder = CreateInfoSection("🎯 Modo de Ingreso", 5);
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

            // Panel dinero
            var dineroPanel = CreateInfoSection("💰 Ingreso por Dinero", 6);
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
                Background = new SolidColorBrush(Color.FromRgb(254, 249, 195)),
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

            // Peso manual
            var pesoPanel = CreateInfoSection($"✏️ {tipoUnidad} Manual", 7);
            var pesoGrid = new Grid();
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pesoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
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

            // Conversión
            var conversionBorder = CreateInfoSection("🔄 Conversión Automática", 8);
            TxtConversionInfo = new TextBlock
            {
                Text = "💡 Seleccione modo de ingreso arriba",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Padding = new Thickness(15, 10, 15, 10),
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            conversionBorder.Child = TxtConversionInfo;
            mainGrid.Children.Add(conversionBorder);

            // Cálculos
            var calculosPanel = CreateInfoSection("💰 Cálculos", 9);
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
                Text = $"💡 Activando lectura automática...",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 15, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(TxtStatus, 11);
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

            botonesPanel.Children.Add(btnCancelar);

            Grid.SetRow(botonesPanel, 12);
            mainGrid.Children.Add(botonesPanel);

            Content = mainGrid;
        }

        // ===== CAMPOS DE CONTROLES =====
        private TextBlock TxtVistaPrevia;
        private TextBlock TxtUltimaLectura;
        private Button BtnLecturaAutomatica;
        private Button BtnIngresoRapido;
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
        private TextBlock LabelDinamico;

        // ===== MÉTODOS DE APOYO =====

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

    // ===== DIALOG INGRESO RÁPIDO =====
    public class IngresoRapidoDialog : Window
    {
        private readonly RawMaterial _producto;
        private TextBox TxtPesoRapido;
        private TextBlock TxtTotalRapido;
        private Button BtnConfirmarRapido;

        public decimal PesoIngresado { get; private set; } = 0;

        public IngresoRapidoDialog(RawMaterial producto)
        {
            _producto = producto;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var tipoUnidad = ObtenerTipoUnidad(_producto.UnidadMedida);

            Title = $"📝 Ingreso Rápido - {_producto.NombreArticulo}";
            Width = 500;
            Height = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(40);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Info producto
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input peso
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Total
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Spacer
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones

            // Header
            var header = new TextBlock
            {
                Text = $"⚡ Ingreso Rápido de {tipoUnidad}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 25),
                Foreground = new SolidColorBrush(Color.FromRgb(138, 43, 226))
            };
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Info producto
            var infoStack = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 25)
            };

            var nombreProducto = new TextBlock
            {
                Text = _producto.NombreArticulo,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39))
            };

            var stockDisponible = new TextBlock
            {
                Text = $"Stock: {_producto.StockTotal:F3} {_producto.UnidadMedida}",
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var precio = new TextBlock
            {
                Text = $"Precio: {_producto.PrecioVentaFinal:C2} por {_producto.UnidadMedida}",
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };

            infoStack.Children.Add(nombreProducto);
            infoStack.Children.Add(stockDisponible);
            infoStack.Children.Add(precio);

            Grid.SetRow(infoStack, 1);
            mainGrid.Children.Add(infoStack);

            // Input peso
            var pesoPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 25)
            };

            var pesoLabel = new TextBlock
            {
                Text = $"Ingrese {tipoUnidad.ToLower()} ({_producto.UnidadMedida}):",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
            };

            TxtPesoRapido = new TextBox
            {
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(20),
                Text = "",
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(138, 43, 226)),
                BorderThickness = new Thickness(3),
                Height = 70
            };
            TxtPesoRapido.TextChanged += TxtPesoRapido_TextChanged;
            TxtPesoRapido.KeyDown += TxtPesoRapido_KeyDown;

            pesoPanel.Children.Add(pesoLabel);
            pesoPanel.Children.Add(TxtPesoRapido);

            Grid.SetRow(pesoPanel, 2);
            mainGrid.Children.Add(pesoPanel);

            // Total
            TxtTotalRapido = new TextBlock
            {
                Text = "Total: $0.00",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                Margin = new Thickness(0, 0, 0, 25)
            };
            Grid.SetRow(TxtTotalRapido, 3);
            mainGrid.Children.Add(TxtTotalRapido);

            // Botones
            var botonesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnCancelar = new Button
            {
                Content = "❌ Cancelar",
                Width = 120,
                Height = 40,
                Margin = new Thickness(0, 0, 20, 0),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            btnCancelar.Click += (s, e) => { DialogResult = false; Close(); };

            BtnConfirmarRapido = new Button
            {
                Content = "✅ Agregar",
                Width = 120,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(138, 43, 226)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                IsEnabled = false
            };
            BtnConfirmarRapido.Click += BtnConfirmarRapido_Click;

            botonesPanel.Children.Add(btnCancelar);
            botonesPanel.Children.Add(BtnConfirmarRapido);

            Grid.SetRow(botonesPanel, 5);
            mainGrid.Children.Add(botonesPanel);

            Content = mainGrid;

            TxtPesoRapido.Focus();
        }

        private void TxtPesoRapido_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(TxtPesoRapido.Text.Replace(",", "."),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal peso) && peso > 0)
                {
                    var total = peso * _producto.PrecioVentaFinal;
                    TxtTotalRapido.Text = $"Total: {total:C2}";

                    if (peso <= _producto.StockTotal)
                    {
                        TxtTotalRapido.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                        BtnConfirmarRapido.IsEnabled = true;
                    }
                    else
                    {
                        TxtTotalRapido.Text += " ⚠️ EXCEDE STOCK";
                        TxtTotalRapido.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                        BtnConfirmarRapido.IsEnabled = false;
                    }
                }
                else
                {
                    TxtTotalRapido.Text = "Total: $0.00";
                    TxtTotalRapido.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                    BtnConfirmarRapido.IsEnabled = false;
                }
            }
            catch
            {
                TxtTotalRapido.Text = "Total: $0.00";
                BtnConfirmarRapido.IsEnabled = false;
            }
        }

        private void TxtPesoRapido_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && BtnConfirmarRapido.IsEnabled)
            {
                BtnConfirmarRapido_Click(sender, new RoutedEventArgs());
                return;
            }

            if (!IsValidKey(e.Key))
            {
                e.Handled = true;
            }
        }

        private void BtnConfirmarRapido_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(TxtPesoRapido.Text.Replace(",", "."),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal peso) && peso > 0)
                {
                    PesoIngresado = peso;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ingrese una cantidad válida mayor a cero.",
                                  "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPesoRapido.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}