using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Views;
using costbenefi.Services;
using System.ComponentModel;
using System.Globalization;
using costbenefi.Managers;
using costbenefi.Managers;
using System.ComponentModel;

namespace costbenefi
{
    public partial class MainWindow : Window
    {
        private AppDbContext _context;
        private List<RawMaterial> _allMaterials = new();
        private List<RawMaterial> _filteredMaterials = new();


        // ========== VARIABLES POS ==========
        private List<RawMaterial> _productosParaVenta = new();
        private List<RawMaterial> _productosParaVentaFiltrados = new();
        private ObservableCollection<DetalleVenta> _carritoItems = new();
        private CorteCajaService _corteCajaService;

        // Servicios POS
        private TicketPrinter _ticketPrinter;
        private BasculaService _basculaService;
        private ScannerPOSService _scannerService;
        private POSIntegrationService _posIntegrationService;

        // Estado POS
        private bool _posLoaded = false;
        private decimal _totalVentasHoy = 0;
        private int _cantidadVentasHoy = 0;

        public MainWindow()
        {
            try
            {
                // Inicializar componentes de UI
                InitializeComponent();

                // Inicializar colecciones básicas
                _allMaterials = new List<RawMaterial>();
                _filteredMaterials = new List<RawMaterial>();
                _productosParaVenta = new List<RawMaterial>();
                _productosParaVentaFiltrados = new List<RawMaterial>();
                _carritoItems = new ObservableCollection<DetalleVenta>();

                // Configurar carrito
                LstCarrito.ItemsSource = _carritoItems;

                // Configurar timer para actualización de fecha/hora
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMinutes(1);
                timer.Tick += (s, e) => UpdateDateTime();
                timer.Start();

                // Configurar carga diferida
                this.Loaded += MainWindow_Loaded;
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ ERROR CRÍTICO en constructor de MainWindow:\n\n" +
                    $"Mensaje: {ex.Message}\n\n" +
                    $"El sistema se cerrará. Por favor reporte este error.",
                    "Error Fatal - MainWindow",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                Application.Current.Shutdown(1);
            }
            this.Loaded += async (s, e) =>
            {
                await Task.Delay(500); // Dar tiempo a que se renderice

                // Forzar actualización completa
                this.UpdateLayout();
                this.InvalidateVisual();

                // Si tienes un TabControl, forzar su actualización
                var tabControl = this.FindName("MainTabControl") as TabControl;
                if (tabControl != null)
                {
                    var currentTab = tabControl.SelectedIndex;
                    tabControl.SelectedIndex = -1;
                    tabControl.UpdateLayout();
                    tabControl.SelectedIndex = currentTab >= 0 ? currentTab : 0;
                }

                System.Diagnostics.Debug.WriteLine("🎉 UI refrescada después de logout");
            };
        }
       
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mostrar mensaje de carga
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "⏳ Inicializando sistema...";
                }

                // Inicializar contexto de base de datos
                _context = new AppDbContext();
                await _context.Database.EnsureCreatedAsync();

                // Cargar datos
                await LoadDataSafe();

                // Inicializar servicios POS
                await Task.Run(() => InitializePOSServicesSafe());

                // Configurar actualización automática
                ConfigurarActualizacionAutomatica();

