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
    public partial class AddMaterialGranelWindow : Window
    {
        private readonly AppDbContext _context;
        private string _barcodeBuffer = "";
        private DateTime _lastKeyPress = DateTime.MinValue;
        private const int SCANNER_TIMEOUT_MS = 100;
        private bool _isCalculating = false;

        public string MotivoCreacion { get; private set; } = "";

        public AddMaterialGranelWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            ConfigurarEscaner();
            InicializarFormulario();
        }

        public AddMaterialGranelWindow(AppDbContext context, string barcode) : this(context)
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
                // Configurar valores por defecto seguros
                if (TxtCantidadTotal != null) TxtCantidadTotal.Text = "1000";
                if (TxtPrecioTotal != null) TxtPrecioTotal.Text = "0";
                if (TxtIVA != null) TxtIVA.Text = "16";
                if (TxtStockAnterior != null) TxtStockAnterior.Text = "0";
                if (TxtAlertaMinimo != null) TxtAlertaMinimo.Text = "0";
                if (ChkIncludeIVA != null) ChkIncludeIVA.IsChecked = true;

                // Seleccionar primer elemento por defecto
                if (CmbUnidadMedida?.Items.Count > 0) CmbUnidadMedida.SelectedIndex = 0;

                // Actualizar UI después de que todo esté cargado
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ActualizarConversion();
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
                    TxtObservaciones.Text = $"NUEVA ENTRADA A GRANEL - {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
                                          $"Basado en producto existente: {material.NombreArticulo}";
                }

                if (TxtCantidadTotal != null) TxtCantidadTotal.Focus();

                MessageBox.Show("Información básica cargada.\nConfigura la cantidad y precio para esta nueva entrada.",
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

        private void UnidadChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isCalculating && IsLoaded)
            {
                ActualizarConversion();
                CalcularTodo();
            }
        }

        private void ValorChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isCalculating && IsLoaded)
            {
                ActualizarConversion();
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

        #endregion

        #region LÓGICA DE CÁLCULOS

        private void ActualizarConversion()
        {
            if (_isCalculating) return;

            try
            {
                if (TxtConversion == null || TxtCantidadTotal == null || CmbUnidadMedida == null)
                {
                    return;
                }

                if (!decimal.TryParse(TxtCantidadTotal.Text, out decimal cantidad) || cantidad <= 0)
                {
                    TxtConversion.Text = "💡 Ingrese una cantidad válida";
                    return;
                }

                string unidad = (CmbUnidadMedida.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string conversion = GenerarTextoConversion(cantidad, unidad);
                TxtConversion.Text = $"💡 Equivale a: {conversion}";
            }
            catch (Exception ex)
            {
                if (TxtConversion != null)
                    TxtConversion.Text = "💡 Error en conversión";
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarConversion: {ex.Message}");
            }
        }

        private string GenerarTextoConversion(decimal cantidad, string unidad)
        {
            try
            {
                return unidad switch
                {
                    "Mililitros" => cantidad >= 1000 ? $"{cantidad / 1000:F2} litros" : $"{cantidad:F0} ml",
                    "Litros" => cantidad < 1 ? $"{cantidad * 1000:F0} ml" : $"{cantidad:F2} litros",
                    "Gramos" => cantidad >= 1000 ? $"{cantidad / 1000:F2} kg" : $"{cantidad:F0} gramos",
                    "Kilos" => cantidad < 1 ? $"{cantidad * 1000:F0} gramos" : $"{cantidad:F2} kg",
                    "Centimetros" => cantidad >= 100 ? $"{cantidad / 100:F2} metros" : $"{cantidad:F0} cm",
                    "Metros" => cantidad < 1 ? $"{cantidad * 100:F0} cm" : $"{cantidad:F2} metros",
                    _ => $"{cantidad:F2} {unidad?.ToLower() ?? "unidades"}"
                };
            }
            catch
            {
                return $"{cantidad:F2} unidades";
            }
        }

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

                // Obtener valores de forma segura
                decimal cantidadTotal = decimal.TryParse(TxtCantidadTotal?.Text, out decimal ct) ? ct : 0;
                decimal precioTotal = decimal.TryParse(TxtPrecioTotal?.Text, out decimal pt) ? pt : 0;
                decimal porcentajeIVA = decimal.TryParse(TxtIVA?.Text, out decimal iva) ? iva / 100m : 0.16m;
                bool incluyeIVA = ChkIncludeIVA?.IsChecked == true;
                decimal stockAnterior = decimal.TryParse(TxtStockAnterior?.Text, out decimal sa) ? sa : 0;
                string unidad = (CmbUnidadMedida?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mililitros";

                if (cantidadTotal <= 0 || precioTotal <= 0)
                {
                    LimpiarResultados();
                    return;
                }

                // CALCULAR PRECIOS TOTALES
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

                // CÁLCULO CORRECTO: Precio Total ÷ Cantidad Total = Costo por Unidad
                decimal costoPorUnidadConIVA = precioConIVA / cantidadTotal;
                decimal costoPorUnidadSinIVA = precioSinIVA / cantidadTotal;

                // Convertir unidades para display
                string unidadCorta = ObtenerUnidadCorta(unidad);

                // Actualizar UI
                MostrarResultados(
                    precioConIVA, precioSinIVA,
                    costoPorUnidadConIVA, costoPorUnidadSinIVA,
                    unidadCorta, unidad,
                    cantidadTotal, stockAnterior
                );

                // Generar análisis
                GenerarAnalisis(
                    cantidadTotal, precioTotal,
                    precioConIVA, precioSinIVA,
                    costoPorUnidadConIVA, costoPorUnidadSinIVA,
                    unidad, unidadCorta,
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

        private bool ValidarCamposCalculos()
        {
            return TxtCantidadTotal != null && TxtPrecioTotal != null &&
                   TxtIVA != null && TxtStockAnterior != null &&
                   decimal.TryParse(TxtCantidadTotal.Text, out decimal ct) && ct > 0 &&
                   decimal.TryParse(TxtPrecioTotal.Text, out decimal pt) && pt > 0 &&
                   decimal.TryParse(TxtIVA.Text, out decimal iva) && iva >= 0 &&
                   decimal.TryParse(TxtStockAnterior.Text, out decimal sa) && sa >= 0;
        }

        private void MostrarResultados(
            decimal precioConIVA, decimal precioSinIVA,
            decimal costoPorUnidadConIVA, decimal costoPorUnidadSinIVA,
            string unidadCorta, string unidadLarga,
            decimal cantidadTotal, decimal stockAnterior)
        {
            try
            {
                // Precios totales
                if (TxtPrecioSinIVA != null) TxtPrecioSinIVA.Text = precioSinIVA.ToString("C2");
                if (TxtPrecioConIVA != null) TxtPrecioConIVA.Text = precioConIVA.ToString("C2");

                // Costos por unidad con etiqueta de unidad
                if (TxtCostoPorUnidadConIVA != null)
                    TxtCostoPorUnidadConIVA.Text = $"{costoPorUnidadConIVA:C4}/{unidadCorta} CON IVA";

                if (TxtCostoPorUnidadSinIVA != null)
                    TxtCostoPorUnidadSinIVA.Text = $"{costoPorUnidadSinIVA:C4}/{unidadCorta} SIN IVA";

                // Conversiones
                if (TxtConversiones != null)
                {
                    TxtConversiones.Text = GenerarTextoConversiones(
                        costoPorUnidadConIVA, costoPorUnidadSinIVA,
                        unidadLarga, unidadCorta
                    );
                }

                // Stock final
                if (TxtStockFinal != null)
                {
                    decimal stockFinal = stockAnterior + cantidadTotal;
                    TxtStockFinal.Text = $"{stockFinal:F2} {unidadCorta}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MostrarResultados: {ex.Message}");
            }
        }

        private string GenerarTextoConversiones(
            decimal costoConIVA, decimal costoSinIVA,
            string unidadLarga, string unidadCorta)
        {
            try
            {
                return unidadLarga switch
                {
                    "Mililitros" => $"Por litro: {costoConIVA * 1000:C2} CON | {costoSinIVA * 1000:C2} SIN",
                    "Litros" => $"Por ml: {costoConIVA / 1000:C4} CON | {costoSinIVA / 1000:C4} SIN",
                    "Gramos" => $"Por kg: {costoConIVA * 1000:C2} CON | {costoSinIVA * 1000:C2} SIN",
                    "Kilos" => $"Por gramo: {costoConIVA / 1000:C4} CON | {costoSinIVA / 1000:C4} SIN",
                    "Centimetros" => $"Por metro: {costoConIVA * 100:C2} CON | {costoSinIVA * 100:C2} SIN",
                    "Metros" => $"Por cm: {costoConIVA / 100:C4} CON | {costoSinIVA / 100:C4} SIN",
                    _ => $"Costo base por {unidadCorta}"
                };
            }
            catch
            {
                return "Error en conversiones";
            }
        }

        private void GenerarAnalisis(
            decimal cantidad, decimal precioTotal,
            decimal precioConIVA, decimal precioSinIVA,
            decimal costoPorUnidadConIVA, decimal costoPorUnidadSinIVA,
            string unidad, string unidadCorta,
            bool incluyeIVA, decimal porcentajeIVA)
        {
            if (TxtAnalisis == null) return;

            try
            {
                var analisis = new System.Text.StringBuilder();

                analisis.AppendLine("📊 ANÁLISIS DETALLADO DE COMPRA A GRANEL");
                analisis.AppendLine("=" + new string('=', 40));

                analisis.AppendLine($"📦 Cantidad comprada: {cantidad:F2} {unidad.ToLower()}");
                analisis.AppendLine($"💰 Precio pagado: {precioTotal:C2} {(incluyeIVA ? "(incluye IVA)" : "(sin IVA)")}");

                analisis.AppendLine($"\n📈 DESGLOSE DE PRECIOS:");
                analisis.AppendLine($"   • Precio total CON IVA: {precioConIVA:C2}");
                analisis.AppendLine($"   • Precio total SIN IVA: {precioSinIVA:C2}");
                analisis.AppendLine($"   • IVA ({porcentajeIVA * 100:F1}%): {precioConIVA - precioSinIVA:C2}");

                analisis.AppendLine($"\n💡 COSTO POR UNIDAD:");
                analisis.AppendLine($"   • Por {unidadCorta} CON IVA: {costoPorUnidadConIVA:C4}");
                analisis.AppendLine($"   • Por {unidadCorta} SIN IVA: {costoPorUnidadSinIVA:C4}");

                // Análisis de rentabilidad
                decimal margen50 = costoPorUnidadSinIVA * 1.5m;
                decimal margen100 = costoPorUnidadSinIVA * 2m;
                analisis.AppendLine($"\n💼 ANÁLISIS DE RENTABILIDAD:");
                analisis.AppendLine($"   • Para 50% de ganancia, vender a: {margen50:C4}/{unidadCorta} + IVA");
                analisis.AppendLine($"   • Para 100% de ganancia, vender a: {margen100:C4}/{unidadCorta} + IVA");

                // Análisis de conversiones útiles
                if (unidad == "Mililitros" && cantidad >= 1000)
                {
                    decimal costoPorLitroSinIVA = costoPorUnidadSinIVA * 1000;
                    analisis.AppendLine($"\n🔄 CONVERSIONES ÚTILES:");
                    analisis.AppendLine($"   • Costo por litro: {costoPorLitroSinIVA:C2} SIN IVA");
                }
                else if (unidad == "Gramos" && cantidad >= 1000)
                {
                    decimal costoPorKgSinIVA = costoPorUnidadSinIVA * 1000;
                    analisis.AppendLine($"\n🔄 CONVERSIONES ÚTILES:");
                    analisis.AppendLine($"   • Costo por kilogramo: {costoPorKgSinIVA:C2} SIN IVA");
                }
                else if (unidad == "Centimetros" && cantidad >= 100)
                {
                    decimal costoPorMetroSinIVA = costoPorUnidadSinIVA * 100;
                    analisis.AppendLine($"\n🔄 CONVERSIONES ÚTILES:");
                    analisis.AppendLine($"   • Costo por metro: {costoPorMetroSinIVA:C2} SIN IVA");
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
                if (TxtCostoPorUnidadConIVA != null) TxtCostoPorUnidadConIVA.Text = "$0.00/ml CON IVA";
                if (TxtCostoPorUnidadSinIVA != null) TxtCostoPorUnidadSinIVA.Text = "$0.00/ml SIN IVA";
                if (TxtConversiones != null) TxtConversiones.Text = "Conversiones aparecerán aquí";
                if (TxtStockFinal != null) TxtStockFinal.Text = "0.00 ml";
                if (TxtAnalisis != null) TxtAnalisis.Text = "💡 Complete los datos para ver el análisis...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en LimpiarResultados: {ex.Message}");
            }
        }

        private string ObtenerUnidadCorta(string unidad)
        {
            try
            {
                return unidad switch
                {
                    "Mililitros" => "ml",
                    "Litros" => "L",
                    "Gramos" => "g",
                    "Kilos" => "kg",
                    "Centimetros" => "cm",
                    "Metros" => "m",
                    _ => unidad?.ToLower() ?? "u"
                };
            }
            catch
            {
                return "u";
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

                MessageBox.Show("✅ Material a granel guardado exitosamente!\n\n" +
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

            if (!decimal.TryParse(TxtCantidadTotal?.Text, out decimal cantidad) || cantidad <= 0)
            {
                MessageBox.Show("La cantidad debe ser un número válido mayor a 0.", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidadTotal?.Focus();
                return false;
            }

            if (!decimal.TryParse(TxtPrecioTotal?.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("El precio debe ser un número válido mayor a 0.", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrecioTotal?.Focus();
                return false;
            }

            if (!decimal.TryParse(TxtIVA?.Text, out decimal iva) || iva < 0)
            {
                MessageBox.Show("El porcentaje de IVA debe ser un número válido mayor o igual a 0.", "Validación",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtIVA?.Focus();
                return false;
            }

            return true;
        }

        private async Task GuardarMaterial()
        {
            // Obtener valores de forma segura
            decimal cantidadTotal = decimal.Parse(TxtCantidadTotal.Text);
            decimal precioTotal = decimal.Parse(TxtPrecioTotal.Text);
            decimal porcentajeIVA = decimal.Parse(TxtIVA.Text) / 100m;
            bool incluyeIVA = ChkIncludeIVA?.IsChecked == true;
            decimal stockAnterior = decimal.TryParse(TxtStockAnterior?.Text, out decimal sa) ? sa : 0;
            decimal alertaMinimo = decimal.TryParse(TxtAlertaMinimo?.Text, out decimal am) ? am : 0;
            string unidad = (CmbUnidadMedida?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mililitros";
            string unidadCorta = ObtenerUnidadCorta(unidad);

            // Calcular precios totales
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

            // CORRECCIÓN: Calcular costo POR UNIDAD correctamente
            decimal costoPorUnidadConIVA = precioConIVA / cantidadTotal;
            decimal costoPorUnidadSinIVA = precioSinIVA / cantidadTotal;

            var material = new RawMaterial
            {
                // Información básica
                NombreArticulo = TxtNombre?.Text?.Trim() ?? "",
                Categoria = CmbCategoria?.Text?.Trim() ?? "",
                Proveedor = TxtProveedor?.Text?.Trim() ?? "",
                CodigoBarras = TxtCodigoBarras?.Text?.Trim() ?? "",

                // Unidades
                UnidadMedida = unidadCorta,
                UnidadBase = unidadCorta,
                FactorConversion = 1,

                // Stock
                StockAntiguo = stockAnterior,
                StockNuevo = cantidadTotal,
                AlertaStockBajo = alertaMinimo,

                // CORRECCIÓN: Guardar precios POR UNIDAD, NO totales
                PrecioConIVA = costoPorUnidadConIVA,     // Precio por unidad CON IVA
                PrecioSinIVA = costoPorUnidadSinIVA,     // Precio por unidad SIN IVA
                PrecioPorUnidad = costoPorUnidadSinIVA,
                PrecioPorUnidadBase = costoPorUnidadSinIVA,
                PrecioBaseConIVA = costoPorUnidadConIVA,
                PrecioBaseSinIVA = costoPorUnidadSinIVA,

                // Metadatos
                Observaciones = GenerarObservaciones(cantidadTotal, precioTotal, unidad, incluyeIVA),
                FechaCreacion = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            // Guardar material
            _context.RawMaterials.Add(material);
            await _context.SaveChangesAsync();

            // CORRECCIÓN: Crear movimiento con precios POR UNIDAD
            var movimiento = new Movimiento
            {
                RawMaterialId = material.Id,
                TipoMovimiento = "Creación",
                Cantidad = cantidadTotal,
                Motivo = $"Creación inicial - Material a granel: {cantidadTotal:F2} {unidadCorta} por {precioTotal:C2} {(incluyeIVA ? "(incluye IVA)" : "(sin IVA)")}",
                Usuario = Environment.UserName,
                PrecioConIVA = costoPorUnidadConIVA,  // Precio POR UNIDAD
                PrecioSinIVA = costoPorUnidadSinIVA,  // Precio POR UNIDAD
                UnidadMedida = unidadCorta,
                FechaMovimiento = DateTime.Now
            };

            _context.Movimientos.Add(movimiento);
            await _context.SaveChangesAsync();

            // Configurar motivo para el formulario principal (si lo necesita)
            MotivoCreacion = $"Material a granel creado: {cantidadTotal:F2} {unidadCorta}";
        }

        private string GenerarObservaciones(decimal cantidad, decimal precio, string unidad, bool incluyeIVA)
        {
            var obs = new System.Text.StringBuilder();

            // Agregar observaciones existentes si las hay
            if (!string.IsNullOrWhiteSpace(TxtObservaciones?.Text))
            {
                obs.AppendLine(TxtObservaciones.Text.Trim());
                obs.AppendLine();
            }

            obs.AppendLine($"--- CREACIÓN A GRANEL [{DateTime.Now:dd/MM/yyyy HH:mm}] ---");
            obs.AppendLine($"Cantidad inicial: {cantidad:F2} {unidad.ToLower()}");
            obs.AppendLine($"Precio total: {precio:C2} {(incluyeIVA ? "(incluye IVA)" : "(sin IVA)")}");
            obs.AppendLine($"Costo por unidad calculado automáticamente");
            obs.AppendLine($"Tipo: Material a granel");

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