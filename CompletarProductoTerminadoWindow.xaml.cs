using System;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Models;

namespace costbenefi
{
    /// <summary>
    /// ✅ VENTANA PARA COMPLETAR DATOS DEL PRODUCTO TERMINADO
    /// </summary>
    public partial class CompletarProductoTerminadoWindow : Window
    {
        public DatosProductoTerminado DatosProducto { get; private set; }

        private ProcesoFabricacion _proceso;
        private string _numeroLote;
        private decimal _cantidadFinal;
        private decimal _costoUnitario;

        public CompletarProductoTerminadoWindow(ProcesoFabricacion proceso, string numeroLote, decimal cantidadFinal, decimal costoUnitario)
        {
            InitializeComponent();

            _proceso = proceso;
            _numeroLote = numeroLote;
            _cantidadFinal = cantidadFinal;
            _costoUnitario = costoUnitario;

            ConfigurarVentana();
            CargarDatosIniciales();
        }

        private void ConfigurarVentana()
        {
            Title = $"Completar Producto: {_proceso.NombreProducto}";
            Width = 600;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
        }

        private void CargarDatosIniciales()
        {
            try
            {
                // ✅ INFORMACIÓN DEL LOTE
                TxtInfoLote.Text = $"Lote: {_numeroLote} | Cantidad: {_cantidadFinal:F2} {_proceso.UnidadMedidaProducto}";
                TxtCostoUnitario.Text = $"Costo unitario: {_costoUnitario:C4}";

                // ✅ DATOS PRE-LLENADOS
                TxtNombreProducto.Text = $"{_proceso.NombreProducto} - Lote {_numeroLote}";
                TxtCategoria.Text = $"Fabricados - {_proceso.CategoriaProducto}";

                // ✅ CONFIGURACIÓN DE VENTA
                ChkActivoParaVenta.IsChecked = true;
                TxtMargenObjetivo.Text = _proceso.MargenObjetivo.ToString("F0");
                TxtStockMinimo.Text = "1";

                // ✅ CALCULAR PRECIO SUGERIDO
                CalcularPrecioSugerido();

                // ✅ GENERAR CÓDIGO DE BARRAS AUTOMÁTICO
                GenerarCodigoBarrasAutomatico();

                // ✅ CONFIGURAR EVENTOS
                TxtMargenObjetivo.TextChanged += (s, e) => CalcularPrecioSugerido();
                ChkActivoParaVenta.Checked += (s, e) => ActualizarInterfazVenta();
                ChkActivoParaVenta.Unchecked += (s, e) => ActualizarInterfazVenta();

                // ✅ FOCO INICIAL
                TxtCodigoBarras.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos iniciales: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GenerarCodigoBarrasAutomatico()
        {
            try
            {
                // ✅ GENERAR CÓDIGO ÚNICO BASADO EN FECHA Y LOTE
                var fecha = DateTime.Now;
                var codigoSugerido = $"FAB{fecha:yyMM}{_numeroLote.Replace("FAB", "").Substring(0, Math.Min(6, _numeroLote.Length))}";

                TxtCodigoBarras.Text = codigoSugerido;
                TxtCodigoBarras.SelectAll();
            }
            catch
            {
                TxtCodigoBarras.Text = $"FAB{DateTime.Now:yyMMddHHmm}";
            }
        }

        private void CalcularPrecioSugerido()
        {
            try
            {
                if (decimal.TryParse(TxtMargenObjetivo.Text, out decimal margen))
                {
                    decimal precioSugerido = _costoUnitario * (1 + (margen / 100));
                    TxtPrecioVenta.Text = precioSugerido.ToString("F2");

                    // ✅ MOSTRAR INFORMACIÓN ADICIONAL
                    decimal gananciaUnitaria = precioSugerido - _costoUnitario;
                    TxtInfoGanancia.Text = $"Ganancia: {gananciaUnitaria:C4} por unidad";
                }
            }
            catch
            {
                TxtPrecioVenta.Text = _costoUnitario.ToString("F2");
            }
        }

        private void ActualizarInterfazVenta()
        {
            bool activoParaVenta = ChkActivoParaVenta.IsChecked == true;

            TxtPrecioVenta.IsEnabled = activoParaVenta;
            TxtMargenObjetivo.IsEnabled = activoParaVenta;
            TxtStockMinimo.IsEnabled = activoParaVenta;
            DpFechaVencimiento.IsEnabled = activoParaVenta;

            if (!activoParaVenta)
            {
                TxtPrecioVenta.Text = "0";
                TxtInfoGanancia.Text = "Producto no configurado para venta";
            }
            else
            {
                CalcularPrecioSugerido();
            }
        }

        private void BtnGenerarCodigo_Click(object sender, RoutedEventArgs e)
        {
            GenerarCodigoBarrasAutomatico();
        }

        private void BtnCalcularPrecio_Click(object sender, RoutedEventArgs e)
        {
            CalcularPrecioSugerido();
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ VALIDAR DATOS
                if (string.IsNullOrWhiteSpace(TxtNombreProducto.Text))
                {
                    MessageBox.Show("El nombre del producto es obligatorio.",
                                   "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNombreProducto.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtCategoria.Text))
                {
                    MessageBox.Show("La categoría es obligatoria.",
                                   "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtCategoria.Focus();
                    return;
                }

                if (!decimal.TryParse(TxtPrecioVenta.Text, out decimal precioVenta) || precioVenta < 0)
                {
                    MessageBox.Show("El precio de venta debe ser un número válido mayor o igual a 0.",
                                   "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPrecioVenta.Focus();
                    return;
                }

                if (!decimal.TryParse(TxtMargenObjetivo.Text, out decimal margen) || margen < 0)
                {
                    MessageBox.Show("El margen objetivo debe ser un número válido mayor o igual a 0.",
                                   "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtMargenObjetivo.Focus();
                    return;
                }

                if (!decimal.TryParse(TxtStockMinimo.Text, out decimal stockMinimo) || stockMinimo < 0)
                {
                    MessageBox.Show("El stock mínimo debe ser un número válido mayor o igual a 0.",
                                   "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtStockMinimo.Focus();
                    return;
                }

                // ✅ CREAR OBJETO CON DATOS
                DatosProducto = new DatosProductoTerminado
                {
                    NombreProducto = TxtNombreProducto.Text.Trim(),
                    Categoria = TxtCategoria.Text.Trim(),
                    CodigoBarras = TxtCodigoBarras.Text.Trim(),
                    ActivoParaVenta = ChkActivoParaVenta.IsChecked == true,
                    PrecioVenta = precioVenta,
                    MargenObjetivo = margen,
                    StockMinimoVenta = stockMinimo,
                    FechaVencimiento = DpFechaVencimiento.SelectedDate
                };

                // ✅ VALIDAR DATOS
                if (!DatosProducto.ValidarDatos())
                {
                    MessageBox.Show("Hay errores en los datos ingresados. Por favor revise.",
                                   "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ CONFIRMACIÓN
                var mensaje = $"✅ CONFIRMAR CREACIÓN DEL PRODUCTO\n\n" +
                             $"📦 Nombre: {DatosProducto.NombreProducto}\n" +
                             $"📂 Categoría: {DatosProducto.Categoria}\n" +
                             $"🏷️ Código: {DatosProducto.CodigoBarras}\n" +
                             $"💰 Precio: {DatosProducto.PrecioVenta:C2}\n" +
                             $"📊 Margen: {DatosProducto.MargenObjetivo:F0}%\n" +
                             $"🛒 Activo para venta: {(DatosProducto.ActivoParaVenta ? "Sí" : "No")}\n\n" +
                             $"¿Crear el producto con estos datos?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Creación",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar datos: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("¿Está seguro que desea cancelar?\n\nSe perderá la fabricación en proceso.",
                                           "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            var ayuda = "📋 COMPLETAR PRODUCTO TERMINADO\n\n" +
                       "🎯 CAMPOS OBLIGATORIOS:\n" +
                       "• Nombre del producto\n" +
                       "• Categoría\n" +
                       "• Precio de venta (si está activo para venta)\n\n" +

                       "🏷️ CÓDIGO DE BARRAS:\n" +
                       "• Se genera automáticamente\n" +
                       "• Puede modificarlo manualmente\n" +
                       "• Debe ser único en el sistema\n\n" +

                       "💰 CONFIGURACIÓN DE VENTA:\n" +
                       "• Margen objetivo: porcentaje de ganancia deseado\n" +
                       "• Precio se calcula automáticamente\n" +
                       "• Stock mínimo: cantidad mínima para alertas\n\n" +

                       "✅ RESULTADO:\n" +
                       "• El producto se agregará a RawMaterials\n" +
                       "• Estará disponible inmediatamente en POS\n" +
                       "• Se registrarán todos los movimientos";

            MessageBox.Show(ayuda, "Ayuda - Completar Producto",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}