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
    /// Ventana especializada para vista ampliada de gráficos ABC
    /// </summary>
    public partial class VentanaGraficoAmpliado : Window
    {
        #region Variables privadas
        private List<ItemAnalisisABC> _datosAnalisis;
        private string _tipoAnalisis = "productos";
        private string _criterioAnalisis = "rentabilidad";
        private string _tipoGraficoActual = "pareto";
        private string _temaActual = "estandar";
        private Stopwatch _cronometroRender = new();
        private bool _esPantallaCompleta = false;
        private WindowState _estadoAnterior;
        private WindowStyle _estiloAnterior;
        #endregion

        #region Constructor
        public VentanaGraficoAmpliado(List<ItemAnalisisABC> datos, string tipoAnalisis, string criterioAnalisis)
        {
            InitializeComponent();

            _datosAnalisis = datos ?? new List<ItemAnalisisABC>();
            _tipoAnalisis = tipoAnalisis;
            _criterioAnalisis = criterioAnalisis;

            InicializarVentana();
        }
        #endregion

        #region Inicialización
        private void InicializarVentana()
        {
            try
            {
                // Configurar información del header
                ActualizarInformacionHeader();

                // Configurar estado inicial
                TxtStatusVentana.Text = "📈 Inicializando vista ampliada...";
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

                // Dibujar gráfico inicial al cargar
                Loaded += (s, e) => DibujarGraficoAmpliado();

                System.Diagnostics.Debug.WriteLine($"✅ VentanaGraficoAmpliado inicializada:");
                System.Diagnostics.Debug.WriteLine($"   📊 Datos: {_datosAnalisis.Count} items");
                System.Diagnostics.Debug.WriteLine($"   🎯 Tipo: {_tipoAnalisis}");
                System.Diagnostics.Debug.WriteLine($"   📈 Criterio: {_criterioAnalisis}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando VentanaGraficoAmpliado: {ex.Message}");
                MessageBox.Show($"Error al inicializar vista ampliada:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarInformacionHeader()
        {
            var tipoTexto = _tipoAnalisis switch
            {
                "productos" => "Productos",
                "proveedores" => "Proveedores",
                "clientes" => "Clientes",
                "categorias" => "Categorías",
                _ => "Items"
            };

            var criterioTexto = _criterioAnalisis switch
            {
                "rentabilidad" => "Rentabilidad",
                "volumen" => "Volumen de Ventas",
                "rotacion" => "Rotación",
                "mixto" => "Puntuación Mixta",
                _ => "Valor"
            };

            TxtItemsInfo.Text = $"📊 Items analizados: {_datosAnalisis.Count}";
            TxtTipoAnalisis.Text = $"🎯 Análisis: {tipoTexto}";
            TxtCriterioAnalisis.Text = $"💰 Criterio: {criterioTexto}";
            TxtTituloGrafico.Text = $"Análisis Pareto - {tipoTexto} por {criterioTexto}";
            TxtTotalItems.Text = $"Total: {_datosAnalisis.Count} items";
            TxtValorTotal.Text = $"Valor Total: ${_datosAnalisis.Sum(d => d.Valor):N0}";

            // Calcular información de Pareto
            if (_datosAnalisis.Any())
            {
                var total = _datosAnalisis.Count;
                var primeros20Porciento = Math.Max(1, (int)(total * 0.2));
                var valorPrimeros20 = _datosAnalisis.Take(primeros20Porciento).Sum(r => r.Valor);
                var valorTotal = _datosAnalisis.Sum(r => r.Valor);
                var porcentajePareto = valorTotal > 0 ? (valorPrimeros20 / valorTotal) * 100 : 0;

                TxtPareto8020Info.Text = $"📊 80/20: {porcentajePareto:F1}%";
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
            if (CmbTipoGrafico.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _tipoGraficoActual = item.Tag.ToString();
                DibujarGraficoAmpliado();
            }
        }

        private void CmbTemaGrafico_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTemaGrafico.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _temaActual = item.Tag.ToString();
                DibujarGraficoAmpliado();
            }
        }

        private void CmbResolucion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbResolucion.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                ActualizarResolucionCanvas(item.Tag.ToString());
                TxtResolucionActual.Text = $"Resolución: {item.Content}";
                DibujarGraficoAmpliado();
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            DibujarGraficoAmpliado();
            TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
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
                TxtStatusVentana.Text = "🎨 Renderizando gráfico de alta resolución...";

                switch (_tipoGraficoActual)
                {
                    case "pareto":
                        DibujarGraficoPareto();
                        break;
                    case "barras":
                        DibujarGraficoBarras();
                        break;
                    case "linea":
                        DibujarGraficoLinea();
                        break;
                    case "calor":
                        DibujarMapaCalor();
                        break;
                    default:
                        DibujarGraficoPareto();
                        break;
                }

                _cronometroRender.Stop();
                TxtTiempoRenderizado.Text = $"Renderizado: {_cronometroRender.ElapsedMilliseconds}ms";
                TxtStatusVentana.Text = "✅ Gráfico de alta resolución renderizado correctamente";

                System.Diagnostics.Debug.WriteLine($"✅ Gráfico ampliado renderizado: {_tipoGraficoActual} en {_cronometroRender.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico ampliado: {ex.Message}");
                TxtStatusVentana.Text = "❌ Error al renderizar gráfico";
            }
        }

        private void DibujarGraficoPareto()
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
                var maxValor = itemsGrafico.Max(i => i.Valor);

                // Configurar colores según tema
                var colores = ObtenerColoresTema();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar barras con mayor detalle
                var anchoBarraPorItem = areaGrafico.Width / itemsGrafico.Count;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var alturaRelativa = maxValor > 0 ? (double)(item.Valor / maxValor) : 0;
                    var alturaBarra = alturaRelativa * areaGrafico.Height * 0.8;

                    // Barra principal con gradiente
                    var barra = new Rectangle
                    {
                        Width = anchoBarraPorItem * 0.7,
                        Height = alturaBarra,
                        Fill = CrearGradienteBarra(item.ClaseABC, colores),
                        Stroke = new SolidColorBrush(colores["Borde"]),
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(barra, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.15);
                    Canvas.SetTop(barra, areaGrafico.Bottom - alturaBarra);
                    canvas.Children.Add(barra);

                    // Etiqueta del item (mejorada para vista ampliada)
                    if (ChkMostrarEtiquetas.IsChecked == true)
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
                            Text = $"${item.Valor:N0}",
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

                // Línea de Pareto (80%) mejorada
                if (ChkMostrarLinea80.IsChecked == true)
                {
                    DibujarLineaPareto(canvas, areaGrafico, colores);
                }

                // Línea acumulativa
                DibujarLineaAcumulativa(canvas, areaGrafico, itemsGrafico, colores);

                // Etiquetas de ejes mejoradas
                DibujarEtiquetasEjes(canvas, areaGrafico, maxValor, colores);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoPareto: {ex.Message}");
            }
        }

        private void DibujarGraficoBarras()
        {
            // Implementación simplificada del gráfico de barras
            DibujarGraficoPareto(); // Por ahora usar la misma base
        }

        private void DibujarGraficoLinea()
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
                var colores = ObtenerColoresTema();

                // Dibujar ejes
                DibujarEjes(canvas, areaGrafico, colores);

                // Dibujar línea acumulativa principal
                DibujarLineaAcumulativaCompleta(canvas, areaGrafico, itemsGrafico, colores);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoLinea: {ex.Message}");
            }
        }

        private void DibujarMapaCalor()
        {
            try
            {
                CanvasGraficoAmpliado.Children.Clear();

                // Implementación futura del mapa de calor
                var mensaje = new TextBlock
                {
                    Text = "🔥 Mapa de Calor\n\nFuncionalidad en desarrollo\nPróximamente disponible",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(mensaje, CanvasGraficoAmpliado.MinWidth / 2 - 150);
                Canvas.SetTop(mensaje, CanvasGraficoAmpliado.MinHeight / 2 - 50);
                CanvasGraficoAmpliado.Children.Add(mensaje);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarMapaCalor: {ex.Message}");
            }
        }
        #endregion

        #region Métodos auxiliares de dibujo
        private Dictionary<string, Color> ObtenerColoresTema()
        {
            return _temaActual switch
            {
                "oscuro" => new Dictionary<string, Color>
                {
                    ["ClaseA"] = Color.FromRgb(34, 197, 94),
                    ["ClaseB"] = Color.FromRgb(251, 191, 36),
                    ["ClaseC"] = Color.FromRgb(248, 113, 113),
                    ["Fondo"] = Color.FromRgb(31, 41, 55),
                    ["Texto"] = Color.FromRgb(243, 244, 246),
                    ["TextoValor"] = Color.FromRgb(156, 163, 175),
                    ["Borde"] = Color.FromRgb(75, 85, 99),
                    ["Linea80"] = Color.FromRgb(248, 113, 113),
                    ["LineaAcum"] = Color.FromRgb(59, 130, 246)
                },
                "vibrante" => new Dictionary<string, Color>
                {
                    ["ClaseA"] = Color.FromRgb(236, 72, 153),
                    ["ClaseB"] = Color.FromRgb(168, 85, 247),
                    ["ClaseC"] = Color.FromRgb(59, 130, 246),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(17, 24, 39),
                    ["TextoValor"] = Color.FromRgb(75, 85, 99),
                    ["Borde"] = Color.FromRgb(209, 213, 219),
                    ["Linea80"] = Color.FromRgb(220, 38, 127),
                    ["LineaAcum"] = Color.FromRgb(147, 51, 234)
                },
                "minimal" => new Dictionary<string, Color>
                {
                    ["ClaseA"] = Color.FromRgb(0, 0, 0),
                    ["ClaseB"] = Color.FromRgb(75, 85, 99),
                    ["ClaseC"] = Color.FromRgb(156, 163, 175),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(0, 0, 0),
                    ["TextoValor"] = Color.FromRgb(107, 114, 128),
                    ["Borde"] = Color.FromRgb(229, 231, 235),
                    ["Linea80"] = Color.FromRgb(0, 0, 0),
                    ["LineaAcum"] = Color.FromRgb(75, 85, 99)
                },
                _ => new Dictionary<string, Color> // Estándar
                {
                    ["ClaseA"] = Color.FromRgb(16, 185, 129),
                    ["ClaseB"] = Color.FromRgb(245, 158, 11),
                    ["ClaseC"] = Color.FromRgb(239, 68, 68),
                    ["Fondo"] = Color.FromRgb(255, 255, 255),
                    ["Texto"] = Color.FromRgb(55, 65, 81),
                    ["TextoValor"] = Color.FromRgb(107, 114, 128),
                    ["Borde"] = Color.FromRgb(229, 231, 235),
                    ["Linea80"] = Color.FromRgb(239, 68, 68),
                    ["LineaAcum"] = Color.FromRgb(59, 130, 246)
                }
            };
        }

        private LinearGradientBrush CrearGradienteBarra(string claseABC, Dictionary<string, Color> colores)
        {
            var colorBase = claseABC switch
            {
                "A" => colores["ClaseA"],
                "B" => colores["ClaseB"],
                "C" => colores["ClaseC"],
                _ => colores["ClaseC"]
            };

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

        private void DibujarLineaPareto(Canvas canvas, Rect area, Dictionary<string, Color> colores)
        {
            var lineaPareto = new Line
            {
                X1 = area.X,
                Y1 = area.Y + area.Height * 0.2, // 80% desde arriba
                X2 = area.Right,
                Y2 = area.Y + area.Height * 0.2,
                Stroke = new SolidColorBrush(colores["Linea80"]),
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 10, 5 }
            };
            canvas.Children.Add(lineaPareto);

            // Etiqueta 80%
            var etiqueta80 = new TextBlock
            {
                Text = "80%",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(colores["Linea80"])
            };
            Canvas.SetLeft(etiqueta80, area.X - 40);
            Canvas.SetTop(etiqueta80, area.Y + area.Height * 0.2 - 10);
            canvas.Children.Add(etiqueta80);
        }

        private void DibujarLineaAcumulativa(Canvas canvas, Rect area, List<ItemAnalisisABC> items, Dictionary<string, Color> colores)
        {
            if (items.Count < 2) return;

            var path = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(colores["LineaAcum"]),
                StrokeThickness = 3,
                Fill = null
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure();

            // Punto inicial
            var anchoBarraPorItem = area.Width / items.Count;
            figure.StartPoint = new Point(area.X + anchoBarraPorItem * 0.5, area.Bottom);

            for (int i = 0; i < items.Count; i++)
            {
                var x = area.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.5;
                var y = area.Bottom - ((double)items[i].PorcentajeAcumulado / 100.0) * area.Height * 0.8;

                figure.Segments.Add(new LineSegment(new Point(x, y), true));
            }

            geometry.Figures.Add(figure);
            path.Data = geometry;
            canvas.Children.Add(path);
        }

        private void DibujarLineaAcumulativaCompleta(Canvas canvas, Rect area, List<ItemAnalisisABC> items, Dictionary<string, Color> colores)
        {
            // Implementación más detallada para gráfico de línea principal
            DibujarLineaAcumulativa(canvas, area, items, colores);
        }

        private void DibujarEtiquetasEjes(Canvas canvas, Rect area, decimal maxValor, Dictionary<string, Color> colores)
        {
            // Etiquetas del eje Y (valores)
            for (int i = 0; i <= 5; i++)
            {
                var valor = maxValor * i / 5;
                var y = area.Bottom - (area.Height * i / 5);

                var etiqueta = new TextBlock
                {
                    Text = $"${valor:N0}",
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
                // TODO: Implementar exportación a imagen
                MessageBox.Show($"📸 Exportar Gráfico a Imagen\n\nFuncionalidad disponible próximamente.\n\nIncluirá:\n• Exportación a PNG, JPG, SVG\n• Resolución personalizable\n• Calidad profesional\n• Metadatos incluidos", "Exportar Imagen", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusVentana.Text = "📸 Preparando exportación de imagen...";
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
                // TODO: Implementar ventana de configuración avanzada
                MessageBox.Show($"⚙️ Configuración Avanzada de Gráficos\n\nPróximamente disponible:\n• Personalización de colores\n• Configuración de ejes\n• Opciones de renderizado\n• Filtros avanzados\n• Animaciones", "Configuración Avanzada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo configuración: {ex.Message}");
            }
        }
        #endregion
    }
}