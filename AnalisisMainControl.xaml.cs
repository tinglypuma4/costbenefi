using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using costbenefi.Data;
using Microsoft.EntityFrameworkCore;
using costbenefi.Models;
using System.Threading.Tasks;

namespace costbenefi.Views
{
    /// <summary>
    /// UserControl principal para el módulo de Análisis Costo-Beneficio
    /// </summary>
    public partial class AnalisisMainControl : UserControl
    {
        // ========== VARIABLES PRIVADAS ==========
        private AppDbContext _context;
        private List<RawMaterial> _productos = new();
        private List<Venta> _ventas = new();
        private DateTime _periodoInicio = DateTime.Now.AddMonths(-1);
        private DateTime _periodoFin = DateTime.Now;
        private bool _moduloCargado = false;

        // ========== CONSTRUCTOR ==========
        public AnalisisMainControl()
        {
            InitializeComponent();
            Loaded += AnalisisMainControl_Loaded;
        }

        // ========== EVENTOS DE CARGA ==========
        private async void AnalisisMainControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "📈 Iniciando módulo de análisis...";

                // Actualizar fecha/hora
                ActualizarFechaHora();

                // Inicializar contexto
                _context = new AppDbContext();

                // Cargar categorías en ComboBox
                await CargarCategorias();

                // Cargar datos iniciales
                await CargarDatosAnalisis();

                // Configurar pestaña por defecto
                AnalisisTabControl.SelectedIndex = 0;
                ActivarBoton(BtnRentabilidad);

                _moduloCargado = true;
                TxtStatusAnalisis.Text = "✅ Módulo de análisis costo-beneficio listo";
                ActualizarStatusBar();

                System.Diagnostics.Debug.WriteLine("✅ AnalisisMainControl cargado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando AnalisisMainControl: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cargar módulo de análisis";

                MessageBox.Show(
                    $"Error al inicializar el módulo de análisis:\n\n{ex.Message}",
                    "Error de Inicialización",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // ========== MÉTODOS DE CARGA DE DATOS ==========
        private async Task CargarCategorias()
        {
            try
            {
                var categorias = await _context.RawMaterials
                    .Where(m => !m.Eliminado)
                    .Select(m => m.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                // Limpiar y agregar "Todas"
                CmbCategoria.Items.Clear();
                CmbCategoria.Items.Add(new ComboBoxItem { Content = "Todas", IsSelected = true });

                // Agregar categorías encontradas
                foreach (var categoria in categorias)
                {
                    if (!string.IsNullOrEmpty(categoria))
                    {
                        CmbCategoria.Items.Add(new ComboBoxItem { Content = categoria });
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📊 Categorías cargadas: {categorias.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando categorías: {ex.Message}");
            }
        }

        private async Task CargarDatosAnalisis()
        {
            try
            {
                TxtStatusAnalisis.Text = "📊 Cargando datos para análisis...";

                // Cargar productos activos
                _productos = await _context.RawMaterials
                    .Where(m => !m.Eliminado)
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                // Cargar ventas del período
                _ventas = await _context.Ventas
                    .Include(v => v.DetallesVenta)
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"📦 Productos cargados: {_productos.Count}");
                System.Diagnostics.Debug.WriteLine($"💰 Ventas del período: {_ventas.Count}");

                TxtStatusAnalisis.Text = "✅ Datos cargados - Listo para análisis";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cargar datos de análisis";
            }
        }

        // ========== EVENTOS DE BOTONES DE NAVEGACIÓN ==========
        private void BtnRentabilidad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalisisTabControl.SelectedIndex = 0;
                ActivarBoton(BtnRentabilidad);
                TxtStatusAnalisis.Text = "💰 Análisis de rentabilidad seleccionado";
                System.Diagnostics.Debug.WriteLine("📊 Cambiado a: Análisis de Rentabilidad");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnRentabilidad_Click: {ex.Message}");
            }
        }

        private void BtnPuntoEquilibrio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalisisTabControl.SelectedIndex = 1;
                ActivarBoton(BtnPuntoEquilibrio);
                TxtStatusAnalisis.Text = "⚖️ Análisis de punto de equilibrio seleccionado";
                System.Diagnostics.Debug.WriteLine("📊 Cambiado a: Punto de Equilibrio");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnPuntoEquilibrio_Click: {ex.Message}");
            }
        }

        private void BtnMetricasAvanzadas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalisisTabControl.SelectedIndex = 2;
                ActivarBoton(BtnMetricasAvanzadas);
                TxtStatusAnalisis.Text = "📊 Métricas avanzadas seleccionadas";
                System.Diagnostics.Debug.WriteLine("📊 Cambiado a: Métricas Avanzadas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnMetricasAvanzadas_Click: {ex.Message}");
            }
        }

