using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using costbenefi.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;
using System.ComponentModel;
using costbenefi.Managers;
using System.Globalization;
using System.Windows.Data;



namespace costbenefi
{
    public partial class MainWindow : Window
    {
        private AppDbContext _context;
        private List<RawMaterial> _allMaterials = new();
        private List<RawMaterial> _filteredMaterials = new();
        private List<ServicioVenta> _serviciosParaVenta = new();
        private List<object> _itemsPOS = new(); // Lista mixta para productos Y servicios

        private ScannerProtectionService _scannerProtection;
        private UnifiedScannerService _unifiedScanner;

        // ========== VARIABLES POS ==========
        private List<RawMaterial> _productosParaVenta = new();
        private List<RawMaterial> _productosParaVentaFiltrados = new();
        private ObservableCollection<DetalleVenta> _carritoItems = new();
        private CorteCajaService _corteCajaService;
        private RawMaterial _ultimoProductoEscaneado = null;
        private DateTime _tiempoUltimoEscaneo = DateTime.MinValue;
        private bool _modoEscanerActivo = false;

        private DescuentoAplicadoInfo _descuentoInfo = null;

        // Servicios POS
        private TicketPrinter _ticketPrinter;
        private BasculaService _basculaService;
        private POSIntegrationService _posIntegrationService;
        private bool _cerrandoSesion = false;
    


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

                // ✅ INICIALIZAR SERVICIOS DEL ESCÁNER
                _scannerProtection = new ScannerProtectionService(this);
                _cerrandoSesion = false;

                // Configurar eventos de carga (UNIFICADO)
                this.Loaded += MainWindow_Loaded_Complete;

                this.KeyDown += MainWindow_KeyDown_Plus;

                System.Diagnostics.Debug.WriteLine("✅ MainWindow constructor completado exitosamente");
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
        }
        private async Task MainWindow_Loaded_Internal()
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

        // ✅ EVENTO DE CARGA UNIFICADO (AGREGAR ESTE MÉTODO NUEVO)
        private async void MainWindow_Loaded_Complete(object sender, RoutedEventArgs e)
        {
            try
            {
                // Carga de datos principal (sin await porque es void)
                await MainWindow_Loaded_Internal();

                // Esperar un poco para que se renderice completamente
                await Task.Delay(500);

                // Forzar actualización completa de UI
                this.UpdateLayout();
                this.InvalidateVisual();

                // Actualizar TabControl si existe
                var tabControl = this.FindName("MainTabControl") as TabControl;
                if (tabControl != null)
                {
                    var currentTab = tabControl.SelectedIndex;
                    tabControl.SelectedIndex = -1;
                    tabControl.UpdateLayout();
                    tabControl.SelectedIndex = currentTab >= 0 ? currentTab : 0;
                }

                System.Diagnostics.Debug.WriteLine("🎉 UI completamente inicializada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en carga completa: {ex.Message}");
                if (TxtStatus != null)
                    TxtStatus.Text = "⚠️ Error en inicialización completa";
            }
        }
        private async Task ManejarTeclaMas()
        {
            try
            {
                // Verificar que hay un producto reciente (escaneado en los últimos 30 segundos)
                if (_ultimoProductoEscaneado == null ||
                    (DateTime.Now - _tiempoUltimoEscaneo).TotalSeconds > 30)
                {
                    TxtStatusPOS.Text = "⚠️ No hay producto reciente para agregar cantidad";
                    MessageBox.Show("No hay ningún producto reciente para agregar cantidad.\n\n" +
                                   "Escanee un producto primero o seleccione uno de la lista.",
                                   "Sin Producto Seleccionado",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"📦 Abriendo ventana de cantidad para: {_ultimoProductoEscaneado.NombreArticulo}");

                // Abrir ventana de cantidad
                var cantidadWindow = new IngresarCantidadWindow(_ultimoProductoEscaneado.NombreArticulo)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (cantidadWindow.ShowDialog() == true && cantidadWindow.SeConfirmo)
                {
                    int cantidadAdicional = cantidadWindow.CantidadIngresada;
                    System.Diagnostics.Debug.WriteLine($"✅ Usuario confirmó cantidad adicional: {cantidadAdicional}");

                    // Agregar cantidad adicional al carrito
                    await AgregarProductoAlCarrito(_ultimoProductoEscaneado, cantidadAdicional);

                    TxtStatusPOS.Text = $"✅ Agregado: {_ultimoProductoEscaneado.NombreArticulo} x{cantidadAdicional} (cantidad adicional)";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Usuario canceló ingreso de cantidad adicional");
                    TxtStatusPOS.Text = "❌ Ingreso de cantidad adicional cancelado";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ManejarTeclaMas: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error al agregar cantidad adicional: {ex.Message}";

                MessageBox.Show($"Error al procesar cantidad adicional:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void MainWindow_KeyDown_Plus(object sender, KeyEventArgs e)
        {
            try
            {
                // Solo procesar en pestaña POS
                if (MainTabControl.SelectedIndex != 1 || !_posLoaded)
                    return;

                // Detectar tecla "+" (tanto en teclado principal como numérico)
                if (e.Key == Key.Add || e.Key == Key.OemPlus)
                {
                    System.Diagnostics.Debug.WriteLine("🔑 Tecla '+' detectada en POS");

                    await ManejarTeclaMas();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en KeyDown_Plus: {ex.Message}");
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

                // ✅ NUEVO: INICIALIZACIÓN INDEPENDIENTE DEL ESCÁNER
                InicializarEscanerIndependiente();

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

                System.Diagnostics.Debug.WriteLine("✅ Servicios POS inicializados - Escáner independiente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error en InitializePOSServicesSafe: {ex.Message}");
                // ✅ CRÍTICO: Continuar sin algunos servicios POS - NO lanzar excepción
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

        private void InicializarEscanerIndependiente()
        {
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000); // Esperar a que se cargue el POS

                    System.Diagnostics.Debug.WriteLine("🔄 Inicializando escáner en hilo independiente...");

                    // ✅ NUEVO: Inicializar UnifiedScannerService DE FORMA SEGURA
                    _unifiedScanner = new UnifiedScannerService(this, _scannerProtection);

                    // ✅ CONFIGURAR EVENTOS CON MANEJO DE ERRORES MEJORADO
                    _unifiedScanner.CodigoDetectado += OnCodigoEscaneadoPOS_Protected;

                    _unifiedScanner.EstadoCambiado += (s, mensaje) =>
                    {
                        try
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                if (TxtStatusPOS != null)
                                    TxtStatusPOS.Text = $"📱 {mensaje}";

                                if (TxtEstadoEscaner != null)
                                {
                                    TxtEstadoEscaner.Text = "📱 OK";
                                    TxtEstadoEscaner.Parent?.SetValue(Border.BackgroundProperty,
                                        new SolidColorBrush(Color.FromRgb(34, 197, 94)));
                                }
                                System.Diagnostics.Debug.WriteLine($"✅ Estado escáner: {mensaje}");
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Error en evento EstadoCambiado: {ex.Message}");
                        }
                    };

                    // ✅ ERROR HANDLER MEJORADO PARA ESCÁNER
                    _unifiedScanner.ErrorOcurrido += (s, error) =>
                    {
                        try
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Error escáner (no crítico): {error}");

                                if (TxtEstadoEscaner != null)
                                {
                                    TxtEstadoEscaner.Text = "📱 ERROR";
                                    TxtEstadoEscaner.Parent?.SetValue(Border.BackgroundProperty,
                                        new SolidColorBrush(Color.FromRgb(239, 68, 68))); // Rojo
                                }

                                // ✅ NO actualizar TxtStatusPOS para no interferir con el POS
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Error manejando error escáner: {ex.Message}");
                        }
                    };

                    System.Diagnostics.Debug.WriteLine("✅ Escáner inicializado independientemente");

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error en InicializarEscanerIndependiente: {ex.Message}");

                    // ✅ ACTUALIZAR INTERFAZ INDICANDO ERROR PERO NO BLOQUEAR POS
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (TxtEstadoEscaner != null)
                        {
                            TxtEstadoEscaner.Text = "📱 NO DISPONIBLE";
                            TxtEstadoEscaner.Parent?.SetValue(Border.BackgroundProperty,
                                new SolidColorBrush(Color.FromRgb(107, 114, 128))); // Gris
                        }
                    });
                }
            });
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
        // ✅ HANDLER DEL ESCÁNER CON PROTECCIÓN INTEGRADA MEJORADA
        private async void OnCodigoEscaneadoPOS_Protected(object sender, CodigoEscaneadoEventArgs e)
        {
            try
            {
                // ✅ DEBUG DETALLADO
                System.Diagnostics.Debug.WriteLine($"🔍 ===== OnCodigoEscaneadoPOS_Protected INICIADO =====");
                System.Diagnostics.Debug.WriteLine($"   📝 Código recibido: '{e.CodigoBarras}'");
                System.Diagnostics.Debug.WriteLine($"   📝 Contexto: {e.Contexto}");
                System.Diagnostics.Debug.WriteLine($"   📝 _posLoaded: {_posLoaded}");
                System.Diagnostics.Debug.WriteLine($"   📝 Tab seleccionado: {MainTabControl.SelectedIndex}");

                // ✅ MEJORAR: Si POS no está cargado, intentar cargarlo
                if (!_posLoaded)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ POS no cargado - Intentando carga rápida...");

                    try
                    {
                        await LoadDataPuntoVenta();
                        System.Diagnostics.Debug.WriteLine($"✅ POS cargado para procesamiento de escáner");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error cargando POS: {ex.Message}");
                        TxtStatusPOS.Text = "❌ Error: POS no disponible para escáner";
                        return;
                    }
                }

                // Solo procesar si estamos en contexto POS
                if (e.Contexto != ScannerContext.PuntoVenta)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ RECHAZADO - Contexto incorrecto: {e.Contexto} (se requiere PuntoVenta)");
                    return;
                }

                // ✅ VERIFICACIÓN MEJORADA DE TAB
                if (MainTabControl.SelectedIndex != 1)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No estamos en tab POS - Cambiando automáticamente...");

                    // ✅ CAMBIAR AL TAB POS AUTOMÁTICAMENTE
                    Dispatcher.Invoke(() =>
                    {
                        MainTabControl.SelectedIndex = 1;
                    });

                    // Esperar un momento para que se active
                    await Task.Delay(500);
                }

                string codigo = e.CodigoBarras.Trim();
                System.Diagnostics.Debug.WriteLine($"✅ PROCESANDO código: '{codigo}'");

                TxtStatusPOS.Text = $"🔍 Buscando producto: {codigo}";

                // ✅ ACTIVAR PROTECCIÓN INMEDIATAMENTE
                _scannerProtection.OnProductoEscaneado(codigo, "Procesando...");

                // Buscar producto en lista POS
                var producto = _productosParaVenta.FirstOrDefault(p =>
                    p.CodigoBarras.Equals(codigo, StringComparison.OrdinalIgnoreCase));

