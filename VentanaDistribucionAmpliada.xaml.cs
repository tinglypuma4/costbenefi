using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana especializada para vista ampliada de distribución ABC
    /// </summary>
    public partial class VentanaDistribucionAmpliada : Window
    {
        #region Variables privadas
        private List<ItemAnalisisABC> _datosAnalisis;
        private string _tipoAnalisis = "productos";
        private string _criterioAnalisis = "rentabilidad";
        private string _tipoVisualizacionActual = "circular";
        private string _estiloActual = "estandar";
        #endregion

        #region Constructor
        public VentanaDistribucionAmpliada(List<ItemAnalisisABC> datos, string tipoAnalisis, string criterioAnalisis)
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
                TxtStatusDistribucion.Text = "🥧 Inicializando distribución ABC ampliada...";
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

                // Dibujar distribución inicial al cargar
                Loaded += (s, e) => DibujarDistribucionAmpliada();

                System.Diagnostics.Debug.WriteLine($"✅ VentanaDistribucionAmpliada inicializada:");
                System.Diagnostics.Debug.WriteLine($"   📊 Datos: {_datosAnalisis.Count} items");
                System.Diagnostics.Debug.WriteLine($"   🎯 Tipo: {_tipoAnalisis}");
                System.Diagnostics.Debug.WriteLine($"   📈 Criterio: {_criterioAnalisis}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando VentanaDistribucionAmpliada: {ex.Message}");
                MessageBox.Show($"Error al inicializar distribución ampliada:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            TxtTotalItems.Text = $"📊 Total: {_datosAnalisis.Count} items";
            TxtTipoAnalisis.Text = $"🎯 {tipoTexto}";
            TxtCriterioAnalisis.Text = $"💰 {criterioTexto}";
            TxtTituloGrafico.Text = $"Distribución ABC - {tipoTexto} por {criterioTexto}";
            TxtTotalValor.Text = $"Total: ${_datosAnalisis.Sum(d => d.Valor):N0}";
            TxtItemsAnalizados.Text = $"Items: {_datosAnalisis.Count}";

            // Actualizar métricas de clases
            ActualizarMetricasClases();
        }

        private void ActualizarMetricasClases()
        {
            if (!_datosAnalisis.Any()) return;

            var claseA = _datosAnalisis.Where(r => r.ClaseABC == "A").ToList();
            var claseB = _datosAnalisis.Where(r => r.ClaseABC == "B").ToList();
            var claseC = _datosAnalisis.Where(r => r.ClaseABC == "C").ToList();
            var total = _datosAnalisis.Count;

            // Cantidades y porcentajes
            TxtCantidadA.Text = claseA.Count.ToString();
            TxtPorcentajeA.Text = total > 0 ? $"{(claseA.Count * 100.0 / total):F1}%" : "0%";

            TxtCantidadB.Text = claseB.Count.ToString();
            TxtPorcentajeB.Text = total > 0 ? $"{(claseB.Count * 100.0 / total):F1}%" : "0%";

            TxtCantidadC.Text = claseC.Count.ToString();
            TxtPorcentajeC.Text = total > 0 ? $"{(claseC.Count * 100.0 / total):F1}%" : "0%";

            // Valores por clase
            TxtValorA.Text = $"${claseA.Sum(c => c.Valor):N0}";
            TxtValorB.Text = $"${claseB.Sum(c => c.Valor):N0}";
            TxtValorC.Text = $"${claseC.Sum(c => c.Valor):N0}";
        }
        #endregion

        #region Eventos de UI
        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CmbTipoVisualizacion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTipoVisualizacion.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _tipoVisualizacionActual = item.Tag.ToString();
                DibujarDistribucionAmpliada();
            }
        }

        private void CmbEstilo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbEstilo.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _estiloActual = item.Tag.ToString();
                DibujarDistribucionAmpliada();
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            DibujarDistribucionAmpliada();
            TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
        }

        private void BtnAnimaciones_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementar animaciones
            MessageBox.Show("✨ Animaciones\n\nFuncionalidad disponible próximamente.\n\nIncluirá:\n• Transiciones suaves\n• Animaciones de entrada\n• Efectos de hover\n• Rotación automática", "Animaciones", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExportarDistribucion_Click(object sender, RoutedEventArgs e)
        {
            ExportarDistribucionAImagen();
        }

        private void BtnDetalles_Click(object sender, RoutedEventArgs e)
        {
            MostrarDetallesAvanzados();
        }
        #endregion

        #region Dibujo de distribución
        private void DibujarDistribucionAmpliada()
        {
            try
            {
                TxtStatusDistribucion.Text = "🎨 Renderizando distribución ABC...";

                switch (_tipoVisualizacionActual)
                {
                    case "circular":
                        DibujarGraficoCircular();
                        break;
                    case "barras":
                        DibujarBarrasHorizontales();
                        break;
                    case "area":
                        DibujarGraficoArea();
                        break;
                    case "dona":
                        DibujarGraficoDona();
                        break;
                    default:
                        DibujarGraficoCircular();
                        break;
                }

                // Generar análisis detallado
                GenerarAnalisisDetallado();

                TxtStatusDistribucion.Text = "✅ Distribución ABC renderizada correctamente";

                System.Diagnostics.Debug.WriteLine($"✅ Distribución ampliada renderizada: {_tipoVisualizacionActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando distribución ampliada: {ex.Message}");
                TxtStatusDistribucion.Text = "❌ Error al renderizar distribución";
            }
        }

        private void DibujarGraficoCircular()
        {
            try
            {
                CanvasDistribucionAmpliada.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var canvas = CanvasDistribucionAmpliada;
                var centerX = canvas.MinWidth / 2;
                var centerY = canvas.MinHeight / 2;
                var radius = Math.Min(centerX, centerY) - 80;

                var claseA = _datosAnalisis.Where(r => r.ClaseABC == "A").ToList();
                var claseB = _datosAnalisis.Where(r => r.ClaseABC == "B").ToList();
                var claseC = _datosAnalisis.Where(r => r.ClaseABC == "C").ToList();
                var total = _datosAnalisis.Count;

                if (total == 0) return;

                var colores = ObtenerColoresEstilo();

                // Calcular ángulos
                var anguloA = (double)claseA.Count / total * 360;
                var anguloB = (double)claseB.Count / total * 360;
                var anguloC = (double)claseC.Count / total * 360;

                var anguloInicio = 0.0;

                // Dibujar sector A
                if (claseA.Count > 0)
                {
                    DibujarSectorCircular(canvas, centerX, centerY, radius, anguloInicio, anguloA, colores["ClaseA"], "A", claseA.Count, (double)claseA.Count / total * 100);
                    anguloInicio += anguloA;
                }

                // Dibujar sector B
                if (claseB.Count > 0)
                {
                    DibujarSectorCircular(canvas, centerX, centerY, radius, anguloInicio, anguloB, colores["ClaseB"], "B", claseB.Count, (double)claseB.Count / total * 100);
                    anguloInicio += anguloB;
                }

                // Dibujar sector C
                if (claseC.Count > 0)
                {
                    DibujarSectorCircular(canvas, centerX, centerY, radius, anguloInicio, anguloC, colores["ClaseC"], "C", claseC.Count, (double)claseC.Count / total * 100);
                }

                // Dibujar leyenda si está habilitada
                if (ChkMostrarLeyenda.IsChecked == true)
                {
                    DibujarLeyendaCircular(canvas, colores);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoCircular: {ex.Message}");
            }
        }

        private void DibujarBarrasHorizontales()
        {
            try
            {
                CanvasDistribucionAmpliada.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var canvas = CanvasDistribucionAmpliada;
                var width = canvas.MinWidth - 100;
                var height = canvas.MinHeight - 100;

                var claseA = _datosAnalisis.Where(r => r.ClaseABC == "A").ToList();
                var claseB = _datosAnalisis.Where(r => r.ClaseABC == "B").ToList();
                var claseC = _datosAnalisis.Where(r => r.ClaseABC == "C").ToList();
                var total = _datosAnalisis.Count;

                if (total == 0) return;

                var colores = ObtenerColoresEstilo();
                var alturaBarraPorClase = Math.Min(60, height / 4);
                var margenY = 50;

                // Barra A
                var anchoA = (double)claseA.Count / total * width;
                DibujarBarraHorizontal(canvas, 50, margenY, anchoA, alturaBarraPorClase, colores["ClaseA"], "A", claseA.Count, (double)claseA.Count / total * 100);

                // Barra B
                var anchoB = (double)claseB.Count / total * width;
                DibujarBarraHorizontal(canvas, 50, margenY + alturaBarraPorClase + 20, anchoB, alturaBarraPorClase, colores["ClaseB"], "B", claseB.Count, (double)claseB.Count / total * 100);

                // Barra C
                var anchoC = (double)claseC.Count / total * width;
                DibujarBarraHorizontal(canvas, 50, margenY + (alturaBarraPorClase + 20) * 2, anchoC, alturaBarraPorClase, colores["ClaseC"], "C", claseC.Count, (double)claseC.Count / total * 100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarBarrasHorizontales: {ex.Message}");
            }
        }

        private void DibujarGraficoArea()
        {
            // Por ahora usar barras horizontales como base
            DibujarBarrasHorizontales();
        }

        private void DibujarGraficoDona()
        {
            try
            {
                CanvasDistribucionAmpliada.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var canvas = CanvasDistribucionAmpliada;
                var centerX = canvas.MinWidth / 2;
                var centerY = canvas.MinHeight / 2;
                var radiusExterno = Math.Min(centerX, centerY) - 80;
                var radiusInterno = radiusExterno * 0.5; // 50% del radio externo

                var claseA = _datosAnalisis.Where(r => r.ClaseABC == "A").ToList();
                var claseB = _datosAnalisis.Where(r => r.ClaseABC == "B").ToList();
                var claseC = _datosAnalisis.Where(r => r.ClaseABC == "C").ToList();
                var total = _datosAnalisis.Count;

                if (total == 0) return;

                var colores = ObtenerColoresEstilo();

                // Calcular ángulos
                var anguloA = (double)claseA.Count / total * 360;
                var anguloB = (double)claseB.Count / total * 360;
                var anguloC = (double)claseC.Count / total * 360;

                var anguloInicio = 0.0;

                // Dibujar sectores de dona
                if (claseA.Count > 0)
                {
                    DibujarSectorDona(canvas, centerX, centerY, radiusExterno, radiusInterno, anguloInicio, anguloA, colores["ClaseA"], "A", claseA.Count, (double)claseA.Count / total * 100);
                    anguloInicio += anguloA;
                }

                if (claseB.Count > 0)
                {
                    DibujarSectorDona(canvas, centerX, centerY, radiusExterno, radiusInterno, anguloInicio, anguloB, colores["ClaseB"], "B", claseB.Count, (double)claseB.Count / total * 100);
                    anguloInicio += anguloB;
                }

                if (claseC.Count > 0)
                {
                    DibujarSectorDona(canvas, centerX, centerY, radiusExterno, radiusInterno, anguloInicio, anguloC, colores["ClaseC"], "C", claseC.Count, (double)claseC.Count / total * 100);
                }

                // Texto central
                var textoCentral = new TextBlock
                {
                    Text = $"ABC\n{total}\nItems",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(colores["Texto"]),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(textoCentral, centerX - 30);
                Canvas.SetTop(textoCentral, centerY - 25);
                canvas.Children.Add(textoCentral);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en DibujarGraficoDona: {ex.Message}");
            }
        }
        #endregion

        #region Métodos auxiliares de dibujo
        private Dictionary<string, Color> ObtenerColoresEstilo()
        {
            return _estiloActual switch
            {
                "oscuro" => new Dictionary<string, Color>
                {
                    ["ClaseA"] = Color.FromRgb(34, 197, 94),
                    ["ClaseB"] = Color.FromRgb(251, 191, 36),
                    ["ClaseC"] = Color.FromRgb(248, 113, 113),
                    ["Texto"] = Color.FromRgb(243, 244, 246),
                    ["Fondo"] = Color.FromRgb(31, 41, 55)
                },
                "vibrante" => new Dictionary<string, Color>
                {
                    ["ClaseA"] = Color.FromRgb(236, 72, 153),
                    ["ClaseB"] = Color.FromRgb(168, 85, 247),
                    ["ClaseC"] = Color.FromRgb(59, 130, 246),
                    ["Texto"] = Color.FromRgb(17, 24, 39),
                    ["Fondo"] = Color.FromRgb(255, 255, 255)
                },
                "minimal" => new Dictionary<string, Color>
                {
                    ["ClaseA"] = Color.FromRgb(0, 0, 0),
                    ["ClaseB"] = Color.FromRgb(75, 85, 99),
                    ["ClaseC"] = Color.FromRgb(156, 163, 175),
                    ["Texto"] = Color.FromRgb(0, 0, 0),
                    ["Fondo"] = Color.FromRgb(255, 255, 255)
                },
                _ => new Dictionary<string, Color> // Estándar
                {
                    ["ClaseA"] = Color.FromRgb(16, 185, 129),
                    ["ClaseB"] = Color.FromRgb(245, 158, 11),
                    ["ClaseC"] = Color.FromRgb(239, 68, 68),
                    ["Texto"] = Color.FromRgb(55, 65, 81),
                    ["Fondo"] = Color.FromRgb(255, 255, 255)
                }
            };
        }

        private void DibujarSectorCircular(Canvas canvas, double centerX, double centerY, double radius, double anguloInicio, double anguloSector, Color color, string clase, int cantidad, double porcentaje)
        {
            if (anguloSector <= 0) return;

            var path = new System.Windows.Shapes.Path
            {
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure();

            // Convertir ángulos a radianes
            var anguloInicioRad = (anguloInicio - 90) * Math.PI / 180;
            var anguloFinRad = (anguloInicio + anguloSector - 90) * Math.PI / 180;

            // Punto inicial (centro)
            figure.StartPoint = new Point(centerX, centerY);

            // Línea al inicio del arco
            var puntoInicio = new Point(
                centerX + radius * Math.Cos(anguloInicioRad),
                centerY + radius * Math.Sin(anguloInicioRad)
            );
            figure.Segments.Add(new LineSegment(puntoInicio, true));

            // Arco
            var puntoFin = new Point(
                centerX + radius * Math.Cos(anguloFinRad),
                centerY + radius * Math.Sin(anguloFinRad)
            );

            var arcSegment = new ArcSegment
            {
                Point = puntoFin,
                Size = new Size(radius, radius),
                IsLargeArc = anguloSector > 180,
                SweepDirection = SweepDirection.Clockwise
            };
            figure.Segments.Add(arcSegment);

            // Línea de regreso al centro
            figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

            geometry.Figures.Add(figure);
            path.Data = geometry;
            canvas.Children.Add(path);

            // Etiquetas si están habilitadas
            if (ChkMostrarPorcentajes.IsChecked == true || ChkMostrarValores.IsChecked == true)
            {
                var anguloMedio = (anguloInicio + anguloSector / 2 - 90) * Math.PI / 180;
                var x = centerX + (radius * 0.7) * Math.Cos(anguloMedio);
                var y = centerY + (radius * 0.7) * Math.Sin(anguloMedio);

                var texto = "";
                if (ChkMostrarPorcentajes.IsChecked == true && ChkMostrarValores.IsChecked == true)
                    texto = $"{clase}\n{cantidad}\n{porcentaje:F1}%";
                else if (ChkMostrarPorcentajes.IsChecked == true)
                    texto = $"{clase}\n{porcentaje:F1}%";
                else if (ChkMostrarValores.IsChecked == true)
                    texto = $"{clase}\n{cantidad}";

                var etiqueta = new TextBlock
                {
                    Text = texto,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                Canvas.SetLeft(etiqueta, x - 20);
                Canvas.SetTop(etiqueta, y - 15);
                canvas.Children.Add(etiqueta);
            }
        }

        private void DibujarBarraHorizontal(Canvas canvas, double x, double y, double ancho, double altura, Color color, string clase, int cantidad, double porcentaje)
        {
            var barra = new Rectangle
            {
                Width = ancho,
                Height = altura,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };

            Canvas.SetLeft(barra, x);
            Canvas.SetTop(barra, y);
            canvas.Children.Add(barra);

            // Etiqueta de clase
            var etiquetaClase = new TextBlock
            {
                Text = $"Clase {clase}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(etiquetaClase, x - 45);
            Canvas.SetTop(etiquetaClase, y + altura / 2 - 10);
            canvas.Children.Add(etiquetaClase);

            // Etiquetas de valores
            if (ChkMostrarPorcentajes.IsChecked == true || ChkMostrarValores.IsChecked == true)
            {
                var texto = "";
                if (ChkMostrarPorcentajes.IsChecked == true && ChkMostrarValores.IsChecked == true)
                    texto = $"{cantidad} items ({porcentaje:F1}%)";
                else if (ChkMostrarPorcentajes.IsChecked == true)
                    texto = $"{porcentaje:F1}%";
                else if (ChkMostrarValores.IsChecked == true)
                    texto = $"{cantidad} items";

                var etiquetaValor = new TextBlock
                {
                    Text = texto,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(etiquetaValor, x + ancho / 2 - 30);
                Canvas.SetTop(etiquetaValor, y + altura / 2 - 8);
                canvas.Children.Add(etiquetaValor);
            }
        }

        private void DibujarSectorDona(Canvas canvas, double centerX, double centerY, double radiusExterno, double radiusInterno, double anguloInicio, double anguloSector, Color color, string clase, int cantidad, double porcentaje)
        {
            if (anguloSector <= 0) return;

            // Implementación similar al sector circular pero con hueco interno
            // Por simplicidad, usar el método del sector circular por ahora
            DibujarSectorCircular(canvas, centerX, centerY, radiusExterno, anguloInicio, anguloSector, color, clase, cantidad, porcentaje);
        }

        private void DibujarLeyendaCircular(Canvas canvas, Dictionary<string, Color> colores)
        {
            var x = canvas.MinWidth - 150;
            var y = 50;

            // Fondo de leyenda
            var fondoLeyenda = new Rectangle
            {
                Width = 130,
                Height = 100,
                Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Stroke = new SolidColorBrush(Colors.LightGray),
                StrokeThickness = 1
            };

            Canvas.SetLeft(fondoLeyenda, x);
            Canvas.SetTop(fondoLeyenda, y);
            canvas.Children.Add(fondoLeyenda);

            // Elementos de leyenda
            DibujarElementoLeyenda(canvas, x + 10, y + 15, colores["ClaseA"], "Clase A (Alta)");
            DibujarElementoLeyenda(canvas, x + 10, y + 40, colores["ClaseB"], "Clase B (Media)");
            DibujarElementoLeyenda(canvas, x + 10, y + 65, colores["ClaseC"], "Clase C (Baja)");
        }

        private void DibujarElementoLeyenda(Canvas canvas, double x, double y, Color color, string texto)
        {
            var cuadrado = new Rectangle
            {
                Width = 15,
                Height = 15,
                Fill = new SolidColorBrush(color)
            };

            Canvas.SetLeft(cuadrado, x);
            Canvas.SetTop(cuadrado, y);
            canvas.Children.Add(cuadrado);

            var etiqueta = new TextBlock
            {
                Text = texto,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(etiqueta, x + 20);
            Canvas.SetTop(etiqueta, y - 2);
            canvas.Children.Add(etiqueta);
        }
        #endregion

        #region Funcionalidades adicionales
        private void GenerarAnalisisDetallado()
        {
            try
            {
                PanelAnalisisDetallado.Children.Clear();

                if (!_datosAnalisis.Any()) return;

                var insights = GenerarInsightsDistribucion();

                foreach (var insight in insights)
                {
                    var textBlock = new TextBlock
                    {
                        Text = insight,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    PanelAnalisisDetallado.Children.Add(textBlock);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando análisis detallado: {ex.Message}");
            }
        }

        private List<string> GenerarInsightsDistribucion()
        {
            var insights = new List<string>();

            try
            {
                var claseA = _datosAnalisis.Where(r => r.ClaseABC == "A").ToList();
                var claseB = _datosAnalisis.Where(r => r.ClaseABC == "B").ToList();
                var claseC = _datosAnalisis.Where(r => r.ClaseABC == "C").ToList();
                var total = _datosAnalisis.Count;

                insights.Add($"🔍 ANÁLISIS ABC DETALLADO");
                insights.Add($"📊 Total analizado: {total} items");
                insights.Add("");

                // Análisis de Clase A
                insights.Add($"🥇 CLASE A ({claseA.Count} items - {(double)claseA.Count / total * 100:F1}%):");
                insights.Add($"• Valor total: ${claseA.Sum(c => c.Valor):N0}");
                insights.Add($"• Valor promedio: ${(claseA.Any() ? claseA.Average(c => c.Valor) : 0):N0}");
                insights.Add($"• Concentra el {(claseA.Sum(c => c.Valor) / _datosAnalisis.Sum(d => d.Valor) * 100):F1}% del valor total");
                insights.Add("");

                // Análisis de Clase B
                insights.Add($"🥈 CLASE B ({claseB.Count} items - {(double)claseB.Count / total * 100:F1}%):");
                insights.Add($"• Valor total: ${claseB.Sum(c => c.Valor):N0}");
                insights.Add($"• Valor promedio: ${(claseB.Any() ? claseB.Average(c => c.Valor) : 0):N0}");
                insights.Add("");

                // Análisis de Clase C
                insights.Add($"🥉 CLASE C ({claseC.Count} items - {(double)claseC.Count / total * 100:F1}%):");
                insights.Add($"• Valor total: ${claseC.Sum(c => c.Valor):N0}");
                insights.Add($"• Valor promedio: ${(claseC.Any() ? claseC.Average(c => c.Valor) : 0):N0}");
                insights.Add("");

                // Recomendaciones
                insights.Add($"💡 RECOMENDACIONES:");
                if (claseA.Count > 0)
                    insights.Add($"• Prioriza los {claseA.Count} items Clase A");
                if (claseC.Count > total * 0.5)
                    insights.Add($"• Revisa {claseC.Count} items Clase C para optimización");
                insights.Add($"• Aplica regla 80/20 en recursos");

            }
            catch (Exception ex)
            {
                insights.Add($"❌ Error generando insights: {ex.Message}");
            }

            return insights;
        }

        private void ExportarDistribucionAImagen()
        {
            try
            {
                MessageBox.Show($"📸 Exportar Distribución ABC\n\nFuncionalidad disponible próximamente.\n\nIncluirá:\n• Exportación a PNG/JPG/SVG\n• Alta resolución\n• Metadatos incluidos\n• Formatos profesionales", "Exportar Imagen", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusDistribucion.Text = "📸 Preparando exportación...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exportando imagen: {ex.Message}");
            }
        }

        private void MostrarDetallesAvanzados()
        {
            try
            {
                MessageBox.Show($"📋 Detalles Avanzados ABC\n\nPróximamente disponible:\n• Análisis estadístico completo\n• Comparativas temporales\n• Proyecciones\n• Recomendaciones personalizadas", "Detalles Avanzados", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error mostrando detalles: {ex.Message}");
            }
        }
        #endregion
    }
}