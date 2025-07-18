using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

// ===== USING PARA EL SISTEMA PDF =====
using costbenefi.Services;
using costbenefi.Helpers;

namespace costbenefi.Views
{
    public partial class ReporteStockWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly StockPDFService _pdfService; // ⬅️ Servicio PDF iText7
        private List<RawMaterial> _todosLosMateriales;
        private List<RawMaterial> _materialesFiltrados;
        private bool _cargaCompleta = false;
        private bool _filtrosExpandidos = false;

        public ReporteStockWindow(AppDbContext context)
        {
            _context = context;
            _pdfService = new StockPDFService(_context); // ⬅️ Inicializar servicio PDF
            InitializeComponent();
            CargarDatosIniciales();
        }

        private async void CargarDatosIniciales()
        {
            try
            {
                // Cargar todos los productos activos
                _todosLosMateriales = await _context.RawMaterials
                    .Where(m => !m.Eliminado)
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                if (!_todosLosMateriales.Any())
                {
                    MessageBox.Show("No hay productos registrados en el sistema.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Cargar los controles de filtrado
                CargarFiltrosAvanzados();

                // Mostrar todos los datos inicialmente
                _materialesFiltrados = new List<RawMaterial>(_todosLosMateriales);
                ActualizarDataGrid();
                ActualizarEstadisticas();

                // Marcar como carga completa
                _cargaCompleta = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarFiltrosAvanzados()
        {
            try
            {
                // Cargar productos en el popup
                PanelProductos.Children.Clear();
                foreach (var material in _todosLosMateriales)
                {
                    var checkbox = new CheckBox
                    {
                        Content = material.NombreArticulo,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        Tag = material,
                        FontSize = 11
                    };
                    checkbox.Checked += ProductoCheckbox_Changed;
                    checkbox.Unchecked += ProductoCheckbox_Changed;
                    PanelProductos.Children.Add(checkbox);
                }

                // Cargar categorías únicas
                PanelCategorias.Children.Clear();
                var categorias = _todosLosMateriales
                    .Select(m => m.Categoria)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c);

                foreach (var categoria in categorias)
                {
                    var checkbox = new CheckBox
                    {
                        Content = categoria,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        FontSize = 11
                    };
                    checkbox.Checked += FiltroCheckbox_Changed;
                    checkbox.Unchecked += FiltroCheckbox_Changed;
                    PanelCategorias.Children.Add(checkbox);
                }

                // Cargar proveedores únicos
                PanelProveedores.Children.Clear();
                var proveedores = _todosLosMateriales
                    .Select(m => m.Proveedor)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .OrderBy(p => p);

                foreach (var proveedor in proveedores)
                {
                    var checkbox = new CheckBox
                    {
                        Content = proveedor,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        FontSize = 11
                    };
                    checkbox.Checked += FiltroCheckbox_Changed;
                    checkbox.Unchecked += FiltroCheckbox_Changed;
                    PanelProveedores.Children.Add(checkbox);
                }

                ActualizarContadorProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar filtros: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Event Handlers

        private void BtnToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtrosExpandidos = !_filtrosExpandidos;
            PanelFiltrosDetallados.Visibility = _filtrosExpandidos ? Visibility.Visible : Visibility.Collapsed;
            BtnToggleFiltros.Content = _filtrosExpandidos ? "🔍 Ocultar Filtros" : "🔍 Mostrar Filtros";
        }

        private void BtnFiltroTodos_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
        }

        private void BtnFiltroStockBajo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            ChkSoloStockBajo.IsChecked = true;
            AplicarFiltros();
        }

        private void BtnFiltroAltoValor_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            if (_todosLosMateriales?.Any() == true)
            {
                var promedio = _todosLosMateriales.Average(m => m.ValorTotalConIVA);
                ChkFiltrarPorValor.IsChecked = true;
                TxtValorMin.Text = (promedio * 1.5m).ToString("F2");
                AplicarFiltros();
            }
        }

        private void BtnProductos_Click(object sender, RoutedEventArgs e)
        {
            PopupProductos.IsOpen = !PopupProductos.IsOpen;
        }

        private void BtnCategorias_Click(object sender, RoutedEventArgs e)
        {
            PopupCategorias.IsOpen = !PopupCategorias.IsOpen;
        }

        private void BtnProveedores_Click(object sender, RoutedEventArgs e)
        {
            PopupProveedores.IsOpen = !PopupProveedores.IsOpen;
        }

        private void BtnAplicarProductos_Click(object sender, RoutedEventArgs e)
        {
            PopupProductos.IsOpen = false;
            AplicarFiltros();
        }

        private void BtnAplicarCategorias_Click(object sender, RoutedEventArgs e)
        {
            PopupCategorias.IsOpen = false;
            AplicarFiltros();
        }

        private void BtnAplicarProveedores_Click(object sender, RoutedEventArgs e)
        {
            PopupProveedores.IsOpen = false;
            AplicarFiltros();
        }

        #endregion

        private void ProductoCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_cargaCompleta && PanelProductos != null)
            {
                ActualizarContadorProductos();
            }
        }

        private void FiltroCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_cargaCompleta)
            {
                AplicarFiltros();
            }
        }

        private void ActualizarContadorProductos()
        {
            if (PanelProductos == null || TxtProductosSeleccionados == null) return;

            try
            {
                var seleccionados = PanelProductos.Children.OfType<CheckBox>()
                    .Count(cb => cb.IsChecked == true);
                TxtProductosSeleccionados.Text = seleccionados.ToString();

                // Actualizar texto del botón
                if (BtnProductos != null)
                {
                    var total = PanelProductos.Children.Count;
                    BtnProductos.Content = seleccionados == total ? "Todos seleccionados" :
                                          seleccionados == 0 ? "Ninguno seleccionado" :
                                          $"{seleccionados} de {total}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar contador: {ex.Message}");
            }
        }

        private void ChkTodosProductos_Checked(object sender, RoutedEventArgs e)
        {
            if (!_cargaCompleta || PanelProductos == null) return;

            try
            {
                foreach (CheckBox cb in PanelProductos.Children.OfType<CheckBox>())
                {
                    cb.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar todos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkTodosProductos_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_cargaCompleta || PanelProductos == null) return;

            try
            {
                foreach (CheckBox cb in PanelProductos.Children.OfType<CheckBox>())
                {
                    cb.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al deseleccionar todos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSeleccionarTodos_Click(object sender, RoutedEventArgs e)
        {
            ChkTodosProductos.IsChecked = true;
        }

        private void BtnDeseleccionarTodos_Click(object sender, RoutedEventArgs e)
        {
            ChkTodosProductos.IsChecked = false;
        }

        private void BtnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
        }

        private void AplicarFiltros()
        {
            if (!_cargaCompleta || _todosLosMateriales == null) return;

            try
            {
                var materialesFiltrados = new List<RawMaterial>(_todosLosMateriales);

                // Filtrar por productos seleccionados
                if (PanelProductos != null)
                {
                    var productosSeleccionados = PanelProductos.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true && cb.Tag is RawMaterial)
                        .Select(cb => ((RawMaterial)cb.Tag).Id)
                        .ToList();

                    if (productosSeleccionados.Any())
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => productosSeleccionados.Contains(m.Id))
                            .ToList();
                    }
                }

                // Filtrar por categorías seleccionadas
                if (PanelCategorias != null)
                {
                    var categoriasSeleccionadas = PanelCategorias.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();

                    if (categoriasSeleccionadas.Any())
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => categoriasSeleccionadas.Contains(m.Categoria))
                            .ToList();
                    }
                }

                // Filtrar por proveedores seleccionados
                if (PanelProveedores != null)
                {
                    var proveedoresSeleccionados = PanelProveedores.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();

                    if (proveedoresSeleccionados.Any())
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => proveedoresSeleccionados.Contains(m.Proveedor))
                            .ToList();
                    }
                }

                // Filtrar por rango de stock
                if (ChkFiltrarPorStock?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtStockMin?.Text, out decimal stockMin))
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => m.StockTotal >= stockMin)
                            .ToList();
                    }

                    if (decimal.TryParse(TxtStockMax?.Text, out decimal stockMax))
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => m.StockTotal <= stockMax)
                            .ToList();
                    }
                }

                // Filtrar por rango de valor
                if (ChkFiltrarPorValor?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtValorMin?.Text, out decimal valorMin))
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => m.ValorTotalConIVA >= valorMin)
                            .ToList();
                    }

                    if (decimal.TryParse(TxtValorMax?.Text, out decimal valorMax))
                    {
                        materialesFiltrados = materialesFiltrados
                            .Where(m => m.ValorTotalConIVA <= valorMax)
                            .ToList();
                    }
                }

                // Filtrar solo productos con stock bajo
                if (ChkSoloStockBajo?.IsChecked == true)
                {
                    materialesFiltrados = materialesFiltrados
                        .Where(m => m.TieneStockBajo)
                        .ToList();
                }

                _materialesFiltrados = materialesFiltrados;
                ActualizarDataGrid();
                ActualizarEstadisticas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimpiarFiltros()
        {
            if (!_cargaCompleta) return;

            try
            {
                // Limpiar selección de productos
                if (ChkTodosProductos != null)
                {
                    ChkTodosProductos.IsChecked = true;
                }

                // Seleccionar todas las categorías
                if (PanelCategorias != null)
                {
                    foreach (CheckBox cb in PanelCategorias.Children.OfType<CheckBox>())
                    {
                        cb.IsChecked = true;
                    }
                }

                // Seleccionar todos los proveedores
                if (PanelProveedores != null)
                {
                    foreach (CheckBox cb in PanelProveedores.Children.OfType<CheckBox>())
                    {
                        cb.IsChecked = true;
                    }
                }

                // Limpiar filtros de rango
                if (ChkFiltrarPorStock != null)
                {
                    ChkFiltrarPorStock.IsChecked = false;
                }
                if (TxtStockMin != null) TxtStockMin.Text = "";
                if (TxtStockMax != null) TxtStockMax.Text = "";

                if (ChkFiltrarPorValor != null)
                {
                    ChkFiltrarPorValor.IsChecked = false;
                }
                if (TxtValorMin != null) TxtValorMin.Text = "";
                if (TxtValorMax != null) TxtValorMax.Text = "";

                if (ChkSoloStockBajo != null)
                {
                    ChkSoloStockBajo.IsChecked = false;
                }

                // Cerrar popups
                if (PopupProductos != null) PopupProductos.IsOpen = false;
                if (PopupCategorias != null) PopupCategorias.IsOpen = false;
                if (PopupProveedores != null) PopupProveedores.IsOpen = false;

                // Aplicar filtros limpios
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al limpiar filtros: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarDataGrid()
        {
            if (DgStock == null || _materialesFiltrados == null) return;

            try
            {
                var datosReporte = _materialesFiltrados.Select(m => new
                {
                    Nombre = m.NombreArticulo ?? "",
                    Categoria = m.Categoria ?? "",
                    Stock = m.StockTotal,
                    Unidad = m.UnidadMedida ?? "",
                    PrecioUnitario = m.PrecioConIVA > 0 ? m.PrecioConIVA : m.PrecioPorUnidad,
                    ValorTotal = m.ValorTotalConIVA,
                    StockBajo = m.TieneStockBajo ? "⚠️ Sí" : "✅ No",
                    Proveedor = m.Proveedor ?? "Sin proveedor"
                }).ToList();

                DgStock.ItemsSource = datosReporte;

                // Actualizar resumen detalle
                if (TxtResumenDetalle != null)
                {
                    TxtResumenDetalle.Text = $"Mostrando {datosReporte.Count} de {_todosLosMateriales.Count} productos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarEstadisticas()
        {
            try
            {
                if (_materialesFiltrados?.Any() == true)
                {
                    var totalProductos = _materialesFiltrados.Count;
                    var valorTotal = _materialesFiltrados.Sum(m => m.ValorTotalConIVA);
                    var productosStockBajo = _materialesFiltrados.Count(m => m.TieneStockBajo);

                    if (TxtEstadisticas != null)
                    {
                        TxtEstadisticas.Text = $"📊 Productos: {totalProductos} | Valor: {valorTotal:C0}";
                    }

                    if (productosStockBajo > 0)
                    {
                        if (TxtAlertaStock != null)
                        {
                            TxtAlertaStock.Text = $"⚠️ Stock Bajo: {productosStockBajo}";
                        }
                        if (BorderAlertaStock != null)
                        {
                            BorderAlertaStock.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (BorderAlertaStock != null)
                        {
                            BorderAlertaStock.Visibility = Visibility.Collapsed;
                        }
                    }

                    Title = $"📊 Reporte de Stock - {totalProductos} productos | {valorTotal:C0} | Stock bajo: {productosStockBajo}";
                }
                else
                {
                    if (TxtEstadisticas != null)
                    {
                        TxtEstadisticas.Text = "📊 Productos: 0 | Valor: $0.00";
                    }
                    if (BorderAlertaStock != null)
                    {
                        BorderAlertaStock.Visibility = Visibility.Collapsed;
                    }
                    Title = "📊 Reporte de Stock - Sin datos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar estadísticas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region ===== EXPORTACIÓN PDF CON iTEXT7 =====

        /// <summary>
        /// ✅ CORREGIDO: Generación de PDF usando solo iText7 (sin MiKTeX)
        /// </summary>
        private async void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validar que hay datos para exportar
                if (_materialesFiltrados == null || !_materialesFiltrados.Any())
                {
                    MessageBox.Show("No hay datos para exportar.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Obtener configuración del reporte
                var periodo = ObtenerPeriodoSeleccionado();
                var tipoFormato = ObtenerTipoFormatoSeleccionado();

                // 3. Preparar filtros aplicados
                var filtrosAplicados = PrepararFiltrosAplicados();

                // 4. Mostrar confirmación
                var confirmacion = MostrarConfirmacionExportacion(periodo, tipoFormato, filtrosAplicados);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                // 5. Mostrar indicador de carga
                MostrarIndicadorCarga(true);

                try
                {
                    // 6. ✅ CORREGIDO: Generar PDF usando iText7
                    var rutaPDF = await _pdfService.GenerarReportePDFAsync(
                        _materialesFiltrados,
                        periodo,
                        tipoFormato,
                        filtrosAplicados);

                    if (!string.IsNullOrEmpty(rutaPDF))
                    {
                        // 7. Abrir PDF automáticamente
                        _pdfService.AbrirPDF(rutaPDF);

                        // 8. Mostrar mensaje de éxito
                        MostrarMensajeExito(periodo, tipoFormato, _materialesFiltrados.Count);
                    }
                    else
                    {
                        // ✅ CORREGIDO: Mensaje sin referencias a MiKTeX
                        MessageBox.Show("Error al generar el reporte PDF.\n\nPor favor, verifique:\n" +
                                      "• Que tenga permisos de escritura en la carpeta seleccionada\n" +
                                      "• Que no haya archivos PDF abiertos con el mismo nombre\n" +
                                      "• Que el servicio PDF esté funcionando correctamente",
                            "Error de Generación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Error de permisos: No se puede escribir en la ubicación seleccionada.\n\n" +
                                  "Por favor, elija una carpeta diferente o ejecute la aplicación como administrador.",
                        "Error de Permisos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("La carpeta seleccionada no existe.\n\nPor favor, seleccione una ubicación válida.",
                        "Carpeta No Encontrada", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Error de archivo: {ioEx.Message}\n\n" +
                                  "Posibles causas:\n" +
                                  "• El archivo ya está abierto en otro programa\n" +
                                  "• No hay espacio suficiente en disco\n" +
                                  "• La ruta es demasiado larga",
                        "Error de Archivo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    // ✅ CORREGIDO: Mostrar el error real para debugging
                    var mensajeError = $"Error inesperado al generar el PDF:\n\n" +
                                     $"Tipo: {ex.GetType().Name}\n" +
                                     $"Mensaje: {ex.Message}\n\n";

                    if (ex.InnerException != null)
                    {
                        mensajeError += $"Error interno: {ex.InnerException.Message}\n\n";
                    }

                    mensajeError += "Por favor, contacte al administrador del sistema.";

                    MessageBox.Show(mensajeError, "Error de Generación",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    // ✅ NUEVO: Log para debugging
                    System.Diagnostics.Debug.WriteLine($"Error PDF: {ex}");
                }
                finally
                {
                    MostrarIndicadorCarga(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                MostrarIndicadorCarga(false);
            }
        }

        /// <summary>
        /// Obtiene el período seleccionado desde el ComboBox
        /// </summary>
        private PeriodoReporte ObtenerPeriodoSeleccionado()
        {
            return CmbPeriodo?.SelectedIndex switch
            {
                0 => PeriodoReporte.Dia,
                1 => PeriodoReporte.Semana,
                2 => PeriodoReporte.Mes,
                3 => PeriodoReporte.Año,
                _ => PeriodoReporte.Mes
            };
        }

        /// <summary>
        /// Obtiene el tipo de formato seleccionado desde el ComboBox
        /// </summary>
        private TipoFormatoReporte ObtenerTipoFormatoSeleccionado()
        {
            return CmbTipoReporte?.SelectedIndex switch
            {
                0 => TipoFormatoReporte.Estandar,
                1 => TipoFormatoReporte.Ejecutivo,
                2 => TipoFormatoReporte.Detallado,
                3 => TipoFormatoReporte.SoloStockBajo,
                _ => TipoFormatoReporte.Estandar
            };
        }

        /// <summary>
        /// Prepara información de filtros aplicados
        /// </summary>
        private FiltrosAplicados PrepararFiltrosAplicados()
        {
            var filtros = new FiltrosAplicados();

            try
            {
                // Obtener categorías seleccionadas
                if (PanelCategorias != null)
                {
                    filtros.CategoriasSeleccionadas = PanelCategorias.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();
                }

                // Obtener proveedores seleccionados
                if (PanelProveedores != null)
                {
                    filtros.ProveedoresSeleccionados = PanelProveedores.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();
                }

                // Filtros adicionales
                filtros.SoloStockBajo = ChkSoloStockBajo?.IsChecked == true;

                if (decimal.TryParse(TxtStockMin?.Text, out decimal stockMin))
                    filtros.StockMinimo = stockMin;

                if (decimal.TryParse(TxtStockMax?.Text, out decimal stockMax))
                    filtros.StockMaximo = stockMax;

                if (decimal.TryParse(TxtValorMin?.Text, out decimal valorMin))
                    filtros.ValorMinimo = valorMin;

                if (decimal.TryParse(TxtValorMax?.Text, out decimal valorMax))
                    filtros.ValorMaximo = valorMax;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al preparar filtros: {ex.Message}");
            }

            return filtros;
        }

        /// <summary>
        /// Muestra confirmación antes de generar el PDF
        /// </summary>
        private MessageBoxResult MostrarConfirmacionExportacion(PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros)
        {
            var mensaje = new StringBuilder();
            mensaje.AppendLine("¿Generar reporte PDF con la siguiente configuración?");
            mensaje.AppendLine();
            mensaje.AppendLine($"📊 Formato: {ObtenerNombreFormato(tipoFormato)}");
            mensaje.AppendLine($"📅 Período: {ObtenerNombrePeriodo(periodo)}");
            mensaje.AppendLine($"📦 Productos: {_materialesFiltrados.Count:N0}");
            mensaje.AppendLine($"💰 Valor total: {_materialesFiltrados.Sum(m => m.ValorTotalConIVA):C2}");

            var productosStockBajo = _materialesFiltrados.Count(m => m.TieneStockBajo);
            if (productosStockBajo > 0)
            {
                mensaje.AppendLine($"⚠️ Stock bajo: {productosStockBajo} productos");
            }

            if (filtros.TieneFiltrosAplicados)
            {
                mensaje.AppendLine();
                mensaje.AppendLine("🔍 Filtros aplicados:");
                if (filtros.CategoriasSeleccionadas.Any() && filtros.CategoriasSeleccionadas.Count < _todosLosMateriales.Select(m => m.Categoria).Distinct().Count())
                    mensaje.AppendLine($"   • Categorías: {filtros.CategoriasSeleccionadas.Count}");
                if (filtros.ProveedoresSeleccionados.Any() && filtros.ProveedoresSeleccionados.Count < _todosLosMateriales.Select(m => m.Proveedor).Distinct().Count())
                    mensaje.AppendLine($"   • Proveedores: {filtros.ProveedoresSeleccionados.Count}");
                if (filtros.SoloStockBajo)
                    mensaje.AppendLine($"   • Solo stock bajo");
                if (filtros.ValorMinimo.HasValue || filtros.ValorMaximo.HasValue)
                    mensaje.AppendLine($"   • Filtro de valor aplicado");
                if (filtros.StockMinimo.HasValue || filtros.StockMaximo.HasValue)
                    mensaje.AppendLine($"   • Filtro de stock aplicado");
            }

            mensaje.AppendLine();
            mensaje.AppendLine("El PDF se generará usando iText7 y se abrirá automáticamente.");

            return MessageBox.Show(mensaje.ToString(), "Confirmar Exportación",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        /// <summary>
        /// Muestra mensaje de éxito después de generar el PDF
        /// </summary>
        private void MostrarMensajeExito(PeriodoReporte periodo, TipoFormatoReporte formato, int totalProductos)
        {
            var mensaje = $"✅ ¡Reporte generado exitosamente con iText7!\n\n" +
                          $"📄 Formato: {ObtenerNombreFormato(formato)}\n" +
                          $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                          $"📊 Productos incluidos: {totalProductos:N0}\n" +
                          $"💰 Valor total: {_materialesFiltrados.Sum(m => m.ValorTotalConIVA):C2}\n\n" +
                          $"El archivo PDF se abrió automáticamente.";

            MessageBox.Show(mensaje, "PDF Generado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Muestra/oculta indicador de carga
        /// </summary>
        private void MostrarIndicadorCarga(bool mostrar)
        {
            // Cambiar cursor
            Cursor = mostrar ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;

            // Deshabilitar botón durante generación
            if (BtnExportarPDF != null)
                BtnExportarPDF.IsEnabled = !mostrar;

            // Cambiar texto del botón
            if (BtnExportarPDF != null)
                BtnExportarPDF.Content = mostrar ? "⏳ Generando PDF..." : "📄 Exportar PDF";

            // Deshabilitar otros controles críticos
            if (CmbPeriodo != null) CmbPeriodo.IsEnabled = !mostrar;
            if (CmbTipoReporte != null) CmbTipoReporte.IsEnabled = !mostrar;
        }

        /// <summary>
        /// Obtiene el nombre descriptivo del formato
        /// </summary>
        private string ObtenerNombreFormato(TipoFormatoReporte formato)
        {
            return formato switch
            {
                TipoFormatoReporte.Ejecutivo => "Ejecutivo",
                TipoFormatoReporte.Detallado => "Detallado",
                TipoFormatoReporte.SoloStockBajo => "Solo Stock Bajo",
                _ => "Estándar"
            };
        }

        /// <summary>
        /// Obtiene el nombre descriptivo del período
        /// </summary>
        private string ObtenerNombrePeriodo(PeriodoReporte periodo)
        {
            return periodo switch
            {
                PeriodoReporte.Dia => "Diario",
                PeriodoReporte.Semana => "Semanal",
                PeriodoReporte.Mes => "Mensual",
                PeriodoReporte.Año => "Anual",
                _ => "Personalizado"
            };
        }

        #endregion

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private async void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validar que hay datos para exportar
                if (_materialesFiltrados == null || !_materialesFiltrados.Any())
                {
                    MessageBox.Show("No hay datos para exportar.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Obtener configuración del reporte
                var periodo = ObtenerPeriodoSeleccionado();
                var tipoFormato = ObtenerTipoFormatoSeleccionado();

                // 3. Preparar filtros aplicados
                var filtrosAplicados = PrepararFiltrosAplicados();

                // 4. Mostrar confirmación
                var confirmacion = MostrarConfirmacionExportacionExcel(periodo, tipoFormato, filtrosAplicados);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                // 5. Mostrar indicador de carga
                MostrarIndicadorCargaExcel(true);

                try
                {
                    // 6. ✅ Crear servicio Excel y generar archivo
                    var excelService = new StockExcelService(_context);
                    var rutaExcel = await excelService.GenerarReporteExcelAsync(
                        _materialesFiltrados,
                        periodo,
                        tipoFormato,
                        filtrosAplicados);

                    if (!string.IsNullOrEmpty(rutaExcel))
                    {
                        // 7. Abrir Excel automáticamente
                        excelService.AbrirExcel(rutaExcel);

                        // 8. Mostrar mensaje de éxito
                        MostrarMensajeExitoExcel(periodo, tipoFormato, _materialesFiltrados.Count);
                    }
                    else
                    {
                        MessageBox.Show("Error al generar el reporte Excel.\n\nPor favor, verifique:\n" +
                                      "• Que tenga permisos de escritura en la carpeta seleccionada\n" +
                                      "• Que no haya archivos Excel abiertos con el mismo nombre\n" +
                                      "• Que ClosedXML esté instalado correctamente",
                            "Error de Generación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Error de permisos: No se puede escribir en la ubicación seleccionada.\n\n" +
                                  "Por favor, elija una carpeta diferente o ejecute la aplicación como administrador.",
                        "Error de Permisos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("La carpeta seleccionada no existe.\n\nPor favor, seleccione una ubicación válida.",
                        "Carpeta No Encontrada", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Error de archivo: {ioEx.Message}\n\n" +
                                  "Posibles causas:\n" +
                                  "• El archivo ya está abierto en Excel\n" +
                                  "• No hay espacio suficiente en disco\n" +
                                  "• La ruta es demasiado larga",
                        "Error de Archivo", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    var mensajeError = $"Error inesperado al generar el Excel:\n\n" +
                                     $"Tipo: {ex.GetType().Name}\n" +
                                     $"Mensaje: {ex.Message}\n\n";

                    if (ex.InnerException != null)
                    {
                        mensajeError += $"Error interno: {ex.InnerException.Message}\n\n";
                    }

                    mensajeError += "Por favor, contacte al administrador del sistema.";

                    MessageBox.Show(mensajeError, "Error de Generación",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    System.Diagnostics.Debug.WriteLine($"Error Excel: {ex}");
                }
                finally
                {
                    MostrarIndicadorCargaExcel(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                MostrarIndicadorCargaExcel(false);
            }
        }

        /// <summary>
        /// Muestra confirmación antes de generar el Excel
        /// </summary>
        private MessageBoxResult MostrarConfirmacionExportacionExcel(PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros)
        {
            var mensaje = new StringBuilder();
            mensaje.AppendLine("¿Generar reporte Excel con la siguiente configuración?");
            mensaje.AppendLine();
            mensaje.AppendLine($"📊 Formato: {ObtenerNombreFormato(tipoFormato)}");
            mensaje.AppendLine($"📅 Período: {ObtenerNombrePeriodo(periodo)}");
            mensaje.AppendLine($"📦 Productos: {_materialesFiltrados.Count:N0}");
            mensaje.AppendLine($"💰 Valor total: {_materialesFiltrados.Sum(m => m.ValorTotalConIVA):C2}");

            var productosStockBajo = _materialesFiltrados.Count(m => m.TieneStockBajo);
            if (productosStockBajo > 0)
            {
                mensaje.AppendLine($"⚠️ Stock bajo: {productosStockBajo} productos");
            }

            mensaje.AppendLine();
            mensaje.AppendLine("📋 El Excel incluirá las siguientes hojas:");
            mensaje.AppendLine("   • 📊 Resumen Ejecutivo");
            mensaje.AppendLine("   • 📦 Detalle de Productos");
            mensaje.AppendLine("   • 🏷️ Análisis por Categorías");
            mensaje.AppendLine("   • 🏢 Análisis por Proveedores");
            if (productosStockBajo > 0)
                mensaje.AppendLine("   • ⚠️ Alertas de Stock");
            if (_context != null)
                mensaje.AppendLine("   • 📈 Análisis de Movimientos");

            if (filtros.TieneFiltrosAplicados)
            {
                mensaje.AppendLine();
                mensaje.AppendLine("🔍 Filtros aplicados:");
                if (filtros.CategoriasSeleccionadas.Any() && filtros.CategoriasSeleccionadas.Count < _todosLosMateriales.Select(m => m.Categoria).Distinct().Count())
                    mensaje.AppendLine($"   • Categorías: {filtros.CategoriasSeleccionadas.Count}");
                if (filtros.ProveedoresSeleccionados.Any() && filtros.ProveedoresSeleccionados.Count < _todosLosMateriales.Select(m => m.Proveedor).Distinct().Count())
                    mensaje.AppendLine($"   • Proveedores: {filtros.ProveedoresSeleccionados.Count}");
                if (filtros.SoloStockBajo)
                    mensaje.AppendLine($"   • Solo stock bajo");
                if (filtros.ValorMinimo.HasValue || filtros.ValorMaximo.HasValue)
                    mensaje.AppendLine($"   • Filtro de valor aplicado");
                if (filtros.StockMinimo.HasValue || filtros.StockMaximo.HasValue)
                    mensaje.AppendLine($"   • Filtro de stock aplicado");
            }

            mensaje.AppendLine();
            mensaje.AppendLine("El Excel se generará usando ClosedXML y se abrirá automáticamente.");

            return MessageBox.Show(mensaje.ToString(), "Confirmar Exportación a Excel",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        /// <summary>
        /// Muestra mensaje de éxito después de generar el Excel
        /// </summary>
        private void MostrarMensajeExitoExcel(PeriodoReporte periodo, TipoFormatoReporte formato, int totalProductos)
        {
            var mensaje = $"✅ ¡Reporte Excel generado exitosamente con ClosedXML!\n\n" +
                          $"📊 Formato: {ObtenerNombreFormato(formato)}\n" +
                          $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                          $"📦 Productos incluidos: {totalProductos:N0}\n" +
                          $"💰 Valor total: {_materialesFiltrados.Sum(m => m.ValorTotalConIVA):C2}\n\n" +
                          $"📋 Hojas incluidas en el Excel:\n" +
                          $"   • Resumen ejecutivo con métricas clave\n" +
                          $"   • Detalle completo de productos\n" +
                          $"   • Análisis por categorías y proveedores\n" +
                          $"   • Alertas de stock (si aplica)\n" +
                          $"   • Análisis de movimientos (si aplica)\n\n" +
                          $"El archivo Excel se abrió automáticamente.";

            MessageBox.Show(mensaje, "Excel Generado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Muestra/oculta indicador de carga para Excel
        /// </summary>
        private void MostrarIndicadorCargaExcel(bool mostrar)
        {
            // Cambiar cursor
            Cursor = mostrar ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;

            // Deshabilitar botón durante generación
            if (BtnExportarExcel != null)
                BtnExportarExcel.IsEnabled = !mostrar;

            // Cambiar texto del botón
            if (BtnExportarExcel != null)
                BtnExportarExcel.Content = mostrar ? "⏳ Generando Excel..." : "📊 Exportar Excel";

            // Deshabilitar otros controles críticos
            if (CmbPeriodo != null) CmbPeriodo.IsEnabled = !mostrar;
            if (CmbTipoReporte != null) CmbTipoReporte.IsEnabled = !mostrar;
            if (BtnExportarPDF != null) BtnExportarPDF.IsEnabled = !mostrar;
        }

    }
}