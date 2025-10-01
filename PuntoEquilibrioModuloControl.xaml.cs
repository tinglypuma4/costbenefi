using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using costbenefi.Data;
using costbenefi.Models;
using Microsoft.EntityFrameworkCore;

namespace costbenefi.Views
{
    /// <summary>
    /// UserControl para Análisis de Punto de Equilibrio - Completamente funcional
    /// </summary>
    public partial class PuntoEquilibrioModuloControl : UserControl
    {
        #region Variables privadas
        private AppDbContext _context;
        private List<RawMaterial> _productos;
        private List<Venta> _ventas;
        private DateTime _periodoInicio;
        private DateTime _periodoFin;
        private List<ItemPuntoEquilibrio> _resultadosPuntoEquilibrio = new();
        private string _tipoAnalisisActual = "productos";
        private string _periodoAnalisisActual = "mensual";
        private Stopwatch _cronometroAnalisis = new();
        private bool _disposed = false;
        #endregion

        #region Constructor
        public PuntoEquilibrioModuloControl(AppDbContext contextReferencia, List<RawMaterial> productos, List<Venta> ventas, DateTime periodoInicio, DateTime periodoFin)
        {
            InitializeComponent();

            // Crear nuestro propio contexto en lugar de usar el compartido
            _context = new AppDbContext();
            _productos = productos ?? new List<RawMaterial>();
            _ventas = ventas ?? new List<Venta>();
            _periodoInicio = periodoInicio;
            _periodoFin = periodoFin;

            InicializarControl();
        }
        #endregion

        #region Inicialización
        private void InicializarControl()
        {
            try
            {
                // Configurar información del header
                TxtPeriodoHeader.Text = $"📅 {_periodoInicio:dd/MM} - {_periodoFin:dd/MM}";
                TxtFechaAnalisis.Text = $"🕒 {DateTime.Now:HH:mm:ss}";

                // Estado inicial
                TxtStatusAnalisis.Text = "⚖️ Análisis de Punto de Equilibrio inicializando...";

                System.Diagnostics.Debug.WriteLine($"✅ PuntoEquilibrioModuloControl inicializado:");
                System.Diagnostics.Debug.WriteLine($"   📦 Productos: {_productos.Count}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas: {_ventas.Count}");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM} - {_periodoFin:dd/MM}");

                // Agregar evento de limpieza
                Unloaded += PuntoEquilibrioModuloControl_Unloaded;

                // Ejecutar análisis automáticamente al cargar
                Loaded += async (s, e) => await EjecutarAnalisisCompleto();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando PuntoEquilibrioModuloControl: {ex.Message}");
                MessageBox.Show($"Error al inicializar análisis de punto de equilibrio:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Eventos de UI
        private void BtnVentanaIndependiente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "🔗 Ventana independiente disponible próximamente...";
                MessageBox.Show("🔗 Ventana Independiente de Punto de Equilibrio\n\nFuncionalidad en desarrollo.\n\nSe abrirá una ventana dedicada con gráficos avanzados y análisis detallado.",
                              "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo ventana independiente: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir ventana independiente";
            }
        }

        private async void CmbTipoAnalisis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTipoAnalisis.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _tipoAnalisisActual = item.Tag.ToString();
                ActualizarTitulosSegunTipo();
                await EjecutarAnalisisCompleto();
            }
        }

