using System;
using System.Windows;
using System.Windows.Controls;

namespace costbenefi.Views
{
    /// <summary>
    /// UserControl para módulos en desarrollo
    /// </summary>
    public partial class ModuloEnDesarrolloControl : UserControl
    {
        #region Propiedades
        public string NombreModulo { get; private set; }
        public string DescripcionModulo { get; private set; }
        public DateTime FechaEstimada { get; private set; }
        public int PorcentajeCompletado { get; private set; }
        #endregion

        #region Constructor
        public ModuloEnDesarrolloControl(string nombreModulo)
        {
            InitializeComponent();

            NombreModulo = nombreModulo ?? "Módulo";
            ConfigurarModulo();
            InicializarControl();
        }

        // Constructor con parámetros adicionales
        public ModuloEnDesarrolloControl(string nombreModulo, string descripcion, DateTime fechaEstimada, int porcentaje) : this(nombreModulo)
        {
            DescripcionModulo = descripcion;
            FechaEstimada = fechaEstimada;
            PorcentajeCompletado = porcentaje;
            ActualizarInformacionPersonalizada();
        }
        #endregion

        #region Configuración
        private void ConfigurarModulo()
        {
            // Configurar información específica según el módulo
            switch (NombreModulo?.ToLower())
            {
                case "puntoequilibrio":
                case "punto equilibrio":
                    DescripcionModulo = "Análisis de punto de equilibrio con múltiples productos, costos fijos y variables, simulación de escenarios y gráficos interactivos.";
                    FechaEstimada = DateTime.Now.AddMonths(1);
                    PorcentajeCompletado = 65;
                    break;

                case "metricasavanzadas":
                case "métricas avanzadas":
                case "metricas":
                    DescripcionModulo = "Métricas avanzadas de rentabilidad: EBITDA, ROI, ROE, ROIC, ratios financieros, indicadores de liquidez y análisis de eficiencia operativa.";
                    FechaEstimada = DateTime.Now.AddMonths(2);
                    PorcentajeCompletado = 40;
                    break;

                case "tendencias":
                case "comparativas":
                case "comparativastemporales":
                    DescripcionModulo = "Análisis de tendencias temporales, comparativas históricas, proyecciones futuras, estacionalidad y patrones de comportamiento.";
                    FechaEstimada = DateTime.Now.AddMonths(3);
                    PorcentajeCompletado = 25;
                    break;

                case "inventarios":
                    DescripcionModulo = "Gestión optimizada de inventarios, control de stock, alertas de reposición y análisis de rotación.";
                    FechaEstimada = DateTime.Now.AddMonths(4);
                    PorcentajeCompletado = 15;
                    break;

                case "presupuestos":
                    DescripcionModulo = "Planificación presupuestaria, control de gastos, proyecciones financieras y análisis de desviaciones.";
                    FechaEstimada = DateTime.Now.AddMonths(3);
                    PorcentajeCompletado = 30;
                    break;

                default:
                    DescripcionModulo = "Funcionalidades avanzadas de análisis financiero y optimización de procesos de negocio.";
                    FechaEstimada = DateTime.Now.AddMonths(2);
                    PorcentajeCompletado = 35;
                    break;
            }
        }

        private void InicializarControl()
        {
            try
            {
                // Configurar textos principales
                TxtNombreModulo.Text = NombreModulo;
                TxtDescripcion.Text = DescripcionModulo;
                TxtFechaEstimada.Text = FechaEstimada.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));

                // Configurar fecha actual
                var fechaTexto = $"Actualizado: {DateTime.Now:dd/MM/yyyy HH:mm}";

                System.Diagnostics.Debug.WriteLine($"🚧 Módulo en desarrollo inicializado:");
                System.Diagnostics.Debug.WriteLine($"   📝 Nombre: {NombreModulo}");
                System.Diagnostics.Debug.WriteLine($"   📋 Descripción: {DescripcionModulo}");
                System.Diagnostics.Debug.WriteLine($"   📅 Fecha estimada: {FechaEstimada:MMM yyyy}");
                System.Diagnostics.Debug.WriteLine($"   📊 Completado: {PorcentajeCompletado}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando ModuloEnDesarrolloControl: {ex.Message}");

