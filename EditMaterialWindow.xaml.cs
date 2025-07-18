using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using costbenefi.Data;
using costbenefi.Models;
using Microsoft.EntityFrameworkCore;

namespace costbenefi.Views
{
    public partial class EditAddStockWindow : Window
    {
        private readonly AppDbContext _context;
        private RawMaterial _materialOriginal;
        private bool _esNuevo;
        private bool _isUpdatingCalculations = false;

        public string MotivoEdicion { get; private set; } = "";

        public EditAddStockWindow(AppDbContext context, RawMaterial material)
        {
            InitializeComponent();
            _context = context;

            if (material == null)
            {
                MessageBox.Show("Error: Se requiere un material para continuar.", "Error Fatal",
                    MessageBoxButton.OK, MessageBoxImage.Stop);
                Close();
                return;
            }

            _materialOriginal = material;
            _esNuevo = false;
            Loaded += (s, e) => CargarDatosMaterialEnUI();
        }

        #region CARGA DE DATOS Y CONFIGURACIÓN INICIAL

        private void CargarDatosMaterialEnUI()
        {
            if (_materialOriginal == null) return;

            try
            {
                // Actualizar título y header
                Title = $"Gestión de: {_materialOriginal.NombreArticulo}";
                TxtTitulo.Text = $"📦 {_materialOriginal.NombreArticulo?.ToUpper()}";
                TxtSubtitulo.Text = $"ID: {_materialOriginal.Id} | Stock Actual: {_materialOriginal.StockTotal:F2} {_materialOriginal.UnidadMedida}";

                // Cargar información básica
                TxtId.Text = _materialOriginal.Id.ToString();
                TxtNombre.Text = _materialOriginal.NombreArticulo ?? "";
                CmbCategoria.Text = _materialOriginal.Categoria ?? "";
                TxtProveedor.Text = _materialOriginal.Proveedor ?? "";
                TxtCodigoBarras.Text = _materialOriginal.CodigoBarras ?? "";
                TxtObservaciones.Text = _materialOriginal.Observaciones ?? "";

                // Cargar información de stock
                TxtStockActual.Text = _materialOriginal.StockTotal.ToString("F2");
                TxtUnidad.Text = _materialOriginal.UnidadMedida ?? "kg";
                TxtUnidadAgregar.Text = _materialOriginal.UnidadMedida ?? "kg";
                TxtUnidadQuitar.Text = _materialOriginal.UnidadMedida ?? "kg";

                // Cargar precios
                TxtPrecioPorUnidadConIVA.Text = _materialOriginal.PrecioConIVA.ToString("F4");
                TxtPrecioPorUnidadSinIVA.Text = _materialOriginal.PrecioSinIVA.ToString("F4");
                TxtAlertaMinimo.Text = _materialOriginal.AlertaStockBajo.ToString("F2");

                // Configurar valores por defecto para agregar stock
                TxtCantidadAgregar.Text = "0";
                TxtPrecioTotalAgregar.Text = "0";
                TxtIVAAgregar.Text = "16";
                ChkIncluyeIVAAgregar.IsChecked = true;

                // Configurar valores por defecto para quitar stock
                TxtCantidadQuitar.Text = "0";
                CmbRazonQuitar.SelectedIndex = -1;

                // Configurar event handlers adicionales
                CmbRazonQuitar.SelectionChanged += CmbRazonQuitar_SelectionChanged;

                ActualizarValoresTotales();
                VerificarAlertaStockBajo();
                ActualizarCalculosAgregar();
                ActualizarCalculosQuitar();

                // Habilitar botones
                BtnAgregarStock.IsEnabled = true;
                BtnQuitarStock.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar material: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region CÁLCULOS Y ACTUALIZACIONES DE UI

        private void ActualizarValoresTotales()
        {
            try
            {
                if (_materialOriginal == null) return;

                TxtValorTotalConIVA.Text = _materialOriginal.ValorTotalConIVA.ToString("C2");
                TxtValorTotalSinIVA.Text = _materialOriginal.ValorTotalSinIVA.ToString("C2");
                TxtIVACalculado.Text = _materialOriginal.DiferenciaIVATotal.ToString("C2");

                // Calcular y mostrar porcentaje de IVA
                if (_materialOriginal.PrecioSinIVA > 0)
                {
                    decimal porcentajeIVA = ((_materialOriginal.PrecioConIVA - _materialOriginal.PrecioSinIVA) / _materialOriginal.PrecioSinIVA) * 100;
                    TxtPorcentajeIVA.Text = $"{porcentajeIVA:F1}%";
                }
                else
                {
                    TxtPorcentajeIVA.Text = "0.0%";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar valores totales: {ex.Message}", "Error de Cálculo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void VerificarAlertaStockBajo()
        {
            try
            {
                if (_materialOriginal.TieneStockBajo)
                {
                    TxtStockActual.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    TxtStockActual.Foreground = new SolidColorBrush(Colors.White);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al verificar alerta de stock: {ex.Message}", "Error de Verificación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ActualizarCalculosAgregar()
        {
            if (_isUpdatingCalculations) return;

            try
            {
                if (!decimal.TryParse(TxtCantidadAgregar.Text, out decimal cantidad) || cantidad <= 0)
                {
                    TxtResumenAgregar.Text = "💡 Ingrese una cantidad válida para agregar...";
                    BtnAgregarStock.IsEnabled = false;
                    return;
                }

                if (!decimal.TryParse(TxtPrecioTotalAgregar.Text, out decimal precioTotal) || precioTotal <= 0)
                {
                    TxtResumenAgregar.Text = "💡 Ingrese un precio total válido...";
                    BtnAgregarStock.IsEnabled = false;
                    return;
                }

                decimal porcentajeIVA = decimal.TryParse(TxtIVAAgregar.Text, out decimal iva) ? iva : 16;
                bool incluyeIVA = ChkIncluyeIVAAgregar.IsChecked == true;

                decimal factorIVA = 1 + (porcentajeIVA / 100);
                decimal precioConIVA, precioSinIVA;

                if (incluyeIVA)
                {
                    precioConIVA = precioTotal;
                    precioSinIVA = precioTotal / factorIVA;
                }
                else
                {
                    precioSinIVA = precioTotal;
                    precioConIVA = precioTotal * factorIVA;
                }

                decimal precioUnitarioConIVA = precioConIVA / cantidad;
                decimal precioUnitarioSinIVA = precioSinIVA / cantidad;
                decimal stockResultante = _materialOriginal.StockTotal + cantidad;
                decimal valorTotalResultante = stockResultante * precioUnitarioConIVA;

                TxtResumenAgregar.Text = $"✅ RESUMEN DE ENTRADA:\n" +
                                       $"• Cantidad: {cantidad:F2} {_materialOriginal.UnidadMedida}\n" +
                                       $"• Precio unitario (c/IVA): {precioUnitarioConIVA:C4}\n" +
                                       $"• Precio unitario (s/IVA): {precioUnitarioSinIVA:C4}\n" +
                                       $"• Stock resultante: {stockResultante:F2} {_materialOriginal.UnidadMedida}\n" +
                                       $"• Valor total inventario: {valorTotalResultante:C2}";

                BtnAgregarStock.IsEnabled = true;
            }
            catch (Exception ex)
            {
                TxtResumenAgregar.Text = "❌ Error en cálculos. Verifique los datos.";
                BtnAgregarStock.IsEnabled = false;
            }
        }

        private void ActualizarCalculosQuitar()
        {
            try
            {
                if (!decimal.TryParse(TxtCantidadQuitar.Text, out decimal cantidad) || cantidad <= 0)
                {
                    TxtResumenQuitar.Text = "⚠️ Ingrese una cantidad válida a quitar...";
                    BtnQuitarStock.IsEnabled = false;
                    return;
                }

                if (cantidad > _materialOriginal.StockTotal)
                {
                    TxtResumenQuitar.Text = $"❌ STOCK INSUFICIENTE\n" +
                                          $"Solicitado: {cantidad:F2} {_materialOriginal.UnidadMedida}\n" +
                                          $"Disponible: {_materialOriginal.StockTotal:F2} {_materialOriginal.UnidadMedida}";
                    BtnQuitarStock.IsEnabled = false;
                    return;
                }

                decimal stockResultante = _materialOriginal.StockTotal - cantidad;
                decimal valorAfectado = cantidad * _materialOriginal.PrecioConIVA;
                string razon = (CmbRazonQuitar.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Razón no seleccionada";

                TxtResumenQuitar.Text = $"⚠️ RESUMEN DE SALIDA:\n" +
                                      $"• Cantidad a quitar: {cantidad:F2} {_materialOriginal.UnidadMedida}\n" +
                                      $"• Stock resultante: {stockResultante:F2} {_materialOriginal.UnidadMedida}\n" +
                                      $"• Valor afectado: {valorAfectado:C2}\n" +
                                      $"• Razón: {razon}";

                BtnQuitarStock.IsEnabled = CmbRazonQuitar.SelectedItem != null;
            }
            catch (Exception ex)
            {
                TxtResumenQuitar.Text = "❌ Error en cálculos de salida.";
                BtnQuitarStock.IsEnabled = false;
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void PrecioChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && !_isUpdatingCalculations)
            {
                ActualizarValoresTotales();
                VerificarAlertaStockBajo();
            }
        }

        private void CantidadAgregarChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) ActualizarCalculosAgregar();
        }

        private void PrecioAgregarChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) ActualizarCalculosAgregar();
        }

        private void IVAAgregarChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) ActualizarCalculosAgregar();
        }

        private void IVATextBoxChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) ActualizarCalculosAgregar();
        }

        private void CantidadQuitarChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) ActualizarCalculosQuitar();
        }

