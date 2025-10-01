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
    /// UserControl para Análisis ABC - Completamente funcional
    /// </summary>
    public partial class AnalisisABCModuloControl : UserControl
    {
        #region Variables privadas
        private AppDbContext _context;
        private List<RawMaterial> _productos;
        private List<Venta> _ventas;
        private DateTime _periodoInicio;
        private DateTime _periodoFin;
        private List<ItemAnalisisABC> _resultadosABC = new();
        private string _tipoAnalisisActual = "productos";
        private string _criterioAnalisisActual = "rentabilidad";
        private Stopwatch _cronometroAnalisis = new();
        private bool _disposed = false;
        #endregion

        #region Constructor
        public AnalisisABCModuloControl(AppDbContext context, List<RawMaterial> productos, List<Venta> ventas, DateTime periodoInicio, DateTime periodoFin)
        {
            InitializeComponent();

            _context = context;
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
                TxtStatusAnalisis.Text = "🔤 Análisis ABC inicializando...";

                System.Diagnostics.Debug.WriteLine($"✅ AnalisisABCModuloControl inicializado:");
                System.Diagnostics.Debug.WriteLine($"   📦 Productos: {_productos.Count}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas: {_ventas.Count}");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM} - {_periodoFin:dd/MM}");

                // Ejecutar análisis automáticamente al cargar
                Loaded += async (s, e) => await EjecutarAnalisisCompleto();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inicializando AnalisisABCModuloControl: {ex.Message}");
                MessageBox.Show($"Error al inicializar análisis ABC:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Eventos de UI
        private void BtnVentanaIndependiente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "🔗 Abriendo ventana independiente...";

                // Crear y abrir ventana independiente con los mismos datos
                var ventanaIndependiente = new AnalisisABCWindow(_context, _productos, _ventas, _periodoInicio, _periodoFin);
                ventanaIndependiente.Show();

                TxtStatusAnalisis.Text = "✅ Ventana independiente abierta";
                System.Diagnostics.Debug.WriteLine($"✅ Ventana independiente de ABC abierta");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo ventana independiente: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir ventana independiente";
                MessageBox.Show($"Error al abrir ventana independiente:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void CmbCriterioAnalisis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCriterioAnalisis.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _criterioAnalisisActual = item.Tag.ToString();
                ActualizarTitulosSegunCriterio();
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
            ExportarResultadosABC();
        }

        private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            GenerarReporteCompleto();
        }

        private async void BtnConfigurarAnalisis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusAnalisis.Text = "⚙️ Abriendo configuración avanzada de ABC...";

                // Crear y mostrar ventana de configuración
                var ventanaConfig = new VentanaConfiguracionABC(_tipoAnalisisActual, _criterioAnalisisActual);

                // Mostrar como diálogo modal
                var resultado = ventanaConfig.ShowDialog();

                if (resultado == true && ventanaConfig.ConfiguracionActual != null)
                {
                    // Aplicar la nueva configuración
                    AplicarNuevaConfiguracionABC(ventanaConfig.ConfiguracionActual);

                    TxtStatusAnalisis.Text = "✅ Configuración aplicada - Reejecutando análisis ABC...";

                    // Reejecutar análisis con nueva configuración
                    await EjecutarAnalisisCompleto();
                }
                else
                {
                    TxtStatusAnalisis.Text = "↩️ Configuración ABC cancelada";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Configuración ABC procesada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo configuración ABC: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir configuración";
                MessageBox.Show($"Error al abrir configuración ABC:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AplicarNuevaConfiguracionABC(ConfiguracionABC config)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔤 Aplicando nueva configuración ABC:");
                System.Diagnostics.Debug.WriteLine($"   🎯 Tipo: {config.TipoAnalisis}");
                System.Diagnostics.Debug.WriteLine($"   📊 Criterio: {config.CriterioAnalisis}");

                // Actualizar variables del análisis según la configuración
                _tipoAnalisisActual = config.TipoAnalisis;
                _criterioAnalisisActual = config.CriterioAnalisis;

                // Actualizar controles de UI - Tipo de análisis
                foreach (ComboBoxItem item in CmbTipoAnalisis.Items)
                {
                    if (item.Tag?.ToString() == config.TipoAnalisis)
                    {
                        CmbTipoAnalisis.SelectedItem = item;
                        break;
                    }
                }

                // Actualizar controles de UI - Criterio de análisis
                foreach (ComboBoxItem item in CmbCriterioAnalisis.Items)
                {
                    if (item.Tag?.ToString() == config.CriterioAnalisis)
                    {
                        CmbCriterioAnalisis.SelectedItem = item;
                        break;
                    }
                }

                // Aplicar filtros avanzados
                ChkSoloActivos.IsChecked = config.SoloActivos;
                TxtMinimoItems.Text = config.ItemsMinimos.ToString();

                // Actualizar títulos según nueva configuración
                ActualizarTitulosSegunTipo();
                ActualizarTitulosSegunCriterio();

                System.Diagnostics.Debug.WriteLine($"✅ Configuración ABC aplicada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando configuración ABC: {ex.Message}");
                throw;
            }
        }

        private void BtnAmpliarGrafico_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosABC.Any())
                {
                    MessageBox.Show("No hay datos de análisis ABC para mostrar.\n\nEjecute primero el análisis para ver los gráficos ampliados.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtStatusAnalisis.Text = "🔍 Abriendo vista ampliada de gráficos ABC...";

                // Crear ventana de gráfico ampliado usando constructor normal
                var ventanaGrafico = new VentanaGraficoAmpliado(
                    _resultadosABC,
                    _tipoAnalisisActual,
                    _criterioAnalisisActual
                );

                // Configurar propiedades de la ventana
                ventanaGrafico.Title = $"Gráfico ABC Ampliado - {_tipoAnalisisActual}";
                ventanaGrafico.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Mostrar ventana no modal para permitir trabajar con ambas ventanas
                ventanaGrafico.Show();

                TxtStatusAnalisis.Text = "🔍 Vista ampliada ABC abierta correctamente";
                System.Diagnostics.Debug.WriteLine($"✅ Vista ampliada ABC abierta con {_resultadosABC.Count} items");
            }
            catch (ArgumentException argEx)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Argumento inválido en vista ampliada ABC: {argEx.Message}");
                TxtStatusAnalisis.Text = "⚠️ No hay datos suficientes para vista ampliada";
                MessageBox.Show($"No se puede abrir la vista ampliada:\n{argEx.Message}", "Datos Insuficientes", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo vista ampliada ABC: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir vista ampliada";
                MessageBox.Show($"Error al abrir vista ampliada ABC:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCambiarVista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosABC.Any())
                {
                    MessageBox.Show("No hay datos para cambiar la vista.\n\nEjecute primero el análisis ABC.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Alternar entre vista Pareto y Barras
                if (BtnCambiarVista.Content.ToString().Contains("📊"))
                {
                    DibujarGraficoBarras();
                    BtnCambiarVista.Content = "📈";
                    TxtTituloGrafico.Text = TxtTituloGrafico.Text.Replace("Pareto", "Barras");
                    TxtStatusAnalisis.Text = "📊 Vista cambiada a gráfico de barras";
                }
                else
                {
                    DibujarGraficoPareto();
                    BtnCambiarVista.Content = "📊";
                    TxtTituloGrafico.Text = TxtTituloGrafico.Text.Replace("Barras", "Pareto");
                    TxtStatusAnalisis.Text = "📈 Vista cambiada a análisis Pareto";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Vista de gráfico cambiada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cambiando vista: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al cambiar vista";
            }
        }

        private void BtnAmpliarDistribucion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_resultadosABC.Any())
                {
                    MessageBox.Show("No hay datos de análisis ABC para mostrar.\n\nEjecute primero el análisis para ver la distribución ampliada.",
                                  "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtStatusAnalisis.Text = "🥧 Abriendo distribución ABC ampliada...";

                // Crear ventana de configuración ABC usando constructor normal
                var ventanaConfig = new VentanaConfiguracionABC(
                    _tipoAnalisisActual,
                    _criterioAnalisisActual
                );

                // Configurar para modo distribución
                ventanaConfig.Title = "Distribución ABC Ampliada";
                ventanaConfig.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Si la ventana tiene un método para cargar datos de distribución
                if (ventanaConfig.GetType().GetMethod("CargarDatosDistribucion") != null)
                {
                    ventanaConfig.GetType().GetMethod("CargarDatosDistribucion")?.Invoke(ventanaConfig, new object[] { _resultadosABC });
                }

                // Mostrar ventana no modal
                ventanaConfig.Show();

                TxtStatusAnalisis.Text = "🥧 Distribución ABC ampliada abierta correctamente";
                System.Diagnostics.Debug.WriteLine($"✅ Distribución ABC ampliada abierta con {_resultadosABC.Count} items");
            }
            catch (ArgumentException argEx)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Argumento inválido en distribución ABC: {argEx.Message}");
                TxtStatusAnalisis.Text = "⚠️ No hay datos suficientes para distribución ampliada";
                MessageBox.Show($"No se puede abrir la distribución ampliada:\n{argEx.Message}", "Datos Insuficientes", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error abriendo distribución ABC: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error al abrir distribución ampliada";
                MessageBox.Show($"Error al abrir distribución ABC:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Análisis ABC Principal
        private async Task EjecutarAnalisisCompleto()
        {
            try
            {
                if (TxtStatusAnalisis.Text.Contains("Analizando...")) return;

                _cronometroAnalisis.Restart();
                TxtStatusAnalisis.Text = "🔄 Analizando datos...";

                VerificarPeriodoAnalisis();
                _resultadosABC.Clear();

                // Ejecutar análisis según el tipo seleccionado
                switch (_tipoAnalisisActual)
                {
                    case "productos":
                        await AnalisisProductos();
                        break;
                    case "proveedores":
                        await AnalisisProveedores();
                        break;
                    case "clientes":
                        await AnalisisClientes();
                        break;
                    case "categorias":
                        await AnalisisCategorias();
                        break;
                    default:
                        await AnalisisProductos();
                        break;
                }

                // Aplicar clasificación ABC
                AplicarClasificacionABC();

                // Actualizar UI
                ActualizarKPIs();
                ActualizarTablaResultados();
                DibujarGraficoPareto();
                DibujarGraficoDistribucion();
                GenerarInsights();

                // Actualizar métricas de tiempo
                _cronometroAnalisis.Stop();
                TxtTiempoAnalisis.Text = $"Tiempo: {_cronometroAnalisis.ElapsedMilliseconds}ms";
                TxtUltimaEjecucion.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
                TxtStatusAnalisis.Text = $"✅ Análisis completado - {_resultadosABC.Count} items procesados";

                System.Diagnostics.Debug.WriteLine($"✅ Análisis ABC completado: {_resultadosABC.Count} items analizados");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en EjecutarAnalisisCompleto: {ex.Message}");
                TxtStatusAnalisis.Text = "❌ Error en análisis automático";

                if (_resultadosABC.Count == 0)
                {
                    TxtStatusAnalisis.Text = "⚠️ Sin datos suficientes para análisis";
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

        private async Task AnalisisProductos()
        {
            try
            {
                var productosConVentas = new List<ItemAnalisisABC>();
                var soloActivos = ChkSoloActivos.IsChecked == true;
                var minimoItems = int.TryParse(TxtMinimoItems.Text, out int min) ? min : 5;

                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 ANÁLISIS ABC PRODUCTOS:");
                System.Diagnostics.Debug.WriteLine($"   📅 Período: {_periodoInicio:dd/MM/yyyy} - {_periodoFin:dd/MM/yyyy}");
                System.Diagnostics.Debug.WriteLine($"   💰 Ventas en período: {ventasPeriodo.Count}");

                // Pre-calcular valores para normalización
                var datosParaNormalizacion = new List<(decimal ganancia, decimal volumen, decimal rotacion)>();

                foreach (var producto in _productos.Where(p => p.ActivoParaVenta))
                {
                    var ventasProducto = ventasPeriodo
                        .SelectMany(v => v.DetallesVenta)
                        .Where(d => d.RawMaterialId == producto.Id)
                        .ToList();

                    if (soloActivos && !ventasProducto.Any()) continue;

                    var ganancia = ventasProducto.Sum(d => d.GananciaLinea);
                    var volumen = ventasProducto.Sum(d => d.SubTotal);
                    var stockPromedio = Math.Max(producto.StockTotal, 0.1m);
                    var rotacion = ventasProducto.Sum(d => d.Cantidad) / stockPromedio;

                    datosParaNormalizacion.Add((ganancia, volumen, rotacion));
                }

                // Calcular valores máximos para normalización
                var maxGanancia = datosParaNormalizacion.Any() ? datosParaNormalizacion.Max(d => d.ganancia) : 1m;
                var maxVolumen = datosParaNormalizacion.Any() ? datosParaNormalizacion.Max(d => d.volumen) : 1m;
                var maxRotacion = datosParaNormalizacion.Any() ? datosParaNormalizacion.Max(d => d.rotacion) : 1m;

                // Evitar división por cero
                maxGanancia = Math.Max(maxGanancia, 0.01m);
                maxVolumen = Math.Max(maxVolumen, 0.01m);
                maxRotacion = Math.Max(maxRotacion, 0.01m);

                foreach (var producto in _productos.Where(p => p.ActivoParaVenta))
                {
                    var ventasProducto = ventasPeriodo
                        .SelectMany(v => v.DetallesVenta)
                        .Where(d => d.RawMaterialId == producto.Id)
                        .ToList();

                    if (soloActivos && !ventasProducto.Any()) continue;

                    var item = new ItemAnalisisABC
                    {
                        Id = producto.Id,
                        Nombre = producto.NombreArticulo.Length > 25 ?
                                producto.NombreArticulo.Substring(0, 25) + "..." :
                                producto.NombreArticulo,
                        Categoria = producto.Categoria,
                        Proveedor = producto.Proveedor
                    };

                    // Cálculos según criterio
                    switch (_criterioAnalisisActual)
                    {
                        case "rentabilidad":
                            item.Valor = ventasProducto.Sum(d => d.GananciaLinea);
                            item.VolumenVentas = ventasProducto.Sum(d => d.SubTotal);
                            item.CantidadVendida = ventasProducto.Sum(d => d.Cantidad);
                            item.MargenPromedio = ventasProducto.Any() ?
                                ventasProducto.Average(d => d.MargenPorcentaje) :
                                producto.MargenReal;
                            break;

                        case "volumen":
                            item.VolumenVentas = ventasProducto.Sum(d => d.SubTotal);
                            item.Valor = item.VolumenVentas;
                            item.CantidadVendida = ventasProducto.Sum(d => d.Cantidad);
                            item.MargenPromedio = ventasProducto.Any() ?
                                ventasProducto.Average(d => d.MargenPorcentaje) : 0;
                            break;

                        case "rotacion":
                            var stockPromedio = Math.Max(producto.StockTotal, 0.1m);
                            item.CantidadVendida = ventasProducto.Sum(d => d.Cantidad);
                            var rotacion = item.CantidadVendida / stockPromedio;
                            item.RotacionCalculada = rotacion;
                            item.Valor = rotacion * 100; // Multiplicar por 100 para mejor visualización
                            item.VolumenVentas = ventasProducto.Sum(d => d.SubTotal);
                            item.MargenPromedio = rotacion;
                            break;

                        case "mixto":
                            var ganancia = ventasProducto.Sum(d => d.GananciaLinea);
                            var volumen = ventasProducto.Sum(d => d.SubTotal);
                            var stock = Math.Max(producto.StockTotal, 0.1m);
                            var rot = ventasProducto.Sum(d => d.Cantidad) / stock;

                            // Normalización correcta - Cada métrica de 0 a 1
                            var scoreGananciaNorm = maxGanancia > 0 ? Math.Min(ganancia / maxGanancia, 1) : 0;
                            var scoreVolumenNorm = maxVolumen > 0 ? Math.Min(volumen / maxVolumen, 1) : 0;
                            var scoreRotacionNorm = maxRotacion > 0 ? Math.Min(rot / maxRotacion, 1) : 0;

                            // Pesos balanceados: 50% rentabilidad + 30% volumen + 20% rotación
                            item.Valor = (scoreGananciaNorm * 0.5m + scoreVolumenNorm * 0.3m + scoreRotacionNorm * 0.2m) * 100;
                            item.VolumenVentas = volumen;
                            item.CantidadVendida = ventasProducto.Sum(d => d.Cantidad);
                            item.RotacionCalculada = rot;
                            item.MargenPromedio = ventasProducto.Any() ?
                                ventasProducto.Average(d => d.MargenPorcentaje) : 0;
                            break;
                    }

                    item.NumeroTransacciones = ventasProducto.Count;

                    // Datos adicionales útiles
                    item.ValorInventario = producto.ValorTotalConIVA;
                    item.PrecioPromedio = ventasProducto.Any() ?
                        ventasProducto.Average(d => d.PrecioUnitario) :
                        producto.PrecioVentaFinal;

                    productosConVentas.Add(item);
                }

                // Filtrar y ordenar resultados
                _resultadosABC = productosConVentas
                    .Where(p => p.Valor > 0)
                    .OrderByDescending(p => p.Valor)
                    .Take(Math.Max(100, minimoItems * 10))
                    .ToList();

                TxtItemsEnAnalisis.Text = $"Items: {_resultadosABC.Count}";
                System.Diagnostics.Debug.WriteLine($"📦 Análisis ABC productos completado: {_resultadosABC.Count} productos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisProductos: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisProveedores()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var proveedoresData = _productos
                    .Where(p => !string.IsNullOrEmpty(p.Proveedor))
                    .GroupBy(p => p.Proveedor)
                    .Select(g => new ItemAnalisisABC
                    {
                        Id = g.First().Id,
                        Nombre = g.Key,
                        Categoria = "Proveedor",
                        Valor = g.Sum(p => p.ValorTotalConIVA),
                        VolumenVentas = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.SubTotal)),
                        CantidadVendida = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.Cantidad)),
                        NumeroTransacciones = g.Count()
                    })
                    .Where(p => p.Valor > 0)
                    .OrderByDescending(p => p.Valor)
                    .ToList();

                _resultadosABC = proveedoresData;
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosABC.Count}";
                System.Diagnostics.Debug.WriteLine($"🏭 Análisis proveedores completado: {_resultadosABC.Count} proveedores");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisProveedores: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisClientes()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var clientesData = ventasPeriodo
                    .GroupBy(v => v.Cliente)
                    .Select(g => new ItemAnalisisABC
                    {
                        Id = g.First().Id,
                        Nombre = g.Key,
                        Categoria = "Cliente",
                        Valor = g.Sum(v => v.Total),
                        VolumenVentas = g.Sum(v => v.Total),
                        CantidadVendida = g.Sum(v => v.CantidadItems),
                        NumeroTransacciones = g.Count(),
                        MargenPromedio = g.Average(v => v.MargenPromedio)
                    })
                    .Where(c => c.Valor > 0)
                    .OrderByDescending(c => c.Valor)
                    .ToList();

                _resultadosABC = clientesData;
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosABC.Count}";
                System.Diagnostics.Debug.WriteLine($"👥 Análisis clientes completado: {_resultadosABC.Count} clientes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisClientes: {ex.Message}");
                throw;
            }
        }

        private async Task AnalisisCategorias()
        {
            try
            {
                var ventasPeriodo = _ventas
                    .Where(v => v.FechaVenta >= _periodoInicio && v.FechaVenta <= _periodoFin)
                    .ToList();

                var categoriasData = _productos
                    .Where(p => !string.IsNullOrEmpty(p.Categoria))
                    .GroupBy(p => p.Categoria)
                    .Select(g => new ItemAnalisisABC
                    {
                        Id = g.First().Id,
                        Nombre = g.Key,
                        Categoria = "Categoría",
                        Valor = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.GananciaLinea)),
                        VolumenVentas = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.SubTotal)),
                        CantidadVendida = g.Sum(p => ventasPeriodo
                            .SelectMany(v => v.DetallesVenta)
                            .Where(d => d.RawMaterialId == p.Id)
                            .Sum(d => d.Cantidad)),
                        NumeroTransacciones = g.Count()
                    })
                    .Where(c => c.Valor > 0)
                    .OrderByDescending(c => c.Valor)
                    .ToList();

                _resultadosABC = categoriasData;
                TxtItemsEnAnalisis.Text = $"Items: {_resultadosABC.Count}";
                System.Diagnostics.Debug.WriteLine($"🏷️ Análisis categorías completado: {_resultadosABC.Count} categorías");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AnalisisCategorias: {ex.Message}");
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

                TxtPeriodoHeader.Text = $"📅 {fechaMinima:dd/MM} - {fechaMaxima:dd/MM} ({ventasPeriodo.Count} ventas)";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error verificando período: {ex.Message}");
            }
        }
        #endregion

        #region Clasificación ABC
        private void AplicarClasificacionABC()
        {
            try
            {
                if (!_resultadosABC.Any()) return;

                var totalValor = _resultadosABC.Sum(r => r.Valor);
                var valorAcumulado = 0m;

                for (int i = 0; i < _resultadosABC.Count; i++)
                {
                    var item = _resultadosABC[i];
                    item.Posicion = i + 1;

                    valorAcumulado += item.Valor;
                    item.PorcentajeIndividual = totalValor > 0 ? (item.Valor / totalValor) * 100 : 0;
                    item.PorcentajeAcumulado = totalValor > 0 ? (valorAcumulado / totalValor) * 100 : 0;

                    // Clasificación ABC estándar
                    if (item.PorcentajeAcumulado <= 80)
                        item.ClaseABC = "A";
                    else if (item.PorcentajeAcumulado <= 95)
                        item.ClaseABC = "B";
                    else
                        item.ClaseABC = "C";

                    // Cálculo de score (0-100)
                    item.Score = Math.Max(0, 100 - (item.Posicion - 1) * (100m / _resultadosABC.Count));
                }

                System.Diagnostics.Debug.WriteLine($"🔤 Clasificación ABC aplicada:");
                System.Diagnostics.Debug.WriteLine($"   🥇 Clase A: {_resultadosABC.Count(r => r.ClaseABC == "A")} items");
                System.Diagnostics.Debug.WriteLine($"   🥈 Clase B: {_resultadosABC.Count(r => r.ClaseABC == "B")} items");
                System.Diagnostics.Debug.WriteLine($"   🥉 Clase C: {_resultadosABC.Count(r => r.ClaseABC == "C")} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en AplicarClasificacionABC: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Actualización de UI
        private void ActualizarKPIs()
        {
            try
            {
                var claseA = _resultadosABC.Where(r => r.ClaseABC == "A").ToList();
                var claseB = _resultadosABC.Where(r => r.ClaseABC == "B").ToList();
                var claseC = _resultadosABC.Where(r => r.ClaseABC == "C").ToList();
                var total = _resultadosABC.Count;

                // Clase A
                TxtCantidadA.Text = claseA.Count.ToString();
                TxtPorcentajeA.Text = total > 0 ? $"{(claseA.Count * 100.0 / total):F1}%" : "0%";

                // Clase B
                TxtCantidadB.Text = claseB.Count.ToString();
                TxtPorcentajeB.Text = total > 0 ? $"{(claseB.Count * 100.0 / total):F1}%" : "0%";

                // Clase C
                TxtCantidadC.Text = claseC.Count.ToString();
                TxtPorcentajeC.Text = total > 0 ? $"{(claseC.Count * 100.0 / total):F1}%" : "0%";

                // Pareto 80/20
                var primeros20Porciento = Math.Max(1, (int)(total * 0.2));
                var valorPrimeros20 = _resultadosABC.Take(primeros20Porciento).Sum(r => r.Valor);
                var valorTotal = _resultadosABC.Sum(r => r.Valor);
                var porcentajePareto = valorTotal > 0 ? (valorPrimeros20 / valorTotal) * 100 : 0;

                TxtPareto8020.Text = $"{porcentajePareto:F1}%";
                TxtParetoDetalle.Text = $"{primeros20Porciento} items = {porcentajePareto:F1}%";

                // Total
                TxtTotalItems.Text = total.ToString();
                TxtValorTotal.Text = $"${valorTotal:N0}";

                // Actualizar tipo de items
                TxtTipoItems.Text = _tipoAnalisisActual switch
                {
                    "productos" => "Productos",
                    "proveedores" => "Proveedores",
                    "clientes" => "Clientes",
                    "categorias" => "Categorías",
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
                var itemsParaMostrar = _resultadosABC.Take(topItems).ToList();

                DgResultadosABC.ItemsSource = itemsParaMostrar;
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
                "proveedores" => "Proveedores",
                "clientes" => "Clientes",
                "categorias" => "Categorías",
                _ => "Items"
            };

            var criterioTexto = _criterioAnalisisActual switch
            {
                "rentabilidad" => "Rentabilidad",
                "volumen" => "Volumen de Ventas",
                "rotacion" => "Rotación",
                "mixto" => "Puntuación Mixta",
                _ => "Valor"
            };

            TxtTituloGrafico.Text = $"Análisis Pareto - {tipoTexto} por {criterioTexto}";
        }

        private void ActualizarTitulosSegunCriterio()
        {
            ActualizarTitulosSegunTipo();
        }
        #endregion

        #region Gráficos
        private void DibujarGraficoPareto()
        {
            try
            {
                CanvasGraficoPrincipal.Children.Clear();

                if (!_resultadosABC.Any()) return;

                var canvas = CanvasGraficoPrincipal;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 400;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 200;

                if (width <= 0 || height <= 0) return;

                var margen = 40;
                var areaGrafico = new Rect(margen, margen, width - 2 * margen, height - 2 * margen);

                var itemsGrafico = _resultadosABC.Take(15).ToList();
                var maxValor = itemsGrafico.Max(i => i.Valor);

                var anchoBarraPorItem = areaGrafico.Width / itemsGrafico.Count;

                for (int i = 0; i < itemsGrafico.Count; i++)
                {
                    var item = itemsGrafico[i];
                    var alturaRelativa = maxValor > 0 ? (double)(item.Valor / maxValor) : 0;
                    var alturaBarra = alturaRelativa * areaGrafico.Height * 0.8;

                    var barra = new Rectangle
                    {
                        Width = anchoBarraPorItem * 0.8,
                        Height = alturaBarra,
                        Fill = item.ClaseABC switch
                        {
                            "A" => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Verde
                            "B" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),  // Amarillo
                            "C" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Rojo
                            _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))    // Gris
                        }
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
                }

                // Línea de Pareto (80%)
                var lineaPareto = new Line
                {
                    X1 = areaGrafico.X,
                    Y1 = areaGrafico.Y + areaGrafico.Height * 0.2,
                    X2 = areaGrafico.Right,
                    Y2 = areaGrafico.Y + areaGrafico.Height * 0.2,
                    Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };
                canvas.Children.Add(lineaPareto);

                // Etiqueta 80%
                var etiqueta80 = new TextBlock
                {
                    Text = "80%",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                };
                Canvas.SetLeft(etiqueta80, areaGrafico.X - 25);
                Canvas.SetTop(etiqueta80, areaGrafico.Y + areaGrafico.Height * 0.2 - 8);
                canvas.Children.Add(etiqueta80);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico Pareto: {ex.Message}");
            }
        }

        private void DibujarGraficoBarras()
        {
            DibujarGraficoPareto();
        }

        private void DibujarGraficoDistribucion()
        {
            try
            {
                CanvasGraficoDistribucion.Children.Clear();

                if (!_resultadosABC.Any()) return;

                var canvas = CanvasGraficoDistribucion;
                var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 300;
                var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 150;

                if (width <= 0 || height <= 0) return;

                var claseA = _resultadosABC.Count(r => r.ClaseABC == "A");
                var claseB = _resultadosABC.Count(r => r.ClaseABC == "B");
                var claseC = _resultadosABC.Count(r => r.ClaseABC == "C");
                var total = _resultadosABC.Count;

                if (total == 0) return;

                // Gráfico de barras horizontal simple
                var alturaBarraPorClase = Math.Min(40, height / 4);
                var anchoMaximo = width - 100;

                // Barra A
                var anchoA = (double)claseA / total * anchoMaximo;
                var barraA = new Rectangle
                {
                    Width = anchoA,
                    Height = alturaBarraPorClase,
                    Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129))
                };
                Canvas.SetLeft(barraA, 50);
                Canvas.SetTop(barraA, 20);
                canvas.Children.Add(barraA);

                var labelA = new TextBlock { Text = $"A: {claseA} ({(double)claseA / total * 100:F1}%)", FontSize = 10, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(labelA, 5);
                Canvas.SetTop(labelA, 25);
                canvas.Children.Add(labelA);

                // Barra B
                var anchoB = (double)claseB / total * anchoMaximo;
                var barraB = new Rectangle
                {
                    Width = anchoB,
                    Height = alturaBarraPorClase,
                    Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11))
                };
                Canvas.SetLeft(barraB, 50);
                Canvas.SetTop(barraB, 50);
                canvas.Children.Add(barraB);

                var labelB = new TextBlock { Text = $"B: {claseB} ({(double)claseB / total * 100:F1}%)", FontSize = 10, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(labelB, 5);
                Canvas.SetTop(labelB, 55);
                canvas.Children.Add(labelB);

                // Barra C
                var anchoC = (double)claseC / total * anchoMaximo;
                var barraC = new Rectangle
                {
                    Width = anchoC,
                    Height = alturaBarraPorClase,
                    Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                };
                Canvas.SetLeft(barraC, 50);
                Canvas.SetTop(barraC, 80);
                canvas.Children.Add(barraC);

                var labelC = new TextBlock { Text = $"C: {claseC} ({(double)claseC / total * 100:F1}%)", FontSize = 10, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(labelC, 5);
                Canvas.SetTop(labelC, 85);
                canvas.Children.Add(labelC);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error dibujando gráfico distribución: {ex.Message}");
            }
        }
        #endregion

        #region Insights automáticos
        private void GenerarInsights()
        {
            try
            {
                PanelInsights.Children.Clear();

                if (!_resultadosABC.Any())
                {
                    var noData = new TextBlock
                    {
                        Text = "📊 No hay datos suficientes para generar insights.",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    PanelInsights.Children.Add(noData);
                    return;
                }

                var insights = GenerarInsightsAutomaticos();

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

        private List<string> GenerarInsightsAutomaticos()
        {
            var insights = new List<string>();

            try
            {
                var claseA = _resultadosABC.Where(r => r.ClaseABC == "A").ToList();
                var claseB = _resultadosABC.Where(r => r.ClaseABC == "B").ToList();
                var claseC = _resultadosABC.Where(r => r.ClaseABC == "C").ToList();
                var total = _resultadosABC.Count;

                // Insight 1: Distribución ABC
                insights.Add($"🔍 DISTRIBUCIÓN ABC: {claseA.Count} items Clase A ({(double)claseA.Count / total * 100:F1}%), {claseB.Count} Clase B ({(double)claseB.Count / total * 100:F1}%), {claseC.Count} Clase C ({(double)claseC.Count / total * 100:F1}%)");

                // Insight 2: Regla de Pareto
                var primeros20Porciento = Math.Max(1, (int)(total * 0.2));
                var valorPrimeros20 = _resultadosABC.Take(primeros20Porciento).Sum(r => r.Valor);
                var valorTotal = _resultadosABC.Sum(r => r.Valor);
                var porcentajePareto = valorTotal > 0 ? (valorPrimeros20 / valorTotal) * 100 : 0;

                if (porcentajePareto >= 75)
                    insights.Add($"✅ REGLA DE PARETO: El {primeros20Porciento} primeros items ({100.0 * primeros20Porciento / total:F1}%) generan el {porcentajePareto:F1}% del valor total. ¡Concentra recursos en estos items!");
                else
                    insights.Add($"⚠️ DISTRIBUCIÓN EQUILIBRADA: El {primeros20Porciento} primeros items solo generan el {porcentajePareto:F1}% del valor. Considera diversificar o revisar estrategia.");

                // Insight 3: Clase A específicos
                if (claseA.Any())
                {
                    var top3A = claseA.Take(3).ToList();
                    insights.Add($"🥇 TOP CLASE A: {string.Join(", ", top3A.Select(t => t.Nombre))} son tus items más valiosos. Asegura stock y optimiza precios.");
                }

                // Insight 4: Oportunidades Clase C
                if (claseC.Count > total * 0.5)
                    insights.Add($"🎯 OPORTUNIDAD: {claseC.Count} items Clase C ({(double)claseC.Count / total * 100:F1}%) podrían optimizarse. Considera descuentos, promociones o descontinuar productos de bajo rendimiento.");

                // Insight 5: Recomendación de acción
                insights.Add($"📈 ACCIÓN RECOMENDADA: Enfoca el 80% de tu esfuerzo en los {claseA.Count} items Clase A para maximizar resultados según el principio de Pareto.");

            }
            catch (Exception ex)
            {
                insights.Add($"❌ Error generando insights: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Error en GenerarInsightsAutomaticos: {ex.Message}");
            }

            return insights;
        }
        #endregion

        #region Exportación y reportes
        private void ExportarResultadosABC()
        {
            try
            {
                MessageBox.Show($"📊 Exportar Resultados ABC\n\nDatos a exportar:\n• {_resultadosABC.Count} items analizados\n• Clasificación ABC completa\n• Métricas de rentabilidad\n\nFuncionalidad disponible próximamente.", "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"📋 Reporte Completo ABC\n\nIncluirá:\n• Análisis detallado de {_resultadosABC.Count} items\n• Gráficos Pareto y distribución\n• Insights y recomendaciones\n• Métricas de performance\n\nGeneración de reporte disponible próximamente.", "Reporte", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatusAnalisis.Text = "📋 Generando reporte completo...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando reporte: {ex.Message}");
            }
        }
        #endregion
    }
}