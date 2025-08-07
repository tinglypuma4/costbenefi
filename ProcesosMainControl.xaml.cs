using System;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Data;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class ProcesosMainControl : UserControl
    {
        private AppDbContext _context;

        public ProcesosMainControl()
        {
            InitializeComponent();
            this.Unloaded += ProcesosMainControl_Unloaded; // ✅ AGREGAR ESTA LÍNEA
            InitializeAsync();
        }
        private async void InitializeAsync()
        {
            try
            {
                // Inicializar contexto
                _context = new AppDbContext();

                // Cargar estadísticas iniciales
                await CargarEstadisticasProcesos();

                TxtStatusProcesos.Text = "✅ Módulo de Procesos cargado correctamente";
            }
            catch (Exception ex)
            {
                TxtStatusProcesos.Text = "❌ Error al cargar módulo de Procesos";
                System.Diagnostics.Debug.WriteLine($"Error en ProcesosMainControl: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre la ventana de Procesos de Fabricación
        /// </summary>
        private void BtnFabricacion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir ventana de fabricación
                FabricacionProceso.AbrirFabricacion(Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir fabricación:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Abre la ventana de Servicios de Venta
        /// </summary>
        private void BtnServicios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusProcesos.Text = "🛍️ Abriendo Servicios de Venta...";

                var serviciosWindow = new ServiciosVentaWindow()  // ← Faltaba esta línea
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                serviciosWindow.ShowDialog();

                // Actualizar estadísticas después de cerrar la ventana
                _ = CargarEstadisticasProcesos();
                TxtStatusProcesos.Text = "🛍️ Ventana de Servicios cerrada";
            }
            catch (Exception ex)
            {
                TxtStatusProcesos.Text = "❌ Error al abrir Servicios";
                MessageBox.Show($"Error al abrir Servicios de Venta:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Actualiza las estadísticas del módulo
        /// </summary>
        private async void BtnRefrescarProcesos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnRefrescarProcesos.IsEnabled = false;
                BtnRefrescarProcesos.Content = "⏳";
                TxtStatusProcesos.Text = "🔄 Actualizando estadísticas...";

                await CargarEstadisticasProcesos();

                TxtStatusProcesos.Text = $"✅ Estadísticas actualizadas - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                TxtStatusProcesos.Text = "❌ Error al actualizar estadísticas";
                MessageBox.Show($"Error al actualizar:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnRefrescarProcesos.IsEnabled = true;
                BtnRefrescarProcesos.Content = "🔄 Actualizar";
            }
        }

        /// <summary>
        /// Muestra ayuda sobre el módulo de procesos
        /// </summary>
        private void BtnAyudaProcesos_Click(object sender, RoutedEventArgs e)
        {
            string ayuda = "🔧 AYUDA - MÓDULO DE PROCESOS\n\n" +
                          "📋 PROCESOS DE FABRICACIÓN:\n" +
                          "• Cree recetas que transforman materias primas en productos terminados\n" +
                          "• El sistema descuenta automáticamente del inventario\n" +
                          "• Agregue los productos fabricados al inventario\n" +
                          "• Analice costos vs beneficios de fabricar\n\n" +

                          "🛍️ SERVICIOS DE VENTA:\n" +
                          "• Configure servicios que consumen productos del inventario\n" +
                          "• Cree promociones y combos automáticos\n" +
                          "• Integre servicios directamente al punto de venta\n" +
                          "• Aplique descuentos inteligentes\n\n" +

                          "💡 CONSEJOS:\n" +
                          "• Verifique siempre tener stock suficiente antes de procesar\n" +
                          "• Los movimientos quedan registrados para auditoría\n" +
                          "• Use análisis costo-beneficio para tomar decisiones\n" +
                          "• Configure alertas de stock bajo en materias primas críticas";

            MessageBox.Show(ayuda, "Ayuda - Módulo de Procesos",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Carga las estadísticas del módulo de procesos
        /// </summary>
        private async System.Threading.Tasks.Task CargarEstadisticasProcesos()
        {
            try
            {
                // TODO: Implementar cuando tengamos los modelos de Proceso
                // Por ahora usar valores por defecto

                TxtTotalProcesos.Text = "0";
                TxtProcesosActivos.Text = "0";
                TxtProcesamientosHoy.Text = "0";
                TxtAhorroEstimado.Text = "$0.00";

                /*
                // FUTURO: Cuando tengamos las tablas de procesos
                var totalProcesos = await _context.Procesos.CountAsync();
                var procesosActivos = await _context.Procesos.CountAsync(p => p.Activo);
                var procesamientosHoy = await _context.ProcesamientosRealizados
                    .CountAsync(pr => pr.Fecha.Date == DateTime.Today);
                var ahorroEstimado = await _context.ProcesamientosRealizados
                    .Where(pr => pr.Fecha >= DateTime.Today.AddDays(-30))
                    .SumAsync(pr => pr.AhorroGenerado);

                TxtTotalProcesos.Text = totalProcesos.ToString();
                TxtProcesosActivos.Text = procesosActivos.ToString();
                TxtProcesamientosHoy.Text = procesamientosHoy.ToString();
                TxtAhorroEstimado.Text = ahorroEstimado.ToString("C2");
                */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando estadísticas de procesos: {ex.Message}");

                // Valores por defecto en caso de error
                TxtTotalProcesos.Text = "Error";
                TxtProcesosActivos.Text = "Error";
                TxtProcesamientosHoy.Text = "Error";
                TxtAhorroEstimado.Text = "Error";
            }
        }

        /// <summary>
        /// Limpia recursos al destruir el control
        /// </summary>
        private void ProcesosMainControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _context?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar recursos ProcesosMainControl: {ex.Message}");
            }
        }
    }
}