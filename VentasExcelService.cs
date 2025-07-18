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
    /// Servicio Excel profesional para reportes de ventas
    /// Compatible con sistema de análisis de ventas y rentabilidad
    /// </summary>
    public class VentasExcelService
    {
        #region Configuración

        private readonly CultureInfo _cultura;
        private readonly AppDbContext _context;

        // Colores corporativos para ClosedXML - Ventas
        private static readonly XLColor COLOR_ENCABEZADO = XLColor.FromHtml("#059669");
        private static readonly XLColor COLOR_EXITO = XLColor.FromHtml("#10B981");
        private static readonly XLColor COLOR_ERROR = XLColor.FromHtml("#EF4444");
        private static readonly XLColor COLOR_GRIS_CLARO = XLColor.FromHtml("#F3F4F6");
        private static readonly XLColor COLOR_AMARILLO = XLColor.FromHtml("#F59E0B");
        private static readonly XLColor COLOR_AZUL = XLColor.FromHtml("#3B82F6");
        private static readonly XLColor COLOR_MORADO = XLColor.FromHtml("#8B5CF6");

        public VentasExcelService(AppDbContext context = null)
        {
            _cultura = CultureInfo.GetCultureInfo("es-MX");
            _context = context;
        }

        #endregion

        #region Método Principal

        public async Task<string> GenerarReporteExcelAsync(
            List<Venta> ventas,
            PeriodoReporte periodo,
            TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar,
            FiltrosAplicados filtrosAplicados = null)
        {
            try
            {
                Debug.WriteLine("📊 [Excel Ventas] Iniciando generación...");

                if (!ValidarParametrosEntrada(ventas))
                    return null;

                var rutaDestino = MostrarSaveFileDialog(periodo, tipoFormato);
                if (string.IsNullOrEmpty(rutaDestino))
                    return null;

                await Task.Run(async () => await GenerarExcelVentasAsync(ventas, periodo, tipoFormato, filtrosAplicados, rutaDestino));

                Debug.WriteLine("✅ [Excel Ventas] Completado exitosamente");
                return rutaDestino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [Excel Ventas] Error: {ex}");
                MessageBox.Show($"Error al generar Excel de ventas:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Generación Excel Profesional

        private async Task GenerarExcelVentasAsync(List<Venta> ventas, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros, string rutaDestino)
        {
            // Crear reporte de análisis
            var reporteVentas = new ReporteVentas(ventas, periodo);

            using (var workbook = new XLWorkbook())
            {
                // ===== HOJA 1: RESUMEN EJECUTIVO =====
                var wsResumen = workbook.Worksheets.Add("📊 Resumen Ejecutivo");
                await CrearHojaResumenEjecutivo(wsResumen, reporteVentas, periodo, tipoFormato, filtros);

                // ===== HOJA 2: DETALLE DE VENTAS =====
                var wsDetalle = workbook.Worksheets.Add("💰 Detalle de Ventas");
                CrearHojaDetalleVentas(wsDetalle, ventas, tipoFormato);

                // ===== HOJA 3: ANÁLISIS POR PRODUCTOS =====
                var wsProductos = workbook.Worksheets.Add("📦 Por Productos");
                CrearHojaAnalisisProductos(wsProductos, reporteVentas.AnalisisPorProducto);

                // ===== HOJA 4: ANÁLISIS POR CLIENTES =====
                var wsClientes = workbook.Worksheets.Add("👥 Por Clientes");
                CrearHojaAnalisisClientes(wsClientes, reporteVentas.AnalisisPorCliente);

                // ===== HOJA 5: ANÁLISIS POR USUARIOS =====
                var wsUsuarios = workbook.Worksheets.Add("👤 Por Usuarios");
                CrearHojaAnalisisUsuarios(wsUsuarios, reporteVentas.AnalisisPorUsuario);

                // ===== HOJA 6: FORMAS DE PAGO =====
                var wsFormasPago = workbook.Worksheets.Add("💳 Formas de Pago");
                CrearHojaFormasPago(wsFormasPago, reporteVentas.AnalisisPorFormaPago);

                // ===== HOJA 7: ANÁLISIS TEMPORAL =====
                var wsTemporal = workbook.Worksheets.Add("📅 Análisis Temporal");
                CrearHojaAnalisisTemporal(wsTemporal, reporteVentas);

                // ===== HOJA 8: RENTABILIDAD =====
                var wsRentabilidad = workbook.Worksheets.Add("📈 Rentabilidad");
                CrearHojaRentabilidad(wsRentabilidad, reporteVentas);

                // Configuraciones globales
                ConfigurarLibroCompleto(workbook, periodo, tipoFormato);

                // Guardar archivo
                workbook.SaveAs(rutaDestino);
            }
        }

        #endregion

        #region Crear Hojas Específicas

        private async Task CrearHojaResumenEjecutivo(IXLWorksheet ws, ReporteVentas reporte, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros)
        {
            var row = 1;

            // ===== ENCABEZADO PRINCIPAL =====
            ws.Cell(row, 1).Value = "🎯 REPORTE EJECUTIVO DE VENTAS";
            ws.Range(row, 1, row, 6).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 6));
            row += 2;

            // ===== INFORMACIÓN DEL REPORTE =====
            ws.Cell(row, 1).Value = "📅 Período:";
            ws.Cell(row, 2).Value = reporte.ObtenerNombrePeriodo();
            ws.Cell(row, 4).Value = "📋 Formato:";
            ws.Cell(row, 5).Value = ObtenerNombreFormato(tipoFormato);
            EstiloInformacionGeneral(ws.Range(row, 1, row, 6));
            row++;

            ws.Cell(row, 1).Value = "🕐 Generado:";
            ws.Cell(row, 2).Value = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm", _cultura);
            ws.Cell(row, 4).Value = "👤 Usuario:";
            ws.Cell(row, 5).Value = Environment.UserName;
            EstiloInformacionGeneral(ws.Range(row, 1, row, 6));
            row++;

            ws.Cell(row, 1).Value = "📊 Rango de fechas:";
            ws.Cell(row, 2).Value = reporte.ObtenerInfoPeriodo();
            EstiloInformacionGeneral(ws.Range(row, 1, row, 6));
            row += 2;

            // ===== MÉTRICAS PRINCIPALES =====
            ws.Cell(row, 1).Value = "💰 MÉTRICAS PRINCIPALES";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var metricas = new[]
            {
                ("🎯 Total de Ventas", reporte.TotalVentas.ToString("N0"), COLOR_ENCABEZADO),
                ("💰 Ingresos Totales", reporte.TotalIngresos.ToString("C2", _cultura), COLOR_EXITO),
                ("📈 Ganancia Bruta", reporte.GananciaBrutaTotal.ToString("C2", _cultura), COLOR_EXITO),
                ("💎 Ganancia Neta", reporte.GananciaNetaTotal.ToString("C2", _cultura), COLOR_EXITO),
                ("📊 Margen Promedio Bruto", $"{reporte.MargenPromedioBruto:F2}%", COLOR_AMARILLO),
                ("📈 Margen Promedio Neto", $"{reporte.MargenPromedioNeto:F2}%", COLOR_AMARILLO),
                ("💳 Total Comisiones", reporte.TotalComisiones.ToString("C2", _cultura), COLOR_ERROR),
                ("🎫 Promedio por Ticket", reporte.PromedioVentaPorTicket.ToString("C2", _cultura), COLOR_AZUL),
                ("👥 Clientes Únicos", reporte.ClientesUnicos.ToString("N0"), COLOR_AZUL),
                ("📦 Productos Vendidos", reporte.ProductosDiferentesVendidos.ToString("N0"), COLOR_AZUL),
                ("📊 Unidades Totales", reporte.TotalUnidadesVendidas.ToString("F1"), COLOR_MORADO),
                ("💰 Costo Total", reporte.TotalCostos.ToString("C2", _cultura), COLOR_ERROR)
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

                if (filtros.ClientesSeleccionados.Any())
                {
                    ws.Cell(row, 1).Value = "👥 Clientes:";
                    ws.Cell(row, 2).Value = string.Join(", ", filtros.ClientesSeleccionados);
                    row++;
                }

                if (filtros.UsuariosSeleccionados.Any())
                {
                    ws.Cell(row, 1).Value = "👤 Usuarios:";
                    ws.Cell(row, 2).Value = string.Join(", ", filtros.UsuariosSeleccionados);
                    row++;
                }

                if (filtros.ProductosSeleccionados.Any())
                {
                    ws.Cell(row, 1).Value = "📦 Productos:";
                    ws.Cell(row, 2).Value = string.Join(", ", filtros.ProductosSeleccionados);
                    row++;
                }

                if (filtros.SoloVentasRentables)
                {
                    ws.Cell(row, 1).Value = "📈 Solo ventas rentables:";
                    ws.Cell(row, 2).Value = "Sí";
                    row++;
                }

                row++;
            }

            // ===== RESUMEN RÁPIDO =====
            ws.Cell(row, 1).Value = "🚀 RESUMEN RÁPIDO";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            ws.Cell(row, 1).Value = reporte.ObtenerResumenRapido();
            ws.Range(row, 1, row, 6).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;

            // Ajustar columnas
            ws.ColumnsUsed().AdjustToContents();
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 25);
        }

        private void CrearHojaDetalleVentas(IXLWorksheet ws, List<Venta> ventas, TipoFormatoReporte tipoFormato)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "💰 DETALLE COMPLETO DE VENTAS";
            ws.Range(row, 1, row, 12).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 12));
            row += 2;

            // Encabezados de columnas
            var encabezados = new[] {
                "🎫 Ticket", "📅 Fecha", "🕐 Hora", "👤 Cliente", "👨‍💼 Usuario",
                "💰 SubTotal", "💎 Total", "💳 Forma Pago", "📈 Ganancia", "📊 Margen %", "🏦 Comisión", "💵 Neto"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos de ventas
            var ventasOrdenadas = ventas.OrderByDescending(v => v.FechaVenta).ToList();

            foreach (var venta in ventasOrdenadas)
            {
                ws.Cell(row, 1).Value = venta.NumeroTicket;
                ws.Cell(row, 2).Value = venta.FechaVenta.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = venta.FechaVenta.ToString("HH:mm");
                ws.Cell(row, 4).Value = venta.Cliente ?? "Sin cliente";
                ws.Cell(row, 5).Value = venta.Usuario ?? "Sin usuario";
                ws.Cell(row, 6).Value = venta.SubTotal;
                ws.Cell(row, 7).Value = venta.Total;
                ws.Cell(row, 8).Value = DeterminarFormaPago(venta);
                ws.Cell(row, 9).Value = venta.GananciaNeta;
                ws.Cell(row, 10).Value = venta.MargenNeto;
                ws.Cell(row, 11).Value = venta.ComisionTotal;
                ws.Cell(row, 12).Value = venta.TotalRealRecibido;

                // Formateo especial
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 10).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 12).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según rentabilidad
                if (venta.MargenNeto > 30)
                {
                    ws.Range(row, 1, row, 12).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                    ws.Cell(row, 10).Style.Font.FontColor = COLOR_EXITO;
                }
                else if (venta.MargenNeto < 10)
                {
                    ws.Range(row, 1, row, 12).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                    ws.Cell(row, 10).Style.Font.FontColor = COLOR_ERROR;
                }

                ws.Cell(row, 10).Style.Font.Bold = true;

                // Alternar colores de fila
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 12).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;
                }

                row++;
            }

            // Ajustar columnas y aplicar filtros
            ws.ColumnsUsed().AdjustToContents();

            // Aplicar AutoFilter
            var dataRange = ws.Range(3, 1, row - 1, 12);
            dataRange.SetAutoFilter();

            // Congelar paneles (fila 4, columna 2)
            ws.SheetView.FreezeRows(3);
            ws.SheetView.FreezeColumns(2);
        }

        private void CrearHojaAnalisisProductos(IXLWorksheet ws, List<AnalisisProductoVendido> productos)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📦 ANÁLISIS POR PRODUCTOS";
            ws.Range(row, 1, row, 8).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 8));
            row += 2;

            // Encabezados
            var encabezados = new[] {
                "🏆 Pos.", "📦 Producto", "📊 Vendido", "🎫 Ventas", "💰 Ingresos",
                "💎 Ganancia", "📈 Margen %", "💲 Precio Prom."
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos ordenados por ingresos
            var productosOrdenados = productos.OrderByDescending(p => p.IngresoTotal).ToList();

            foreach (var (producto, posicion) in productosOrdenados.Select((p, i) => (p, i + 1)))
            {
                var emoji = posicion switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => posicion.ToString()
                };

                ws.Cell(row, 1).Value = emoji;
                ws.Cell(row, 2).Value = producto.NombreProducto;
                ws.Cell(row, 3).Value = producto.CantidadVendida;
                ws.Cell(row, 4).Value = producto.VentasRealizadas;
                ws.Cell(row, 5).Value = producto.IngresoTotal;
                ws.Cell(row, 6).Value = producto.GananciaTotal;
                ws.Cell(row, 7).Value = producto.MargenPromedio / 100; // Convertir a porcentaje
                ws.Cell(row, 8).Value = producto.PrecioPromedioVenta;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según margen
                if (producto.MargenPromedio > 30)
                {
                    ws.Cell(row, 7).Style.Font.FontColor = COLOR_EXITO;
                    ws.Cell(row, 7).Style.Font.Bold = true;
                }
                else if (producto.MargenPromedio < 10)
                {
                    ws.Cell(row, 7).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 7).Style.Font.Bold = true;
                }

                // Destacar top 3
                if (posicion <= 3)
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Cell(row, 1).Style.Font.FontSize = 14;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 8);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaAnalisisClientes(IXLWorksheet ws, List<AnalisisCliente> clientes)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "👥 ANÁLISIS POR CLIENTES";
            ws.Range(row, 1, row, 9).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 9));
            row += 2;

            // Encabezados
            var encabezados = new[] {
                "👑 Cliente", "🛒 Compras", "💰 Total Gastado", "📊 Promedio",
                "💎 Ganancia", "📦 Productos", "📅 Última Compra", "⭐ Tipo", "📈 Estado"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos ordenados por total gastado
            var clientesOrdenados = clientes.OrderByDescending(c => c.TotalGastado).ToList();

            foreach (var cliente in clientesOrdenados)
            {
                var tipoCliente = cliente.EsClienteValioso ? "💎 VIP" :
                                 cliente.EsClienteFrecuente ? "⭐ Frecuente" : "👤 Regular";

                var estadoCliente = cliente.DiasSinComprar <= 7 ? "🟢 Activo" :
                                   cliente.DiasSinComprar <= 30 ? "🟡 Normal" : "🔴 Inactivo";

                ws.Cell(row, 1).Value = cliente.NombreCliente;
                ws.Cell(row, 2).Value = cliente.CantidadCompras;
                ws.Cell(row, 3).Value = cliente.TotalGastado;
                ws.Cell(row, 4).Value = cliente.PromedioCompra;
                ws.Cell(row, 5).Value = cliente.GananciaGenerada;
                ws.Cell(row, 6).Value = cliente.ProductosComprados;
                ws.Cell(row, 7).Value = cliente.UltimaCompra.ToString("dd/MM/yyyy");
                ws.Cell(row, 8).Value = tipoCliente;
                ws.Cell(row, 9).Value = estadoCliente;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según tipo de cliente
                if (cliente.EsClienteValioso)
                {
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Cell(row, 8).Style.Font.FontColor = COLOR_AMARILLO;
                    ws.Cell(row, 8).Style.Font.Bold = true;
                }
                else if (cliente.EsClienteFrecuente)
                {
                    ws.Cell(row, 8).Style.Font.FontColor = COLOR_EXITO;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 9);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaAnalisisUsuarios(IXLWorksheet ws, List<AnalisisUsuarioVendedor> usuarios)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "👤 ANÁLISIS POR USUARIOS/VENDEDORES";
            ws.Range(row, 1, row, 9).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 9));
            row += 2;

            // Encabezados
            var encabezados = new[] {
                "🏅 Usuario", "🎯 Ventas", "💰 Total Vendido", "📊 Promedio",
                "💎 Ganancia", "📈 Margen %", "👥 Clientes", "📦 Productos", "🏆 Rendimiento"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos ordenados por total vendido
            var usuariosOrdenados = usuarios.OrderByDescending(u => u.TotalVendido).ToList();

            foreach (var (usuario, posicion) in usuariosOrdenados.Select((u, i) => (u, i + 1)))
            {
                var rendimiento = usuario.EsVendedorEficiente ? "🏆 Excelente" :
                                 usuario.MargenPromedio > 15 ? "⭐ Bueno" : "📈 Regular";

                var emoji = posicion == 1 ? "👑" : usuario.EsVendedorEficiente ? "🏆" : "👤";

                ws.Cell(row, 1).Value = $"{emoji} {usuario.NombreUsuario}";
                ws.Cell(row, 2).Value = usuario.VentasRealizadas;
                ws.Cell(row, 3).Value = usuario.TotalVendido;
                ws.Cell(row, 4).Value = usuario.PromedioVenta;
                ws.Cell(row, 5).Value = usuario.GananciaGenerada;
                ws.Cell(row, 6).Value = usuario.MargenPromedio / 100; // Convertir a porcentaje
                ws.Cell(row, 7).Value = usuario.ClientesAtendidos;
                ws.Cell(row, 8).Value = usuario.ProductosVendidos;
                ws.Cell(row, 9).Value = rendimiento;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.0";

                // Colorear según rendimiento
                if (usuario.EsVendedorEficiente)
                {
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                    ws.Cell(row, 6).Style.Font.FontColor = COLOR_EXITO;
                    ws.Cell(row, 9).Style.Font.FontColor = COLOR_EXITO;
                }
                else if (usuario.MargenPromedio < 10)
                {
                    ws.Cell(row, 6).Style.Font.FontColor = COLOR_ERROR;
                }

                ws.Cell(row, 6).Style.Font.Bold = true;
                ws.Cell(row, 9).Style.Font.Bold = true;

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 9);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaFormasPago(IXLWorksheet ws, List<AnalisisFormaPago> formasPago)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "💳 ANÁLISIS POR FORMAS DE PAGO";
            ws.Range(row, 1, row, 7).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 7));
            row += 2;

            // Encabezados
            var encabezados = new[] {
                "💳 Forma de Pago", "🎫 Transacciones", "💰 Monto Total", "📊 Promedio",
                "📈 % del Total", "🏦 Comisiones", "💵 Monto Neto"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos
            foreach (var formaPago in formasPago)
            {
                ws.Cell(row, 1).Value = formaPago.FormaPago;
                ws.Cell(row, 2).Value = formaPago.CantidadTransacciones;
                ws.Cell(row, 3).Value = formaPago.MontoTotal;
                ws.Cell(row, 4).Value = formaPago.PromedioTransaccion;
                ws.Cell(row, 5).Value = formaPago.PorcentajeDelTotal / 100; // Convertir a porcentaje
                ws.Cell(row, 6).Value = formaPago.ComisionTotal;
                ws.Cell(row, 7).Value = formaPago.MontoNeto;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear comisiones
                if (formaPago.ComisionTotal > 0)
                {
                    ws.Cell(row, 6).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 6).Style.Font.Bold = true;
                }

                // Resaltar el método más usado
                if (formaPago.PorcentajeDelTotal > 40)
                {
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 7);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaAnalisisTemporal(IXLWorksheet ws, ReporteVentas reporte)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📅 ANÁLISIS TEMPORAL DE VENTAS";
            ws.Range(row, 1, row, 4).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 4));
            row += 2;

            // Análisis por horarios
            ws.Cell(row, 1).Value = "🕐 VENTAS POR HORARIO";
            ws.Range(row, 1, row, 4).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 4));
            row++;

            var encabezadosHora = new[] { "🕐 Hora", "🎫 Ventas", "💰 Monto", "📊 Promedio" };
            for (int i = 0; i < encabezadosHora.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezadosHora[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezadosHora.Length));
            row++;

            var ventasPorHora = reporte.ObtenerVentasPorHora().Where(h => h.CantidadVentas > 0).ToList();
            foreach (var horario in ventasPorHora)
            {
                ws.Cell(row, 1).Value = $"{horario.Hora:D2}:00";
                ws.Cell(row, 2).Value = horario.CantidadVentas;
                ws.Cell(row, 3).Value = horario.MontoVentas;
                ws.Cell(row, 4).Value = horario.CantidadVentas > 0 ? horario.MontoVentas / horario.CantidadVentas : 0;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";

                // Resaltar horarios pico
                if (horario.CantidadVentas >= ventasPorHora.Max(h => h.CantidadVentas) * 0.8)
                {
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Range(row, 1, row, 4).Style.Font.Bold = true;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
        }

        private void CrearHojaRentabilidad(IXLWorksheet ws, ReporteVentas reporte)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📈 ANÁLISIS DE RENTABILIDAD";
            ws.Range(row, 1, row, 6).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 6));
            row += 2;

            // Resumen financiero
            ws.Cell(row, 1).Value = "💰 RESUMEN FINANCIERO";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var datosFinancieros = new[]
            {
                ("💰 Ingresos Totales", reporte.TotalIngresos),
                ("📉 Costos Totales", reporte.TotalCostos),
                ("📈 Ganancia Bruta", reporte.GananciaBrutaTotal),
                ("🏦 Comisiones Pagadas", reporte.TotalComisiones),
                ("💎 Ganancia Neta", reporte.GananciaNetaTotal)
            };

            foreach (var (concepto, valor) in datosFinancieros)
            {
                ws.Cell(row, 1).Value = concepto;
                ws.Cell(row, 2).Value = valor;
                ws.Cell(row, 3).Value = reporte.TotalIngresos > 0 ? (valor / reporte.TotalIngresos) : 0;

                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";

                if (concepto.Contains("Ganancia"))
                {
                    ws.Cell(row, 2).Style.Font.FontColor = COLOR_EXITO;
                    ws.Cell(row, 2).Style.Font.Bold = true;
                }
                else if (concepto.Contains("Costo") || concepto.Contains("Comisiones"))
                {
                    ws.Cell(row, 2).Style.Font.FontColor = COLOR_ERROR;
                }

                row++;
            }

            row += 2;

            // Top productos más rentables
            ws.Cell(row, 1).Value = "🏆 TOP PRODUCTOS MÁS RENTABLES";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var encabezadosRentables = new[] { "📦 Producto", "💰 Ingresos", "💎 Ganancia", "📈 Margen", "🎯 Ventas", "📊 Cantidad" };
            for (int i = 0; i < encabezadosRentables.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezadosRentables[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezadosRentables.Length));
            row++;

            var productosRentables = reporte.ObtenerProductosMayorMargen(10);
            foreach (var producto in productosRentables)
            {
                ws.Cell(row, 1).Value = producto.NombreProducto;
                ws.Cell(row, 2).Value = producto.IngresoTotal;
                ws.Cell(row, 3).Value = producto.GananciaTotal;
                ws.Cell(row, 4).Value = producto.MargenPromedio / 100;
                ws.Cell(row, 5).Value = producto.VentasRealizadas;
                ws.Cell(row, 6).Value = producto.CantidadVendida;

                // Formateo
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.0";

                // Colorear según rentabilidad
                if (producto.MargenPromedio > 40)
                {
                    ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                    ws.Cell(row, 4).Style.Font.FontColor = COLOR_EXITO;
                }

                ws.Cell(row, 4).Style.Font.Bold = true;
                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
        }

        #endregion

        #region Métodos de Utilidad y Estilo

        private string DeterminarFormaPago(Venta venta)
        {
            var formas = new List<string>();
            if (venta.MontoEfectivo > 0) formas.Add("💵 Efectivo");
            if (venta.MontoTarjeta > 0) formas.Add("💳 Tarjeta");
            if (venta.MontoTransferencia > 0) formas.Add("📱 Transferencia");

            if (formas.Count > 1)
                return "🔄 Combinado";
            else if (formas.Count == 1)
                return formas[0];
            else
                return "❓ Desconocido";
        }

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

        private void ConfigurarLibroCompleto(XLWorkbook workbook, PeriodoReporte periodo, TipoFormatoReporte tipoFormato)
        {
            // Configurar propiedades del libro
            workbook.Properties.Title = $"Reporte de Ventas - {ObtenerNombreFormato(tipoFormato)}";
            workbook.Properties.Subject = $"Análisis de Ventas - {ObtenerNombrePeriodo(periodo)}";
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
                ws.PageSetup.Header.Center.AddText($"Reporte de Ventas - {DateTime.Now:dd/MM/yyyy}");
                ws.PageSetup.Footer.Left.AddText("Sistema Costo-Beneficio");
                ws.PageSetup.Footer.Right.AddText("Página &P de &N");
            }
        }

        private bool ValidarParametrosEntrada(List<Venta> ventas)
        {
            if (ventas == null || !ventas.Any())
            {
                MessageBox.Show("No hay ventas seleccionadas para el reporte.",
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
                    Title = "Guardar Reporte de Ventas en Excel",
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    AddExtension = true,
                    FileName = $"ReporteVentas_{ObtenerNombreFormato(tipoFormato)}_{ObtenerNombrePeriodo(periodo)}_{DateTime.Now:yyyy-MM-dd}",
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
                TipoFormatoReporte.PorProductos => "Por_Productos",
                TipoFormatoReporte.PorClientes => "Por_Clientes",
                TipoFormatoReporte.PorUsuarios => "Por_Usuarios",
                TipoFormatoReporte.Financiero => "Financiero",
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