using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using costbenefi.Models;
using costbenefi.Data;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio PDF profesional CORREGIDO y compatible con ReporteMovimientos actualizado
    /// Integración perfecta con el sistema robusto de cortes
    /// </summary>
    public class StockPDFService
    {
        #region Configuración y Estilos

        private readonly CultureInfo _cultura;
        private readonly AppDbContext _context;

        // Colores corporativos
        private static readonly Color COLOR_PRIMARIO = Color.FromHex("#2C3E50");   // Azul oscuro
        private static readonly Color COLOR_EXITO = Color.FromHex("#27AE60");      // Verde
        private static readonly Color COLOR_ERROR = Color.FromHex("#E74C3C");      // Rojo
        private static readonly Color COLOR_TEXTO = Color.FromHex("#2C3E50");      // Texto principal
        private static readonly Color COLOR_GRIS_CLARO = Color.FromHex("#ECF0F1"); // Fondo sutil
        private static readonly Color COLOR_AMARILLO = Color.FromHex("#F39C12");   // Para movimientos

        public StockPDFService(AppDbContext context = null)
        {
            _cultura = CultureInfo.GetCultureInfo("es-ES");
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #endregion

        #region Método Principal

        public async Task<string> GenerarReportePDFAsync(
            List<RawMaterial> productos,
            PeriodoReporte periodo,
            TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar,
            FiltrosAplicados filtrosAplicados = null)
        {
            try
            {
                Debug.WriteLine("📊 [PDF Corregido] Iniciando generación...");

                if (!ValidarParametrosEntrada(productos))
                    return null;

                var rutaDestino = MostrarSaveFileDialog(periodo, tipoFormato);
                if (string.IsNullOrEmpty(rutaDestino))
                    return null;

                await Task.Run(async () => await GenerarPDFProfesionalAsync(productos, periodo, tipoFormato, filtrosAplicados, rutaDestino));

                Debug.WriteLine("✅ [PDF Corregido] Completado exitosamente");
                return rutaDestino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [PDF Corregido] Error: {ex}");
                MessageBox.Show($"Error al generar PDF profesional:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Generación PDF Profesional

        private async Task GenerarPDFProfesionalAsync(List<RawMaterial> productos, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros, string rutaDestino)
        {
            var totalProductos = productos.Count;
            var valorTotal = productos.Sum(p => p.ValorTotalConIVA);
            var productosStockBajo = productos.Count(p => p.TieneStockBajo);

            // ✅ CORREGIDO: Obtener reporte de movimientos usando el método actualizado
            ReporteMovimientos reporteMovimientos = null;
            if (_context != null)
            {
                reporteMovimientos = await ObtenerReporteMovimientosAsync(productos, periodo);
            }

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);

                    // Encabezado empresarial
                    page.Header().Height(50).Background(COLOR_PRIMARIO).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("REPORTE DE INVENTARIO")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.White);

                            col.Item().Text($"Período: {ObtenerNombrePeriodo(periodo)} | Formato: {ObtenerNombreFormato(tipoFormato)}")
                                .FontFamily("Arial")
                                .FontSize(10)
                                .FontColor(Colors.White);
                        });

                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                            .FontFamily("Arial")
                            .FontSize(10)
                            .FontColor(Colors.White);
                    });

                    // Contenido detallado
                    page.Content().Padding(10).Column(contenido =>
                    {
                        // Resumen ejecutivo
                        if (tipoFormato != TipoFormatoReporte.Detallado)
                        {
                            contenido.Item().Background(COLOR_GRIS_CLARO).Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Resumen Ejecutivo")
                                        .FontFamily("Arial")
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(COLOR_PRIMARIO);

                                    col.Item().Text($"Total de productos: {totalProductos:N0}")
                                        .FontFamily("Arial")
                                        .FontSize(10)
                                        .FontColor(COLOR_TEXTO);

                                    col.Item().Text($"Valor del inventario: ${valorTotal:N2}")
                                        .FontFamily("Arial")
                                        .FontSize(10)
                                        .FontColor(COLOR_EXITO);
                                });

                                row.ConstantItem(120).Column(col =>
                                {
                                    if (productosStockBajo > 0)
                                    {
                                        col.Item().Text("Stock Crítico")
                                            .FontFamily("Arial")
                                            .FontSize(10)
                                            .Bold()
                                            .FontColor(COLOR_ERROR);

                                        col.Item().Text($"{productosStockBajo} productos")
                                            .FontFamily("Arial")
                                            .FontSize(10)
                                            .FontColor(COLOR_ERROR);
                                    }
                                    else
                                    {
                                        col.Item().Text("Estado Óptimo")
                                            .FontFamily("Arial")
                                            .FontSize(10)
                                            .Bold()
                                            .FontColor(COLOR_EXITO);
                                    }
                                });
                            });
                        }

                        // Filtros aplicados
                        if (filtros?.TieneFiltrosAplicados == true)
                        {
                            contenido.Item().PaddingVertical(8).Text("Filtros Aplicados")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingLeft(10).Column(col =>
                            {
                                if (filtros.CategoriasSeleccionadas.Any())
                                    col.Item().Text($"Categorías: {string.Join(", ", filtros.CategoriasSeleccionadas)}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                if (filtros.ProveedoresSeleccionados.Any())
                                    col.Item().Text($"Proveedores: {string.Join(", ", filtros.ProveedoresSeleccionados)}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                if (filtros.SoloStockBajo)
                                    col.Item().Text("Solo stock bajo")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                if (filtros.StockMinimo.HasValue)
                                    col.Item().Text($"Stock mínimo: {filtros.StockMinimo:F1}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                if (filtros.StockMaximo.HasValue)
                                    col.Item().Text($"Stock máximo: {filtros.StockMaximo:F1}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                if (filtros.ValorMinimo.HasValue)
                                    col.Item().Text($"Valor mínimo: ${filtros.ValorMinimo:N2}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                if (filtros.ValorMaximo.HasValue)
                                    col.Item().Text($"Valor máximo: ${filtros.ValorMaximo:N2}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                            });
                        }

                        contenido.Item().PaddingVertical(8);

                        // Tabla de productos
                        contenido.Item().Text("Detalle de Productos")
                            .FontFamily("Arial")
                            .FontSize(12)
                            .Bold()
                            .FontColor(COLOR_PRIMARIO);

                        contenido.Item().PaddingVertical(5);

                        var productosLimitados = tipoFormato == TipoFormatoReporte.SoloStockBajo
                            ? productos.Where(p => !p.Eliminado && p.TieneStockBajo).Take(20).ToList()
                            : productos.Where(p => !p.Eliminado).Take(20).ToList();

                        contenido.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f); // Producto
                                columns.RelativeColumn(1.5f); // Categoría
                                columns.RelativeColumn(1);    // Stock
                                columns.RelativeColumn(1);    // Precio Unitario
                                columns.RelativeColumn(1.5f); // Valor Total
                                columns.RelativeColumn(0.8f); // Estado
                            });

                            // Encabezado de tabla
                            tabla.Header(header =>
                            {
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Producto")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Categoría")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Stock")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Precio Unit.")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Valor Total")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Estado")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(Colors.White);
                            });

                            // Filas de la tabla
                            foreach (var (producto, indice) in productosLimitados.Select((p, i) => (p, i)))
                            {
                                var colorFila = indice % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;
                                var nombreCorto = TruncateString(producto.NombreArticulo ?? "Sin nombre", 25);
                                var categoriaCorta = TruncateString(producto.Categoria ?? "Sin categoría", 15);

                                tabla.Cell().Background(colorFila).Padding(5).Text(nombreCorto)
                                    .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(5).Text(categoriaCorta)
                                    .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(5).AlignRight().Text($"{producto.StockTotal:F1}")
                                    .FontFamily("Arial").FontSize(9).FontColor(producto.TieneStockBajo ? COLOR_ERROR : COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(5).AlignRight().Text($"${producto.PrecioPorUnidad:N2}")
                                    .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(5).AlignRight().Text($"${producto.ValorTotalConIVA:N2}")
                                    .FontFamily("Arial").FontSize(9).FontColor(COLOR_EXITO);
                                tabla.Cell().Background(colorFila).Padding(5).AlignCenter().Text(producto.TieneStockBajo ? "⚠" : "✓")
                                    .FontFamily("Arial").FontSize(9).FontColor(producto.TieneStockBajo ? COLOR_ERROR : COLOR_EXITO);
                            }
                        });

                        if (productos.Count > 20)
                        {
                            contenido.Item().PaddingTop(5).Text($"Se muestran los primeros 20 de {productos.Count} productos")
                                .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO).Italic();
                        }

                        // ✅ SECCIÓN DE MOVIMIENTOS CORREGIDA
                        if (reporteMovimientos != null && reporteMovimientos.EsValidoParaGenerar())
                        {
                            contenido.Item().PaddingVertical(15);

                            contenido.Item().Text("📊 ANÁLISIS DE MOVIMIENTOS DE STOCK")
                                .FontFamily("Arial")
                                .FontSize(16)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(8);

                            // Resumen con números grandes
                            contenido.Item().Background(Color.FromHex("#E8F5E8")).Padding(15).Text("💰 RESUMEN GENERAL DE MOVIMIENTOS")
                                .FontFamily("Arial")
                                .FontSize(14)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().Background(Color.FromHex("#E8F5E8")).Padding(15).Text($"📈 TOTAL ENTRADAS: {reporteMovimientos.TotalEntradas:F2} unidades")
                                .FontFamily("Arial")
                                .FontSize(18)
                                .Bold()
                                .FontColor(COLOR_EXITO);

                            contenido.Item().Background(Color.FromHex("#E8F5E8")).Padding(15).Text($"📉 TOTAL SALIDAS: {reporteMovimientos.TotalSalidas:F2} unidades")
                                .FontFamily("Arial")
                                .FontSize(18)
                                .Bold()
                                .FontColor(COLOR_ERROR);

                            contenido.Item().Background(Color.FromHex("#E8F5E8")).Padding(15).Text($"💵 VALOR TOTAL MOVIDO: ${reporteMovimientos.ValorTotalMovido:N2}")
                                .FontFamily("Arial")
                                .FontSize(18)
                                .Bold()
                                .FontColor(COLOR_AMARILLO);

                            contenido.Item().Background(Color.FromHex("#E8F5E8")).Padding(15).Text($"⚖️ DIFERENCIA NETA: {reporteMovimientos.DiferenciaNeta:F2} unidades")
                                .FontFamily("Arial")
                                .FontSize(18)
                                .Bold()
                                .FontColor(reporteMovimientos.DiferenciaNeta >= 0 ? COLOR_EXITO : COLOR_ERROR);

                            // Información del período
                            contenido.Item().PaddingVertical(8);
                            contenido.Item().Background(Color.FromHex("#FEF7E0")).Padding(10).Text($"📊 {reporteMovimientos.ObtenerInfoPeriodo()} | Productos afectados: {reporteMovimientos.ProductosConMovimientos} | Total movimientos: {reporteMovimientos.TotalMovimientos}")
                                .FontFamily("Arial")
                                .FontSize(10)
                                .FontColor(COLOR_TEXTO);

                            // Tabla detallada si hay estadísticas
                            if (reporteMovimientos.EstadisticasPorProducto.Any())
                            {
                                contenido.Item().PaddingVertical(10);

                                contenido.Item().Text("📋 Detalle por Producto")
                                    .FontFamily("Arial")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(COLOR_PRIMARIO);

                                contenido.Item().PaddingVertical(5);

                                var estadisticasMovimientos = reporteMovimientos.EstadisticasPorProducto.Take(10).ToList();

                                contenido.Item().Table(tabla =>
                                {
                                    tabla.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.5f); // Producto
                                        columns.RelativeColumn(1);    // Stock Inicial
                                        columns.RelativeColumn(1);    // Entradas
                                        columns.RelativeColumn(1);    // Salidas
                                        columns.RelativeColumn(1);    // Stock Final
                                        columns.RelativeColumn(1.5f); // Valor Movido
                                    });

                                    // Encabezado
                                    tabla.Header(header =>
                                    {
                                        header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Producto")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                        header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Stock Inicial")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                        header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Entradas")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                        header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Salidas")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                        header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Stock Final")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                        header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Valor Movido")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    });

                                    // Filas con datos
                                    foreach (var (estadisticaMovimiento, indiceMovimiento) in estadisticasMovimientos.Select((e, i) => (e, i)))
                                    {
                                        var colorFilaMovimiento = indiceMovimiento % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;
                                        var nombreCortoMovimiento = TruncateString(estadisticaMovimiento.NombreProducto, 25);

                                        tabla.Cell().Background(colorFilaMovimiento).Padding(4).Text(nombreCortoMovimiento)
                                            .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                        tabla.Cell().Background(colorFilaMovimiento).Padding(4).AlignRight().Text($"{estadisticaMovimiento.StockInicial:F1}")
                                            .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                        tabla.Cell().Background(colorFilaMovimiento).Padding(4).AlignRight().Text($"{estadisticaMovimiento.Entradas:F1}")
                                            .FontFamily("Arial").FontSize(8).FontColor(estadisticaMovimiento.Entradas > 0 ? COLOR_EXITO : COLOR_TEXTO);
                                        tabla.Cell().Background(colorFilaMovimiento).Padding(4).AlignRight().Text($"{estadisticaMovimiento.Salidas:F1}")
                                            .FontFamily("Arial").FontSize(8).FontColor(estadisticaMovimiento.Salidas > 0 ? COLOR_ERROR : COLOR_TEXTO);
                                        tabla.Cell().Background(colorFilaMovimiento).Padding(4).AlignRight().Text($"{estadisticaMovimiento.StockFinal:F1}")
                                            .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                        tabla.Cell().Background(colorFilaMovimiento).Padding(4).AlignRight().Text($"${estadisticaMovimiento.ValorMovido:N0}")
                                            .FontFamily("Arial").FontSize(8).FontColor(COLOR_AMARILLO);
                                    }
                                });

                                if (reporteMovimientos.EstadisticasPorProducto.Count > 10)
                                {
                                    contenido.Item().PaddingTop(5).Text($"Se muestran los 10 productos con mayor valor movido de {reporteMovimientos.EstadisticasPorProducto.Count} productos con movimientos")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO).Italic();
                                }
                            }

                            // Detalle cronológico limitado
                            if (reporteMovimientos.Movimientos.Any())
                            {
                                contenido.Item().PaddingVertical(15);

                                contenido.Item().Text("📅 ÚLTIMOS MOVIMIENTOS")
                                    .FontFamily("Arial")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(COLOR_PRIMARIO);

                                contenido.Item().PaddingVertical(5);

                                var movimientosDetalle = reporteMovimientos.Movimientos
                                    .OrderByDescending(m => m.FechaMovimiento)
                                    .Take(15) // Reducido para que quepa en el PDF
                                    .ToList();

                                foreach (var movimiento in movimientosDetalle)
                                {
                                    var fechaTexto = movimiento.FechaMovimiento.ToString("dd/MM/yyyy HH:mm");
                                    var tipoTexto = movimiento.TipoMovimiento ?? "Movimiento";
                                    var colorTipo = movimiento.EsEntrada ? COLOR_EXITO : movimiento.EsSalida ? COLOR_ERROR : COLOR_TEXTO;
                                    var emoji = movimiento.EsEntrada ? "📈" : movimiento.EsSalida ? "📉" : "📋";

                                    var textoCompleto = $"{emoji} {fechaTexto} - {tipoTexto}: {movimiento.Cantidad:F1} {movimiento.UnidadMedida} de {movimiento.RawMaterial?.NombreArticulo ?? "producto"} por ${movimiento.ValorTotalConIVA:N2}";

                                    if (!string.IsNullOrEmpty(movimiento.Usuario))
                                        textoCompleto += $" (Usuario: {movimiento.Usuario})";

                                    var colorFondoMovimiento = movimientosDetalle.IndexOf(movimiento) % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;

                                    contenido.Item().Background(colorFondoMovimiento).Padding(6).Border(1).BorderColor(colorTipo).Text(textoCompleto)
                                        .FontFamily("Arial")
                                        .FontSize(8)
                                        .FontColor(COLOR_TEXTO);
                                }

                                if (reporteMovimientos.Movimientos.Count > 15)
                                {
                                    contenido.Item().PaddingTop(5).Background(Color.FromHex("#FFF3CD")).Padding(6).Text($"📝 Se muestran los últimos 15 movimientos de un total de {reporteMovimientos.Movimientos.Count} movimientos")
                                        .FontFamily("Arial")
                                        .FontSize(8)
                                        .FontColor(COLOR_TEXTO)
                                        .Italic();
                                }
                            }
                        }
                    });

                    // Pie de página corporativo
                    page.Footer().Height(20).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Sistema Costo-Beneficio")
                            .FontFamily("Arial")
                            .FontSize(8)
                            .FontColor(COLOR_TEXTO);

                        row.RelativeItem().AlignRight().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontFamily("Arial")
                            .FontSize(8)
                            .FontColor(COLOR_TEXTO);
                    });
                });
            });

            documento.GeneratePdf(rutaDestino);
        }

        #endregion

        #region ✅ MÉTODO CORREGIDO PARA OBTENER MOVIMIENTOS

        /// <summary>
        /// Obtiene el reporte de movimientos usando la versión simplificada
        /// CORREGIDO: Compatible con ReporteMovimientos simplificado
        /// </summary>
        private async Task<ReporteMovimientos> ObtenerReporteMovimientosAsync(List<RawMaterial> productos, PeriodoReporte periodo)
        {
            try
            {
                if (_context == null) return null;

                Debug.WriteLine($"📊 [PDF] Obteniendo movimientos para {productos.Count} productos, período: {periodo}");

                // Obtener IDs de productos
                var productosIds = productos.Select(p => p.Id).ToList();

                // ✅ CREAR PRIMERO EL REPORTE PARA OBTENER LAS FECHAS
                var reporteTemporal = new ReporteMovimientos(productos, new List<Movimiento>(), periodo);

                // Obtener movimientos del período usando las fechas configuradas
                var movimientos = await _context.Movimientos
                    .Include(m => m.RawMaterial)
                    .Where(m => productosIds.Contains(m.RawMaterialId) &&
                               m.FechaMovimiento >= reporteTemporal.FechaInicio &&
                               m.FechaMovimiento <= reporteTemporal.FechaFin)
                    .OrderBy(m => m.RawMaterialId)
                    .ThenBy(m => m.FechaMovimiento)
                    .ToListAsync();

                Debug.WriteLine($"📊 [PDF] Encontrados {movimientos.Count} movimientos en el período");

                // ✅ CREAR EL REPORTE FINAL CON LOS MOVIMIENTOS OBTENIDOS
                var reporteMovimientos = new ReporteMovimientos(productos, movimientos, periodo);

                return reporteMovimientos;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [PDF] Error al obtener reporte de movimientos: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos de Utilidad

        private bool ValidarParametrosEntrada(List<RawMaterial> productos)
        {
            if (productos == null || !productos.Any())
            {
                MessageBox.Show("No hay productos seleccionados para el reporte.",
                    "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private string MostrarSaveFileDialog(PeriodoReporte periodo, TipoFormatoReporte tipoFormato)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Guardar Reporte de Stock",
                    Filter = "Archivos PDF (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    AddExtension = true,
                    FileName = $"ReporteStock_{ObtenerNombreFormato(tipoFormato)}_{ObtenerNombrePeriodo(periodo)}_{DateTime.Now:yyyy-MM-dd}",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                return saveDialog.ShowDialog() == true ? saveDialog.FileName : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar el diálogo: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }

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

        public void AbrirPDF(string rutaPDF)
        {
            try
            {
                if (File.Exists(rutaPDF))
                {
                    Process.Start(new ProcessStartInfo(rutaPDF) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("El archivo PDF no fue encontrado.",
                        "Archivo No Encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el PDF: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}