        private void BtnAnalisisABC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalisisTabControl.SelectedIndex = 3;
                ActivarBoton(BtnAnalisisABC);
                TxtStatusAnalisis.Text = "🔤 Análisis ABC seleccionado";
                System.Diagnostics.Debug.WriteLine("📊 Cambiado a: Análisis ABC");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnAnalisisABC_Click: {ex.Message}");
            }
        }

        private void BtnComparativasTempo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnalisisTabControl.SelectedIndex = 4;
                ActivarBoton(BtnComparativasTempo);
                TxtStatusAnalisis.Text = "📅 Análisis de tendencias seleccionado";
                System.Diagnostics.Debug.WriteLine("📊 Cambiado a: Comparativas Temporales");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnComparativasTempo_Click: {ex.Message}");
            }
        }

        // ========== EVENTOS DE BOTONES DE ACCIÓN ==========
        private async void BtnActualizarAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnActualizarAnalisis.IsEnabled = false;
                BtnActualizarAnalisis.Content = "⏳ Actualizando...";

                await CargarDatosAnalisis();
                ActualizarStatusBar();

                TxtStatusAnalisis.Text = $"✅ Análisis actualizado - {DateTime.Now:HH:mm:ss}";

                // Mostrar notificación de éxito
                MessageBox.Show(
                    "✅ Datos de análisis actualizados correctamente!\n\n" +
                    $"📦 Productos: {_productos.Count}\n" +
                    $"💰 Ventas: {_ventas.Count}",
                    "Actualización Completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando análisis: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al actualizar análisis";

                MessageBox.Show(
                    $"Error al actualizar datos de análisis:\n\n{ex.Message}",
                    "Error de Actualización",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                BtnActualizarAnalisis.IsEnabled = true;
                BtnActualizarAnalisis.Content = "🔄 Actualizar";
            }
        }

        private void BtnExportarAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Implementar exportación de análisis
                MessageBox.Show(
                    "📊 Exportación de Análisis\n\n" +
                    "Esta funcionalidad estará disponible en una próxima versión.\n\n" +
                    "Permitirá exportar:\n" +
                    "• Reportes de rentabilidad en PDF\n" +
                    "• Análisis comparativo en Excel\n" +
                    "• Gráficos y métricas avanzadas\n" +
                    "• Dashboards personalizados",
                    "Próximamente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                TxtStatusAnalisis.Text = "📊 Función de exportación disponible próximamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnExportarAnalisis_Click: {ex.Message}");
            }
        }

        // ========== EVENTOS DEL TABCONTROL ==========
        private void AnalisisTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_moduloCargado) return;

                var tabControl = sender as TabControl;
                if (tabControl?.SelectedItem is TabItem selectedTab)
                {
                    var tabName = selectedTab.Name ?? "Desconocido";
                    System.Diagnostics.Debug.WriteLine($"📊 Tab seleccionado: {tabName}");

                    // Actualizar estado visual de botones según la pestaña
                    switch (tabControl.SelectedIndex)
                    {
                        case 0: ActivarBoton(BtnRentabilidad); break;
                        case 1: ActivarBoton(BtnPuntoEquilibrio); break;
                        case 2: ActivarBoton(BtnMetricasAvanzadas); break;
                        case 3: ActivarBoton(BtnAnalisisABC); break;
                        case 4: ActivarBoton(BtnComparativasTempo); break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisTabControl_SelectionChanged: {ex.Message}");
            }
        }

        // ========== MÉTODOS AUXILIARES ==========

        private void ActualizarFechaHora()
        {
            try
            {
                TxtFechaHora.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando fecha/hora: {ex.Message}");
                TxtFechaHora.Text = "--/--/---- --:--";
            }
        }

        private void ActivarBoton(Button botonActivo)
        {
            try
            {
                // Resetear todos los botones a estado inactivo
                var botones = new[] { BtnRentabilidad, BtnPuntoEquilibrio, BtnMetricasAvanzadas, BtnAnalisisABC, BtnComparativasTempo };

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
                // Actualizar contadores
                TxtProductosAnalisis.Text = $"Productos: {_productos.Count}";

                // Actualizar período actual
                var periodoTexto = GetPeriodoSeleccionado();
                TxtPeriodoActual.Text = $"Período: {periodoTexto}";

                // Actualizar hora de última actualización
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm}";

                System.Diagnostics.Debug.WriteLine("📊 Status bar actualizado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando status bar: {ex.Message}");
            }
        }

        private string GetPeriodoSeleccionado()
        {
            try
            {
                var selectedItem = CmbPeriodo.SelectedItem as ComboBoxItem;
                return selectedItem?.Content?.ToString() ?? "Último mes";
            }
            catch
            {
                return "Último mes";
            }
        }

        // ========== MÉTODOS DE ANÁLISIS (PLACEHOLDER) ==========

        /// <summary>
        /// Calcula las métricas de rentabilidad básicas
        /// TODO: Implementar cálculos reales
        /// </summary>
        private async Task<RentabilidadMetricas> CalcularRentabilidad()
        {
            return await Task.FromResult(new RentabilidadMetricas
            {
                MargenBrutoPromedio = 0,
                MargenNetoPromedio = 0,
                ProductoMasRentable = "N/A",
                ProductoMenosRentable = "N/A",
                TotalVentas = _ventas.Sum(v => v.Total),
                TotalCostos = 0
            });
        }

        /// <summary>
        /// Calcula el punto de equilibrio por producto
        /// TODO: Implementar cálculos reales
        /// </summary>
        private async Task<List<PuntoEquilibrioProducto>> CalcularPuntosEquilibrio()
        {
            return await Task.FromResult(new List<PuntoEquilibrioProducto>());
        }

        // ========== LIMPIEZA DE RECURSOS ==========
        
    }

    // ========== CLASES AUXILIARES PARA ANÁLISIS ==========

    /// <summary>
    /// Métricas de rentabilidad calculadas
    /// </summary>
    public class RentabilidadMetricas
    {
        public decimal MargenBrutoPromedio { get; set; }
        public decimal MargenNetoPromedio { get; set; }
        public string ProductoMasRentable { get; set; } = "";
        public string ProductoMenosRentable { get; set; } = "";
        public decimal TotalVentas { get; set; }
        public decimal TotalCostos { get; set; }
        public decimal GananciaTotal => TotalVentas - TotalCostos;
        public decimal ROI => TotalCostos > 0 ? (GananciaTotal / TotalCostos) * 100 : 0;
    }

    /// <summary>
    /// Punto de equilibrio por producto
    /// </summary>
    public class PuntoEquilibrioProducto
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = "";
        public decimal CostoFijo { get; set; }
        public decimal CostoVariable { get; set; }
        public decimal PrecioVenta { get; set; }
        public int UnidadesEquilibrio { get; set; }
        public decimal VentasEquilibrio { get; set; }
        public decimal MargenContribucion => PrecioVenta - CostoVariable;
    }

    /// <summary>
    /// Clasificación ABC de productos
    /// </summary>
    public class ClasificacionABC
    {
        public string Producto { get; set; } = "";
        public decimal VentasAcumuladas { get; set; }
        public decimal PorcentajeAcumulado { get; set; }
        public string ClaseABC { get; set; } = ""; // A, B, o C
    }
}
