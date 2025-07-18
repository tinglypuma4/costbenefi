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
    /// Servicio PDF profesional para reportes de ventas
    /// Compatible con sistema de análisis de ventas y rentabilidad
    /// AJUSTADO: Solo corrige layout sin cambiar lógica original
    /// </summary>
    public class VentasPDFService
    {
        #region Configuración y Estilos

        private readonly CultureInfo _cultura;
        private readonly AppDbContext _context;

        // Colores corporativos para ventas
        private static readonly Color COLOR_PRIMARIO = Color.FromHex("#059669");      // Verde ventas
        private static readonly Color COLOR_EXITO = Color.FromHex("#10B981");         // Verde claro
        private static readonly Color COLOR_ERROR = Color.FromHex("#EF4444");         // Rojo
        private static readonly Color COLOR_TEXTO = Color.FromHex("#1F2937");         // Texto principal
        private static readonly Color COLOR_GRIS_CLARO = Color.FromHex("#F3F4F6");    // Fondo sutil
        private static readonly Color COLOR_AMARILLO = Color.FromHex("#F59E0B");      // Para comisiones
        private static readonly Color COLOR_AZUL = Color.FromHex("#3B82F6");          // Para clientes

        public VentasPDFService(AppDbContext context = null)
        {
            _cultura = CultureInfo.GetCultureInfo("es-ES");
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #endregion

        #region Método Principal

        public async Task<string> GenerarReportePDFAsync(
            List<Venta> ventas,
            PeriodoReporte periodo,
            TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar,
            FiltrosAplicados filtrosAplicados = null)
        {
            try
            {
                Debug.WriteLine("📊 [PDF Ventas] Iniciando generación...");

                if (!ValidarParametrosEntrada(ventas))
                    return null;

                var rutaDestino = MostrarSaveFileDialog(periodo, tipoFormato);
                if (string.IsNullOrEmpty(rutaDestino))
                    return null;

                await Task.Run(async () => await GenerarPDFVentasAsync(ventas, periodo, tipoFormato, filtrosAplicados, rutaDestino));

                Debug.WriteLine("✅ [PDF Ventas] Completado exitosamente");
                return rutaDestino;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [PDF Ventas] Error: {ex}");
                MessageBox.Show($"Error al generar PDF de ventas:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region Generación PDF Profesional

        private async Task GenerarPDFVentasAsync(List<Venta> ventas, PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros, string rutaDestino)
        {
            // Crear reporte de análisis
            var reporteVentas = new ReporteVentas(ventas, periodo);

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);

                    // ✅ ENCABEZADO CORREGIDO - Estructura simplificada como StockPDF
                    page.Header().Height(50).Background(COLOR_PRIMARIO).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("REPORTE DE VENTAS")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.White);

                            col.Item().Text($"Período: {reporteVentas.ObtenerNombrePeriodo()} | Formato: {ObtenerNombreFormato(tipoFormato)}")
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
                        // Resumen ejecutivo de ventas
                        contenido.Item().Background(COLOR_GRIS_CLARO).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("💰 RESUMEN EJECUTIVO")
                                    .FontFamily("Arial")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(COLOR_PRIMARIO);

                                col.Item().Text($"🎯 Total de ventas: {reporteVentas.TotalVentas:N0}")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .FontColor(COLOR_TEXTO);

                                col.Item().Text($"💰 Ingresos totales: {reporteVentas.TotalIngresos:C2}")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .FontColor(COLOR_EXITO)
                                    .Bold();

                                col.Item().Text($"📈 Ganancia neta: {reporteVentas.GananciaNetaTotal:C2}")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .FontColor(COLOR_EXITO);
                            });

                            row.ConstantItem(150).Column(col =>
                            {
                                col.Item().Text($"📊 Margen promedio")
                                    .FontFamily("Arial")
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(COLOR_PRIMARIO);

                                col.Item().Text($"{reporteVentas.MargenPromedioNeto:F1}%")
                                    .FontFamily("Arial")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(reporteVentas.MargenPromedioNeto > 20 ? COLOR_EXITO : COLOR_ERROR);

                                col.Item().Text($"👥 {reporteVentas.ClientesUnicos} clientes")
                                    .FontFamily("Arial")
                                    .FontSize(9)
                                    .FontColor(COLOR_TEXTO);

                                col.Item().Text($"📦 {reporteVentas.ProductosDiferentesVendidos} productos")
                                    .FontFamily("Arial")
                                    .FontSize(9)
                                    .FontColor(COLOR_TEXTO);
                            });
                        });

                        // Filtros aplicados
                        if (filtros?.TieneFiltrosAplicados == true)
                        {
                            contenido.Item().PaddingVertical(8).Text("🔍 Filtros Aplicados")
                                .FontFamily("Arial")
                                .FontSize(11)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingLeft(10).Column(col =>
                            {
                                if (filtros.ClientesSeleccionados.Any())
                                    col.Item().Text($"👥 Clientes: {string.Join(", ", filtros.ClientesSeleccionados.Take(5))}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                if (filtros.UsuariosSeleccionados.Any())
                                    col.Item().Text($"👤 Usuarios: {string.Join(", ", filtros.UsuariosSeleccionados.Take(5))}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                if (filtros.ProductosSeleccionados.Any())
                                    col.Item().Text($"📦 Productos: {string.Join(", ", filtros.ProductosSeleccionados.Take(3))}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                if (filtros.MontoMinimo.HasValue)
                                    col.Item().Text($"💰 Monto mínimo: {filtros.MontoMinimo:C2}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                if (filtros.SoloVentasRentables)
                                    col.Item().Text("📈 Solo ventas rentables")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                            });
                        }

                        contenido.Item().PaddingVertical(8);

                        // Análisis por formas de pago
                        if (reporteVentas.AnalisisPorFormaPago.Any())
                        {
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
                                    columns.RelativeColumn(2);    // Forma de Pago
                                    columns.RelativeColumn(1);    // Transacciones
                                    columns.RelativeColumn(1.5f); // Monto Total
                                    columns.RelativeColumn(1.5f); // Promedio
                                    columns.RelativeColumn(1);    // Porcentaje
                                    columns.RelativeColumn(1.5f); // Comisiones
                                });

                                // Encabezado
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Forma de Pago")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Trans.")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Monto Total")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Promedio")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("%")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_PRIMARIO).Padding(5).Text("Comisiones")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                });

                                // Datos
                                var formasPago = reporteVentas.AnalisisPorFormaPago.ToList();
                                for (int i = 0; i < formasPago.Count; i++)
                                {
                                    var formaPago = formasPago[i];
                                    var colorFila = i % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;

                                    tabla.Cell().Background(colorFila).Padding(4).Text(formaPago.FormaPago)
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{formaPago.CantidadTransacciones:N0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{formaPago.MontoTotal:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{formaPago.PromedioTransaccion:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{formaPago.PorcentajeDelTotal:F1}%")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_AZUL);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{formaPago.ComisionTotal:C2}")
                                        .FontFamily("Arial").FontSize(8).FontColor(formaPago.ComisionTotal > 0 ? COLOR_ERROR : COLOR_TEXTO);
                                }
                            });

                            contenido.Item().PaddingVertical(8);
                        }

                        // TOP Productos más vendidos
                        if (reporteVentas.AnalisisPorProducto.Any())
                        {
                            contenido.Item().Text("🏆 TOP PRODUCTOS MÁS VENDIDOS")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            var topProductos = reporteVentas.AnalisisPorProducto.Take(10).ToList();

                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);    // Producto
                                    columns.RelativeColumn(1);    // Cantidad
                                    columns.RelativeColumn(1.5f); // Ingresos
                                    columns.RelativeColumn(1.5f); // Ganancia
                                    columns.RelativeColumn(1);    // Margen
                                });

                                // Encabezado
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(COLOR_EXITO).Padding(5).Text("Producto")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_EXITO).Padding(5).Text("Vendido")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_EXITO).Padding(5).Text("Ingresos")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_EXITO).Padding(5).Text("Ganancia")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_EXITO).Padding(5).Text("Margen")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                });

                                // Datos
                                for (int i = 0; i < topProductos.Count; i++)
                                {
                                    var producto = topProductos[i];
                                    var posicion = i + 1;
                                    var colorFila = posicion % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;
                                    var nombreCorto = TruncateString(producto.NombreProducto, 30);

                                    // Emoji de posición
                                    var emojiPosicion = posicion switch
                                    {
                                        1 => "🥇",
                                        2 => "🥈",
                                        3 => "🥉",
                                        _ => $"{posicion}."
                                    };

                                    tabla.Cell().Background(colorFila).Padding(4).Text($"{emojiPosicion} {nombreCorto}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{producto.CantidadVendida:F1}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{producto.IngresoTotal:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{producto.GananciaTotal:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{producto.MargenPromedio:F1}%")
                                        .FontFamily("Arial").FontSize(8).FontColor(producto.MargenPromedio > 20 ? COLOR_EXITO : COLOR_ERROR);
                                }
                            });

                            contenido.Item().PaddingVertical(8);
                        }

                        // TOP Clientes
                        if (reporteVentas.AnalisisPorCliente.Any())
                        {
                            contenido.Item().Text("👑 TOP CLIENTES")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            var topClientes = reporteVentas.AnalisisPorCliente.Take(8).ToList();

                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2.5f); // Cliente
                                    columns.RelativeColumn(1);    // Compras
                                    columns.RelativeColumn(1.5f); // Total Gastado
                                    columns.RelativeColumn(1.5f); // Promedio
                                    columns.RelativeColumn(1.5f); // Ganancia
                                });

                                // Encabezado
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(COLOR_AZUL).Padding(5).Text("Cliente")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AZUL).Padding(5).Text("Compras")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AZUL).Padding(5).Text("Total")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AZUL).Padding(5).Text("Promedio")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AZUL).Padding(5).Text("Ganancia")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                });

                                // Datos
                                for (int i = 0; i < topClientes.Count; i++)
                                {
                                    var cliente = topClientes[i];
                                    var posicion = i + 1;
                                    var colorFila = posicion % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;
                                    var nombreCorto = TruncateString(cliente.NombreCliente, 25);

                                    var emojiCliente = cliente.EsClienteValioso ? "💎" : cliente.EsClienteFrecuente ? "⭐" : "👤";

                                    tabla.Cell().Background(colorFila).Padding(4).Text($"{emojiCliente} {nombreCorto}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{cliente.CantidadCompras:N0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{cliente.TotalGastado:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{cliente.PromedioCompra:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{cliente.GananciaGenerada:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                }
                            });

                            contenido.Item().PaddingVertical(8);
                        }

                        // Análisis por usuarios vendedores
                        if (reporteVentas.AnalisisPorUsuario.Any())
                        {
                            contenido.Item().Text("🏅 RENDIMIENTO POR VENDEDOR")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);    // Usuario
                                    columns.RelativeColumn(1);    // Ventas
                                    columns.RelativeColumn(1.5f); // Total Vendido
                                    columns.RelativeColumn(1.5f); // Ganancia
                                    columns.RelativeColumn(1);    // Margen
                                });

                                // Encabezado
                                tabla.Header(header =>
                                {
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Vendedor")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Ventas")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Total")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Ganancia")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                    header.Cell().Background(COLOR_AMARILLO).Padding(5).Text("Margen")
                                        .FontFamily("Arial").FontSize(9).Bold().FontColor(Colors.White);
                                });

                                // Datos
                                var usuarios = reporteVentas.AnalisisPorUsuario.ToList();
                                for (int i = 0; i < usuarios.Count; i++)
                                {
                                    var usuario = usuarios[i];
                                    var posicion = i + 1;
                                    var colorFila = posicion % 2 == 0 ? Colors.White : COLOR_GRIS_CLARO;

                                    var emojiUsuario = usuario.EsVendedorEficiente ? "🏆" : posicion == 1 ? "👑" : "👤";

                                    tabla.Cell().Background(colorFila).Padding(4).Text($"{emojiUsuario} {usuario.NombreUsuario}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.VentasRealizadas:N0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.TotalVendido:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.GananciaGenerada:C0}")
                                        .FontFamily("Arial").FontSize(8).FontColor(COLOR_EXITO);
                                    tabla.Cell().Background(colorFila).Padding(4).AlignRight().Text($"{usuario.MargenPromedio:F1}%")
                                        .FontFamily("Arial").FontSize(8).FontColor(usuario.MargenPromedio > 20 ? COLOR_EXITO : COLOR_ERROR);
                                }
                            });
                        }

                        // Análisis de horarios
                        var ventasPorHora = reporteVentas.ObtenerVentasPorHora().Where(h => h.CantidadVentas > 0).ToList();
                        if (ventasPorHora.Any())
                        {
                            contenido.Item().PaddingVertical(8);

                            contenido.Item().Text("🕐 ANÁLISIS DE HORARIOS DE MAYOR ACTIVIDAD")
                                .FontFamily("Arial")
                                .FontSize(12)
                                .Bold()
                                .FontColor(COLOR_PRIMARIO);

                            contenido.Item().PaddingVertical(5);

                            var horariosTop = ventasPorHora.OrderByDescending(h => h.MontoVentas).Take(8).ToList();

                            contenido.Item().Row(row =>
                            {
                                for (int i = 0; i < horariosTop.Count && i < 4; i++)
                                {
                                    var horario = horariosTop[i];
                                    row.RelativeItem().Background(COLOR_GRIS_CLARO).Padding(8).Column(col =>
                                    {
                                        col.Item().Text($"🕐 {horario.Hora:D2}:00")
                                            .FontFamily("Arial").FontSize(10).Bold().FontColor(COLOR_PRIMARIO);

                                        col.Item().Text($"{horario.CantidadVentas} ventas")
                                            .FontFamily("Arial").FontSize(8).FontColor(COLOR_TEXTO);

                                        col.Item().Text($"{horario.MontoVentas:C0}")
                                            .FontFamily("Arial").FontSize(9).Bold().FontColor(COLOR_EXITO);
                                    });
                                }
                            });
                        }
                    });

                    // Pie de página corporativo
                    page.Footer().Height(20).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Sistema Costo-Beneficio - Módulo de Ventas")
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
                    Title = "Guardar Reporte de Ventas",
                    Filter = "Archivos PDF (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
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
                TipoFormatoReporte.PorProductos => "Por Productos",
                TipoFormatoReporte.PorClientes => "Por Clientes",
                TipoFormatoReporte.PorUsuarios => "Por Usuarios",
                TipoFormatoReporte.Financiero => "Financiero",
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