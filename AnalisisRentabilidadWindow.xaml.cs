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
    /// Ventana especializada para Análisis de Rentabilidad - Completamente funcional
    /// </summary>
    public partial class AnalisisRentabilidadWindow : Window
    {
        #region Variables privadas
        private readonly AppDbContext _context;
        private List<RawMaterial> _productos;
        private List<Venta> _ventas;
        private DateTime _periodoInicio;
        private DateTime _periodoFin;
        private List<ItemAnalisisRentabilidad> _resultadosRentabilidad = new();
        private string _tipoAnalisisActual = "productos";
        private string _metricaAnalisisActual = "margen_bruto";
        private Stopwatch _cronometroAnalisis = new();
        #endregion

        #region Constructor
        public AnalisisRentabilidadWindow(AppDbContext context, List<RawMaterial> productos, List<Venta> ventas, DateTime periodoInicio, DateTime periodoFin)
        {
            InitializeComponent();

            _context = context;
            _productos = productos ?? new List<RawMaterial>();
            _ventas = ventas ?? new List<Venta>();
            _periodoInicio = periodoInicio;
            _periodoFin = periodoFin;

            InicializarVentana();
        }
        #endregion

        #region Inicialización
        private void InicializarVentana()
        {
            try
            {
                // Configurar información del header
                TxtPeriodoHeader.Text = $"📅 {_periodoInicio:dd/MM} - {_periodoFin:dd/MM}";
                TxtFechaAnalisis.Text = $"🕒 {DateTime.Now:HH:mm:ss}";

                // Estado inicial
                TxtStatusAnalisis.Text = "💰 Análisis de Rentabilidad inicializando...";

                System.Diagnostics.Debug.WriteLine($"✅ AnalisisRentabilidadWindow inicializada:");
                System.Diagnostics.Debug.WriteLine($"   📦 Productos: {_productos.Count}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas: {_ventas.Count}");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM} - {_periodoFin:dd/MM}");

                // Ejecutar análisis automáticamente al cargar
                Loaded += async (s, e) => await EjecutarAnalisisCompleto();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando AnalisisRentabilidadWindow: {ex.Message}");
                MessageBox.Show($"Error al inicializar análisis de rentabilidad:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Eventos de UI
        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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

        private async void CmbMetricaAnalisis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbMetricaAnalisis.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _metricaAnalisisActual = item.Tag.ToString();
                ActualizarTitulosSegunMetrica();
                await EjecutarAnalisisCompleto();
            }
        }

        private async void BtnActualizarDatos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnActualizarDatos.IsEnabled = false;
                BtnActualizarDatos.Content = "⏳ Actualizando...";

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
                BtnActualizarDatos.Content = "🔄 Actualizar";
            }
        }

        private void BtnExportarResultados_Click(object sender, RoutedEventArgs e)
        {
            ExportarResultadosRentabilidad();
        }

        private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            GenerarReporteCompleto();
        }

        private async void BtnConfiguracionAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "⚙️ Abriendo configuración avanzada de rentabilidad...";

                // Crear y mostrar ventana de configuración
                var ventanaConfig = new VentanaConfiguracionRentabilidad(_tipoAnalisisActual, _metricaAnalisisActual);

                // Mostrar como diálogo modal
                var resultado = ventanaConfig.ShowDialog();

                if (resultado == true && ventanaConfig.ConfiguracionActual != null)
                {
                    // Aplicar la nueva configuración
                    AplicarNuevaConfiguracionRentabilidad(ventanaConfig.ConfiguracionActual);

                    TxtStatusAnalisis.Text = "✅ Configuración aplicada - Reejecutando análisis...";

                    // Reejecutar análisis con nueva configuración
                    await EjecutarAnalisisCompleto();
                }
                else
                {
                    TxtStatusAnalisis.Text = "↩️ Configuración cancelada";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Configuración de rentabilidad procesada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo configuración rentabilidad: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir configuración";
                MessageBox.Show($"Error al abrir configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnAmpliarGrafico_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosRentabilidad.Any())
                {
                    MessageBox.Show("No hay datos de análisis de rentabilidad para mostrar.\n\nEjecute primero el análisis para ver los gráficos ampliados.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtStatusAnalisis.Text = "🔍 Abriendo vista ampliada de gráficos de rentabilidad...";

                // Crear ventana de gráfico ampliado usando el método factory
                var ventanaGrafico = VentanaGraficoAmpliadoRentabilidad.CrearVentana(
                    _resultadosRentabilidad,
                    _tipoAnalisisActual,
                    _metricaAnalisisActual
                );

                // Mostrar ventana no modal para permitir trabajar con ambas ventanas
                ventanaGrafico.Show();

                TxtStatusAnalisis.Text = "🔍 Vista ampliada de rentabilidad abierta correctamente";
                System.Diagnostics.Debug.WriteLine($"✅ Vista ampliada de rentabilidad abierta con {_resultadosRentabilidad.Count} items");
            }
            catch (ArgumentException argEx)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Argumento inválido en vista ampliada: {argEx.Message}");
                TxtStatusAnalisis.Text = "⚠️ No hay datos suficientes para vista ampliada";
                MessageBox.Show($"No se puede abrir la vista ampliada:\n{argEx.Message}", "Datos Insuficientes", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo vista ampliada rentabilidad: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir vista ampliada";
                MessageBox.Show($"Error al abrir vista ampliada:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AplicarNuevaConfiguracionRentabilidad(ConfiguracionRentabilidad config)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"💰 Aplicando nueva configuración de rentabilidad:");
                System.Diagnostics.Debug.WriteLine($"   🎯 Tipo: {config.TipoAnalisis}");
                System.Diagnostics.Debug.WriteLine($"   📊 Métrica: {config.MetricaAnalisis}");

                // Actualizar variables del análisis según la configuración
                _tipoAnalisisActual = config.TipoAnalisis;
                _metricaAnalisisActual = config.MetricaAnalisis;

                // Actualizar controles de UI - Tipo de análisis
                foreach (ComboBoxItem item in CmbTipoAnalisis.Items)
                {
                    if (item.Tag?.ToString() == config.TipoAnalisis)
                    {
                        CmbTipoAnalisis.SelectedItem = item;
                        break;
                    }
                }

                // Actualizar controles de UI - Métrica de análisis
                foreach (ComboBoxItem item in CmbMetricaAnalisis.Items)
                {
                    if (item.Tag?.ToString() == config.MetricaAnalisis)
                    {
                        CmbMetricaAnalisis.SelectedItem = item;
                        break;
                    }
                }

                // Aplicar filtros avanzados
                ChkSoloActivos.IsChecked = config.SoloActivos;
                TxtMinimoItems.Text = config.ItemsMinimos.ToString();

                // Actualizar títulos según nueva configuración
                ActualizarTitulosSegunTipo();
                ActualizarTitulosSegunMetrica();

                System.Diagnostics.Debug.WriteLine($"✅ Configuración de rentabilidad aplicada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando configuración rentabilidad: {ex.Message}");
                throw;
            }
        }

        #endregion

        private void BtnCambiarVista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosRentabilidad.Any())
                {
                    MessageBox.Show("No hay datos para cambiar la vista.\n\nEjecute primero el análisis de rentabilidad.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Alternar entre vista Barras y Líneas
                if (BtnCambiarVista.Content.ToString().Contains("📊"))
                {
                    DibujarGraficoLineas();
                    BtnCambiarVista.Content = "📈";
                    TxtTituloGrafico.Text = TxtTituloGrafico.Text.Replace("Barras", "Líneas");
                    TxtStatusAnalisis.Text = "📈 Vista cambiada a gráfico de líneas";
                }
                else
                {
                    DibujarGraficoBarras();
                    BtnCambiarVista.Content = "📊";
                    TxtTituloGrafico.Text = TxtTituloGrafico.Text.Replace("Líneas", "Barras");
                    TxtStatusAnalisis.Text = "📊 Vista cambiada a gráfico de barras";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Vista de gráfico cambiada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cambiando vista: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cambiar vista";
            }
        }

        private void BtnAmpliarTendencias_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosRentabilidad.Any())
                {
                    MessageBox.Show("No hay datos de análisis de rentabilidad para mostrar.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtStatusAnalisis.Text = "📈 Análisis de tendencias disponible próximamente";
                MessageBox.Show("📈 Análisis de Tendencias Ampliado\n\nFuncionalidad en desarrollo.\n\nMostrará evolución temporal de rentabilidad y proyecciones.",
                              "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo tendencias: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir análisis de tendencias";
            }
        }
      

        #region Análisis Principal
        private async Task EjecutarAnalisisCompleto()
        {
            try
            {
                if (TxtStatusAnalisis.Text.Contains("Analizando...")) return;

                _cronometroAnalisis.Restart();
                TxtStatusAnalisis.Text = "🔄 Analizando rentabilidad...";

                VerificarPeriodoAnalisis();
                _resultadosRentabilidad.Clear();

                // Ejecutar análisis según el tipo seleccionado
                switch (_tipoAnalisisActual)
                {
                    case "productos":
                        await AnalisisProductosRentabilidad();
                        break;
                    case "categorias":
                        await AnalisisCategoriasRentabilidad();
                        break;
                    case "proveedores":
                        await AnalisisProveedoresRentabilidad();
                        break;
                    case "clientes":
                        await AnalisisClientesRentabilidad();
                        break;
                    default:
                        await AnalisisProductosRentabilidad();
                        break;
                }

                // Aplicar ordenamiento según métrica
                AplicarOrdenamientoPorMetrica();

                // Actualizar UI
                ActualizarKPIs();
                ActualizarTablaResultados();
                DibujarGraficoBarras();
                DibujarGraficoCostoBeneficio();
                GenerarInsights();

                // Actualizar métricas de tiempo
                _cronometroAnalisis.Stop();
                TxtTiempoAnalisis.Text = $"Tiempo: {_cronometroAnalisis.ElapsedMilliseconds}ms";
                TxtUltimaEjecucion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
                TxtStatusAnalisis.Text = $"✅ Análisis completado - {_resultadosRentabilidad.Count} items procesados";

                System.Diagnostics.Debug.WriteLine($"✅ Análisis de rentabilidad completado: {_resultadosRentabilidad.Count} items analizados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en EjecutarAnalisisCompleto: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error en análisis de rentabilidad";

                if (_resultadosRentabilidad.Count == 0)
                {
                    TxtStatusAnalisis.Text = "⚠️ Sin datos suficientes para análisis de rentabilidad";
                }
            }
        }

        private async Task RecargarDatosDesdeBaseDatos()
        {
            try
            {
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

        private async Task AnalisisProductosRentabilidad()
        {
            try
            {
                var productosConRentabilidad = new List<ItemAnalisisRentabilidad>();
                var soloActivos = ChkSoloActivos.IsChecked == true;
                var minimoItems = int.TryParse(TxtMinimoItems.Text, out int min) ? min : 5;

                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"💰 ANÁLISIS RENTABILIDAD PRODUCTOS:");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas en período: {ventasPeriodo.Count}");

                foreach (var producto in _productos.Where(p => p.ActivoParaVenta))
                {
                    var ventasProducto = ventasPeriodo
                        .SelectMany(v => v.DetallesVenta)
                        .Where(d => d.RawMaterialId == producto.Id)
                        .ToList();

                    if (soloActivos && !ventasProducto.Any())
                        continue;

                    var item = new ItemAnalisisRentabilidad
                    {
                        Id = producto.Id,
                        Nombre = producto.NombreArticulo.Length > 25 ?
                                producto.NombreArticulo.Substring(0, 25) + "..." :
                                producto.NombreArticulo,
                        Categoria = producto.Categoria,
                        Proveedor = producto.Proveedor
                    };

                    // ✅ CÁLCULOS CORREGIDOS
                    if (ventasProducto.Any())
                    {
                        // Datos base
                        item.TotalVentas = ventasProducto.Sum(d => d.SubTotal);
                        item.TotalCostos = ventasProducto.Sum(d => d.CostoUnitario * d.Cantidad);
                        item.CantidadVendida = ventasProducto.Sum(d => d.Cantidad);
                        item.NumeroTransacciones = ventasProducto.Count;

                        // ✅ GANANCIA BRUTA CORREGIDA - Cálculo manual para consistencia
                        item.GananciaBruta = item.TotalVentas - item.TotalCostos;

                        // ✅ VALIDACIÓN DE CONSISTENCIA con BD
                        var gananciaBD = ventasProducto.Sum(d => d.GananciaLinea);
                        var diferencia = Math.Abs(item.GananciaBruta - gananciaBD);
                        if (diferencia > 0.01m)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Inconsistencia {producto.NombreArticulo}: Calc={item.GananciaBruta:C}, BD={gananciaBD:C}");
                            // Usar el cálculo manual como fuente de verdad
                        }

                        // ✅ ESTIMACIÓN DE COSTOS OPERATIVOS como % de ventas (típicamente 10-15%)
                        var porcentajeCostosOperativos = 0.12m; // 12% estimado
                        item.CostosOperativos = item.TotalVentas * porcentajeCostosOperativos;

                        // ✅ GANANCIA NETA = Ganancia Bruta - Costos Operativos estimados
                        item.GananciaNeta = item.GananciaBruta - item.CostosOperativos;

                        // ✅ MÁRGENES CORREGIDOS
                        item.MargenBruto = item.TotalVentas > 0 ? (item.GananciaBruta / item.TotalVentas) * 100 : 0;
                        item.MargenNeto = item.TotalVentas > 0 ? (item.GananciaNeta / item.TotalVentas) * 100 : 0;

                        // ✅ ROI CORREGIDO (sobre costos totales incluyendo estimación operativa)
                        var costosTotal = item.TotalCostos + item.CostosOperativos;
                        item.ROI = costosTotal > 0 ? (item.GananciaNeta / costosTotal) * 100 : 0;

                        // Precios promedio
                        item.PrecioVentaPromedio = ventasProducto.Average(d => d.PrecioUnitario);
                        item.CostoPromedio = ventasProducto.Average(d => d.CostoUnitario);

                        // ✅ VALIDACIONES DE RANGO
                        if (item.MargenBruto < -200 || item.MargenBruto > 500)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Margen bruto sospechoso {producto.NombreArticulo}: {item.MargenBruto:F1}%");
                        }

                        if (item.MargenNeto < item.MargenBruto - 50) // Margen neto no debería ser 50% menor que bruto
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Margen neto muy bajo {producto.NombreArticulo}: Bruto={item.MargenBruto:F1}%, Neto={item.MargenNeto:F1}%");
                        }

                        // Valor para ordenamiento según métrica
                        item.ValorMetrica = _metricaAnalisisActual switch
                        {
                            "margen_bruto" => item.MargenBruto,
                            "margen_neto" => item.MargenNeto,
                            "roi" => item.ROI,
                            "ganancia_total" => item.GananciaBruta,
                            "costo_beneficio" => item.TotalVentas > 0 ? item.GananciaBruta / item.TotalVentas * 100 : 0,
                            _ => item.MargenBruto
                        };
                    }
                    else
                    {
                        // Producto sin ventas
                        item.TotalCostos = producto.PrecioConIVA;
                        item.ValorMetrica = 0;
                        item.MargenBruto = 0;
                        item.MargenNeto = 0;
                        item.ROI = 0;
                        item.GananciaBruta = 0;
                        item.GananciaNeta = 0;
                        item.CostosOperativos = 0;
                    }

                    productosConRentabilidad.Add(item);
                }

                _resultadosRentabilidad = productosConRentabilidad
                    .Where(p => p.ValorMetrica > 0 || !soloActivos)
                    .OrderByDescending(p => p.ValorMetrica)
                    .Take(Math.Max(100, minimoItems * 10))
                    .ToList();

                TxtItemsEnAnalisis.Text = $"Items: {_resultadosRentabilidad.Count}";
                System.Diagnostics.Debug.WriteLine($"💰 Análisis rentabilidad productos completado: {_resultadosRentabilidad.Count} productos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisProductosRentabilidad: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisCategoriasRentabilidad()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var categoriasData = _productos
                    .Where(p => !string.IsNullOrEmpty(p.Categoria))
                    .GroupBy(p => p.Categoria)
                    .Select(g => new ItemAnalisisRentabilidad
                    {
                        Id = g.First().Id,
                        Nombre = g.Key,
                        Categoria = "Categoría",
                        TotalVentas = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.SubTotal)),
                        TotalCostos = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.CostoUnitario * d.Cantidad)),
                        CantidadVendida = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.Cantidad)),
                        NumeroTransacciones = g.Count()
                    })
                    .Where(c => c.TotalVentas > 0)
                    .ToList();

                // ✅ CALCULAR MÉTRICAS CORREGIDAS
                foreach (var categoria in categoriasData)
                {
                    // ✅ Ganancia bruta calculada manualmente
                    categoria.GananciaBruta = categoria.TotalVentas - categoria.TotalCostos;

                    // ✅ Estimación de costos operativos (12% de ventas)
                    categoria.CostosOperativos = categoria.TotalVentas * 0.12m;

                    // ✅ Ganancia neta
                    categoria.GananciaNeta = categoria.GananciaBruta - categoria.CostosOperativos;

                    // ✅ Márgenes corregidos
                    categoria.MargenBruto = categoria.TotalVentas > 0 ? (categoria.GananciaBruta / categoria.TotalVentas) * 100 : 0;
                    categoria.MargenNeto = categoria.TotalVentas > 0 ? (categoria.GananciaNeta / categoria.TotalVentas) * 100 : 0;

                    // ✅ ROI sobre costos totales
                    var costosTotal = categoria.TotalCostos + categoria.CostosOperativos;
                    categoria.ROI = costosTotal > 0 ? (categoria.GananciaNeta / costosTotal) * 100 : 0;

                    categoria.ValorMetrica = _metricaAnalisisActual switch
                    {
                        "margen_bruto" => categoria.MargenBruto,
                        "margen_neto" => categoria.MargenNeto,
                        "roi" => categoria.ROI,
                        "ganancia_total" => categoria.GananciaBruta,
                        "costo_beneficio" => categoria.TotalVentas > 0 ? categoria.GananciaBruta / categoria.TotalVentas * 100 : 0,
                        _ => categoria.MargenBruto
                    };
                }

                _resultadosRentabilidad = categoriasData.OrderByDescending(c => c.ValorMetrica).ToList();
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosRentabilidad.Count}";
                System.Diagnostics.Debug.WriteLine($"🏷️ Análisis rentabilidad categorías completado: {_resultadosRentabilidad.Count} categorías");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisCategoriasRentabilidad: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisProveedoresRentabilidad()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🏭 ANÁLISIS PROVEEDORES:");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas en período: {ventasPeriodo.Count}");

                var proveedoresData = _productos
                    .Where(p => !string.IsNullOrEmpty(p.Proveedor))
                    .GroupBy(p => p.Proveedor)
                    .Select(g => new ItemAnalisisRentabilidad
                    {
                        Id = g.First().Id,
                        Nombre = g.Key,
                        Categoria = "Proveedor",
                        TotalVentas = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.SubTotal)),
                        TotalCostos = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.CostoUnitario * d.Cantidad)),
                        CantidadVendida = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.Cantidad)),
                        NumeroTransacciones = g.Count()
                    })
                    .Where(p => p.TotalVentas > 0)
                    .ToList();

                // ✅ CALCULAR MÉTRICAS CORREGIDAS
                foreach (var proveedor in proveedoresData)
                {
                    // ✅ Cálculos corregidos
                    proveedor.GananciaBruta = proveedor.TotalVentas - proveedor.TotalCostos;
                    proveedor.CostosOperativos = proveedor.TotalVentas * 0.12m;
                    proveedor.GananciaNeta = proveedor.GananciaBruta - proveedor.CostosOperativos;

                    proveedor.MargenBruto = proveedor.TotalVentas > 0 ? (proveedor.GananciaBruta / proveedor.TotalVentas) * 100 : 0;
                    proveedor.MargenNeto = proveedor.TotalVentas > 0 ? (proveedor.GananciaNeta / proveedor.TotalVentas) * 100 : 0;

                    var costosTotal = proveedor.TotalCostos + proveedor.CostosOperativos;
                    proveedor.ROI = costosTotal > 0 ? (proveedor.GananciaNeta / costosTotal) * 100 : 0;

                    proveedor.ValorMetrica = _metricaAnalisisActual switch
                    {
                        "margen_bruto" => proveedor.MargenBruto,
                        "margen_neto" => proveedor.MargenNeto,
                        "roi" => proveedor.ROI,
                        "ganancia_total" => proveedor.GananciaBruta,
                        "costo_beneficio" => proveedor.TotalVentas > 0 ? proveedor.GananciaBruta / proveedor.TotalVentas * 100 : 0,
                        _ => proveedor.MargenBruto
                    };
                }

                _resultadosRentabilidad = proveedoresData.OrderByDescending(p => p.ValorMetrica).ToList();
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosRentabilidad.Count}";
                System.Diagnostics.Debug.WriteLine($"🏭 Análisis proveedores completado: {_resultadosRentabilidad.Count} proveedores");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisProveedoresRentabilidad: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisClientesRentabilidad()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"👥 ANÁLISIS CLIENTES:");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas en período: {ventasPeriodo.Count}");

                var clientesData = ventasPeriodo
                    .GroupBy(v => v.Cliente)
                    .Select(g => new ItemAnalisisRentabilidad
                    {
                        Id = g.First().Id,
                        Nombre = g.Key,
                        Categoria = "Cliente",
                        TotalVentas = g.Sum(v => v.Total),
                        TotalCostos = g.Sum(v => v.CostoTotal),
                        CantidadVendida = g.Sum(v => v.CantidadItems),
                        NumeroTransacciones = g.Count()
                    })
                    .Where(c => c.TotalVentas > 0)
                    .ToList();

                // ✅ CALCULAR MÉTRICAS CORREGIDAS
                foreach (var cliente in clientesData)
                {
                    // ✅ Cálculos corregidos usando datos de la venta
                    cliente.GananciaBruta = cliente.TotalVentas - cliente.TotalCostos;
                    cliente.CostosOperativos = cliente.TotalVentas * 0.12m;
                    cliente.GananciaNeta = cliente.GananciaBruta - cliente.CostosOperativos;

                    cliente.MargenBruto = cliente.TotalVentas > 0 ? (cliente.GananciaBruta / cliente.TotalVentas) * 100 : 0;
                    cliente.MargenNeto = cliente.TotalVentas > 0 ? (cliente.GananciaNeta / cliente.TotalVentas) * 100 : 0;

                    var costosTotal = cliente.TotalCostos + cliente.CostosOperativos;
                    cliente.ROI = costosTotal > 0 ? (cliente.GananciaNeta / costosTotal) * 100 : 0;

                    cliente.ValorMetrica = _metricaAnalisisActual switch
                    {
                        "margen_bruto" => cliente.MargenBruto,
                        "margen_neto" => cliente.MargenNeto,
                        "roi" => cliente.ROI,
                        "ganancia_total" => cliente.GananciaBruta,
                        "costo_beneficio" => cliente.TotalVentas > 0 ? cliente.GananciaBruta / cliente.TotalVentas * 100 : 0,
                        _ => cliente.MargenBruto
                    };
                }

                _resultadosRentabilidad = clientesData.OrderByDescending(c => c.ValorMetrica).ToList();
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosRentabilidad.Count}";
                System.Diagnostics.Debug.WriteLine($"👥 Análisis clientes completado: {_resultadosRentabilidad.Count} clientes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisClientesRentabilidad: {ex.Message}");
                throw;
            }
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

                System.Diagnostics.Debug.WriteLine($"📅 VERIFICACIÓN DE PERÍODO - RENTABILIDAD:");
                System.Diagnostics.Debug.WriteLine($"   🎯 Período solicitado: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas totales en BD: {_ventas.Count}");
                System.Diagnostics.Debug.WriteLine($"   ✅ Ventas en período: {ventasPeriodo.Count}");
                System.Diagnostics.Debug.WriteLine($"   📊 Fecha mín real: {fechaMinima:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   📊 Fecha máx real: {fechaMaxima:dd/MM/yyyy}");

                TxtPeriodoHeader.Text = $"📅 {fechaMinima:dd/MM} - {fechaMaxima:dd/MM} ({ventasPeriodo.Count} ventas)";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error verificando período: {ex.Message}");
            }
        }
        // Reemplaza el método BtnConfiguracionAnalisis_Click en AnalisisRentabilidadWindow.xaml.cs

        #region Integración con Ventanas Auxiliares
       

        // Método auxiliar para aplicar la nueva configuración
        private void AplicarNuevaConfiguracion(ConfiguracionRentabilidad config)
        {
            try
            {
                // Actualizar variables del análisis según la configuración
                _tipoAnalisisActual = config.TipoAnalisis;
                _metricaAnalisisActual = config.MetricaAnalisis;

                // Actualizar controles de UI
                foreach (ComboBoxItem item in CmbTipoAnalisis.Items)
                {
                    if (item.Tag?.ToString() == config.TipoAnalisis)
                    {
                        CmbTipoAnalisis.SelectedItem = item;
                        break;
                    }
                }

                foreach (ComboBoxItem item in CmbMetricaAnalisis.Items)
                {
                    if (item.Tag?.ToString() == config.MetricaAnalisis)
                    {
                        CmbMetricaAnalisis.SelectedItem = item;
                        break;
                    }
                }

                // Aplicar filtros avanzados
                ChkSoloActivos.IsChecked = config.SoloActivos;
                TxtMinimoItems.Text = config.ItemsMinimos.ToString();

                // Aquí puedes agregar más lógica para aplicar umbrales, pesos, etc.
                // según las necesidades específicas de tu análisis

                System.Diagnostics.Debug.WriteLine($"✅ Configuración aplicada: {config.TipoAnalisis} por {config.MetricaAnalisis}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando configuración: {ex.Message}");
                throw;
            }
        }
        private void AplicarOrdenamientoPorMetrica()
        {
            try
            {
                if (!_resultadosRentabilidad.Any()) return;

                // Asignar posiciones
                for (int i = 0; i < _resultadosRentabilidad.Count; i++)
                {
                    _resultadosRentabilidad[i].Posicion = i + 1;
                }

                System.Diagnostics.Debug.WriteLine($"💰 Ordenamiento aplicado por métrica: {_metricaAnalisisActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AplicarOrdenamientoPorMetrica: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Actualización de UI
        private void ActualizarKPIs()
        {
            try
            {
                if (!_resultadosRentabilidad.Any())
                {
                    TxtMargenPromedio.Text = "0%";
                    TxtGananciaTotal.Text = "$0";
                    TxtROI.Text = "0%";
                    TxtTopRentable.Text = "N/A";
                    TxtTotalItems.Text = "0";
                    return;
                }

                // Margen promedio
                var margenPromedio = _resultadosRentabilidad.Average(r => r.MargenBruto);
                TxtMargenPromedio.Text = $"{margenPromedio:F1}%";
                TxtMargenDetalle.Text = $"Bruto promedio";

                // Ganancia total
                var gananciaTotal = _resultadosRentabilidad.Sum(r => r.GananciaBruta);
                TxtGananciaTotal.Text = gananciaTotal >= 1000 ? $"${gananciaTotal / 1000:F1}K" : $"${gananciaTotal:F0}";
                TxtGananciaDetalle.Text = $"En período";

                // ROI promedio
                var roiPromedio = _resultadosRentabilidad.Average(r => r.ROI);
                TxtROI.Text = $"{roiPromedio:F1}%";
                TxtROIDetalle.Text = "Retorno inversión";

                // Más rentable
                var masRentable = _resultadosRentabilidad.First();
                TxtTopRentable.Text = $"{masRentable.ValorMetrica:F1}%";
                TxtTopRentableNombre.Text = masRentable.Nombre.Length > 12 ?
                    masRentable.Nombre.Substring(0, 12) + "..." : masRentable.Nombre;

                // Total items
                TxtTotalItems.Text = _resultadosRentabilidad.Count.ToString();
                var ventaTotal = _resultadosRentabilidad.Sum(r => r.TotalVentas);
                TxtVentaTotal.Text = ventaTotal >= 1000 ? $"${ventaTotal / 1000:F1}K vendidos" : $"${ventaTotal:F0} vendidos";

                // Tipo de items
                TxtTipoItems.Text = _tipoAnalisisActual switch
                {
                    "productos" => "Productos",
                    "categorias" => "Categorías",
                    "proveedores" => "Proveedores",
                    "clientes" => "Clientes",
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
                var topItems = int.TryParse(TxtTopItems.Text, out int top) ? top : 20;
                var itemsParaMostrar = _resultadosRentabilidad.Take(topItems).ToList();

                DgResultadosRentabilidad.ItemsSource = itemsParaMostrar;
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
                "clientes" => "Clientes",
                _ => "Items"
            };

            var metricaTexto = _metricaAnalisisActual switch
            {
                "margen_bruto" => "Margen Bruto",
                "margen_neto" => "Margen Neto",
                "roi" => "ROI",
                "ganancia_total" => "Ganancia Total",
                "costo_beneficio" => "Costo-Beneficio",
                _ => "Rentabilidad"
            };

            TxtTituloGrafico.Text = $"Rentabilidad - {tipoTexto} por {metricaTexto}";
        }

        private void ActualizarTitulosSegunMetrica()
        {
            ActualizarTitulosSegunTipo();
        }
        #endregion

        #region Gráficos
        private void DibujarGraficoBarras()
        {
            try
            {
                CanvasGraficoPrincipal.Children.Clear();

                if (!_resultadosRentabilidad.Any()) return;

                var canvas = CanvasGraficoPrincipal;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 400;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 200;

                if (width <= 0 || height <= 0) return;

                var margen = 40;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _resultadosRentabilidad.Take(12).ToList();
                var maxValor = itemsGrafico.Max(i => i.ValorMetrica);

                var anchoBarraPorItem = areaGrafico.Width / itemsGrafico.Count;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var alturaRelativa = maxValor > 0 ? (double)(item.ValorMetrica / maxValor) : 0;
                    var alturaBarra = alturaRelativa * areaGrafico.Height * 0.8;

                    // Color según rentabilidad
                    Color colorBarra;
                    if (item.ValorMetrica >= 30) colorBarra = Color.FromRgb(16, 185, 129); // Verde - Alta rentabilidad
                    else if (item.ValorMetrica >= 15) colorBarra = Color.FromRgb(245, 158, 11); // Amarillo - Media
                    else if (item.ValorMetrica >= 5) colorBarra = Color.FromRgb(249, 115, 22); // Naranja - Baja
                    else colorBarra = Color.FromRgb(239, 68, 68); // Rojo - Muy baja

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

                    // Valor encima de la barra
                    var valorTexto = new TextBlock
                    {
                        Text = $"{item.ValorMetrica:F1}%",
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                    };

                    Canvas.SetLeft(valorTexto, areaGrafico.X + i * anchoBarraPorItem + anchoBarraPorItem * 0.3);
                    Canvas.SetTop(valorTexto, areaGrafico.Bottom - alturaBarra - 15);
                    canvas.Children.Add(valorTexto);
                }

                // Línea de referencia (rentabilidad objetivo: 25%)
                var lineaObjetivo = new Line
                {
                    X1 = areaGrafico.X,
                    Y1 = areaGrafico.Bottom - (areaGrafico.Height * 0.8 * 0.25), // 25% como objetivo
                    X2 = areaGrafico.Right,
                    Y2 = areaGrafico.Bottom - (areaGrafico.Height * 0.8 * 0.25),
                    Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };
                canvas.Children.Add(lineaObjetivo);

                // Etiqueta objetivo
                var etiquetaObjetivo = new TextBlock
                {
                    Text = "Objetivo: 25%",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                };
                Canvas.SetLeft(etiquetaObjetivo, areaGrafico.X - 35);
                Canvas.SetTop(etiquetaObjetivo, areaGrafico.Bottom - (areaGrafico.Height * 0.8 * 0.25) - 10);
                canvas.Children.Add(etiquetaObjetivo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico de barras: {ex.Message}");
            }
        }

        private void DibujarGraficoLineas()
        {
            try
            {
                CanvasGraficoPrincipal.Children.Clear();

                if (!_resultadosRentabilidad.Any()) return;

                var canvas = CanvasGraficoPrincipal;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 400;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 200;

                if (width <= 0 || height <= 0) return;

                var margen = 40;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _resultadosRentabilidad.Take(12).ToList();
                var maxValor = itemsGrafico.Max(i => i.ValorMetrica);

                var anchoPorItem = areaGrafico.Width / (itemsGrafico.Count - 1);

                // Dibujar línea de tendencia
                var polyline = new Polyline
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(30, 59, 130, 246))
                };

                var points = new PointCollection();

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var x = areaGrafico.X + i * anchoPorItem;
                    var alturaRelativa = maxValor > 0 ? (double)(item.ValorMetrica / maxValor) : 0;
                    var y = areaGrafico.Bottom - (alturaRelativa * areaGrafico.Height * 0.8);

                    points.Add(new Point(x, y));

                    // Punto en la línea
                    var punto = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                    };

                    Canvas.SetLeft(punto, x - 3);
                    Canvas.SetTop(punto, y - 3);
                    canvas.Children.Add(punto);
                }

                polyline.Points = points;
                canvas.Children.Add(polyline);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico de líneas: {ex.Message}");
            }
        }

        private void DibujarGraficoCostoBeneficio()
        {
            try
            {
                CanvasGraficoTendencias.Children.Clear();

                if (!_resultadosRentabilidad.Any()) return;

                var canvas = CanvasGraficoTendencias;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 300;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 150;

                if (width <= 0 || height <= 0) return;

                var margen = 30;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var top5 = _resultadosRentabilidad.Take(5).ToList();
                var alturaBarraPorItem = areaGrafico.Height / top5.Count;

                for (int i = 0; i < top5.Count; i++)
                {
                    var item = top5[i];

                    // Barra de costo (roja)
                    var costoRelativo = item.TotalCostos > 0 ?
                        Math.Min(item.TotalCostos / top5.Max(t => t.TotalCostos), 1) : 0;
                    var anchoCosto = (double)costoRelativo * areaGrafico.Width * 0.4;

                    var barraCosto = new Rectangle
                    {
                        Width = anchoCosto,
                        Height = alturaBarraPorItem * 0.3,
                        Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                    };

                    Canvas.SetLeft(barraCosto, areaGrafico.X);
                    Canvas.SetTop(barraCosto, areaGrafico.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.1);
                    canvas.Children.Add(barraCosto);

                    // Barra de beneficio (verde)
                    var beneficioRelativo = item.GananciaBruta > 0 ?
                        Math.Min(item.GananciaBruta / top5.Max(t => t.GananciaBruta), 1) : 0;
                    var anchoBeneficio = (double)beneficioRelativo * areaGrafico.Width * 0.4;

                    var barraBeneficio = new Rectangle
                    {
                        Width = anchoBeneficio,
                        Height = alturaBarraPorItem * 0.3,
                        Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    };

                    Canvas.SetLeft(barraBeneficio, areaGrafico.X);
                    Canvas.SetTop(barraBeneficio, areaGrafico.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.5);
                    canvas.Children.Add(barraBeneficio);

                    // Etiqueta del item
                    var etiqueta = new TextBlock
                    {
                        Text = item.Nombre.Length > 12 ? item.Nombre.Substring(0, 12) + "..." : item.Nombre,
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                    };

                    Canvas.SetLeft(etiqueta, areaGrafico.X + areaGrafico.Width * 0.45);
                    Canvas.SetTop(etiqueta, areaGrafico.Y + i * alturaBarraPorItem + alturaBarraPorItem * 0.25);
                    canvas.Children.Add(etiqueta);
                }

                // Leyenda
                var leyendaCosto = new Rectangle
                {
                    Width = 15,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                };
                Canvas.SetLeft(leyendaCosto, areaGrafico.X);
                Canvas.SetTop(leyendaCosto, areaGrafico.Bottom + 10);
                canvas.Children.Add(leyendaCosto);

                var textoLeyendaCosto = new TextBlock
                {
                    Text = "Costo",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                };
                Canvas.SetLeft(textoLeyendaCosto, areaGrafico.X + 20);
                Canvas.SetTop(textoLeyendaCosto, areaGrafico.Bottom + 8);
                canvas.Children.Add(textoLeyendaCosto);

                var leyendaBeneficio = new Rectangle
                {
                    Width = 15,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                };
                Canvas.SetLeft(leyendaBeneficio, areaGrafico.X + 60);
                Canvas.SetTop(leyendaBeneficio, areaGrafico.Bottom + 10);
                canvas.Children.Add(leyendaBeneficio);

                var textoLeyendaBeneficio = new TextBlock
                {
                    Text = "Beneficio",
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81))
                };
                Canvas.SetLeft(textoLeyendaBeneficio, areaGrafico.X + 80);
                Canvas.SetTop(textoLeyendaBeneficio, areaGrafico.Bottom + 8);
                canvas.Children.Add(textoLeyendaBeneficio);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico costo-beneficio: {ex.Message}");
            }
        }
        #endregion

        #region Insights automáticos
        private void GenerarInsights()
        {
            try
            {
                PanelInsights.Children.Clear();

                if (!_resultadosRentabilidad.Any())
                {
                    var noData = new TextBlock
                    {
                        Text = "💰 No hay datos suficientes para generar insights de rentabilidad.",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    PanelInsights.Children.Add(noData);
                    return;
                }

                var insights = GenerarInsightsRentabilidad();

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
        #endregion
        private List<string> GenerarInsightsRentabilidad()
        {
            var insights = new List<string>();

            try
            {
                if (!_resultadosRentabilidad.Any())
                {
                    insights.Add("📊 No hay datos suficientes para generar insights de rentabilidad.");
                    return insights;
                }

                var total = _resultadosRentabilidad.Count;

                // ✅ CLASIFICACIÓN MEJORADA POR MÁRGENES
                var altaRentabilidad = _resultadosRentabilidad.Count(r => r.ValorMetrica >= 30);
                var rentabilidadMedia = _resultadosRentabilidad.Count(r => r.ValorMetrica >= 15 && r.ValorMetrica < 30);
                var bajaRentabilidad = _resultadosRentabilidad.Count(r => r.ValorMetrica >= 5 && r.ValorMetrica < 15);
                var sinRentabilidad = _resultadosRentabilidad.Count(r => r.ValorMetrica < 5);

                // Insight 1: Distribución de rentabilidad MEJORADA
                insights.Add($"📊 DISTRIBUCIÓN DE RENTABILIDAD: {altaRentabilidad} items alta rentabilidad (≥30%), " +
                            $"{rentabilidadMedia} media (15-30%), {bajaRentabilidad} baja (5-15%), {sinRentabilidad} crítica (<5%)");

                // Insight 2: Top performer con detalles
                var topItem = _resultadosRentabilidad.First();
                var topMargenBruto = topItem.MargenBruto;
                var topMargenNeto = topItem.MargenNeto;
                insights.Add($"🏆 TOP PERFORMER: {topItem.Nombre} lidera con {topItem.ValorMetrica:F1}% de rentabilidad " +
                            $"(Margen bruto: {topMargenBruto:F1}%, neto: {topMargenNeto:F1}%), generando ${topItem.GananciaBruta:N0} en ganancias.");

                // ✅ Insight 3: Análisis de márgenes mejorado
                var margenBrutoPromedio = _resultadosRentabilidad.Average(r => r.MargenBruto);
                var margenNetoPromedio = _resultadosRentabilidad.Average(r => r.MargenNeto);
                var diferenciaMargen = margenBrutoPromedio - margenNetoPromedio;

                if (margenBrutoPromedio >= 25)
                    insights.Add($"✅ MARGEN BRUTO SALUDABLE: {margenBrutoPromedio:F1}% promedio está por encima del estándar (20-25%). " +
                                $"Margen neto: {margenNetoPromedio:F1}% (diferencia de {diferenciaMargen:F1}% por costos operativos).");
                else if (margenBrutoPromedio >= 15)
                    insights.Add($"⚠️ MARGEN MODERADO: {margenBrutoPromedio:F1}% bruto es aceptable, pero neto de {margenNetoPromedio:F1}% " +
                                $"muestra impacto de {diferenciaMargen:F1}% por costos operativos. Oportunidad de optimización.");
                else
                    insights.Add($"🚨 MARGEN CRÍTICO: {margenBrutoPromedio:F1}% bruto y {margenNetoPromedio:F1}% neto están por debajo del mínimo. " +
                                $"Costos operativos consumen {diferenciaMargen:F1}% adicional. Revisión urgente necesaria.");

                // ✅ Insight 4: ROI analysis MEJORADO
                var roiPromedio = _resultadosRentabilidad.Average(r => r.ROI);
                var itemsROIPositivo = _resultadosRentabilidad.Count(r => r.ROI > 0);
                var itemsROINegativo = total - itemsROIPositivo;

                if (roiPromedio >= 50)
                    insights.Add($"🚀 ROI EXCELENTE: {roiPromedio:F1}% promedio indica inversión muy eficiente. " +
                                $"{itemsROIPositivo}/{total} items con ROI positivo. Cada peso genera ${(roiPromedio / 100):F2} adicionales.");
                else if (roiPromedio >= 25)
                    insights.Add($"📈 ROI BUENO: {roiPromedio:F1}% promedio es sólido con {itemsROIPositivo}/{total} items rentables. " +
                                $"{itemsROINegativo} items requieren atención.");
                else if (roiPromedio > 0)
                    insights.Add($"📉 ROI MEJORABLE: {roiPromedio:F1}% promedio sugiere optimizar mix de productos. " +
                                $"Solo {itemsROIPositivo}/{total} items son verdaderamente rentables.");
                else
                    insights.Add($"🚨 ROI NEGATIVO: {roiPromedio:F1}% indica pérdidas. {itemsROINegativo}/{total} items destruyen valor. " +
                                $"Revisión inmediata de precios y costos requerida.");

                // ✅ Insight 5: Análisis de costos operativos
                var itemsConCostosOp = _resultadosRentabilidad.Where(r => r.CostosOperativos > 0).ToList();
                if (itemsConCostosOp.Any())
                {
                    var impactoCostosOp = itemsConCostosOp.Average(r => r.MargenBruto - r.MargenNeto);
                    insights.Add($"💼 IMPACTO OPERATIVO: Los costos operativos reducen el margen promedio en {impactoCostosOp:F1}%. " +
                                $"Evalúa eficiencia operativa para mejorar rentabilidad neta.");
                }

                // Insight 6: Oportunidades específicas por tipo MEJORADO
                switch (_tipoAnalisisActual)
                {
                    case "productos":
                        var productosProblematicos = _resultadosRentabilidad.Where(r => r.ValorMetrica < 10).ToList();
                        var productosEstrella = _resultadosRentabilidad.Where(r => r.ValorMetrica >= 40).ToList();

                        if (productosProblematicos.Any())
                            insights.Add($"🎯 ACCIÓN PRODUCTOS: {productosProblematicos.Count} productos con rentabilidad crítica (<10%). " +
                                        $"Considera descontinuar, repricing o promociones. {productosEstrella.Count} productos estrella (≥40%) para potenciar.");
                        break;

                    case "categorias":
                        var categoriasTop = _resultadosRentabilidad.Take(3).ToList();
                        var totalVentasCategorias = categoriasTop.Sum(c => c.TotalVentas);
                        insights.Add($"🏷️ CATEGORÍAS TOP: {string.Join(", ", categoriasTop.Select(c => c.Nombre))} " +
                                    $"generan ${totalVentasCategorias:N0} en ventas. Enfoca inventario y marketing aquí.");
                        break;

                    case "proveedores":
                        var proveedorProblematico = _resultadosRentabilidad.LastOrDefault();
                        if (proveedorProblematico != null)
                            insights.Add($"🏭 PROVEEDORES: Negocia mejores términos con {proveedorProblematico.Nombre} " +
                                        $"(rentabilidad {proveedorProblematico.ValorMetrica:F1}%) o busca alternativas.");
                        break;

                    case "clientes":
                        var clienteValioso = _resultadosRentabilidad.FirstOrDefault();
                        if (clienteValioso != null)
                            insights.Add($"👥 CLIENTES: {clienteValioso.Nombre} es tu cliente más valioso " +
                                        $"({clienteValioso.ValorMetrica:F1}% rentabilidad). Desarrolla programa VIP y estrategias de retención.");
                        break;
                }

                // ✅ Insight 7: Recomendación de acción ESPECÍFICA
                var porcentajeCriticos = (double)sinRentabilidad / total * 100;
                if (porcentajeCriticos > 30)
                    insights.Add($"🚨 ACCIÓN URGENTE: {porcentajeCriticos:F0}% de items tienen rentabilidad crítica. " +
                                $"Plan 30 días: 1) Revisar precios de {sinRentabilidad} items, 2) Reducir costos operativos, 3) Descontinuar no viables.");
                else if (porcentajeCriticos > 15)
                    insights.Add($"⚠️ OPTIMIZACIÓN GRADUAL: {porcentajeCriticos:F0}% de items necesitan mejora. " +
                                $"Prioriza los {altaRentabilidad} items estrella y mejora gradualmente el resto.");
                else
                    insights.Add($"💡 MANTENER RUMBO: Solo {porcentajeCriticos:F0}% items críticos. " +
                                $"Concentra esfuerzos en potenciar los {altaRentabilidad} items más rentables para maximizar crecimiento.");

            }
            catch (Exception ex)
            {
                insights.Add($"❌ Error generando insights: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Error en GenerarInsightsRentabilidad: {ex.Message}");
            }

            return insights;
        }
        #endregion

        #region Exportación y reportes
        private void ExportarResultadosRentabilidad()
        {
            try
            {
                MessageBox.Show($"📊 Exportar Resultados de Rentabilidad\n\nDatos a exportar:\n• {_resultadosRentabilidad.Count} items analizados\n• Márgenes brutos y netos\n• ROI y ganancias por item\n• Análisis costo-beneficio\n\nFuncionalidad disponible próximamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"📋 Reporte Completo de Rentabilidad\n\nIncluirá:\n• Análisis detallado de {_resultadosRentabilidad.Count} items\n• Gráficos de márgenes y ROI\n• Comparativa costo vs beneficio\n• Insights y recomendaciones estratégicas\n• Proyecciones de optimización\n\nGeneración de reporte disponible próximamente.", "Reporte", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusAnalisis.Text = "📋 Generando reporte de rentabilidad...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando reporte: {ex.Message}");
            }
        }
        #endregion
    }

    #region Clase auxiliar para análisis de rentabilidad
    public class ItemAnalisisRentabilidad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Proveedor { get; set; } = "";

        // Métricas financieras
        public decimal TotalVentas { get; set; }
        public decimal TotalCostos { get; set; }
        public decimal GananciaBruta { get; set; }
        public decimal GananciaNeta { get; set; }

        // ✅ ESTIMACIÓN DE COSTOS OPERATIVOS (como % de ventas)
        public decimal CostosOperativos { get; set; }

        // Métricas de rentabilidad
        public decimal MargenBruto { get; set; }
        public decimal MargenNeto { get; set; }
        public decimal ROI { get; set; }
        public decimal ValorMetrica { get; set; }

        // Datos adicionales
        public decimal CantidadVendida { get; set; }
        public int NumeroTransacciones { get; set; }
        public decimal PrecioVentaPromedio { get; set; }
        public decimal CostoPromedio { get; set; }

        // Para tabla
        public int Posicion { get; set; }

        // ✅ PROPIEDADES FORMATEADAS MEJORADAS
        public string MargenFormateado => $"{MargenBruto:F1}%";
        public string MargenNetoFormateado => $"{MargenNeto:F1}%";
        public string GananciaFormateada => GananciaBruta >= 1000 ? $"${GananciaBruta / 1000:F1}K" : $"${GananciaBruta:F0}";
        public string GananciaNetaFormateada => GananciaNeta >= 1000 ? $"${GananciaNeta / 1000:F1}K" : $"${GananciaNeta:F0}";
        public string ROIFormateado => $"{ROI:F1}%";
        public string VentasFormateadas => TotalVentas >= 1000 ? $"${TotalVentas / 1000:F1}K" : $"${TotalVentas:F0}";
        public string CostosFormateados => TotalCostos >= 1000 ? $"${TotalCostos / 1000:F1}K" : $"${TotalCostos:F0}";
        public string CostosOperativosFormateados => CostosOperativos >= 1000 ? $"${CostosOperativos / 1000:F1}K" : $"${CostosOperativos:F0}";
    }
    #endregion
}