        private async void CmbPeriodoAnalisis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPeriodoAnalisis.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _periodoAnalisisActual = item.Tag.ToString();
                ActualizarTitulosSegunPeriodo();
                await EjecutarAnalisisCompleto();
            }
        }

        private async void BtnActualizarDatos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnActualizarDatos.IsEnabled = false;
                BtnActualizarDatos.Content = "⏳";

                TxtStatusAnalisis.Text = "🔄 Recargando datos desde base de datos...";
                await RecargarDatosDesdeBaseDatos();
                await EjecutarAnalisisCompleto();

                TxtStatusAnalisis.Text = "✅ Datos actualizados correctamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando datos: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al actualizar datos";
                MessageBox.Show($"Error al actualizar datos:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnActualizarDatos.IsEnabled = true;
                BtnActualizarDatos.Content = "🔄";
            }
        }

        private void BtnExportarResultados_Click(object sender, RoutedEventArgs e)
        {
            ExportarResultadosPuntoEquilibrio();
        }

        private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            GenerarReporteCompleto();
        }

        private async void BtnConfiguracionAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "⚙️ Configuración disponible próximamente...";
                MessageBox.Show("⚙️ Configuración Avanzada de Punto de Equilibrio\n\nPróximamente disponible:\n• Configuración de costos fijos/variables\n• Múltiples productos simultáneos\n• Análisis de sensibilidad\n• Escenarios personalizados",
                              "Configuración", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo configuración: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir configuración";
            }
        }

        private void BtnAmpliarGrafico_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosPuntoEquilibrio.Any())
                {
                    MessageBox.Show("No hay datos de punto de equilibrio para mostrar.\n\nEjecute primero el análisis para ver los gráficos ampliados.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtStatusAnalisis.Text = "🔍 Abriendo vista ampliada de gráfico de punto de equilibrio...";

                // Crear y mostrar la ventana de gráfico ampliado
                var ventanaAmpliada = VentanaGraficoAmpliadoPuntoEquilibrio.CrearVentana(
                    _resultadosPuntoEquilibrio,
                    _tipoAnalisisActual,
                    _periodoAnalisisActual
                );

                ventanaAmpliada.Show();

                TxtStatusAnalisis.Text = "✅ Vista ampliada de punto de equilibrio abierta";
                System.Diagnostics.Debug.WriteLine($"✅ Ventana gráfico ampliado punto equilibrio abierta: {_resultadosPuntoEquilibrio.Count} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo vista ampliada: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir vista ampliada";
                MessageBox.Show($"Error al abrir vista ampliada de gráficos:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCambiarVista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosPuntoEquilibrio.Any())
                {
                    MessageBox.Show("No hay datos para cambiar la vista.\n\nEjecute primero el análisis de punto de equilibrio.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Alternar entre vista Línea de Equilibrio y Barras
                if (BtnCambiarVista.Content.ToString().Contains("📊"))
                {
                    DibujarGraficoSensibilidad();
                    BtnCambiarVista.Content = "📈";
                    TxtTituloGrafico.Text = TxtTituloGrafico.Text.Replace("Línea", "Sensibilidad");
                    TxtStatusAnalisis.Text = "📊 Vista cambiada a análisis de sensibilidad";
                }
                else
                {
                    DibujarGraficoLineaEquilibrio();
                    BtnCambiarVista.Content = "📊";
                    TxtTituloGrafico.Text = TxtTituloGrafico.Text.Replace("Sensibilidad", "Línea");
                    TxtStatusAnalisis.Text = "📈 Vista cambiada a línea de equilibrio";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Vista de gráfico cambiada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cambiando vista: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cambiar vista";
            }
        }

        private void BtnAmpliarComparativa_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosPuntoEquilibrio.Any())
                {
                    MessageBox.Show("No hay datos de punto de equilibrio para mostrar.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtStatusAnalisis.Text = "📊 Comparativa ampliada disponible próximamente";
                MessageBox.Show("📊 Comparativa de Productos Ampliada\n\nFuncionalidad en desarrollo.\n\nMostrará comparación detallada del punto de equilibrio entre productos.",
                              "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo comparativa: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir comparativa";
            }
        }

        #endregion

        #region Análisis Principal
        private async Task EjecutarAnalisisCompleto()
        {
            try
            {
                if (TxtStatusAnalisis.Text.Contains("Analizando...")) return;

                // Verificar que el contexto esté disponible
                if (_disposed || _context == null)
                {
                    TxtStatusAnalisis.Text = "⚠️ Contexto no disponible - reiniciando...";
                    _context = new AppDbContext();
                    _disposed = false;
                }

                _cronometroAnalisis.Restart();
                TxtStatusAnalisis.Text = "🔄 Calculando puntos de equilibrio...";

                VerificarPeriodoAnalisis();
                _resultadosPuntoEquilibrio.Clear();

                // Ejecutar análisis según el tipo seleccionado
                switch (_tipoAnalisisActual)
                {
                    case "productos":
                        await AnalisisProductosPuntoEquilibrio();
                        break;
                    case "categorias":
                        await AnalisisCategoriasEquilibrio();
                        break;
                    case "proveedores":
                        await AnalisisProveedoresEquilibrio();
                        break;
                    case "negocio_completo":
                        await AnalisisNegocioCompleto();
                        break;
                    default:
                        await AnalisisProductosPuntoEquilibrio();
                        break;
                }

                // Ordenar por punto de equilibrio más favorable
                _resultadosPuntoEquilibrio = _resultadosPuntoEquilibrio
                    .Where(r => r.PuntoEquilibrioUnidades > 0)
                    .OrderBy(r => r.PuntoEquilibrioUnidades)
                    .ToList();

                // Actualizar UI
                ActualizarKPIs();
                ActualizarTablaResultados();
                DibujarGraficoLineaEquilibrio();
                DibujarGraficoComparativaProductos();
                GenerarInsights();

                // Actualizar métricas de tiempo
                _cronometroAnalisis.Stop();
                TxtTiempoAnalisis.Text = $"Tiempo: {_cronometroAnalisis.ElapsedMilliseconds}ms";
                TxtUltimaEjecucion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
                TxtStatusAnalisis.Text = $"✅ Análisis completado - {_resultadosPuntoEquilibrio.Count} items procesados";

                System.Diagnostics.Debug.WriteLine($"✅ Análisis de punto de equilibrio completado: {_resultadosPuntoEquilibrio.Count} items analizados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en EjecutarAnalisisCompleto: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error en análisis de punto de equilibrio";

                if (_resultadosPuntoEquilibrio.Count == 0)
                {
                    TxtStatusAnalisis.Text = "⚠️ Sin datos suficientes para análisis de punto de equilibrio";
                }
            }
        }

        private async Task RecargarDatosDesdeBaseDatos()
        {
            try
            {
                // Verificar que el contexto esté disponible
                if (_disposed || _context == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Contexto no disponible, creando nuevo...");
                    _context = new AppDbContext();
                }

                _productos = await _context.RawMaterials
                    .Where(m => !m.Eliminado && m.ActivoParaVenta)
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                _ventas = await _context.Ventas
                    .Include(v => v.DetallesVenta)
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"🔄 Datos recargados: {_productos.Count} productos, {_ventas.Count} ventas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error recargando datos: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisProductosPuntoEquilibrio()
        {
            try
            {
                var productosConEquilibrio = new List<ItemPuntoEquilibrio>();
                var soloActivos = ChkSoloActivos.IsChecked == true;
                var minimoVentas = int.TryParse(TxtMinimoVentas.Text, out int min) ? min : 10;

                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var diasEnPeriodo = (_periodoFin - _periodoInicio).Days;
                var factorPeriodo = _periodoAnalisisActual switch
                {
                    "semanal" => (decimal)diasEnPeriodo / 7m,
                    "mensual" => (decimal)diasEnPeriodo / 30m,
                    "anual" => (decimal)diasEnPeriodo / 365m,
                    _ => (decimal)diasEnPeriodo / 30m
                };

                System.Diagnostics.Debug.WriteLine($"⚖️ ANÁLISIS PUNTO DE EQUILIBRIO PRODUCTOS:");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   🔢 Factor período ({_periodoAnalisisActual}): {factorPeriodo:F2}");

                foreach (var producto in _productos.Where(p => p.ActivoParaVenta))
                {
                    var ventasProducto = ventasPeriodo
                        .SelectMany(v => v.DetallesVenta)
                        .Where(d => d.RawMaterialId == producto.Id)
                        .ToList();

                    if (soloActivos && ventasProducto.Count < minimoVentas)
                        continue;

                    var item = new ItemPuntoEquilibrio
                    {
                        Id = producto.Id,
                        Nombre = producto.NombreArticulo.Length > 25 ?
                                producto.NombreArticulo.Substring(0, 25) + "..." :
                                producto.NombreArticulo,
                        Categoria = producto.Categoria,
                        Proveedor = producto.Proveedor
                    };

                    // Calcular promedios del período
                    if (ventasProducto.Any())
                    {
                        item.PrecioVentaPromedio = ventasProducto.Average(d => d.PrecioUnitario);
                        item.CostoVariableUnitario = ventasProducto.Average(d => d.CostoUnitario);
                        item.CantidadVendidaPeriodo = ventasProducto.Sum(d => d.Cantidad);
                        item.IngresosTotalesPeriodo = ventasProducto.Sum(d => d.SubTotal);
                    }
                    else
                    {
                        // Producto sin ventas - usar datos del catálogo
                        item.PrecioVentaPromedio = producto.PrecioVentaFinal;
                        item.CostoVariableUnitario = producto.PrecioConIVA;
                        item.CantidadVendidaPeriodo = 0;
                        item.IngresosTotalesPeriodo = 0;
                    }

                    // Cálculo del margen de contribución
                    item.MargenContribucionUnitario = item.PrecioVentaPromedio - item.CostoVariableUnitario;
                    item.MargenContribucionPorcentaje = item.PrecioVentaPromedio > 0 ?
                        (item.MargenContribucionUnitario / item.PrecioVentaPromedio) * 100 : 0;

                    // Estimación de costos fijos (como % de ingresos - típicamente 15-25%)
                    var porcentajeCostosFijos = 0.20m; // 20% estimado
                    item.CostosFijosPeriodo = item.IngresosTotalesPeriodo * porcentajeCostosFijos;

                    // Ajustar costos fijos según período de análisis
                    item.CostosFijosAjustados = _periodoAnalisisActual switch
                    {
                        "semanal" => item.CostosFijosPeriodo / (decimal)factorPeriodo,
                        "mensual" => item.CostosFijosPeriodo / (decimal)factorPeriodo,
                        "anual" => item.CostosFijosPeriodo * 12 / (decimal)factorPeriodo,
                        _ => item.CostosFijosPeriodo / (decimal)factorPeriodo
                    };

                    // Cálculo del punto de equilibrio
                    if (item.MargenContribucionUnitario > 0)
                    {
                        item.PuntoEquilibrioUnidades = item.CostosFijosAjustados / item.MargenContribucionUnitario;
                        item.PuntoEquilibrioIngresos = item.PuntoEquilibrioUnidades * item.PrecioVentaPromedio;

                        // Días para alcanzar equilibrio
                        var ventaPromediaDiaria = item.CantidadVendidaPeriodo / Math.Max(diasEnPeriodo, 1);
                        item.DiasParaEquilibrio = ventaPromediaDiaria > 0 ?
                            (int)(item.PuntoEquilibrioUnidades / ventaPromediaDiaria) : 999;

                        // Estado del producto
                        if (item.CantidadVendidaPeriodo >= item.PuntoEquilibrioUnidades * factorPeriodo)
                            item.EstadoEquilibrio = "✅ Rentable";
                        else if (item.CantidadVendidaPeriodo >= item.PuntoEquilibrioUnidades * factorPeriodo * 0.7m)
                            item.EstadoEquilibrio = "⚠️ Cerca";
                        else
                            item.EstadoEquilibrio = "❌ Déficit";
                    }
                    else
                    {
                        item.PuntoEquilibrioUnidades = 0;
                        item.PuntoEquilibrioIngresos = 0;
                        item.DiasParaEquilibrio = 999;
                        item.EstadoEquilibrio = "❌ Margen Negativo";
                    }

                    productosConEquilibrio.Add(item);
                }

                _resultadosPuntoEquilibrio = productosConEquilibrio
                    .Where(p => p.MargenContribucionUnitario > 0)
                    .ToList();

                TxtItemsEnAnalisis.Text = $"Items: {_resultadosPuntoEquilibrio.Count}";
                System.Diagnostics.Debug.WriteLine($"⚖️ Análisis punto equilibrio productos completado: {_resultadosPuntoEquilibrio.Count} productos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisProductosPuntoEquilibrio: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisCategoriasEquilibrio()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var categoriasData = _productos
                    .Where(p => !string.IsNullOrEmpty(p.Categoria))
                    .GroupBy(p => p.Categoria)
                    .Select(g => CalcularEquilibrioCategoria(g.Key, g.ToList(), ventasPeriodo))
                    .Where(c => c.MargenContribucionUnitario > 0)
                    .ToList();

                _resultadosPuntoEquilibrio = categoriasData;
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosPuntoEquilibrio.Count}";
                System.Diagnostics.Debug.WriteLine($"🏷️ Análisis categorías completado: {_resultadosPuntoEquilibrio.Count} categorías");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisCategoriasEquilibrio: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisProveedoresEquilibrio()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var proveedoresData = _productos
                    .Where(p => !string.IsNullOrEmpty(p.Proveedor))
                    .GroupBy(p => p.Proveedor)
                    .Select(g => CalcularEquilibrioProveedor(g.Key, g.ToList(), ventasPeriodo))
                    .Where(p => p.MargenContribucionUnitario > 0)
                    .ToList();

                _resultadosPuntoEquilibrio = proveedoresData;
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosPuntoEquilibrio.Count}";
                System.Diagnostics.Debug.WriteLine($"🏭 Análisis proveedores completado: {_resultadosPuntoEquilibrio.Count} proveedores");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisProveedoresEquilibrio: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisNegocioCompleto()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var negocioCompleto = new ItemPuntoEquilibrio
                {
                    Id = 0,
                    Nombre = "NEGOCIO COMPLETO",
                    Categoria = "Consolidado",
                    Proveedor = "Todos"
                };

                // Cálculos agregados
                var totalIngresos = ventasPeriodo.Sum(v => v.Total);
                var totalCostosVariables = ventasPeriodo.SelectMany(v => v.DetallesVenta).Sum(d => d.CostoUnitario * d.Cantidad);
                var totalUnidadesVendidas = ventasPeriodo.SelectMany(v => v.DetallesVenta).Sum(d => d.Cantidad);

                negocioCompleto.IngresosTotalesPeriodo = totalIngresos;
                negocioCompleto.CantidadVendidaPeriodo = totalUnidadesVendidas;
                negocioCompleto.PrecioVentaPromedio = totalUnidadesVendidas > 0 ? totalIngresos / totalUnidadesVendidas : 0;
                negocioCompleto.CostoVariableUnitario = totalUnidadesVendidas > 0 ? totalCostosVariables / totalUnidadesVendidas : 0;

                negocioCompleto.MargenContribucionUnitario = negocioCompleto.PrecioVentaPromedio - negocioCompleto.CostoVariableUnitario;
                negocioCompleto.MargenContribucionPorcentaje = negocioCompleto.PrecioVentaPromedio > 0 ?
                    (negocioCompleto.MargenContribucionUnitario / negocioCompleto.PrecioVentaPromedio) * 100 : 0;

                // Estimación de costos fijos del negocio (25% de ingresos)
                negocioCompleto.CostosFijosPeriodo = totalIngresos * 0.25m;
                negocioCompleto.CostosFijosAjustados = negocioCompleto.CostosFijosPeriodo;

                if (negocioCompleto.MargenContribucionUnitario > 0)
                {
                    negocioCompleto.PuntoEquilibrioUnidades = negocioCompleto.CostosFijosAjustados / negocioCompleto.MargenContribucionUnitario;
                    negocioCompleto.PuntoEquilibrioIngresos = negocioCompleto.PuntoEquilibrioUnidades * negocioCompleto.PrecioVentaPromedio;

                    if (totalUnidadesVendidas >= negocioCompleto.PuntoEquilibrioUnidades)
                        negocioCompleto.EstadoEquilibrio = "✅ NEGOCIO RENTABLE";
                    else
                        negocioCompleto.EstadoEquilibrio = "⚠️ POR DEBAJO DEL EQUILIBRIO";
                }

                _resultadosPuntoEquilibrio = new List<ItemPuntoEquilibrio> { negocioCompleto };
                TxtItemsEnAnalisis.Text = $"Items: 1 (Negocio completo)";
                System.Diagnostics.Debug.WriteLine($"🏢 Análisis negocio completo: Punto equilibrio en {negocioCompleto.PuntoEquilibrioUnidades:F0} unidades");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisNegocioCompleto: {ex.Message}");
                throw;
            }
        }

        private ItemPuntoEquilibrio CalcularEquilibrioCategoria(string categoria, List<RawMaterial> productos, List<Venta> ventasPeriodo)
        {
            var item = new ItemPuntoEquilibrio
            {
                Id = productos.First().Id,
                Nombre = categoria,
                Categoria = "Categoría",
                Proveedor = "Múltiples"
            };

            var ventasCategoria = ventasPeriodo
                .SelectMany(v => v.DetallesVenta)
                .Where(d => productos.Any(p => p.Id == d.RawMaterialId))
                .ToList();

            if (ventasCategoria.Any())
            {
                item.IngresosTotalesPeriodo = ventasCategoria.Sum(d => d.SubTotal);
                item.CantidadVendidaPeriodo = ventasCategoria.Sum(d => d.Cantidad);
                item.PrecioVentaPromedio = ventasCategoria.Average(d => d.PrecioUnitario);
                item.CostoVariableUnitario = ventasCategoria.Average(d => d.CostoUnitario);

                item.MargenContribucionUnitario = item.PrecioVentaPromedio - item.CostoVariableUnitario;
                item.MargenContribucionPorcentaje = item.PrecioVentaPromedio > 0 ?
                    (item.MargenContribucionUnitario / item.PrecioVentaPromedio) * 100 : 0;

                item.CostosFijosPeriodo = item.IngresosTotalesPeriodo * 0.20m;
                item.CostosFijosAjustados = item.CostosFijosPeriodo;

                if (item.MargenContribucionUnitario > 0)
                {
                    item.PuntoEquilibrioUnidades = item.CostosFijosAjustados / item.MargenContribucionUnitario;
                    item.PuntoEquilibrioIngresos = item.PuntoEquilibrioUnidades * item.PrecioVentaPromedio;
                    item.EstadoEquilibrio = item.CantidadVendidaPeriodo >= item.PuntoEquilibrioUnidades ? "✅ Rentable" : "⚠️ Déficit";
                }
            }

            return item;
        }

        private ItemPuntoEquilibrio CalcularEquilibrioProveedor(string proveedor, List<RawMaterial> productos, List<Venta> ventasPeriodo)
        {
            var item = new ItemPuntoEquilibrio
            {
                Id = productos.First().Id,
                Nombre = proveedor,
                Categoria = "Proveedor",
                Proveedor = proveedor
            };

            var ventasProveedor = ventasPeriodo
                .SelectMany(v => v.DetallesVenta)
                .Where(d => productos.Any(p => p.Id == d.RawMaterialId))
                .ToList();

            if (ventasProveedor.Any())
            {
                item.IngresosTotalesPeriodo = ventasProveedor.Sum(d => d.SubTotal);
                item.CantidadVendidaPeriodo = ventasProveedor.Sum(d => d.Cantidad);
                item.PrecioVentaPromedio = ventasProveedor.Average(d => d.PrecioUnitario);
                item.CostoVariableUnitario = ventasProveedor.Average(d => d.CostoUnitario);

                item.MargenContribucionUnitario = item.PrecioVentaPromedio - item.CostoVariableUnitario;
                item.MargenContribucionPorcentaje = item.PrecioVentaPromedio > 0 ?
                    (item.MargenContribucionUnitario / item.PrecioVentaPromedio) * 100 : 0;

                item.CostosFijosPeriodo = item.IngresosTotalesPeriodo * 0.18m;
                item.CostosFijosAjustados = item.CostosFijosPeriodo;

                if (item.MargenContribucionUnitario > 0)
                {
                    item.PuntoEquilibrioUnidades = item.CostosFijosAjustados / item.MargenContribucionUnitario;
                    item.PuntoEquilibrioIngresos = item.PuntoEquilibrioUnidades * item.PrecioVentaPromedio;
                    item.EstadoEquilibrio = item.CantidadVendidaPeriodo >= item.PuntoEquilibrioUnidades ? "✅ Rentable" : "⚠️ Déficit";
                }
            }

            return item;
        }

        private void VerificarPeriodoAnalisis()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var fechaMinima = ventasPeriodo.Any() ? ventasPeriodo.Min(v => v.FechaVenta) : _periodoInicio;
                var fechaMaxima = ventasPeriodo.Any() ? ventasPeriodo.Max(v => v.FechaVenta) : _periodoFin;

                TxtPeriodoHeader.Text = $"📅 {fechaMinima:dd/MM} - {fechaMaxima:dd/MM} ({ventasPeriodo.Count} ventas)";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error verificando período: {ex.Message}");
            }
        }
        #endregion

        #region Actualización de UI
        private void ActualizarKPIs()
        {
            try
            {
                if (!_resultadosPuntoEquilibrio.Any())
                {
                    TxtEquilibrioPromedio.Text = "0";
                    TxtProductosRentables.Text = "0";
                    TxtMargenPromedio.Text = "0%";
                    TxtMejorProducto.Text = "N/A";
                    TxtTotalProductos.Text = "0";
                    return;
                }

                // Equilibrio promedio
                var equilibrioPromedio = _resultadosPuntoEquilibrio.Average(r => r.PuntoEquilibrioUnidades);
                TxtEquilibrioPromedio.Text = equilibrioPromedio >= 1000 ? $"{equilibrioPromedio / 1000:F1}K" : $"{equilibrioPromedio:F0}";
                TxtEquilibrioDetalle.Text = $"Unidades/{_periodoAnalisisActual}";

                // Productos rentables
                var rentables = _resultadosPuntoEquilibrio.Count(r => r.EstadoEquilibrio.Contains("✅"));
                TxtProductosRentables.Text = rentables.ToString();
                TxtRentablesDetalle.Text = $"de {_resultadosPuntoEquilibrio.Count} analizados";

                // Margen promedio
                var margenPromedio = _resultadosPuntoEquilibrio.Average(r => r.MargenContribucionPorcentaje);
                TxtMargenPromedio.Text = $"{margenPromedio:F1}%";
                TxtMargenDetalle.Text = "Contribución promedio";

                // Mejor producto (menor punto de equilibrio)
                var mejorProducto = _resultadosPuntoEquilibrio.OrderBy(r => r.PuntoEquilibrioUnidades).First();
                TxtMejorProducto.Text = $"{mejorProducto.PuntoEquilibrioUnidades:F0}";
                TxtMejorProductoNombre.Text = mejorProducto.Nombre.Length > 12 ?
                    mejorProducto.Nombre.Substring(0, 12) + "..." : mejorProducto.Nombre;

                // Total productos
                TxtTotalProductos.Text = _resultadosPuntoEquilibrio.Count.ToString();
                var totalIngresos = _resultadosPuntoEquilibrio.Sum(r => r.IngresosTotalesPeriodo);
                TxtIngresosTotal.Text = totalIngresos >= 1000 ? $"${totalIngresos / 1000:F1}K periodo" : $"${totalIngresos:F0} periodo";

                // Tipo de items
                TxtTipoItems.Text = _tipoAnalisisActual switch
                {
                    "productos" => "Productos",
                    "categorias" => "Categorías",
                    "proveedores" => "Proveedores",
                    "negocio_completo" => "Negocio",
                    _ => "Items"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ActualizarKPIs: {ex.Message}");
            }
        }

        private void ActualizarTablaResultados()
        {
            try
            {
                var topItems = int.TryParse(TxtTopItems.Text, out int top) ? top : 15;
                var itemsParaMostrar = _resultadosPuntoEquilibrio.Take(topItems).ToList();

                DgResultadosEquilibrio.ItemsSource = itemsParaMostrar;
                System.Diagnostics.Debug.WriteLine($"📊 Tabla actualizada con {itemsParaMostrar.Count} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ActualizarTablaResultados: {ex.Message}");
            }
        }

        private void ActualizarTitulosSegunTipo()
        {
            var tipoTexto = _tipoAnalisisActual switch
            {
                "productos" => "Productos",
                "categorias" => "Categorías",
                "proveedores" => "Proveedores",
                "negocio_completo" => "Negocio Completo",
                _ => "Items"
            };

            TxtTituloGrafico.Text = $"Punto de Equilibrio - {tipoTexto} ({_periodoAnalisisActual})";
        }

        private void ActualizarTitulosSegunPeriodo()
        {
            ActualizarTitulosSegunTipo();
        }
        #endregion

        #region Gráficos
        private void DibujarGraficoLineaEquilibrio()
        {
            try
            {
                CanvasGraficoPrincipal.Children.Clear();

                if (!_resultadosPuntoEquilibrio.Any()) return;

                var canvas = CanvasGraficoPrincipal;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 400;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 200;

                if (width <= 0 || height <= 0) return;

                var margen = 40;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _resultadosPuntoEquilibrio.Take(10).ToList();
                var maxEquilibrio = itemsGrafico.Max(i => i.PuntoEquilibrioUnidades);

                var anchoBarraPorItem = areaGrafico.Width / itemsGrafico.Count;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var alturaRelativa = maxEquilibrio > 0 ? (double)(item.PuntoEquilibrioUnidades / maxEquilibrio) : 0;
                    var alturaBarra = alturaRelativa * areaGrafico.Height * 0.8;

                    // Color según estado de equilibrio
                    Color colorBarra;
                    if (item.EstadoEquilibrio.Contains("✅")) colorBarra = Color.FromRgb(16, 185, 129); // Verde - Rentable
                    else if (item.EstadoEquilibrio.Contains("⚠️")) colorBarra = Color.FromRgb(245, 158, 11); // Amarillo - Cerca
                    else colorBarra = Color.FromRgb(239, 68, 68); // Rojo - Déficit

                    var barra = new Rectangle
                    {
                        Width = anchoBarraPorItem * 0.8,
                        Height = alturaBarra,
                        Fill = new SolidColorBrush(colorBarra)
                    };

                    Canvas.SetLeft(barra, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.1);
                    Canvas.SetTop(barra, areaGrafico.Bottom - alturaBarra);
                    canvas.Children.Add(barra);

                    // Etiqueta del item
                    var etiqueta = new TextBlock
                    {
                        Text = item.Nombre.Length > 8 ? item.Nombre.Substring(0, 8) + "..." : item.Nombre,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                        RenderTransform = new RotateTransform(-45)
                    };

                    Canvas.SetLeft(etiqueta, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.5);
                    Canvas.SetTop(etiqueta, areaGrafico.Bottom + 5);
                    canvas.Children.Add(etiqueta);

                    // Valor del punto de equilibrio
                    var valorTexto = new TextBlock
                    {
                        Text = item.PuntoEquilibrioUnidades >= 1000 ?
                            $"{item.PuntoEquilibrioUnidades / 1000:F1}K" :
                            $"{item.PuntoEquilibrioUnidades:F0}",
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                    };

                    Canvas.SetLeft(valorTexto, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.2);
                    Canvas.SetTop(valorTexto, areaGrafico.Bottom - alturaBarra - 15);
                    canvas.Children.Add(valorTexto);
                }

                // Línea de referencia promedio
                var equilibrioPromedio = itemsGrafico.Average(i => i.PuntoEquilibrioUnidades);
                var alturaPromedioRelativa = (double)(equilibrioPromedio / maxEquilibrio);
                var alturaLineaPromedio = areaGrafico.Bottom - (alturaPromedioRelativa * areaGrafico.Height * 0.8);

                var lineaPromedio = new Line
                {
                    X1 = areaGrafico.X,
                    Y1 = alturaLineaPromedio,
                    X2 = areaGrafico.Right,
                    Y2 = alturaLineaPromedio,
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };
                canvas.Children.Add(lineaPromedio);

                // Etiqueta promedio
                var etiquetaPromedio = new TextBlock
                {
                    Text = $"Promedio: {(equilibrioPromedio >= 1000 ? $"{equilibrioPromedio / 1000:F1}K" : $"{equilibrioPromedio:F0}")}",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                };
                Canvas.SetLeft(etiquetaPromedio, areaGrafico.X - 35);
                Canvas.SetTop(etiquetaPromedio, alturaLineaPromedio - 10);
                canvas.Children.Add(etiquetaPromedio);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico línea equilibrio: {ex.Message}");
            }
        }

        private void DibujarGraficoSensibilidad()
        {
            try
            {
                CanvasGraficoPrincipal.Children.Clear();

                if (!_resultadosPuntoEquilibrio.Any()) return;

                var canvas = CanvasGraficoPrincipal;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 400;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 200;

                if (width <= 0 || height <= 0) return;

                var margen = 40;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _resultadosPuntoEquilibrio.Take(8).ToList();

                // Análisis de sensibilidad - variación del precio +/- 20%
                var colores = new[] {
                    Color.FromRgb(239, 68, 68),   // -20% (Rojo)
                    Color.FromRgb(245, 158, 11),  // -10% (Amarillo)
                    Color.FromRgb(16, 185, 129),  // Actual (Verde)
                    Color.FromRgb(59, 130, 246),  // +10% (Azul)
                    Color.FromRgb(139, 92, 246)   // +20% (Púrpura)
                };

                var variaciones = new[] { -0.2m, -0.1m, 0m, 0.1m, 0.2m };
                var anchoGrupoPorItem = areaGrafico.Width / itemsGrafico.Count;
                var anchoBarraPorVariacion = anchoGrupoPorItem * 0.15;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var maxEquilibrioItem = 0m;

                    // Calcular punto de equilibrio para cada variación
                    var equilibrios = new decimal[5];
                    for (int v = 0; v < variaciones.Length; v++)
                    {
                        var precioAjustado = item.PrecioVentaPromedio * (1 + variaciones[v]);
                        var margenAjustado = precioAjustado - item.CostoVariableUnitario;
                        equilibrios[v] = margenAjustado > 0 ? item.CostosFijosAjustados / margenAjustado : 0;
                        maxEquilibrioItem = Math.Max(maxEquilibrioItem, equilibrios[v]);
                    }

                    // Dibujar barras de sensibilidad
                    for (int v = 0; v < variaciones.Length; v++)
                    {
                        if (equilibrios[v] > 0)
                        {
                            var alturaRelativa = maxEquilibrioItem > 0 ? (double)(equilibrios[v] / maxEquilibrioItem) : 0;
                            var alturaBarra = alturaRelativa * areaGrafico.Height * 0.15; // Más pequeñas

                            var barra = new Rectangle
                            {
                                Width = anchoBarraPorVariacion,
                                Height = alturaBarra,
                                Fill = new SolidColorBrush(colores[v])
                            };

                            var xPos = areaGrafico.X + i * anchoGrupoPorItem + v * anchoBarraPorVariacion + anchoGrupoPorItem * 0.1;
                            Canvas.SetLeft(barra, xPos);
                            Canvas.SetTop(barra, areaGrafico.Bottom - alturaBarra);
                            canvas.Children.Add(barra);
                        }
                    }

                    // Etiqueta del producto
                    var etiquetaProducto = new TextBlock
                    {
                        Text = item.Nombre.Length > 6 ? item.Nombre.Substring(0, 6) + "..." : item.Nombre,
                        FontSize = 7,
                        Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)),
                        RenderTransform = new RotateTransform(-45)
                    };

                    Canvas.SetLeft(etiquetaProducto, areaGrafico.X + i * anchoGrupoPorItem + anchoGrupoPorItem * 0.4);
                    Canvas.SetTop(etiquetaProducto, areaGrafico.Bottom + 5);
                    canvas.Children.Add(etiquetaProducto);
                }

                // Leyenda de variaciones
                var leyendaTextos = new[] { "-20%", "-10%", "Actual", "+10%", "+20%" };
                for (int v = 0; v < colores.Length; v++)
                {
                    var rectanguloLeyenda = new Rectangle
                    {
                        Width = 12,
                        Height = 8,
                        Fill = new SolidColorBrush(colores[v])
                    };
                    Canvas.SetLeft(rectanguloLeyenda, areaGrafico.X + v * 50);
                    Canvas.SetTop(rectanguloLeyenda, areaGrafico.Y - 20);
                    canvas.Children.Add(rectanguloLeyenda);

                    var textoLeyenda = new TextBlock
                    {
                        Text = leyendaTextos[v],
                        FontSize = 7,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                    };
                    Canvas.SetLeft(textoLeyenda, areaGrafico.X + v * 50 + 15);
                    Canvas.SetTop(textoLeyenda, areaGrafico.Y - 22);
                    canvas.Children.Add(textoLeyenda);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico sensibilidad: {ex.Message}");
            }
        }

        private void DibujarGraficoComparativaProductos()
        {
            try
            {
                CanvasGraficoComparativa.Children.Clear();

                if (!_resultadosPuntoEquilibrio.Any()) return;

                var canvas = CanvasGraficoComparativa;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 300;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 150;

                if (width <= 0 || height <= 0) return;

                var margen = 30;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var top5 = _resultadosPuntoEquilibrio.Take(5).ToList();
                var alturaBarraPorItem = areaGrafico.Height / top5.Count;

                for (int i = 0; i < top5.Count; i++)
                {
                    var item = top5[i];

                    // Barra de punto de equilibrio
                    var equilibrioRelativo = item.PuntoEquilibrioUnidades > 0 ?
                        Math.Min(item.PuntoEquilibrioUnidades / top5.Max(t => t.PuntoEquilibrioUnidades), 1) : 0;
                    var anchoEquilibrio = (double)equilibrioRelativo * areaGrafico.Width * 0.8;

                    // Color según estado
                    Color colorBarra;
                    if (item.EstadoEquilibrio.Contains("✅")) colorBarra = Color.FromRgb(16, 185, 129);
                    else if (item.EstadoEquilibrio.Contains("⚠️")) colorBarra = Color.FromRgb(245, 158, 11);
                    else colorBarra = Color.FromRgb(239, 68, 68);

                    var barraEquilibrio = new Rectangle
                    {
                        Width = anchoEquilibrio,
                        Height = alturaBarraPorItem * 0.6,
                        Fill = new SolidColorBrush(colorBarra)
                    };

                    Canvas.SetLeft(barraEquilibrio, areaGrafico.X);
                    Canvas.SetTop(barraEquilibrio, areaGrafico.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.2);
                    canvas.Children.Add(barraEquilibrio);

                    // Etiqueta del item y valor
                    var etiqueta = new TextBlock
                    {
                        Text = $"{item.Nombre} ({item.PuntoEquilibrioUnidades:F0})",
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                    };

                    Canvas.SetLeft(etiqueta, areaGrafico.X + areaGrafico.Width * 0.85);
                    Canvas.SetTop(etiqueta, areaGrafico.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.3);
                    canvas.Children.Add(etiqueta);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico comparativa: {ex.Message}");
            }
        }
        #endregion

        #region Insights automáticos
        private void GenerarInsights()
        {
            try
            {
                PanelInsights.Children.Clear();

                if (!_resultadosPuntoEquilibrio.Any())
                {
                    var noData = new TextBlock
                    {
                        Text = "⚖️ No hay datos suficientes para generar insights de punto de equilibrio.",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    PanelInsights.Children.Add(noData);
                    return;
                }

                var insights = GenerarInsightsPuntoEquilibrio();

                foreach (var insight in insights)
                {
                    var textBlock = new TextBlock
                    {
                        Text = insight,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    PanelInsights.Children.Add(textBlock);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando insights: {ex.Message}");
            }
        }

        private List<string> GenerarInsightsPuntoEquilibrio()
        {
            var insights = new List<string>();

            try
            {
                var total = _resultadosPuntoEquilibrio.Count;
                var rentables = _resultadosPuntoEquilibrio.Count(r => r.EstadoEquilibrio.Contains("✅"));
                var cerca = _resultadosPuntoEquilibrio.Count(r => r.EstadoEquilibrio.Contains("⚠️"));
                var deficit = _resultadosPuntoEquilibrio.Count(r => r.EstadoEquilibrio.Contains("❌"));

                // Insight 1: Estado general
                insights.Add($"📊 ESTADO GENERAL: {rentables} productos rentables ({(double)rentables / total * 100:F0}%), " +
                            $"{cerca} cerca del equilibrio, {deficit} en déficit del total de {total} analizados.");

                // Insight 2: Mejor y peor performers
                var mejorProducto = _resultadosPuntoEquilibrio.OrderBy(r => r.PuntoEquilibrioUnidades).First();
                var peorProducto = _resultadosPuntoEquilibrio.OrderByDescending(r => r.PuntoEquilibrioUnidades).First();

                insights.Add($"🏆 MEJOR PRODUCTO: {mejorProducto.Nombre} necesita solo {mejorProducto.PuntoEquilibrioUnidades:F0} unidades para equilibrar. " +
                            $"🚨 DESAFIANTE: {peorProducto.Nombre} requiere {peorProducto.PuntoEquilibrioUnidades:F0} unidades.");

                // Insight 3: Análisis de márgenes
                var margenPromedio = _resultadosPuntoEquilibrio.Average(r => r.MargenContribucionPorcentaje);
                if (margenPromedio >= 40)
                    insights.Add($"✅ MÁRGENES SÓLIDOS: {margenPromedio:F1}% promedio es excelente para alcanzar equilibrios rápidamente.");
                else if (margenPromedio >= 25)
                    insights.Add($"⚠️ MÁRGENES MODERADOS: {margenPromedio:F1}% es aceptable, pero optimizar precios reduciría puntos de equilibrio.");
                else
                    insights.Add($"🚨 MÁRGENES CRÍTICOS: {margenPromedio:F1}% requiere revisión urgente - puntos de equilibrio muy altos.");

                // Insight 4: Recomendación estratégica
                var equilibrioPromedio = _resultadosPuntoEquilibrio.Average(r => r.PuntoEquilibrioUnidades);

                if (rentables >= total * 0.7) // 70% o más rentables
                    insights.Add($"💡 ESTRATEGIA: Portfolio sólido. Enfocar en escalar los {rentables} productos rentables y optimizar los restantes.");
                else if (rentables >= total * 0.4) // 40-70% rentables
                    insights.Add($"⚙️ OPTIMIZACIÓN: Revisar costos fijos y precios de los {deficit + cerca} productos no rentables para mejorar equilibrios.");
                else
                    insights.Add($"🚨 REESTRUCTURA: Solo {(double)rentables / total * 100:F0}% rentables. Considerar descontinuar productos con equilibrios > {equilibrioPromedio * 1.5m:F0} unidades.");

                // Insight 5: Sensibilidad al volumen
                var productosAltoVolumen = _resultadosPuntoEquilibrio.Count(r => r.CantidadVendidaPeriodo >= r.PuntoEquilibrioUnidades);
                insights.Add($"📈 VOLUMEN: {productosAltoVolumen} productos ya superaron su punto de equilibrio en el período analizado. " +
                            $"Priorizar promoción de productos cercanos al equilibrio.");

            }
            catch (Exception ex)
            {
                insights.Add($"❌ Error generando insights: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Error en GenerarInsightsPuntoEquilibrio: {ex.Message}");
            }

            return insights;
        }
        #endregion

        #region Exportación y reportes
        private void ExportarResultadosPuntoEquilibrio()
        {
            try
            {
                MessageBox.Show($"📊 Exportar Resultados de Punto de Equilibrio\n\nDatos a exportar:\n• {_resultadosPuntoEquilibrio.Count} items analizados\n• Puntos de equilibrio por producto\n• Márgenes de contribución\n• Análisis de sensibilidad\n• Estados de rentabilidad\n\nFuncionalidad disponible próximamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusAnalisis.Text = "📊 Preparando exportación...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error exportando resultados: {ex.Message}");
            }
        }

        private void GenerarReporteCompleto()
        {
            try
            {
                MessageBox.Show($"📋 Reporte Completo de Punto de Equilibrio\n\nIncluirá:\n• Análisis detallado de {_resultadosPuntoEquilibrio.Count} items\n• Gráficos de líneas de equilibrio\n• Análisis de sensibilidad de precios\n• Comparativas entre productos\n• Recomendaciones estratégicas\n• Proyecciones de volúmenes\n\nGeneración de reporte disponible próximamente.", "Reporte", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusAnalisis.Text = "📋 Generando reporte de punto de equilibrio...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando reporte: {ex.Message}");
            }
        }
        #endregion

        #region Limpieza de Recursos
        private void PuntoEquilibrioModuloControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_disposed && _context != null)
                {
                    _context.Dispose();
                    _disposed = true;
                    System.Diagnostics.Debug.WriteLine("🧹 PuntoEquilibrioModuloControl: Contexto liberado");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error liberando recursos PuntoEquilibrio: {ex.Message}");
            }
        }
        #endregion
    }
}