                // Actualizar status final
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "✅ Sistema listo";
                }
            }
            catch (Exception ex)
            {
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "❌ Error al cargar sistema";
                }

                MessageBox.Show(
                    $"⚠️ Error al cargar algunos componentes:\n\n" +
                    $"{ex.Message}\n\n" +
                    $"El sistema funcionará con funcionalidad limitada.",
                    "Advertencia de Carga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // ========== INICIALIZACIÓN POS ==========
        // ✅ CORREGIR - Inicialización con báscula real
        private void InitializePOSServicesSafe()
        {
            try
            {
                // Inicializar servicios básicos primero
                _ticketPrinter = new TicketPrinter();
                _corteCajaService = new CorteCajaService(_context);
                _scannerService = new ScannerPOSService();

                // Inicializar báscula de forma segura
                try
                {
                    _basculaService = new BasculaService(_context);

                    // Configurar eventos de báscula
                    _basculaService.PesoRecibido += (sender, e) =>
                    {
                        Dispatcher.BeginInvoke(new Action(async () => await OnPesoRecibido(e.Peso)));
                    };

                    _basculaService.ErrorOcurrido += (sender, error) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (TxtStatusPOS != null)
                                TxtStatusPOS.Text = $"❌ Error báscula: {error}";
                        }));
                    };

                    // Intentar conectar báscula (no crítico si falla)
                    try
                    {
                        if (_basculaService.Conectar())
                        {
                            System.Diagnostics.Debug.WriteLine("✅ Báscula conectada");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("⚠️ Báscula no conectada");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error al conectar báscula: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error al inicializar báscula: {ex.Message}");
                    _basculaService = null; // Continuar sin báscula
                }

                System.Diagnostics.Debug.WriteLine("✅ Servicios POS inicializados (con o sin báscula)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error en InitializePOSServicesSafe: {ex.Message}");
                // Continuar sin algunos servicios POS
            }
        }
        private async Task LoadDataSafe()
        {
            try
            {
                // Cargar materiales de forma segura
                _allMaterials = await _context.RawMaterials
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                _filteredMaterials = new List<RawMaterial>(_allMaterials);

                // Actualizar UI en el hilo principal
                Dispatcher.Invoke(() =>
                {
                    UpdateDataGrid();
                    UpdateStatusBar();
                });

                System.Diagnostics.Debug.WriteLine($"✅ Cargados {_allMaterials.Count} materiales");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error al cargar datos: {ex.Message}");

                // Inicializar con listas vacías para evitar errores
                _allMaterials = new List<RawMaterial>();
                _filteredMaterials = new List<RawMaterial>();

                Dispatcher.Invoke(() =>
                {
                    if (TxtStatus != null)
                        TxtStatus.Text = "⚠️ Error al cargar datos - Sistema en modo limitado";
                });
            }
        }
        private void UpdateDateTime()
        {
            // El binding automático debería manejar esto, pero podemos forzar actualización si es necesario
        }

        private async void LoadData()
        {
            try
            {
                TxtStatus.Text = "⏳ Cargando datos...";

                // ✅ El filtro global automáticamente excluye eliminados
                _allMaterials = await _context.RawMaterials
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                _filteredMaterials = new List<RawMaterial>(_allMaterials);

                UpdateDataGrid();
                UpdateStatusBar();

                TxtStatus.Text = "✅ Sistema listo";

                // Mostrar estadísticas de productos eliminados (opcional)
                var totalEliminados = await _context.GetDeletedRawMaterials().CountAsync();
                if (totalEliminados > 0)
                {
                    TxtStatus.Text += $" | {totalEliminados} eliminados";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al cargar datos";
            }
        }

        // ========== CARGA DE DATOS POS ==========
        private async Task LoadDataPuntoVenta()
        {
            try
            {
                TxtStatusPOS.Text = "⏳ Cargando productos para venta...";

                // Cargar productos disponibles para venta
                _productosParaVenta = await _context.GetProductosDisponiblesParaVenta().ToListAsync();
                _productosParaVentaFiltrados = new List<RawMaterial>(_productosParaVenta);

                // Actualizar lista
                LstProductosPOS.ItemsSource = _productosParaVentaFiltrados;

                // Cargar estadísticas del día
                await LoadEstadisticasDelDia();

                // Actualizar contadores
                UpdateContadoresPOS();

                TxtStatusPOS.Text = "✅ Sistema POS listo";
                _posLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos POS: {ex.Message}",
                              "Error POS", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al cargar datos POS";
            }
        }
        private async Task LoadEstadisticasDelDia()
        {
            try
            {
                var hoy = DateTime.Today;
                var ventasHoy = await _context.GetVentasDelDia(hoy).ToListAsync();

                _cantidadVentasHoy = ventasHoy.Count;
                _totalVentasHoy = ventasHoy.Sum(v => v.Total);

                // Actualizar header
                TxtVentasHoy.Text = $"Ventas hoy: {_cantidadVentasHoy}";
                TxtTotalVentasHoy.Text = $"Total: {_totalVentasHoy:C2}";
                await VerificarEstadoCorteCaja();
            }
            catch (Exception ex)
            {
                TxtVentasHoy.Text = "Ventas hoy: Error";
                TxtTotalVentasHoy.Text = "Total: $0.00";
            }
        }

        // <summary>
        /// Verifica si se puede hacer corte de caja hoy
        /// </summary>
        private async Task VerificarEstadoCorteCaja()
        {
            try
            {
                var hoy = DateTime.Today;
                var existeCorte = await _corteCajaService.ExisteCorteDelDiaAsync(hoy);
                var estadisticas = await _corteCajaService.ObtenerEstadisticasDelDiaAsync(hoy);

                // Actualizar interfaz según el estado
                if (existeCorte)
                {
                    var corte = await _corteCajaService.ObtenerCorteDelDiaAsync(hoy);
                    ActualizarEstadoCorteCaja(corte);
                }
                else if (estadisticas.CantidadTickets > 0)
                {
                    // Hay ventas pero no corte - mostrar notificación
                    TxtStatusPOS.Text = $"💰 {estadisticas.CantidadTickets} ventas pendientes de corte";
                }
                else
                {
                    // Sin ventas del día
                    TxtStatusPOS.Text = "📊 Sin ventas registradas hoy";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar estado corte: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza la interfaz según el estado del corte
        /// </summary>
        private void ActualizarEstadoCorteCaja(CorteCaja corte)
        {
            if (corte == null) return;

            switch (corte.Estado)
            {
                case "Completado":
                    TxtStatusPOS.Text = $"✅ Corte completado - {corte.TotalVentasCalculado:C2}";
                    break;
                case "Pendiente":
                    TxtStatusPOS.Text = $"⏳ Corte pendiente - {corte.CantidadTickets} tickets";
                    break;
                case "Cancelado":
                    TxtStatusPOS.Text = $"❌ Corte cancelado - Revisar";
                    break;
            }
        }

        private void UpdateContadoresPOS()
        {
            // Actualizar contador productos
            TxtCountProductos.Text = $"{_productosParaVentaFiltrados.Count} productos";
            TxtProductosDisponibles.Text = $"Productos: {_productosParaVenta.Count}";

            // Actualizar contador carrito
            TxtCountCarrito.Text = $"{_carritoItems.Count} artículos";

            // Calcular totales del carrito
            ActualizarTotalesCarrito();
        }

        private void UpdateDataGrid()
        {
            DgMateriales.ItemsSource = null;
            DgMateriales.ItemsSource = _filteredMaterials;

            // Actualizar contadores en header
            TxtContadorHeader.Text = $"{_filteredMaterials.Count} productos activos";
        }

        /// <summary>
        /// Abre la ventana de corte de caja
        /// </summary>
        private async void BtnCorteCaja_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar si hay ventas del día
                var hoy = DateTime.Today;
                var estadisticas = await _corteCajaService.ObtenerEstadisticasDelDiaAsync(hoy);

                if (estadisticas.CantidadTickets == 0)
                {
                    MessageBox.Show("No hay ventas registradas para hacer el corte de caja de hoy.",
                                  "Sin Ventas", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Verificar si ya existe corte
                var corteExistente = await _corteCajaService.ObtenerCorteDelDiaAsync(hoy);

                Window corteCajaWindow;

                if (corteExistente != null)
                {
                    // Abrir corte existente para edición
                    var mensaje = $"Ya existe un corte para hoy ({corteExistente.Estado}).\n\n" +
                                 $"🎯 Total ventas: {corteExistente.TotalVentasCalculado:C2}\n" +
                                 $"📄 Tickets: {corteExistente.CantidadTickets}\n" +
                                 $"⚖️ Estado: {corteExistente.ObtenerEstadoDescriptivo()}\n\n" +
                                 $"¿Desea abrirlo?";

                    var resultado = MessageBox.Show(mensaje, "Corte Existente",
                                                  MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado != MessageBoxResult.Yes) return;

                    corteCajaWindow = new CorteCajaWindow(_context, corteExistente);
                }
                else
                {
                    // Confirmar creación de nuevo corte
                    var confirmar = MessageBox.Show(
                        $"🎯 CREAR CORTE DE CAJA - {hoy:dd/MM/yyyy}\n\n" +
                        $"📊 Resumen del día:\n" +
                        $"   • Total ventas: {estadisticas.TotalVentas:C2}\n" +
                        $"   • Cantidad tickets: {estadisticas.CantidadTickets}\n" +
                        $"   • Efectivo: {estadisticas.EfectivoTotal:C2}\n" +
                        $"   • Tarjeta: {estadisticas.TarjetaTotal:C2}\n" +
                        $"   • Transferencia: {estadisticas.TransferenciaTotal:C2}\n\n" +
                        $"¿Proceder con el corte de caja?",
                        "Confirmar Corte de Caja", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (confirmar != MessageBoxResult.Yes) return;

                    corteCajaWindow = new CorteCajaWindow(_context, hoy);
                }

                // Configurar ventana
                corteCajaWindow.Owner = this;
                corteCajaWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                // Mostrar ventana
                if (corteCajaWindow.ShowDialog() == true)
                {
                    // Actualizar estadísticas después del corte
                    await LoadEstadisticasDelDia();
                    await VerificarEstadoCorteCaja();

                    TxtStatusPOS.Text = "✅ Corte de caja completado exitosamente";

                    // Mostrar notificación de éxito
                    MessageBox.Show("✅ Corte de caja procesado correctamente!\n\n" +
                                  "El sistema se ha actualizado con la información del corte.",
                                  "Corte Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir corte de caja: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al abrir corte de caja";
            }
        }

        /// <summary>
        /// Abre la ventana de reportes de cortes históricos
        /// </summary>
        private async void BtnReporteCortes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reporteWindow = new ReporteCorteCajaWindow(_context)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                reporteWindow.Show();
                TxtStatusPOS.Text = "📊 Reporte de cortes de caja abierto";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir reporte de cortes: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al abrir reporte de cortes";
            }
        }

        /// <summary>
        /// Muestra estadísticas rápidas de cortes de caja
        /// </summary>
        private async void BtnEstadisticasCortes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var desde = DateTime.Today.AddDays(-30); // Últimos 30 días
                var hasta = DateTime.Today;

                var analisis = await _corteCajaService.ObtenerAnalisisCortesAsync(desde, hasta);

                var mensaje = $"📊 ESTADÍSTICAS DE CORTES (30 días)\n\n" +
                             $"📅 Período: {analisis.Periodo}\n" +
                             $"📊 Cortes realizados: {analisis.CantidadCortes}\n" +
                             $"💰 Total ventas: {analisis.TotalVentas:C2}\n" +
                             $"💵 Total comisiones: {analisis.TotalComisiones:C2}\n" +
                             $"📈 Total ganancias: {analisis.TotalGanancias:C2}\n" +
                             $"📊 Promedio diario: {analisis.PromedioDiario:C2}\n\n" +
                             $"⚖️ CONCILIACIÓN:\n" +
                             $"   • Diferencias detectadas: {analisis.DiferienciasDetectadas}\n" +
                             $"   • Sobrantes total: {analisis.SobrantesTotal:C2}\n" +
                             $"   • Faltantes total: {analisis.FaltantesTotal:C2}\n" +
                             $"   • Exactitud: {analisis.PorcentajeExactitud:F1}%";

                MessageBox.Show(mensaje, "Estadísticas de Cortes",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusPOS.Text = "📊 Estadísticas mostradas";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener estadísticas: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al obtener estadísticas";
            }
        }
        private void UpdateStatusBar()
        {
            if (_filteredMaterials?.Any() == true)
            {
                // Calcular totales de todos los materiales filtrados
                decimal totalConIVA = _filteredMaterials.Sum(m => m.ValorTotalConIVA);
                decimal totalSinIVA = _filteredMaterials.Sum(m => m.ValorTotalSinIVA);
                decimal diferenciaIVA = totalConIVA - totalSinIVA;

                // Actualizar textos del status bar (versión completa)
                TxtTotalConIVA.Text = $"Total con IVA: {totalConIVA:C2}";
                TxtTotalSinIVA.Text = $"Total sin IVA: {totalSinIVA:C2}";
                TxtDiferenciaIVA.Text = $"Diferencia IVA: {diferenciaIVA:C2}";

                // Actualizar versiones compactas en el toolbar
                TxtTotalConIVACompact.Text = $"c/IVA: {FormatCompactCurrency(totalConIVA)}";
                TxtTotalSinIVACompact.Text = $"s/IVA: {FormatCompactCurrency(totalSinIVA)}";
                TxtDiferenciaIVACompact.Text = $"Δ: {FormatCompactCurrency(diferenciaIVA)}";

                // Cambiar colores según el valor
                if (diferenciaIVA > 1000)
                {
                    TxtDiferenciaIVA.Foreground = new SolidColorBrush(
                        Color.FromRgb(220, 53, 69)); // Rojo para valores altos
                }
                else if (diferenciaIVA > 500)
                {
                    TxtDiferenciaIVA.Foreground = new SolidColorBrush(
                        Color.FromRgb(255, 193, 7)); // Amarillo para valores medios
                }
                else
                {
                    TxtDiferenciaIVA.Foreground = new SolidColorBrush(
                        Color.FromRgb(40, 167, 69)); // Verde para valores bajos
                }
            }
            else
            {
                // Reset para valores vacíos
                TxtTotalConIVA.Text = "Total con IVA: $0.00";
                TxtTotalSinIVA.Text = "Total sin IVA: $0.00";
                TxtDiferenciaIVA.Text = "Diferencia IVA: $0.00";

                TxtTotalConIVACompact.Text = "c/IVA: $0";
                TxtTotalSinIVACompact.Text = "s/IVA: $0";
                TxtDiferenciaIVACompact.Text = "Δ: $0";
            }
        }

        private string FormatCompactCurrency(decimal value)
        {
            if (value >= 1000000)
                return $"${value / 1000000:F1}M";
            else if (value >= 1000)
                return $"${value / 1000:F1}K";
            else
                return $"${value:F0}";
        }

        // ========== MÉTODOS POS PRINCIPALES ==========
        private async Task AgregarProductoAlCarrito(RawMaterial producto, decimal cantidad = 1)
        {
            try
            {
                // Verificar stock disponible
                if (cantidad > producto.StockTotal)
                {
                    MessageBox.Show($"Stock insuficiente. Disponible: {producto.StockTotal:F2} {producto.UnidadMedida}",
                                  "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Buscar si ya existe en el carrito
                var itemExistente = _carritoItems.FirstOrDefault(i => i.RawMaterialId == producto.Id);

                if (itemExistente != null)
                {
                    // Actualizar cantidad existente
                    if (itemExistente.Cantidad + cantidad > producto.StockTotal)
                    {
                        MessageBox.Show($"Cantidad total excede el stock. Disponible: {producto.StockTotal:F2} {producto.UnidadMedida}",
                                      "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    itemExistente.ActualizarCantidad(itemExistente.Cantidad + cantidad);
                }
                else
                {
                    // Crear nuevo item del carrito
                    var nuevoItem = new DetalleVenta
                    {
                        RawMaterialId = producto.Id,
                        NombreProducto = producto.NombreArticulo,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.PrecioVentaFinal,
                        UnidadMedida = producto.UnidadMedida,
                        CostoUnitario = producto.PrecioConIVA,
                        PorcentajeIVA = producto.PorcentajeIVA,
                        DescuentoAplicado = producto.TieneDescuentoActivo ? producto.PrecioDescuento : 0
                    };

                    nuevoItem.CalcularSubTotal();
                    _carritoItems.Add(nuevoItem);
                }

                // Actualizar interfaz
                UpdateContadoresPOS();
                TxtStatusPOS.Text = $"✅ Agregado: {producto.NombreArticulo} x{cantidad:F2}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar producto: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarTotalesCarrito()
        {
            try
            {
                if (_carritoItems.Any())
                {
                    // ✅ CORRECCIÓN: Sin IVA adicional, los precios ya incluyen IVA
                    decimal subtotal = _carritoItems.Sum(i => i.SubTotal);
                    decimal descuentoTotal = _carritoItems.Sum(i => i.DescuentoAplicado * i.Cantidad);

                    // ✅ IVA es solo informativo (muestra cuánto IVA está incluido en los precios)
                    // Asumiendo que los precios incluyen 16% de IVA
                    decimal ivaIncluido = subtotal - (subtotal / 1.16m);

                    // ✅ Total = subtotal (sin agregar IVA adicional)
                    decimal total = subtotal;

                    // Actualizar textos
                    TxtSubTotal.Text = subtotal.ToString("C2");
                    TxtDescuento.Text = descuentoTotal.ToString("C2");
                    TxtIVA.Text = $"{ivaIncluido:C2} (incluido)"; // Mostrar como "incluido"
                    TxtTotal.Text = total.ToString("C2");

                    // Calcular análisis costo-beneficio
                    var gananciaTotal = _carritoItems.Sum(i => i.GananciaLinea);
                    var margenPromedio = _carritoItems.Any() ?
                        _carritoItems.Average(i => i.MargenPorcentaje) : 0;

                    TxtGananciaPrevista.Text = $"Ganancia: {gananciaTotal:C2}";
                    TxtMargenPromedio.Text = $"Margen: {margenPromedio:F1}%";
                }
                else
                {
                    // Resetear totales
                    TxtSubTotal.Text = "$0.00";
                    TxtDescuento.Text = "$0.00";
                    TxtIVA.Text = "$0.00 (incluido)";
                    TxtTotal.Text = "$0.00";
                    TxtGananciaPrevista.Text = "Ganancia: $0.00";
                    TxtMargenPromedio.Text = "Margen: 0%";
                }
            }
            catch (Exception ex)
            {
                TxtStatusPOS.Text = "❌ Error al calcular totales";
            }
        }

        // ✅ MÉTODO ÚNICO - PROCESAMIENTO DE VENTA (CORREGIDO)
        private async Task<bool> ProcesarVentaUnico()
        {
            // ✅ USAR CONTEXTO SEPARADO CON TRANSACCIÓN
            using var ventaContext = new AppDbContext();
            using var transaction = await ventaContext.Database.BeginTransactionAsync();

            try
            {
                if (!_carritoItems.Any())
                {
                    MessageBox.Show("El carrito está vacío.", "Carrito Vacío",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                // Calcular total
                decimal subtotal = _carritoItems.Sum(i => i.SubTotal);
                decimal total = subtotal;

                // Abrir ventana de pago
                var pagoWindow = new ProcesarPagoWindow(total, TxtCliente.Text.Trim());
                if (pagoWindow.ShowDialog() != true)
                {
                    return false;
                }

                // ✅ VERIFICAR Y RESERVAR STOCK PRIMERO (sin modificar aún)
                var productosParaActualizar = new List<(RawMaterial producto, decimal cantidad)>();

                foreach (var item in _carritoItems)
                {
                    var producto = await ventaContext.RawMaterials.FindAsync(item.RawMaterialId);
                    if (producto == null)
                    {
                        throw new InvalidOperationException($"Producto no encontrado: {item.NombreProducto}");
                    }

                    if (producto.StockTotal < item.Cantidad)
                    {
                        throw new InvalidOperationException($"Stock insuficiente para {item.NombreProducto}. Disponible: {producto.StockTotal:F2}");
                    }

                    productosParaActualizar.Add((producto, item.Cantidad));
                }

                // ✅ CREAR LA VENTA
                var venta = new Venta
                {
                    Cliente = pagoWindow.NombreCliente,
                    Usuario = UserService.UsuarioActual?.NombreUsuario ?? Environment.UserName,
                    FormaPago = pagoWindow.FormaPagoFinal,
                    Estado = "Completada",
                    Observaciones = pagoWindow.DetallesPago
                };

                // Configurar pagos y comisiones
                venta.EstablecerFormasPago(
                    pagoWindow.MontoEfectivo,
                    pagoWindow.MontoTarjeta,
                    pagoWindow.MontoTransferencia
                );

                if (pagoWindow.ComisionTarjeta > 0)
                {
                    venta.CalcularComisiones(pagoWindow.PorcentajeComisionTarjeta);
                    venta.CalcularIVAComision(pagoWindow.IVAComision > 0);
                }

                // Agregar detalles
                foreach (var item in _carritoItems)
                {
                    var detalleVenta = new DetalleVenta
                    {
                        RawMaterialId = item.RawMaterialId,
                        NombreProducto = item.NombreProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        UnidadMedida = item.UnidadMedida,
                        CostoUnitario = item.CostoUnitario,
                        PorcentajeIVA = item.PorcentajeIVA,
                        DescuentoAplicado = item.DescuentoAplicado
                    };
                    detalleVenta.CalcularSubTotal();
                    venta.AgregarDetalle(detalleVenta);
                }

                venta.CalcularTotales();
                venta.GenerarNumeroTicket();

                // ✅ AGREGAR VENTA PRIMERO
                ventaContext.Ventas.Add(venta);
                await ventaContext.SaveChangesAsync(); // Esto genera el ID de la venta

                // ✅ AHORA SÍ ACTUALIZAR STOCK Y CREAR MOVIMIENTOS
                foreach (var (producto, cantidad) in productosParaActualizar)
                {
                    // Reducir stock
                    if (!producto.ReducirStock(cantidad))
                    {
                        throw new InvalidOperationException($"Error al reducir stock de {producto.NombreArticulo}");
                    }

                    // Crear movimiento
                    // ✅ CORRECTO - Usar método estático que ya tienes
                    var movimiento = Movimiento.CrearMovimientoVenta(
                        producto.Id,
                        cantidad,
                        $"Venta POS - Ticket #{venta.NumeroTicket}",
                        Environment.UserName,
                        producto.PrecioConIVA,
                        producto.UnidadMedida,
                        venta.NumeroTicket.ToString(),
                        venta.Cliente);

                    ventaContext.Movimientos.Add(movimiento);
                }

                // ✅ GUARDAR TODOS LOS CAMBIOS DE UNA VEZ
                await ventaContext.SaveChangesAsync();

                // ✅ CONFIRMAR TRANSACCIÓN
                await transaction.CommitAsync();

                // ✅ LIMPIAR INTERFAZ Y ACTUALIZAR
                _carritoItems.Clear();
                UpdateContadoresPOS();
                await LoadEstadisticasDelDia();
                await RefrescarProductosAutomatico("stock actualizado después de venta");

                // Imprimir ticket (sin afectar la transacción)
                try
                {
                    await _ticketPrinter.ImprimirTicket(venta, "Impresora_POS");
                }
                catch (Exception ex)
                {
                    // No fallar la venta por problemas de impresión
                    MessageBox.Show($"Venta procesada correctamente.\nAdvertencia al imprimir: {ex.Message}",
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Mostrar confirmación
                string mensaje = $"✅ VENTA PROCESADA EXITOSAMENTE!\n\n" +
                                $"📄 Ticket: #{venta.NumeroTicket}\n" +
                                $"👤 Cliente: {venta.Cliente}\n" +
                                $"💰 Total: {venta.Total:C2}\n" +
                                $"📊 Ganancia: {venta.GananciaBruta:C2}";

                if (venta.ComisionTotal > 0)
                {
                    mensaje += $"\n🏦 Comisión: {venta.ComisionTotal:C2}\n" +
                              $"💵 Neto recibido: {venta.TotalRealRecibido:C2}";
                }

                MessageBox.Show(mensaje, "Venta Completada",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusPOS.Text = $"✅ Venta #{venta.NumeroTicket} completada - {venta.Total:C2}";
                return true;
            }
            catch (Exception ex)
            {
                // ✅ ROLLBACK AUTOMÁTICO SI HAY ERROR
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en rollback: {rollbackEx.Message}");
                }

                // Mostrar error específico
                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"❌ Error al procesar venta:\n\n{errorMsg}\n\nTodos los cambios han sido revertidos.",
                               "Error de Venta", MessageBoxButton.OK, MessageBoxImage.Error);

                TxtStatusPOS.Text = "❌ Error al procesar venta - Sistema restaurado";
                return false;
            }
        }

        private async Task LimpiarContextoPrincipal()
        {
            try
            {
                // Detectar y limpiar entidades con problemas
                var entriesConProblemas = _context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added)
                    .ToList();

                foreach (var entry in entriesConProblemas)
                {
                    entry.State = EntityState.Detached;
                }

                // Si hay muchos problemas, recrear el contexto
                if (entriesConProblemas.Count > 10)
                {
                    _context?.Dispose();
                    _context = new AppDbContext();
                    await LoadDataSafe(); // Recargar datos básicos
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando contexto: {ex.Message}");
            }
        }

        // ========== EVENT HANDLERS POS ==========
        private void TxtBuscarPOS_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = TxtBuscarPOS.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                _productosParaVentaFiltrados = new List<RawMaterial>(_productosParaVenta);
            }
            else
            {
                _productosParaVentaFiltrados = _productosParaVenta.Where(p =>
                    p.NombreArticulo.ToLower().Contains(searchText) ||
                    p.Categoria.ToLower().Contains(searchText) ||
                    p.CodigoBarras.ToLower().Contains(searchText)
                ).ToList();
            }

            LstProductosPOS.ItemsSource = _productosParaVentaFiltrados;
            UpdateContadoresPOS();
        }

        private async void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚪 Botón cerrar sesión presionado");

                // ===== MOSTRAR INDICADOR DE PROCESO =====
                if (TxtStatusPOS != null)
                    TxtStatusPOS.Text = "🚪 Cerrando sesión...";

                if (TxtStatus != null)
                    TxtStatus.Text = "🚪 Cerrando sesión...";

                // ===== DESHABILITAR BOTÓN PARA EVITAR DOBLE-CLICK =====
                BtnCerrarSesionPOS.IsEnabled = false;

                // ===== LIMPIAR DATOS SENSIBLES LOCALES =====
                try
                {
                    if (_carritoItems != null)
                    {
                        _carritoItems.Clear();
                        System.Diagnostics.Debug.WriteLine("🛒 Carrito limpiado");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error limpiando datos locales: {ex.Message}");
                }

                // ===== 🎯 USAR EL NUEVO MÉTODO DE REINICIO =====
                System.Diagnostics.Debug.WriteLine("🔄 Llamando SessionManager.CerrarSesionYReiniciar...");

                bool exitoso = await SessionManager.CerrarSesionYReiniciar(
                    razon: "Cierre manual desde POS",
                    mostrarConfirmacion: true
                );

                if (!exitoso)
                {
                    // Usuario canceló - restaurar interfaz
                    System.Diagnostics.Debug.WriteLine("❌ Cierre de sesión cancelado");

                    if (TxtStatusPOS != null)
                        TxtStatusPOS.Text = "✅ Sistema POS listo";

                    if (TxtStatus != null)
                        TxtStatus.Text = "✅ Sistema listo";

                    BtnCerrarSesionPOS.IsEnabled = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Proceso de reinicio iniciado - Esta instancia se cerrará");
                    // No necesitamos hacer nada más - la aplicación se reiniciará
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en BtnCerrarSesion_Click: {ex}");

                // Restaurar interfaz en caso de error
                if (TxtStatusPOS != null)
                    TxtStatusPOS.Text = "❌ Error al cerrar sesión";

                if (TxtStatus != null)
                    TxtStatus.Text = "❌ Error al cerrar sesión";

                BtnCerrarSesionPOS.IsEnabled = true;

                MessageBox.Show(
                    $"❌ Error al procesar cierre de sesión:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void BtnConfigComisiones_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configWindow = new ConfigurarComisionesWindow();
                if (configWindow.ShowDialog() == true)
                {
                    TxtStatusPOS.Text = "✅ Configuración de comisiones actualizada";

                    string mensaje = "✅ Configuración de comisiones guardada!\n\n" +
                                   "Los nuevos valores se aplicarán en las próximas ventas.";

                    // Agregar información sobre IVA si está configurado
                    if (configWindow.TerminalCobraIVA)
                    {
                        mensaje += "\n\n🧮 Nota: El terminal cobrará IVA adicional del 16% sobre las comisiones.";
                    }

                    MessageBox.Show(mensaje, "Configuración Actualizada",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al configurar comisiones: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al configurar comisiones";
            }
        }
        private async Task RefrescarProductosAutomatico(string motivo = "")
        {
            try
            {
                if (!_posLoaded) return; // Solo si POS está cargado

                // Recargar productos desde base de datos
                _productosParaVenta = await _context.GetProductosDisponiblesParaVenta().ToListAsync();

                // Aplicar filtro actual si existe
                string filtroActual = TxtBuscarPOS.Text.ToLower().Trim();
                if (string.IsNullOrEmpty(filtroActual))
                {
                    _productosParaVentaFiltrados = new List<RawMaterial>(_productosParaVenta);
                }
                else
                {
                    _productosParaVentaFiltrados = _productosParaVenta.Where(p =>
                        p.NombreArticulo.ToLower().Contains(filtroActual) ||
                        p.Categoria.ToLower().Contains(filtroActual) ||
                        p.CodigoBarras.ToLower().Contains(filtroActual)
                    ).ToList();
                }

                // Actualizar lista sin perder selección
                var seleccionActual = LstProductosPOS.SelectedItem;
                LstProductosPOS.ItemsSource = null;
                LstProductosPOS.ItemsSource = _productosParaVentaFiltrados;

                // Restaurar selección si el producto sigue disponible
                if (seleccionActual is RawMaterial productoSeleccionado)
                {
                    var productoActualizado = _productosParaVentaFiltrados
                        .FirstOrDefault(p => p.Id == productoSeleccionado.Id);
                    if (productoActualizado != null)
                    {
                        LstProductosPOS.SelectedItem = productoActualizado;
                    }
                }

                // Actualizar contadores
                UpdateContadoresPOS();

                // Mostrar mensaje discreto si hay motivo
                if (!string.IsNullOrEmpty(motivo))
                {
                    TxtStatusPOS.Text = $"✅ Productos actualizados ({motivo})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTO-REFRESH] Error: {ex.Message}");
            }
        }

        private void BtnConfigurarPrecios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var preciosWindow = new ConfigurarPrecioVentaWindow(_context);
                if (preciosWindow.ShowDialog() == true)
                {
                    // Actualizar datos POS después de configurar precios
                    _ = LoadDataPuntoVenta();
                    TxtStatusPOS.Text = "✅ Precios actualizados - Sistema POS sincronizado";

                    MessageBox.Show("✅ Precios de venta configurados exitosamente!\n\n" +
                                  "Los productos están listos para el punto de venta.",
                                  "Configuración Completada",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir configuración de precios: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al configurar precios";
            }
        }

        private async void TxtBuscarPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string codigo = TxtBuscarPOS.Text.Trim();
                if (!string.IsNullOrEmpty(codigo))
                {
                    // Buscar producto por código de barras
                    var producto = _productosParaVenta.FirstOrDefault(p =>
                        p.CodigoBarras.Equals(codigo, StringComparison.OrdinalIgnoreCase));

                    if (producto != null)
                    {
                        await AgregarProductoAlCarrito(producto);
                        TxtBuscarPOS.Text = "";
                    }
                    else
                    {
                        MessageBox.Show($"Producto no encontrado: {codigo}",
                                      "Producto No Encontrado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void BtnBuscarPOS_Click(object sender, RoutedEventArgs e)
        {
            TxtBuscarPOS.Focus();
        }

        private void LstProductosPOS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Solo para feedback visual
        }

        private async void LstProductosPOS_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LstProductosPOS.SelectedItem is RawMaterial producto)
            {
                // Verificar si es producto por peso
                if (producto.UnidadMedida.ToLower().Contains("kg") ||
                    producto.UnidadMedida.ToLower().Contains("gr"))
                {
                    // Abrir ventana para ingresar peso con los parámetros requeridos
                    var pesoWindow = new IngresarPesoWindow(_context, producto, _basculaService);
                    if (pesoWindow.ShowDialog() == true)
                    {
                        await AgregarProductoAlCarrito(producto, pesoWindow.PesoIngresado);
                    }
                }
                else
                {
                    // Agregar cantidad fija
                    await AgregarProductoAlCarrito(producto, 1);
                }
            }
        }

        private void BtnEliminarDelCarrito_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DetalleVenta item)
            {
                _carritoItems.Remove(item);
                UpdateContadoresPOS();
                TxtStatusPOS.Text = $"✅ Eliminado: {item.NombreProducto}";
            }
        }

        private async void BtnProcesarVenta_Click(object sender, RoutedEventArgs e)
        {
            await ProcesarVentaUnico();
        }

        private void BtnLimpiarCarrito_Click(object sender, RoutedEventArgs e)
        {
            if (_carritoItems.Any())
            {
                var result = MessageBox.Show("¿Limpiar el carrito de compras?",
                                           "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _carritoItems.Clear();
                    UpdateContadoresPOS();
                    TxtStatusPOS.Text = "✅ Carrito limpiado";
                }
            }
        }

        private async void BtnVerVentas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ventasHoy = await _context.GetVentasDelDia(DateTime.Today).ToListAsync();

                if (!ventasHoy.Any())
                {
                    MessageBox.Show("No hay ventas registradas hoy.", "Sin Ventas",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string reporte = $"📊 VENTAS DEL DÍA - {DateTime.Today:dd/MM/yyyy}\n\n";
                reporte += $"Total de ventas: {ventasHoy.Count}\n";
                reporte += $"Monto total: {ventasHoy.Sum(v => v.Total):C2}\n";
                reporte += $"Ganancia total: {ventasHoy.Sum(v => v.GananciaBruta):C2}\n\n";
                reporte += "ÚLTIMAS VENTAS:\n";

                foreach (var venta in ventasHoy.OrderByDescending(v => v.FechaVenta).Take(10))
                {
                    reporte += $"#{venta.NumeroTicket} - {venta.FechaVenta:HH:mm} - {venta.Total:C2} - {venta.Cliente}\n";
                }

                MessageBox.Show(reporte, "Ventas del Día", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar ventas: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    
        protected override async void OnClosing(CancelEventArgs e)
{
    try
    {
        System.Diagnostics.Debug.WriteLine("🚪 Usuario intentó cerrar ventana principal");

        // ===== CANCELAR EL CIERRE AUTOMÁTICO =====
        e.Cancel = true;

        // ===== MOSTRAR INDICADOR DE PROCESO =====
        if (TxtStatusPOS != null)
            TxtStatusPOS.Text = "💾 Cerrando sistema...";
            
        if (TxtStatus != null)
            TxtStatus.Text = "💾 Cerrando sistema...";

        // ===== DESHABILITAR CONTROLES PARA EVITAR ACCIONES ADICIONALES =====
        this.IsEnabled = false;

        // ===== USAR SessionManager PARA SALIR COMPLETAMENTE =====
        System.Diagnostics.Debug.WriteLine("🛑 Llamando SessionManager.SalirCompletamente...");
        
        bool exitoso = await SessionManager.SalirCompletamente(
            razon: "Cierre desde botón X de ventana",
            mostrarConfirmacion: true
        );

        if (!exitoso)
        {
            // Usuario canceló - restaurar ventana
            System.Diagnostics.Debug.WriteLine("❌ Cierre cancelado - restaurando ventana");
            
            this.IsEnabled = true;
            
            if (TxtStatusPOS != null)
                TxtStatusPOS.Text = "✅ Sistema POS listo";
                
            if (TxtStatus != null)
                TxtStatus.Text = "✅ Sistema listo";
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("✅ Proceso de cierre iniciado");
            // No necesitamos hacer nada más - la aplicación se cerrará completamente
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"💥 ERROR en OnClosing: {ex}");
        
        // En caso de error, permitir cierre normal
        e.Cancel = false;
        
        MessageBox.Show(
            $"❌ Error al procesar cierre:\n\n{ex.Message}\n\nEl sistema se cerrará.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
        private void BtnEscanerPOS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _scannerService.MostrarVentanaEscaneo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al activar escáner: {ex.Message}",
                              "Error Escáner", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoEscaner.Text = "📱 ERROR";
                TxtEstadoEscaner.Parent.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 68, 68)));
            }
        }

        private async void BtnSalirSistema_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🛑 Botón salir sistema presionado");

                // ===== MOSTRAR INDICADOR DE PROCESO =====
                if (TxtStatusPOS != null)
                    TxtStatusPOS.Text = "🛑 Saliendo del sistema...";

                if (TxtStatus != null)
                    TxtStatus.Text = "🛑 Saliendo del sistema...";

                // ===== DESHABILITAR BOTÓN PARA EVITAR DOBLE-CLICK =====
                var button = sender as Button;
                if (button != null)
                    button.IsEnabled = false;

                // ===== DESHABILITAR VENTANA PARA EVITAR OTRAS ACCIONES =====
                this.IsEnabled = false;

                // ===== LIMPIAR DATOS SENSIBLES LOCALES =====
                try
                {
                    if (_carritoItems != null)
                    {
                        _carritoItems.Clear();
                        System.Diagnostics.Debug.WriteLine("🛒 Carrito limpiado antes de salir");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error limpiando datos locales: {ex.Message}");
                }

                // ===== 🛑 USAR SessionManager PARA SALIR COMPLETAMENTE =====
                System.Diagnostics.Debug.WriteLine("🛑 Llamando SessionManager.SalirCompletamente...");

                bool exitoso = await SessionManager.SalirCompletamente(
                    razon: "Salida manual desde botón Salir",
                    mostrarConfirmacion: true
                );

                if (!exitoso)
                {
                    // Usuario canceló - restaurar interfaz
                    System.Diagnostics.Debug.WriteLine("❌ Salida cancelada");

                    this.IsEnabled = true;

                    if (button != null)
                        button.IsEnabled = true;

                    if (TxtStatusPOS != null)
                        TxtStatusPOS.Text = "✅ Sistema POS listo";

                    if (TxtStatus != null)
                        TxtStatus.Text = "✅ Sistema listo";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🛑 Proceso de salida iniciado - Sistema se cerrará");
                    // No necesitamos hacer nada más - la aplicación se cerrará completamente
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en BtnSalirSistema_Click: {ex}");

                // Restaurar interfaz en caso de error
                this.IsEnabled = true;

                var button = sender as Button;
                if (button != null)
                    button.IsEnabled = true;

                if (TxtStatusPOS != null)
                    TxtStatusPOS.Text = "❌ Error al salir";

                if (TxtStatus != null)
                    TxtStatus.Text = "❌ Error al salir";

                MessageBox.Show(
                    $"❌ Error al salir del sistema:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ✅ CORREGIR - Botón para configurar báscula
        private async void BtnBascula_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir configuración de báscula
                var configWindow = new ConfigurarBasculaWindow(_context);
                if (configWindow.ShowDialog() == true)
                {
                    // Reconectar báscula con nueva configuración
                    await _basculaService.DesconectarAsync();
                    await Task.Delay(500); // Pequeña pausa

                    var conectado = await _basculaService.ConectarAsync();

                    if (conectado)
                    {
                        TxtEstadoBascula.Text = "⚖️ OK";
                        TxtEstadoBascula.Parent.SetValue(Border.BackgroundProperty,
                            new SolidColorBrush(Color.FromRgb(34, 197, 94)));
                        TxtStatusPOS.Text = "✅ Báscula configurada y conectada";
                    }
                    else
                    {
                        TxtEstadoBascula.Text = "⚖️ ERROR";
                        TxtEstadoBascula.Parent.SetValue(Border.BackgroundProperty,
                            new SolidColorBrush(Color.FromRgb(239, 68, 68)));
                        TxtStatusPOS.Text = "❌ Error al conectar báscula";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al configurar báscula: {ex.Message}",
                              "Error Báscula", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoBascula.Text = "⚖️ ERROR";
                TxtEstadoBascula.Parent.SetValue(Border.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(239, 68, 68)));
            }
        }

        private void BtnImpresora_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configWindow = new ConfigurarImpresoraWindow();
                configWindow.ShowDialog();

                TxtStatusPOS.Text = "✅ Configuración de impresora actualizada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al configurar impresora: {ex.Message}",
                              "Error Impresora", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoImpresora.Text = "🖨️ ERROR";
                TxtEstadoImpresora.Parent.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(239, 68, 68)));
            }
        }

        private async Task OnPesoRecibido(decimal peso)
        {
            try
            {
                if (LstProductosPOS.SelectedItem is RawMaterial producto)
                {
                    await AgregarProductoAlCarrito(producto, peso);
                }
            }
            catch (Exception ex)
            {
                TxtStatusPOS.Text = $"❌ Error al procesar peso: {ex.Message}";
            }
        }

        private async Task OnProductoEscaneado(string codigoBarras)
        {
            try
            {
                var producto = _productosParaVenta.FirstOrDefault(p =>
                    p.CodigoBarras.Equals(codigoBarras, StringComparison.OrdinalIgnoreCase));

                if (producto != null)
                {
                    await AgregarProductoAlCarrito(producto);
                }
                else
                {
                    MessageBox.Show($"Producto no encontrado: {codigoBarras}",
                                  "Código No Encontrado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                TxtStatusPOS.Text = $"❌ Error al procesar código: {ex.Message}";
            }
        }

        private void OnErrorPOSOcurrido(string dispositivo, string mensaje)
        {
            MessageBox.Show($"Error en dispositivo POS: {mensaje}",
                          "Error POS", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatusPOS.Text = $"❌ Error POS: {dispositivo}";
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = TxtBuscar.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                _filteredMaterials = new List<RawMaterial>(_allMaterials);
            }
            else
            {
                _filteredMaterials = _allMaterials.Where(m =>
                    m.NombreArticulo.ToLower().Contains(searchText) ||
                    m.Categoria.ToLower().Contains(searchText) ||
                    m.Proveedor.ToLower().Contains(searchText) ||
                    m.CodigoBarras.ToLower().Contains(searchText)
                ).ToList();
            }

            UpdateDataGrid();
            UpdateStatusBar();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            TxtBuscar.Focus();
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }
        private async Task RefreshData()
        {
            try
            {
                BtnActualizar.IsEnabled = false;
                BtnActualizar.Content = "⏳";
                BtnActualizar.ToolTip = "Actualizando...";

                await Task.Delay(500);

                // Actualizar inventario (código existente)
                _allMaterials = await _context.RawMaterials
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                string currentSearch = TxtBuscar.Text.ToLower().Trim();
                if (string.IsNullOrEmpty(currentSearch))
                {
                    _filteredMaterials = new List<RawMaterial>(_allMaterials);
                }
                else
                {
                    _filteredMaterials = _allMaterials.Where(m =>
                        m.NombreArticulo.ToLower().Contains(currentSearch) ||
                        m.Categoria.ToLower().Contains(currentSearch) ||
                        m.Proveedor.ToLower().Contains(currentSearch) ||
                        m.CodigoBarras.ToLower().Contains(currentSearch)).ToList();
                }

                UpdateDataGrid();
                UpdateStatusBar();

                // ✅ AUTOMÁTICO: Actualizar POS si está cargado
                await RefrescarProductosAutomatico("inventario actualizado");

                TxtStatus.Text = $"✅ Actualizado - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al actualizar datos";
            }
            finally
            {
                BtnActualizar.IsEnabled = true;
                BtnActualizar.Content = "🔄";
                BtnActualizar.ToolTip = "Actualizar datos";
            }
        }
        private async void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectorWindow = new TipoMaterialSelectorWindow(_context);
                if (selectorWindow.ShowDialog() == true)
                {
                    await RefreshData();
                    TxtStatus.Text = "✅ Material agregado correctamente";
                    await RefrescarProductosAutomatico("nuevo producto agregado");

                    // Mostrar confirmación con información del último material agregado
                    var ultimoMaterial = await _context.RawMaterials
                        .OrderByDescending(m => m.Id)
                        .FirstOrDefaultAsync();

                    if (ultimoMaterial != null)
                    {
                        MessageBox.Show(
                            $"✅ Material creado exitosamente!\n\n" +
                            $"Producto: {ultimoMaterial.NombreArticulo}\n" +
                            $"Stock inicial: {ultimoMaterial.StockTotal:F2} {ultimoMaterial.UnidadMedida}\n" +
                            $"Valor total: {ultimoMaterial.ValorTotalConIVA:C2}",
                            "Material Agregado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar material: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al agregar material";
            }
        }

        private async void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (DgMateriales.SelectedItem is RawMaterial selectedMaterial)
            {
                try
                {
                    var editWindow = new EditAddStockWindow(_context, selectedMaterial);
                    if (editWindow.ShowDialog() == true)
                    {
                        await RefreshData();
                        TxtStatus.Text = $"✅ Material actualizado - {editWindow.MotivoEdicion}";
                        await RefrescarProductosAutomatico("producto editado");

                        MessageBox.Show(
                            $"✅ Material actualizado correctamente!\n\n" +
                            $"Cambios: {editWindow.MotivoEdicion}",
                            "Actualización Exitosa",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    await LimpiarContextoPrincipal(); // ✅ AGREGAR ESTA LÍNEA
                    MessageBox.Show($"Error al editar material: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtStatus.Text = "❌ Error al editar material";
                }
            }
            else
            {
                MessageBox.Show("Seleccione un material para editar.",
                              "Selección Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (DgMateriales.SelectedItem is RawMaterial selectedMaterial)
            {
                try
                {
                    // Verificar movimientos
                    var cantidadMovimientos = await _context.Movimientos
                        .CountAsync(m => m.RawMaterialId == selectedMaterial.Id);

                    // Confirmación
                    var mensaje = $"¿Eliminar '{selectedMaterial.NombreArticulo}'?\n\n" +
                                 $"Movimientos: {cantidadMovimientos}\n" +
                                 $"Stock: {selectedMaterial.StockTotal:F2} {selectedMaterial.UnidadMedida}\n" +
                                 $"Valor: {selectedMaterial.ValorTotalConIVA:C2}\n\n" +
                                 $"✅ El historial se conservará para auditoría.";

                    var result = MessageBox.Show(mensaje, "Confirmar Eliminación",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    // Obtener material
                    var materialDB = await _context.RawMaterials.FindAsync(selectedMaterial.Id);
                    if (materialDB == null)
                    {
                        MessageBox.Show("Material no encontrado.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Eliminación lógica
                    materialDB.Eliminado = true;
                    materialDB.FechaEliminacion = DateTime.Now;
                    materialDB.UsuarioEliminacion = Environment.UserName;
                    materialDB.MotivoEliminacion = $"Eliminado - Stock: {materialDB.StockTotal:F2}";

                    // Crear movimiento
                    var movimiento = new Movimiento
                    {
                        RawMaterialId = materialDB.Id,
                        TipoMovimiento = "Eliminación",
                        Cantidad = materialDB.StockTotal,
                        Motivo = $"Eliminado por {Environment.UserName}",
                        Usuario = Environment.UserName,
                        PrecioConIVA = materialDB.PrecioConIVA,
                        PrecioSinIVA = materialDB.PrecioSinIVA,
                        UnidadMedida = materialDB.UnidadMedida
                    };

                    _context.Movimientos.Add(movimiento);
                    await _context.SaveChangesAsync();

                    // Actualizar
                    await RefreshData();
                    TxtStatus.Text = "✅ Eliminado (historial conservado)";
                    await RefrescarProductosAutomatico("producto eliminado");

                    MessageBox.Show(
                        $"✅ ¡Eliminación exitosa!\n\n" +
                        $"Producto: {materialDB.NombreArticulo}\n" +
                        $"Historial: {cantidadMovimientos + 1} movimientos\n" +
                        $"Nuevo ID: #{movimiento.Id}",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                catch (Exception ex)
                {
                    await LimpiarContextoPrincipal(); // ✅ AGREGAR ESTA LÍNEA
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un material.", "Selección Requerida",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEscaner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var scannerWindow = new BarcodeScannerWindow(_context);
                scannerWindow.ShowDialog();

                // Refrescar datos después de usar el escáner
                _ = RefreshData();
            }
            catch (Exception ex)
            {
                // Si no existe BarcodeScannerWindow, mostrar input manual
                var inputWindow = new ManualBarcodeInputWindow(_context);
                if (inputWindow.ShowDialog() == true)
                {
                    _ = RefreshData();
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Disposed servicios POS
            _ticketPrinter?.Dispose();
            _basculaService?.Dispose();
            _scannerService?.Dispose();
            _posIntegrationService?.Dispose();
            _corteCajaService?.Dispose();

            _context?.Dispose();
            base.OnClosed(e);
        }
        private void ConfigurarActualizacionAutomatica()
        {
            var timerActualizacion = new System.Windows.Threading.DispatcherTimer();
            timerActualizacion.Interval = TimeSpan.FromMinutes(5); // Cada 5 minutos
            timerActualizacion.Tick += async (s, e) =>
            {
                if (_posLoaded && MainTabControl.SelectedIndex == 1) // Solo si está en pestaña POS
                {
                    await RefrescarProductosAutomatico();

                    // También verificar estado de corte de caja
                    await VerificarEstadoCorteCaja();
                }
            };
            timerActualizacion.Start();
        }
        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                switch (tabControl.SelectedIndex)
                {
                    case 0: // Materia Prima
                        TxtStatus.Text = "✅ Módulo de Materia Prima activo";
                        break;
                    case 1: // PUNTO DE VENTA
                        TxtStatusPOS.Text = "💰 Cargando Sistema POS...";
                        if (!_posLoaded)
                        {
                            await LoadDataPuntoVenta();
                        }
                        else
                        {
                            // Si ya está cargado, verificar estado de corte
                            await VerificarEstadoCorteCaja();
                        }
                        break;
                    case 2: // Reportes
                        TxtStatus.Text = "📊 Módulo de Reportes disponible";
                        break;
                    case 3: // Procesos
                        TxtStatus.Text = "⚙️ Módulo de Procesos (próximamente)";
                        break;
                    case 4: // Análisis
                        TxtStatus.Text = "📈 Módulo de Análisis (próximamente)";
                        break;
                    case 5: // Configuración
                        TxtStatus.Text = "⚙️ Configuración del sistema (próximamente)";
                        break;
                    case 6: // Mi Información
                        TxtStatus.Text = "👨‍💻 Información del desarrollador - Esaú Villagrán";
                        break;
                }
            }
        }
        // ========== MÉTODOS PARA GESTIÓN DE USUARIOS Y SESIONES ==========

        private async void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ CORRECCIÓN: No pasar contexto, que GestionUsuariosWindow cree el suyo
                var gestionUsuariosWindow = new GestionUsuariosWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var resultado = gestionUsuariosWindow.ShowDialog();

                if (resultado == true)
                {
                    TxtStatus.Text = "✅ Gestión de usuarios completada";
                }
                else
                {
                    TxtStatus.Text = "📊 Gestión de usuarios cerrada";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir gestión de usuarios:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir gestión de usuarios";
            }
        }

        /// <summary>
        /// Abre la configuración del sistema
        /// </summary>
        private void BtnConfiguracionSistema_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Crear ventana de configuración del sistema
                MessageBox.Show("🔧 Configuración del Sistema\n\n" +
                              "Esta funcionalidad estará disponible en una próxima versión.\n\n" +
                              "Incluirá:\n" +
                              "• Configuración de empresa\n" +
                              "• Parámetros de sistema\n" +
                              "• Configuración de dispositivos\n" +
                              "• Backup y restauración",
                              "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatus.Text = "🔧 Configuración del sistema (próximamente)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir configuración: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir configuración";
            }
        }

        /// <summary>
        /// Abre el selector de usuario para historial de sesiones
        /// </summary>

        private async void BtnHistorialSesiones_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ USAR CONTEXTO TEMPORAL PARA VERIFICACIÓN INICIAL
                using var tempContext = new AppDbContext();
                var cantidadUsuarios = await tempContext.Users.CountAsync(u => !u.Eliminado);

                if (cantidadUsuarios == 0)
                {
                    MessageBox.Show("No hay usuarios registrados en el sistema.",
                                  "Sin Usuarios", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtStatus.Text = "📊 Sin usuarios para mostrar historial";
                    return;
                }

                // ✅ CORRECCIÓN: No pasar contexto
                var selectorWindow = new SelectorUsuarioWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (selectorWindow.ShowDialog() == true)
                {
                    var usuarioSeleccionado = selectorWindow.UsuarioSeleccionado;

                    if (usuarioSeleccionado != null)
                    {
                        // ✅ CREAR NUEVO CONTEXTO PARA CARGAR SESIONES
                        using var contextSesiones = new AppDbContext();
                        var sesiones = await contextSesiones.UserSessions
                            .Where(s => s.UserId == usuarioSeleccionado.Id)
                            .OrderByDescending(s => s.FechaInicio)
                            .ToListAsync();

                        var historialWindow = new HistorialSesionesWindow(usuarioSeleccionado, sesiones)
                        {
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };

                        historialWindow.Show();
                        TxtStatus.Text = $"🕐 Historial de sesiones abierto para: {usuarioSeleccionado.NombreCompleto}";
                    }
                }
                else
                {
                    TxtStatus.Text = "📊 Selección de usuario cancelada";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir historial de sesiones:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir historial de sesiones";
            }
        }

        private void BtnReporteVentas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reporteVentasWindow = new ReporteVentasWindow(_context)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                reporteVentasWindow.Show();

                TxtStatus.Text = "🎯 Reporte de Ventas abierto";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir reporte de ventas: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir reporte de ventas";
            }
        }

        // Evento para reporte de stock
        private void BtnReporteStock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reporteStockWindow = new ReporteStockWindow(_context)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                reporteStockWindow.Show();

                TxtStatus.Text = "📦 Reporte de Stock abierto";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir reporte de stock: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir reporte de stock";
            }
        }

        // Efectos visuales para los borders
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                border.BorderThickness = new Thickness(2);
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                border.BorderThickness = new Thickness(1);
            }
        }
        private void BtnConfigurarProcesos_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🔧 Módulo de Procesos\n\n" +
                          "Esta funcionalidad estará disponible en una próxima versión.\n" +
                          "Permitirá configurar y gestionar procesos de producción.",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnVerDashboard_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📊 Dashboard de Análisis\n\n" +
                          "Esta funcionalidad estará disponible en una próxima versión.\n" +
                          "Incluirá gráficos avanzados y métricas de rendimiento.",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAbrirConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⚙️ Configuración del Sistema\n\n" +
                          "Esta funcionalidad estará disponible en una próxima versión.\n" +
                          "Permitirá configurar parámetros generales del sistema.",
                          "Próximamente", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAbrirInformacion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var infoWindow = new MiInformacionWindow
                {
                    Owner = this
                };
                infoWindow.ShowDialog();

                // Actualizar status después de cerrar la ventana
                TxtStatus.Text = "👨‍💻 Ventana de información cerrada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir información del desarrollador: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir información";
            }
        }
    }

    public class ManualBarcodeInputWindow : Window
    {
        private AppDbContext _context;
        public string CodigoIngresado { get; private set; } = "";

        public ManualBarcodeInputWindow(AppDbContext context)
        {
            _context = context;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Ingreso Manual de Código";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var grid = new Grid();
            grid.Margin = new Thickness(20);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Título
            var titulo = new TextBlock
            {
                Text = "📱 Ingreso Manual de Código",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 59, 78))
            };
            Grid.SetRow(titulo, 0);
            grid.Children.Add(titulo);

            // Instrucción
            var instruccion = new TextBlock
            {
                Text = "Escriba o pegue el código de barras:",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            Grid.SetRow(instruccion, 1);
            grid.Children.Add(instruccion);

            // TextBox para código
            var txtCodigo = new TextBox
            {
                Name = "TxtCodigo",
                Padding = new Thickness(10),
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 212, 218)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20),
                MaxLength = 50 // Límite razonable para códigos de barras
            };
            Grid.SetRow(txtCodigo, 2);
            grid.Children.Add(txtCodigo);

            // Botones
            var panelBotones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnCancelar = new Button
            {
                Content = "❌ Cancelar",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12
            };
            btnCancelar.Click += (s, e) => { DialogResult = false; Close(); };

            var btnAceptar = new Button
            {
                Content = "✅ Procesar",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12
            };
            btnAceptar.Click += async (s, e) => await ProcesarCodigo(txtCodigo.Text);

            panelBotones.Children.Add(btnCancelar);
            panelBotones.Children.Add(btnAceptar);
            Grid.SetRow(panelBotones, 4);
            grid.Children.Add(panelBotones);

            Content = grid;

            // Enfocar el textbox al cargar
            Loaded += (s, e) => txtCodigo.Focus();

            // Procesar al presionar Enter
            txtCodigo.KeyDown += async (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    await ProcesarCodigo(txtCodigo.Text);
                }
            };
        }

        private async Task ProcesarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                MessageBox.Show("Ingrese un código válido.", "Código Requerido",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CodigoIngresado = codigo.Trim();

            try
            {
                // Buscar solo en productos activos
                var existingMaterial = await _context.RawMaterials
                    .FirstOrDefaultAsync(m => m.CodigoBarras == CodigoIngresado);

                if (existingMaterial != null)
                {
                    // Código existente - abrir formulario de edición
                    var editWindow = new EditAddStockWindow(_context, existingMaterial);
                    if (editWindow.ShowDialog() == true)
                    {
                        MessageBox.Show(
                            $"✅ Producto actualizado!\n\n" +
                            $"Cambios: {editWindow.MotivoEdicion}",
                            "Actualización por Código",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Código nuevo - abrir selector
                    var selectorWindow = new TipoMaterialSelectorWindow(_context, CodigoIngresado);
                    if (selectorWindow.ShowDialog() == true)
                    {
                        MessageBox.Show(
                            "✅ Nuevo producto creado correctamente!",
                            "Creación por Código",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar código: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




    }
}