        private void CmbRazonQuitar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) ActualizarCalculosQuitar();
        }

        #endregion

        #region OPERACIONES DE STOCK

        private async void BtnAgregarStock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarDatosAgregar()) return;

                decimal cantidadAgregar = decimal.Parse(TxtCantidadAgregar.Text);
                decimal precioTotal = decimal.Parse(TxtPrecioTotalAgregar.Text);
                decimal porcentajeIVA = decimal.Parse(TxtIVAAgregar.Text);
                bool incluyeIVA = ChkIncluyeIVAAgregar.IsChecked == true;

                // Calcular precios unitarios
                decimal factorIVA = 1 + (porcentajeIVA / 100);
                decimal precioConIVA, precioSinIVA;

                if (incluyeIVA)
                {
                    precioConIVA = precioTotal;
                    precioSinIVA = precioTotal / factorIVA;
                }
                else
                {
                    precioSinIVA = precioTotal;
                    precioConIVA = precioTotal * factorIVA;
                }

                decimal precioUnitarioConIVA = precioConIVA / cantidadAgregar;
                decimal precioUnitarioSinIVA = precioSinIVA / cantidadAgregar;

                // Confirmar la operación
                var result = MessageBox.Show(
                    $"¿Confirmar entrada de stock?\n\n" +
                    $"Producto: {_materialOriginal.NombreArticulo}\n" +
                    $"Cantidad a agregar: {cantidadAgregar:F2} {_materialOriginal.UnidadMedida}\n" +
                    $"Precio total: {precioTotal:C2} ({(incluyeIVA ? "con" : "sin")} IVA)\n" +
                    $"Precio unitario: {precioUnitarioConIVA:C4} (c/IVA)\n" +
                    $"Stock actual: {_materialOriginal.StockTotal:F2}\n" +
                    $"Stock resultante: {(_materialOriginal.StockTotal + cantidadAgregar):F2}",
                    "Confirmar Entrada", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                // Obtener el material más reciente de la base de datos
                var materialDB = await _context.RawMaterials.FindAsync(_materialOriginal.Id);
                if (materialDB == null)
                {
                    MessageBox.Show("Error: No se encontró el material en la base de datos.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // CORRECCIÓN: Sumar al stock existente, no reemplazar
                decimal stockAnterior = materialDB.StockTotal;
                materialDB.StockNuevo += cantidadAgregar;

                // Actualizar precios con promedio ponderado
                if (stockAnterior > 0)
                {
                    // Promedio ponderado
                    decimal valorAnteriorConIVA = stockAnterior * materialDB.PrecioConIVA;
                    decimal valorNuevoConIVA = cantidadAgregar * precioUnitarioConIVA;
                    decimal valorTotalConIVA = valorAnteriorConIVA + valorNuevoConIVA;

                    materialDB.PrecioConIVA = valorTotalConIVA / (stockAnterior + cantidadAgregar);
                    materialDB.PrecioSinIVA = materialDB.PrecioConIVA / factorIVA;
                }
                else
                {
                    // Primer stock, usar precios directos
                    materialDB.PrecioConIVA = precioUnitarioConIVA;
                    materialDB.PrecioSinIVA = precioUnitarioSinIVA;
                }

                materialDB.PrecioPorUnidad = materialDB.PrecioSinIVA;
                materialDB.FechaActualizacion = DateTime.Now;

                // Crear movimiento
                var movimiento = new Movimiento
                {
                    RawMaterialId = materialDB.Id,
                    TipoMovimiento = "Entrada",
                    Cantidad = cantidadAgregar,
                    Motivo = $"Entrada de stock - Compra por {precioTotal:C2} ({(incluyeIVA ? "con" : "sin")} IVA)",
                    Usuario = Environment.UserName,
                    PrecioConIVA = precioUnitarioConIVA,
                    PrecioSinIVA = precioUnitarioSinIVA,
                    UnidadMedida = materialDB.UnidadMedida,
                    FechaMovimiento = DateTime.Now
                };

                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();

                // Actualizar el objeto local
                _materialOriginal.StockNuevo = materialDB.StockNuevo;
                _materialOriginal.PrecioConIVA = materialDB.PrecioConIVA;
                _materialOriginal.PrecioSinIVA = materialDB.PrecioSinIVA;
                _materialOriginal.PrecioPorUnidad = materialDB.PrecioPorUnidad;

                MotivoEdicion = $"Entrada: +{cantidadAgregar:F2} {materialDB.UnidadMedida}";

                // Limpiar campos y actualizar UI
                TxtCantidadAgregar.Text = "0";
                TxtPrecioTotalAgregar.Text = "0";
                TxtStockActual.Text = _materialOriginal.StockTotal.ToString("F2");
                TxtSubtitulo.Text = $"ID: {_materialOriginal.Id} | Stock Actual: {_materialOriginal.StockTotal:F2} {_materialOriginal.UnidadMedida}";

                ActualizarValoresTotales();
                ActualizarCalculosAgregar();

                MessageBox.Show($"✅ Entrada registrada exitosamente!\n\n" +
                              $"Cantidad agregada: {cantidadAgregar:F2} {materialDB.UnidadMedida}\n" +
                              $"Stock actual: {_materialOriginal.StockTotal:F2} {materialDB.UnidadMedida}\n" +
                              $"Nuevo precio unitario: {materialDB.PrecioConIVA:C4}",
                              "Entrada Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar stock: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnQuitarStock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarDatosQuitar()) return;

                decimal cantidadQuitar = decimal.Parse(TxtCantidadQuitar.Text);
                string razon = ((ComboBoxItem)CmbRazonQuitar.SelectedItem).Content.ToString();

                // Confirmar la operación
                var result = MessageBox.Show(
                    $"¿Confirmar salida de stock?\n\n" +
                    $"Producto: {_materialOriginal.NombreArticulo}\n" +
                    $"Cantidad a quitar: {cantidadQuitar:F2} {_materialOriginal.UnidadMedida}\n" +
                    $"Razón: {razon}\n" +
                    $"Stock actual: {_materialOriginal.StockTotal:F2}\n" +
                    $"Stock resultante: {(_materialOriginal.StockTotal - cantidadQuitar):F2}\n\n" +
                    "Esta acción no se puede deshacer.",
                    "Confirmar Salida", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                // Obtener el material más reciente de la base de datos
                var materialDB = await _context.RawMaterials.FindAsync(_materialOriginal.Id);
                if (materialDB == null)
                {
                    MessageBox.Show("Error: No se encontró el material en la base de datos.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // CORRECCIÓN: Restar del stock de manera correcta
                decimal cantidadRestante = cantidadQuitar;

                if (materialDB.StockNuevo >= cantidadRestante)
                {
                    materialDB.StockNuevo -= cantidadRestante;
                }
                else
                {
                    cantidadRestante -= materialDB.StockNuevo;
                    materialDB.StockNuevo = 0;
                    materialDB.StockAntiguo -= cantidadRestante;
                }

                materialDB.FechaActualizacion = DateTime.Now;

                // Crear movimiento
                var movimiento = new Movimiento
                {
                    RawMaterialId = materialDB.Id,
                    TipoMovimiento = "Salida",
                    Cantidad = cantidadQuitar,
                    Motivo = $"Salida por: {razon}",
                    Usuario = Environment.UserName,
                    PrecioConIVA = materialDB.PrecioConIVA,
                    PrecioSinIVA = materialDB.PrecioSinIVA,
                    UnidadMedida = materialDB.UnidadMedida,
                    FechaMovimiento = DateTime.Now
                };

                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();

                // Actualizar el objeto local
                _materialOriginal.StockAntiguo = materialDB.StockAntiguo;
                _materialOriginal.StockNuevo = materialDB.StockNuevo;

                MotivoEdicion = $"Salida: -{cantidadQuitar:F2} {materialDB.UnidadMedida} ({razon})";

                // Limpiar campos y actualizar UI
                TxtCantidadQuitar.Text = "0";
                CmbRazonQuitar.SelectedIndex = -1;
                TxtStockActual.Text = _materialOriginal.StockTotal.ToString("F2");
                TxtSubtitulo.Text = $"ID: {_materialOriginal.Id} | Stock Actual: {_materialOriginal.StockTotal:F2} {_materialOriginal.UnidadMedida}";

                ActualizarValoresTotales();
                VerificarAlertaStockBajo();
                ActualizarCalculosQuitar();

                MessageBox.Show($"✅ Salida registrada exitosamente!\n\n" +
                              $"Cantidad retirada: {cantidadQuitar:F2} {materialDB.UnidadMedida}\n" +
                              $"Razón: {razon}\n" +
                              $"Stock actual: {_materialOriginal.StockTotal:F2} {materialDB.UnidadMedida}",
                              "Salida Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al quitar stock: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region VALIDACIONES

        private bool ValidarDatosAgregar()
        {
            if (!decimal.TryParse(TxtCantidadAgregar.Text, out decimal cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a 0.", "Datos Inválidos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidadAgregar.Focus();
                return false;
            }

            if (!decimal.TryParse(TxtPrecioTotalAgregar.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Ingrese un precio total válido mayor a 0.", "Datos Inválidos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrecioTotalAgregar.Focus();
                return false;
            }

            if (!decimal.TryParse(TxtIVAAgregar.Text, out decimal iva) || iva < 0)
            {
                MessageBox.Show("Ingrese un porcentaje de IVA válido (0 o mayor).", "Datos Inválidos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtIVAAgregar.Focus();
                return false;
            }

            return true;
        }

        private bool ValidarDatosQuitar()
        {
            if (!decimal.TryParse(TxtCantidadQuitar.Text, out decimal cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a 0.", "Datos Inválidos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidadQuitar.Focus();
                return false;
            }

            if (cantidad > _materialOriginal.StockTotal)
            {
                MessageBox.Show($"La cantidad excede el stock disponible.\n\n" +
                              $"Solicitado: {cantidad:F2}\n" +
                              $"Disponible: {_materialOriginal.StockTotal:F2}",
                              "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidadQuitar.Focus();
                return false;
            }

            if (CmbRazonQuitar.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una razón para la salida de stock.", "Datos Inválidos",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbRazonQuitar.Focus();
                return false;
            }

            return true;
        }

        #endregion

        #region BOTONES PRINCIPALES

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener el material más reciente de la base de datos
                var materialDB = await _context.RawMaterials.FindAsync(_materialOriginal.Id);
                if (materialDB == null)
                {
                    MessageBox.Show("Error: No se encontró el material en la base de datos.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Actualizar solo la información básica (no stock ni precios)
                bool huboChangios = false;

                if (materialDB.NombreArticulo != TxtNombre.Text.Trim())
                {
                    materialDB.NombreArticulo = TxtNombre.Text.Trim();
                    huboChangios = true;
                }

                if (materialDB.Categoria != CmbCategoria.Text.Trim())
                {
                    materialDB.Categoria = CmbCategoria.Text.Trim();
                    huboChangios = true;
                }

                if (materialDB.Proveedor != TxtProveedor.Text.Trim())
                {
                    materialDB.Proveedor = TxtProveedor.Text.Trim();
                    huboChangios = true;
                }

                if (materialDB.CodigoBarras != TxtCodigoBarras.Text.Trim())
                {
                    materialDB.CodigoBarras = TxtCodigoBarras.Text.Trim();
                    huboChangios = true;
                }

                if (decimal.TryParse(TxtAlertaMinimo.Text, out decimal alertaMinimo) &&
                    materialDB.AlertaStockBajo != alertaMinimo)
                {
                    materialDB.AlertaStockBajo = alertaMinimo;
                    huboChangios = true;
                }

                if (materialDB.Observaciones != TxtObservaciones.Text.Trim())
                {
                    materialDB.Observaciones = TxtObservaciones.Text.Trim();
                    huboChangios = true;
                }

                if (huboChangios)
                {
                    materialDB.FechaActualizacion = DateTime.Now;

                    // Crear movimiento de edición solo si hubo cambios
                    var movimiento = new Movimiento
                    {
                        RawMaterialId = materialDB.Id,
                        TipoMovimiento = "Edición",
                        Cantidad = 0,
                        Motivo = "Actualización de información del producto",
                        Usuario = Environment.UserName,
                        PrecioConIVA = materialDB.PrecioConIVA,
                        PrecioSinIVA = materialDB.PrecioSinIVA,
                        UnidadMedida = materialDB.UnidadMedida,
                        FechaMovimiento = DateTime.Now
                    };

                    _context.Movimientos.Add(movimiento);
                    MotivoEdicion = "Información actualizada";
                }

                await _context.SaveChangesAsync();

                if (huboChangios)
                {
                    MessageBox.Show("✅ Información del producto actualizada correctamente.", "Guardado Exitoso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("ℹ️ No se detectaron cambios para guardar.", "Sin Cambios",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro de que desea cancelar?\n\nSe perderán todos los cambios no guardados.",
                "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        #endregion

        #region MÉTODOS DE AYUDA

        private void ShowHelp_InfoBasica(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "📋 Información Básica del Producto\n\n" +
                "• ID: Identificador único del material (solo lectura)\n" +
                "• Nombre: Denominación comercial o técnica\n" +
                "• Categoría: Clasificación para organización\n" +
                "• Proveedor: Empresa o persona que suministra\n" +
                "• Código: Código de barras o referencia interna",
                "Ayuda - Información Básica",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHelp_GestionStock(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "📦 Gestión de Stock\n\n" +
                "• Use 'Agregar' para compras y entradas\n" +
                "• Use 'Quitar' para consumos y salidas\n" +
                "• Los precios se calculan por promedio ponderado\n" +
                "• Siempre especifique la razón del movimiento\n" +
                "• Todos los movimientos se registran automáticamente",
                "Ayuda - Gestión de Stock",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHelp_CantidadAgregar(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "➕ Cantidad a Agregar\n\n" +
                "• Unidades que ingresan al inventario\n" +
                "• Use decimales si es necesario (ej: 1.5)\n" +
                "• La unidad de medida se muestra automáticamente\n" +
                "• Debe ser mayor a 0",
                "Ayuda - Cantidad a Agregar",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHelp_PrecioTotal(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "💰 Precio Total Pagado\n\n" +
                "• Monto total de la compra (no por unidad)\n" +
                "• El sistema calculará el precio unitario automáticamente\n" +
                "• Marque 'Inc. IVA' si el precio incluye impuestos\n" +
                "• Use punto decimal para centavos (ej: 1250.50)",
                "Ayuda - Precio Total",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowHelp_IVA(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "📊 Porcentaje de IVA\n\n" +
                "• Impuesto aplicable al material\n" +
                "• En México típicamente 16%\n" +
                "• Puede variar según el tipo de producto\n" +
                "• Use solo números (ej: 16 para 16%)",
                "Ayuda - IVA",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Métodos de ayuda adicionales (todos apuntan a los principales)
        private void ShowHelp_Nombre(object sender, RoutedEventArgs e) { ShowHelp_InfoBasica(sender, e); }
        private void ShowHelp_Categoria(object sender, RoutedEventArgs e) { ShowHelp_InfoBasica(sender, e); }
        private void ShowHelp_Proveedor(object sender, RoutedEventArgs e) { ShowHelp_InfoBasica(sender, e); }
        private void ShowHelp_CodigoBarras(object sender, RoutedEventArgs e) { ShowHelp_InfoBasica(sender, e); }
        private void ShowHelp_CantidadQuitar(object sender, RoutedEventArgs e) { ShowHelp_GestionStock(sender, e); }
        private void ShowHelp_RazonQuitar(object sender, RoutedEventArgs e) { ShowHelp_GestionStock(sender, e); }
        private void ShowHelp_StockActual(object sender, RoutedEventArgs e) { ShowHelp_GestionStock(sender, e); }
        private void ShowHelp_StockMinimo(object sender, RoutedEventArgs e) { ShowHelp_GestionStock(sender, e); }
        private void ShowHelp_Precios(object sender, RoutedEventArgs e) { ShowHelp_PrecioTotal(sender, e); }
        private void ShowHelp_PrecioSinIVA(object sender, RoutedEventArgs e) { ShowHelp_PrecioTotal(sender, e); }
        private void ShowHelp_PrecioConIVA(object sender, RoutedEventArgs e) { ShowHelp_PrecioTotal(sender, e); }
        private void ShowHelp_ValorTotal(object sender, RoutedEventArgs e) { ShowHelp_GestionStock(sender, e); }
        private void ShowHelp_Observaciones(object sender, RoutedEventArgs e) { ShowHelp_InfoBasica(sender, e); }

        #endregion

        #region FUNCIONES ADICIONALES

        private void VerBitacora_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "🚧 Funcionalidad en Desarrollo\n\n" +
                "La ventana de bitácora completa estará disponible próximamente.\n" +
                "Por ahora puede ver el historial en el módulo de Reportes > Movimientos.",
                "Bitácora", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "🚧 Funcionalidad en Desarrollo\n\n" +
                "La generación de reportes individuales estará disponible próximamente.\n" +
                "Por ahora puede usar el módulo de Reportes en la ventana principal.",
                "Reportes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}