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
using ClosedXML.Excel;
using costbenefi.Models;
using costbenefi.Data;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio Excel profesional CORREGIDO y compatible con ReporteMovimientos actualizado
    /// Integración perfecta con el sistema robusto de cortes
    /// </summary>
    public class StockExcelService
    {
        #region Configuración

        private readonly CultureInfo _cultura;
        private readonly AppDbContext _context;

        // Colores corporativos para ClosedXML
        private static readonly XLColor COLOR_ENCABEZADO = XLColor.FromHtml("#2C3E50");
        private static readonly XLColor COLOR_EXITO = XLColor.FromHtml("#27AE60");
        private static readonly XLColor COLOR_ERROR = XLColor.FromHtml("#E74C3C");
        private static readonly XLColor COLOR_GRIS_CLARO = XLColor.FromHtml("#ECF0F1");
        private static readonly XLColor COLOR_AMARILLO = XLColor.FromHtml("#F39C12");
        private static readonly XLColor COLOR_AZUL = XLColor.FromHtml("#3498DB");

        public StockExcelService(AppDbContext context = null)
        {
            _cultura = CultureInfo.GetCultureInfo("es-MX");
            _context = context;
        }

        #endregion

        #region Método Principal

        public async Task<string> GenerarReporteExcelAsync(
            List<RawMaterial> productos,
            PeriodoReporte periodo,
            TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar,
            FiltrosAplicados filtrosAplicados = null)
        {
            try
            {
                Debug.WriteLine("📊 [Excel Corregido] Iniciando generación...");

                if (!ValidarParametrosEntrada(productos))
                    return null;

                var rutaDestino = MostrarSaveFileDialog(periodo, tipoFormato);
                if (string.IsNullOrEmpty(rutaDestino))
                    return null;

                await Task.Run(async () => await GenerarExcelProfesionalAsync(productos, periodo, tipoFormato, filtrosAplicados, rutaDestino));

                Debug.WriteLine("✅ [Excel Corregido] Completado exitosamente");
                return rutaDestino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [Excel Corregido] Error: {ex}");
                MessageBox.Show($"Error al generar Excel profesional:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Generación Excel Profesional

        private async Task GenerarExcelProfesionalAsync(List<RawMaterial> productos, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros, string rutaDestino)
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

            using (var workbook = new XLWorkbook())
            {
                // ===== HOJA 1: RESUMEN EJECUTIVO =====
                var wsResumen = workbook.Worksheets.Add("📊 Resumen Ejecutivo");
                await CrearHojaResumenEjecutivo(wsResumen, productos, periodo, tipoFormato, filtros, reporteMovimientos);

                // ===== HOJA 2: DETALLE DE PRODUCTOS =====
                var wsDetalle = workbook.Worksheets.Add("📦 Detalle de Productos");
                CrearHojaDetalleProductos(wsDetalle, productos, tipoFormato);

                // ===== HOJA 3: ANÁLISIS POR CATEGORÍAS =====
                var wsCategorias = workbook.Worksheets.Add("🏷️ Por Categorías");
                CrearHojaAnalisisCategorias(wsCategorias, productos);

                // ===== HOJA 4: ANÁLISIS POR PROVEEDORES =====
                var wsProveedores = workbook.Worksheets.Add("🏢 Por Proveedores");
                CrearHojaAnalisisProveedores(wsProveedores, productos);

                // ===== HOJA 5: ALERTAS DE STOCK =====
                if (productosStockBajo > 0)
                {
                    var wsAlertas = workbook.Worksheets.Add("⚠️ Alertas de Stock");
                    CrearHojaAlertasStock(wsAlertas, productos.Where(p => p.TieneStockBajo).ToList());
                }

                // ===== HOJA 6: MOVIMIENTOS (SI HAY DATOS) =====
                if (reporteMovimientos != null && reporteMovimientos.EsValidoParaGenerar())
                {
                    var wsMovimientos = workbook.Worksheets.Add("📈 Movimientos");
                    await CrearHojaMovimientos(wsMovimientos, reporteMovimientos);
                }

                // Configuraciones globales
                ConfigurarLibroCompleto(workbook, periodo, tipoFormato);

                // Guardar archivo
                workbook.SaveAs(rutaDestino);
            }
        }

        #endregion

        #region Crear Hojas Específicas

        private async Task CrearHojaResumenEjecutivo(IXLWorksheet ws, List<RawMaterial> productos, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros, ReporteMovimientos reporteMovimientos)
        {
            var row = 1;

            // ===== ENCABEZADO PRINCIPAL =====
            ws.Cell(row, 1).Value = "📊 REPORTE EJECUTIVO DE INVENTARIO";
            ws.Range(row, 1, row, 6).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 6));
            row += 2;

            // ===== INFORMACIÓN DEL REPORTE =====
            ws.Cell(row, 1).Value = "📅 Período:";
            ws.Cell(row, 2).Value = ObtenerNombrePeriodo(periodo);
            ws.Cell(row, 4).Value = "📋 Formato:";
            ws.Cell(row, 5).Value = ObtenerNombreFormato(tipoFormato);
            EstiloInformacionGeneral(ws.Range(row, 1, row, 6));
            row++;

            ws.Cell(row, 1).Value = "🕐 Generado:";
            ws.Cell(row, 2).Value = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm", _cultura);
            ws.Cell(row, 4).Value = "👤 Usuario:";
            ws.Cell(row, 5).Value = Environment.UserName;
            EstiloInformacionGeneral(ws.Range(row, 1, row, 6));
            row += 2;

            // ===== MÉTRICAS PRINCIPALES =====
            ws.Cell(row, 1).Value = "📊 MÉTRICAS PRINCIPALES";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var metricas = new[]
            {
                ("📦 Total de Productos", productos.Count.ToString("N0"), COLOR_ENCABEZADO),
                ("💰 Valor Total del Inventario", productos.Sum(p => p.ValorTotalConIVA).ToString("C2", _cultura), COLOR_EXITO),
                ("⚠️ Productos con Stock Bajo", productos.Count(p => p.TieneStockBajo).ToString("N0"), COLOR_ERROR),
                ("💎 Valor Promedio por Producto", (productos.Sum(p => p.ValorTotalConIVA) / Math.Max(productos.Count, 1)).ToString("C2", _cultura), COLOR_AMARILLO),
                ("🏷️ Categorías Diferentes", productos.Select(p => p.Categoria).Distinct().Count().ToString("N0"), COLOR_AZUL),
                ("🏢 Proveedores Diferentes", productos.Select(p => p.Proveedor).Distinct().Count().ToString("N0"), COLOR_AZUL)
            };

            foreach (var (titulo, valor, color) in metricas)
            {
                ws.Cell(row, 1).Value = titulo;
                ws.Cell(row, 2).Value = valor;

                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontColor = color;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Font.FontSize = 12;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;

                row++;
            }
            row++;

            // ===== FILTROS APLICADOS =====
            if (filtros?.TieneFiltrosAplicados == true)
            {
                ws.Cell(row, 1).Value = "🔍 FILTROS APLICADOS";
                ws.Range(row, 1, row, 6).Merge();
                EstiloSubEncabezado(ws.Range(row, 1, row, 6));
                row++;

                if (filtros.CategoriasSeleccionadas.Any())
                {
                    ws.Cell(row, 1).Value = "🏷️ Categorías:";
                    ws.Cell(row, 2).Value = string.Join(", ", filtros.CategoriasSeleccionadas);
                    row++;
                }

                if (filtros.ProveedoresSeleccionados.Any())
                {
                    ws.Cell(row, 1).Value = "🏢 Proveedores:";
                    ws.Cell(row, 2).Value = string.Join(", ", filtros.ProveedoresSeleccionados);
                    row++;
                }

                if (filtros.SoloStockBajo)
                {
                    ws.Cell(row, 1).Value = "⚠️ Solo Stock Bajo:";
                    ws.Cell(row, 2).Value = "Sí";
                    row++;
                }

                row++;
            }

            // ===== RESUMEN DE MOVIMIENTOS CORREGIDO =====
            if (reporteMovimientos != null && reporteMovimientos.EsValidoParaGenerar())
            {
                ws.Cell(row, 1).Value = "📈 RESUMEN DE MOVIMIENTOS";
                ws.Range(row, 1, row, 6).Merge();
                EstiloSubEncabezado(ws.Range(row, 1, row, 6));
                row++;

                var movimientosMetricas = new[]
                {
                    ("📊 Productos con Movimientos", reporteMovimientos.ProductosConMovimientos.ToString("N0")),
                    ("📈 Total de Entradas", $"{reporteMovimientos.TotalEntradas:F2} unidades"),
                    ("📉 Total de Salidas", $"{reporteMovimientos.TotalSalidas:F2} unidades"),
                    ("⚖️ Diferencia Neta", $"{reporteMovimientos.DiferenciaNeta:F2} unidades"),
                    ("💵 Valor Total Movido", reporteMovimientos.ValorTotalMovido.ToString("C2", _cultura)),
                    ("🔄 Total de Movimientos", reporteMovimientos.TotalMovimientos.ToString("N0"))
                };

                foreach (var (titulo, valor) in movimientosMetricas)
                {
                    ws.Cell(row, 1).Value = titulo;
                    ws.Cell(row, 2).Value = valor;
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 2).Style.Font.Bold = true;
                    row++;
                }
            }

            // Ajustar columnas
            ws.ColumnsUsed().AdjustToContents();
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 25);
        }

        private void CrearHojaDetalleProductos(IXLWorksheet ws, List<RawMaterial> productos, TipoFormatoReporte tipoFormato)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📦 DETALLE COMPLETO DE PRODUCTOS";
            ws.Range(row, 1, row, 9).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 9));
            row += 2;

            // Encabezados de columnas
            var encabezados = new[] {
                "📦 Producto", "🏷️ Categoría", "📊 Stock Total", "📏 Unidad",
                "💰 Precio Unitario", "💎 Valor Total", "⚠️ Stock Bajo", "🏢 Proveedor", "📅 Última Actualización"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos de productos
            var productosParaMostrar = tipoFormato == TipoFormatoReporte.SoloStockBajo
                ? productos.Where(p => p.TieneStockBajo).ToList()
                : productos;

            foreach (var producto in productosParaMostrar)
            {
                ws.Cell(row, 1).Value = producto.NombreArticulo ?? "Sin nombre";
                ws.Cell(row, 2).Value = producto.Categoria ?? "Sin categoría";
                ws.Cell(row, 3).Value = producto.StockTotal;
                ws.Cell(row, 4).Value = producto.UnidadMedida ?? "";
                ws.Cell(row, 5).Value = producto.PrecioPorUnidad;
                ws.Cell(row, 6).Value = producto.ValorTotalConIVA;
                ws.Cell(row, 7).Value = producto.TieneStockBajo ? "⚠️ SÍ" : "✅ NO";
                ws.Cell(row, 8).Value = producto.Proveedor ?? "Sin proveedor";
                ws.Cell(row, 9).Value = producto.FechaActualizacion.ToString("dd/MM/yyyy HH:mm");

                // Formateo especial
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.0000";
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según stock bajo
                if (producto.TieneStockBajo)
                {
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                    ws.Cell(row, 7).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 7).Style.Font.Bold = true;
                }
                else
                {
                    ws.Cell(row, 7).Style.Font.FontColor = COLOR_EXITO;
                }

                // Alternar colores de fila
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;
                }

                row++;
            }

            // Ajustar columnas y aplicar filtros
            ws.ColumnsUsed().AdjustToContents();

            // Aplicar AutoFilter
            var dataRange = ws.Range(3, 1, row - 1, 9);
            dataRange.SetAutoFilter();

            // Congelar paneles (fila 4, columna 2)
            ws.SheetView.FreezeRows(3);
            ws.SheetView.FreezeColumns(1);
        }

        private void CrearHojaAnalisisCategorias(IXLWorksheet ws, List<RawMaterial> productos)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "🏷️ ANÁLISIS POR CATEGORÍAS";
            ws.Range(row, 1, row, 6).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 6));
            row += 2;

            // Agrupar por categorías
            var analisisCategorias = productos
                .GroupBy(p => p.Categoria ?? "Sin Categoría")
                .Select(g => new
                {
                    Categoria = g.Key,
                    CantidadProductos = g.Count(),
                    ValorTotal = g.Sum(p => p.ValorTotalConIVA),
                    ProductosStockBajo = g.Count(p => p.TieneStockBajo),
                    StockTotal = g.Sum(p => p.StockTotal),
                    PrecioPromedio = g.Average(p => p.PrecioPorUnidad)
                })
                .OrderByDescending(x => x.ValorTotal)
                .ToList();

            // Encabezados
            var encabezados = new[] {
                "🏷️ Categoría", "📦 Cantidad", "💰 Valor Total", "📊 Stock Total",
                "💲 Precio Promedio", "⚠️ Stock Bajo"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos
            foreach (var categoria in analisisCategorias)
            {
                ws.Cell(row, 1).Value = categoria.Categoria;
                ws.Cell(row, 2).Value = categoria.CantidadProductos;
                ws.Cell(row, 3).Value = categoria.ValorTotal;
                ws.Cell(row, 4).Value = categoria.StockTotal;
                ws.Cell(row, 5).Value = categoria.PrecioPromedio;
                ws.Cell(row, 6).Value = categoria.ProductosStockBajo;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                // Resaltar problemas de stock
                if (categoria.ProductosStockBajo > 0)
                {
                    ws.Cell(row, 6).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 6).Style.Font.Bold = true;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 6);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaAnalisisProveedores(IXLWorksheet ws, List<RawMaterial> productos)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "🏢 ANÁLISIS POR PROVEEDORES";
            ws.Range(row, 1, row, 7).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 7));
            row += 2;

            // Agrupar por proveedores
            var analisisProveedores = productos
                .GroupBy(p => p.Proveedor ?? "Sin Proveedor")
                .Select(g => new
                {
                    Proveedor = g.Key,
                    CantidadProductos = g.Count(),
                    ValorTotal = g.Sum(p => p.ValorTotalConIVA),
                    ProductosStockBajo = g.Count(p => p.TieneStockBajo),
                    Categorias = string.Join(", ", g.Select(p => p.Categoria ?? "Sin Cat.").Distinct().Take(3)),
                    StockTotal = g.Sum(p => p.StockTotal),
                    UltimaActualizacion = g.Max(p => p.FechaActualizacion)
                })
                .OrderByDescending(x => x.ValorTotal)
                .ToList();

            // Encabezados
            var encabezados = new[] {
                "🏢 Proveedor", "📦 Cantidad", "💰 Valor Total", "⚠️ Stock Bajo",
                "🏷️ Categorías Principales", "📊 Stock Total", "📅 Última Actualización"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos
            foreach (var proveedor in analisisProveedores)
            {
                ws.Cell(row, 1).Value = proveedor.Proveedor;
                ws.Cell(row, 2).Value = proveedor.CantidadProductos;
                ws.Cell(row, 3).Value = proveedor.ValorTotal;
                ws.Cell(row, 4).Value = proveedor.ProductosStockBajo;
                ws.Cell(row, 5).Value = proveedor.Categorias;
                ws.Cell(row, 6).Value = proveedor.StockTotal;
                ws.Cell(row, 7).Value = proveedor.UltimaActualizacion.ToString("dd/MM/yyyy HH:mm");

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

                // Resaltar problemas de stock
                if (proveedor.ProductosStockBajo > 0)
                {
                    ws.Cell(row, 4).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 4).Style.Font.Bold = true;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 7);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaAlertasStock(IXLWorksheet ws, List<RawMaterial> productosStockBajo)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "⚠️ ALERTAS DE STOCK CRÍTICO";
            ws.Range(row, 1, row, 7).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 7));
            ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = COLOR_ERROR;
            ws.Range(row, 1, row, 7).Style.Font.FontColor = XLColor.White;
            row += 2;

            // Resumen crítico
            ws.Cell(row, 1).Value = $"🚨 TOTAL DE PRODUCTOS EN ESTADO CRÍTICO: {productosStockBajo.Count}";
            ws.Range(row, 1, row, 7).Merge();
            ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
            ws.Range(row, 1, row, 7).Style.Font.Bold = true;
            ws.Range(row, 1, row, 7).Style.Font.FontColor = COLOR_ERROR;
            row += 2;

            // Encabezados
            var encabezados = new[] {
                "📦 Producto", "🏷️ Categoría", "📊 Stock Actual", "⚠️ Stock Mínimo",
                "💰 Valor Afectado", "🏢 Proveedor", "🆘 Nivel de Urgencia"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Ordenar por urgencia (stock más bajo primero)
            var productosOrdenados = productosStockBajo
                .OrderBy(p => p.StockTotal)
                .ToList();

            // Datos
            foreach (var producto in productosOrdenados)
            {
                var nivelUrgencia = producto.StockTotal <= 0 ? "🆘 CRÍTICO" :
                                   producto.StockTotal <= producto.AlertaStockBajo * 0.5m ? "🔴 ALTO" : "🟡 MEDIO";

                ws.Cell(row, 1).Value = producto.NombreArticulo ?? "Sin nombre";
                ws.Cell(row, 2).Value = producto.Categoria ?? "Sin categoría";
                ws.Cell(row, 3).Value = producto.StockTotal;
                ws.Cell(row, 4).Value = producto.AlertaStockBajo;
                ws.Cell(row, 5).Value = producto.ValorTotalConIVA;
                ws.Cell(row, 6).Value = producto.Proveedor ?? "Sin proveedor";
                ws.Cell(row, 7).Value = nivelUrgencia;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según urgencia
                if (producto.StockTotal <= 0)
                {
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                    ws.Cell(row, 7).Style.Font.FontColor = COLOR_ERROR;
                }
                else if (producto.StockTotal <= producto.AlertaStockBajo * 0.5m)
                {
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Cell(row, 7).Style.Font.FontColor = COLOR_AMARILLO;
                }

                ws.Cell(row, 7).Style.Font.Bold = true;
                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(4, 1, row - 1, 7);
            dataRange.SetAutoFilter();
        }

        private async Task CrearHojaMovimientos(IXLWorksheet ws, ReporteMovimientos reporteMovimientos)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📈 ANÁLISIS DE MOVIMIENTOS DE STOCK";
            ws.Range(row, 1, row, 8).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 8));
            row += 2;

            // Resumen de movimientos CORREGIDO
            ws.Cell(row, 1).Value = "📊 RESUMEN DEL PERÍODO";
            ws.Range(row, 1, row, 8).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 8));
            row++;

            var resumenMovimientos = new[]
            {
                ("📅 Período:", reporteMovimientos.ObtenerInfoPeriodo()),
                ("📈 Total Entradas:", $"{reporteMovimientos.TotalEntradas:F2} unidades"),
                ("📉 Total Salidas:", $"{reporteMovimientos.TotalSalidas:F2} unidades"),
                ("⚖️ Diferencia Neta:", $"{reporteMovimientos.DiferenciaNeta:F2} unidades"),
                ("💵 Valor Total Movido:", reporteMovimientos.ValorTotalMovido.ToString("C2", _cultura)),
                ("🔄 Total Movimientos:", reporteMovimientos.TotalMovimientos.ToString("N0"))
            };

            foreach (var (titulo, valor) in resumenMovimientos)
            {
                ws.Cell(row, 1).Value = titulo;
                ws.Cell(row, 2).Value = valor;
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
            }
            row++;

            // Detalle por producto CORREGIDO
            if (reporteMovimientos.EstadisticasPorProducto.Any())
            {
                ws.Cell(row, 1).Value = "📋 DETALLE POR PRODUCTO";
                ws.Range(row, 1, row, 8).Merge();
                EstiloSubEncabezado(ws.Range(row, 1, row, 8));
                row++;

                // Encabezados
                var encabezados = new[] {
                    "📦 Producto", "🏷️ Categoría", "📊 Stock Inicial", "📈 Entradas",
                    "📉 Salidas", "📊 Stock Final", "💵 Valor Movido", "🔄 Movimientos"
                };

                for (int i = 0; i < encabezados.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = encabezados[i];
                }
                EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
                row++;

                // Datos de estadísticas
                foreach (var estadistica in reporteMovimientos.EstadisticasPorProducto.Take(100)) // Permitir más datos en Excel
                {
                    ws.Cell(row, 1).Value = estadistica.NombreProducto;
                    ws.Cell(row, 2).Value = estadistica.Categoria;
                    ws.Cell(row, 3).Value = estadistica.StockInicial;
                    ws.Cell(row, 4).Value = estadistica.Entradas;
                    ws.Cell(row, 5).Value = estadistica.Salidas;
                    ws.Cell(row, 6).Value = estadistica.StockFinal;
                    ws.Cell(row, 7).Value = estadistica.ValorMovido;
                    ws.Cell(row, 8).Value = estadistica.CantidadMovimientos;

                    // Formateo
                    ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";

                    // Colorear entradas y salidas
                    if (estadistica.Entradas > 0)
                        ws.Cell(row, 4).Style.Font.FontColor = COLOR_EXITO;
                    if (estadistica.Salidas > 0)
                        ws.Cell(row, 5).Style.Font.FontColor = COLOR_ERROR;

                    row++;
                }

                ws.ColumnsUsed().AdjustToContents();
                if (reporteMovimientos.EstadisticasPorProducto.Count > 0)
                {
                    var dataRange = ws.Range(row - reporteMovimientos.EstadisticasPorProducto.Take(100).Count(), 1, row - 1, 8);
                    dataRange.SetAutoFilter();
                }
            }

            // ✅ NUEVA HOJA: Detalle cronológico de movimientos
            if (reporteMovimientos.Movimientos.Any())
            {
                var wsDetalleMovimientos = ws.Workbook.Worksheets.Add("📅 Detalle Cronológico");
                CrearHojaDetalleMovimientos(wsDetalleMovimientos, reporteMovimientos);
            }
        }

        /// <summary>
        /// ✅ NUEVA HOJA: Crea hoja con detalle cronológico de cada movimiento
        /// </summary>
        private void CrearHojaDetalleMovimientos(IXLWorksheet ws, ReporteMovimientos reporteMovimientos)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📅 DETALLE CRONOLÓGICO DE MOVIMIENTOS";
            ws.Range(row, 1, row, 10).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 10));
            row += 2;

            // Información del período
            ws.Cell(row, 1).Value = $"📊 {reporteMovimientos.ObtenerInfoPeriodo()} | {reporteMovimientos.ObtenerResumenRapido()}";
            ws.Range(row, 1, row, 10).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 10));
            row += 2;

            // Encabezados de la tabla
            var encabezados = new[] {
                "📅 Fecha", "🕐 Hora", "📦 Producto", "🔄 Tipo", "📊 Cantidad",
                "📏 Unidad", "💰 Valor", "👤 Usuario", "📝 Motivo", "📋 Documento"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos ordenados cronológicamente (más recientes primero)
            var movimientosOrdenados = reporteMovimientos.Movimientos
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(1000) // Límite para Excel
                .ToList();

            foreach (var movimiento in movimientosOrdenados)
            {
                ws.Cell(row, 1).Value = movimiento.FechaMovimiento.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = movimiento.FechaMovimiento.ToString("HH:mm:ss");
                ws.Cell(row, 3).Value = movimiento.RawMaterial?.NombreArticulo ?? "Producto desconocido";
                ws.Cell(row, 4).Value = $"{movimiento.TipoMovimientoIcon} {movimiento.TipoMovimiento}";
                ws.Cell(row, 5).Value = movimiento.Cantidad;
                ws.Cell(row, 6).Value = movimiento.UnidadMedida ?? "";
                ws.Cell(row, 7).Value = movimiento.ValorTotalConIVA;
                ws.Cell(row, 8).Value = movimiento.Usuario ?? "";
                ws.Cell(row, 9).Value = movimiento.Motivo ?? "";
                ws.Cell(row, 10).Value = movimiento.NumeroDocumento ?? "";

                // Formateo
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según tipo de movimiento
                if (movimiento.EsEntrada)
                {
                    ws.Cell(row, 4).Style.Font.FontColor = COLOR_EXITO;
                    ws.Cell(row, 5).Style.Font.FontColor = COLOR_EXITO;
                }
                else if (movimiento.EsSalida)
                {
                    ws.Cell(row, 4).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 5).Style.Font.FontColor = COLOR_ERROR;
                }

                // Alternar colores de fila
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();

            if (movimientosOrdenados.Count > 0)
            {
                var dataRange = ws.Range(4, 1, row - 1, 10);
                dataRange.SetAutoFilter();

                // Congelar paneles
                ws.SheetView.FreezeRows(4);
                ws.SheetView.FreezeColumns(3);
            }

            if (reporteMovimientos.Movimientos.Count > 1000)
            {
                ws.Cell(row + 1, 1).Value = $"📝 Nota: Se muestran los últimos 1000 movimientos de un total de {reporteMovimientos.Movimientos.Count} movimientos";
                ws.Range(row + 1, 1, row + 1, 10).Merge();
                ws.Range(row + 1, 1, row + 1, 10).Style.Font.Italic = true;
                ws.Range(row + 1, 1, row + 1, 10).Style.Fill.BackgroundColor = COLOR_AMARILLO;
            }
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

                Debug.WriteLine($"📊 [Excel] Obteniendo movimientos para {productos.Count} productos, período: {periodo}");

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

                Debug.WriteLine($"📊 [Excel] Encontrados {movimientos.Count} movimientos en el período");

                // ✅ CREAR EL REPORTE FINAL CON LOS MOVIMIENTOS OBTENIDOS
                var reporteMovimientos = new ReporteMovimientos(productos, movimientos, periodo);

                return reporteMovimientos;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [Excel] Error al obtener reporte de movimientos: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos de Estilo

        private void EstiloEncabezadoPrincipal(IXLRange range)
        {
            range.Style.Fill.BackgroundColor = COLOR_ENCABEZADO;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 16;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
        }

        private void EstiloSubEncabezado(IXLRange range)
        {
            range.Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;
            range.Style.Font.FontColor = COLOR_ENCABEZADO;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 14;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        private void EstiloEncabezadoTabla(IXLRange range)
        {
            range.Style.Fill.BackgroundColor = COLOR_AZUL;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 12;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private void EstiloInformacionGeneral(IXLRange range)
        {
            range.Style.Font.FontSize = 11;
            range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        #endregion

        #region Configuración y Utilidades

        private void ConfigurarLibroCompleto(XLWorkbook workbook, PeriodoReporte periodo, TipoFormatoReporte tipoFormato)
        {
            // Configurar propiedades del libro
            workbook.Properties.Title = $"Reporte de Stock - {ObtenerNombreFormato(tipoFormato)}";
            workbook.Properties.Subject = $"Análisis de Inventario - {ObtenerNombrePeriodo(periodo)}";
            workbook.Properties.Author = "Sistema Costo-Beneficio";
            workbook.Properties.Company = "Tu Empresa";
            workbook.Properties.Created = DateTime.Now;

            // Configurar todas las hojas
            foreach (var ws in workbook.Worksheets)
            {
                // Configuraciones generales
                ws.Style.Font.FontName = "Arial";
                ws.Style.Font.FontSize = 10;

                // Márgenes para impresión
                ws.PageSetup.Margins.Top = 0.75;
                ws.PageSetup.Margins.Bottom = 0.75;
                ws.PageSetup.Margins.Left = 0.7;
                ws.PageSetup.Margins.Right = 0.7;

                // Orientación y escala
                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                ws.PageSetup.FitToPages(1, 0); // Ajustar al ancho de página

                // Encabezado y pie de página
                ws.PageSetup.Header.Center.AddText($"Reporte de Stock - {DateTime.Now:dd/MM/yyyy}");
                ws.PageSetup.Footer.Left.AddText("Sistema Costo-Beneficio");
                ws.PageSetup.Footer.Right.AddText("Página &P de &N");
            }
        }

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
                    Title = "Guardar Reporte de Stock en Excel",
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
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
                TipoFormatoReporte.SoloStockBajo => "Solo_Stock_Bajo",
                _ => "Estandar"
            };
        }

        public void AbrirExcel(string rutaExcel)
        {
            try
            {
                if (File.Exists(rutaExcel))
                {
                    Process.Start(new ProcessStartInfo(rutaExcel) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("El archivo Excel no fue encontrado.",
                        "Archivo No Encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el Excel: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}