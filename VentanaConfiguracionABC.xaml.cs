using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana de configuración avanzada para análisis ABC
    /// </summary>
    public partial class VentanaConfiguracionABC : Window
    {
        #region Propiedades públicas
        public string TipoSeleccionado { get; private set; } = "productos";
        public string CriterioSeleccionado { get; private set; } = "rentabilidad";
        public double UmbralA { get; private set; } = 80;
        public double UmbralB { get; private set; } = 95;
        public ConfiguracionABC ConfiguracionActual { get; private set; }
        #endregion

        #region Variables privadas
        private bool _configuracionCambiada = false;
        #endregion

        #region Constructor
        public VentanaConfiguracionABC(string tipoActual, string criterioActual)
        {
            InitializeComponent();

            TipoSeleccionado = tipoActual;
            CriterioSeleccionado = criterioActual;

            InicializarVentana();
        }
        #endregion

        #region Inicialización
        private void InicializarVentana()
        {
            try
            {
                // Configurar selecciones actuales
                ConfigurarSeleccionesIniciales();

                // Inicializar configuración
                ConfiguracionActual = new ConfiguracionABC();

                // Dibujar vista previa inicial
                DibujarVistaPrevia();

                // Actualizar total de pesos
                ActualizarTotalPesos();

                System.Diagnostics.Debug.WriteLine($"✅ VentanaConfiguracionABC inicializada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando configuración: {ex.Message}");
                MessageBox.Show($"Error al inicializar configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarSeleccionesIniciales()
        {
            // Configurar tipo de análisis
            foreach (ComboBoxItem item in CmbTipoAnalisisConfig.Items)
            {
                if (item.Tag?.ToString() == TipoSeleccionado)
                {
                    CmbTipoAnalisisConfig.SelectedItem = item;
                    break;
                }
            }

            // Configurar criterio de análisis
            foreach (ComboBoxItem item in CmbCriterioAnalisisConfig.Items)
            {
                if (item.Tag?.ToString() == CriterioSeleccionado)
                {
                    CmbCriterioAnalisisConfig.SelectedItem = item;
                    break;
                }
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
                System.Diagnostics.Debug.WriteLine($"🎯 Tipo de análisis cambiado a: {TipoSeleccionado}");
            }
        }

        private void CmbCriterioAnalisisConfig_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCriterioAnalisisConfig.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                CriterioSeleccionado = item.Tag.ToString();
                _configuracionCambiada = true;
                System.Diagnostics.Debug.WriteLine($"📊 Criterio de análisis cambiado a: {CriterioSeleccionado}");
            }
        }

        private void UmbralA_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (double.TryParse(TxtUmbralA.Text, out double valor))
                {
                    if (valor > 0 && valor <= 100)
                    {
                        UmbralA = valor;

                        // Ajustar umbral B si es necesario
                        if (valor >= UmbralB)
                        {
                            UmbralB = Math.Min(100, valor + 10);
                            TxtUmbralB.Text = UmbralB.ToString();
                        }

                        DibujarVistaPrevia();
                        _configuracionCambiada = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en UmbralA_TextChanged: {ex.Message}");
            }
        }

        private void UmbralB_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (double.TryParse(TxtUmbralB.Text, out double valor))
                {
                    if (valor > UmbralA && valor <= 100)
                    {
                        UmbralB = valor;
                        DibujarVistaPrevia();
                        _configuracionCambiada = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en UmbralB_TextChanged: {ex.Message}");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (sender is Slider slider)
                {
                    var valor = Math.Round(slider.Value);

                    if (slider == SliderRentabilidad)
                    {
                        TxtPesoRentabilidad.Text = $"{valor}%";
                    }
                    else if (slider == SliderVolumen)
                    {
                        TxtPesoVolumen.Text = $"{valor}%";
                    }
                    else if (slider == SliderRotacion)
                    {
                        TxtPesoRotacion.Text = $"{valor}%";
                    }

                    ActualizarTotalPesos();
                    _configuracionCambiada = true;
                }
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
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando configuración: {ex.Message}");
                MessageBox.Show($"Error al aplicar configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Métodos auxiliares
        private void DibujarVistaPrevia()
        {
            try
            {
                CanvasVistaPrevia.Children.Clear();

                var width = CanvasVistaPrevia.ActualWidth > 0 ? CanvasVistaPrevia.ActualWidth : 400;
                var height = CanvasVistaPrevia.Height;

                if (width <= 0) return;

                // Calcular anchos proporcionales
                var anchoA = width * (UmbralA / 100);
                var anchoB = width * ((UmbralB - UmbralA) / 100);
                var anchoC = width * ((100 - UmbralB) / 100);

                // Dibujar segmento A
                var rectA = new Rectangle
                {
                    Width = anchoA,
                    Height = height - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectA, 0);
                Canvas.SetTop(rectA, 5);
                CanvasVistaPrevia.Children.Add(rectA);

                // Etiqueta A
                var lblA = new TextBlock
                {
                    Text = "A",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(lblA, anchoA / 2 - 5);
                Canvas.SetTop(lblA, height / 2 - 5);
                CanvasVistaPrevia.Children.Add(lblA);

                // Dibujar segmento B
                var rectB = new Rectangle
                {
                    Width = anchoB,
                    Height = height - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectB, anchoA);
                Canvas.SetTop(rectB, 5);
                CanvasVistaPrevia.Children.Add(rectB);

                // Etiqueta B
                var lblB = new TextBlock
                {
                    Text = "B",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(lblB, anchoA + anchoB / 2 - 5);
                Canvas.SetTop(lblB, height / 2 - 5);
                CanvasVistaPrevia.Children.Add(lblB);

                // Dibujar segmento C
                var rectC = new Rectangle
                {
                    Width = anchoC,
                    Height = height - 10,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rectC, anchoA + anchoB);
                Canvas.SetTop(rectC, 5);
                CanvasVistaPrevia.Children.Add(rectC);

                // Etiqueta C
                var lblC = new TextBlock
                {
                    Text = "C",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(lblC, anchoA + anchoB + anchoC / 2 - 5);
                Canvas.SetTop(lblC, height / 2 - 5);
                CanvasVistaPrevia.Children.Add(lblC);

                // Actualizar descripción
                TxtDescripcionUmbrales.Text = $"A: 0-{UmbralA:F0}% | B: {UmbralA:F0}-{UmbralB:F0}% | C: {UmbralB:F0}-100%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando vista previa: {ex.Message}");
            }
        }

        private void ActualizarTotalPesos()
        {
            try
            {
                var total = SliderRentabilidad.Value + SliderVolumen.Value + SliderRotacion.Value;
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
                    // Restaurar umbrales
                    TxtUmbralA.Text = "80";
                    TxtUmbralB.Text = "95";
                    UmbralA = 80;
                    UmbralB = 95;

                    // Restaurar pesos
                    SliderRentabilidad.Value = 50;
                    SliderVolumen.Value = 30;
                    SliderRotacion.Value = 20;

                    // Restaurar checkboxes
                    ChkSoloActivosConfig.IsChecked = true;
                    ChkExcluirSinVentas.IsChecked = false;
                    ChkAgruparCategorias.IsChecked = false;
                    ChkActualizacionAutomatica.IsChecked = true;
                    ChkMostrarInsights.IsChecked = true;
                    ChkGuardarConfiguracion.IsChecked = true;

                    // Restaurar valores numéricos
                    TxtValorMinimo.Text = "0";
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
                MessageBox.Show(
                    $"📊 Vista Previa de Configuración ABC\n\n" +
                    $"🎯 Análisis: {TipoSeleccionado} por {CriterioSeleccionado}\n\n" +
                    $"📊 Umbrales de Clasificación:\n" +
                    $"• Clase A: 0% - {UmbralA:F0}%\n" +
                    $"• Clase B: {UmbralA:F0}% - {UmbralB:F0}%\n" +
                    $"• Clase C: {UmbralB:F0}% - 100%\n\n" +
                    $"⚖️ Pesos para Análisis Mixto:\n" +
                    $"• Rentabilidad: {SliderRentabilidad.Value:F0}%\n" +
                    $"• Volumen: {SliderVolumen.Value:F0}%\n" +
                    $"• Rotación: {SliderRotacion.Value:F0}%\n\n" +
                    $"🔍 Filtros:\n" +
                    $"• Solo activos: {(ChkSoloActivosConfig.IsChecked == true ? "Sí" : "No")}\n" +
                    $"• Valor mínimo: ${TxtValorMinimo.Text}\n" +
                    $"• Items máximos: {TxtItemsMaximos.Text}",
                    "Vista Previa de Configuración",
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
                    $"❓ Ayuda - Configuración Avanzada ABC\n\n" +
                    $"🎯 TIPOS DE ANÁLISIS:\n" +
                    $"• Productos: Analiza items de inventario\n" +
                    $"• Proveedores: Analiza proveedores por valor\n" +
                    $"• Clientes: Analiza clientes por compras\n" +
                    $"• Categorías: Analiza por agrupaciones\n\n" +
                    $"📊 CRITERIOS:\n" +
                    $"• Rentabilidad: Basado en ganancias\n" +
                    $"• Volumen: Basado en ventas totales\n" +
                    $"• Rotación: Basado en movimiento\n" +
                    $"• Mixto: Combinación ponderada\n\n" +
                    $"📈 UMBRALES ABC:\n" +
                    $"• Clase A: Items de mayor valor (ej: 0-80%)\n" +
                    $"• Clase B: Items de valor medio (ej: 80-95%)\n" +
                    $"• Clase C: Items de menor valor (ej: 95-100%)\n\n" +
                    $"⚖️ PESOS MIXTOS:\n" +
                    $"Para análisis mixto, ajusta la importancia\n" +
                    $"de cada criterio. La suma debe ser 100%.",
                    "Ayuda de Configuración",
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
                if (UmbralA <= 0 || UmbralA >= 100)
                {
                    MessageBox.Show("El umbral de Clase A debe estar entre 1 y 99.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (UmbralB <= UmbralA || UmbralB > 100)
                {
                    MessageBox.Show("El umbral de Clase B debe ser mayor que Clase A y menor o igual a 100.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Validar pesos para análisis mixto
                var totalPesos = SliderRentabilidad.Value + SliderVolumen.Value + SliderRotacion.Value;
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
                ConfiguracionActual = new ConfiguracionABC
                {
                    TipoAnalisis = TipoSeleccionado,
                    CriterioAnalisis = CriterioSeleccionado,
                    UmbralA = UmbralA,
                    UmbralB = UmbralB,
                    PesoRentabilidad = SliderRentabilidad.Value,
                    PesoVolumen = SliderVolumen.Value,
                    PesoRotacion = SliderRotacion.Value,
                    SoloActivos = ChkSoloActivosConfig.IsChecked == true,
                    ExcluirSinVentas = ChkExcluirSinVentas.IsChecked == true,
                    AgruparCategorias = ChkAgruparCategorias.IsChecked == true,
                    ValorMinimo = double.TryParse(TxtValorMinimo.Text, out double valMin) ? valMin : 0,
                    ItemsMaximos = int.TryParse(TxtItemsMaximos.Text, out int maxIt) ? maxIt : 100,
                    ItemsMinimos = int.TryParse(TxtItemsMinimos.Text, out int minIt) ? minIt : 5,
                    ActualizacionAutomatica = ChkActualizacionAutomatica.IsChecked == true,
                    MostrarInsights = ChkMostrarInsights.IsChecked == true,
                    GuardarConfiguracion = ChkGuardarConfiguracion.IsChecked == true
                };

                System.Diagnostics.Debug.WriteLine($"✅ Configuración guardada correctamente");
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
                DibujarVistaPrevia();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        #endregion
    }

    #region Clase de configuración
    /// <summary>
    /// Clase para almacenar la configuración del análisis ABC
    /// </summary>
    public class ConfiguracionABC
    {
        public string TipoAnalisis { get; set; } = "productos";
        public string CriterioAnalisis { get; set; } = "rentabilidad";
        public double UmbralA { get; set; } = 80;
        public double UmbralB { get; set; } = 95;
        public double PesoRentabilidad { get; set; } = 50;
        public double PesoVolumen { get; set; } = 30;
        public double PesoRotacion { get; set; } = 20;
        public bool SoloActivos { get; set; } = true;
        public bool ExcluirSinVentas { get; set; } = false;
        public bool AgruparCategorias { get; set; } = false;
        public double ValorMinimo { get; set; } = 0;
        public int ItemsMaximos { get; set; } = 100;
        public int ItemsMinimos { get; set; } = 5;
        public bool ActualizacionAutomatica { get; set; } = true;
        public bool MostrarInsights { get; set; } = true;
        public bool GuardarConfiguracion { get; set; } = true;
    }
    #endregion
}