                if (producto != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ PRODUCTO ENCONTRADO: {producto.NombreArticulo}");
                    _ultimoProductoEscaneado = producto;
                    _tiempoUltimoEscaneo = DateTime.Now;

                    // Verificar disponibilidad
                    if (!producto.DisponibleParaVenta)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Producto no disponible: {producto.NombreArticulo}");
                        TxtStatusPOS.Text = $"⚠️ Producto no disponible: {producto.NombreArticulo}";
                        MessageBox.Show($"El producto '{producto.NombreArticulo}' no está disponible para venta.\n\n" +
                                       $"Estado: {producto.EstadoProducto}",
                                       "Producto No Disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ✅ ACTUALIZAR PROTECCIÓN CON NOMBRE DEL PRODUCTO
                    _scannerProtection.OnProductoEscaneado(codigo, producto.NombreArticulo);

                    // Verificar si es producto por peso
                    if (EsProductoAGranel(producto.UnidadMedida))
                    {
                        System.Diagnostics.Debug.WriteLine($"📏 Producto por peso detectado: {producto.UnidadMedida}");

                        var pesoWindow = new IngresarPesoWindow(_context, producto, _basculaService);
                        if (pesoWindow.ShowDialog() == true)
                        {
                            await AgregarProductoAlCarrito(producto, pesoWindow.PesoIngresado);
                            TxtStatusPOS.Text = $"✅ Agregado: {producto.NombreArticulo} ({pesoWindow.PesoIngresado:F2} {producto.UnidadMedida})";
                        }
                        else
                        {
                            TxtStatusPOS.Text = "❌ Ingreso de peso cancelado";
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"📦 Producto por unidades: {producto.UnidadMedida}");

                        await AgregarProductoAlCarrito(producto, 1);
                        TxtStatusPOS.Text = $"✅ Agregado: {producto.NombreArticulo} (1 unidad)";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ PRODUCTO NO ENCONTRADO: {codigo}");
                    await ManejarProductoNoEncontrado(codigo);
                }

                System.Diagnostics.Debug.WriteLine($"🔍 ===== OnCodigoEscaneadoPOS_Protected COMPLETADO =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR CRÍTICO en OnCodigoEscaneadoPOS_Protected: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error procesando código: {ex.Message}";

                MessageBox.Show($"Error al procesar código de barras:\n\n{ex.Message}",
                               "Error de Escáner", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ HELPER: MANEJAR PRODUCTO NO ENCONTRADO
        private async Task ManejarProductoNoEncontrado(string codigo)
        {
            try
            {
                var resultado = MessageBox.Show(
                    $"❌ PRODUCTO NO ENCONTRADO\n\n" +
                    $"Código: {codigo}\n\n" +
                    $"¿Desea crear un nuevo producto con este código?",
                    "Producto No Encontrado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    TxtStatusPOS.Text = "📝 Abriendo formulario para nuevo producto...";
                    var selectorWindow = new TipoMaterialSelectorWindow(_context, codigo);
                    if (selectorWindow.ShowDialog() == true)
                    {
                        await RefrescarProductosAutomatico("nuevo producto desde escáner POS");
                        TxtStatusPOS.Text = "✅ Producto creado y disponible para venta";
                    }
                    else
                    {
                        TxtStatusPOS.Text = "❌ Creación de producto cancelada";
                    }
                }
                else
                {
                    TxtStatusPOS.Text = $"⚠️ Código ignorado: {codigo}";
                }
            }
            catch (Exception ex)
            {
                TxtStatusPOS.Text = $"❌ Error manejando producto no encontrado: {ex.Message}";
            }
        }

        // ========== CARGA DE DATOS POS ==========
        public async Task LoadDataPuntoVenta()
        {
            try
            {
                TxtStatusPOS.Text = "⏳ Cargando productos y servicios para venta...";

                // ✅ CARGAR PRODUCTOS (existente)
                _productosParaVenta = await _context.GetProductosDisponiblesParaVenta().ToListAsync();

                // ✅ NUEVO: CARGAR SERVICIOS INTEGRADOS AL POS
                _serviciosParaVenta = await _context.ServiciosVenta
          .Include(s => s.MaterialesNecesarios)
              .ThenInclude(m => m.RawMaterial)
          .Where(s => s.IntegradoPOS && s.Activo)
          .OrderBy(s => s.PrioridadPOS)
          .ThenBy(s => s.NombreServicio)
          .ToListAsync();

                // ✅ COMBINAR PRODUCTOS Y SERVICIOS EN UNA LISTA MIXTA
                _itemsPOS = new List<object>();
                _itemsPOS.AddRange(_productosParaVenta.Cast<object>());
                _itemsPOS.AddRange(_serviciosParaVenta.Cast<object>());

                // ✅ APLICAR FILTRO ACTUAL
                FiltrarItemsPOS();

                // Cargar estadísticas del día
                await LoadEstadisticasDelDia();

                // Actualizar contadores
                UpdateContadoresPOS();

                TxtStatusPOS.Text = $"✅ Sistema POS listo - {_productosParaVenta.Count} productos, {_serviciosParaVenta.Count} servicios";

                // ✅ CRÍTICO: SIEMPRE establecer como cargado, independientemente del escáner
                _posLoaded = true;

                System.Diagnostics.Debug.WriteLine($"✅ POS CARGADO COMPLETAMENTE - _posLoaded = {_posLoaded}");
                System.Diagnostics.Debug.WriteLine($"📊 Productos: {_productosParaVenta.Count}, Servicios: {_serviciosParaVenta.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en LoadDataPuntoVenta: {ex.Message}");

                MessageBox.Show($"Error al cargar datos POS: {ex.Message}",
                              "Error POS", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al cargar datos POS";

                // ✅ INCLUSO CON ERROR, marcar como cargado para permitir operaciones básicas
                _posLoaded = true;
                System.Diagnostics.Debug.WriteLine($"⚠️ POS marcado como cargado pese a errores para permitir operaciones");
            }
        }

        private void FiltrarItemsPOS()
        {
            try
            {
                string textoBusqueda = TxtBuscarPOS?.Text?.ToLower()?.Trim() ?? "";

                if (string.IsNullOrEmpty(textoBusqueda))
                {
                    // Mostrar todos los items
                    LstProductosPOS.ItemsSource = _itemsPOS;
                }
                else
                {
                    // Filtrar productos y servicios
                    var itemsFiltrados = _itemsPOS.Where(item =>
                    {
                        if (item is RawMaterial producto)
                        {
                            return producto.NombreArticulo.ToLower().Contains(textoBusqueda) ||
                                   producto.Categoria.ToLower().Contains(textoBusqueda) ||
                                   producto.CodigoBarras.ToLower().Contains(textoBusqueda);
                        }
                        else if (item is ServicioVenta servicio)
                        {
                            return servicio.NombreServicio.ToLower().Contains(textoBusqueda) ||
                                   servicio.CategoriaServicio.ToLower().Contains(textoBusqueda) ||
                                   servicio.CodigoServicio.ToLower().Contains(textoBusqueda);
                        }
                        return false;
                    }).ToList();

                    LstProductosPOS.ItemsSource = itemsFiltrados;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en FiltrarItemsPOS: {ex.Message}");
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
            try
            {
                // Actualizar contador de items disponibles (productos + servicios)
                var totalItems = (_productosParaVenta?.Count ?? 0) + (_serviciosParaVenta?.Count ?? 0);
                TxtCountProductos.Text = $"{totalItems} items";
                TxtProductosDisponibles.Text = $"Productos: {_productosParaVenta?.Count ?? 0}";

                // ✅ NUEVO: Contador de servicios en status bar
                if (TxtServiciosDisponibles != null)
                {
                    TxtServiciosDisponibles.Text = $"Servicios: {_serviciosParaVenta?.Count ?? 0}";
                }

                // Actualizar contador carrito
                TxtCountCarrito.Text = $"{_carritoItems.Count} artículos";

                // Calcular totales del carrito (funcionalidad existente)
                ActualizarTotalesCarrito();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en UpdateContadoresPOS: {ex.Message}");
            }
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
        

        private void BtnModoEscaner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ PROTECCIÓN ANTI-AUTOCLICK INMEDIATA
                var button = sender as Button;
                if (button != null)
                {
                    // Verificar si es click real vs automático
                    bool esClickReal = button.IsMouseOver ||
                                      System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed ||
                                      System.Windows.Input.Mouse.RightButton == MouseButtonState.Pressed;

                    if (!esClickReal)
                    {
                        System.Diagnostics.Debug.WriteLine("🛡️ CLICK AUTOMÁTICO DEL ESCÁNER BLOQUEADO en botón modo escáner");
                        return;
                    }

                    // Quitar foco inmediatamente para evitar futuros auto-clicks
                    this.Focus();
                    System.Diagnostics.Debug.WriteLine("✅ Click manual válido detectado en botón modo escáner");
                }

                // ✅ CORRECCIÓN: Era *modoEscanerActivo, ahora es _modoEscanerActivo
                _modoEscanerActivo = !_modoEscanerActivo;

                if (_modoEscanerActivo)
                {
                    // ✅ ACTIVAR MODO ESCÁNER
                    _unifiedScanner?.SetEnabled(true);
                    _unifiedScanner?.LimpiarBuffer(); // Limpiar cualquier buffer anterior

                    // Cambiar interfaz a ROJO/ACTIVO
                    BtnModoEscaner.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Rojo
                    TxtIconoEscaner.Text = "🔴";
                    TxtTextoEscaner.Text = "ESCÁNER ACTIVO";

                    // Actualizar estado en interfaz
                    if (TxtEstadoEscaner != null)
                    {
                        TxtEstadoEscaner.Text = "📱 ACTIVO";
                        TxtEstadoEscaner.Parent?.SetValue(Border.BackgroundProperty,
                            new SolidColorBrush(Color.FromRgb(34, 197, 94))); // Verde
                    }

                    TxtStatusPOS.Text = "📱 Escáner ACTIVADO - Listo para escanear productos";
                    System.Diagnostics.Debug.WriteLine("📱 MODO ESCÁNER ACTIVADO MANUALMENTE");
                }
                else
                {
                    // ✅ DESACTIVAR MODO ESCÁNER
                    _unifiedScanner?.SetEnabled(false);

                    // Cambiar interfaz a VERDE/INACTIVO
                    BtnModoEscaner.Background = new SolidColorBrush(Color.FromRgb(5, 150, 105)); // Verde
                    TxtIconoEscaner.Text = "📱";
                    TxtTextoEscaner.Text = "Activar Escáner";

                    // Actualizar estado en interfaz
                    if (TxtEstadoEscaner != null)
                    {
                        TxtEstadoEscaner.Text = "📱 INACTIVO";
                        TxtEstadoEscaner.Parent?.SetValue(Border.BackgroundProperty,
                            new SolidColorBrush(Color.FromRgb(107, 114, 128))); // Gris
                    }

                    TxtStatusPOS.Text = "🚫 Escáner DESACTIVADO - Sistema seguro, no procesará códigos";
                    System.Diagnostics.Debug.WriteLine("📱 MODO ESCÁNER DESACTIVADO MANUALMENTE");
                }

                // ✅ MOSTRAR INFORMACIÓN ADICIONAL EN DEBUG
                var statusInfo = _unifiedScanner?.GetStatusInfo() ?? "Sin información";
                System.Diagnostics.Debug.WriteLine($"📊 Estado del escáner: {statusInfo}");

                // ✅ FEEDBACK VISUAL ADICIONAL (OPCIONAL)
                // Pequeña animación del botón para confirmar el cambio
                if (button != null)
                {
                    var originalTransform = button.RenderTransform;
                    var scaleTransform = new ScaleTransform(0.95, 0.95);
                    button.RenderTransform = scaleTransform;

                    // Restaurar después de 100ms
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(100);
                    timer.Tick += (s, args) =>
                    {
                        button.RenderTransform = originalTransform;
                        timer.Stop();
                    };
                    timer.Start();
                }

                // ✅ LOG FINAL DE CONFIRMACIÓN
                System.Diagnostics.Debug.WriteLine($"🎯 CAMBIO DE MODO COMPLETADO - Nuevo estado: {(_modoEscanerActivo ? "ACTIVO" : "INACTIVO")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en BtnModoEscaner_Click: {ex}");
                MessageBox.Show($"Error al cambiar modo escáner: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // En caso de error, restaurar estado visual
                TxtStatusPOS.Text = "❌ Error al cambiar modo escáner";
            }
        }
        private void VerificarEstadoEscaner()
        {
            try
            {
                if (_unifiedScanner != null)
                {
                    var statusInfo = _unifiedScanner.GetStatusInfo();
                    System.Diagnostics.Debug.WriteLine($"🔍 Verificación estado escáner: {statusInfo}");

                    // Sincronizar interfaz con estado real
                    if (statusInfo.Contains("DESHABILITADO") && _modoEscanerActivo)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Desincronización detectada - Corrigiendo interfaz");
                        _modoEscanerActivo = false;
                        BtnModoEscaner.Background = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                        TxtIconoEscaner.Text = "📱";
                        TxtTextoEscaner.Text = "Activar Escáner";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error verificando estado escáner: {ex.Message}");
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
        private async Task<decimal> CalcularDescuentoPromociones()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 DEBUG: Iniciando CalcularDescuentoPromociones() EXPANDIDO");

                // Obtener promociones automáticas vigentes
                var promocionesVigentes = await _context.GetPromocionesVigentes()
                    .Where(p => p.AplicacionAutomatica)
                    .OrderBy(p => p.Prioridad) // Aplicar por prioridad
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Promociones vigentes encontradas: {promocionesVigentes.Count}");

                if (!promocionesVigentes.Any())
                {
                    System.Diagnostics.Debug.WriteLine("❌ DEBUG: No hay promociones vigentes");
                    return 0;
                }

                decimal descuentoTotal = 0;

                // ✅ PROCESAR CADA TIPO DE PROMOCIÓN
                foreach (var promocion in promocionesVigentes)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Procesando promoción: {promocion.NombrePromocion} (Tipo: {promocion.TipoPromocion})");

                    decimal descuentoPromocion = promocion.TipoPromocion switch
                    {
                        "DescuentoPorcentaje" => await CalcularDescuentoPorcentaje(promocion),
                        "DescuentoFijo" => await CalcularDescuentoFijo(promocion),
                        "Cantidad" => await CalcularDescuentoCantidad(promocion),
                        "CompraYLleva" => await CalcularDescuentoCompraYLleva(promocion),
                        _ => 0
                    };

                    if (descuentoPromocion > 0)
                    {
                        descuentoTotal += descuentoPromocion;
                        System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Promoción aplicada: {promocion.NombrePromocion} - Descuento: ${descuentoPromocion:F2}");

                        // Registrar uso de la promoción (opcional)
                        // promocion.RegistrarUso(); // Descomentar si quieres trackear usos
                    }

                    // Si no es combinable y ya se aplicó una promoción, terminar
                    /*
  if (!promocion.Combinable && descuentoTotal > 0)
  {
      System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Promoción no combinable aplicada, terminando");
      break;
  }
  */
                }

                System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Descuento TOTAL final: ${descuentoTotal:F2}");
                return descuentoTotal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Error calculando promociones: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Calcula descuento por porcentaje (funcionalidad original)
        /// </summary>
        private async Task<decimal> CalcularDescuentoPorcentaje(PromocionVenta promocion)
        {
            try
            {
                decimal subtotal = _carritoItems.Sum(i => i.SubTotal);
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: DescuentoPorcentaje - Subtotal: ${subtotal}, Monto mín: ${promocion.MontoMinimo}");

                if (subtotal >= promocion.MontoMinimo)
                {
                    decimal descuento = subtotal * (promocion.ValorPromocion / 100);

                    // Aplicar límite máximo si existe
                    if (promocion.DescuentoMaximo > 0)
                        descuento = Math.Min(descuento, promocion.DescuentoMaximo);

                    return descuento;
                }

                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Error en DescuentoPorcentaje: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Calcula descuento fijo
        /// </summary>
        private async Task<decimal> CalcularDescuentoFijo(PromocionVenta promocion)
        {
            try
            {
                decimal subtotal = _carritoItems.Sum(i => i.SubTotal);
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: DescuentoFijo - Subtotal: ${subtotal}, Monto mín: ${promocion.MontoMinimo}");

                if (subtotal >= promocion.MontoMinimo)
                {
                    // El descuento fijo no puede ser mayor al subtotal
                    return Math.Min(promocion.ValorPromocion, subtotal);
                }

                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Error en DescuentoFijo: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Calcula descuento por cantidad (PRINCIPAL)
        /// </summary>
        private async Task<decimal> CalcularDescuentoCantidad(PromocionVenta promocion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: CalcularDescuentoCantidad iniciado");
                System.Diagnostics.Debug.WriteLine($"   - Promoción: {promocion.NombrePromocion}");
                System.Diagnostics.Debug.WriteLine($"   - Cantidad mínima: {promocion.CantidadMinima}");
                System.Diagnostics.Debug.WriteLine($"   - Precio promocional: ${promocion.ValorPromocion:F2}");
                System.Diagnostics.Debug.WriteLine($"   - Productos aplicables: '{promocion.ProductosAplicables}'");

                // Verificar que tiene productos específicos
                if (string.IsNullOrEmpty(promocion.ProductosAplicables))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DEBUG: No hay productos específicos definidos");
                    return 0;
                }

                // Obtener IDs de productos aplicables
                var productIds = promocion.ProductosAplicables.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => int.TryParse(p, out _))
                    .Select(p => int.Parse(p))
                    .ToList();

                if (!productIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DEBUG: No se pudieron parsear los IDs de productos");
                    return 0;
                }

                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Productos aplicables: [{string.Join(", ", productIds)}]");

                decimal descuentoTotal = 0;

                // Buscar items en el carrito que apliquen
                foreach (var productId in productIds)
                {
                    var itemsCarrito = _carritoItems.Where(i =>
                        i.RawMaterialId == productId && i.EsProducto).ToList();

                    if (!itemsCarrito.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Producto {productId} no está en el carrito");
                        continue;
                    }

                    // Sumar cantidad total del producto en el carrito
                    decimal cantidadTotalCarrito = itemsCarrito.Sum(i => i.Cantidad);
                    System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Producto {productId} - Cantidad en carrito: {cantidadTotalCarrito}");

                    // Verificar si cumple la cantidad mínima
                    if (cantidadTotalCarrito >= promocion.CantidadMinima)
                    {
                        // Calcular cuántas "promociones completas" se pueden aplicar
                        int promocionesCompletas = (int)(cantidadTotalCarrito / promocion.CantidadMinima);
                        decimal cantidadPromocional = promocionesCompletas * promocion.CantidadMinima;

                        System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Promociones completas: {promocionesCompletas}");
                        System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Cantidad promocional: {cantidadPromocional}");

                        // Obtener precio normal del producto
                        var itemMuestra = itemsCarrito.First();
                        decimal precioNormalUnitario = itemMuestra.PrecioUnitario;
                        decimal precioNormalTotal = cantidadPromocional * precioNormalUnitario;
                        decimal precioPromocionalTotal = promocionesCompletas * promocion.ValorPromocion;

                        System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Precio normal total: ${precioNormalTotal:F2}");
                        System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: Precio promocional total: ${precioPromocionalTotal:F2}");

                        // El descuento es la diferencia
                        decimal descuentoProducto = Math.Max(0, precioNormalTotal - precioPromocionalTotal);
                        descuentoTotal += descuentoProducto;

                        System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Descuento para producto {productId}: ${descuentoProducto:F2}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Producto {productId} no cumple cantidad mínima ({cantidadTotalCarrito} < {promocion.CantidadMinima})");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Descuento total por cantidad: ${descuentoTotal:F2}");
                return descuentoTotal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Error en CalcularDescuentoCantidad: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Calcula descuento compra y lleva (2x1, 3x2, etc.)
        /// </summary>
        private async Task<decimal> CalcularDescuentoCompraYLleva(PromocionVenta promocion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG: CalcularDescuentoCompraYLleva iniciado");
                System.Diagnostics.Debug.WriteLine($"   - Promoción: {promocion.NombrePromocion}");
                System.Diagnostics.Debug.WriteLine($"   - Compra: {promocion.CantidadMinima}");
                System.Diagnostics.Debug.WriteLine($"   - Lleva: {promocion.ValorPromocion}");

                // ValorPromocion contiene la cantidad que se lleva
                // CantidadMinima contiene la cantidad que se paga
                int compra = promocion.CantidadMinima;
                int lleva = (int)promocion.ValorPromocion;
                int gratis = lleva - compra;

                if (gratis <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ DEBUG: No hay productos gratis (lleva {lleva} - compra {compra} = {gratis})");
                    return 0;
                }

                // Si no hay productos específicos, aplicar a todos
                var productIds = new List<int>();
                if (!string.IsNullOrEmpty(promocion.ProductosAplicables))
                {
                    productIds = promocion.ProductosAplicables.Split(',')
                        .Select(p => p.Trim())
                        .Where(p => int.TryParse(p, out _))
                        .Select(p => int.Parse(p))
                        .ToList();
                }

                decimal descuentoTotal = 0;

                // Si hay productos específicos, solo aplicar a esos
                if (productIds.Any())
                {
                    foreach (var productId in productIds)
                    {
                        var itemsCarrito = _carritoItems.Where(i =>
                            i.RawMaterialId == productId && i.EsProducto).ToList();

                        if (itemsCarrito.Any())
                        {
                            decimal cantidadTotal = itemsCarrito.Sum(i => i.Cantidad);
                            int promocionesCompletas = (int)(cantidadTotal / lleva);
                            int productosGratis = promocionesCompletas * gratis;

                            if (productosGratis > 0)
                            {
                                decimal precioUnitario = itemsCarrito.First().PrecioUnitario;
                                decimal descuentoProducto = productosGratis * precioUnitario;
                                descuentoTotal += descuentoProducto;

                                System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Producto {productId} - {productosGratis} gratis = ${descuentoProducto:F2}");
                            }
                        }
                    }
                }
                else
                {
                    // Aplicar a todos los productos del carrito
                    var productosAgrupados = _carritoItems.Where(i => i.EsProducto)
                        .GroupBy(i => i.RawMaterialId)
                        .ToList();

                    foreach (var grupo in productosAgrupados)
                    {
                        decimal cantidadTotal = grupo.Sum(i => i.Cantidad);
                        int promocionesCompletas = (int)(cantidadTotal / lleva);
                        int productosGratis = promocionesCompletas * gratis;

                        if (productosGratis > 0)
                        {
                            decimal precioUnitario = grupo.First().PrecioUnitario;
                            decimal descuentoProducto = productosGratis * precioUnitario;
                            descuentoTotal += descuentoProducto;

                            System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Producto {grupo.Key} - {productosGratis} gratis = ${descuentoProducto:F2}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🎁 DEBUG: Descuento total compra y lleva: ${descuentoTotal:F2}");
                return descuentoTotal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DEBUG: Error en CalcularDescuentoCompraYLleva: {ex.Message}");
                return 0;
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
                System.Diagnostics.Debug.WriteLine("🔍 === ActualizarTotalesCarrito ===");

                if (_carritoItems.Any())
                {
                    // ✅ Calcular subtotal
                    decimal subtotal = _carritoItems.Sum(i => i.SubTotal);
                    System.Diagnostics.Debug.WriteLine($"🔍 Subtotal: ${subtotal:F2}");

                    // ✅ Descuentos automáticos por productos
                    decimal descuentoProductos = _carritoItems.Sum(i => i.DescuentoAplicado * i.Cantidad);
                    System.Diagnostics.Debug.WriteLine($"🔍 Descuentos productos: ${descuentoProductos:F2}");

                    // ✅ Descuentos por promociones (llamar método sin await)
                    decimal descuentoPromociones = 0;
                    try
                    {
                        // Llamar método de promociones de forma sincrónica
                        var task = CalcularDescuentoPromociones();
                        descuentoPromociones = task.Result; // ⚠️ Solo para simplificar - no ideal pero funciona
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error calculando promociones: {ex.Message}");
                        descuentoPromociones = 0;
                    }

                    // ✅ Total de descuentos
                    decimal descuentoTotal = descuentoProductos + descuentoPromociones;
                    System.Diagnostics.Debug.WriteLine($"🔍 Descuento total: ${descuentoTotal:F2}");

                    // ✅ IVA incluido
                    decimal ivaIncluido = subtotal - (subtotal / 1.16m);

                    // ✅ Total final
                    decimal totalFinal = Math.Max(0, subtotal - descuentoTotal);
                    System.Diagnostics.Debug.WriteLine($"🔍 Total final: ${totalFinal:F2}");

                    // ✅ Actualizar interfaz
                    TxtSubTotal.Text = subtotal.ToString("C2");
                    TxtDescuento.Text = descuentoTotal.ToString("C2");
                    TxtIVA.Text = $"{ivaIncluido:C2} (incluido)";
                    TxtTotal.Text = totalFinal.ToString("C2");

                    // Análisis costo-beneficio
                    var gananciaTotal = _carritoItems.Sum(i => i.GananciaLinea);
                    var margenPromedio = _carritoItems.Any() ? _carritoItems.Average(i => i.MargenPorcentaje) : 0;

                    TxtGananciaPrevista.Text = $"Ganancia: {gananciaTotal:C2}";
                    TxtMargenPromedio.Text = $"Margen: {margenPromedio:F1}%";

                    // Status
                    if (descuentoTotal > 0)
                    {
                        TxtStatusPOS.Text = $"🎁 Descuentos aplicados - Total: ${descuentoTotal:F2}";
                    }
                }
                else
                {
                    // Carrito vacío
                    TxtSubTotal.Text = "$0.00";
                    TxtDescuento.Text = "$0.00";
                    TxtIVA.Text = "$0.00 (incluido)";
                    TxtTotal.Text = "$0.00";
                    TxtGananciaPrevista.Text = "Ganancia: $0.00";
                    TxtMargenPromedio.Text = "Margen: 0%";
                }

                System.Diagnostics.Debug.WriteLine("✅ ActualizarTotalesCarrito completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ActualizarTotalesCarrito: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error al calcular totales";

                // Valores seguros en caso de error
                TxtSubTotal.Text = "Error";
                TxtDescuento.Text = "Error";
                TxtIVA.Text = "Error";
                TxtTotal.Text = "Error";
            }
        }
        // ✅ MÉTODO ÚNICO - PROCESAMIENTO DE VENTA (CORREGIDO)
        private async Task<bool> ProcesarVentaUnico()
        {
            using var ventaContext = new AppDbContext();
            using var transaction = await ventaContext.Database.BeginTransactionAsync();

            try
            {
                System.Diagnostics.Debug.WriteLine("🏪 === INICIANDO PROCESAMIENTO DE VENTA ===");

                // ✅ VALIDAR CARRITO NO VACÍO Y VERIFICAR COHERENCIA
                if (!_carritoItems.Any())
                {
                    MessageBox.Show("El carrito está vacío.", "Carrito Vacío",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                // ✅ VERIFICAR ESTADO COMPLETO DEL CARRITO ANTES DE PROCESAR
                System.Diagnostics.Debug.WriteLine("🔍 Verificando estado del carrito antes de procesar venta...");
                VerificarEstadoCarritoCompleto();

                if (!ValidarCoherenciaCarrito())
                {
                    MessageBox.Show("❌ Se detectaron inconsistencias en el carrito.\n\n" +
                                   "Por favor, revise los productos y descuentos aplicados.\n" +
                                   "Consulte la consola de debug para más detalles.",
                                   "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // ✅ VALIDAR DESCUENTOS ANTES DE PROCESAR
                System.Diagnostics.Debug.WriteLine("🔍 Validando descuentos aplicados...");
                foreach (var item in _carritoItems)
                {
                    if (item.TieneDescuentoManual && !item.ValidarDescuento())
                    {
                        MessageBox.Show($"❌ Descuento inválido en {item.NombreProducto}.\n\n" +
                                       "Por favor, revise los descuentos aplicados.",
                                       "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                // ✅ CALCULAR TOTALES CORRECTAMENTE
                // IMPORTANTE: SubTotal ya incluye los descuentos aplicados con AplicarDescuentoConAuditoria
                decimal subtotal = _carritoItems.Sum(i => i.SubTotal);
                decimal descuentoPromociones = await CalcularDescuentoPromociones();

                // ✅ NO restar descuentos de productos porque ya están incluidos en SubTotal
                decimal totalFinal = subtotal - descuentoPromociones;

                // ✅ CALCULAR DESCUENTOS TOTALES PARA INFORMACIÓN
                decimal totalDescuentosAplicados = _carritoItems.Sum(i => i.TotalDescuentoLinea);
                decimal totalOriginalSinDescuentos = _carritoItems.Sum(i => i.PrecioOriginal * i.Cantidad);

                System.Diagnostics.Debug.WriteLine($"💰 CÁLCULOS DE VENTA CORREGIDOS:");
                System.Diagnostics.Debug.WriteLine($"   • Total original (sin desc.): ${totalOriginalSinDescuentos:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Descuentos aplicados: ${totalDescuentosAplicados:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Subtotal (ya con desc.): ${subtotal:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Desc. promocionales: ${descuentoPromociones:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Total final: ${totalFinal:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Verificación: ${totalOriginalSinDescuentos - totalDescuentosAplicados - descuentoPromociones:F2}");

                // ✅ ABRIR VENTANA DE PAGO
                var pagoWindow = new ProcesarPagoWindow(totalFinal, TxtCliente.Text.Trim());
                if (pagoWindow.ShowDialog() != true)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Pago cancelado por usuario");
                    return false;
                }

               
                // ✅ VERIFICAR STOCK - productos y servicios por separado
                System.Diagnostics.Debug.WriteLine("📦 Verificando disponibilidad de stock...");
                var productosParaActualizar = new List<(RawMaterial producto, decimal cantidad)>();
                var serviciosParaActualizar = new List<(ServicioVenta servicio, decimal cantidad)>();

                foreach (var item in _carritoItems)
                {
                    if (item.EsProducto)
                    {
                        // Es un producto
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
                        System.Diagnostics.Debug.WriteLine($"✅ Producto verificado: {item.NombreProducto} x{item.Cantidad}");
                    }
                    else if (item.EsServicio)
                    {
                        // Es un servicio
                        var servicio = await ventaContext.ServiciosVenta
                            .Include(s => s.MaterialesNecesarios)
                                .ThenInclude(m => m.RawMaterial)
                            .FirstOrDefaultAsync(s => s.Id == item.ServicioVentaId);

                        if (servicio == null)
                        {
                            throw new InvalidOperationException($"Servicio no encontrado: {item.NombreProducto}");
                        }

                        System.Diagnostics.Debug.WriteLine($"🔍 SERVICIO: {servicio.NombreServicio}");
                        System.Diagnostics.Debug.WriteLine($"🔍 MATERIALES ENCONTRADOS: {servicio.MaterialesNecesarios?.Count ?? 0}");
                        foreach (var mat in servicio.MaterialesNecesarios ?? new List<MaterialServicio>())
                        {
                            System.Diagnostics.Debug.WriteLine($"   - {mat.RawMaterial?.NombreArticulo}: {mat.CantidadNecesaria} {mat.UnidadMedida}");
                        }

                        if (!servicio.DisponibleParaVenta)
                        {
                            throw new InvalidOperationException($"Servicio no disponible: {servicio.NombreServicio}");
                        }

                        serviciosParaActualizar.Add((servicio, item.Cantidad));
                        System.Diagnostics.Debug.WriteLine($"✅ Servicio verificado: {item.NombreProducto} x{item.Cantidad}");
                    }
                }

                // ✅ CREAR LA VENTA
                System.Diagnostics.Debug.WriteLine("📝 Creando registro de venta...");
                var venta = new Venta
                {
                    Cliente = pagoWindow.NombreCliente,
                    Usuario = UserService.UsuarioActual?.NombreUsuario ?? Environment.UserName,
                    FormaPago = pagoWindow.FormaPagoFinal,
                    Estado = "Completada",
                    Observaciones = pagoWindow.DetallesPago
                };

                // ✅ CONFIGURAR FORMAS DE PAGO Y COMISIONES
                venta.EstablecerFormasPago(
                    pagoWindow.MontoEfectivo,
                    pagoWindow.MontoTarjeta,
                    pagoWindow.MontoTransferencia
                );

                if (pagoWindow.ComisionTarjeta > 0)
                {
                    venta.CalcularComisiones(pagoWindow.PorcentajeComisionTarjeta);
                    venta.CalcularIVAComision(pagoWindow.IVAComision > 0);
                    System.Diagnostics.Debug.WriteLine($"🏦 Comisiones calculadas: ${venta.ComisionTotal:F2}");
                }

                // ✅ AGREGAR DETALLES DE VENTA CON INFORMACIÓN DE DESCUENTOS CORREGIDA
                System.Diagnostics.Debug.WriteLine("📋 Agregando detalles de venta...");
                foreach (var item in _carritoItems)
                {
                    var detalleVenta = new DetalleVenta
                    {
                        RawMaterialId = item.RawMaterialId,
                        ServicioVentaId = item.ServicioVentaId,
                        NombreProducto = item.NombreProducto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario, // Ya incluye descuento si se aplicó
                        UnidadMedida = item.UnidadMedida,
                        CostoUnitario = item.CostoUnitario,
                        PorcentajeIVA = item.PorcentajeIVA,

                        // ✅ IMPORTANTE: DescuentoAplicado debe ser 0 si ya se aplicó descuento manual
                        DescuentoAplicado = item.TieneDescuentoManual ? 0 : item.DescuentoAplicado,

                        // ✅ INFORMACIÓN DE AUDITORÍA DE DESCUENTOS
                        PrecioOriginal = item.PrecioOriginal > 0 ? item.PrecioOriginal : item.PrecioUnitario,
                        DescuentoUnitario = item.DescuentoUnitario,
                        MotivoDescuentoDetalle = item.MotivoDescuentoDetalle ?? "",
                        TieneDescuentoManual = item.TieneDescuentoManual
                    };

                    detalleVenta.CalcularSubTotal();
                    venta.AgregarDetalle(detalleVenta);

                    // ✅ DEBUG INFORMACIÓN DETALLADA
                    System.Diagnostics.Debug.WriteLine($"📦 {item.NombreProducto}:");
                    System.Diagnostics.Debug.WriteLine($"   • Cantidad: {item.Cantidad:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Precio unitario: ${item.PrecioUnitario:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • SubTotal: ${detalleVenta.SubTotal:F2}");

                    if (item.TieneDescuentoManual)
                    {
                        System.Diagnostics.Debug.WriteLine($"   🎁 DESCUENTO MANUAL:");
                        System.Diagnostics.Debug.WriteLine($"      • Precio original: ${item.PrecioOriginal:F2}");
                        System.Diagnostics.Debug.WriteLine($"      • Descuento unitario: ${item.DescuentoUnitario:F2}");
                        System.Diagnostics.Debug.WriteLine($"      • Total descuento línea: ${item.TotalDescuentoLinea:F2}");
                        System.Diagnostics.Debug.WriteLine($"      • Motivo: {item.MotivoDescuentoDetalle}");
                        System.Diagnostics.Debug.WriteLine($"      • Ahorro: ${(item.PrecioOriginal - item.PrecioUnitario) * item.Cantidad:F2}");
                    }
                }

                // ✅ REGISTRAR INFORMACIÓN DE AUDITORÍA DE DESCUENTOS EN LA VENTA
                if (_descuentoInfo != null)
                {
                    System.Diagnostics.Debug.WriteLine("🔍 Registrando auditoría de descuentos...");
                    venta.EstablecerAuditoriaDescuento(
                        _descuentoInfo.UsuarioAutorizador,
                        _descuentoInfo.TipoUsuario,
                        _descuentoInfo.Motivo
                    );

                    System.Diagnostics.Debug.WriteLine($"🎁 Auditoría registrada:");
                    System.Diagnostics.Debug.WriteLine($"   • Usuario: {venta.UsuarioAutorizadorDescuento} ({venta.TipoUsuarioAutorizador})");
                    System.Diagnostics.Debug.WriteLine($"   • Motivo: {venta.MotivoDescuentoGeneral}");
                    System.Diagnostics.Debug.WriteLine($"   • Total descuentos: ${venta.TotalDescuentosAplicados:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Porcentaje: {venta.PorcentajeDescuentoTotal:F2}%");
                    System.Diagnostics.Debug.WriteLine($"   • Items con descuento: {venta.CantidadItemsConDescuento}");
                }

                // ✅ CALCULAR TOTALES Y GENERAR TICKET
                venta.CalcularTotales();
                venta.GenerarNumeroTicket();

                System.Diagnostics.Debug.WriteLine($"🎫 Ticket generado: #{venta.NumeroTicket}");
                System.Diagnostics.Debug.WriteLine($"💰 Total venta: ${venta.Total:F2}");
                System.Diagnostics.Debug.WriteLine($"📈 Ganancia: ${venta.GananciaBruta:F2}");

                // ✅ AGREGAR VENTA A LA BASE DE DATOS
                ventaContext.Ventas.Add(venta);
                await ventaContext.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("✅ Venta guardada en base de datos");

                // ✅ ACTUALIZAR STOCK DE PRODUCTOS
                System.Diagnostics.Debug.WriteLine("📦 Actualizando stock de productos...");
                foreach (var (producto, cantidad) in productosParaActualizar)
                {
                    if (!producto.ReducirStock(cantidad))
                    {
                        throw new InvalidOperationException($"Error al reducir stock de {producto.NombreArticulo}");
                    }

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
                    System.Diagnostics.Debug.WriteLine($"📦 Stock reducido: {producto.NombreArticulo} -{cantidad}");
                }

                // ✅ ACTUALIZAR STOCK DE SERVICIOS Y CONSUMIR MATERIALES
                System.Diagnostics.Debug.WriteLine("🛍️ Procesando servicios y consumiendo materiales...");
                foreach (var (servicio, cantidad) in serviciosParaActualizar)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 PROCESANDO SERVICIO: {servicio.NombreServicio} x{cantidad}");
                    System.Diagnostics.Debug.WriteLine($"🔄 MATERIALES A CONSUMIR: {servicio.MaterialesNecesarios?.Count ?? 0}");

                    if (!servicio.ReducirStock(cantidad))
                    {
                        throw new InvalidOperationException($"Error al reducir stock de servicio {servicio.NombreServicio}");
                    }

                    // Consumir materiales del servicio
                    foreach (var materialServicio in servicio.MaterialesNecesarios)
                    {
                        var cantidadNecesaria = materialServicio.CantidadNecesaria * cantidad;
                        System.Diagnostics.Debug.WriteLine($"  🔹 Consumiendo: {materialServicio.RawMaterial?.NombreArticulo} - {cantidadNecesaria:F2} {materialServicio.UnidadMedida}");
                        System.Diagnostics.Debug.WriteLine($"  🔹 Stock actual: {materialServicio.RawMaterial?.StockTotal:F2}");

                        if (materialServicio.RawMaterial.StockTotal < cantidadNecesaria)
                        {
                            throw new InvalidOperationException(
                                $"Stock insuficiente del material '{materialServicio.RawMaterial.NombreArticulo}' " +
                                $"para el servicio '{servicio.NombreServicio}'. " +
                                $"Necesario: {cantidadNecesaria:F2}, Disponible: {materialServicio.RawMaterial.StockTotal:F2}");
                        }

                        if (!materialServicio.RawMaterial.ReducirStock(cantidadNecesaria))
                        {
                            throw new InvalidOperationException($"Error al reducir stock del material {materialServicio.RawMaterial.NombreArticulo}");
                        }

                        var movimientoMaterial = Movimiento.CrearMovimientoVenta(
                            materialServicio.RawMaterial.Id,
                            cantidadNecesaria,
                            $"Servicio: {servicio.NombreServicio} - Ticket #{venta.NumeroTicket}",
                            Environment.UserName,
                            materialServicio.RawMaterial.PrecioConIVA,
                            materialServicio.RawMaterial.UnidadMedida,
                            venta.NumeroTicket.ToString(),
                            venta.Cliente);

                        ventaContext.Movimientos.Add(movimientoMaterial);
                        System.Diagnostics.Debug.WriteLine($"  ✅ Material consumido: {materialServicio.RawMaterial.NombreArticulo} -{cantidadNecesaria:F2}");
                    }
                }

                // ✅ GUARDAR TODOS LOS CAMBIOS
                await ventaContext.SaveChangesAsync();
                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine("✅ Transacción confirmada exitosamente");

                // ✅ LIMPIAR INTERFAZ Y VARIABLES COMPLETAMENTE
                LimpiarCarritoCompleto(); // Usa el método completo en lugar de solo .Clear()
                await LoadEstadisticasDelDia();
                await RefrescarProductosAutomatico("stock actualizado después de venta");

                System.Diagnostics.Debug.WriteLine("🔄 Interfaz actualizada y limpiada");

                // ✅ IMPRIMIR TICKET
                try
                {
                    System.Diagnostics.Debug.WriteLine("🖨️ Intentando imprimir ticket...");
                    await _ticketPrinter.ImprimirTicket(venta, "Impresora_POS");
                    System.Diagnostics.Debug.WriteLine("✅ Ticket impreso correctamente");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error al imprimir: {ex.Message}");
                    MessageBox.Show($"Venta procesada correctamente.\nAdvertencia al imprimir: {ex.Message}",
                                  "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // ✅ MOSTRAR CONFIRMACIÓN CON INFORMACIÓN DE DESCUENTOS
                string mensaje = $"✅ VENTA PROCESADA EXITOSAMENTE!\n\n" +
                                $"📄 Ticket: #{venta.NumeroTicket}\n" +
                                $"👤 Cliente: {venta.Cliente}\n" +
                                $"💰 Total: {venta.Total:C2}\n" +
                                $"📊 Ganancia: {venta.GananciaBruta:C2}";

                // ✅ AGREGAR INFORMACIÓN DE COMISIONES
                if (venta.ComisionTotal > 0)
                {
                    mensaje += $"\n🏦 Comisión: {venta.ComisionTotal:C2}\n" +
                              $"💵 Neto recibido: {venta.TotalRealRecibido:C2}";
                }

                // ✅ AGREGAR INFORMACIÓN DE DESCUENTOS SI APLICA
                if (venta.TieneDescuentosAplicados)
                {
                    mensaje += $"\n\n🎁 DESCUENTOS APLICADOS:";
                    mensaje += $"\n   • Total descuentos: ${venta.TotalDescuentosAplicados:C2}";
                    mensaje += $"\n   • Porcentaje: {venta.PorcentajeDescuentoTotal:F1}%";
                    mensaje += $"\n   • Items con descuento: {venta.CantidadItemsConDescuento}";
                    mensaje += $"\n   • Autorizado por: {venta.UsuarioAutorizadorDescuento} ({venta.TipoUsuarioAutorizador})";
                    mensaje += $"\n   • Motivo: {venta.MotivoDescuentoGeneral}";
                    mensaje += $"\n   • Fecha: {venta.FechaHoraDescuento?.ToString("dd/MM/yyyy HH:mm")}";
                }

                MessageBox.Show(mensaje, "Venta Completada",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ ACTUALIZAR STATUS CON INFORMACIÓN DE DESCUENTOS
                var statusDescuento = venta.TieneDescuentosAplicados
                    ? $" (Desc: ${venta.TotalDescuentosAplicados:F2})"
                    : "";

                TxtStatusPOS.Text = $"✅ Venta #{venta.NumeroTicket} completada - {venta.Total:C2}{statusDescuento}";

                System.Diagnostics.Debug.WriteLine("🎉 === VENTA PROCESADA EXITOSAMENTE ===");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR EN PROCESAMIENTO: {ex}");

                try
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine("🔄 Transacción revertida exitosamente");
                }
                catch (Exception rollbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 ERROR EN ROLLBACK: {rollbackEx.Message}");
                }

                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"❌ Error al procesar venta:\n\n{errorMsg}\n\nTodos los cambios han sido revertidos.",
                               "Error de Venta", MessageBoxButton.OK, MessageBoxImage.Error);

                TxtStatusPOS.Text = "❌ Error al procesar venta - Sistema restaurado";
                return false;
            }
        }

        // ===== AGREGAR ESTOS MÉTODOS A MainWindow.xaml.cs =====

        #region Métodos de Debug y Validación del Carrito

        /// <summary>
        /// Muestra información detallada del carrito para debugging
        /// </summary>
        private void MostrarInfoDebugCarrito(string momento = "")
        {
            if (!System.Diagnostics.Debugger.IsAttached) return;

            System.Diagnostics.Debug.WriteLine($"🛒 === INFO CARRITO {momento.ToUpper()} ===");

            if (!_carritoItems.Any())
            {
                System.Diagnostics.Debug.WriteLine("   Carrito vacío");
                return;
            }

            decimal totalOriginal = 0;
            decimal totalDescuentos = 0;
            decimal totalFinal = 0;

            foreach (var item in _carritoItems)
            {
                var precioOriginalReal = item.PrecioOriginal > 0 ? item.PrecioOriginal : item.PrecioUnitario;
                var subtotalOriginal = precioOriginalReal * item.Cantidad;
                var descuentoLinea = item.TotalDescuentoLinea;

                totalOriginal += subtotalOriginal;
                totalDescuentos += descuentoLinea;
                totalFinal += item.SubTotal;

                System.Diagnostics.Debug.WriteLine($"📦 {item.NombreProducto}:");
                System.Diagnostics.Debug.WriteLine($"   • Cantidad: {item.Cantidad:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Precio original: ${precioOriginalReal:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Precio actual: ${item.PrecioUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"   • SubTotal original: ${subtotalOriginal:F2}");
                System.Diagnostics.Debug.WriteLine($"   • SubTotal actual: ${item.SubTotal:F2}");

                if (item.TieneDescuentoManual)
                {
                    System.Diagnostics.Debug.WriteLine($"   🎁 Descuento unitario: ${item.DescuentoUnitario:F2}");
                    System.Diagnostics.Debug.WriteLine($"   🎁 Total descuento línea: ${descuentoLinea:F2}");
                    System.Diagnostics.Debug.WriteLine($"   🎁 Motivo: {item.MotivoDescuentoDetalle}");
                }

                if (item.DescuentoAplicado > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"   ⚠️  DescuentoAplicado: ${item.DescuentoAplicado:F2} (debería ser 0)");
                }

                System.Diagnostics.Debug.WriteLine("");
            }

            System.Diagnostics.Debug.WriteLine($"💰 TOTALES:");
            System.Diagnostics.Debug.WriteLine($"   • Total original: ${totalOriginal:F2}");
            System.Diagnostics.Debug.WriteLine($"   • Total descuentos: ${totalDescuentos:F2}");
            System.Diagnostics.Debug.WriteLine($"   • Total final: ${totalFinal:F2}");
            System.Diagnostics.Debug.WriteLine($"   • Verificación: ${totalOriginal - totalDescuentos:F2}");
            System.Diagnostics.Debug.WriteLine($"   • Diferencia: ${Math.Abs(totalFinal - (totalOriginal - totalDescuentos)):F2}");

            if (Math.Abs(totalFinal - (totalOriginal - totalDescuentos)) > 0.01m)
            {
                System.Diagnostics.Debug.WriteLine("   ❌ INCONSISTENCIA DETECTADA");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("   ✅ Cálculos coherentes");
            }

            System.Diagnostics.Debug.WriteLine($"🛒 === FIN INFO CARRITO ===\n");
        }

        /// <summary>
        /// Valida la coherencia de todos los items del carrito
        /// </summary>
        private bool ValidarCoherenciaCarrito()
        {
            bool esCoherente = true;

            System.Diagnostics.Debug.WriteLine("🔍 === VALIDANDO COHERENCIA DEL CARRITO ===");

            foreach (var item in _carritoItems)
            {
                // ✅ VALIDAR PRECIOS
                if (item.PrecioUnitario <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ {item.NombreProducto}: Precio unitario inválido (${item.PrecioUnitario:F2})");
                    esCoherente = false;
                }

                // ✅ VALIDAR CANTIDADES
                if (item.Cantidad <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ {item.NombreProducto}: Cantidad inválida ({item.Cantidad:F2})");
                    esCoherente = false;
                }

                // ✅ VALIDAR SUBTOTAL
                var subtotalEsperado = item.PrecioUnitario * item.Cantidad - item.DescuentoAplicado;
                if (Math.Abs(item.SubTotal - subtotalEsperado) > 0.01m)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ {item.NombreProducto}: SubTotal inconsistente");
                    System.Diagnostics.Debug.WriteLine($"   • Esperado: ${subtotalEsperado:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Actual: ${item.SubTotal:F2}");
                    esCoherente = false;
                }

                // ✅ VALIDAR DESCUENTOS MANUALES
                if (item.TieneDescuentoManual)
                {
                    if (!item.ValidarDescuento())
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ {item.NombreProducto}: Descuento manual inválido");
                        esCoherente = false;
                    }

                    // ✅ VERIFICAR QUE NO HAYA DOBLE DESCUENTO
                    if (item.DescuentoAplicado > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️  {item.NombreProducto}: Tiene descuento manual Y DescuentoAplicado");
                        System.Diagnostics.Debug.WriteLine($"   • DescuentoAplicado: ${item.DescuentoAplicado:F2} (debería ser 0)");
                        System.Diagnostics.Debug.WriteLine($"   • DescuentoUnitario: ${item.DescuentoUnitario:F2}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"🔍 Validación completa: {(esCoherente ? "✅ COHERENTE" : "❌ INCOHERENTE")}");
            return esCoherente;
        }

        /// <summary>
        /// Llama a este método después de aplicar descuentos para verificar todo
        /// </summary>
        private void VerificarEstadoCarritoCompleto()
        {
            System.Diagnostics.Debug.WriteLine("\n🔍 === VERIFICACIÓN COMPLETA DEL CARRITO ===");

            MostrarInfoDebugCarrito("VERIFICACIÓN FINAL");

            if (ValidarCoherenciaCarrito())
            {
                System.Diagnostics.Debug.WriteLine("✅ El carrito está en estado coherente");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Se detectaron inconsistencias en el carrito");
            }

            System.Diagnostics.Debug.WriteLine("🔍 === FIN VERIFICACIÓN COMPLETA ===\n");
        }

        /// <summary>
        /// Limpia completamente el carrito después de una venta exitosa
        /// </summary>
        private void LimpiarCarritoCompleto()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧹 === LIMPIANDO CARRITO COMPLETAMENTE ===");

                // ✅ MOSTRAR ESTADO ANTES DE LIMPIAR
                MostrarInfoDebugCarrito("ANTES DE LIMPIAR");

                // ✅ LIMPIAR ITEMS DEL CARRITO
                foreach (var item in _carritoItems.ToList())
                {
                    System.Diagnostics.Debug.WriteLine($"🗑️ Limpiando: {item.NombreProducto}");

                    // ✅ RESETEAR VALORES DE DESCUENTO SI FUERON APLICADOS
                    if (item.TieneDescuentoManual)
                    {
                        System.Diagnostics.Debug.WriteLine($"   • Tenía descuento manual: ${item.DescuentoUnitario:F2}");
                        item.RemoverDescuento(); // Esto restaura el precio original
                    }

                    // ✅ LIMPIAR CUALQUIER OTRO DESCUENTO
                    if (item.DescuentoAplicado > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"   • Tenía descuento tradicional: ${item.DescuentoAplicado:F2}");
                        item.DescuentoAplicado = 0;
                        item.CalcularSubTotal();
                    }
                }

                // ✅ VACIAR LA LISTA COMPLETAMENTE
                _carritoItems.Clear();

                // ✅ LIMPIAR INFORMACIÓN DE DESCUENTOS GLOBALES
                if (_descuentoInfo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🧹 Limpiando info descuento global: {_descuentoInfo}");
                    _descuentoInfo = null;
                }

                // ✅ ACTUALIZAR INTERFAZ
                ActualizarTotalesCarrito();
                UpdateContadoresPOS();

                // ✅ LIMPIAR CAMPOS DE CLIENTE SI ES NECESARIO
                if (TxtCliente != null)
                {
                    TxtCliente.Text = "";
                }

                System.Diagnostics.Debug.WriteLine("✅ Carrito limpiado completamente");
                System.Diagnostics.Debug.WriteLine($"   • Items en carrito: {_carritoItems.Count}");
                System.Diagnostics.Debug.WriteLine($"   • Info descuento: {(_descuentoInfo != null ? "Presente" : "null")}");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error limpiando carrito: {ex.Message}");
                // No lanzar excepción aquí, solo registrar el error
            }
        }

        #endregion
        // <summary>
        /// Limpia completamente el carrito después de una venta exitosa
        /// </summary>
       
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
                // ✅ CORRECCIÓN FINAL: Usar el método que maneja productos Y servicios
                FiltrarItemsPOS();
                UpdateContadoresPOS();
            }

        private async void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ DEBUG CRÍTICO: ¿Qué está activando este botón?
                System.Diagnostics.Debug.WriteLine($"🚪 === BOTÓN CERRAR SESIÓN ACTIVADO ===");
                System.Diagnostics.Debug.WriteLine($"   Sender: {sender?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   RoutedEvent: {e?.RoutedEvent?.Name}");
                System.Diagnostics.Debug.WriteLine($"   Source: {e?.Source?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Hora: {DateTime.Now:HH:mm:ss.fff}");

                // ✅ VERIFICAR SI ES CLICK REAL O AUTOMÁTICO
                var button = sender as Button;
                if (button != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   Mouse sobre botón: {button.IsMouseOver}");
                    System.Diagnostics.Debug.WriteLine($"   Botón tiene foco: {button.IsFocused}");
                    System.Diagnostics.Debug.WriteLine($"   Botón habilitado: {button.IsEnabled}");
                }

                System.Diagnostics.Debug.WriteLine($"🚪 === FIN DEBUG BOTÓN ===");

                System.Diagnostics.Debug.WriteLine("🚪 Botón cerrar sesión presionado");

                // ✅ PROTECCIÓN ESPECÍFICA: Si el botón tiene foco pero no mouse over = activación por teclado
                if (button != null && button.IsFocused && !button.IsMouseOver)
                {
                    System.Diagnostics.Debug.WriteLine($"🛡️ CLICK POR TECLADO DETECTADO - Botón tiene foco sin mouse");
                    MessageBox.Show("⚠️ Activación por teclado detectada\n\nUse el mouse para hacer clic en el botón de cerrar sesión.",
                                   "Protección Anti-Autoclick", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Quitar foco del botón
                    this.Focus();
                    return;
                }

                // ✅ PROTECCIÓN TEMPORAL: Si acabamos de escanear, ignorar
                if (_ultimoProductoEscaneado != null &&
                    (DateTime.Now - _tiempoUltimoEscaneo).TotalSeconds < 5)
                {
                    System.Diagnostics.Debug.WriteLine($"🛡️ CLICK DE CERRAR SESIÓN BLOQUEADO - Escaneo reciente");
                    MessageBox.Show("⚠️ Acción bloqueada\n\nEscaneo reciente detectado.\nEspere unos segundos antes de cerrar sesión.",
                                   "Protección Anti-Autoclick", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ PROTECCIÓN ADICIONAL: Verificar si es click automático
                if (button != null && !button.IsMouseOver && !button.IsFocused)
                {
                    System.Diagnostics.Debug.WriteLine($"🛡️ CLICK AUTOMÁTICO DETECTADO - Sin interacción real del usuario");
                    MessageBox.Show("⚠️ Click automático detectado\n\nUse el mouse para hacer clic en el botón.",
                                   "Protección Anti-Autoclick", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🛡️ PROTECCIÓN ANTI-DOBLE-CLICK
                if (_cerrandoSesion)
                {
                    System.Diagnostics.Debug.WriteLine("🛡️ YA SE ESTÁ CERRANDO SESIÓN - IGNORANDO CLICK");
                    return;
                }

                // ✅ MARCAR QUE ESTAMOS CERRANDO SESIÓN
                _cerrandoSesion = true;

                System.Diagnostics.Debug.WriteLine("✅ Click validado - Procesando cierre de sesión");

                // ✅ MOSTRAR INDICADOR DE PROCESO
                if (TxtStatusPOS != null)
                    TxtStatusPOS.Text = "🚪 Cerrando sesión...";

                if (TxtStatus != null)
                    TxtStatus.Text = "🚪 Cerrando sesión...";

                // ✅ DESHABILITAR BOTÓN PARA EVITAR DOBLE-CLICK
                BtnCerrarSesionPOS.IsEnabled = false;

                // ✅ LIMPIAR DATOS SENSIBLES LOCALES
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

                // ✅ USAR EL NUEVO MÉTODO DE REINICIO
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

                    // ✅ RESTAURAR ESTADO PARA PERMITIR NUEVO INTENTO
                    _cerrandoSesion = false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Proceso de reinicio iniciado - Esta instancia se cerrará");
                    // No restaurar _cerrandoSesion porque la aplicación se va a cerrar
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

                // ✅ RESTAURAR ESTADO PARA PERMITIR NUEVO INTENTO
                _cerrandoSesion = false;

                MessageBox.Show(
                    $"❌ Error al procesar cierre de sesión:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ========== AGREGAR ESTE MÉTODO PARA TESTING ==========


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
        public async Task RefrescarProductosAutomatico(string motivo = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 INICIANDO RefrescarProductosAutomatico: {motivo}");

                // ✅ MEJORAR: Verificar múltiples condiciones
                if (!_posLoaded)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 POS no cargado - Intentando carga automática...");

                    // ✅ INTENTAR CARGAR POS AUTOMÁTICAMENTE
                    try
                    {
                        await LoadDataPuntoVenta();
                        System.Diagnostics.Debug.WriteLine($"✅ POS cargado automáticamente para actualización");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error cargando POS automáticamente: {ex.Message}");
                        return;
                    }
                }

                // ✅ VERIFICACIÓN ADICIONAL: Continuar solo si realmente no se puede
                if (!_posLoaded)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 SALIENDO porque _posLoaded sigue siendo false");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔄 PASO 1: Creando contexto fresco...");

                // ✅ SOLO USAR CONTEXTO FRESCO - SIN TOCAR EL PRINCIPAL
                using var freshContext = new AppDbContext();
                _allMaterials = await freshContext.RawMaterials
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"🔄 PASO 2: RECARGADOS: {_allMaterials.Count} materiales");

                // ✅ RECREAR LISTA FILTRADA
                string searchText = TxtBuscar?.Text?.ToLower()?.Trim() ?? "";
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

                System.Diagnostics.Debug.WriteLine($"🔄 PASO 3: Filtrados: {_filteredMaterials.Count} materiales");

                // ✅ ACTUALIZAR INTERFAZ
                Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 PASO 4: Actualizando DataGrid...");

                    DgMateriales.ItemsSource = null;
                    DgMateriales.ItemsSource = _filteredMaterials;
                    DgMateriales.Items.Refresh();

                    UpdateStatusBar();

                    System.Diagnostics.Debug.WriteLine($"🔄 PASO 5: DataGrid actualizado con {_filteredMaterials.Count} items");
                });

                // ✅ ACTUALIZAR POS COMPLETO
                System.Diagnostics.Debug.WriteLine($"🔄 PASO 6: Actualizando POS...");

                // Recargar productos Y servicios
                // ✅ CARGAR TODOS LOS PRODUCTOS (con y sin stock) para el buscador
                _productosParaVenta = await freshContext.RawMaterials
                    .Where(m => !m.Eliminado && m.ActivoParaVenta) // Solo activos, pero CON o SIN stock
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();
                _serviciosParaVenta = await freshContext.ServiciosVenta
                    .Include(s => s.MaterialesNecesarios)
                        .ThenInclude(m => m.RawMaterial)
                    .Where(s => s.IntegradoPOS && s.Activo)
                    .OrderBy(s => s.PrioridadPOS)
                    .ThenBy(s => s.NombreServicio)
                    .ToListAsync();

                // ✅ RECREAR LISTA MIXTA COMPLETA
                _itemsPOS = new List<object>();
                _itemsPOS.AddRange(_productosParaVenta.Cast<object>());
                _itemsPOS.AddRange(_serviciosParaVenta.Cast<object>());

                System.Diagnostics.Debug.WriteLine($"🔄 PASO 7: Items POS: {_productosParaVenta.Count} productos + {_serviciosParaVenta.Count} servicios = {_itemsPOS.Count} total");

                // ✅ ACTUALIZAR INTERFAZ POS
                Dispatcher.Invoke(() =>
                {
                    // Aplicar filtro actual si existe
                    FiltrarItemsPOS();
                    UpdateContadoresPOS();
                    System.Diagnostics.Debug.WriteLine($"🔄 PASO 8: Interfaz POS actualizada");
                });

                System.Diagnostics.Debug.WriteLine($"🔄 COMPLETADO: RefrescarProductosAutomatico");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en RefrescarProductosAutomatico: {ex.Message}");

                // ✅ NO lanzar excepción para no romper el sistema
                if (TxtStatusPOS != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtStatusPOS.Text = "⚠️ Error en actualización automática";
                    });
                }
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
            // Solo para feedback visual - mantener funcionalidad original
            if (LstProductosPOS.SelectedItem != null)
            {
                if (LstProductosPOS.SelectedItem is RawMaterial producto)
                {
                    TxtStatusPOS.Text = $"📦 Producto seleccionado: {producto.NombreArticulo} - Stock: {producto.StockTotal:F2}";
                }
                else if (LstProductosPOS.SelectedItem is ServicioVenta servicio)
                {
                    TxtStatusPOS.Text = $"🛍️ Servicio seleccionado: {servicio.NombreServicio} - Precio: {servicio.PrecioServicio:C2}";
                }
            }
        }

        private async void LstProductosPOS_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (LstProductosPOS.SelectedItem == null) return;

                // ✅ MANEJAR PRODUCTOS (funcionalidad original)
                if (LstProductosPOS.SelectedItem is RawMaterial producto)
                {
                    // ✅ CORRECCIÓN: Usar método universal para detectar productos a granel
                    if (EsProductoAGranel(producto.UnidadMedida))
                    {
                        // Abrir ventana para ingresar peso/volumen/longitud
                        var pesoWindow = new IngresarPesoWindow(_context, producto, _basculaService);
                        if (pesoWindow.ShowDialog() == true)
                        {
                            await AgregarProductoAlCarrito(producto, pesoWindow.PesoIngresado);
                        }
                    }
                    else
                    {
                        // Agregar cantidad fija (funcionalidad original)
                        await AgregarProductoAlCarrito(producto, 1);
                    }
                }
                // ✅ NUEVO: MANEJAR SERVICIOS
                else if (LstProductosPOS.SelectedItem is ServicioVenta servicio)
                {
                    await AgregarServicioAlCarrito(servicio, 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar selección: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al procesar selección";
            }
        }

        private async void BtnEliminarDelCarrito_Click_ConAutorizacion(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is Button btn) || !(btn.Tag is DetalleVenta item))
                    return;

                System.Diagnostics.Debug.WriteLine($"🗑️ Solicitando autorización para eliminar: {item.NombreProducto}");

                // ✅ REQUERIR AUTORIZACIÓN
                var autorizacionWindow = new AutorizacionDescuentoWindow($"eliminar '{item.NombreProducto}' del carrito")
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (autorizacionWindow.ShowDialog() != true || !autorizacionWindow.AutorizacionExitosa)
                {
                    TxtStatusPOS.Text = $"❌ Autorización para eliminar '{item.NombreProducto}' cancelada";
                    return;
                }

                // ✅ CONFIRMACIÓN ADICIONAL
                var mensaje = $"🗑️ ELIMINAR PRODUCTO\n\n" +
                             $"Producto: {item.NombreProducto}\n" +
                             $"Cantidad: {item.Cantidad:F2} {item.UnidadMedida}\n" +
                             $"Subtotal: {item.SubTotal:C2}\n\n" +
                             $"Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}\n\n" +
                             $"¿Confirmar eliminación?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Eliminación",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _carritoItems.Remove(item);
                    UpdateContadoresPOS();

                    TxtStatusPOS.Text = $"🗑️ Eliminado: {item.NombreProducto} - Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}";

                    System.Diagnostics.Debug.WriteLine($"✅ Producto eliminado por autorización de: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}");
                }
                else
                {
                    TxtStatusPOS.Text = "❌ Eliminación de producto cancelada";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnEliminarDelCarrito_Click_ConAutorizacion: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error al eliminar producto: {ex.Message}";

                MessageBox.Show($"Error al eliminar producto del carrito:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnProcesarVenta_Click(object sender, RoutedEventArgs e)
        {
            await ProcesarVentaUnico();
        }

        private void BtnLimpiarCarrito_Click_ConAutorizacion(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_carritoItems.Any())
                {
                    MessageBox.Show("El carrito ya está vacío.", "Carrito Vacío",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("🗑️ Solicitando autorización para limpiar carrito");

                // ✅ REQUERIR AUTORIZACIÓN
                var autorizacionWindow = new AutorizacionDescuentoWindow("limpiar el carrito")
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (autorizacionWindow.ShowDialog() != true || !autorizacionWindow.AutorizacionExitosa)
                {
                    TxtStatusPOS.Text = "❌ Autorización para limpiar carrito cancelada";
                    return;
                }

                // ✅ CONFIRMACIÓN ADICIONAL
                var itemsCount = _carritoItems.Count;
                var totalCarrito = _carritoItems.Sum(i => i.SubTotal);

                var mensaje = $"🗑️ LIMPIAR CARRITO\n\n" +
                             $"Se eliminarán {itemsCount} productos\n" +
                             $"Total del carrito: {totalCarrito:C2}\n\n" +
                             $"Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}\n\n" +
                             $"¿Confirmar la limpieza del carrito?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Limpieza",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Limpiar carrito
                    _carritoItems.Clear();

                    // Limpiar cliente
                    TxtCliente.Text = "Cliente General";

                    // Actualizar interfaz
                    UpdateContadoresPOS();

                    TxtStatusPOS.Text = $"🗑️ Carrito limpiado - Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}";

                    System.Diagnostics.Debug.WriteLine($"✅ Carrito limpiado por autorización de: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}");
                }
                else
                {
                    TxtStatusPOS.Text = "❌ Limpieza de carrito cancelada";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnLimpiarCarrito_Click_ConAutorizacion: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error al limpiar carrito: {ex.Message}";

                MessageBox.Show($"Error al limpiar carrito:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void ActualizarPromocionesACombinables()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Iniciando actualización de promociones...");

                // Usar contexto fresco
                using var context = new AppDbContext();

                // Obtener TODAS las promociones que no son combinables
                var promocionesNoCombinable = await context.PromocionesVenta
                    .Where(p => !p.Eliminado && !p.Combinable)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"📊 Encontradas {promocionesNoCombinable.Count} promociones no combinables");

                if (!promocionesNoCombinable.Any())
                {
                    System.Diagnostics.Debug.WriteLine("✅ Todas las promociones ya son combinables");
                    MessageBox.Show("✅ Todas las promociones ya son combinables!",
                                   "Sin Cambios", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Actualizar todas a combinables
                foreach (var promocion in promocionesNoCombinable)
                {
                    promocion.Combinable = true;
                    System.Diagnostics.Debug.WriteLine($"✅ Promoción '{promocion.NombrePromocion}' ahora es combinable");
                }

                // Guardar cambios
                await context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"🎁 {promocionesNoCombinable.Count} promociones actualizadas exitosamente");

                MessageBox.Show($"✅ {promocionesNoCombinable.Count} promociones actualizadas!\n\n" +
                               "Ahora se pueden aplicar múltiples promociones simultáneamente.",
                               "Promociones Actualizadas", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusPOS.Text = $"✅ {promocionesNoCombinable.Count} promociones ahora son combinables";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando promociones: {ex.Message}");
                MessageBox.Show($"Error al actualizar promociones:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al actualizar promociones";
            }
        }
        private async void VerificarEstadoPromociones_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new AppDbContext();

                var totalPromociones = await context.PromocionesVenta
                    .Where(p => !p.Eliminado && p.Activa)
                    .CountAsync();

                var combinables = await context.PromocionesVenta
                    .Where(p => !p.Eliminado && p.Activa && p.Combinable)
                    .CountAsync();

                var noCombinable = totalPromociones - combinables;

                string mensaje = $"📊 ESTADO ACTUAL:\n\n" +
                                $"🎁 Total promociones activas: {totalPromociones}\n" +
                                $"✅ Promociones combinables: {combinables}\n" +
                                $"❌ Promociones NO combinables: {noCombinable}\n\n";

                if (noCombinable > 0)
                {
                    mensaje += "⚠️ Hay promociones que no se pueden combinar.\n" +
                              "Solo se aplicará una promoción por venta.";
                }
                else
                {
                    mensaje += "🎉 ¡Perfecto! Todas las promociones son combinables.\n" +
                              "Se pueden aplicar múltiples promociones simultáneamente.";
                }

                MessageBox.Show(mensaje, "Estado de Promociones",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al verificar estado: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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


        private bool EsProductoAGranel(string unidadMedida)
        {
            try
            {
                if (string.IsNullOrEmpty(unidadMedida)) return false;

                var unidad = unidadMedida.ToLower().Trim();

                // ⚖️ PESO
                if (unidad.Contains("kg") || unidad.Contains("kilogramos") ||
                    unidad.Contains("gr") || unidad.Contains("gramos") ||
                    unidad.Contains("lb") || unidad.Contains("libras"))
                    return true;

                // 🧴 VOLUMEN/LÍQUIDOS  
                if (unidad.Contains("litros") || unidad.Contains("lt") ||
                    unidad.Contains("l") || unidad.Contains("ml") ||
                    unidad.Contains("mililitros") || unidad.Contains("galones") ||
                    unidad.Contains("gal"))
                    return true;

                // 📏 LONGITUD
                if (unidad.Contains("metros") || unidad.Contains("mts") ||
                    unidad.Contains("m") || unidad.Contains("cm") ||
                    unidad.Contains("centímetros"))
                    return true;

                System.Diagnostics.Debug.WriteLine($"🔍 Unidad '{unidadMedida}' NO es a granel");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error detectando granel: {ex.Message}");
                return false;
            }
        }

        private string ObtenerTipoUnidad(string unidadMedida)
        {
            if (string.IsNullOrEmpty(unidadMedida)) return "Cantidad";

            var unidad = unidadMedida.ToLower().Trim();

            if (unidad.Contains("kg") || unidad.Contains("gr") || unidad.Contains("gramos") ||
                unidad.Contains("kilogramos") || unidad.Contains("lb") || unidad.Contains("libras"))
                return "Peso";

            if (unidad.Contains("litros") || unidad.Contains("lt") || unidad.Contains("l") ||
                unidad.Contains("ml") || unidad.Contains("mililitros") || unidad.Contains("galones"))
                return "Volumen";

            if (unidad.Contains("metros") || unidad.Contains("mts") || unidad.Contains("m") ||
                unidad.Contains("cm") || unidad.Contains("centímetros"))
                return "Longitud";

            return "Cantidad";
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
        public async Task RefreshData()
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
        private string DeterminarTipoUsuario(string nombreUsuario)
        {
            try
            {
                // Verificar si es usuario soporte
                if (SoporteSystem.EsUsuarioSoporte(nombreUsuario))
                    return "Soporte";

                // Buscar en base de datos
                using var context = new AppDbContext();
                var usuario = context.Users.FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

                return usuario?.Rol ?? "Desconocido";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error determinando tipo usuario: {ex.Message}");
                return "Desconocido";
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
            try
            {
                // Disposed servicios POS
                _ticketPrinter?.Dispose();
                _basculaService?.Dispose();
                _posIntegrationService?.Dispose();
                _corteCajaService?.Dispose();

                // ✅ CAMBIAR: Dispose del escáner unificado
                _unifiedScanner?.Dispose();
                _scannerProtection?.Dispose();

                // Dispose del contexto
                _context?.Dispose();

                System.Diagnostics.Debug.WriteLine("🗑️ Todos los servicios disposed correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en OnClosed: {ex.Message}");
            }

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
        private void BtnInfoEscanerUnificado_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var estadoUnificado = _unifiedScanner?.GetStatusInfo() ?? "Escáner unificado: No inicializado";
                var estadisticasProteccion = _scannerProtection?.ObtenerEstadisticas() ?? "Protección: No inicializada";
                var contextoActual = MainTabControl.SelectedIndex == 1 ? "POS Activo" : "POS Inactivo";

                var mensaje = $"📱 ESCÁNER UNIVERSAL - TODOS LOS TIPOS\n\n" +
                             $"🎯 {estadoUnificado}\n" +
                             $"📊 Contexto: {contextoActual}\n\n" +
                             $"🛡️ PROTECCIÓN:\n{estadisticasProteccion}\n\n" +
                             $"💡 COMPATIBILIDAD TOTAL:\n" +
                             $"✅ Escáneres USB (como teclado) - Tu Steren Com-596\n" +
                             $"✅ Escáneres Serie/COM (puerto serie)\n" +
                             $"✅ Cualquier escáner del mercado\n" +
                             $"✅ Detección automática del tipo\n" +
                             $"✅ Protección anti-autoclick integrada";

                MessageBox.Show(mensaje, "Información del Escáner Universal",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusPOS.Text = "📱 Información del escáner universal mostrada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar información del escáner: {ex.Message}",
                               "Error Escáner", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task AgregarServicioAlCarrito(ServicioVenta servicio, decimal cantidad = 1)
        {
            try
            {
                // Verificar disponibilidad del servicio
                if (!servicio.DisponibleParaVenta)
                {
                    MessageBox.Show($"El servicio '{servicio.NombreServicio}' no está disponible para venta.",
                                   "Servicio No Disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Verificar stock del servicio (si aplica)
                if (servicio.StockDisponible < cantidad)
                {
                    MessageBox.Show($"Stock insuficiente del servicio. Disponible: {servicio.StockDisponible}",
                                   "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Buscar si ya existe en el carrito
                var itemExistente = _carritoItems.FirstOrDefault(i =>
                    i.ServicioVentaId == servicio.Id && i.EsServicio);

                if (itemExistente != null)
                {
                    // Verificar que no exceda el stock disponible
                    if (itemExistente.Cantidad + cantidad > servicio.StockDisponible)
                    {
                        MessageBox.Show($"Cantidad total excede el stock disponible del servicio. Disponible: {servicio.StockDisponible}",
                                       "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Actualizar cantidad existente
                    itemExistente.ActualizarCantidad(itemExistente.Cantidad + cantidad);
                }
                else
                {
                    // ✅ CORRECCIÓN: Crear item del carrito para servicio usando campos correctos
                    var nuevoItem = new DetalleVenta
                    {
                        RawMaterialId = null, // ✅ NULL para servicios
                        ServicioVentaId = servicio.Id, // ✅ ID del servicio
                        NombreProducto = $"🛍️ {servicio.NombreServicio}",
                        Cantidad = cantidad,
                        PrecioUnitario = servicio.PrecioServicio,
                        UnidadMedida = "servicio",
                        CostoUnitario = servicio.CostoTotal,
                        PorcentajeIVA = servicio.PorcentajeIVA,
                        DescuentoAplicado = 0
                    };

                    nuevoItem.CalcularSubTotal();
                    _carritoItems.Add(nuevoItem);
                }

                // Actualizar interfaz
                UpdateContadoresPOS();
                TxtStatusPOS.Text = $"✅ Servicio agregado: {servicio.NombreServicio} x{cantidad}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar servicio: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatusPOS.Text = "❌ Error al agregar servicio";
            }
        }
        private void BtnInfoEscaner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var estadoUnificado = _unifiedScanner?.GetStatusInfo() ?? "Escáner unificado: No inicializado";
                var estadisticasProteccion = _scannerProtection?.ObtenerEstadisticas() ?? "Protección: No inicializada";
                var contextoActual = MainTabControl.SelectedIndex == 1 ? "POS Activo" : "POS Inactivo";

                var mensaje = $"📱 INFORMACIÓN COMPLETA DEL ESCÁNER\n\n" +
                             $"🔌 {estadoUnificado}\n" +
                             $"📊 Contexto: {contextoActual}\n\n" +
                             $"🛡️ PROTECCIÓN:\n{estadisticasProteccion}\n\n" +
                             $"💡 COMPATIBILIDAD:\n" +
                             $"• Escáneres serie/COM: Detección automática\n" +
                             $"• El escáner está activo automáticamente en POS\n" +
                             $"• Protección anti-autoclick integrada";

                MessageBox.Show(mensaje, "Información del Escáner",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusPOS.Text = "📱 Información del escáner mostrada";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar información del escáner: {ex.Message}",
                               "Error Escáner", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                switch (tabControl.SelectedIndex)
                {
                    case 0: // Materia Prima
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.MateriaPrima);
                            System.Diagnostics.Debug.WriteLine("🔄 Escáner unificado cambiado a contexto: Materia Prima");
                        }
                        if (TxtStatus != null)
                            TxtStatus.Text = "✅ Módulo de Materia Prima activo";
                        break;

                    case 1: // PUNTO DE VENTA
                        if (TxtStatusPOS != null)
                            TxtStatusPOS.Text = "💰 Cargando POS...";

                        // ✅ SOLO CONFIGURAR CONTEXTO - NO HABILITAR AUTOMÁTICAMENTE
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.PuntoVenta);
                            // ✅ NO habilitar automáticamente - Que el usuario lo active manualmente

                            System.Diagnostics.Debug.WriteLine("🔄 Escáner unificado cambiado a contexto: Punto de Venta");
                        }

                        // ✅ ESTABLECER ESTADO INICIAL COMO INACTIVO
                        if (TxtEstadoEscaner != null)
                        {
                            TxtEstadoEscaner.Text = "📱 INACTIVO";
                            TxtEstadoEscaner.Parent?.SetValue(Border.BackgroundProperty,
                                new SolidColorBrush(Color.FromRgb(107, 114, 128))); // Gris
                        }

                        // Cargar datos POS si es necesario
                        if (!_posLoaded)
                        {
                            await LoadDataPuntoVenta();
                        }
                        else
                        {
                            await VerificarEstadoCorteCaja();
                        }

                        if (TxtStatusPOS != null)
                            TxtStatusPOS.Text = "✅ Sistema POS listo - Escáner disponible (inactivo)";
                        break;
                    case 2: // Reportes
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.Ninguno);
                            System.Diagnostics.Debug.WriteLine("🔄 Escáner unificado desactivado para Reportes");
                        }
                        if (TxtStatus != null)
                            TxtStatus.Text = "📊 Módulo de Reportes disponible";
                        break;

                    case 3: // Procesos
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.Ninguno);
                        }
                        if (TxtStatus != null)
                            TxtStatus.Text = "⚙️ Módulo de Procesos activo";
                        break;

                    case 4: // Análisis
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.Ninguno);
                        }
                        if (TxtStatus != null)
                            TxtStatus.Text = "📈 Módulo de Análisis (próximamente)";
                        break;

                    case 5: // Configuración
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.Ninguno);
                        }
                        if (TxtStatus != null)
                            TxtStatus.Text = "⚙️ Configuración del sistema (próximamente)";
                        break;

                    case 6: // Mi Información
                        if (_unifiedScanner != null)
                        {
                            _unifiedScanner.SetContext(ScannerContext.Ninguno);
                        }
                        if (TxtStatus != null)
                            TxtStatus.Text = "👨‍💻 Información del desarrollador - Esaú Villagrán";
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine($"⚠️ Tab no reconocido: {tabControl.SelectedIndex}");
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
        private async void BtnAplicarDescuento_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_carritoItems.Any())
                {
                    MessageBox.Show("No hay productos en el carrito para aplicar descuento.",
                                  "Carrito Vacío", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                decimal totalActual = _carritoItems.Sum(i => i.SubTotal);

                if (totalActual <= 0)
                {
                    MessageBox.Show("El total del carrito debe ser mayor a $0.00 para aplicar descuento.",
                                  "Total Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Autorización
                var autorizacionWindow = new AutorizacionDescuentoWindow("aplicar descuento")
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (autorizacionWindow.ShowDialog() != true || !autorizacionWindow.AutorizacionExitosa)
                {
                    TxtStatusPOS.Text = "❌ Autorización de descuento cancelada";
                    return;
                }

                // Configurar descuento
                var descuentoWindow = new AplicarDescuentoWindow(totalActual, autorizacionWindow.UsuarioAutorizador.NombreCompleto)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (descuentoWindow.ShowDialog() == true && descuentoWindow.DescuentoAplicado)
                {
                    // Aplicar descuento
                    await AplicarDescuentoAlCarrito(descuentoWindow);

                    TxtStatusPOS.Text = $"🎁 Descuento aplicado: {descuentoWindow.DescuentoCalculado:C2}";

                    MessageBox.Show(
                        $"🎁 Descuento aplicado exitosamente!\n\n" +
                        $"Descuento: {descuentoWindow.DescuentoCalculado:C2}\n" +
                        $"Autorizado por: {descuentoWindow.UsuarioAutorizador}",
                        "Descuento Aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    TxtStatusPOS.Text = "❌ Aplicación de descuento cancelada";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnAplicarDescuento_Click: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error al aplicar descuento: {ex.Message}";

                MessageBox.Show($"Error al aplicar descuento:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void BtnCancelarVenta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_carritoItems.Any())
                {
                    MessageBox.Show("No hay venta activa para cancelar.", "Sin Venta Activa",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("❌ Solicitando autorización para cancelar venta completa");

                // ✅ REQUERIR AUTORIZACIÓN
                var autorizacionWindow = new AutorizacionDescuentoWindow("cancelar la venta completa")
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (autorizacionWindow.ShowDialog() != true || !autorizacionWindow.AutorizacionExitosa)
                {
                    TxtStatusPOS.Text = "❌ Autorización para cancelar venta cancelada";
                    return;
                }

                // ✅ CONFIRMACIÓN ADICIONAL
                var itemsCount = _carritoItems.Count;
                var totalVenta = _carritoItems.Sum(i => i.SubTotal);

                var mensaje = $"❌ CANCELAR VENTA COMPLETA\n\n" +
                             $"Se cancelará la venta con:\n" +
                             $"• {itemsCount} productos\n" +
                             $"• Total: {totalVenta:C2}\n\n" +
                             $"Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}\n\n" +
                             $"Esta acción no se puede deshacer.\n" +
                             $"¿Confirmar cancelación de la venta?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Cancelación de Venta",
                                              MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Limpiar carrito
                    _carritoItems.Clear();

                    // Limpiar cliente
                    TxtCliente.Text = "Cliente General";

                    // Actualizar interfaz
                    UpdateContadoresPOS();

                    TxtStatusPOS.Text = $"❌ Venta cancelada - Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}";

                    System.Diagnostics.Debug.WriteLine($"✅ Venta cancelada por autorización de: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}");

                    MessageBox.Show(
                        $"❌ Venta cancelada exitosamente\n\n" +
                        $"Total cancelado: {totalVenta:C2}\n" +
                        $"Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}",
                        "Venta Cancelada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    TxtStatusPOS.Text = "✅ Cancelación de venta abortada";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnCancelarVenta_Click: {ex.Message}");
                TxtStatusPOS.Text = $"❌ Error al cancelar venta: {ex.Message}";

                MessageBox.Show($"Error al cancelar venta:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AplicarDescuentoAlCarrito(AplicarDescuentoWindow descuentoInfo)
        {
            try
            {
                if (!descuentoInfo.DescuentoAplicado || descuentoInfo.DescuentoCalculado <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ No hay descuento para aplicar");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🎁 === APLICANDO DESCUENTO CON AUDITORÍA CORREGIDA ===");
                System.Diagnostics.Debug.WriteLine($"   • Descuento total: ${descuentoInfo.DescuentoCalculado:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Autorizado por: {descuentoInfo.UsuarioAutorizador}");
                System.Diagnostics.Debug.WriteLine($"   • Motivo: {descuentoInfo.MotivoDescuento}");

                // ✅ CALCULAR TOTAL ORIGINAL (PRECIOS ACTUALES * CANTIDADES)
                decimal totalOriginalActual = _carritoItems.Sum(i => i.PrecioUnitario * i.Cantidad);
                decimal descuentoTotal = descuentoInfo.DescuentoCalculado;
                decimal descuentoAplicado = 0;

                System.Diagnostics.Debug.WriteLine($"💰 TOTALES PARA CÁLCULO:");
                System.Diagnostics.Debug.WriteLine($"   • Total actual carrito: ${totalOriginalActual:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Descuento a aplicar: ${descuentoTotal:F2}");

                // ✅ APLICAR DESCUENTO PROPORCIONAL A CADA ITEM
                for (int i = 0; i < _carritoItems.Count; i++)
                {
                    var item = _carritoItems[i];

                    // ✅ CALCULAR PROPORCIÓN BASADA EN PRECIO ACTUAL
                    decimal proporcion = totalOriginalActual > 0 ? (item.PrecioUnitario * item.Cantidad) / totalOriginalActual : 0;
                    decimal descuentoItem;

                    // ✅ ASEGURAR QUE EL ÚLTIMO ITEM RECIBA EL DESCUENTO EXACTO
                    if (i == _carritoItems.Count - 1)
                    {
                        descuentoItem = descuentoTotal - descuentoAplicado;
                    }
                    else
                    {
                        descuentoItem = Math.Round(descuentoTotal * proporcion, 2);
                        descuentoAplicado += descuentoItem;
                    }

                    decimal descuentoPorUnidad = item.Cantidad > 0 ? descuentoItem / item.Cantidad : 0;

                    System.Diagnostics.Debug.WriteLine($"📦 {item.NombreProducto}:");
                    System.Diagnostics.Debug.WriteLine($"   • Precio actual: ${item.PrecioUnitario:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Cantidad: {item.Cantidad:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Subtotal actual: ${item.PrecioUnitario * item.Cantidad:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Proporción: {proporcion:P2}");
                    System.Diagnostics.Debug.WriteLine($"   • Descuento total línea: ${descuentoItem:F2}");
                    System.Diagnostics.Debug.WriteLine($"   • Descuento por unidad: ${descuentoPorUnidad:F2}");

                    // ✅ APLICAR DESCUENTO CON AUDITORÍA CORREGIDA
                    item.AplicarDescuentoConAuditoria(
                        descuentoPorUnidad,
                        descuentoInfo.MotivoDescuento,
                        descuentoInfo.UsuarioAutorizador
                    );

                    System.Diagnostics.Debug.WriteLine($"   ✅ RESULTADO:");
                    System.Diagnostics.Debug.WriteLine($"      • Precio final: ${item.PrecioUnitario:F2}");
                    System.Diagnostics.Debug.WriteLine($"      • SubTotal final: ${item.SubTotal:F2}");
                    System.Diagnostics.Debug.WriteLine($"      • Validación: {(item.ValidarDescuento() ? "✅ Válido" : "❌ Inválido")}");
                }

                // ✅ GUARDAR INFORMACIÓN PARA LA VENTA
                _descuentoInfo = new DescuentoAplicadoInfo
                {
                    TotalDescuento = descuentoTotal,
                    UsuarioAutorizador = descuentoInfo.UsuarioAutorizador,
                    TipoUsuario = DeterminarTipoUsuario(descuentoInfo.UsuarioAutorizador),
                    Motivo = descuentoInfo.MotivoDescuento,
                    FechaHoraAplicacion = DateTime.Now
                };

                // ✅ ACTUALIZAR INTERFAZ Y VERIFICAR
                ActualizarTotalesCarrito();
                UpdateContadoresPOS();

                // ✅ VERIFICACIÓN COMPLETA DEL ESTADO FINAL
                VerificarEstadoCarritoCompleto();

                // ✅ VERIFICACIÓN FINAL
                var totalFinalCarrito = _carritoItems.Sum(i => i.SubTotal);
                var descuentoRealAplicado = _carritoItems.Sum(i => i.TotalDescuentoLinea);

                System.Diagnostics.Debug.WriteLine($"🎉 DESCUENTO APLICADO EXITOSAMENTE:");
                System.Diagnostics.Debug.WriteLine($"   • Total original: ${totalOriginalActual:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Descuento aplicado: ${descuentoRealAplicado:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Total final: ${totalFinalCarrito:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Verificación: ${totalOriginalActual - descuentoRealAplicado:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Diferencia: ${Math.Abs(totalFinalCarrito - (totalOriginalActual - descuentoRealAplicado)):F2}");

                // ✅ VALIDAR QUE TODOS LOS DESCUENTOS SEAN COHERENTES
                bool todosValidos = true;
                foreach (var item in _carritoItems)
                {
                    if (item.TieneDescuentoManual && !item.ValidarDescuento())
                    {
                        todosValidos = false;
                        System.Diagnostics.Debug.WriteLine($"❌ Descuento inválido en: {item.NombreProducto}");
                        System.Diagnostics.Debug.WriteLine(item.ObtenerInfoDebugDescuento());
                    }
                }

                if (!todosValidos)
                {
                    throw new InvalidOperationException("Se detectaron descuentos inválidos después de la aplicación");
                }

                System.Diagnostics.Debug.WriteLine("✅ Todos los descuentos validados correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en AplicarDescuentoAlCarrito: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"Error al aplicar descuento:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        public class DescuentoAplicadoInfo
        {
            public decimal TotalDescuento { get; set; }
            public string UsuarioAutorizador { get; set; }
            public string TipoUsuario { get; set; }
            public string Motivo { get; set; }
            public DateTime FechaHoraAplicacion { get; set; }

            public override string ToString()
            {
                return $"${TotalDescuento:F2} - {UsuarioAutorizador} ({TipoUsuario}) - {Motivo}";
            }
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
        public class TypeConverter : IValueConverter
        {
            public static TypeConverter Instance { get; } = new TypeConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == null || parameter == null) return false;

                string targetTypeName = parameter.ToString();
                string actualTypeName = value.GetType().Name;

                return actualTypeName == targetTypeName;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
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