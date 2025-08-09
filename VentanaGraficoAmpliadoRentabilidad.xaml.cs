using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana especializada para vista ampliada de gráficos de Rentabilidad
    /// </summary>
    public partial class VentanaGraficoAmpliadoRentabilidad : Window
    {
        #region Variables privadas
        private List<ItemAnalisisRentabilidad> _datosAnalisis;
        private string _tipoAnalisis = "productos";
        private string _metricaAnalisis = "margen_bruto";
        private string _tipoGraficoActual = "barras";
        private string _temaActual = "estandar";
        private Stopwatch _cronometroRender = new();
        private bool _esPantallaCompleta = false;
        private WindowState _estadoAnterior;
        private WindowStyle _estiloAnterior;
        #endregion

        #region Constructor y Factory
        public VentanaGraficoAmpliadoRentabilidad(List<ItemAnalisisRentabilidad> datos, string tipoAnalisis, string metricaAnalisis)
        {
            InitializeComponent();

            _datosAnalisis = datos ?? new List<ItemAnalisisRentabilidad>();
            _tipoAnalisis = tipoAnalisis;
            _metricaAnalisis = metricaAnalisis;

            InicializarVentana();
        }

        /// <summary>
        /// Método de factory para crear la ventana de forma segura
        /// </summary>
        public static VentanaGraficoAmpliadoRentabilidad CrearVentana(List<ItemAnalisisRentabilidad> datos, string tipoAnalisis, string metricaAnalisis)
        {
            try
            {
                if (datos == null || !datos.Any())
                {
                    throw new ArgumentException("No hay datos de análisis de rentabilidad para mostrar", nameof(datos));
                }

                return new VentanaGraficoAmpliadoRentabilidad(datos, tipoAnalisis, metricaAnalisis);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error creando VentanaGraficoAmpliadoRentabilidad: {ex.Message}");
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
                TxtStatusVentana.Text = "💰 Inicializando vista ampliada de rentabilidad...";
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

                // Dibujar gráfico inicial al cargar
                Loaded += (s, e) => DibujarGraficoAmpliado();

                System.Diagnostics.Debug.WriteLine($"✅ VentanaGraficoAmpliadoRentabilidad inicializada:");
                System.Diagnostics.Debug.WriteLine($"   📊 Datos: {_datosAnalisis.Count} items");
                System.Diagnostics.Debug.WriteLine($"   🎯 Tipo: {_tipoAnalisis}");
                System.Diagnostics.Debug.WriteLine($"   💰 Métrica: {_metricaAnalisis}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando VentanaGraficoAmpliadoRentabilidad: {ex.Message}");
                MessageBox.Show($"Error al inicializar vista ampliada:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidarElementosUI()
        {
            try
            {
                // Verificar elementos críticos del XAML
                if (CanvasGraficoAmpliado == null)
                    throw new InvalidOperationException("CanvasGraficoAmpliado no está inicializado");

                if (TxtStatusVentana == null)
                    throw new InvalidOperationException("TxtStatusVentana no está inicializado");

                if (TxtUltimaActualizacion == null)
                    throw new InvalidOperationException("TxtUltimaActualizacion no está inicializado");

                // Los CheckBox pueden ser null, se manejan con operador null-conditional
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
                    "clientes" => "Clientes",
                    _ => "Items"
                };

                var metricaTexto = _metricaAnalisis switch
                {
                    "margen_bruto" => "Margen Bruto",
                    "margen_neto" => "Margen Neto",
                    "roi" => "ROI (Retorno)",
                    "ganancia_total" => "Ganancia Total",
                    "costo_beneficio" => "Costo-Beneficio",
                    _ => "Rentabilidad"
                };

                // Actualizar elementos del header principal
                TxtItemsInfo.Text = $"📊 Items analizados: {_datosAnalisis.Count}";
                TxtTipoAnalisis.Text = $"🎯 Análisis: {tipoTexto}";
                TxtMetricaAnalisis.Text = $"💰 Métrica: {metricaTexto}";

                // Actualizar título del gráfico
                TxtTituloGrafico.Text = $"Análisis de Rentabilidad - {tipoTexto} por {metricaTexto}";
                TxtDescripcionGrafico.Text = $"Gráfico interactivo mostrando análisis detallado de rentabilidad, márgenes y ROI";

                // Métricas del footer
                TxtTotalItems.Text = $"Total: {_datosAnalisis.Count} items";
                TxtValorTotal.Text = $"Ganancia Total: ${_datosAnalisis.Sum(d => d.GananciaBruta):N0}";

                // Calcular y mostrar información de rentabilidad en el header del gráfico
                if (_datosAnalisis.Any())
                {
                    var altaRentabilidad = _datosAnalisis.Count(r => r.ValorMetrica >= 25);
                    var mediaRentabilidad = _datosAnalisis.Count(r => r.ValorMetrica >= 15 && r.ValorMetrica < 25);
                    var bajaRentabilidad = _datosAnalisis.Count(r => r.ValorMetrica < 15);
                    var margenPromedio = _datosAnalisis.Average(r => r.MargenBruto);

                    TxtRentabilidadInfo.Text = $"📊 Distribución: {altaRentabilidad}A/{mediaRentabilidad}M/{bajaRentabilidad}B";
                    TxtMargenPromedio.Text = $"📈 Margen Promedio: {margenPromedio:F1}%";
                }
                else
                {
                    TxtRentabilidadInfo.Text = "📊 Distribución: 0A/0M/0B";
                    TxtMargenPromedio.Text = "📈 Margen Promedio: 0%";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Header actualizado: {_datosAnalisis.Count} items, {tipoTexto} por {metricaTexto}");
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
                TxtStatusVentana.Text = "🔄 Actualizando gráfico de rentabilidad...";
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
                TxtStatusVentana.Text = "🎨 Renderizando gráfico de rentabilidad de alta resolución...";

                switch (_tipoGraficoActual)
                {
                    case "barras":
                        DibujarGraficoBarrasRentabilidad();
                        break;
                    case "linea":
                        DibujarGraficoLineaRentabilidad();
                        break;
                    case "costo_beneficio":
                        DibujarGraficoCostoBeneficio();
                        break;
                    case "circular":
                        DibujarGraficoCircular();
                        break;
                    default:
                        DibujarGraficoBarrasRentabilidad();
                        break;
                }

                _cronometroRender.Stop();
                TxtTiempoRenderizado.Text = $"Renderizado: {_cronometroRender.ElapsedMilliseconds}ms";
                TxtStatusVentana.Text = "✅ Gráfico de rentabilidad de alta resolución renderizado";

                System.Diagnostics.Debug.WriteLine($"✅ Gráfico rentabilidad ampliado renderizado: {_tipoGraficoActual} en {_cronometroRender.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico ampliado rentabilidad: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al renderizar gráfico";
            }
        }

        private void DibujarGraficoBarrasRentabilidad()
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

                // Tomar hasta 30 items para vista ampliada
                var itemsGrafico = _datosAnalisis.Take(30).ToList();
                var maxValor = itemsGrafico.Max(i => i.ValorMetrica);

                // Configurar colores según tema
                var colores = ObtenerColoresRentabilidad();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar barras con clasificación por rentabilidad
                var anchoBarraPorItem = areaGrafico.Width / itemsGrafico.Count;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var alturaRelativa = maxValor > 0 ? (double)(item.ValorMetrica / maxValor) : 0;
                    var alturaBarra = alturaRelativa * areaGrafico.Height * 0.8;

                    // Barra principal con gradiente según rentabilidad
                    var barra = new Rectangle
                    {
                        Width = anchoBarraPorItem * 0.7,
                        Height = alturaBarra,
                        Fill = CrearGradienteRentabilidad(item.ValorMetrica, colores),
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
                            Text = $"{item.ValorMetrica:F1}%",
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

                // Líneas de referencia de rentabilidad
                if (ChkMostrarLineasReferencia?.IsChecked == true)
                {
                    DibujarLineasReferencia(canvas, areaGrafico, maxValor, colores);
                }

                // Etiquetas de ejes mejoradas
                DibujarEtiquetasEjes(canvas, areaGrafico, maxValor, colores);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoBarrasRentabilidad: {ex.Message}");
            }
        }

        private void DibujarGraficoLineaRentabilidad()
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

                var itemsGrafico = _datosAnalisis.Take(30).ToList();
                var maxValor = itemsGrafico.Max(i => i.ValorMetrica);
                var colores = ObtenerColoresRentabilidad();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar línea de rentabilidad
                DibujarLineaRentabilidad(canvas, areaGrafico, itemsGrafico, maxValor, colores);

                // Líneas de referencia
                if (ChkMostrarLineasReferencia?.IsChecked == true)
                {
                    DibujarLineasReferencia(canvas, areaGrafico, maxValor, colores);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoLineaRentabilidad: {ex.Message}");
            }
        }

        private void DibujarGraficoCostoBeneficio()
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

                var itemsGrafico = _datosAnalisis.Take(20).ToList();
                var colores = ObtenerColoresRentabilidad();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar análisis costo vs beneficio
                DibujarBarrasCostoBeneficio(canvas, areaGrafico, itemsGrafico, colores);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoCostoBeneficio: {ex.Message}");
            }
        }

        private void DibujarGraficoCircular()
        {
            try
            {
                CanvasGraficoAmpliado.Children.Clear();

                // Implementación futura del gráfico circular de rentabilidad
                var mensaje = new TextBlock
                {
                    Text = "🍰 Gráfico Circular de Rentabilidad\n\nFuncionalidad en desarrollo\nPróximamente disponible",
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
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoCircular: {ex.Message}");
            }
        }
        #endregion

        #region Métodos auxiliares de dibujo
        private Dictionary<string, Color> ObtenerColoresRentabilidad()
        {
            return _temaActual switch
            {
                "oscuro" => new Dictionary<string, Color>
                {
                    ["RentabilidadAlta"] = Color.FromRgb(34, 197, 94),
                    ["RentabilidadMedia"] = Color.FromRgb(251, 191, 36),
                    ["RentabilidadBaja"] = Color.FromRgb(248, 113, 113),
                    ["Fondo"] = Color.FromRgb(31, 41, 55),
                    ["Texto"] = Color.FromRgb(243, 244, 246),
                    ["TextoValor"] = Color.FromRgb(156, 163, 175),
                    ["Borde"] = Color.FromRgb(75, 85, 99),
                    ["LineaObjetivo"] = Color.FromRgb(34, 197, 94),
                    ["LineaCosto"] = Color.FromRgb(248, 113, 113),
                    ["LineaBeneficio"] = Color.FromRgb(34, 197, 94)
                },
                "vibrante" => new Dictionary<string, Color>
                {
                    ["RentabilidadAlta"] = Color.FromRgb(236, 72, 153),
                    ["RentabilidadMedia"] = Color.FromRgb(168, 85, 247),
                    ["RentabilidadBaja"] = Color.FromRgb(59, 130, 246),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(17, 24, 39),
                    ["TextoValor"] = Color.FromRgb(75, 85, 99),
                    ["Borde"] = Color.FromRgb(209, 213, 219),
                    ["LineaObjetivo"] = Color.FromRgb(220, 38, 127),
                    ["LineaCosto"] = Color.FromRgb(59, 130, 246),
                    ["LineaBeneficio"] = Color.FromRgb(236, 72, 153)
                },
                _ => new Dictionary<string, Color> // Estándar
                {
                    ["RentabilidadAlta"] = Color.FromRgb(16, 185, 129),
                    ["RentabilidadMedia"] = Color.FromRgb(245, 158, 11),
                    ["RentabilidadBaja"] = Color.FromRgb(239, 68, 68),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(55, 65, 81),
                    ["TextoValor"] = Color.FromRgb(107, 114, 128),
                    ["Borde"] = Color.FromRgb(229, 231, 235),
                    ["LineaObjetivo"] = Color.FromRgb(16, 185, 129),
                    ["LineaCosto"] = Color.FromRgb(239, 68, 68),
                    ["LineaBeneficio"] = Color.FromRgb(16, 185, 129)
                }
            };
        }

        private LinearGradientBrush CrearGradienteRentabilidad(decimal valorMetrica, Dictionary<string, Color> colores)
        {
            Color colorBase;
            if (valorMetrica >= 25)
                colorBase = colores["RentabilidadAlta"];
            else if (valorMetrica >= 15)
                colorBase = colores["RentabilidadMedia"];
            else
                colorBase = colores["RentabilidadBaja"];

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

        private void DibujarLineasReferencia(Canvas canvas, Rect area, decimal maxValor, Dictionary<string, Color> colores)
        {
            // Línea objetivo 25% (alta rentabilidad)
            if (maxValor > 25)
            {
                var yObjetivo = area.Bottom - (area.Height * 0.8 * (25 / (double)maxValor));
                var lineaObjetivo = new Line
                {
                    X1 = area.X,
                    Y1 = yObjetivo,
                    X2 = area.Right,
                    Y2 = yObjetivo,
                    Stroke = new SolidColorBrush(colores["LineaObjetivo"]),
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection { 10, 5 }
                };
                canvas.Children.Add(lineaObjetivo);

                // Etiqueta objetivo
                var etiquetaObjetivo = new TextBlock
                {
                    Text = "25% ✓",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(colores["LineaObjetivo"])
                };
                Canvas.SetLeft(etiquetaObjetivo, area.X - 50);
                Canvas.SetTop(etiquetaObjetivo, yObjetivo - 10);
                canvas.Children.Add(etiquetaObjetivo);
            }

            // Línea mínima 15% (rentabilidad media)
            if (maxValor > 15)
            {
                var yMinima = area.Bottom - (area.Height * 0.8 * (15 / (double)maxValor));
                var lineaMinima = new Line
                {
                    X1 = area.X,
                    Y1 = yMinima,
                    X2 = area.Right,
                    Y2 = yMinima,
                    Stroke = new SolidColorBrush(colores["RentabilidadMedia"]),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };
                canvas.Children.Add(lineaMinima);

                // Etiqueta mínima
                var etiquetaMinima = new TextBlock
                {
                    Text = "15%",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(colores["RentabilidadMedia"])
                };
                Canvas.SetLeft(etiquetaMinima, area.X - 35);
                Canvas.SetTop(etiquetaMinima, yMinima - 8);
                canvas.Children.Add(etiquetaMinima);
            }
        }

        private void DibujarLineaRentabilidad(Canvas canvas, Rect area, List<ItemAnalisisRentabilidad> items, decimal maxValor, Dictionary<string, Color> colores)
        {
            if (items.Count < 2) return;

            var path = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(colores["RentabilidadAlta"]),
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
                var y = area.Bottom - ((double)(items[i].ValorMetrica / maxValor)) * area.Height * 0.8;

                figure.Segments.Add(new LineSegment(new Point(x, y), true));

                // Puntos en la línea
                var punto = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(items[i].ValorMetrica >= 25 ? colores["RentabilidadAlta"] :
                                               items[i].ValorMetrica >= 15 ? colores["RentabilidadMedia"] :
                                               colores["RentabilidadBaja"])
                };

                Canvas.SetLeft(punto, x - 4);
                Canvas.SetTop(punto, y - 4);
                canvas.Children.Add(punto);
            }

            geometry.Figures.Add(figure);
            path.Data = geometry;
            canvas.Children.Add(path);
        }

        private void DibujarBarrasCostoBeneficio(Canvas canvas, Rect area, List<ItemAnalisisRentabilidad> items, Dictionary<string, Color> colores)
        {
            var alturaBarraPorItem = area.Height / items.Count;
            var maxCosto = items.Max(i => i.TotalCostos);
            var maxBeneficio = items.Max(i => i.GananciaBruta);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                // Barra de costo (izquierda)
                var costoRelativo = maxCosto > 0 ? (double)(item.TotalCostos / maxCosto) : 0;
                var anchoCosto = costoRelativo * area.Width * 0.4;

                var barraCosto = new Rectangle
                {
                    Width = anchoCosto,
                    Height = alturaBarraPorItem * 0.3,
                    Fill = new SolidColorBrush(colores["LineaCosto"])
                };

                Canvas.SetLeft(barraCosto, area.X);
                Canvas.SetTop(barraCosto, area.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.1);
                canvas.Children.Add(barraCosto);

                // Barra de beneficio (derecha)
                var beneficioRelativo = maxBeneficio > 0 ? (double)(item.GananciaBruta / maxBeneficio) : 0;
                var anchoBeneficio = beneficioRelativo * area.Width * 0.4;

                var barraBeneficio = new Rectangle
                {
                    Width = anchoBeneficio,
                    Height = alturaBarraPorItem * 0.3,
                    Fill = new SolidColorBrush(colores["LineaBeneficio"])
                };

                Canvas.SetLeft(barraBeneficio, area.X + area.Width * 0.5);
                Canvas.SetTop(barraBeneficio, area.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.5);
                canvas.Children.Add(barraBeneficio);

                // Etiqueta del item
                var etiqueta = new TextBlock
                {
                    Text = item.Nombre.Length > 10 ? item.Nombre.Substring(0, 10) + "..." : item.Nombre,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(colores["Texto"])
                };

                Canvas.SetLeft(etiqueta, area.X + area.Width * 0.42);
                Canvas.SetTop(etiqueta, area.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.3);
                canvas.Children.Add(etiqueta);
            }
        }

        private void DibujarEtiquetasEjes(Canvas canvas, Rect area, decimal maxValor, Dictionary<string, Color> colores)
        {
            // Etiquetas del eje Y (rentabilidad %)
            for (int i = 0; i <= 5; i++)
            {
                var valor = maxValor * i / 5;
                var y = area.Bottom - (area.Height * i / 5);

                var etiqueta = new TextBlock
                {
                    Text = $"{valor:F0}%",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(colores["Texto"]),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Canvas.SetLeft(etiqueta, area.X - 50);
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
                MessageBox.Show($"📸 Exportar Gráfico de Rentabilidad\n\nFuncionalidad disponible próximamente.\n\nIncluirá:\n• Exportación a PNG, JPG, SVG\n• Resolución personalizable\n• Análisis de rentabilidad incluido\n• Metadatos financieros", "Exportar Imagen", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusVentana.Text = "📸 Preparando exportación de gráfico de rentabilidad...";
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
                MessageBox.Show($"⚙️ Configuración Avanzada de Gráficos de Rentabilidad\n\nPróximamente disponible:\n• Personalización de umbrales de rentabilidad\n• Configuración de colores por nivel\n• Opciones de renderizado avanzado\n• Filtros por márgenes y ROI\n• Alertas de rentabilidad", "Configuración Avanzada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo configuración: {ex.Message}");
            }
        }
        #endregion
    }
}