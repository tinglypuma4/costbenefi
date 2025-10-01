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
    /// UserControl principal MODIFICADO para cargar módulos internamente
    /// </summary>
    public partial class AnalisisMainControl : UserControl
    {
        #region Variables Privadas (MANTENER LAS TUYAS)
        private AppDbContext _context;
        private List<RawMaterial> _productos = new();
        private List<Venta> _ventas = new();
        private DateTime _periodoInicio = DateTime.Now.AddMonths(-1);
        private DateTime _periodoFin = DateTime.Now;
        private bool _moduloCargado = false;

        // 🆕 NUEVAS VARIABLES PARA CARGA DINÁMICA
        private string _moduloActual = "";
        private Dictionary<string, UserControl> _modulosCache = new();
        #endregion

        #region Constructor (MANTENER EL TUYO)
        public AnalisisMainControl()
        {
            InitializeComponent();
            Loaded += AnalisisMainControl_Loaded;
            Unloaded += AnalisisMainControl_Unloaded;
        }
        #endregion

        #region Eventos de Carga (MANTENER EL TUYO)
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

                _moduloCargado = true;
                TxtStatusAnalisis.Text = "✅ Dashboard listo - Selecciona un módulo para comenzar";
                ActualizarStatusBar();

                System.Diagnostics.Debug.WriteLine("✅ AnalisisMainControl cargado exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando AnalisisMainControl: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cargar módulo de análisis";
                MessageBox.Show($"Error al inicializar el módulo de análisis:\n\n{ex.Message}",
                              "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region 🆕 NUEVOS EVENTOS DE BOTONES - CARGAR USERCONTROLS

        private async void BtnRentabilidad_Click(object sender, RoutedEventArgs e)
        {
            await CargarModulo("Rentabilidad");
            ActivarBoton(BtnRentabilidad);
        }

        private async void BtnAnalisisFinanciero_Click(object sender, RoutedEventArgs e)
        {
            await CargarModulo("FinancieroAvanzado");
            ActivarBoton(BtnAnalisisFinanciero);
        }

        private async void BtnAnalisisABC_Click(object sender, RoutedEventArgs e)
        {
            await CargarModulo("AnalisisABC");
            ActivarBoton(BtnAnalisisABC);
        }

        private async void BtnPuntoEquilibrio_Click(object sender, RoutedEventArgs e)
        {
            await CargarModulo("PuntoEquilibrio");
            ActivarBoton(BtnPuntoEquilibrio);
        }

        private async void BtnMetricasAvanzadas_Click(object sender, RoutedEventArgs e)
        {
            await CargarModulo("MetricasAvanzadas");
            ActivarBoton(BtnMetricasAvanzadas);
        }

        private async void BtnComparativasTempo_Click(object sender, RoutedEventArgs e)
        {
            await CargarModulo("Tendencias");
            ActivarBoton(BtnComparativasTempo);
        }

        #endregion

        #region 🆕 MÉTODO PRINCIPAL PARA CARGAR MÓDULOS

        private async Task CargarModulo(string nombreModulo)
        {
            try
            {
                // Evitar recargar el mismo módulo
                if (_moduloActual == nombreModulo) return;

                TxtStatusAnalisis.Text = $"🔄 Cargando módulo {nombreModulo}...";
                BtnActualizarAnalisis.IsEnabled = false;

                // Ocultar pantalla de bienvenida
                PantallaBienvenida.Visibility = Visibility.Collapsed;

                UserControl moduloControl = null;

                // Verificar si ya está en caché
                if (_modulosCache.ContainsKey(nombreModulo))
                {
                    moduloControl = _modulosCache[nombreModulo];
                    System.Diagnostics.Debug.WriteLine($"📦 Módulo {nombreModulo} cargado desde caché");
                }
                else
                {
                    // Crear nuevo UserControl según el módulo
                    switch (nombreModulo)
                    {
                        case "Rentabilidad":
                            // 🎯 CONVERTIR TU AnalisisRentabilidadWindow EN UserControl
                            moduloControl = new RentabilidadModuloControl(_context, _productos, _ventas, _periodoInicio, _periodoFin);
                            break;

                        case "AnalisisABC":
                            // 🎯 CONVERTIR TU AnalisisABCWindow EN UserControl
                            moduloControl = new AnalisisABCModuloControl(_context, _productos, _ventas, _periodoInicio, _periodoFin);
                            break;

                        case "FinancieroAvanzado":
                            // 🎯 CONVERTIR TU AnalisisFinancieroAvanzadoWindow EN UserControl
                            moduloControl = new FinancieroAvanzadoModuloControl(_context);
                            break;

                        case "PuntoEquilibrio":
                            // ✅ MÓDULO DE PUNTO DE EQUILIBRIO COMPLETADO
                            moduloControl = new PuntoEquilibrioModuloControl(_context, _productos, _ventas, _periodoInicio, _periodoFin);
                            break;

                        case "MetricasAvanzadas":
                        case "Tendencias":
                            // 📋 MÓDULOS EN DESARROLLO
                            moduloControl = new ModuloEnDesarrolloControl(nombreModulo);
                            break;

                        default:
                            moduloControl = new ModuloErrorControl(nombreModulo, "Módulo no encontrado");
                            break;
                    }

                    // Agregar al caché
                    if (moduloControl != null)
                    {
                        _modulosCache[nombreModulo] = moduloControl;
                        System.Diagnostics.Debug.WriteLine($"💾 Módulo {nombreModulo} agregado al caché");
                    }
                }

                // 🎯 MOSTRAR EL MÓDULO EN EL CONTENTPRESENTER
                ContentPresenterModulos.Content = moduloControl;

                _moduloActual = nombreModulo;
                TxtStatusAnalisis.Text = $"✅ Módulo {nombreModulo} cargado correctamente";
                ActualizarStatusBar();

                System.Diagnostics.Debug.WriteLine($"📊 Módulo cargado: {nombreModulo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando módulo {nombreModulo}: {ex.Message}");
                TxtStatusAnalisis.Text = $"❌ Error cargando módulo {nombreModulo}";

                // Mostrar módulo de error
                ContentPresenterModulos.Content = new ModuloErrorControl(nombreModulo, ex.Message);
            }
            finally
            {
                BtnActualizarAnalisis.IsEnabled = true;
            }
        }

        #endregion

        // 🎯 MANTENER TODOS TUS OTROS MÉTODOS EXACTAMENTE IGUAL:
        // - CargarCategorias()
        // - CargarDatosAnalisis() 
        // - CmbPeriodo_SelectionChanged()
        // - ActualizarPeriodoFechas()
        // - BtnActualizarAnalisis_Click()
        // - BtnExportarAnalisis_Click()
        // - ActivarBoton()
        // - ActualizarStatusBar()
        // - etc.

        #region Métodos Existentes (MANTENER TODOS IGUALES)
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

                CmbCategoria.Items.Clear();
                CmbCategoria.Items.Add(new ComboBoxItem { Content = "Todas", IsSelected = true });

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

                _productos = await _context.RawMaterials
                    .Where(m => !m.Eliminado)
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                _ventas = await _context.Ventas
                    .Include(v => v.DetallesVenta)
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"📊 DATOS CARGADOS:");
                System.Diagnostics.Debug.WriteLine($"   📦 Productos: {_productos.Count}");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas en período: {_ventas.Count}");

                if (_ventas.Any())
                {
                    var fechaMin = _ventas.Min(v => v.FechaVenta);
                    var fechaMax = _ventas.Max(v => v.FechaVenta);
                    System.Diagnostics.Debug.WriteLine($"   📊 Rango real de ventas: {fechaMin:dd/MM/yyyy} - {fechaMax:dd/MM/yyyy}");
                }

                TxtStatusAnalisis.Text = "✅ Datos cargados - Listo para análisis";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cargar datos de análisis";
                throw;
            }
        }

        // 🔧 AGREGAR ESTOS MÉTODOS AL FINAL DE LA REGIÓN "Métodos Existentes" 
        // EN TU AnalisisMainControl.xaml.cs, ANTES DEL #endregion

        private void ActualizarFechaHora()
        {
            try
            {
                TxtFechaHora.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando fecha/hora: {ex.Message}");
            }
        }

        private void CmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbPeriodo.SelectedItem is ComboBoxItem item)
                {
                    var periodo = item.Content.ToString();
                    ActualizarPeriodoFechas(periodo);

                    // Si hay un módulo cargado, podríamos actualizar los datos
                    if (!string.IsNullOrEmpty(_moduloActual) && _moduloCargado)
                    {
                        _ = CargarDatosAnalisis();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en CmbPeriodo_SelectionChanged: {ex.Message}");
            }
        }

        private void ActualizarPeriodoFechas(string periodo)
        {
            try
            {
                switch (periodo)
                {
                    case "Último mes":
                        _periodoInicio = DateTime.Now.AddMonths(-1);
                        _periodoFin = DateTime.Now;
                        break;
                    case "Últimos 3 meses":
                        _periodoInicio = DateTime.Now.AddMonths(-3);
                        _periodoFin = DateTime.Now;
                        break;
                    case "Último semestre":
                        _periodoInicio = DateTime.Now.AddMonths(-6);
                        _periodoFin = DateTime.Now;
                        break;
                    case "Último año":
                        _periodoInicio = DateTime.Now.AddYears(-1);
                        _periodoFin = DateTime.Now;
                        break;
                    case "Personalizado":
                        // TODO: Abrir diálogo para seleccionar fechas personalizadas
                        MessageBox.Show("🚧 Selección de período personalizado próximamente disponible",
                                      "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"📅 Período actualizado: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando período: {ex.Message}");
            }
        }

        private async void BtnActualizarAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "🔄 Actualizando datos de análisis...";
                BtnActualizarAnalisis.IsEnabled = false;
                BtnActualizarAnalisis.Content = "⏳ Actualizando...";

                // Recargar datos
                await CargarDatosAnalisis();

                // Si hay un módulo cargado, limpiarlo del caché para forzar recarga
                if (!string.IsNullOrEmpty(_moduloActual) && _modulosCache.ContainsKey(_moduloActual))
                {
                    _modulosCache.Remove(_moduloActual);
                    await CargarModulo(_moduloActual);
                }

                TxtStatusAnalisis.Text = "✅ Análisis actualizado correctamente";
                ActualizarStatusBar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando análisis: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al actualizar análisis";
                MessageBox.Show($"Error actualizando análisis:\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("📊 Exportación de Análisis\n\nPróximamente disponible:\n• Reporte PDF completo\n• Excel con datos detallados\n• Gráficos y métricas\n• Comparativas históricas",
                               "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusAnalisis.Text = "📊 Función de exportación disponible próximamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en exportación: {ex.Message}");
            }
        }

        private void ActivarBoton(Button botonActivo)
        {
            try
            {
                // Lista de todos los botones de módulos
                var botones = new[] {
            BtnRentabilidad,
            BtnAnalisisFinanciero,
            BtnPuntoEquilibrio,
            BtnMetricasAvanzadas,
            BtnAnalisisABC,
            BtnComparativasTempo
        };

                // Resetear todos los botones
                foreach (var boton in botones)
                {
                    if (boton != null)
                    {
                        boton.Opacity = 0.7;
                        boton.FontWeight = FontWeights.Normal;
                    }
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
                // Actualizar contador de productos
                TxtProductosAnalisis.Text = $"Productos: {_productos?.Count ?? 0}";

                // Actualizar período actual
                var periodo = "";
                if (CmbPeriodo.SelectedItem is ComboBoxItem item)
                {
                    periodo = item.Content.ToString();
                }
                TxtPeriodoActual.Text = $"Período: {periodo}";

                // Actualizar última actualización
                TxtUltimaActualizacion.Text = $"Actualizado: {DateTime.Now:HH:mm}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando status bar: {ex.Message}");
            }
        }

        #endregion

        #region Limpieza de Recursos
        private void AnalisisMainControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Limpiar caché de módulos
                foreach (var modulo in _modulosCache.Values)
                {
                    if (modulo is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _modulosCache.Clear();

                _context?.Dispose();
                System.Diagnostics.Debug.WriteLine("🧹 AnalisisMainControl: Recursos liberados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error liberando recursos: {ex.Message}");
            }
        }
        #endregion
    }
}