                // Configuración de emergencia
                TxtNombreModulo.Text = NombreModulo ?? "Módulo";
                TxtDescripcion.Text = "Error cargando descripción del módulo";
                TxtFechaEstimada.Text = "Próximamente";
            }
        }

        private void ActualizarInformacionPersonalizada()
        {
            try
            {
                if (!string.IsNullOrEmpty(DescripcionModulo))
                {
                    TxtDescripcion.Text = DescripcionModulo;
                }

                if (FechaEstimada != default)
                {
                    TxtFechaEstimada.Text = FechaEstimada.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
                }

                System.Diagnostics.Debug.WriteLine($"🔄 Información personalizada actualizada para: {NombreModulo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando información personalizada: {ex.Message}");
            }
        }
        #endregion

        #region Eventos
        private void BtnNotificarInteres_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔔 Usuario solicitó notificación para: {NombreModulo}");

                // Crear mensaje personalizado según el módulo
                var mensaje = GenerarMensajeNotificacion();

                var resultado = MessageBox.Show(mensaje,
                                               "Solicitud de Notificación Registrada",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Information);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Simular registro de notificación
                    RegistrarSolicitudNotificacion();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnNotificarInteres_Click: {ex.Message}");
                MessageBox.Show("Error al registrar la solicitud. Inténtelo nuevamente.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GenerarMensajeNotificacion()
        {
            var fechaFormateada = FechaEstimada.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));

            return $"✅ ¡Gracias por tu interés en el módulo '{NombreModulo}'!\n\n" +
                   $"📋 Características incluidas:\n{DescripcionModulo}\n\n" +
                   $"📅 Fecha estimada: {fechaFormateada}\n" +
                   $"📊 Progreso actual: {PorcentajeCompletado}%\n\n" +
                   $"🔔 Hemos registrado tu solicitud y priorizaremos su desarrollo.\n" +
                   $"Te notificaremos por email cuando esté disponible.\n\n" +
                   $"¿Deseas también recibir actualizaciones de progreso?";
        }

        private void RegistrarSolicitudNotificacion()
        {
            try
            {
                // TODO: Implementar registro real en base de datos
                // Por ahora solo simular el registro

                var confirmacion = $"🎯 ¡Perfecto!\n\n" +
                                 $"Tu solicitud para el módulo '{NombreModulo}' ha sido registrada exitosamente.\n\n" +
                                 $"📧 Te enviaremos actualizaciones sobre:\n" +
                                 $"• Progreso del desarrollo\n" +
                                 $"• Fecha de lanzamiento confirmada\n" +
                                 $"• Acceso anticipado (si aplica)\n" +
                                 $"• Documentación y tutoriales\n\n" +
                                 $"¡Gracias por ayudarnos a priorizar las funcionalidades más importantes!";

                MessageBox.Show(confirmacion, "Notificación Configurada", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Debug.WriteLine($"✅ Solicitud de notificación registrada para: {NombreModulo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error registrando solicitud: {ex.Message}");
                MessageBox.Show("Solicitud registrada localmente. La notificación será configurada en la próxima actualización.",
                              "Registro Local", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Métodos Públicos
        /// <summary>
        /// Actualiza la información del módulo dinámicamente
        /// </summary>
        public void ActualizarInformacion(string nuevaDescripcion, DateTime nuevaFecha, int nuevoPorcentaje)
        {
            try
            {
                DescripcionModulo = nuevaDescripcion ?? DescripcionModulo;
                FechaEstimada = nuevaFecha != default ? nuevaFecha : FechaEstimada;
                PorcentajeCompletado = nuevoPorcentaje >= 0 && nuevoPorcentaje <= 100 ? nuevoPorcentaje : PorcentajeCompletado;

                ActualizarInformacionPersonalizada();

                System.Diagnostics.Debug.WriteLine($"🔄 Información actualizada para {NombreModulo}: {PorcentajeCompletado}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando información del módulo: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el estado actual del desarrollo
        /// </summary>
        public string ObtenerEstadoDesarrollo()
        {
            return PorcentajeCompletado switch
            {
                >= 80 => "🎯 Próximo a finalizar",
                >= 60 => "🔄 En desarrollo activo",
                >= 40 => "📋 En diseño avanzado",
                >= 20 => "📝 En planificación",
                _ => "💡 En conceptualización"
            };
        }

        /// <summary>
        /// Verifica si el módulo está listo para beta testing
        /// </summary>
        public bool EstaListoParaBeta()
        {
            return PorcentajeCompletado >= 70;
        }
        #endregion

        #region Limpieza de Recursos
        private void ModuloEnDesarrolloControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Limpiar recursos si es necesario
                System.Diagnostics.Debug.WriteLine($"🧹 ModuloEnDesarrolloControl: Recursos liberados para {NombreModulo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error liberando recursos: {ex.Message}");
            }
        }
        #endregion
    }
}