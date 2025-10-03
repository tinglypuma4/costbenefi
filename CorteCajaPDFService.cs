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
    /// Servicio PDF profesional para reportes de cortes de caja
    /// Compatible con sistema de análisis de cortes y conciliación
    /// </summary>
    public class CorteCajaPDFService
    {
        #region Configuración y Estilos

        private readonly CultureInfo _cultura;
        private readonly AppDbContext _context;

        // ✅ CORREGIDO: Colores para QuestPDF (no ClosedXML)
        private static readonly Color COLOR_PRIMARIO = Color.FromHex("#059669");      // Verde
        private static readonly Color COLOR_EXITO = Color.FromHex("#10B981");         // Verde claro
        private static readonly Color COLOR_ERROR = Color.FromHex("#EF4444");         // Rojo
        private static readonly Color COLOR_TEXTO = Color.FromHex("#1F2937");         // Texto principal
        private static readonly Color COLOR_GRIS_CLARO = Color.FromHex("#F3F4F6");    // Fondo sutil
        private static readonly Color COLOR_AMARILLO = Color.FromHex("#F59E0B");      // Advertencia
        private static readonly Color COLOR_AZUL = Color.FromHex("#3B82F6");          // Info

        public CorteCajaPDFService(AppDbContext context = null)
        {
            _cultura = CultureInfo.GetCultureInfo("es-ES");
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #endregion

        #region Método Principal

        public async Task<string> GenerarReportePDFAsync(
            List<CorteCaja> cortes,
            PeriodoReporte periodo,
            TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar,
            FiltrosAplicadosCorte filtrosAplicados = null)
        {
            try
            {
                Debug.WriteLine("📊 [PDF Cortes] Iniciando generación...");

                if (!ValidarParametrosEntrada(cortes))
                    return null;

                var rutaDestino = MostrarSaveFileDialog(periodo, tipoFormato);
                if (string.IsNullOrEmpty(rutaDestino))
                    return null;

                await Task.Run(async () => await GenerarPDFCortesAsync(cortes, periodo, tipoFormato, filtrosAplicados, rutaDestino));

                Debug.WriteLine("✅ [PDF Cortes] Completado exitosamente");
                return rutaDestino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [PDF Cortes] Error: {ex}");
                MessageBox.Show($"Error al generar PDF de cortes:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Generación PDF Profesional

        private async Task GenerarPDFCortesAsync(List<CorteCaja> cortes, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicadosCorte filtros, string rutaDestino)
        {
            // Crear reporte de análisis
            var reporteCortes = new ReporteCortesCaja(cortes, periodo);

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);

                    // Encabezado
                    page.Header().Height(50).Background(COLOR_PRIMARIO).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("REPORTE DE CORTES DE CAJA")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.White);

                            col.Item().Text($"Período: {reporteCortes.ObtenerNombrePeriodo()} | Formato: {ObtenerNombreFormato(tipoFormato)}")
                                .FontFamily("Arial")
                                .FontSize(10)
                                .FontColor(Colors.White);
                        });

                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                            .FontFamily("Arial")
                            .FontSize(10)
                            .FontColor(Colors.White);
                    });

                    // Contenido detallado
                    page.Content().Padding(10).Column(contenido =>
                    {
                        // Resumen ejecutivo
                        contenido.Item().Background(COLOR_GRIS_CLARO).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("💰 RESUMEN EJECUTIVO")
                                    .FontFamily("Arial")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(COLOR_PRIMARIO);

                                col.Item().Text($"🎯 Total de cortes: {reporteCortes.TotalCortes:N0}")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .FontColor(COLOR_TEXTO);

                                col.Item().Text($"💰 Ventas totales: {reporteCortes.TotalVentasDelPeriodo:C2}")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .FontColor(COLOR_EXITO)
                                    .Bold();

                                col.Item().Text($"📈 Ganancia neta: {reporteCortes.GananciaNetaTotal:C2}")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .FontColor(COLOR_EXITO);
                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                col.Item().Text($"📊 Estado de Cortes")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(COLOR_PRIMARIO);

                                col.Item().Text($"✅ {reporteCortes.CortesCompletados} Completados")
                                    .FontFamily("Arial")
                                    .FontSize(9)
                                    .FontColor(COLOR_EXITO);

                                col.Item().Text($"⏳ {reporteCortes.CortesPendientes} Pendientes")
                                    .FontFamily("Arial")
                                    .FontSize(9)
                                    .FontColor(COLOR_AMARILLO);

                                col.Item().Text($"❌ {reporteCortes.CortesCancelados} Cancelados")
                                    .FontFamily("Arial")
                                    .FontSize(9)
                                    .FontColor(COLOR_ERROR);
                            });
                        });

                        contenido.Item().PaddingVertical(8);

                        // Análisis de diferencias
                        contenido.Item().Text("📊 ANÁLISIS DE DIFERENCIAS")
                            .FontFamily("Arial")
                            .FontSize(12)
                            .Bold()
                            .FontColor(COLOR_PRIMARIO);

                        contenido.Item().PaddingVertical(5);

                        contenido.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                            });

                            // Encabezado
                            tabla.Header(header =>
                            {
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Concepto")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Cantidad")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Monto")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("%")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                            });

                            // Datos
                            var datosDiferencias = new[]
                            {
                                ("✅ Cortes Exactos", reporteCortes.CortesExactos, 0m, reporteCortes.PorcentajeCortesExactos, COLOR_EXITO),
                                ("📈 Cortes con Sobrante", reporteCortes.CortesConSobrante, reporteCortes.TotalSobrantes,
                                    reporteCortes.CortesCompletados > 0 ? (decimal)reporteCortes.CortesConSobrante / reporteCortes.CortesCompletados * 100 : 0,
                                    COLOR_AMARILLO),
                                ("📉 Cortes con Faltante", reporteCortes.CortesConFaltante, reporteCortes.TotalFaltantes,
                                    reporteCortes.CortesCompletados > 0 ? (decimal)reporteCortes.CortesConFaltante / reporteCortes.CortesCompletados * 100 : 0,
                                    COLOR_ERROR),
                                ("⚠️ Total con Diferencias", reporteCortes.CortesConDiferencias,
                                    reporteCortes.TotalSobrantes - reporteCortes.TotalFaltantes,
                                    reporteCortes.PorcentajeCortesConDiferencias, COLOR_AMARILLO)
                            };

                            for (int i = 0; i < datosDiferencias.Length; i++)
                            {
                                var (concepto, cantidad, monto, porcentaje, color) = datosDiferencias[i];
                                var colorFila = i % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;

                                tabla.Cell().Background(colorFila).Padding(4).Text(concepto)
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{cantidad:N0}")
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text(monto != 0 ? $"{monto:C2}" : "-")
                                    .FontFamily("Arial").FontSize(8).FontColor(color).Bold();
                                tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{porcentaje:F1}%")
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_AZUL);
                            }
                        });

                        contenido.Item().PaddingVertical(8);

                        // Análisis por formas de pago
                        var analisisFormasPago = reporteCortes.ObtenerAnalisisFormasPago();

                        contenido.Item().Text("💳 ANÁLISIS POR FORMAS DE PAGO")
                            .FontFamily("Arial")
                            .FontSize(12)
                            .Bold()
                            .FontColor(COLOR_PRIMARIO);

                        contenido.Item().PaddingVertical(5);

                        contenido.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                            });

                            // Encabezado
                            tabla.Header(header =>
                            {
                                header.Cell().Background(COLOR_AZUL).Padding(5).Text("Forma de Pago")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_AZUL).Padding(5).Text("Monto Total")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_AZUL).Padding(5).Text("% Total")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                header.Cell().Background(COLOR_AZUL).Padding(5).Text("Promedio/Corte")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                            });

                            // Datos
                            var formasPago = new[]
                            {
                                ("💵 Efectivo", analisisFormasPago.TotalEfectivo, analisisFormasPago.PorcentajeEfectivo,
                                    analisisFormasPago.PromedioEfectivoPorCorte),
                                ("💳 Tarjeta", analisisFormasPago.TotalTarjeta, analisisFormasPago.PorcentajeTarjeta,
                                    analisisFormasPago.PromedioTarjetaPorCorte),
                                ("📱 Transferencia", analisisFormasPago.TotalTransferencia, analisisFormasPago.PorcentajeTransferencia,
                                    reporteCortes.TotalCortes > 0 ? analisisFormasPago.TotalTransferencia / reporteCortes.TotalCortes : 0)
                            };

                            for (int i = 0; i < formasPago.Length; i++)
                            {
                                var (forma, total, porcentaje, promedio) = formasPago[i];
                                var colorFila = i % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;

                                tabla.Cell().Background(colorFila).Padding(4).Text(forma)
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{total:C0}")
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{porcentaje:F1}%")
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_AZUL);
                                tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{promedio:C0}")
                                    .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                            }
                        });

                        contenido.Item().PaddingVertical(8);

                        // Análisis de comisiones
                        if (reporteCortes.TotalComisionesDelPeriodo > 0)
                        {
                            contenido.Item().Text("🏦 ANÁLISIS DE COMISIONES")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            // ✅ CORREGIDO: Color.FromHex en lugar de XLColor
                            contenido.Item().Background(Color.FromHex("#FEF3C7")).Padding(10).Column(col =>
                            {
                                col.Item().Text($"💳 Total de Comisiones: {reporteCortes.TotalComisionesDelPeriodo:C2}")
                                    .FontFamily("Arial").FontSize(10).Bold().FontColor(COLOR_ERROR);

                                col.Item().Text($"   • Comisión Base: {reporteCortes.TotalComisionesBase:C2}")
                                    .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);

                                col.Item().Text($"   • IVA sobre Comisión: {reporteCortes.TotalIVAComisiones:C2}")
                                    .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);

                                col.Item().Text($"📊 Impacto en Rentabilidad: {reporteCortes.ImpactoComisiones:F2}%")
                                    .FontFamily("Arial").FontSize(9).Bold().FontColor(COLOR_AMARILLO);
                            });

                            contenido.Item().PaddingVertical(8);
                        }

                        // TOP Usuarios
                        var analisisUsuarios = reporteCortes.ObtenerAnalisisPorUsuario();
                        if (analisisUsuarios.Any())
                        {
                            contenido.Item().Text("👑 RENDIMIENTO POR USUARIO")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.2f);
                                });

                                // Encabezado
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Usuario")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Cortes")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Total Ventas")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Promedio")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Diferencias")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Exactitud")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                });

                                // Datos
                                for (int i = 0; i < analisisUsuarios.Count; i++)
                                {
                                    var usuario = analisisUsuarios[i];
                                    var colorFila = i % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;
                                    var nombreCorto = TruncateString(usuario.NombreUsuario, 30);

                                    var emojiUsuario = usuario.EsUsuarioEficiente ? "🏆" : i == 0 ? "👑" : "👤";

                                    tabla.Cell().Background(colorFila).Padding(4).Text($"{emojiUsuario} {nombreCorto}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.CantidadCortes:N0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.TotalVentas:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.PromedioVentas:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.CortesConDiferencias:N0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(usuario.CortesConDiferencias > 0 ? COLOR_ERROR : COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.PorcentajeExactitud:F0}%")
                                        .FontFamily("Arial").FontSize(8).FontColor(usuario.PorcentajeExactitud >= 80 ? COLOR_EXITO : COLOR_ERROR).Bold();
                                }
                            });

                            contenido.Item().PaddingVertical(8);
                        }

                        // Análisis por día de la semana
                        var analisisDias = reporteCortes.ObtenerAnalisisPorDiaSemana();
                        if (analisisDias.Any())
                        {
                            contenido.Item().Text("📅 ANÁLISIS POR DÍA DE LA SEMANA")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            contenido.Item().Row(row =>
                            {
                                foreach (var dia in analisisDias.Take(7))
                                {
                                    row.RelativeItem().Background(COLOR_GRIS_CLARO).Padding(6).Column(col =>
                                    {
                                        col.Item().Text($"📅 {dia.NombreDia}")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(COLOR_PRIMARIO);

                                        col.Item().Text($"{dia.CantidadCortes} cortes")
                                            .FontFamily("Arial").FontSize(7).FontColor(COLOR_TEXTO);

                                        col.Item().Text($"{dia.VentasPromedio:C0}")
                                            .FontFamily("Arial").FontSize(8).Bold().FontColor(COLOR_EXITO);
                                    });
                                }
                            });
                        }

                        // Mejor y peor día
                        var mejorDia = reporteCortes.ObtenerMejorDia();
                        var peorDia = reporteCortes.ObtenerPeorDia();

                        if (mejorDia != null && peorDia != null)
                        {
                            contenido.Item().PaddingVertical(8);

                            contenido.Item().Row(row =>
                            {
                                // ✅ CORREGIDO: Color.FromHex en lugar de XLColor
                                row.RelativeItem().Background(Color.FromHex("#ECFDF5")).Padding(10).Column(col =>
                                {
                                    col.Item().Text("🏆 MEJOR DÍA")
                                        .FontFamily("Arial").FontSize(10).Bold().FontColor(COLOR_EXITO);

                                    col.Item().Text($"📅 {mejorDia.FechaCorte:dd/MM/yyyy}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);

                                    col.Item().Text($"💰 {mejorDia.TotalVentasCalculado:C0}")
                                        .FontFamily("Arial").FontSize(11).Bold().FontColor(COLOR_EXITO);

                                    col.Item().Text($"🎫 {mejorDia.CantidadTickets} tickets")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                });

                                row.RelativeItem().Background(Color.FromHex("#FEF2F2")).Padding(10).Column(col =>
                                {
                                    col.Item().Text("📉 DÍA MÁS BAJO")
                                        .FontFamily("Arial").FontSize(10).Bold().FontColor(COLOR_ERROR);

                                    col.Item().Text($"📅 {peorDia.FechaCorte:dd/MM/yyyy}")
                                        .FontFamily("Arial").FontSize(9).FontColor(COLOR_TEXTO);

                                    col.Item().Text($"💰 {peorDia.TotalVentasCalculado:C0}")
                                        .FontFamily("Arial").FontSize(11).Bold().FontColor(COLOR_ERROR);

                                    col.Item().Text($"🎫 {peorDia.CantidadTickets} tickets")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                });
                            });
                        }
                    });

                    // Pie de página
                    page.Footer().Height(20).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Sistema Costo-Beneficio - Módulo de Cortes de Caja")
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

        #region Métodos de Utilidad

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
                    Title = "Guardar Reporte de Cortes de Caja",
                    Filter = "Archivos PDF (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
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
                TipoFormatoReporte.PorUsuarios => "Por_Usuarios",
                TipoFormatoReporte.Financiero => "Financiero",
                _ => "Estandar"
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

    #region Clase de Filtros

    /// <summary>
    /// Filtros aplicados para reportes de cortes
    /// </summary>
    public class FiltrosAplicadosCorte
    {
        public List<string> UsuariosSeleccionados { get; set; } = new List<string>();
        public List<string> EstadosSeleccionados { get; set; } = new List<string>();
        public bool SoloConDiferencias { get; set; }
        public bool SoloConComisiones { get; set; }
        public decimal? MontoMinimo { get; set; }
        public decimal? MontoMaximo { get; set; }

        public bool TieneFiltrosAplicados =>
            UsuariosSeleccionados.Any() ||
            EstadosSeleccionados.Any() ||
            SoloConDiferencias ||
            SoloConComisiones ||
            MontoMinimo.HasValue ||
            MontoMaximo.HasValue;
    }

    #endregion
}