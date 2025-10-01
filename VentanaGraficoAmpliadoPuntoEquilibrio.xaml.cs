using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using costbenefi.Models;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana especializada para vista ampliada de gráficos de Punto de Equilibrio
    /// </summary>
    public partial class VentanaGraficoAmpliadoPuntoEquilibrio : Window
    {
        #region Variables privadas
        private List<ItemPuntoEquilibrio> _datosAnalisis;
        private string _tipoAnalisis = "productos";
        private string _periodoAnalisis = "mensual";
        private string _tipoGraficoActual = "barras";
        private string _temaActual = "estandar";
        private Stopwatch _cronometroRender = new();
        private bool _esPantallaCompleta = false;
        private WindowState _estadoAnterior;
        private WindowStyle _estiloAnterior;
        #endregion

        #region Constructor y Factory
        public VentanaGraficoAmpliadoPuntoEquilibrio(List<ItemPuntoEquilibrio> datos, string tipoAnalisis, string periodoAnalisis)
        {
            InitializeComponent();

            _datosAnalisis = datos ?? new List<ItemPuntoEquilibrio>();
            _tipoAnalisis = tipoAnalisis;
            _periodoAnalisis = periodoAnalisis;

            InicializarVentana();
        }

        /// <summary>
        /// Método de factory para crear la ventana de forma segura
        /// </summary>
        public static VentanaGraficoAmpliadoPuntoEquilibrio CrearVentana(List<ItemPuntoEquilibrio> datos, string tipoAnalisis, string periodoAnalisis)
        {
            try
            {
                if (datos == null || !datos.Any())
                {
                    throw new ArgumentException("No hay datos de análisis de punto de equilibrio para mostrar", nameof(datos));
                }

                return new VentanaGraficoAmpliadoPuntoEquilibrio(datos, tipoAnalisis, periodoAnalisis);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creando VentanaGraficoAmpliadoPuntoEquilibrio: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Inicialización
        private void InicializarVentana()
        {
            try
            {
                // Validar elementos de UI
                ValidarElementosUI();

                // Configurar información del header
                ActualizarInformacionHeader();

                // Configurar estado inicial
                TxtStatusVentana.Text = "⚖️ Inicializando vista ampliada de punto de equilibrio...";
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

                // Dibujar gráfico inicial al cargar
                Loaded += (s, e) => DibujarGraficoAmpliado();

                System.Diagnostics.Debug.WriteLine($"✅ VentanaGraficoAmpliadoPuntoEquilibrio inicializada:");
                System.Diagnostics.Debug.WriteLine($"   📊 Datos: {_datosAnalisis.Count} items");
                System.Diagnostics.Debug.WriteLine($"   🎯 Tipo: {_tipoAnalisis}");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoAnalisis}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando VentanaGraficoAmpliadoPuntoEquilibrio: {ex.Message}");
                MessageBox.Show($"Error al inicializar vista ampliada:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidarElementosUI()
        {
            try
            {
                if (CanvasGraficoAmpliado == null)
                    throw new InvalidOperationException("CanvasGraficoAmpliado no está inicializado");

                if (TxtStatusVentana == null)
                    throw new InvalidOperationException("TxtStatusVentana no está inicializado");

                if (TxtUltimaActualizacion == null)
                    throw new InvalidOperationException("TxtUltimaActualizacion no está inicializado");

                System.Diagnostics.Debug.WriteLine($"✅ Elementos de UI validados correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error validando elementos UI: {ex.Message}");
                throw;
            }
        }

        private void ActualizarInformacionHeader()
        {
            try
            {
                var tipoTexto = _tipoAnalisis switch
                {
                    "productos" => "Productos",
                    "categorias" => "Categorías",
                    "proveedores" => "Proveedores",
                    "negocio_completo" => "Negocio Completo",
                    _ => "Items"
                };

                var periodoTexto = _periodoAnalisis switch
                {
                    "semanal" => "Semanal",
                    "mensual" => "Mensual",
                    "anual" => "Anual",
                    _ => "Mensual"
                };

                // Actualizar elementos del header principal
                TxtItemsInfo.Text = $"📊 Items analizados: {_datosAnalisis.Count}";
                TxtTipoAnalisis.Text = $"🎯 Análisis: {tipoTexto}";
                TxtPeriodoAnalisis.Text = $"📅 Período: {periodoTexto}";

                // Actualizar título del gráfico
                TxtTituloGrafico.Text = $"Punto de Equilibrio - {tipoTexto} ({periodoTexto})";
                TxtDescripcionGrafico.Text = $"Gráfico interactivo mostrando análisis detallado de punto de equilibrio, márgenes de contribución y unidades necesarias";

                // Métricas del footer
                TxtTotalItems.Text = $"Total: {_datosAnalisis.Count} items";

                if (_datosAnalisis.Any())
                {
                    var equilibrioPromedio = _datosAnalisis.Average(d => d.PuntoEquilibrioUnidades);
                    TxtEquilibrioPromedio.Text = equilibrioPromedio >= 1000 ?
                        $"Equilibrio Promedio: {equilibrioPromedio / 1000:F1}K unidades" :
                        $"Equilibrio Promedio: {equilibrioPromedio:F0} unidades";

                    // Calcular distribución por estado
                    var rentables = _datosAnalisis.Count(r => r.EstadoEquilibrio.Contains("✅"));
                    var cerca = _datosAnalisis.Count(r => r.EstadoEquilibrio.Contains("⚠️"));
                    var deficit = _datosAnalisis.Count(r => r.EstadoEquilibrio.Contains("❌"));
                    var margenPromedio = _datosAnalisis.Average(r => r.MargenContribucionPorcentaje);

                    TxtDistribucionInfo.Text = $"📊 Estado: {rentables}R/{cerca}C/{deficit}D";
                    TxtMargenPromedio.Text = $"📈 Margen Promedio: {margenPromedio:F1}%";
                }
                else
                {
                    TxtEquilibrioPromedio.Text = "Equilibrio Promedio: 0 unidades";
                    TxtDistribucionInfo.Text = "📊 Estado: 0R/0C/0D";
                    TxtMargenPromedio.Text = "📈 Margen Promedio: 0%";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Header actualizado: {_datosAnalisis.Count} items, {tipoTexto} {periodoTexto}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando header: {ex.Message}");
            }
        }
        #endregion

        #region Eventos de UI
        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CmbTipoGrafico_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbTipoGrafico.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    _tipoGraficoActual = item.Tag.ToString();
                    DibujarGraficoAmpliado();
                    System.Diagnostics.Debug.WriteLine($"✅ Tipo de gráfico cambiado a: {_tipoGraficoActual}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cambiando tipo de gráfico: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al cambiar tipo de gráfico";
            }
        }

        private void CmbTemaGrafico_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbTemaGrafico.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    _temaActual = item.Tag.ToString();
                    DibujarGraficoAmpliado();
                    System.Diagnostics.Debug.WriteLine($"✅ Tema cambiado a: {_temaActual}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cambiando tema: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al cambiar tema";
            }
        }

        private void CmbResolucion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbResolucion.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    ActualizarResolucionCanvas(item.Tag.ToString());
                    TxtResolucionActual.Text = $"Resolución: {item.Content}";
                    DibujarGraficoAmpliado();
                    System.Diagnostics.Debug.WriteLine($"✅ Resolución cambiada a: {item.Content}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cambiando resolución: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al cambiar resolución";
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusVentana.Text = "🔄 Actualizando gráfico de punto de equilibrio...";
                DibujarGraficoAmpliado();
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
                System.Diagnostics.Debug.WriteLine($"✅ Gráfico actualizado manualmente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando gráfico: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al actualizar gráfico";
                MessageBox.Show($"Error al actualizar gráfico:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPantallaCompleta_Click(object sender, RoutedEventArgs e)
        {
            AlternarPantallaCompleta();
        }

        private void BtnExportarGrafico_Click(object sender, RoutedEventArgs e)
        {
            ExportarGraficoAImagen();
        }

        private void BtnConfigGrafico_Click(object sender, RoutedEventArgs e)
        {
            MostrarConfiguracionAvanzada();
        }
        #endregion

        #region Dibujo de gráficos
        private void DibujarGraficoAmpliado()
        {
            try
            {
                _cronometroRender.Restart();
                TxtStatusVentana.Text = "🎨 Renderizando gráfico de punto de equilibrio de alta resolución...";

                switch (_tipoGraficoActual)
                {
                    case "barras":
                        DibujarGraficoBarrasEquilibrio();
                        break;
                    case "linea":
                        DibujarGraficoLineaEquilibrio();
                        break;
                    case "sensibilidad":
                        DibujarGraficoSensibilidad();
                        break;
                    case "comparativa":
                        DibujarGraficoComparativa();
                        break;
                    default:
                        DibujarGraficoBarrasEquilibrio();
                        break;
                }

                _cronometroRender.Stop();
                TxtTiempoRenderizado.Text = $"Renderizado: {_cronometroRender.ElapsedMilliseconds}ms";
                TxtStatusVentana.Text = "✅ Gráfico de punto de equilibrio de alta resolución renderizado";

                System.Diagnostics.Debug.WriteLine($"✅ Gráfico punto equilibrio ampliado renderizado: {_tipoGraficoActual} en {_cronometroRender.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico ampliado punto equilibrio: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al renderizar gráfico";
            }
        }

        private void DibujarGraficoBarrasEquilibrio()
        {
            try
            {
                CanvasGraficoAmpliado.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var canvas = CanvasGraficoAmpliado;
                var width = canvas.MinWidth;
                var height = canvas.MinHeight;

                var margen = 80;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                // Tomar hasta 25 items para vista ampliada
                var itemsGrafico = _datosAnalisis.Take(25).ToList();
                var maxEquilibrio = itemsGrafico.Max(i => i.PuntoEquilibrioUnidades);

                // Configurar colores según tema
                var colores = ObtenerColoresEquilibrio();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar barras con clasificación por estado
                var anchoBarraPorItem = areaGrafico.Width / itemsGrafico.Count;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var alturaRelativa = maxEquilibrio > 0 ? (double)(item.PuntoEquilibrioUnidades / maxEquilibrio) : 0;
                    var alturaBarra = alturaRelativa * areaGrafico.Height * 0.8;

                    // Barra principal con gradiente según estado
                    var barra = new Rectangle
                    {
                        Width = anchoBarraPorItem * 0.7,
                        Height = alturaBarra,
                        Fill = CrearGradienteEquilibrio(item.EstadoEquilibrio, colores),
                        Stroke = new SolidColorBrush(colores["Borde"]),
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(barra, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.15);
                    Canvas.SetTop(barra, areaGrafico.Bottom - alturaBarra);
                    canvas.Children.Add(barra);

                    // Etiqueta del item (mejorada para vista ampliada)
                    if (ChkMostrarEtiquetas?.IsChecked == true)
                    {
                        var etiqueta = new TextBlock
                        {
                            Text = item.Nombre.Length > 12 ? item.Nombre.Substring(0, 12) + "..." : item.Nombre,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(colores["Texto"]),
                            RenderTransform = new RotateTransform(-45)
                        };

                        Canvas.SetLeft(etiqueta, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.5);
                        Canvas.SetTop(etiqueta, areaGrafico.Bottom + 10);
                        canvas.Children.Add(etiqueta);

                        // Valor encima de la barra
                        var valorTexto = new TextBlock
                        {
                            Text = item.PuntoEquilibrioUnidades >= 1000 ?
                                $"{item.PuntoEquilibrioUnidades / 1000:F1}K" :
                                $"{item.PuntoEquilibrioUnidades:F0}",
                            FontSize = 9,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(colores["TextoValor"]),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        Canvas.SetLeft(valorTexto, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.2);
                        Canvas.SetTop(valorTexto, areaGrafico.Bottom - alturaBarra - 20);
                        canvas.Children.Add(valorTexto);
                    }
                }

                // Líneas de referencia de equilibrio
                if (ChkMostrarLineasReferencia?.IsChecked == true)
                {
                    DibujarLineasReferenciaEquilibrio(canvas, areaGrafico, maxEquilibrio, colores);
                }

                // Etiquetas de ejes mejoradas
                DibujarEtiquetasEjes(canvas, areaGrafico, maxEquilibrio, colores);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoBarrasEquilibrio: {ex.Message}");
            }
        }

        private void DibujarGraficoLineaEquilibrio()
        {
            try
            {
                CanvasGraficoAmpliado.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var canvas = CanvasGraficoAmpliado;
                var width = canvas.MinWidth;
                var height = canvas.MinHeight;

                var margen = 80;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _datosAnalisis.Take(25).ToList();
                var maxEquilibrio = itemsGrafico.Max(i => i.PuntoEquilibrioUnidades);
                var colores = ObtenerColoresEquilibrio();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar línea de equilibrio
                DibujarLineaEquilibrio(canvas, areaGrafico, itemsGrafico, maxEquilibrio, colores);

                // Líneas de referencia
                if (ChkMostrarLineasReferencia?.IsChecked == true)
                {
                    DibujarLineasReferenciaEquilibrio(canvas, areaGrafico, maxEquilibrio, colores);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoLineaEquilibrio: {ex.Message}");
            }
        }

        private void DibujarGraficoSensibilidad()
        {
            try
            {
                CanvasGraficoAmpliado.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var canvas = CanvasGraficoAmpliado;
                var width = canvas.MinWidth;
                var height = canvas.MinHeight;

                var margen = 80;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _datosAnalisis.Take(12).ToList();
                var colores = ObtenerColoresEquilibrio();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar análisis de sensibilidad
                DibujarBarrasSensibilidad(canvas, areaGrafico, itemsGrafico, colores);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoSensibilidad: {ex.Message}");
            }
        }

        private void DibujarGraficoComparativa()
        {
            try
            {
                CanvasGraficoAmpliado.Children.Clear();

                // Implementación futura del gráfico comparativo
                var mensaje = new TextBlock
                {
                    Text = "📊 Gráfico Comparativo de Equilibrio\n\nFuncionalidad en desarrollo\nPróximamente disponible",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(mensaje, CanvasGraficoAmpliado.MinWidth / 2 - 200);
                Canvas.SetTop(mensaje, CanvasGraficoAmpliado.MinHeight / 2 - 50);
                CanvasGraficoAmpliado.Children.Add(mensaje);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoComparativa: {ex.Message}");
            }
        }
        #endregion

        #region Métodos auxiliares de dibujo
        private Dictionary<string, Color> ObtenerColoresEquilibrio()
        {
            return _temaActual switch
            {
                "oscuro" => new Dictionary<string, Color>
                {
                    ["EstadoRentable"] = Color.FromRgb(34, 197, 94),
                    ["EstadoCerca"] = Color.FromRgb(251, 191, 36),
                    ["EstadoDeficit"] = Color.FromRgb(248, 113, 113),
                    ["Fondo"] = Color.FromRgb(31, 41, 55),
                    ["Texto"] = Color.FromRgb(243, 244, 246),
                    ["TextoValor"] = Color.FromRgb(156, 163, 175),
                    ["Borde"] = Color.FromRgb(75, 85, 99),
                    ["LineaPromedio"] = Color.FromRgb(59, 130, 246),
                    ["LineaObjetivo"] = Color.FromRgb(34, 197, 94)
                },
                "vibrante" => new Dictionary<string, Color>
                {
                    ["EstadoRentable"] = Color.FromRgb(236, 72, 153),
                    ["EstadoCerca"] = Color.FromRgb(168, 85, 247),
                    ["EstadoDeficit"] = Color.FromRgb(59, 130, 246),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(17, 24, 39),
                    ["TextoValor"] = Color.FromRgb(75, 85, 99),
                    ["Borde"] = Color.FromRgb(209, 213, 219),
                    ["LineaPromedio"] = Color.FromRgb(220, 38, 127),
                    ["LineaObjetivo"] = Color.FromRgb(236, 72, 153)
                },
                _ => new Dictionary<string, Color> // Estándar
                {
                    ["EstadoRentable"] = Color.FromRgb(16, 185, 129),
                    ["EstadoCerca"] = Color.FromRgb(245, 158, 11),
                    ["EstadoDeficit"] = Color.FromRgb(239, 68, 68),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(55, 65, 81),
                    ["TextoValor"] = Color.FromRgb(107, 114, 128),
                    ["Borde"] = Color.FromRgb(229, 231, 235),
                    ["LineaPromedio"] = Color.FromRgb(59, 130, 246),
                    ["LineaObjetivo"] = Color.FromRgb(16, 185, 129)
                }
            };
        }

        private LinearGradientBrush CrearGradienteEquilibrio(string estadoEquilibrio, Dictionary<string, Color> colores)
        {
            Color colorBase;
            if (estadoEquilibrio.Contains("✅"))
                colorBase = colores["EstadoRentable"];
            else if (estadoEquilibrio.Contains("⚠️"))
                colorBase = colores["EstadoCerca"];
            else
                colorBase = colores["EstadoDeficit"];

            var gradiente = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            gradiente.GradientStops.Add(new GradientStop(LightenColor(colorBase, 0.3), 0));
            gradiente.GradientStops.Add(new GradientStop(colorBase, 0.7));
            gradiente.GradientStops.Add(new GradientStop(DarkenColor(colorBase, 0.2), 1));

            return gradiente;
        }

        private Color LightenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)Math.Min(255, color.R + (255 - color.R) * factor),
                (byte)Math.Min(255, color.G + (255 - color.G) * factor),
                (byte)Math.Min(255, color.B + (255 - color.B) * factor)
            );
        }

        private Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * (1 - factor)),
                (byte)(color.G * (1 - factor)),
                (byte)(color.B * (1 - factor))
            );
        }

        private void DibujarEjes(Canvas canvas, Rect area, Dictionary<string, Color> colores)
        {
            // Eje X
            var ejeX = new Line
            {
                X1 = area.X,
                Y1 = area.Bottom,
                X2 = area.Right,
                Y2 = area.Bottom,
                Stroke = new SolidColorBrush(colores["Borde"]),
                StrokeThickness = 2
            };
            canvas.Children.Add(ejeX);

            // Eje Y
            var ejeY = new Line
            {
                X1 = area.X,
                Y1 = area.Y,
                X2 = area.X,
                Y2 = area.Bottom,
                Stroke = new SolidColorBrush(colores["Borde"]),
                StrokeThickness = 2
            };
            canvas.Children.Add(ejeY);
        }

        private void DibujarLineasReferenciaEquilibrio(Canvas canvas, Rect area, decimal maxEquilibrio, Dictionary<string, Color> colores)
        {
            // Línea promedio
            var equilibrioPromedio = _datosAnalisis.Average(d => d.PuntoEquilibrioUnidades);
            if (maxEquilibrio > equilibrioPromedio)
            {
                var yPromedio = area.Bottom - (area.Height * 0.8 * ((double)equilibrioPromedio / (double)maxEquilibrio));
                var lineaPromedio = new Line
                {
                    X1 = area.X,
                    Y1 = yPromedio,
                    X2 = area.Right,
                    Y2 = yPromedio,
                    Stroke = new SolidColorBrush(colores["LineaPromedio"]),
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection { 10, 5 }
                };
                canvas.Children.Add(lineaPromedio);

                // Etiqueta promedio
                var etiquetaPromedio = new TextBlock
                {
                    Text = equilibrioPromedio >= 1000 ? $"Promedio: {equilibrioPromedio / 1000:F1}K" : $"Promedio: {equilibrioPromedio:F0}",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(colores["LineaPromedio"])
                };
                Canvas.SetLeft(etiquetaPromedio, area.X - 80);
                Canvas.SetTop(etiquetaPromedio, yPromedio - 10);
                canvas.Children.Add(etiquetaPromedio);
            }
        }

        private void DibujarLineaEquilibrio(Canvas canvas, Rect area, List<ItemPuntoEquilibrio> items, decimal maxEquilibrio, Dictionary<string, Color> colores)
        {
            if (items.Count < 2) return;

            var path = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(colores["EstadoRentable"]),
                StrokeThickness = 4,
                Fill = null
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure();

            var anchoBarraPorItem = area.Width / items.Count;
            figure.StartPoint = new Point(area.X + anchoBarraPorItem * 0.5, area.Bottom);

            for (int i = 0; i < items.Count; i++)
            {
                var x = area.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.5;
                var y = area.Bottom - ((double)(items[i].PuntoEquilibrioUnidades / maxEquilibrio)) * area.Height * 0.8;

                figure.Segments.Add(new LineSegment(new Point(x, y), true));

                // Puntos en la línea
                var colorPunto = items[i].EstadoEquilibrio.Contains("✅") ? colores["EstadoRentable"] :
                                items[i].EstadoEquilibrio.Contains("⚠️") ? colores["EstadoCerca"] :
                                colores["EstadoDeficit"];

                var punto = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(colorPunto)
                };

                Canvas.SetLeft(punto, x - 4);
                Canvas.SetTop(punto, y - 4);
                canvas.Children.Add(punto);
            }

            geometry.Figures.Add(figure);
            path.Data = geometry;
            canvas.Children.Add(path);
        }

        private void DibujarBarrasSensibilidad(Canvas canvas, Rect area, List<ItemPuntoEquilibrio> items, Dictionary<string, Color> colores)
        {
            var variaciones = new[] { -0.2m, -0.1m, 0m, 0.1m, 0.2m };
            var coloresVariacion = new[] {
                colores["EstadoDeficit"],
                Color.FromRgb(245, 158, 11),
                colores["EstadoRentable"],
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(139, 92, 246)
            };

            var anchoGrupoPorItem = area.Width / items.Count;
            var anchoBarraPorVariacion = anchoGrupoPorItem * 0.15;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var maxEquilibrioItem = 0m;

                // Calcular punto de equilibrio para cada variación de precio
                var equilibrios = new decimal[5];
                for (int v = 0; v < variaciones.Length; v++)
                {
                    equilibrios[v] = item.CalcularEquilibrioConPrecioAjustado(variaciones[v]);
                    maxEquilibrioItem = Math.Max(maxEquilibrioItem, equilibrios[v]);
                }

                // Dibujar barras de sensibilidad
                for (int v = 0; v < variaciones.Length; v++)
                {
                    if (equilibrios[v] > 0)
                    {
                        var alturaRelativa = maxEquilibrioItem > 0 ? (double)(equilibrios[v] / maxEquilibrioItem) : 0;
                        var alturaBarra = alturaRelativa * area.Height * 0.15;

                        var barra = new Rectangle
                        {
                            Width = anchoBarraPorVariacion,
                            Height = alturaBarra,
                            Fill = new SolidColorBrush(coloresVariacion[v])
                        };

                        var xPos = area.X + i * anchoGrupoPorItem + v * anchoBarraPorVariacion + anchoGrupoPorItem * 0.1;
                        Canvas.SetLeft(barra, xPos);
                        Canvas.SetTop(barra, area.Bottom - alturaBarra);
                        canvas.Children.Add(barra);
                    }
                }

                // Etiqueta del producto
                var etiquetaProducto = new TextBlock
                {
                    Text = item.Nombre.Length > 8 ? item.Nombre.Substring(0, 8) + "..." : item.Nombre,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(colores["Texto"]),
                    RenderTransform = new RotateTransform(-45)
                };

                Canvas.SetLeft(etiquetaProducto, area.X + i * anchoGrupoPorItem + anchoGrupoPorItem * 0.4);
                Canvas.SetTop(etiquetaProducto, area.Bottom + 5);
                canvas.Children.Add(etiquetaProducto);
            }

            // Leyenda de variaciones
            var leyendaTextos = new[] { "-20%", "-10%", "Actual", "+10%", "+20%" };
            for (int v = 0; v < coloresVariacion.Length; v++)
            {
                var rectanguloLeyenda = new Rectangle
                {
                    Width = 15,
                    Height = 10,
                    Fill = new SolidColorBrush(coloresVariacion[v])
                };
                Canvas.SetLeft(rectanguloLeyenda, area.X + v * 60);
                Canvas.SetTop(rectanguloLeyenda, area.Y - 25);
                canvas.Children.Add(rectanguloLeyenda);

                var textoLeyenda = new TextBlock
                {
                    Text = leyendaTextos[v],
                    FontSize = 9,
                    Foreground = new SolidColorBrush(colores["Texto"])
                };
                Canvas.SetLeft(textoLeyenda, area.X + v * 60 + 18);
                Canvas.SetTop(textoLeyenda, area.Y - 27);
                canvas.Children.Add(textoLeyenda);
            }
        }

        private void DibujarEtiquetasEjes(Canvas canvas, Rect area, decimal maxEquilibrio, Dictionary<string, Color> colores)
        {
            // Etiquetas del eje Y (unidades de equilibrio)
            for (int i = 0; i <= 5; i++)
            {
                var valor = maxEquilibrio * i / 5;
                var y = area.Bottom - (area.Height * i / 5);

                var textoValor = valor >= 1000 ? $"{valor / 1000:F1}K" : $"{valor:F0}";
                var etiqueta = new TextBlock
                {
                    Text = textoValor,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(colores["Texto"]),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Canvas.SetLeft(etiqueta, area.X - 60);
                Canvas.SetTop(etiqueta, y - 8);
                canvas.Children.Add(etiqueta);

                // Línea guía horizontal
                if (i > 0)
                {
                    var lineaGuia = new Line
                    {
                        X1 = area.X,
                        Y1 = y,
                        X2 = area.Right,
                        Y2 = y,
                        Stroke = new SolidColorBrush(colores["Borde"]),
                        StrokeThickness = 0.5,
                        Opacity = 0.3
                    };
                    canvas.Children.Add(lineaGuia);
                }
            }
        }
        #endregion

        #region Funcionalidades adicionales
        private void ActualizarResolucionCanvas(string resolucion)
        {
            var (width, height) = resolucion switch
            {
                "hd" => (1280, 720),
                "fhd" => (1920, 1080),
                "4k" => (3840, 2160),
                _ => (1920, 1080)
            };

            // Escalar para que quepa en la ventana manteniendo proporciones
            var escala = Math.Min(1000.0 / width, 500.0 / height);
            CanvasGraficoAmpliado.MinWidth = width * escala;
            CanvasGraficoAmpliado.MinHeight = height * escala;
        }

        private void AlternarPantallaCompleta()
        {
            if (!_esPantallaCompleta)
            {
                // Guardar estado actual
                _estadoAnterior = this.WindowState;
                _estiloAnterior = this.WindowStyle;

                // Activar pantalla completa
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                BtnPantallaCompleta.Content = "↙️ Ventana";
                _esPantallaCompleta = true;
            }
            else
            {
                // Restaurar estado anterior
                this.WindowStyle = _estiloAnterior;
                this.WindowState = _estadoAnterior;
                BtnPantallaCompleta.Content = "🖥️ Pantalla Completa";
                _esPantallaCompleta = false;
            }
        }

        private void ExportarGraficoAImagen()
        {
            try
            {
                MessageBox.Show($"📸 Exportar Gráfico de Punto de Equilibrio\n\nFuncionalidad disponible próximamente.\n\nIncluirá:\n• Exportación a PNG, JPG, SVG\n• Resolución personalizable\n• Análisis de equilibrio incluido\n• Metadatos financieros", "Exportar Imagen", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusVentana.Text = "📸 Preparando exportación de gráfico de punto de equilibrio...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exportando imagen: {ex.Message}");
            }
        }

        private void MostrarConfiguracionAvanzada()
        {
            try
            {
                MessageBox.Show($"⚙️ Configuración Avanzada de Gráficos de Punto de Equilibrio\n\nPróximamente disponible:\n• Personalización de umbrales de equilibrio\n• Configuración de colores por estado\n• Opciones de renderizado avanzado\n• Análisis de sensibilidad personalizado\n• Alertas de equilibrio crítico", "Configuración Avanzada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo configuración: {ex.Message}");
            }
        }
        #endregion
    }
}