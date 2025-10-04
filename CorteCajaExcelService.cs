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
    /// Servicio Excel profesional para reportes de cortes de caja
    /// Compatible con sistema de análisis de cortes y conciliación
    /// </summary>
    public class CorteCajaExcelService
    {
        #region Configuración

        private readonly CultureInfo _cultura;
        private readonly AppDbContext _context;

        // Colores corporativos para ClosedXML - Cortes de Caja
        private static readonly XLColor COLOR_ENCABEZADO = XLColor.FromHtml("#059669");
        private static readonly XLColor COLOR_EXITO = XLColor.FromHtml("#10B981");
        private static readonly XLColor COLOR_ERROR = XLColor.FromHtml("#EF4444");
        private static readonly XLColor COLOR_GRIS_CLARO = XLColor.FromHtml("#F3F4F6");
        private static readonly XLColor COLOR_AMARILLO = XLColor.FromHtml("#F59E0B");
        private static readonly XLColor COLOR_AZUL = XLColor.FromHtml("#3B82F6");
        private static readonly XLColor COLOR_MORADO = XLColor.FromHtml("#8B5CF6");

        public CorteCajaExcelService(AppDbContext context = null)
        {
            _cultura = CultureInfo.GetCultureInfo("es-MX");
            _context = context;
        }

        #endregion

        #region Método Principal

        public async Task<string> GenerarReporteExcelAsync(
            List<CorteCaja> cortes,
            PeriodoReporte periodo,
            TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar,
            FiltrosAplicadosCorte filtrosAplicados = null)
        {
            try
            {
                Debug.WriteLine("📊 [Excel Cortes] Iniciando generación...");

                if (!ValidarParametrosEntrada(cortes))
                    return null;

                var rutaDestino = MostrarSaveFileDialog(periodo, tipoFormato);
                if (string.IsNullOrEmpty(rutaDestino))
                    return null;

                await Task.Run(async () => await GenerarExcelCortesAsync(cortes, periodo, tipoFormato, filtrosAplicados, rutaDestino));

                Debug.WriteLine("✅ [Excel Cortes] Completado exitosamente");
                return rutaDestino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [Excel Cortes] Error: {ex}");
                MessageBox.Show($"Error al generar Excel de cortes:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Generación Excel Profesional

        private async Task GenerarExcelCortesAsync(List<CorteCaja> cortes, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicadosCorte filtros, string rutaDestino)
        {
            // Crear reporte de análisis
            var reporteCortes = new ReporteCortesCaja(cortes, periodo);

            using (var workbook = new XLWorkbook())
            {
                // ===== HOJA 1: RESUMEN EJECUTIVO =====
                var wsResumen = workbook.Worksheets.Add("📊 Resumen Ejecutivo");
                await CrearHojaResumenEjecutivo(wsResumen, reporteCortes, periodo, tipoFormato, filtros);

                // ===== HOJA 2: DETALLE DE CORTES =====
                var wsDetalle = workbook.Worksheets.Add("💰 Detalle de Cortes");
                CrearHojaDetalleCortes(wsDetalle, cortes, tipoFormato);

                // ===== HOJA 3: ANÁLISIS POR USUARIOS =====
                var wsUsuarios = workbook.Worksheets.Add("👤 Por Usuarios");
                CrearHojaAnalisisUsuarios(wsUsuarios, reporteCortes.ObtenerAnalisisPorUsuario());

                // ===== HOJA 4: ANÁLISIS TEMPORAL =====
                var wsTemporal = workbook.Worksheets.Add("📅 Análisis Temporal");
                CrearHojaAnalisisTemporal(wsTemporal, reporteCortes);

                // ===== HOJA 5: FORMAS DE PAGO =====
                var wsFormasPago = workbook.Worksheets.Add("💳 Formas de Pago");
                CrearHojaFormasPago(wsFormasPago, reporteCortes);

                // ===== HOJA 6: COMISIONES =====
                var wsComisiones = workbook.Worksheets.Add("🏦 Comisiones");
                CrearHojaComisiones(wsComisiones, reporteCortes, cortes);

                // ===== HOJA 7: DIFERENCIAS Y CONCILIACIÓN =====
                var wsDiferencias = workbook.Worksheets.Add("📊 Diferencias");
                CrearHojaDiferencias(wsDiferencias, reporteCortes, cortes);

                // ===== HOJA 8: RENTABILIDAD =====
                var wsRentabilidad = workbook.Worksheets.Add("📈 Rentabilidad");
                CrearHojaRentabilidad(wsRentabilidad, reporteCortes);

                // Configuraciones globales
                ConfigurarLibroCompleto(workbook, periodo, tipoFormato);

                // Guardar archivo
                workbook.SaveAs(rutaDestino);
            }
        }

        #endregion

        #region Crear Hojas Específicas

        private async Task CrearHojaResumenEjecutivo(IXLWorksheet ws, ReporteCortesCaja reporte, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicadosCorte filtros)
        {
            var row = 1;

            // ===== ENCABEZADO PRINCIPAL =====
            ws.Cell(row, 1).Value = "🎯 REPORTE EJECUTIVO DE CORTES DE CAJA";
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
            ws.Cell(row, 5).Value = UserService.UsuarioActual?.NombreUsuario ?? Environment.UserName;
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
                ("🎯 Total de Cortes", reporte.TotalCortes.ToString("N0"), COLOR_ENCABEZADO),
                ("✅ Cortes Completados", reporte.CortesCompletados.ToString("N0"), COLOR_EXITO),
                ("⏳ Cortes Pendientes", reporte.CortesPendientes.ToString("N0"), COLOR_AMARILLO),
                ("❌ Cortes Cancelados", reporte.CortesCancelados.ToString("N0"), COLOR_ERROR),
                ("💰 Ventas Totales", reporte.TotalVentasDelPeriodo.ToString("C2", _cultura), COLOR_EXITO),
                ("🎫 Tickets Procesados", reporte.TotalTicketsProcesados.ToString("N0"), COLOR_AZUL),
                ("📊 Promedio Ventas/Día", reporte.PromedioVentasPorDia.ToString("C2", _cultura), COLOR_AZUL),
                ("🎫 Promedio Tickets/Día", reporte.PromedioTicketsPorDia.ToString("F1"), COLOR_AZUL),
                ("💎 Ganancia Neta Total", reporte.GananciaNetaTotal.ToString("C2", _cultura), COLOR_EXITO),
                ("📈 Margen Neto Promedio", $"{reporte.MargenNetoPromedio:F2}%", COLOR_AMARILLO),
                ("🏦 Total Comisiones", reporte.TotalComisionesDelPeriodo.ToString("C2", _cultura), COLOR_ERROR),
                ("👥 Usuarios Únicos", reporte.UsuariosUnicos.ToString("N0"), COLOR_MORADO)
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

            // ===== ANÁLISIS DE DIFERENCIAS =====
            ws.Cell(row, 1).Value = "📊 ANÁLISIS DE DIFERENCIAS";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var diferencias = new[]
            {
                ("✅ Cortes Exactos", reporte.CortesExactos.ToString("N0"), $"{reporte.PorcentajeCortesExactos:F1}%", COLOR_EXITO),
                ("📈 Cortes con Sobrante", reporte.CortesConSobrante.ToString("N0"), reporte.TotalSobrantes.ToString("C2", _cultura), COLOR_AMARILLO),
                ("📉 Cortes con Faltante", reporte.CortesConFaltante.ToString("N0"), reporte.TotalFaltantes.ToString("C2", _cultura), COLOR_ERROR),
                ("⚠️ Total con Diferencias", reporte.CortesConDiferencias.ToString("N0"), $"{reporte.PorcentajeCortesConDiferencias:F1}%", COLOR_ERROR),
                ("💰 Efectivo Esperado", "", reporte.TotalEfectivoEsperado.ToString("C2", _cultura), COLOR_AZUL),
                ("💵 Efectivo Contado", "", reporte.TotalEfectivoContado.ToString("C2", _cultura), COLOR_AZUL),
                ("📊 Diferencia Global", "", reporte.DiferenciaGlobalEfectivo.ToString("C2", _cultura),
                    reporte.DiferenciaGlobalEfectivo >= 0 ? COLOR_EXITO : COLOR_ERROR)
            };

            foreach (var (titulo, cantidad, valor, color) in diferencias)
            {
                ws.Cell(row, 1).Value = titulo;
                ws.Cell(row, 2).Value = cantidad;
                ws.Cell(row, 3).Value = valor;

                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Font.FontColor = color;
                ws.Cell(row, 3).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;

                row++;
            }

            // Ajustar columnas
            ws.ColumnsUsed().AdjustToContents();
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 25);
        }

        private void CrearHojaDetalleCortes(IXLWorksheet ws, List<CorteCaja> cortes, TipoFormatoReporte tipoFormato)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "💰 DETALLE COMPLETO DE CORTES DE CAJA";
            ws.Range(row, 1, row, 15).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 15));
            row += 2;

            // Encabezados de columnas
            var encabezados = new[] {
                "📅 Fecha", "🕐 Hora", "👤 Usuario", "📊 Estado",
                "🎫 Tickets", "💰 Ventas", "💵 Efectivo Esp.", "💵 Efectivo Cont.",
                "📊 Diferencia", "💳 Tarjeta", "📱 Transfer.", "🏦 Comisiones",
                "📈 Ganancia", "💎 Ganancia Neta", "📊 Margen %"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos de cortes
            var cortesOrdenados = cortes.OrderByDescending(c => c.FechaCorte).ToList();

            foreach (var corte in cortesOrdenados)
            {
                ws.Cell(row, 1).Value = corte.FechaCorte.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = corte.FechaHoraCorte.ToString("HH:mm");
                ws.Cell(row, 3).Value = corte.UsuarioCorte;
                ws.Cell(row, 4).Value = corte.Estado;
                ws.Cell(row, 5).Value = corte.CantidadTickets;
                ws.Cell(row, 6).Value = corte.TotalVentasCalculado;
                ws.Cell(row, 7).Value = corte.EfectivoEsperado;
                ws.Cell(row, 8).Value = corte.EfectivoContado;
                ws.Cell(row, 9).Value = corte.DiferenciaEfectivo;
                ws.Cell(row, 10).Value = corte.TarjetaCalculado;
                ws.Cell(row, 11).Value = corte.TransferenciaCalculado;
                ws.Cell(row, 12).Value = corte.ComisionesTotalesCalculadas;
                ws.Cell(row, 13).Value = corte.GananciaBrutaCalculada;
                ws.Cell(row, 14).Value = corte.GananciaNetaCalculada;
                ws.Cell(row, 15).Value = corte.TotalVentasCalculado > 0
                    ? (corte.GananciaNetaCalculada / corte.TotalVentasCalculado)
                    : 0;

                // Formateo
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 10).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 12).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 13).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 14).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 15).Style.NumberFormat.Format = "0.00%";

                // Colorear según estado
                if (corte.Estado == "Completado")
                {
                    if (corte.DiferenciaAceptable)
                    {
                        ws.Range(row, 1, row, 15).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                    }
                    else if (corte.TieneFaltante)
                    {
                        ws.Range(row, 1, row, 15).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                        ws.Cell(row, 9).Style.Font.FontColor = COLOR_ERROR;
                    }
                    else if (corte.TieneSobrante)
                    {
                        ws.Range(row, 1, row, 15).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                        ws.Cell(row, 9).Style.Font.FontColor = COLOR_AMARILLO;
                    }
                }

                ws.Cell(row, 9).Style.Font.Bold = true;

                // Alternar colores
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, 15).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 15);
            dataRange.SetAutoFilter();
            ws.SheetView.FreezeRows(3);
            ws.SheetView.FreezeColumns(2);
        }

        private void CrearHojaAnalisisUsuarios(IXLWorksheet ws, List<AnalisisUsuarioCorte> usuarios)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "👤 ANÁLISIS POR USUARIOS";
            ws.Range(row, 1, row, 9).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 9));
            row += 2;

            // Encabezados
            var encabezados = new[] {
                "👤 Usuario", "🎯 Cortes", "💰 Total Ventas", "📊 Promedio",
                "💎 Ganancia", "🏦 Comisiones", "⚠️ Diferencias", "📊 Exactitud %", "🏆 Clasificación"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos
            foreach (var (usuario, posicion) in usuarios.Select((u, i) => (u, i + 1)))
            {
                ws.Cell(row, 1).Value = usuario.NombreUsuario;
                ws.Cell(row, 2).Value = usuario.CantidadCortes;
                ws.Cell(row, 3).Value = usuario.TotalVentas;
                ws.Cell(row, 4).Value = usuario.PromedioVentas;
                ws.Cell(row, 5).Value = usuario.GananciaGenerada;
                ws.Cell(row, 6).Value = usuario.TotalComisiones;
                ws.Cell(row, 7).Value = usuario.CortesConDiferencias;
                ws.Cell(row, 8).Value = usuario.PorcentajeExactitud / 100;
                ws.Cell(row, 9).Value = usuario.ClasificacionUsuario;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "0.00%";

                // Colorear según clasificación
                if (usuario.EsUsuarioEficiente)
                {
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                    ws.Cell(row, 8).Style.Font.FontColor = COLOR_EXITO;
                    ws.Cell(row, 9).Style.Font.FontColor = COLOR_EXITO;
                }
                else if (usuario.PorcentajeExactitud < 70)
                {
                    ws.Cell(row, 8).Style.Font.FontColor = COLOR_ERROR;
                    ws.Cell(row, 9).Style.Font.FontColor = COLOR_ERROR;
                }

                ws.Cell(row, 8).Style.Font.Bold = true;
                ws.Cell(row, 9).Style.Font.Bold = true;

                // Destacar top 3
                if (posicion <= 3)
                {
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
            var dataRange = ws.Range(3, 1, row - 1, 9);
            dataRange.SetAutoFilter();
        }

        private void CrearHojaAnalisisTemporal(IXLWorksheet ws, ReporteCortesCaja reporte)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📅 ANÁLISIS TEMPORAL DE CORTES";
            ws.Range(row, 1, row, 5).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 5));
            row += 2;

            // Análisis por día de la semana
            ws.Cell(row, 1).Value = "📅 ANÁLISIS POR DÍA DE LA SEMANA";
            ws.Range(row, 1, row, 5).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 5));
            row++;

            var encabezadosDia = new[] { "📅 Día", "🎯 Cortes", "💰 Ventas Totales", "📊 Promedio", "🏦 Comisiones" };
            for (int i = 0; i < encabezadosDia.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezadosDia[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezadosDia.Length));
            row++;

            var analisisDias = reporte.ObtenerAnalisisPorDiaSemana();
            foreach (var dia in analisisDias)
            {
                ws.Cell(row, 1).Value = dia.NombreDia;
                ws.Cell(row, 2).Value = dia.CantidadCortes;
                ws.Cell(row, 3).Value = dia.VentasTotal;
                ws.Cell(row, 4).Value = dia.VentasPromedio;
                ws.Cell(row, 5).Value = dia.ComisionesTotal;

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                // Resaltar día con más ventas
                if (dia.VentasTotal == analisisDias.Max(d => d.VentasTotal))
                {
                    ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Range(row, 1, row, 5).Style.Font.Bold = true;
                }

                row++;
            }

            row += 2;

            // Mejor y peor día
            var mejorDia = reporte.ObtenerMejorDia();
            var peorDia = reporte.ObtenerPeorDia();

            if (mejorDia != null)
            {
                ws.Cell(row, 1).Value = "🏆 MEJOR DÍA";
                ws.Range(row, 1, row, 5).Merge();
                EstiloSubEncabezado(ws.Range(row, 1, row, 5));
                row++;

                ws.Cell(row, 1).Value = "📅 Fecha:";
                ws.Cell(row, 2).Value = mejorDia.FechaCorte.ToString("dd/MM/yyyy");
                row++;
                ws.Cell(row, 1).Value = "💰 Ventas:";
                ws.Cell(row, 2).Value = mejorDia.TotalVentasCalculado;
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 2).Style.Font.FontColor = COLOR_EXITO;
                ws.Cell(row, 2).Style.Font.Bold = true;
                row++;
                ws.Cell(row, 1).Value = "🎫 Tickets:";
                ws.Cell(row, 2).Value = mejorDia.CantidadTickets;
                row += 2;
            }

            if (peorDia != null)
            {
                ws.Cell(row, 1).Value = "📉 DÍA MÁS BAJO";
                ws.Range(row, 1, row, 5).Merge();
                EstiloSubEncabezado(ws.Range(row, 1, row, 5));
                row++;

                ws.Cell(row, 1).Value = "📅 Fecha:";
                ws.Cell(row, 2).Value = peorDia.FechaCorte.ToString("dd/MM/yyyy");
                row++;
                ws.Cell(row, 1).Value = "💰 Ventas:";
                ws.Cell(row, 2).Value = peorDia.TotalVentasCalculado;
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                row++;
                ws.Cell(row, 1).Value = "🎫 Tickets:";
                ws.Cell(row, 2).Value = peorDia.CantidadTickets;
            }

            ws.ColumnsUsed().AdjustToContents();
        }

        private void CrearHojaFormasPago(IXLWorksheet ws, ReporteCortesCaja reporte)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "💳 ANÁLISIS POR FORMAS DE PAGO";
            ws.Range(row, 1, row, 6).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 6));
            row += 2;

            var analisisFormas = reporte.ObtenerAnalisisFormasPago();

            // Encabezados
            var encabezados = new[] { "💳 Forma de Pago", "💰 Monto Total", "📊 % del Total", "📊 Promedio/Corte" };
            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            // Datos
            var formasPago = new[]
            {
                ("💵 Efectivo", analisisFormas.TotalEfectivo, analisisFormas.PorcentajeEfectivo, analisisFormas.PromedioEfectivoPorCorte),
                ("💳 Tarjeta", analisisFormas.TotalTarjeta, analisisFormas.PorcentajeTarjeta, analisisFormas.PromedioTarjetaPorCorte),
                ("📱 Transferencia", analisisFormas.TotalTransferencia, analisisFormas.PorcentajeTransferencia,
                    reporte.TotalCortes > 0 ? analisisFormas.TotalTransferencia / reporte.TotalCortes : 0)
            };

            foreach (var (forma, total, porcentaje, promedio) in formasPago)
            {
                ws.Cell(row, 1).Value = forma;
                ws.Cell(row, 2).Value = total;
                ws.Cell(row, 3).Value = porcentaje / 100;
                ws.Cell(row, 4).Value = promedio;

                // Formateo
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";

                // Resaltar forma preferida
                if (forma == $"💵 {analisisFormas.FormaPagoPreferida}" ||
                    forma == $"💳 {analisisFormas.FormaPagoPreferida}" ||
                    forma == $"📱 {analisisFormas.FormaPagoPreferida}")
                {
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Range(row, 1, row, 4).Style.Font.Bold = true;
                }

                row++;
            }

            row += 2;

            // Información adicional
            ws.Cell(row, 1).Value = "💳 Forma de Pago Preferida:";
            ws.Cell(row, 2).Value = analisisFormas.FormaPagoPreferida;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 2).Style.Font.FontColor = COLOR_EXITO;

            ws.ColumnsUsed().AdjustToContents();
        }

        private void CrearHojaComisiones(IXLWorksheet ws, ReporteCortesCaja reporte, List<CorteCaja> cortes)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "🏦 ANÁLISIS DE COMISIONES";
            ws.Range(row, 1, row, 6).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 6));
            row += 2;

            // Resumen de comisiones
            ws.Cell(row, 1).Value = "💳 RESUMEN DE COMISIONES";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var resumenComisiones = new[]
            {
                ("💳 Comisión Base Total", reporte.TotalComisionesBase),
                ("📊 IVA sobre Comisiones", reporte.TotalIVAComisiones),
                ("💰 Total Comisiones (Base + IVA)", reporte.TotalComisionesDelPeriodo),
                ("📈 Impacto en Rentabilidad", reporte.ImpactoComisiones)
            };

            foreach (var (concepto, valor) in resumenComisiones)
            {
                ws.Cell(row, 1).Value = concepto;
                ws.Cell(row, 2).Value = concepto.Contains("Impacto") ? valor / 100 : valor;

                ws.Cell(row, 1).Style.Font.Bold = true;

                if (concepto.Contains("Impacto"))
                {
                    ws.Cell(row, 2).Style.NumberFormat.Format = "0.00%";
                    ws.Cell(row, 2).Style.Font.FontColor = COLOR_ERROR;
                }
                else
                {
                    ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 2).Style.Font.FontColor = COLOR_ERROR;
                }

                ws.Cell(row, 2).Style.Font.Bold = true;
                row++;
            }

            row += 2;

            // Detalle por corte
            ws.Cell(row, 1).Value = "📊 DETALLE POR CORTE";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var encabezados = new[] { "📅 Fecha", "💳 Ventas Tarjeta", "💰 Comisión Base", "📊 IVA", "💎 Total Comisión", "📈 % Impacto" };
            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            var cortesConComision = cortes.Where(c => c.ComisionesTotalesCalculadas > 0)
                                          .OrderByDescending(c => c.ComisionesTotalesCalculadas)
                                          .ToList();

            foreach (var corte in cortesConComision)
            {
                var impacto = corte.TotalVentasCalculado > 0
                    ? (corte.ComisionesTotalesCalculadas / corte.TotalVentasCalculado)
                    : 0;

                ws.Cell(row, 1).Value = corte.FechaCorte.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = corte.TarjetaCalculado;
                ws.Cell(row, 3).Value = corte.ComisionesCalculadas;
                ws.Cell(row, 4).Value = corte.IVAComisionesCalculado;
                ws.Cell(row, 5).Value = corte.ComisionesTotalesCalculadas;
                ws.Cell(row, 6).Value = impacto;

                // Formateo
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.00%";

                ws.Cell(row, 5).Style.Font.Bold = true;
                ws.Cell(row, 5).Style.Font.FontColor = COLOR_ERROR;

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
        }

        private void CrearHojaDiferencias(IXLWorksheet ws, ReporteCortesCaja reporte, List<CorteCaja> cortes)
        {
            var row = 1;

            // Encabezado
            ws.Cell(row, 1).Value = "📊 ANÁLISIS DE DIFERENCIAS Y CONCILIACIÓN";
            ws.Range(row, 1, row, 8).Merge();
            EstiloEncabezadoPrincipal(ws.Range(row, 1, row, 8));
            row += 2;

            // Resumen global
            ws.Cell(row, 1).Value = "💰 RESUMEN GLOBAL";
            ws.Range(row, 1, row, 8).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 8));
            row++;

            var resumenGlobal = new[]
            {
                ("💵 Efectivo Total Esperado", reporte.TotalEfectivoEsperado, COLOR_AZUL),
                ("💰 Efectivo Total Contado", reporte.TotalEfectivoContado, COLOR_AZUL),
                ("📊 Diferencia Global", reporte.DiferenciaGlobalEfectivo,
                    reporte.DiferenciaGlobalEfectivo >= 0 ? COLOR_EXITO : COLOR_ERROR),
                ("📈 Total Sobrantes", reporte.TotalSobrantes, COLOR_AMARILLO),
                ("📉 Total Faltantes", reporte.TotalFaltantes, COLOR_ERROR)
            };

            foreach (var (concepto, valor, color) in resumenGlobal)
            {
                ws.Cell(row, 1).Value = concepto;
                ws.Cell(row, 2).Value = valor;

                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Font.FontColor = color;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;

                row++;
            }

            row += 2;

            // Detalle por corte
            ws.Cell(row, 1).Value = "📋 DETALLE POR CORTE";
            ws.Range(row, 1, row, 8).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 8));
            row++;

            var encabezados = new[] {
                "📅 Fecha", "👤 Usuario", "💵 Esperado", "💰 Contado",
                "📊 Diferencia", "📈 Estado", "📝 Tipo", "✅ Aceptable"
            };

            for (int i = 0; i < encabezados.Length; i++)
            {
                ws.Cell(row, i + 1).Value = encabezados[i];
            }
            EstiloEncabezadoTabla(ws.Range(row, 1, row, encabezados.Length));
            row++;

            var cortesCompletados = cortes.Where(c => c.Estado == "Completado")
                                          .OrderByDescending(c => Math.Abs(c.DiferenciaEfectivo))
                                          .ToList();

            foreach (var corte in cortesCompletados)
            {
                var tipoDiferencia = corte.TieneSobrante ? "📈 Sobrante" :
                                    corte.TieneFaltante ? "📉 Faltante" : "✅ Exacto";

                ws.Cell(row, 1).Value = corte.FechaCorte.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = corte.UsuarioCorte;
                ws.Cell(row, 3).Value = corte.EfectivoEsperado;
                ws.Cell(row, 4).Value = corte.EfectivoContado;
                ws.Cell(row, 5).Value = corte.DiferenciaEfectivo;
                ws.Cell(row, 6).Value = corte.Estado;
                ws.Cell(row, 7).Value = tipoDiferencia;
                ws.Cell(row, 8).Value = corte.DiferenciaAceptable ? "✅ Sí" : "❌ No";

                // Formateo
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                // Colorear según diferencia
                if (corte.TieneSobrante)
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    ws.Cell(row, 5).Style.Font.FontColor = COLOR_AMARILLO;
                }
                else if (corte.TieneFaltante)
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                    ws.Cell(row, 5).Style.Font.FontColor = COLOR_ERROR;
                }
                else
                {
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
                    ws.Cell(row, 5).Style.Font.FontColor = COLOR_EXITO;
                }

                ws.Cell(row, 5).Style.Font.Bold = true;

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
        }

        private void CrearHojaRentabilidad(IXLWorksheet ws, ReporteCortesCaja reporte)
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
                ("💰 Ingresos Totales", reporte.TotalVentasDelPeriodo, COLOR_EXITO),
                ("📉 Costos Totales", reporte.CostosTotales, COLOR_ERROR),
                ("📈 Ganancia Bruta", reporte.GananciaBrutaTotal, COLOR_EXITO),
                ("🏦 Comisiones Totales", reporte.TotalComisionesDelPeriodo, COLOR_ERROR),
                ("💎 Ganancia Neta", reporte.GananciaNetaTotal, COLOR_EXITO),
                ("📊 Margen Bruto %", reporte.MargenBrutoPromedio / 100, COLOR_AMARILLO),
                ("📈 Margen Neto %", reporte.MargenNetoPromedio / 100, COLOR_AMARILLO)
            };

            foreach (var (concepto, valor, color) in datosFinancieros)
            {
                ws.Cell(row, 1).Value = concepto;
                ws.Cell(row, 2).Value = valor;

                ws.Cell(row, 1).Style.Font.Bold = true;

                if (concepto.Contains("%"))
                {
                    ws.Cell(row, 2).Style.NumberFormat.Format = "0.00%";
                }
                else
                {
                    ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                }

                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Font.FontColor = color;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = COLOR_GRIS_CLARO;

                row++;
            }

            row += 2;

            // Métricas adicionales
            ws.Cell(row, 1).Value = "📊 MÉTRICAS ADICIONALES";
            ws.Range(row, 1, row, 6).Merge();
            EstiloSubEncabezado(ws.Range(row, 1, row, 6));
            row++;

            var metricas = new[]
            {
                ("📊 Promedio Ventas por Día", reporte.PromedioVentasPorDia),
                ("🎫 Promedio Tickets por Día", reporte.PromedioTicketsPorDia),
                ("💰 Promedio por Ticket", reporte.PromedioVentaPorTicket),
                ("👥 Usuarios Activos", (decimal)reporte.UsuariosUnicos)
            };

            foreach (var (concepto, valor) in metricas)
            {
                ws.Cell(row, 1).Value = concepto;
                ws.Cell(row, 2).Value = valor;

                ws.Cell(row, 1).Style.Font.Bold = true;

                if (concepto.Contains("Usuarios"))
                {
                    ws.Cell(row, 2).Style.NumberFormat.Format = "0";
                }
                else
                {
                    ws.Cell(row, 2).Style.NumberFormat.Format = concepto.Contains("Tickets") ? "0.00" : "$#,##0.00";
                }

                row++;
            }

            ws.ColumnsUsed().AdjustToContents();
        }

        #endregion

        #region Métodos de Utilidad y Estilo

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
            workbook.Properties.Title = $"Reporte de Cortes de Caja - {ObtenerNombreFormato(tipoFormato)}";
            workbook.Properties.Subject = $"Análisis de Cortes - {ObtenerNombrePeriodo(periodo)}";
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
                ws.PageSetup.FitToPages(1, 0);

                // Encabezado y pie de página
                ws.PageSetup.Header.Center.AddText($"Reporte de Cortes de Caja - {DateTime.Now:dd/MM/yyyy}");
                ws.PageSetup.Footer.Left.AddText("Sistema Costo-Beneficio");
                ws.PageSetup.Footer.Right.AddText("Página &P de &N");
            }
        }

        private bool ValidarParametrosEntrada(List<CorteCaja> cortes)
        {
            if (cortes == null || !cortes.Any())
            {
                MessageBox.Show("No hay cortes seleccionados para el reporte.",
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
                    Title = "Guardar Reporte de Cortes de Caja en Excel",
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    AddExtension = true,
                    FileName = $"ReporteCortesCaja_{ObtenerNombreFormato(tipoFormato)}_{ObtenerNombrePeriodo(periodo)}_{DateTime.Now:yyyy-MM-dd}",
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