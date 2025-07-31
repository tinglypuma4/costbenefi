using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class CrearEditarPromocionWindow : Window
    {
        private AppDbContext _context;
        private PromocionVenta _promocionActual;
        private string _tipoPromocionSeleccionado = "";
        private bool _esEdicion = false;
        private bool _controlesInicializados = false;

        // Datos para análisis de viabilidad
        private List<RawMaterial> _productosDisponibles = new();
        private List<ServicioVenta> _serviciosDisponibles = new();

        // ✅ NUEVO: Variables para productos a granel
        private List<RawMaterial> _productosAGranel = new();
        private RawMaterial _productoSeleccionado = null;

        /// <summary>
        /// Constructor para crear nueva promoción
        /// </summary>
        public CrearEditarPromocionWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _promocionActual = new PromocionVenta();
            _esEdicion = false;
            _controlesInicializados = true;

            InitializeAsync();
        }

        /// <summary>
        /// Constructor para editar promoción existente
        /// </summary>
        public CrearEditarPromocionWindow(PromocionVenta promocion)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _promocionActual = promocion;
            _esEdicion = true;
            _controlesInicializados = true;

            TxtTituloVentana.Text = "🎁 Editar Promoción";
            this.Title = "🎁 Editar Promoción";

            InitializeAsync();
        }

        // ✅ MODIFICADO: InitializeAsync() - agregar carga de productos a granel
        private async void InitializeAsync()
        {
            try
            {
                TxtEstadoFormulario.Text = "⏳ Cargando formulario...";

                // Cargar datos para análisis
                await CargarDatosParaAnalisis();

                // ✅ NUEVO: Cargar productos a granel
                await CargarProductosAGranel();

                // Configurar fechas por defecto
                DpFechaInicio.SelectedDate = DateTime.Today;
                DpFechaFin.SelectedDate = DateTime.Today.AddDays(30);

                // Si es edición, cargar datos
                if (_esEdicion)
                {
                    await CargarDatosPromocion();
                }

                TxtEstadoFormulario.Text = "🎁 Listo para crear promoción";
            }
            catch (Exception ex)
            {
                TxtEstadoFormulario.Text = "❌ Error al cargar formulario";
                MessageBox.Show($"Error al inicializar formulario:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Carga de Datos

        private async System.Threading.Tasks.Task CargarDatosParaAnalisis()
        {
            try
            {
                // Cargar productos disponibles para análisis de viabilidad
                _productosDisponibles = await _context.RawMaterials
                    .Where(p => !p.Eliminado && p.ActivoParaVenta)
                    .ToListAsync();

                // Cargar servicios disponibles
                _serviciosDisponibles = await _context.ServiciosVenta
                    .Where(s => !s.Eliminado && s.Activo)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"✅ Datos cargados: {_productosDisponibles.Count} productos, {_serviciosDisponibles.Count} servicios");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NUEVO: Carga productos a granel en el ComboBox
        /// </summary>
        private async System.Threading.Tasks.Task CargarProductosAGranel()
        {
            try
            {
                _productosAGranel = await _context.GetProductosAGranel().ToListAsync();

                CmbProductoAGranel.ItemsSource = _productosAGranel;

                System.Diagnostics.Debug.WriteLine($"✅ Productos a granel cargados: {_productosAGranel.Count}");

                if (!_productosAGranel.Any())
                {
                    TxtEstadoFormulario.Text = "⚠️ No hay productos a granel disponibles";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando productos a granel: {ex.Message}");
                _productosAGranel = new List<RawMaterial>();
            }
        }

        private async System.Threading.Tasks.Task CargarDatosPromocion()
        {
            try
            {
                // Cargar información básica
                TxtNombrePromocion.Text = _promocionActual.NombrePromocion;
                TxtDescripcion.Text = _promocionActual.Descripcion;
                CmbCategoriaPromocion.Text = _promocionActual.CategoriaPromocion;

                // Cargar fechas
                DpFechaInicio.SelectedDate = _promocionActual.FechaInicio;
                DpFechaFin.SelectedDate = _promocionActual.FechaFin;

                // Cargar horarios
                TxtHoraInicio.Text = _promocionActual.HoraInicio;
                TxtHoraFin.Text = _promocionActual.HoraFin;

                // Cargar límites
                TxtLimiteUsoTotal.Text = _promocionActual.LimiteUsoTotal.ToString();
                TxtLimitePorCliente.Text = _promocionActual.LimitePorCliente.ToString();
                TxtMontoMinimo.Text = _promocionActual.MontoMinimo.ToString("F2");

                // Cargar checkboxes
                ChkActivaPromocion.IsChecked = _promocionActual.Activa;
                ChkAplicacionAutomatica.IsChecked = _promocionActual.AplicacionAutomatica;
                ChkIntegrarPOS.IsChecked = _promocionActual.IntegradaPOS;
                ChkCombinable.IsChecked = _promocionActual.Combinable;

                // Seleccionar tipo de promoción
                _tipoPromocionSeleccionado = _promocionActual.TipoPromocion;
                SeleccionarTipoPromocion(_tipoPromocionSeleccionado);
                CargarValoresEspecificosTipo();

                System.Diagnostics.Debug.WriteLine($"✅ Promoción cargada: {_promocionActual.NombrePromocion}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando promoción: {ex.Message}");
                MessageBox.Show($"Error al cargar datos de la promoción:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ✅ MODIFICADO: CargarValoresEspecificosTipo para incluir producto
        private void CargarValoresEspecificosTipo()
        {
            switch (_tipoPromocionSeleccionado)
            {
                case "DescuentoPorcentaje":
                    TxtValorPorcentaje.Text = _promocionActual.ValorPromocion.ToString("F1");
                    TxtDescuentoMaximo.Text = _promocionActual.DescuentoMaximo.ToString("F2");
                    break;

                case "DescuentoFijo":
                    TxtValorFijo.Text = _promocionActual.ValorPromocion.ToString("F2");
                    break;

                case "Cantidad":
                    TxtCantidadMinima.Text = _promocionActual.CantidadMinima.ToString();
                    TxtPrecioTotal.Text = _promocionActual.ValorPromocion.ToString("F2");

                    // ✅ NUEVO: Cargar producto específico si existe
                    if (!string.IsNullOrEmpty(_promocionActual.ProductosAplicables))
                    {
                        var productIds = _promocionActual.ProductosAplicables.Split(',')
                            .Select(p => p.Trim())
                            .Where(p => int.TryParse(p, out _))
                            .Select(p => int.Parse(p))
                            .ToList();

                        if (productIds.Any())
                        {
                            var producto = _productosAGranel.FirstOrDefault(p => productIds.Contains(p.Id));
                            if (producto != null)
                            {
                                CmbProductoAGranel.SelectedItem = producto;
                            }
                        }
                    }
                    break;

                case "CompraYLleva":
                    TxtCompra.Text = _promocionActual.CantidadMinima.ToString();
                    TxtLleva.Text = _promocionActual.ValorPromocion.ToString("F0");
                    break;
            }
        }

        #endregion

        #region Eventos de Selección de Tipo

        private void BtnTipoPromocion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tipoPromocion)
            {
                _tipoPromocionSeleccionado = tipoPromocion;
                SeleccionarTipoPromocion(tipoPromocion);
                ActualizarVistaPrevia();
                AnalizarViabilidad();
            }
        }

        private void SeleccionarTipoPromocion(string tipo)
        {
            // Deseleccionar todos los botones
            DesseleccionarTodosBotonesTipo();

            // Ocultar todos los paneles de configuración
            OcultarTodosPanelesConfiguracion();

            // Seleccionar el botón correspondiente y mostrar panel
            switch (tipo)
            {
                case "DescuentoPorcentaje":
                    BtnDescuentoPorcentaje.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                    BtnDescuentoPorcentaje.Foreground = Brushes.White;
                    PanelDescuentoPorcentaje.Visibility = Visibility.Visible;
                    MostrarDescripcionTipo("💰 Descuento Porcentual",
                        "Aplica un porcentaje de descuento sobre el total de la compra.\n" +
                        "Ejemplo: 20% de descuento en toda la compra.\n" +
                        "Ideal para promociones generales y liquidaciones.");
                    break;

                case "DescuentoFijo":
                    BtnDescuentoFijo.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                    BtnDescuentoFijo.Foreground = Brushes.White;
                    PanelDescuentoFijo.Visibility = Visibility.Visible;
                    MostrarDescripcionTipo("💵 Descuento Fijo",
                        "Aplica una cantidad fija de descuento.\n" +
                        "Ejemplo: $50 de descuento en compras mayores a $500.\n" +
                        "Perfecto para incentivar compras de mayor volumen.");
                    break;

                case "Cantidad":
                    BtnCantidad.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                    BtnCantidad.Foreground = Brushes.White;
                    PanelCantidad.Visibility = Visibility.Visible;
                    MostrarDescripcionTipo("⚖️ Precio por Cantidad",
                        "Establece un precio especial por cantidad específica.\n" +
                        "Ejemplo: 2 kilos de tomate por $80 (en lugar de $50 c/kilo).\n" +
                        "Típico en verdulerías y tiendas de abarrotes.");
                    break;

                case "CompraYLleva":
                    BtnCompraYLleva.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                    BtnCompraYLleva.Foreground = Brushes.White;
                    PanelCompraYLleva.Visibility = Visibility.Visible;
                    MostrarDescripcionTipo("🎯 Compra y Lleva",
                        "El cliente compra una cantidad y se lleva más.\n" +
                        "Ejemplo: Compra 2 y llévate 3 (2x1).\n" +
                        "Excelente para liquidar inventario rápidamente.");
                    break;

                case "Combo":
                    BtnCombo.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                    BtnCombo.Foreground = Brushes.White;
                    MostrarDescripcionTipo("📦 Combo Especial",
                        "Combina productos/servicios a precio especial.\n" +
                        "Ejemplo: Facial + manicure = $800.\n" +
                        "Aumenta el ticket promedio y satisfacción del cliente.");
                    break;

                case "MontoMinimo":
                    BtnMontoMinimo.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                    BtnMontoMinimo.Foreground = Brushes.White;
                    MostrarDescripcionTipo("💳 Descuento por Monto Mínimo",
                        "Aplica descuento cuando se alcanza un monto mínimo.\n" +
                        "Ejemplo: 15% descuento comprando más de $500.\n" +
                        "Incrementa el valor promedio de las ventas.");
                    break;
            }

            TxtEstadoFormulario.Text = $"🎁 Tipo seleccionado: {ObtenerNombreTipo(tipo)}";
        }

        private void DesseleccionarTodosBotonesTipo()
        {
            var botones = new[] { BtnDescuentoPorcentaje, BtnDescuentoFijo, BtnCantidad,
                                 BtnCompraYLleva, BtnCombo, BtnMontoMinimo };

            foreach (var btn in botones)
            {
                btn.Background = Brushes.White;
                btn.Foreground = Brushes.Black;
            }
        }

        private void OcultarTodosPanelesConfiguracion()
        {
            PanelDescuentoPorcentaje.Visibility = Visibility.Collapsed;
            PanelDescuentoFijo.Visibility = Visibility.Collapsed;
            PanelCantidad.Visibility = Visibility.Collapsed;
            PanelCompraYLleva.Visibility = Visibility.Collapsed;
        }

        private void MostrarDescripcionTipo(string titulo, string descripcion)
        {
            TxtTipoSeleccionado.Text = titulo;
            TxtDescripcionTipo.Text = descripcion;
            PanelDescripcionTipo.Visibility = Visibility.Visible;
        }

        private string ObtenerNombreTipo(string tipo)
        {
            return tipo switch
            {
                "DescuentoPorcentaje" => "Descuento Porcentual",
                "DescuentoFijo" => "Descuento Fijo",
                "Cantidad" => "Precio por Cantidad",
                "CompraYLleva" => "Compra y Lleva",
                "Combo" => "Combo Especial",
                "MontoMinimo" => "Descuento por Monto Mínimo",
                _ => tipo
            };
        }

        #endregion

        #region ✅ NUEVOS: Eventos para Productos a Granel

        /// <summary>
        /// ✅ NUEVO: Maneja la selección de producto en el ComboBox
        /// </summary>
        private void CmbProductoAGranel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_controlesInicializados) return;

                _productoSeleccionado = CmbProductoAGranel.SelectedItem as RawMaterial;

                if (_productoSeleccionado != null)
                {
                    // Mostrar información del producto
                    TxtInfoProducto.Text = $"📦 {_productoSeleccionado.NombreArticulo} - Stock: {_productoSeleccionado.StockTotal:F2} {_productoSeleccionado.UnidadMedida}";
                    TxtPrecioNormalProducto.Text = $"Precio normal: {_productoSeleccionado.PrecioVenta:C2} por {_productoSeleccionado.UnidadMedida}";
                    TxtUnidadMedidaProducto.Text = _productoSeleccionado.UnidadMedida;

                    PanelInfoProductoSeleccionado.Visibility = Visibility.Visible;

                    // Actualizar comparación si ya hay valores
                    ActualizarComparacionPrecios();
                    ActualizarVistaPrevia();

                    TxtEstadoFormulario.Text = $"✅ Producto seleccionado: {_productoSeleccionado.NombreArticulo}";
                }
                else
                {
                    PanelInfoProductoSeleccionado.Visibility = Visibility.Collapsed;
                    PanelComparacion.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en selección de producto: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NUEVO: Actualiza la comparación de precios
        /// </summary>
        private void ActualizarComparacionPrecios()
        {
            try
            {
                if (_productoSeleccionado == null ||
                    !decimal.TryParse(TxtCantidadMinima.Text, out decimal cantidad) ||
                    !decimal.TryParse(TxtPrecioTotal.Text, out decimal precioPromo))
                {
                    PanelComparacion.Visibility = Visibility.Collapsed;
                    return;
                }

                if (cantidad <= 0 || precioPromo <= 0)
                {
                    PanelComparacion.Visibility = Visibility.Collapsed;
                    return;
                }

                // Calcular precio normal
                decimal precioNormalTotal = cantidad * _productoSeleccionado.PrecioVenta;
                decimal ahorro = precioNormalTotal - precioPromo;
                decimal porcentajeDescuento = precioNormalTotal > 0 ? (ahorro / precioNormalTotal) * 100 : 0;

                // Actualizar UI
                TxtPrecioNormalTotal.Text = precioNormalTotal.ToString("C2");
                TxtPrecioPromocionalTotal.Text = precioPromo.ToString("C2");
                TxtAhorroTotal.Text = ahorro.ToString("C2");

                if (ahorro > 0)
                {
                    TxtPorcentajeDescuento.Text = $"💰 Descuento: {porcentajeDescuento:F1}% de ahorro";
                    TxtAhorroTotal.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Verde
                }
                else if (ahorro < 0)
                {
                    TxtPorcentajeDescuento.Text = $"⚠️ Precio promocional es MAYOR al normal";
                    TxtAhorroTotal.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Rojo
                }
                else
                {
                    TxtPorcentajeDescuento.Text = "💫 Precio igual al normal";
                    TxtAhorroTotal.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)); // Gris
                }

                PanelComparacion.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en comparación de precios: {ex.Message}");
            }
        }

        #endregion

        #region Vista Previa y Análisis

        // ✅ MODIFICADO: Método TxtValor_TextChanged para incluir comparación
        private void TxtValor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_controlesInicializados) return;

            // Funcionalidad original
            ActualizarVistaPrevia();
            AnalizarViabilidad();

            // ✅ NUEVO: Actualizar comparación para tipo Cantidad
            if (_tipoPromocionSeleccionado == "Cantidad")
            {
                ActualizarComparacionPrecios();
            }
        }

        private void BtnActualizarVista_Click(object sender, RoutedEventArgs e)
        {
            ActualizarVistaPrevia();
            AnalizarViabilidad();
        }

        private void ActualizarVistaPrevia()
        {
            try
            {
                if (string.IsNullOrEmpty(_tipoPromocionSeleccionado))
                {
                    TxtVistaPrevia.Text = "Seleccione un tipo de promoción para ver la vista previa";
                    return;
                }

                string nombre = TxtNombrePromocion.Text.Trim();
                if (string.IsNullOrEmpty(nombre)) nombre = "Mi Promoción";

                string vistaPrevia = GenerarVistaPrevia(nombre);
                TxtVistaPrevia.Text = vistaPrevia;

                // Actualizar resumen
                ActualizarResumenConfiguracion();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en vista previa: {ex.Message}");
            }
        }

        // ✅ MODIFICADO: GenerarVistaPrevia para mejorar el caso "Cantidad"
        private string GenerarVistaPrevia(string nombre)
        {
            string preview = $"🎁 {nombre}\n\n";

            switch (_tipoPromocionSeleccionado)
            {
                case "DescuentoPorcentaje":
                    decimal.TryParse(TxtValorPorcentaje.Text, out decimal porcentajeDescuento);
                    decimal.TryParse(TxtDescuentoMaximo.Text, out decimal maxDescuento);

                    preview += $"💰 DESCUENTO: {porcentajeDescuento:F1}% de descuento\n";
                    if (maxDescuento > 0)
                        preview += $"🔒 MÁXIMO: Hasta ${maxDescuento:F2} de descuento\n";
                    preview += $"📝 EJEMPLO: En una compra de $1,000:\n";
                    preview += $"   • Descuento: ${Math.Min(1000 * (porcentajeDescuento / 100), maxDescuento > 0 ? maxDescuento : 1000):F2}\n";
                    preview += $"   • Total a pagar: ${1000 - Math.Min(1000 * (porcentajeDescuento / 100), maxDescuento > 0 ? maxDescuento : 1000):F2}";
                    break;

                case "DescuentoFijo":
                    decimal.TryParse(TxtValorFijo.Text, out decimal descuentoFijo);
                    decimal.TryParse(TxtMontoMinimo.Text, out decimal montoMin);

                    preview += $"💵 DESCUENTO: ${descuentoFijo:F2} de descuento\n";
                    if (montoMin > 0)
                        preview += $"🛒 CONDICIÓN: En compras mayores a ${montoMin:F2}\n";
                    preview += $"📝 EJEMPLO: En una compra de ${Math.Max(500, montoMin):F2}:\n";
                    preview += $"   • Descuento: ${descuentoFijo:F2}\n";
                    preview += $"   • Total a pagar: ${Math.Max(500, montoMin) - descuentoFijo:F2}";
                    break;

                case "Cantidad":
                    int.TryParse(TxtCantidadMinima.Text, out int cantidad);
                    decimal.TryParse(TxtPrecioTotal.Text, out decimal precioTotal);

                    if (_productoSeleccionado != null)
                    {
                        preview += $"📦 PRODUCTO: {_productoSeleccionado.NombreArticulo}\n";
                        preview += $"⚖️ OFERTA: {cantidad} {_productoSeleccionado.UnidadMedida} por ${precioTotal:F2}\n";

                        if (cantidad > 0 && precioTotal > 0)
                        {
                            decimal precioPorUnidad = precioTotal / cantidad;
                            decimal precioNormal = _productoSeleccionado.PrecioVenta;
                            decimal ahorro = (precioNormal - precioPorUnidad) * cantidad;

                            preview += $"💡 PRECIO UNITARIO: ${precioPorUnidad:F2} por {_productoSeleccionado.UnidadMedida}\n";
                            preview += $"🆚 PRECIO NORMAL: ${precioNormal:F2} por {_productoSeleccionado.UnidadMedida}\n";

                            if (ahorro > 0)
                            {
                                decimal porcentajeAhorro = (ahorro / (precioNormal * cantidad)) * 100;
                                preview += $"💰 AHORRO: ${ahorro:F2} ({porcentajeAhorro:F1}% descuento)\n";
                            }
                        }

                        preview += $"\n📝 EJEMPLO:\n";
                        preview += $"   • Cliente compra {cantidad} {_productoSeleccionado.UnidadMedida}\n";
                        preview += $"   • Paga ${precioTotal:F2} total\n";
                        preview += $"   • Producto: {_productoSeleccionado.NombreArticulo}";
                    }
                    else
                    {
                        preview += $"⚖️ OFERTA: {cantidad} unidades por ${precioTotal:F2}\n";
                        preview += $"⚠️ Seleccione un producto específico para ver detalles completos";
                    }
                    break;

                case "CompraYLleva":
                    int.TryParse(TxtCompra.Text, out int compra);
                    int.TryParse(TxtLleva.Text, out int lleva);

                    preview += $"🎯 OFERTA: Compra {compra} y llévate {lleva}\n";
                    if (compra > 0 && lleva > 0)
                    {
                        int gratis = lleva - compra;
                        if (gratis > 0)
                            preview += $"🎁 GRATIS: {gratis} producto(s) gratis\n";
                    }
                    preview += $"📝 EJEMPLO: ¡Oferta irresistible!\n";
                    preview += $"   • Cliente paga {compra} productos\n";
                    preview += $"   • Se lleva {lleva} productos en total";
                    break;

                default:
                    preview += "Configure los valores para ver la vista previa";
                    break;
            }

            return preview;
        }

        private void ActualizarResumenConfiguracion()
        {
            try
            {
                TxtResumenTipo.Text = $"🎯 Tipo: {ObtenerNombreTipo(_tipoPromocionSeleccionado)}";

                string valor = ObtenerResumenValor();
                TxtResumenValor.Text = $"💰 Valor: {valor}";

                DateTime? inicio = DpFechaInicio.SelectedDate;
                DateTime? fin = DpFechaFin.SelectedDate;
                TxtResumenVigencia.Text = $"📅 Vigencia: {inicio?.ToString("dd/MM/yyyy") ?? "No definida"} - {fin?.ToString("dd/MM/yyyy") ?? "No definida"}";

                int.TryParse(TxtLimiteUsoTotal.Text, out int limite);
                TxtResumenLimites.Text = $"📊 Límite: {(limite > 0 ? limite.ToString() + " usos" : "Ilimitado")}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en resumen: {ex.Message}");
            }
        }

        private string ObtenerResumenValor()
        {
            return _tipoPromocionSeleccionado switch
            {
                "DescuentoPorcentaje" => $"{TxtValorPorcentaje.Text}% descuento",
                "DescuentoFijo" => $"${TxtValorFijo.Text} descuento",
                "Cantidad" => $"{TxtCantidadMinima.Text} por ${TxtPrecioTotal.Text}",
                "CompraYLleva" => $"Compra {TxtCompra.Text}, lleva {TxtLleva.Text}",
                _ => "No configurado"
            };
        }

        private void AnalizarViabilidad()
        {
            try
            {
                var analisis = CalcularViabilidad();

                // Actualizar color del panel según viabilidad
                switch (analisis.Estado)
                {
                    case "Viable":
                        PanelAnalisisViabilidad.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)); // Verde
                        TxtEstadoViabilidad.Text = "✅ Promoción viable";
                        TxtEstadoViabilidad.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                        break;
                    case "Riesgosa":
                        PanelAnalisisViabilidad.Background = new SolidColorBrush(Color.FromRgb(255, 251, 235)); // Amarillo
                        TxtEstadoViabilidad.Text = "⚠️ Promoción riesgosa";
                        TxtEstadoViabilidad.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
                        break;
                    case "No viable":
                        PanelAnalisisViabilidad.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // Rojo
                        TxtEstadoViabilidad.Text = "❌ Promoción no viable";
                        TxtEstadoViabilidad.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                        break;
                    default:
                        PanelAnalisisViabilidad.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)); // Gris
                        TxtEstadoViabilidad.Text = "ℹ️ Configure para analizar";
                        TxtEstadoViabilidad.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                        break;
                }

                TxtAnalisisDetallado.Text = analisis.Detalle;
                TxtSugerencias.Text = analisis.Sugerencias;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en análisis: {ex.Message}");
            }
        }

        private (string Estado, string Detalle, string Sugerencias) CalcularViabilidad()
        {
            if (string.IsNullOrEmpty(_tipoPromocionSeleccionado))
                return ("Sin configurar", "Seleccione un tipo de promoción", "• Elija el tipo que mejor se adapte a su negocio");

            // Simulación de análisis basado en márgenes promedio
            decimal margenPromedio = _productosDisponibles.Any() ?
                _productosDisponibles.Average(p => p.MargenReal) : 30m;

            decimal impactoDescuento = CalcularImpactoDescuento();
            decimal margenFinal = margenPromedio - impactoDescuento;

            string estado;
            string detalle;
            string sugerencias;

            if (margenFinal >= 15)
            {
                estado = "Viable";
                detalle = $"• Margen promedio actual: {margenPromedio:F1}%\n" +
                         $"• Impacto del descuento: -{impactoDescuento:F1}%\n" +
                         $"• Margen final estimado: {margenFinal:F1}%\n" +
                         $"• Rentabilidad: Mantiene ganancia saludable";
                sugerencias = "• La promoción es rentable y recomendable\n" +
                             "• Considere establecer límites de uso\n" +
                             "• Monitoree las ventas durante la promoción";
            }
            else if (margenFinal >= 5)
            {
                estado = "Riesgosa";
                detalle = $"• Margen promedio actual: {margenPromedio:F1}%\n" +
                         $"• Impacto del descuento: -{impactoDescuento:F1}%\n" +
                         $"• Margen final estimado: {margenFinal:F1}%\n" +
                         $"• Rentabilidad: Margen bajo, riesgo moderado";
                sugerencias = "• Considere reducir el descuento\n" +
                             "• Establezca un monto mínimo de compra\n" +
                             "• Limite la duración de la promoción\n" +
                             "• Aplique solo a productos con mayor margen";
            }
            else
            {
                estado = "No viable";
                detalle = $"• Margen promedio actual: {margenPromedio:F1}%\n" +
                         $"• Impacto del descuento: -{impactoDescuento:F1}%\n" +
                         $"• Margen final estimado: {margenFinal:F1}%\n" +
                         $"• Rentabilidad: Pérdida probable";
                sugerencias = "• Reduzca significativamente el descuento\n" +
                             "• Aumente el monto mínimo de compra\n" +
                             "• Aplique solo a productos específicos\n" +
                             "• Considere un tipo diferente de promoción";
            }

            return (estado, detalle, sugerencias);
        }

        private decimal CalcularImpactoDescuento()
        {
            return _tipoPromocionSeleccionado switch
            {
                "DescuentoPorcentaje" => decimal.TryParse(TxtValorPorcentaje.Text, out decimal p) ? p : 0,
                "DescuentoFijo" => 10m, // Estimación conservadora
                "Cantidad" => CalcularImpactoCantidad(),
                "CompraYLleva" => CalcularImpactoCompraLleva(),
                _ => 0m
            };
        }

        private decimal CalcularImpactoCantidad()
        {
            if (int.TryParse(TxtCantidadMinima.Text, out int cantidad) &&
                decimal.TryParse(TxtPrecioTotal.Text, out decimal precioTotal) &&
                cantidad > 0 && precioTotal > 0)
            {
                decimal precioUnitario = precioTotal / cantidad;
                decimal precioPromedio = _productosDisponibles.Any() ?
                    _productosDisponibles.Average(p => p.PrecioVenta) : 100m;

                if (precioPromedio > 0)
                {
                    decimal descuentoPorcentaje = ((precioPromedio - precioUnitario) / precioPromedio) * 100;
                    return Math.Max(0, descuentoPorcentaje);
                }
            }
            return 0m;
        }

        private decimal CalcularImpactoCompraLleva()
        {
            if (int.TryParse(TxtCompra.Text, out int compra) &&
                int.TryParse(TxtLleva.Text, out int lleva) &&
                compra > 0 && lleva > compra)
            {
                int gratis = lleva - compra;
                decimal porcentajeGratis = (decimal)gratis / lleva * 100;
                return porcentajeGratis;
            }
            return 0m;
        }

        #endregion

        #region Eventos de Botones

        private void BtnAnalisisViabilidad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var analisis = CalcularViabilidad();

                string mensaje = $"📊 ANÁLISIS DETALLADO DE VIABILIDAD\n\n" +
                               $"🎯 PROMOCIÓN: {TxtNombrePromocion.Text.Trim()}\n" +
                               $"📈 ESTADO: {analisis.Estado}\n\n" +
                               $"📋 DETALLES:\n{analisis.Detalle}\n\n" +
                               $"💡 SUGERENCIAS:\n{analisis.Sugerencias}";

                MessageBox.Show(mensaje, "Análisis de Viabilidad",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en análisis: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                BtnGuardar.IsEnabled = false;
                TxtEstadoFormulario.Text = "💾 Guardando promoción...";

                // Usar contexto fresco para evitar conflictos
                using var contextGuardado = new AppDbContext();
                PromocionVenta promocionGuardar;

                if (_esEdicion)
                {
                    promocionGuardar = await contextGuardado.PromocionesVenta
                        .FirstOrDefaultAsync(p => p.Id == _promocionActual.Id);

                    if (promocionGuardar == null)
                    {
                        throw new InvalidOperationException("Promoción no encontrada en la base de datos");
                    }
                }
                else
                {
                    promocionGuardar = new PromocionVenta();
                    contextGuardado.PromocionesVenta.Add(promocionGuardar);
                }

                // Actualizar datos básicos
                promocionGuardar.NombrePromocion = TxtNombrePromocion.Text.Trim();
                promocionGuardar.Descripcion = TxtDescripcion.Text.Trim();
                promocionGuardar.TipoPromocion = _tipoPromocionSeleccionado;
                promocionGuardar.CategoriaPromocion = CmbCategoriaPromocion.Text;

                // Actualizar valores específicos del tipo
                ActualizarValoresPromocion(promocionGuardar);

                // Actualizar fechas y horarios
                promocionGuardar.FechaInicio = DpFechaInicio.SelectedDate ?? DateTime.Today;
                promocionGuardar.FechaFin = DpFechaFin.SelectedDate ?? DateTime.Today.AddDays(30);
                promocionGuardar.HoraInicio = TxtHoraInicio.Text.Trim();
                promocionGuardar.HoraFin = TxtHoraFin.Text.Trim();

                // Actualizar límites
                int.TryParse(TxtLimiteUsoTotal.Text, out int limiteTotal);
                int.TryParse(TxtLimitePorCliente.Text, out int limitePorCliente);
                decimal.TryParse(TxtMontoMinimo.Text, out decimal montoMinimo);

                promocionGuardar.LimiteUsoTotal = limiteTotal;
                promocionGuardar.LimitePorCliente = limitePorCliente;
                promocionGuardar.MontoMinimo = montoMinimo;

                // Actualizar configuración
                promocionGuardar.Activa = ChkActivaPromocion.IsChecked ?? true;
                promocionGuardar.AplicacionAutomatica = ChkAplicacionAutomatica.IsChecked ?? true;
                promocionGuardar.IntegradaPOS = ChkIntegrarPOS.IsChecked ?? true;
                promocionGuardar.Combinable = ChkCombinable.IsChecked ?? false;

                if (!_esEdicion)
                {
                    promocionGuardar.UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema";
                }

                // Configurar para POS si está marcado
                if (promocionGuardar.IntegradaPOS)
                {
                    promocionGuardar.ConfigurarParaPOS(true);
                }

                await contextGuardado.SaveChangesAsync();

                // Actualizar ID si es nueva
                if (!_esEdicion)
                {
                    _promocionActual.Id = promocionGuardar.Id;
                }

                MessageBox.Show($"✅ Promoción '{promocionGuardar.NombrePromocion}' guardada exitosamente!\n\n" +
                              $"ID: {promocionGuardar.Id}\n" +
                              $"Tipo: {ObtenerNombreTipo(promocionGuardar.TipoPromocion)}\n" +
                              $"Estado: {promocionGuardar.EstadoPromocion}\n" +
                              $"Código: {promocionGuardar.CodigoPromocion}",
                              "Promoción Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtEstadoFormulario.Text = "❌ Error al guardar promoción";

                string errorDetallado = $"Error al guardar promoción:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetallado += $"\n\nDetalle: {ex.InnerException.Message}";
                }

                MessageBox.Show(errorDetallado, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"❌ ERROR GUARDANDO PROMOCIÓN: {ex}");
            }
            finally
            {
                BtnGuardar.IsEnabled = true;
            }
        }

        // ✅ MODIFICADO: ActualizarValoresPromocion para guardar producto seleccionado
        private void ActualizarValoresPromocion(PromocionVenta promocion)
        {
            switch (_tipoPromocionSeleccionado)
            {
                case "DescuentoPorcentaje":
                    decimal.TryParse(TxtValorPorcentaje.Text, out decimal porcentajePromo);
                    decimal.TryParse(TxtDescuentoMaximo.Text, out decimal descuentoMax);
                    promocion.ValorPromocion = porcentajePromo;
                    promocion.DescuentoMaximo = descuentoMax;
                    break;

                case "DescuentoFijo":
                    decimal.TryParse(TxtValorFijo.Text, out decimal valorFijo);
                    promocion.ValorPromocion = valorFijo;
                    break;

                case "Cantidad":
                    int.TryParse(TxtCantidadMinima.Text, out int cantidad);
                    decimal.TryParse(TxtPrecioTotal.Text, out decimal precioTotal);

                    promocion.CantidadMinima = cantidad;
                    promocion.ValorPromocion = precioTotal;

                    // ✅ NUEVO: Guardar producto específico
                    if (_productoSeleccionado != null)
                    {
                        promocion.ProductosAplicables = _productoSeleccionado.Id.ToString();
                    }
                    break;

                case "CompraYLleva":
                    int.TryParse(TxtCompra.Text, out int compra);
                    int.TryParse(TxtLleva.Text, out int lleva);
                    promocion.CantidadMinima = compra;
                    promocion.ValorPromocion = lleva;
                    break;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro que desea cancelar?\n\nSe perderán todos los cambios no guardados.",
                    "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cancelar:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Validación

        private bool ValidarFormulario()
        {
            try
            {
                // Validar nombre
                if (string.IsNullOrWhiteSpace(TxtNombrePromocion.Text))
                {
                    MessageBox.Show("El nombre de la promoción es obligatorio.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNombrePromocion.Focus();
                    return false;
                }

                // Validar tipo seleccionado
                if (string.IsNullOrEmpty(_tipoPromocionSeleccionado))
                {
                    MessageBox.Show("Debe seleccionar un tipo de promoción.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Validar fechas
                if (!DpFechaInicio.SelectedDate.HasValue || !DpFechaFin.SelectedDate.HasValue)
                {
                    MessageBox.Show("Debe especificar las fechas de inicio y fin.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (DpFechaInicio.SelectedDate > DpFechaFin.SelectedDate)
                {
                    MessageBox.Show("La fecha de inicio no puede ser posterior a la fecha de fin.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Validar valores específicos del tipo
                return ValidarValoresTipo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en validación:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // ✅ MODIFICADO: ValidarValoresTipo para incluir validación de producto
        private bool ValidarValoresTipo()
        {
            switch (_tipoPromocionSeleccionado)
            {
                case "DescuentoPorcentaje":
                    if (!decimal.TryParse(TxtValorPorcentaje.Text, out decimal porcentajeValor) || porcentajeValor <= 0 || porcentajeValor > 100)
                    {
                        MessageBox.Show("El porcentaje debe ser un número entre 0.1 y 100.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtValorPorcentaje.Focus();
                        return false;
                    }
                    break;

                case "DescuentoFijo":
                    if (!decimal.TryParse(TxtValorFijo.Text, out decimal valorFijo) || valorFijo <= 0)
                    {
                        MessageBox.Show("El valor del descuento debe ser mayor a 0.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtValorFijo.Focus();
                        return false;
                    }
                    break;

                case "Cantidad":
                    if (_productoSeleccionado == null)
                    {
                        MessageBox.Show("Debe seleccionar un producto específico para la promoción por cantidad.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CmbProductoAGranel.Focus();
                        return false;
                    }

                    if (!int.TryParse(TxtCantidadMinima.Text, out int cantidad) || cantidad <= 0)
                    {
                        MessageBox.Show("La cantidad debe ser un número mayor a 0.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtCantidadMinima.Focus();
                        return false;
                    }

                    if (!decimal.TryParse(TxtPrecioTotal.Text, out decimal precio) || precio <= 0)
                    {
                        MessageBox.Show("El precio total debe ser mayor a 0.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtPrecioTotal.Focus();
                        return false;
                    }

                    // ✅ VALIDACIÓN ADICIONAL: Verificar que el precio promocional tenga sentido
                    decimal precioNormalTotal = cantidad * _productoSeleccionado.PrecioVenta;
                    if (precio >= precioNormalTotal)
                    {
                        var resultado = MessageBox.Show(
                            $"⚠️ El precio promocional (${precio:F2}) es igual o mayor al precio normal (${precioNormalTotal:F2}).\n\n" +
                            $"¿Está seguro que desea continuar?",
                            "Precio Sin Descuento", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (resultado != MessageBoxResult.Yes)
                        {
                            TxtPrecioTotal.Focus();
                            return false;
                        }
                    }
                    break;

                case "CompraYLleva":
                    if (!int.TryParse(TxtCompra.Text, out int compra) || compra <= 0)
                    {
                        MessageBox.Show("La cantidad a comprar debe ser mayor a 0.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtCompra.Focus();
                        return false;
                    }
                    if (!int.TryParse(TxtLleva.Text, out int lleva) || lleva <= compra)
                    {
                        MessageBox.Show("La cantidad a llevar debe ser mayor a la cantidad a comprar.",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtLleva.Focus();
                        return false;
                    }
                    break;
            }

            return true;
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _context?.Dispose();
                _context = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar CrearEditarPromocionWindow: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}