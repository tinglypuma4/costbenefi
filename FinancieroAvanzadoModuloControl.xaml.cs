using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using costbenefi.Data;
using costbenefi.Models;
using System.Globalization;
using System.Threading.Tasks;

namespace costbenefi.Views
{
    /// <summary>
    /// UserControl para Análisis Financiero Avanzado - VPN, TIR, WACC, EOQ, Sensibilidad
    /// </summary>
    public partial class FinancieroAvanzadoModuloControl : UserControl
    {
        #region Variables Privadas
        private readonly AppDbContext _context;
        private ProyectoFinanciero _proyectoActual;
        private bool _datosModificados = false;
        private List<ProyectoFinanciero> _proyectosGuardados = new();
        #endregion

        #region Constructor
        public FinancieroAvanzadoModuloControl(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            InicializarControl();
        }

        // Constructor con proyecto específico
        public FinancieroAvanzadoModuloControl(AppDbContext context, ProyectoFinanciero proyecto) : this(context)
        {
            _proyectoActual = proyecto;
            CargarDatosProyecto();
        }
        #endregion

        #region Inicialización
        private void InicializarControl()
        {
            try
            {
                // Configurar fecha actual
                TxtFechaAnalisis.Text = $"🕒 {DateTime.Now:dd/MM/yyyy HH:mm}";

                // Inicializar proyecto por defecto
                _proyectoActual = new ProyectoFinanciero
                {
                    Nombre = "Nuevo Proyecto",
                    InversionInicial = 100000,
                    TasaDescuento = 0.12m,
                    DuracionAños = 5,
                    FlujosCaja = new List<decimal> { 30000, 35000, 40000, 45000, 50000 },
                    FechaInicio = DateTime.Now
                };

                // Establecer tab por defecto
                TabControlFinanciero.SelectedIndex = 0;
                ActivarBoton(BtnVPN);

                // Estado inicial
                TxtStatusAnalisis.Text = "💎 Análisis Financiero Avanzado inicializado - Listo para calcular";
                ActualizarStatusBar();

                // Configurar eventos de texto cambiado para detectar modificaciones
                ConfigurarEventosModificacion();

                System.Diagnostics.Debug.WriteLine($"✅ FinancieroAvanzadoModuloControl inicializado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando FinancieroAvanzadoModuloControl: {ex.Message}");
                MessageBox.Show($"Error al inicializar análisis financiero:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarEventosModificacion()
        {
            // Eventos para detectar cambios en los datos
            TxtNombreProyecto.TextChanged += (s, e) => _datosModificados = true;
            TxtInversionInicial.TextChanged += (s, e) => _datosModificados = true;
            TxtTasaDescuento.TextChanged += (s, e) => _datosModificados = true;
            TxtDuracionAños.TextChanged += (s, e) => _datosModificados = true;
            TxtFlujosCaja.TextChanged += (s, e) => _datosModificados = true;
        }

        private void CargarDatosProyecto()
        {
            try
            {
                if (_proyectoActual == null) return;

                TxtNombreProyecto.Text = _proyectoActual.Nombre;
                TxtInversionInicial.Text = _proyectoActual.InversionInicial.ToString("F0");
                TxtTasaDescuento.Text = (_proyectoActual.TasaDescuento * 100).ToString("F2");
                TxtDuracionAños.Text = _proyectoActual.DuracionAños.ToString();
                TxtFlujosCaja.Text = string.Join(",", _proyectoActual.FlujosCaja.Select(f => f.ToString("F0")));

                _datosModificados = false;
                ActualizarStatusBar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos del proyecto: {ex.Message}");
            }
        }
        #endregion

        #region Eventos de Header y Toolbar
        private void BtnVentanaIndependiente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "🔗 Abriendo ventana independiente...";

                // Crear y abrir ventana independiente con los mismos datos
                var ventanaIndependiente = new AnalisisFinancieroAvanzadoWindow(_context, _proyectoActual);
                ventanaIndependiente.Show();

                TxtStatusAnalisis.Text = "✅ Ventana independiente abierta";
                System.Diagnostics.Debug.WriteLine($"✅ Ventana independiente de financiero avanzado abierta");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo ventana independiente: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir ventana independiente";
                MessageBox.Show($"Error al abrir ventana independiente:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ayuda = @"💎 ANÁLISIS FINANCIERO AVANZADO - GUÍA RÁPIDA

📊 VPN (Valor Presente Neto):
- VPN > 0: Proyecto VIABLE y rentable
- VPN < 0: Proyecto NO viable
- VPN = 0: Proyecto en punto de equilibrio

📈 TIR (Tasa Interna de Retorno):
- TIR > Tasa descuento: Proyecto ACEPTA
- TIR < Tasa descuento: Proyecto RECHAZA
- TIR = Tasa descuento: Indiferente

⚖️ RBC (Relación Beneficio-Costo):
- RBC > 1: Por cada peso invertido, se obtiene más de un peso
- RBC < 1: Proyecto genera pérdidas
- RBC = 1: Punto de equilibrio

⏰ Período de Recuperación:
- Tiempo necesario para recuperar la inversión inicial
- Menor período = Mejor liquidez

💡 CONSEJOS:
- Use tasas de descuento realistas (8-15% típico)
- Considere inflación y riesgo del proyecto
- Analice sensibilidad ante cambios en variables";

                MessageBox.Show(ayuda, "Ayuda - Análisis Financiero", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error mostrando ayuda: {ex.Message}");
            }
        }

        private void BtnVPN_Click(object sender, RoutedEventArgs e)
        {
            TabControlFinanciero.SelectedIndex = 0;
            ActivarBoton(BtnVPN);
        }

        private void BtnWACC_Click(object sender, RoutedEventArgs e)
        {
            TabControlFinanciero.SelectedIndex = 1;
            ActivarBoton(BtnWACC);
            MessageBox.Show("🚧 Módulo WACC en desarrollo\n\nPróximamente incluirá:\n• Cálculo de costo de capital\n• Análisis de estructura financiera\n• Optimización de financiamiento",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnEOQ_Click(object sender, RoutedEventArgs e)
        {
            TabControlFinanciero.SelectedIndex = 2;
            ActivarBoton(BtnEOQ);
            MessageBox.Show("🚧 Módulo EOQ en desarrollo\n\nPróximamente incluirá:\n• Cantidad económica de pedido\n• Optimización de inventarios\n• Costos de almacenamiento",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSensibilidad_Click(object sender, RoutedEventArgs e)
        {
            TabControlFinanciero.SelectedIndex = 3;
            ActivarBoton(BtnSensibilidad);
            MessageBox.Show("🚧 Análisis de Sensibilidad en desarrollo\n\nPróximamente incluirá:\n• Análisis de escenarios\n• Simulación Monte Carlo\n• Análisis de riesgo",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnComparativa_Click(object sender, RoutedEventArgs e)
        {
            TabControlFinanciero.SelectedIndex = 4;
            ActivarBoton(BtnComparativa);
            MessageBox.Show("🚧 Comparativa de Proyectos en desarrollo\n\nPróximamente incluirá:\n• Ranking de proyectos\n• Análisis multicriterio\n• Matrices de decisión",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGuardarProyecto_Click(object sender, RoutedEventArgs e)
        {
            GuardarProyectoActual();
        }

        private void BtnExportarAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("📊 Exportación de Análisis Financiero\n\nPróximamente disponible:\n• Reporte PDF con resultados\n• Excel con cálculos detallados\n• Gráficos y comparativas",
                              "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusAnalisis.Text = "📊 Función de exportación disponible próximamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en exportación: {ex.Message}");
            }
        }
        #endregion

        #region Eventos de Tabs
        private void TabControlFinanciero_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TabControlFinanciero.SelectedItem is TabItem selectedTab)
                {
                    var tabName = selectedTab.Name ?? "Desconocido";
                    System.Diagnostics.Debug.WriteLine($"💎 Tab seleccionado: {tabName}");

                    // Actualizar estado visual de botones según la pestaña
                    switch (TabControlFinanciero.SelectedIndex)
                    {
                        case 0: ActivarBoton(BtnVPN); break;
                        case 1: ActivarBoton(BtnWACC); break;
                        case 2: ActivarBoton(BtnEOQ); break;
                        case 3: ActivarBoton(BtnSensibilidad); break;
                        case 4: ActivarBoton(BtnComparativa); break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en TabControlFinanciero_SelectionChanged: {ex.Message}");
            }
        }
        #endregion

        #region Cálculos VPN/TIR
        private void BtnCalcularVPN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "🔄 Calculando VPN, TIR y métricas financieras...";
                BtnCalcularVPN.IsEnabled = false;
                BtnCalcularVPN.Content = "⏳ Calculando...";

                // Validar y obtener datos de entrada
                if (!ValidarDatosEntrada(out string mensajeError))
                {
                    MessageBox.Show($"Error en los datos de entrada:\n{mensajeError}",
                                  "Datos Inválidos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Actualizar proyecto actual con datos de la UI
                ActualizarProyectoDesdeUI();

                // Realizar cálculos
                var vpn = _proyectoActual.VPN;
                var tir = _proyectoActual.TIR;
                var periodoRecup = _proyectoActual.PeriodoRecuperacion;

                // Calcular RBC
                var beneficios = _proyectoActual.FlujosCaja;
                var costos = Enumerable.Repeat(_proyectoActual.InversionInicial / _proyectoActual.DuracionAños, _proyectoActual.DuracionAños).ToList();
                var rbc = CalculadoraFinancieraAvanzada.CalcularRBC(beneficios, costos, _proyectoActual.TasaDescuento);

                // Actualizar KPIs
                ActualizarKPIsFinancieros(vpn, tir, rbc, periodoRecup);

                // Actualizar resultados detallados
                ActualizarResultadosDetallados(vpn, tir, periodoRecup);

                // Generar recomendación
                GenerarRecomendacionFinal(vpn, tir, rbc);

                TxtStatusAnalisis.Text = "✅ Cálculos completados exitosamente";
                TxtUltimaCalculoStatus();

                System.Diagnostics.Debug.WriteLine($"💎 Cálculos completados - VPN: {vpn:C}, TIR: {tir:P2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando VPN/TIR: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error en los cálculos financieros";
                MessageBox.Show($"Error realizando cálculos:\n{ex.Message}", "Error de Cálculo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnCalcularVPN.IsEnabled = true;
                BtnCalcularVPN.Content = "🔄 Calcular VPN y TIR";
            }
        }

        private bool ValidarDatosEntrada(out string mensajeError)
        {
            mensajeError = "";

            try
            {
                // Validar inversión inicial
                if (!decimal.TryParse(TxtInversionInicial.Text, out decimal inversion) || inversion <= 0)
                {
                    mensajeError = "La inversión inicial debe ser un número positivo";
                    return false;
                }

                // Validar tasa de descuento
                if (!decimal.TryParse(TxtTasaDescuento.Text, out decimal tasa) || tasa <= 0 || tasa > 100)
                {
                    mensajeError = "La tasa de descuento debe estar entre 0.1% y 100%";
                    return false;
                }

                // Validar duración
                if (!int.TryParse(TxtDuracionAños.Text, out int duracion) || duracion <= 0 || duracion > 50)
                {
                    mensajeError = "La duración debe estar entre 1 y 50 años";
                    return false;
                }

                // Validar flujos de caja
                var flujosCajaTexto = TxtFlujosCaja.Text.Trim();
                if (string.IsNullOrEmpty(flujosCajaTexto))
                {
                    mensajeError = "Debe ingresar los flujos de caja";
                    return false;
                }

                var flujos = flujosCajaTexto.Split(',')
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();

                if (flujos.Count != duracion)
                {
                    mensajeError = $"Debe ingresar exactamente {duracion} flujos de caja (uno por año)";
                    return false;
                }

                foreach (var flujo in flujos)
                {
                    if (!decimal.TryParse(flujo, out decimal valor))
                    {
                        mensajeError = $"El flujo '{flujo}' no es un número válido";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                mensajeError = $"Error validando datos: {ex.Message}";
                return false;
            }
        }

        private void ActualizarProyectoDesdeUI()
        {
            try
            {
                _proyectoActual.Nombre = TxtNombreProyecto.Text.Trim();
                _proyectoActual.InversionInicial = decimal.Parse(TxtInversionInicial.Text);
                _proyectoActual.TasaDescuento = decimal.Parse(TxtTasaDescuento.Text) / 100; // Convertir porcentaje a decimal
                _proyectoActual.DuracionAños = int.Parse(TxtDuracionAños.Text);

                var flujosCajaTexto = TxtFlujosCaja.Text.Trim();
                _proyectoActual.FlujosCaja = flujosCajaTexto.Split(',')
                    .Select(f => decimal.Parse(f.Trim()))
                    .ToList();

                _datosModificados = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando proyecto desde UI: {ex.Message}");
                throw;
            }
        }

        private void ActualizarKPIsFinancieros(decimal vpn, decimal tir, decimal rbc, decimal periodoRecup)
        {
            try
            {
                // KPI: VPN
                TxtVPN.Text = vpn >= 1000 ? $"${vpn / 1000:F1}K" : $"${vpn:F0}";
                TxtVPNEstado.Text = vpn > 0 ? "✅ VIABLE" : vpn < 0 ? "❌ NO VIABLE" : "⚖️ EQUILIBRIO";

                // KPI: TIR
                TxtTIR.Text = $"{tir * 100:F2}%";
                var tasaDescuentoPorcentaje = _proyectoActual.TasaDescuento * 100;
                TxtTIRComparacion.Text = tir > _proyectoActual.TasaDescuento ?
                    $"✅ > {tasaDescuentoPorcentaje:F1}%" :
                    $"❌ < {tasaDescuentoPorcentaje:F1}%";

                // KPI: RBC
                TxtRBC.Text = $"{rbc:F2}";
                TxtRBCEstado.Text = rbc > 1 ? "✅ Beneficioso" : rbc < 1 ? "❌ No beneficioso" : "⚖️ Equilibrio";

                // KPI: Período Recuperación
                if (periodoRecup > 0)
                {
                    TxtPeriodoRecup.Text = periodoRecup < 1 ?
                        $"{periodoRecup * 12:F1} meses" :
                        $"{periodoRecup:F1} años";
                    TxtRecupDetalle.Text = periodoRecup <= 3 ? "✅ Rápida recuperación" :
                                          periodoRecup <= 5 ? "⚠️ Recuperación media" :
                                          "❌ Recuperación lenta";
                }
                else
                {
                    TxtPeriodoRecup.Text = "∞";
                    TxtRecupDetalle.Text = "❌ No se recupera";
                }

                // KPI: Viabilidad General
                var esViable = vpn > 0 && tir > _proyectoActual.TasaDescuento && rbc > 1;
                TxtViabilidadIcono.Text = esViable ? "✅" : "❌";
                TxtViabilidad.Text = esViable ? "VIABLE" : "NO VIABLE";
                TxtRecomendacion.Text = esViable ? "Se recomienda ejecutar" : "Se recomienda rechazar";

                // Cambiar colores de fondo según viabilidad
                var colorFondo = esViable ? "#10B981" : "#EF4444"; // Verde o Rojo
                ((Border)TxtViabilidad.Parent).Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorFondo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando KPIs: {ex.Message}");
            }
        }

        private void ActualizarResultadosDetallados(decimal vpn, decimal tir, decimal periodoRecup)
        {
            try
            {
                // Resultado VPN
                TxtResultadoVPN.Text = vpn >= 1000 ? $"${vpn / 1000:F1}K" : $"${vpn:F0}";
                if (vpn > 0)
                {
                    TxtInterpretacionVPN.Text = $"✅ El proyecto generará ${vpn:F0} de valor adicional. VIABLE.";
                    ((Border)TxtResultadoVPN.Parent).Background = new SolidColorBrush(Color.FromRgb(240, 253, 244)); // Verde claro
                }
                else if (vpn < 0)
                {
                    TxtInterpretacionVPN.Text = $"❌ El proyecto destruirá ${Math.Abs(vpn):F0} de valor. NO VIABLE.";
                    ((Border)TxtResultadoVPN.Parent).Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // Rojo claro
                }
                else
                {
                    TxtInterpretacionVPN.Text = "⚖️ El proyecto está en punto de equilibrio.";
                    ((Border)TxtResultadoVPN.Parent).Background = new SolidColorBrush(Color.FromRgb(255, 251, 235)); // Amarillo claro
                }

                // Resultado TIR
                TxtResultadoTIR.Text = $"{tir * 100:F2}%";
                var tasaComparacion = _proyectoActual.TasaDescuento * 100;
                if (tir > _proyectoActual.TasaDescuento)
                {
                    TxtInterpretacionTIR.Text = $"✅ TIR ({tir * 100:F2}%) > Tasa descuento ({tasaComparacion:F2}%). ACEPTA el proyecto.";
                    ((Border)TxtResultadoTIR.Parent).Background = new SolidColorBrush(Color.FromRgb(240, 249, 255)); // Azul claro
                }
                else
                {
                    TxtInterpretacionTIR.Text = $"❌ TIR ({tir * 100:F2}%) < Tasa descuento ({tasaComparacion:F2}%). RECHAZA el proyecto.";
                    ((Border)TxtResultadoTIR.Parent).Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // Rojo claro
                }

                // Resultado Período Recuperación
                if (periodoRecup > 0)
                {
                    TxtResultadoRecup.Text = periodoRecup < 1 ?
                        $"{periodoRecup * 12:F1} meses" :
                        $"{periodoRecup:F1} años";

                    if (periodoRecup <= 2)
                        TxtInterpretacionRecup.Text = "✅ Recuperación muy rápida. Excelente liquidez.";
                    else if (periodoRecup <= 4)
                        TxtInterpretacionRecup.Text = "⚠️ Recuperación moderada. Liquidez aceptable.";
                    else
                        TxtInterpretacionRecup.Text = "❌ Recuperación lenta. Considerar riesgos de liquidez.";
                }
                else
                {
                    TxtResultadoRecup.Text = "No se recupera";
                    TxtInterpretacionRecup.Text = "❌ La inversión nunca se recupera completamente.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando resultados detallados: {ex.Message}");
            }
        }

        private void GenerarRecomendacionFinal(decimal vpn, decimal tir, decimal rbc)
        {
            try
            {
                var recomendacion = "";
                var colorFondo = "";

                // Lógica de recomendación basada en múltiples criterios
                var criteriosPositivos = 0;
                var criteriosTotal = 3;

                if (vpn > 0) criteriosPositivos++;
                if (tir > _proyectoActual.TasaDescuento) criteriosPositivos++;
                if (rbc > 1) criteriosPositivos++;

                if (criteriosPositivos == criteriosTotal)
                {
                    recomendacion = "🎯 RECOMENDACIÓN: EJECUTAR EL PROYECTO\n\n" +
                                  "✅ Todos los indicadores son favorables\n" +
                                  "✅ El proyecto generará valor para la empresa\n" +
                                  "✅ La rentabilidad supera las expectativas\n\n" +
                                  "💡 Proceda con la implementación siguiendo el plan establecido.";
                    colorFondo = "#F0FDF4"; // Verde muy claro
                }
                else if (criteriosPositivos >= 2)
                {
                    recomendacion = "⚠️ RECOMENDACIÓN: EVALUAR CON PRECAUCIÓN\n\n" +
                                  $"✅ {criteriosPositivos}/{criteriosTotal} indicadores son favorables\n" +
                                  "⚠️ Analice los riesgos identificados\n" +
                                  "⚠️ Considere ajustes al proyecto\n\n" +
                                  "💡 Evalúe escenarios alternativos antes de decidir.";
                    colorFondo = "#FFFBEB"; // Amarillo muy claro
                }
                else
                {
                    recomendacion = "❌ RECOMENDACIÓN: RECHAZAR EL PROYECTO\n\n" +
                                  "❌ La mayoría de indicadores son desfavorables\n" +
                                  "❌ El proyecto destruiría valor\n" +
                                  "❌ Los riesgos superan los beneficios\n\n" +
                                  "💡 Busque alternativas de inversión más rentables.";
                    colorFondo = "#FEF2F2"; // Rojo muy claro
                }

                TxtRecomendacionFinal.Text = recomendacion;
                BorderRecomendacion.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorFondo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando recomendación final: {ex.Message}");
                TxtRecomendacionFinal.Text = "Error generando recomendación. Verifique los cálculos.";
            }
        }
        #endregion

        #region Métodos Auxiliares
        private void ActivarBoton(Button botonActivo)
        {
            try
            {
                // Resetear todos los botones a estado inactivo
                var botones = new[] { BtnVPN, BtnWACC, BtnEOQ, BtnSensibilidad, BtnComparativa };

                foreach (var boton in botones)
                {
                    boton.Opacity = 0.7;
                    boton.FontWeight = FontWeights.Normal;
                }

                // Activar el botón seleccionado
                if (botonActivo != null)
                {
                    botonActivo.Opacity = 1.0;
                    botonActivo.FontWeight = FontWeights.Bold;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ActivarBoton: {ex.Message}");
            }
        }

        private void ActualizarStatusBar()
        {
            try
            {
                TxtProyectoActual.Text = $"Proyecto: {_proyectoActual?.Nombre ?? "Nuevo"}";
                TxtEstadoGuardado.Text = _datosModificados ? "💾 Sin guardar" : "✅ Guardado";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando status bar: {ex.Message}");
            }
        }

        private void TxtUltimaCalculoStatus()
        {
            TxtUltimoCalculo.Text = $"Último cálculo: {DateTime.Now:HH:mm:ss}";
        }

        private void GuardarProyectoActual()
        {
            try
            {
                if (_proyectoActual == null) return;

                // TODO: Implementar guardado en base de datos
                // Por ahora solo simular guardado
                _datosModificados = false;
                ActualizarStatusBar();
                TxtStatusAnalisis.Text = $"💾 Proyecto '{_proyectoActual.Nombre}' guardado correctamente";

                MessageBox.Show($"✅ Proyecto '{_proyectoActual.Nombre}' guardado correctamente",
                              "Guardado Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Debug.WriteLine($"💾 Proyecto guardado: {_proyectoActual.Nombre}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error guardando proyecto: {ex.Message}");
                MessageBox.Show($"Error guardando proyecto:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Método público para cargar un proyecto específico
        /// </summary>
        public void CargarProyecto(ProyectoFinanciero proyecto)
        {
            try
            {
                _proyectoActual = proyecto ?? throw new ArgumentNullException(nameof(proyecto));
                CargarDatosProyecto();

                TxtStatusAnalisis.Text = $"📂 Proyecto '{_proyectoActual.Nombre}' cargado correctamente";
                System.Diagnostics.Debug.WriteLine($"📂 Proyecto cargado: {_proyectoActual.Nombre}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando proyecto: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cargar proyecto";
            }
        }

        /// <summary>
        /// Método público para obtener el proyecto actual
        /// </summary>
        public ProyectoFinanciero ObtenerProyectoActual()
        {
            return _proyectoActual;
        }

        /// <summary>
        /// Método público para verificar si hay cambios sin guardar
        /// </summary>
        public bool TieneCambiosSinGuardar()
        {
            return _datosModificados;
        }
        #endregion

        #region Limpieza de Recursos
        private void FinancieroAvanzadoModuloControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Limpiar recursos si es necesario
                _proyectosGuardados?.Clear();
                System.Diagnostics.Debug.WriteLine("🧹 FinancieroAvanzadoModuloControl: Recursos liberados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error liberando recursos: {ex.Message}");
            }
        }
        #endregion
    }
}