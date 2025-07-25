using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
namespace costbenefi.Views
{
    public partial class AddMaterialPiezasWindow : Window
    {
        private readonly AppDbContext _context;
        private string _barcodeBuffer = "";
        private DateTime _lastKeyPress = DateTime.MinValue;
        private const int SCANNER_TIMEOUT_MS = 100;
        private bool _isCalculating = false;

        public string MotivoCreacion { get; private set; } = "";

        public AddMaterialPiezasWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            ConfigurarEscaner();
            InicializarFormulario();
        }

        public AddMaterialPiezasWindow(AppDbContext context, string barcode) : this(context)
        {
            if (!string.IsNullOrEmpty(barcode))
            {
                TxtCodigoBarras.Text = barcode;
                Dispatcher.BeginInvoke(new Action(async () => await ProcesarCodigoBarras(barcode)));
            }
        }

        #region CONFIGURACIÓN INICIAL

        private void ConfigurarEscaner()
        {
            this.PreviewKeyDown += OnPreviewKeyDown;
            this.KeyDown += OnKeyDown;
        }

        private void InicializarFormulario()
        {
            try
            {
                // Configurar valores por defecto
                if (TxtCantidadPorUnidad != null) TxtCantidadPorUnidad.Text = "500";
                if (TxtCantidadComprada != null) TxtCantidadComprada.Text = "1";
                if (TxtUnidadesPorPaquete != null) TxtUnidadesPorPaquete.Text = "12";
                if (TxtPrecioTotal != null) TxtPrecioTotal.Text = "0";
                if (TxtIVA != null) TxtIVA.Text = "16";
                if (TxtStockAnterior != null) TxtStockAnterior.Text = "0";
                if (TxtAlertaMinimo != null) TxtAlertaMinimo.Text = "0";
                if (ChkIncludeIVA != null) ChkIncludeIVA.IsChecked = true;
                if (ChkEsEmpaquetado != null) ChkEsEmpaquetado.IsChecked = false;
                if (RbAlmacenarPiezas != null) RbAlmacenarPiezas.IsChecked = true;

                // Seleccionar primeros elementos por defecto
                if (CmbPresentacion?.Items.Count > 0) CmbPresentacion.SelectedIndex = 0;
                if (CmbUnidadMedida?.Items.Count > 0) CmbUnidadMedida.SelectedIndex = 0;

                // Actualizar UI después de que todo esté cargado
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ActualizarUI();
                    CalcularTodo();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar formulario: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ESCÁNER DE CÓDIGOS

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox textBox && textBox != TxtCodigoBarras) return;
            ProcesarEntradaEscaner(e);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement == this)
            {
                ProcesarEntradaEscaner(e);
            }
        }

        private void ProcesarEntradaEscaner(KeyEventArgs e)
        {
            var now = DateTime.Now;

            if ((now - _lastKeyPress).TotalMilliseconds > SCANNER_TIMEOUT_MS)
            {
                _barcodeBuffer = "";
            }

            _lastKeyPress = now;

            if (e.Key == Key.Enter)
            {
                if (_barcodeBuffer.Length > 3)
                {
                    _ = ProcesarCodigoBarras(_barcodeBuffer);
                    e.Handled = true;
                }
                _barcodeBuffer = "";
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                _barcodeBuffer += (e.Key - Key.D0).ToString();
            }
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                _barcodeBuffer += e.Key.ToString();
            }
        }

        private async Task ProcesarCodigoBarras(string codigo)
        {
            if (TxtCodigoBarras != null)
                TxtCodigoBarras.Text = codigo;

            try
            {
                var existingMaterial = await _context.RawMaterials
                    .FirstOrDefaultAsync(m => m.CodigoBarras == codigo);

                if (existingMaterial != null)
                {
                    var result = MessageBox.Show(
                        $"Producto encontrado: {existingMaterial.NombreArticulo}\n\n" +
                        $"¿Cargar datos existentes?\n" +
                        "(Solo se cargarán nombre, categoría y proveedor)",
                        "Código Encontrado",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        CargarProductoExistente(existingMaterial);
                    }
                }
                else
                {
                    MessageBox.Show("Código nuevo detectado.\nComplete la información del producto.",
                        "Nuevo Producto", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (TxtNombre != null) TxtNombre.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar código: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarProductoExistente(RawMaterial material)
        {
            try
            {
                if (TxtNombre != null) TxtNombre.Text = material.NombreArticulo;
                if (CmbCategoria != null) CmbCategoria.Text = material.Categoria;
                if (TxtProveedor != null) TxtProveedor.Text = material.Proveedor;

                // No cargar observaciones existentes, crear nuevas
                if (TxtObservaciones != null)
                {
                    TxtObservaciones.Text = $"NUEVA ENTRADA DE PIEZAS - {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
                                          $"Basado en producto existente: {material.NombreArticulo}";
                }

                if (TxtCantidadComprada != null) TxtCantidadComprada.Focus();

                MessageBox.Show("Información básica cargada.\nConfigura las cantidades y precios para esta nueva entrada.",
                    "Datos Cargados", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar producto existente: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region EVENTOS DE CONTROLES

        private void PresentacionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isCalculating)
            {
                ActualizarUI();
                CalcularTodo();
            }
        }

        private void EmpaquetadoChanged(object sender, RoutedEventArgs e)
        {
            if (!_isCalculating)
            {
                ActualizarUI();
                CalcularTodo();
            }
        }

        private void TipoAlmacenamientoChanged(object sender, RoutedEventArgs e)
        {
            if (!_isCalculating)
            {
                CalcularTodo();
            }
        }

        private void ValorChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isCalculating && IsLoaded)
            {
                ActualizarResumenCompra();
                CalcularTodo();
            }
        }

        private void ValorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isCalculating && IsLoaded)
            {
                ActualizarResumenCompra();
                CalcularTodo();
            }
        }

        private void IVAChanged(object sender, RoutedEventArgs e)
        {
            if (!_isCalculating && IsLoaded)
            {
                CalcularTodo();
            }
        }

        private void BtnMas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtCantidadComprada?.Text != null)
                {
                    if (decimal.TryParse(TxtCantidadComprada.Text, out decimal cantidad))
                    {
                        TxtCantidadComprada.Text = (cantidad + 1).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al incrementar cantidad: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnMenos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtCantidadComprada?.Text != null)
                {
                    if (decimal.TryParse(TxtCantidadComprada.Text, out decimal cantidad) && cantidad > 0)
                    {
                        TxtCantidadComprada.Text = Math.Max(1, cantidad - 1).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al decrementar cantidad: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region LÓGICA DE UI

        private void ActualizarUI()
        {
            if (_isCalculating) return;

            try
            {
                // Mostrar/ocultar panel de empaquetado
                if (PanelEmpaquetado != null && ChkEsEmpaquetado != null)
                {
                    PanelEmpaquetado.Visibility = ChkEsEmpaquetado.IsChecked == true
                        ? Visibility.Visible : Visibility.Collapsed;
                }

                // Actualizar texto de presentación
                if (CmbPresentacion?.SelectedItem is ComboBoxItem item && TxtPresentacionDisplay != null)
                {
                    string presentacion = item.Content?.ToString() ?? "unidad";
                    TxtPresentacionDisplay.Text = presentacion.ToLower();

                    // Actualizar labels de costos
                    if (TxtLabelCostoPieza != null)
                        TxtLabelCostoPieza.Text = $"Por cada {presentacion.ToLower()}:";
                }

                // Actualizar tipo de unidad en la compra
                if (TxtTipoUnidad != null && ChkEsEmpaquetado != null)
                {
                    if (ChkEsEmpaquetado.IsChecked == true)
                    {
                        TxtTipoUnidad.Text = "paquetes";
                    }
                    else
                    {
                        string presentacion = (CmbPresentacion?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "unidad";
                        TxtTipoUnidad.Text = presentacion.ToLower() + "s";
                    }
                }

                // Actualizar label de costo base
                if (TxtLabelCostoBase != null && CmbUnidadMedida?.SelectedItem is ComboBoxItem unidadItem)
                {
                    string unidad = unidadItem.Content?.ToString() ?? "ml";
                    TxtLabelCostoBase.Text = $"Por cada {unidad}:";
                }

                ActualizarResumenCompra();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarUI: {ex.Message}");
            }
        }

        private void ActualizarResumenCompra()
        {
            if (_isCalculating) return;

            try
            {
                if (TxtResumenCompra == null) return;

                if (!ValidarCamposBasicos())
                {
                    TxtResumenCompra.Text = "📊 Complete los datos para ver el resumen";
                    return;
                }

                // Obtener valores seguros
                decimal cantidadComprada = decimal.TryParse(TxtCantidadComprada?.Text, out decimal cc) ? cc : 0;
                decimal cantidadPorUnidad = decimal.TryParse(TxtCantidadPorUnidad?.Text, out decimal cpu) ? cpu : 0;
                bool esEmpaquetado = ChkEsEmpaquetado?.IsChecked == true;
                string presentacion = (CmbPresentacion?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "unidad";
                string unidad = (CmbUnidadMedida?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ml";

                decimal totalUnidades = cantidadComprada;
                decimal totalContenido = 0;
                string resumen = "";

                if (esEmpaquetado)
                {
                    decimal unidadesPorPaquete = decimal.TryParse(TxtUnidadesPorPaquete?.Text, out decimal upp) ? upp : 1;
                    totalUnidades = cantidadComprada * unidadesPorPaquete;
                    totalContenido = totalUnidades * cantidadPorUnidad;

                    resumen = $"📊 Total: {cantidadComprada} paquete{(cantidadComprada > 1 ? "s" : "")} = " +
                              $"{totalUnidades} {presentacion.ToLower()}{(totalUnidades > 1 ? "s" : "")} = " +
                              $"{totalContenido:F0} {unidad}";

                    // Actualizar info de empaquetado
                    if (TxtInfoEmpaquetado != null)
                    {
                        TxtInfoEmpaquetado.Text = $"💡 Ejemplo: 1 paquete = {unidadesPorPaquete} {presentacion.ToLower()}s " +
                                                 $"de {cantidadPorUnidad}{unidad} = {unidadesPorPaquete * cantidadPorUnidad}{unidad} total";
                    }
                }
                else
                {
                    totalContenido = cantidadComprada * cantidadPorUnidad;
                    resumen = $"📊 Total: {cantidadComprada} {presentacion.ToLower()}{(cantidadComprada > 1 ? "s" : "")} = " +
                              $"{totalContenido:F0} {unidad}";
                }

                TxtResumenCompra.Text = resumen;
            }
            catch (Exception ex)
            {
                if (TxtResumenCompra != null)
                    TxtResumenCompra.Text = "📊 Error en cálculo del resumen";
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarResumenCompra: {ex.Message}");
            }
        }

        #endregion

        #region LÓGICA DE CÁLCULOS

        private void CalcularTodo()
        {
            if (_isCalculating) return;
            _isCalculating = true;

            try
            {
                if (!ValidarCamposCalculos())
                {
                    LimpiarResultados();
                    return;
                }

                // Obtener todos los valores de forma segura
                decimal cantidadPorUnidad = decimal.TryParse(TxtCantidadPorUnidad?.Text, out decimal cpu) ? cpu : 0;
                decimal cantidadComprada = decimal.TryParse(TxtCantidadComprada?.Text, out decimal cc) ? cc : 0;
                decimal precioTotal = decimal.TryParse(TxtPrecioTotal?.Text, out decimal pt) ? pt : 0;
                decimal porcentajeIVA = decimal.TryParse(TxtIVA?.Text, out decimal iva) ? iva / 100m : 0.16m;
                bool incluyeIVA = ChkIncludeIVA?.IsChecked == true;
                bool esEmpaquetado = ChkEsEmpaquetado?.IsChecked == true;
                decimal unidadesPorPaquete = esEmpaquetado ? (decimal.TryParse(TxtUnidadesPorPaquete?.Text, out decimal upp) ? upp : 1) : 1;
                decimal stockAnterior = decimal.TryParse(TxtStockAnterior?.Text, out decimal sa) ? sa : 0;
                bool almacenarComoPiezas = RbAlmacenarPiezas?.IsChecked == true;

                string unidad = (CmbUnidadMedida?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ml";
                string presentacion = (CmbPresentacion?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "unidad";

                // CALCULAR TOTALES
                decimal totalPiezas = esEmpaquetado ? cantidadComprada * unidadesPorPaquete : cantidadComprada;
                decimal totalContenido = totalPiezas * cantidadPorUnidad;

                if (totalPiezas <= 0 || totalContenido <= 0 || precioTotal <= 0)
                {
                    LimpiarResultados();
                    return;
                }

                // CALCULAR PRECIOS
                decimal precioConIVA, precioSinIVA;
                if (incluyeIVA)
                {
                    precioConIVA = precioTotal;
                    precioSinIVA = precioTotal / (1 + porcentajeIVA);
                }
                else
                {
                    precioSinIVA = precioTotal;
                    precioConIVA = precioTotal * (1 + porcentajeIVA);
                }

                // CÁLCULO CORRECTO: Precio Total ÷ Cantidad Total
                decimal costoPorPiezaConIVA = precioConIVA / totalPiezas;
                decimal costoPorPiezaSinIVA = precioSinIVA / totalPiezas;
                decimal costoPorUnidadBaseConIVA = precioConIVA / totalContenido;
                decimal costoPorUnidadBaseSinIVA = precioSinIVA / totalContenido;

                // Mostrar resultados
                MostrarResultados(
                    precioConIVA, precioSinIVA,
                    costoPorPiezaConIVA, costoPorPiezaSinIVA,
                    costoPorUnidadBaseConIVA, costoPorUnidadBaseSinIVA,
                    totalPiezas, totalContenido,
                    unidad, presentacion,
                    almacenarComoPiezas, stockAnterior
                );

                // Generar análisis
                GenerarAnalisis(
                    cantidadComprada, totalPiezas, totalContenido,
                    precioTotal, precioConIVA, precioSinIVA,
                    costoPorPiezaConIVA, costoPorPiezaSinIVA,
                    costoPorUnidadBaseConIVA, costoPorUnidadBaseSinIVA,
                    unidad, presentacion,
                    esEmpaquetado, unidadesPorPaquete,
                    incluyeIVA, porcentajeIVA
                );
            }
            catch (Exception ex)
            {
                LimpiarResultados();
                if (TxtAnalisis != null)
                    TxtAnalisis.Text = $"❌ Error en cálculos: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error en CalcularTodo: {ex.Message}");
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private bool ValidarCamposBasicos()
        {
            return TxtCantidadComprada != null && TxtCantidadPorUnidad != null &&
                   decimal.TryParse(TxtCantidadComprada.Text, out decimal cc) && cc > 0 &&
                   decimal.TryParse(TxtCantidadPorUnidad.Text, out decimal cpu) && cpu > 0 &&
                   (ChkEsEmpaquetado?.IsChecked != true ||
                    (decimal.TryParse(TxtUnidadesPorPaquete?.Text, out decimal upp) && upp > 0));
        }

        private bool ValidarCamposCalculos()
        {
            return ValidarCamposBasicos() &&
                   TxtPrecioTotal != null &&
                   decimal.TryParse(TxtPrecioTotal.Text, out decimal pt) && pt > 0 &&
                   decimal.TryParse(TxtIVA?.Text, out decimal iva) && iva >= 0 &&
                   decimal.TryParse(TxtStockAnterior?.Text, out decimal sa) && sa >= 0;
        }

        private void MostrarResultados(
            decimal precioConIVA, decimal precioSinIVA,
            decimal costoPorPiezaConIVA, decimal costoPorPiezaSinIVA,
            decimal costoPorUnidadBaseConIVA, decimal costoPorUnidadBaseSinIVA,
            decimal totalPiezas, decimal totalContenido,
            string unidad, string presentacion,
            bool almacenarComoPiezas, decimal stockAnterior)
        {
            try
            {
                // Precios totales
                if (TxtPrecioSinIVA != null) TxtPrecioSinIVA.Text = precioSinIVA.ToString("C2");
                if (TxtPrecioConIVA != null) TxtPrecioConIVA.Text = precioConIVA.ToString("C2");

                // Costos por pieza
                if (TxtCostoPiezaConIVA != null)
                    TxtCostoPiezaConIVA.Text = $"{costoPorPiezaConIVA:C2} CON IVA";

                if (TxtCostoPiezaSinIVA != null)
                    TxtCostoPiezaSinIVA.Text = $"{costoPorPiezaSinIVA:C2} SIN IVA";

                // Costos por unidad base
                if (TxtCostoBaseConIVA != null)
                    TxtCostoBaseConIVA.Text = $"{costoPorUnidadBaseConIVA:C4} CON IVA";

                if (TxtCostoBaseSinIVA != null)
                    TxtCostoBaseSinIVA.Text = $"{costoPorUnidadBaseSinIVA:C4} SIN IVA";

                // Conversiones
                if (TxtConversiones != null)
                {
                    TxtConversiones.Text = GenerarTextoConversiones(
                        costoPorUnidadBaseConIVA, costoPorUnidadBaseSinIVA, unidad
                    );
                }

                // Stock final
                if (TxtStockFinal != null)
                {
                    if (almacenarComoPiezas)
                    {
                        decimal stockFinal = stockAnterior + totalPiezas;
                        TxtStockFinal.Text = $"{stockFinal:F0} piezas";
                    }
                    else
                    {
                        decimal stockFinal = stockAnterior + totalContenido;
                        TxtStockFinal.Text = $"{stockFinal:F2} {unidad}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MostrarResultados: {ex.Message}");
            }
        }

        private string GenerarTextoConversiones(decimal costoConIVA, decimal costoSinIVA, string unidad)
        {
            try
            {
                switch (unidad.ToLower())
                {
                    case "ml":
                        return $"Por litro: {costoConIVA * 1000:C2} CON | {costoSinIVA * 1000:C2} SIN";
                    case "l":
                        return $"Por ml: {costoConIVA / 1000:C4} CON | {costoSinIVA / 1000:C4} SIN";
                    case "g":
                        return $"Por kg: {costoConIVA * 1000:C2} CON | {costoSinIVA * 1000:C2} SIN";
                    case "kg":
                        return $"Por gramo: {costoConIVA / 1000:C4} CON | {costoSinIVA / 1000:C4} SIN";
                    case "cm":
                        return $"Por metro: {costoConIVA * 100:C2} CON | {costoSinIVA * 100:C2} SIN";
                    case "m":
                        return $"Por cm: {costoConIVA / 100:C4} CON | {costoSinIVA / 100:C4} SIN";
                    default:
                        return $"Costo base por {unidad}";
                }
            }
            catch
            {
                return "Error en conversiones";
            }
        }

        private void GenerarAnalisis(
            decimal cantidadComprada, decimal totalPiezas, decimal totalContenido,
            decimal precioTotal, decimal precioConIVA, decimal precioSinIVA,
            decimal costoPorPiezaConIVA, decimal costoPorPiezaSinIVA,
            decimal costoPorUnidadBaseConIVA, decimal costoPorUnidadBaseSinIVA,
            string unidad, string presentacion,
            bool esEmpaquetado, decimal unidadesPorPaquete,
            bool incluyeIVA, decimal porcentajeIVA)
        {
            if (TxtAnalisis == null) return;

            try
            {
                var analisis = new System.Text.StringBuilder();

                analisis.AppendLine("📊 ANÁLISIS DETALLADO DE COMPRA EN PIEZAS");
                analisis.AppendLine("=" + new string('=', 40));

                if (esEmpaquetado)
                {
                    analisis.AppendLine($"📦 Compra: {cantidadComprada} paquete(s) × {unidadesPorPaquete} unidades = {totalPiezas} {presentacion.ToLower()}s");
                }
                else
                {
                    analisis.AppendLine($"📦 Compra: {totalPiezas} {presentacion.ToLower()}(s)");
                }

                analisis.AppendLine($"📏 Contenido total: {totalContenido:F2} {unidad}");
                analisis.AppendLine($"💰 Precio pagado: {precioTotal:C2} {(incluyeIVA ? "(incluye IVA)" : "(sin IVA)")}");

                analisis.AppendLine($"\n📈 DESGLOSE DE PRECIOS:");
                analisis.AppendLine($"   • Precio total CON IVA: {precioConIVA:C2}");
                analisis.AppendLine($"   • Precio total SIN IVA: {precioSinIVA:C2}");
                analisis.AppendLine($"   • IVA ({porcentajeIVA * 100:F1}%): {precioConIVA - precioSinIVA:C2}");

                analisis.AppendLine($"\n💡 COSTO POR UNIDAD:");
                analisis.AppendLine($"   • Por {presentacion.ToLower()} CON IVA: {costoPorPiezaConIVA:C2}");
                analisis.AppendLine($"   • Por {presentacion.ToLower()} SIN IVA: {costoPorPiezaSinIVA:C2}");
                analisis.AppendLine($"   • Por {unidad} CON IVA: {costoPorUnidadBaseConIVA:C4}");
                analisis.AppendLine($"   • Por {unidad} SIN IVA: {costoPorUnidadBaseSinIVA:C4}");

                // Análisis de rentabilidad
                decimal margen50 = costoPorPiezaSinIVA * 1.5m;
                decimal margen100 = costoPorPiezaSinIVA * 2m;
                analisis.AppendLine($"\n💼 ANÁLISIS DE RENTABILIDAD:");
                analisis.AppendLine($"   • Para 50% ganancia, vender {presentacion.ToLower()} a: {margen50:C2} + IVA");
                analisis.AppendLine($"   • Para 100% ganancia, vender {presentacion.ToLower()} a: {margen100:C2} + IVA");

                // Conversiones útiles
                switch (unidad.ToLower())
                {
                    case "ml" when totalContenido >= 1000:
                        analisis.AppendLine($"\n🔄 CONVERSIONES ÚTILES:");
                        analisis.AppendLine($"   • Costo por litro: {costoPorUnidadBaseSinIVA * 1000:C2} SIN IVA");
                        break;
                    case "g" when totalContenido >= 1000:
                        analisis.AppendLine($"\n🔄 CONVERSIONES ÚTILES:");
                        analisis.AppendLine($"   • Costo por kg: {costoPorUnidadBaseSinIVA * 1000:C2} SIN IVA");
                        break;
                    case "cm" when totalContenido >= 100:
                        analisis.AppendLine($"\n🔄 CONVERSIONES ÚTILES:");
                        analisis.AppendLine($"   • Costo por metro: {costoPorUnidadBaseSinIVA * 100:C2} SIN IVA");
                        break;
                }

                TxtAnalisis.Text = analisis.ToString();
            }
            catch (Exception ex)
            {
                TxtAnalisis.Text = $"❌ Error al generar análisis: {ex.Message}";
            }
        }

        private void LimpiarResultados()
        {
            try
            {
                if (TxtPrecioSinIVA != null) TxtPrecioSinIVA.Text = "$0.00";
                if (TxtPrecioConIVA != null) TxtPrecioConIVA.Text = "$0.00";
                if (TxtCostoPiezaConIVA != null) TxtCostoPiezaConIVA.Text = "$0.00 CON IVA";
                if (TxtCostoPiezaSinIVA != null) TxtCostoPiezaSinIVA.Text = "$0.00 SIN IVA";
                if (TxtCostoBaseConIVA != null) TxtCostoBaseConIVA.Text = "$0.0000 CON IVA";
                if (TxtCostoBaseSinIVA != null) TxtCostoBaseSinIVA.Text = "$0.0000 SIN IVA";
                if (TxtConversiones != null) TxtConversiones.Text = "Conversiones aparecerán aquí";
                if (TxtStockFinal != null) TxtStockFinal.Text = "0";
                if (TxtAnalisis != null) TxtAnalisis.Text = "💡 Complete los datos para ver el análisis...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en LimpiarResultados: {ex.Message}");
            }
        }

        #endregion

        #region GUARDAR Y VALIDAR

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            try
            {
                if (BtnGuardar != null)
                {
                    BtnGuardar.IsEnabled = false;
                    BtnGuardar.Content = "⏳ Guardando...";
                }

                await GuardarMaterial();

                MessageBox.Show("✅ Material en piezas/paquetes guardado exitosamente!\n\n" +
                              "El producto se ha registrado en el inventario con todos los cálculos automáticos.",
                              "Material Guardado", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al guardar material:\n\n{ex.Message}\n\n" +
                              "Verifique que todos los datos sean correctos e intente nuevamente.",
                              "Error al Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (BtnGuardar != null)
                {
                    BtnGuardar.IsEnabled = true;
                    BtnGuardar.Content = "💾 Guardar Material";
                }
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(TxtNombre?.Text))
            {
                MessageBox.Show("El nombre del producto es requerido.", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNombre?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CmbCategoria?.Text))
            {
                MessageBox.Show("La categoría es requerida.", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbCategoria?.Focus();
                return false;
            }

            if (!ValidarCamposCalculos())
            {
                MessageBox.Show("Complete todos los campos numéricos correctamente.\n\n" +
                              "Verifique que todos los valores sean números válidos y mayores a 0.",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(TxtPrecioTotal?.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("El precio total debe ser mayor a 0.", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrecioTotal?.Focus();
                return false;
            }

            return true;
        }

        private async Task GuardarMaterial()
        {
            // Obtener todos los valores de forma segura
            decimal cantidadPorUnidad = decimal.Parse(TxtCantidadPorUnidad.Text);
            decimal cantidadComprada = decimal.Parse(TxtCantidadComprada.Text);
            decimal precioTotal = decimal.Parse(TxtPrecioTotal.Text);
            decimal porcentajeIVA = decimal.Parse(TxtIVA.Text) / 100m;
            bool incluyeIVA = ChkIncludeIVA.IsChecked == true;
            bool esEmpaquetado = ChkEsEmpaquetado.IsChecked == true;
            decimal unidadesPorPaquete = esEmpaquetado ? decimal.Parse(TxtUnidadesPorPaquete.Text) : 1;
            decimal stockAnterior = decimal.TryParse(TxtStockAnterior?.Text, out decimal sa) ? sa : 0;
            decimal alertaMinimo = decimal.TryParse(TxtAlertaMinimo?.Text, out decimal am) ? am : 0;
            bool almacenarComoPiezas = RbAlmacenarPiezas?.IsChecked == true;

            string unidad = (CmbUnidadMedida?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ml";
            string presentacion = (CmbPresentacion?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "unidad";

            // Calcular totales
            decimal totalPiezas = esEmpaquetado ? cantidadComprada * unidadesPorPaquete : cantidadComprada;
            decimal totalContenido = totalPiezas * cantidadPorUnidad;

            // Calcular precios
            decimal precioConIVA, precioSinIVA;
            if (incluyeIVA)
            {
                precioConIVA = precioTotal;
                precioSinIVA = precioTotal / (1 + porcentajeIVA);
            }
            else
            {
                precioSinIVA = precioTotal;
                precioConIVA = precioTotal * (1 + porcentajeIVA);
            }

            // CORRECCIÓN: Calcular precios POR UNIDAD correctamente
            decimal costoPorPiezaConIVA = precioConIVA / totalPiezas;
            decimal costoPorPiezaSinIVA = precioSinIVA / totalPiezas;
            decimal costoPorUnidadBaseConIVA = precioConIVA / totalContenido;
            decimal costoPorUnidadBaseSinIVA = precioSinIVA / totalContenido;

            // Determinar unidades según tipo de almacenamiento
            string unidadMedida, unidadBase;
            decimal stockNuevo, factorConversion;
            decimal precioUnitarioConIVA, precioUnitarioSinIVA, precioPorUnidad;

            if (almacenarComoPiezas)
            {
                unidadMedida = "piezas";
                unidadBase = unidad;
                stockNuevo = totalPiezas;
                factorConversion = cantidadPorUnidad;
                precioUnitarioConIVA = costoPorPiezaConIVA;
                precioUnitarioSinIVA = costoPorPiezaSinIVA;
                precioPorUnidad = costoPorPiezaSinIVA;
            }
            else
            {
                unidadMedida = unidad;
                unidadBase = unidad;
                stockNuevo = totalContenido;
                factorConversion = 1;
                precioUnitarioConIVA = costoPorUnidadBaseConIVA;
                precioUnitarioSinIVA = costoPorUnidadBaseSinIVA;
                precioPorUnidad = costoPorUnidadBaseSinIVA;
            }

            var material = new RawMaterial
            {
                // Información básica
                NombreArticulo = TxtNombre?.Text?.Trim() ?? "",
                Categoria = CmbCategoria?.Text?.Trim() ?? "",
                Proveedor = TxtProveedor?.Text?.Trim() ?? "",
                CodigoBarras = TxtCodigoBarras?.Text?.Trim() ?? "",

                // Unidades
                UnidadMedida = unidadMedida,
                UnidadBase = unidadBase,
                FactorConversion = factorConversion,

                // Stock
                StockAntiguo = stockAnterior,
                StockNuevo = stockNuevo,
                AlertaStockBajo = alertaMinimo,

                // Precios - SIEMPRE por unidad
                PrecioConIVA = precioUnitarioConIVA,
                PrecioSinIVA = precioUnitarioSinIVA,
                PrecioPorUnidad = precioPorUnidad,
                PrecioPorUnidadBase = costoPorUnidadBaseSinIVA,
                PrecioBaseConIVA = costoPorUnidadBaseConIVA,
                PrecioBaseSinIVA = costoPorUnidadBaseSinIVA,

                // Metadatos
                Observaciones = GenerarObservaciones(
                    presentacion, cantidadComprada, totalPiezas, totalContenido,
                    precioTotal, unidad, esEmpaquetado, unidadesPorPaquete,
                    almacenarComoPiezas, incluyeIVA
                ),
                FechaCreacion = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            // Guardar material
            _context.RawMaterials.Add(material);
            await _context.SaveChangesAsync();

            // CORRECCIÓN: Crear movimiento con información correcta
            var movimiento = new Movimiento
            {
                RawMaterialId = material.Id,
                TipoMovimiento = "Creación",
                Cantidad = stockNuevo,
                Motivo = GenerarMotivoMovimiento(
                    presentacion, cantidadComprada, totalPiezas,
                    esEmpaquetado, unidadesPorPaquete, almacenarComoPiezas
                ),
                Usuario = Environment.UserName,
                PrecioConIVA = precioUnitarioConIVA,
                PrecioSinIVA = precioUnitarioSinIVA,
                UnidadMedida = unidadMedida,
                FechaMovimiento = DateTime.Now
            };

            _context.Movimientos.Add(movimiento);
            await _context.SaveChangesAsync();

            // Configurar motivo para el formulario principal (si lo necesita)
            MotivoCreacion = $"Material en piezas creado: {stockNuevo:F2} {unidadMedida}";
        }

        private string GenerarMotivoMovimiento(
            string presentacion, decimal cantidadComprada, decimal totalPiezas,
            bool esEmpaquetado, decimal unidadesPorPaquete, bool almacenarComoPiezas)
        {
            if (esEmpaquetado)
            {
                return $"Creación inicial - {cantidadComprada} paquete(s) de {unidadesPorPaquete} {presentacion.ToLower()}s = {totalPiezas} total " +
                       $"(Almacenado como {(almacenarComoPiezas ? "piezas" : "contenido total")})";
            }
            else
            {
                return $"Creación inicial - {totalPiezas} {presentacion.ToLower()}(s) " +
                       $"(Almacenado como {(almacenarComoPiezas ? "piezas" : "contenido total")})";
            }
        }

        private string GenerarObservaciones(
            string presentacion, decimal cantidadComprada, decimal totalPiezas,
            decimal totalContenido, decimal precio, string unidad,
            bool esEmpaquetado, decimal unidadesPorPaquete,
            bool almacenarComoPiezas, bool incluyeIVA)
        {
            var obs = new System.Text.StringBuilder();

            // Agregar observaciones existentes si las hay
            if (!string.IsNullOrWhiteSpace(TxtObservaciones?.Text))
            {
                obs.AppendLine(TxtObservaciones.Text.Trim());
                obs.AppendLine();
            }

            obs.AppendLine($"--- CREACIÓN EN PIEZAS [{DateTime.Now:dd/MM/yyyy HH:mm}] ---");
            obs.AppendLine($"Presentación: {presentacion}");

            if (esEmpaquetado)
            {
                obs.AppendLine($"Empaquetado: SÍ ({unidadesPorPaquete} unidades por paquete)");
                obs.AppendLine($"Compra inicial: {cantidadComprada} paquete(s) = {totalPiezas} unidades");
            }
            else
            {
                obs.AppendLine($"Empaquetado: NO");
                obs.AppendLine($"Compra inicial: {totalPiezas} unidad(es)");
            }

            obs.AppendLine($"Contenido total: {totalContenido:F2} {unidad}");
            obs.AppendLine($"Precio total: {precio:C2} {(incluyeIVA ? "(incluye IVA)" : "(sin IVA)")}");
            obs.AppendLine($"Almacenamiento: {(almacenarComoPiezas ? "Por piezas individuales" : "Por contenido total")}");
            obs.AppendLine($"Cálculos automáticos aplicados");

            return obs.ToString();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro de que desea cancelar?\n\nSe perderán todos los datos ingresados.",
                "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        #endregion
    }
}