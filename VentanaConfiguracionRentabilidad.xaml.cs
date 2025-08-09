using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana de configuración avanzada para análisis de Rentabilidad
    /// </summary>
    public partial class VentanaConfiguracionRentabilidad : Window
    {
        #region Propiedades públicas
        public string TipoSeleccionado { get; private set; } = "productos";
        public string MetricaSeleccionada { get; private set; } = "margen_bruto";
        public double UmbralAlto { get; private set; } = 25; // 25% margen alto
        public double UmbralMedio { get; private set; } = 15; // 15% margen medio
        public ConfiguracionRentabilidad ConfiguracionActual { get; private set; }
        #endregion

        #region Variables privadas
        private bool _configuracionCambiada = false;
        #endregion

        #region Constructor
        public VentanaConfiguracionRentabilidad(string tipoActual, string metricaActual)
        {
            InitializeComponent();

            TipoSeleccionado = tipoActual;
            MetricaSeleccionada = metricaActual;

            InicializarVentana();
        }
        #endregion

        #region Inicialización
        private void InicializarVentana()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Iniciando configuración de ventana...");

                // Configurar selecciones actuales (con validación)
                ConfigurarSeleccionesIniciales();

                // Inicializar configuración
                ConfiguracionActual = new ConfiguracionRentabilidad();

                // Actualizar total de pesos de forma segura
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ActualizarTotalPesos();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error en ActualizarTotalPesos inicial: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                System.Diagnostics.Debug.WriteLine($"✅ VentanaConfiguracionRentabilidad inicializada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando configuración rentabilidad: {ex.Message}");
                MessageBox.Show($"Error al inicializar configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarSeleccionesIniciales()
        {
            try
            {
                // Validar que los ComboBox estén disponibles
                if (CmbTipoAnalisisConfig?.Items == null || CmbMetricaAnalisisConfig?.Items == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ ComboBox no disponibles aún");
                    return;
                }

                // Configurar tipo de análisis
                foreach (ComboBoxItem item in CmbTipoAnalisisConfig.Items)
                {
                    if (item?.Tag?.ToString() == TipoSeleccionado)
                    {
                        CmbTipoAnalisisConfig.SelectedItem = item;
                        break;
                    }
                }

                // Configurar métrica de análisis
                foreach (ComboBoxItem item in CmbMetricaAnalisisConfig.Items)
                {
                    if (item?.Tag?.ToString() == MetricaSeleccionada)
                    {
                        CmbMetricaAnalisisConfig.SelectedItem = item;
                        break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Selecciones iniciales configuradas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando selecciones iniciales: {ex.Message}");
            }
        }
        #endregion

        #region Eventos de UI
        private void CmbTipoAnalisisConfig_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTipoAnalisisConfig.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                TipoSeleccionado = item.Tag.ToString();
                _configuracionCambiada = true;
                System.Diagnostics.Debug.WriteLine($"🎯 Tipo de análisis rentabilidad cambiado a: {TipoSeleccionado}");
            }
        }

        private void CmbMetricaAnalisisConfig_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbMetricaAnalisisConfig.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                MetricaSeleccionada = item.Tag.ToString();
                _configuracionCambiada = true;
                System.Diagnostics.Debug.WriteLine($"📊 Métrica de rentabilidad cambiada a: {MetricaSeleccionada}");
            }
        }

        private void UmbralAlto_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (double.TryParse(TxtUmbralAlto.Text, out double valor))
                {
                    if (valor > 0 && valor <= 100)
                    {
                        UmbralAlto = valor;

                        // Ajustar umbral medio si es necesario
                        if (valor <= UmbralMedio)
                        {
                            UmbralMedio = Math.Max(1, valor - 5);
                            if (TxtUmbralMedio != null)
                            {
                                TxtUmbralMedio.Text = UmbralMedio.ToString();
                            }
                        }

                        // Solo dibujar si el Canvas está disponible
                        if (CanvasVistaPrevia != null)
                        {
                            DibujarVistaPrevia();
                        }
                        _configuracionCambiada = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en UmbralAlto_TextChanged: {ex.Message}");
            }
        }

        private void UmbralMedio_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (double.TryParse(TxtUmbralMedio.Text, out double valor))
                {
                    if (valor > 0 && valor < UmbralAlto)
                    {
                        UmbralMedio = valor;

                        // Solo dibujar si el Canvas está disponible
                        if (CanvasVistaPrevia != null)
                        {
                            DibujarVistaPrevia();
                        }
                        _configuracionCambiada = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en UmbralMedio_TextChanged: {ex.Message}");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (sender is not Slider slider)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Sender no es un Slider");
                    return;
                }

                var valor = Math.Round(slider.Value);

                // Validar elementos antes de usarlos
                if (slider == SliderMargen && TxtPesoMargen != null)
                {
                    TxtPesoMargen.Text = $"{valor}%";
                }
                else if (slider == SliderROI && TxtPesoROI != null)
                {
                    TxtPesoROI.Text = $"{valor}%";
                }
                else if (slider == SliderGanancia && TxtPesoGanancia != null)
                {
                    TxtPesoGanancia.Text = $"{valor}%";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Slider no identificado o TextBlock null: {slider?.Name}");
                    return;
                }

                // Actualizar total de pesos de forma segura
                ActualizarTotalPesos();
                _configuracionCambiada = true;

                System.Diagnostics.Debug.WriteLine($"✅ Slider actualizado: {slider.Name} = {valor}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en Slider_ValueChanged: {ex.Message}");
            }
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            MostrarAyuda();
        }

        private void BtnRestaurarDefecto_Click(object sender, RoutedEventArgs e)
        {
            RestaurarValoresDefecto();
        }

        private void BtnVistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            MostrarVistaPrevia();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_configuracionCambiada)
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro de que desea cancelar?\n\nSe perderán todos los cambios realizados.",
                    "Confirmar Cancelación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                    return;
            }

            this.DialogResult = false;
            this.Close();
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar configuración
                if (!ValidarConfiguracion())
                    return;

                // Guardar configuración
                GuardarConfiguracion();

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando configuración rentabilidad: {ex.Message}");
                MessageBox.Show($"Error al aplicar configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Métodos auxiliares
        private void DibujarVistaPrevia()
        {
            try
            {
                // Validar que el Canvas esté disponible
                if (CanvasVistaPrevia == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ CanvasVistaPrevia es null - vista previa diferida");
                    return; // Salir sin error - se dibujará cuando esté disponible
                }

                CanvasVistaPrevia.Children.Clear();

                var width = CanvasVistaPrevia.ActualWidth > 0 ? CanvasVistaPrevia.ActualWidth : 400;
                var height = CanvasVistaPrevia.Height > 0 ? CanvasVistaPrevia.Height : 40;

                if (width <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Canvas sin dimensiones - usando valores por defecto");
                    width = 400; // Valor por defecto
                }

                // Calcular rangos de rentabilidad
                var umbralBajo = 0;
                var umbralMedio = UmbralMedio;
                var umbralAlto = UmbralAlto;

                // Calcular anchos proporcionales (distribución visual equitativa)
                var anchoAlto = width * 0.4; // 40% para alta rentabilidad
                var anchoMedio = width * 0.35; // 35% para media rentabilidad  
                var anchoBajo = width * 0.25; // 25% para baja rentabilidad

                // Dibujar segmento ALTA rentabilidad (Verde)
                var rectAlto = new Rectangle
                {
                    Width = anchoAlto,
                    Height = height - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectAlto, 0);
                Canvas.SetTop(rectAlto, 5);
                CanvasVistaPrevia.Children.Add(rectAlto);

                // Etiqueta ALTA
                var lblAlto = new TextBlock
                {
                    Text = "ALTA",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(lblAlto, anchoAlto / 2 - 15);
                Canvas.SetTop(lblAlto, height / 2 - 5);
                CanvasVistaPrevia.Children.Add(lblAlto);

                // Dibujar segmento MEDIA rentabilidad (Amarillo)
                var rectMedio = new Rectangle
                {
                    Width = anchoMedio,
                    Height = height - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectMedio, anchoAlto);
                Canvas.SetTop(rectMedio, 5);
                CanvasVistaPrevia.Children.Add(rectMedio);

                // Etiqueta MEDIA
                var lblMedio = new TextBlock
                {
                    Text = "MEDIA",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(lblMedio, anchoAlto + anchoMedio / 2 - 20);
                Canvas.SetTop(lblMedio, height / 2 - 5);
                CanvasVistaPrevia.Children.Add(lblMedio);

                // Dibujar segmento BAJA rentabilidad (Rojo)
                var rectBajo = new Rectangle
                {
                    Width = anchoBajo,
                    Height = height - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectBajo, anchoAlto + anchoMedio);
                Canvas.SetTop(rectBajo, 5);
                CanvasVistaPrevia.Children.Add(rectBajo);

                // Etiqueta BAJA
                var lblBajo = new TextBlock
                {
                    Text = "BAJA",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(lblBajo, anchoAlto + anchoMedio + anchoBajo / 2 - 15);
                Canvas.SetTop(lblBajo, height / 2 - 5);
                CanvasVistaPrevia.Children.Add(lblBajo);

                // Actualizar descripción (validar que TxtDescripcionUmbrales también exista)
                if (TxtDescripcionUmbrales != null)
                {
                    TxtDescripcionUmbrales.Text = $"ALTA: mayor o igual {UmbralAlto:F0}% | MEDIA: {UmbralMedio:F0}-{UmbralAlto:F0}% | BAJA: menor a {UmbralMedio:F0}%";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Vista previa dibujada correctamente: {width}x{height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando vista previa rentabilidad: {ex.Message}");
                // No mostrar MessageBox aquí para evitar spam de errores
            }
        }

        private void ActualizarTotalPesos()
        {
            try
            {
                // Validar que los sliders y el TextBlock estén disponibles
                if (SliderMargen == null || SliderROI == null || SliderGanancia == null || TxtTotalPesos == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Sliders o TxtTotalPesos no disponibles aún");
                    return;
                }

                var total = SliderMargen.Value + SliderROI.Value + SliderGanancia.Value;
                TxtTotalPesos.Text = $"{total:F0}%";

                // Cambiar color según si suma 100%
                if (Math.Abs(total - 100) < 0.1)
                {
                    TxtTotalPesos.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)); // Verde
                }
                else
                {
                    TxtTotalPesos.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Rojo
                }

                System.Diagnostics.Debug.WriteLine($"✅ Total de pesos actualizado: {total:F0}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando total de pesos: {ex.Message}");
            }
        }

        private void RestaurarValoresDefecto()
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro de que desea restaurar todos los valores por defecto?\n\nEsta acción no se puede deshacer.",
                    "Restaurar Valores por Defecto",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Restaurar umbrales de rentabilidad
                    TxtUmbralAlto.Text = "25";
                    TxtUmbralMedio.Text = "15";
                    UmbralAlto = 25;
                    UmbralMedio = 15;

                    // Restaurar pesos métricas
                    SliderMargen.Value = 50;
                    SliderROI.Value = 30;
                    SliderGanancia.Value = 20;

                    // Restaurar checkboxes
                    ChkSoloActivosConfig.IsChecked = true;
                    ChkExcluirSinVentas.IsChecked = false;
                    ChkAgruparCategorias.IsChecked = false;
                    ChkActualizacionAutomatica.IsChecked = true;
                    ChkMostrarInsights.IsChecked = true;
                    ChkGuardarConfiguracion.IsChecked = true;

                    // Restaurar valores numéricos
                    TxtMargenMinimo.Text = "5";
                    TxtItemsMaximos.Text = "100";
                    TxtItemsMinimos.Text = "5";

                    // Restaurar ComboBoxes
                    CmbPeriodoAnalisis.SelectedIndex = 0;
                    CmbFormatoNumeros.SelectedIndex = 0;
                    CmbIdioma.SelectedIndex = 0;

                    DibujarVistaPrevia();
                    ActualizarTotalPesos();

                    _configuracionCambiada = true;

                    MessageBox.Show("Valores por defecto restaurados correctamente.", "Restauración Completa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error restaurando valores por defecto: {ex.Message}");
                MessageBox.Show($"Error al restaurar valores:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarVistaPrevia()
        {
            try
            {
                var metricaTexto = MetricaSeleccionada switch
                {
                    "margen_bruto" => "Margen Bruto",
                    "margen_neto" => "Margen Neto",
                    "roi" => "ROI (Retorno Inversión)",
                    "ganancia_total" => "Ganancia Total",
                    "costo_beneficio" => "Ratio Costo-Beneficio",
                    _ => "Rentabilidad General"
                };

                MessageBox.Show(
                    $"💰 Vista Previa - Configuración Rentabilidad\n\n" +
                    $"🎯 Análisis: {TipoSeleccionado} por {metricaTexto}\n\n" +
                    $"📊 Umbrales de Rentabilidad:\n" +
                    $"• ALTA Rentabilidad: mayor o igual {UmbralAlto:F0}%\n" +
                    $"• MEDIA Rentabilidad: {UmbralMedio:F0}% - {UmbralAlto:F0}%\n" +
                    $"• BAJA Rentabilidad: menor a {UmbralMedio:F0}%\n\n" +
                    $"⚖️ Pesos para Análisis Mixto:\n" +
                    $"• Margen: {SliderMargen.Value:F0}%\n" +
                    $"• ROI: {SliderROI.Value:F0}%\n" +
                    $"• Ganancia: {SliderGanancia.Value:F0}%\n\n" +
                    $"🔍 Filtros:\n" +
                    $"• Solo activos: {(ChkSoloActivosConfig.IsChecked == true ? "Sí" : "No")}\n" +
                    $"• Margen mínimo: {TxtMargenMinimo.Text}%\n" +
                    $"• Items máximos: {TxtItemsMaximos.Text}",
                    "Vista Previa - Configuración Rentabilidad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error mostrando vista previa: {ex.Message}");
            }
        }

        private void MostrarAyuda()
        {
            try
            {
                MessageBox.Show(
                    $"❓ Ayuda - Configuración Análisis de Rentabilidad\n\n" +
                    $"🎯 TIPOS DE ANÁLISIS:\n" +
                    $"• Productos: Analiza rentabilidad por producto\n" +
                    $"• Categorías: Analiza por agrupaciones de productos\n" +
                    $"• Proveedores: Analiza rentabilidad por proveedor\n" +
                    $"• Clientes: Analiza rentabilidad por cliente\n\n" +
                    $"📊 MÉTRICAS DE RENTABILIDAD:\n" +
                    $"• Margen Bruto: (Ventas - Costo) / Ventas × 100\n" +
                    $"• Margen Neto: Considera costos adicionales\n" +
                    $"• ROI: (Ganancia / Inversión) × 100\n" +
                    $"• Ganancia Total: Beneficio absoluto en período\n" +
                    $"• Costo-Beneficio: Ratio de eficiencia\n\n" +
                    $"📈 UMBRALES DE RENTABILIDAD:\n" +
                    $"• ALTA: Items muy rentables (ej: mayor o igual 25%)\n" +
                    $"• MEDIA: Items rentabilidad media (ej: 15-25%)\n" +
                    $"• BAJA: Items poco rentables (ej: menor a 15%)\n\n" +
                    $"⚖️ PESOS MIXTOS:\n" +
                    $"Para análisis combinado, ajusta la importancia\n" +
                    $"de cada métrica. La suma debe ser 100%.",
                    "Ayuda - Configuración Rentabilidad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error mostrando ayuda: {ex.Message}");
            }
        }

        private bool ValidarConfiguracion()
        {
            try
            {
                // Validar umbrales
                if (UmbralMedio <= 0 || UmbralMedio >= 100)
                {
                    MessageBox.Show("El umbral de rentabilidad media debe estar entre 1 y 99%.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (UmbralAlto <= UmbralMedio || UmbralAlto > 100)
                {
                    MessageBox.Show("El umbral de rentabilidad alta debe ser mayor que el medio y menor o igual a 100%.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Validar pesos para análisis mixto
                var totalPesos = SliderMargen.Value + SliderROI.Value + SliderGanancia.Value;
                if (Math.Abs(totalPesos - 100) > 0.1)
                {
                    var resultado = MessageBox.Show(
                        $"Los pesos para análisis mixto suman {totalPesos:F0}% en lugar de 100%.\n\n¿Desea continuar de todas formas?",
                        "Advertencia de Pesos",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultado == MessageBoxResult.No)
                        return false;
                }

                // Validar valores numéricos
                if (!int.TryParse(TxtItemsMinimos.Text, out int minItems) || minItems < 1)
                {
                    MessageBox.Show("El número mínimo de items debe ser un número entero mayor a 0.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!int.TryParse(TxtItemsMaximos.Text, out int maxItems) || maxItems < minItems)
                {
                    MessageBox.Show("El número máximo de items debe ser mayor o igual al mínimo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error validando configuración: {ex.Message}");
                MessageBox.Show($"Error en validación:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void GuardarConfiguracion()
        {
            try
            {
                ConfiguracionActual = new ConfiguracionRentabilidad
                {
                    TipoAnalisis = TipoSeleccionado,
                    MetricaAnalisis = MetricaSeleccionada,
                    UmbralAlto = UmbralAlto,
                    UmbralMedio = UmbralMedio,
                    PesoMargen = SliderMargen.Value,
                    PesoROI = SliderROI.Value,
                    PesoGanancia = SliderGanancia.Value,
                    SoloActivos = ChkSoloActivosConfig.IsChecked == true,
                    ExcluirSinVentas = ChkExcluirSinVentas.IsChecked == true,
                    AgruparCategorias = ChkAgruparCategorias.IsChecked == true,
                    MargenMinimo = double.TryParse(TxtMargenMinimo.Text, out double margenMin) ? margenMin : 0,
                    ItemsMaximos = int.TryParse(TxtItemsMaximos.Text, out int maxIt) ? maxIt : 100,
                    ItemsMinimos = int.TryParse(TxtItemsMinimos.Text, out int minIt) ? minIt : 5,
                    ActualizacionAutomatica = ChkActualizacionAutomatica.IsChecked == true,
                    MostrarInsights = ChkMostrarInsights.IsChecked == true,
                    GuardarConfiguracion = ChkGuardarConfiguracion.IsChecked == true
                };

                System.Diagnostics.Debug.WriteLine($"✅ Configuración rentabilidad guardada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error guardando configuración: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Eventos de ventana
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Dibujar vista previa cuando la ventana esté completamente cargada
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Verificar que todos los elementos estén disponibles
                    if (CanvasVistaPrevia != null)
                    {
                        DibujarVistaPrevia();
                        System.Diagnostics.Debug.WriteLine("✅ Vista previa dibujada en OnSourceInitialized");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ CanvasVistaPrevia aún no disponible en OnSourceInitialized");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error en OnSourceInitialized: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        #endregion
    }

    #region Clase de configuración de rentabilidad
    /// <summary>
    /// Clase para almacenar la configuración del análisis de Rentabilidad
    /// </summary>
    public class ConfiguracionRentabilidad
    {
        public string TipoAnalisis { get; set; } = "productos";
        public string MetricaAnalisis { get; set; } = "margen_bruto";
        public double UmbralAlto { get; set; } = 25; // 25% rentabilidad alta
        public double UmbralMedio { get; set; } = 15; // 15% rentabilidad media
        public double PesoMargen { get; set; } = 50; // 50% peso margen
        public double PesoROI { get; set; } = 30; // 30% peso ROI
        public double PesoGanancia { get; set; } = 20; // 20% peso ganancia
        public bool SoloActivos { get; set; } = true;
        public bool ExcluirSinVentas { get; set; } = false;
        public bool AgruparCategorias { get; set; } = false;
        public double MargenMinimo { get; set; } = 5; // 5% margen mínimo
        public int ItemsMaximos { get; set; } = 100;
        public int ItemsMinimos { get; set; } = 5;
        public bool ActualizacionAutomatica { get; set; } = true;
        public bool MostrarInsights { get; set; } = true;
        public bool GuardarConfiguracion { get; set; } = true;
    }
    #endregion
}