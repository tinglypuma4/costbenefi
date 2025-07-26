using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana dedicada para la gestión completa de Materia Prima
    /// </summary>
    public partial class MateriaPrimaWindow : Window
    {
        #region Variables Privadas
        private AppDbContext _context;
        private List<RawMaterial> _allMaterials = new();
        private List<RawMaterial> _filteredMaterials = new();
        #endregion

        #region Constructor y Inicialización
        public MateriaPrimaWindow()
        {
            try
            {
                // Inicializar componentes de UI
                InitializeComponent();

                // Inicializar colecciones básicas
                _allMaterials = new List<RawMaterial>();
                _filteredMaterials = new List<RawMaterial>();

                // Configurar timer para actualización de fecha/hora
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMinutes(1);
                timer.Tick += (s, e) => UpdateDateTime();
                timer.Start();

                // Configurar carga diferida
                this.Loaded += MateriaPrimaWindow_Loaded;

                System.Diagnostics.Debug.WriteLine("✅ MateriaPrimaWindow inicializada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ ERROR CRÍTICO en constructor de MateriaPrimaWindow:\n\n" +
                    $"Mensaje: {ex.Message}\n\n" +
                    $"La ventana se cerrará. Por favor reporte este error.",
                    "Error Fatal - MateriaPrimaWindow",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                this.Close();
            }
        }

        private async void MateriaPrimaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mostrar mensaje de carga
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "⏳ Inicializando gestión de materia prima...";
                }

                // Inicializar contexto de base de datos
                _context = new AppDbContext();
                await _context.Database.EnsureCreatedAsync();

                // Cargar datos
                await LoadDataSafe();

                // Actualizar status final
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "✅ Gestión de materia prima lista";
                }

                System.Diagnostics.Debug.WriteLine("✅ MateriaPrimaWindow cargada completamente");
            }
            catch (Exception ex)
            {
                if (TxtStatus != null)
                {
                    TxtStatus.Text = "❌ Error al cargar gestión de materia prima";
                }

                MessageBox.Show(
                    $"⚠️ Error al cargar gestión de materia prima:\n\n" +
                    $"{ex.Message}\n\n" +
                    $"La ventana funcionará con funcionalidad limitada.",
                    "Advertencia de Carga",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region Métodos de Carga de Datos
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

                System.Diagnostics.Debug.WriteLine($"✅ Cargados {_allMaterials.Count} materiales en MateriaPrimaWindow");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error al cargar datos en MateriaPrimaWindow: {ex.Message}");

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

        private async void LoadData()
        {
            try
            {
                TxtStatus.Text = "⏳ Cargando datos...";

                // El filtro global automáticamente excluye eliminados
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

        private async Task RefreshData()
        {
            try
            {
                BtnActualizar.IsEnabled = false;
                BtnActualizar.Content = "⏳";
                BtnActualizar.ToolTip = "Actualizando...";

                await Task.Delay(500);

                // Actualizar inventario
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
        #endregion

        #region Métodos de Actualización de UI
        private void UpdateDataGrid()
        {
            DgMateriales.ItemsSource = null;
            DgMateriales.ItemsSource = _filteredMaterials;

            // Actualizar contadores en header
            TxtContadorHeader.Text = $"{_filteredMaterials.Count} productos activos";
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

        private void UpdateDateTime()
        {
            // El binding automático debería manejar esto, pero podemos forzar actualización si es necesario
        }
        #endregion

        #region Event Handlers - Botones Principales
        private async void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectorWindow = new TipoMaterialSelectorWindow(_context)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (selectorWindow.ShowDialog() == true)
                {
                    await RefreshData();
                    TxtStatus.Text = "✅ Material agregado correctamente";

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
                    var editWindow = new EditAddStockWindow(_context, selectedMaterial)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (editWindow.ShowDialog() == true)
                    {
                        await RefreshData();
                        TxtStatus.Text = $"✅ Material actualizado - {editWindow.MotivoEdicion}";

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

                    MessageBox.Show(
                        $"✅ ¡Eliminación exitosa!\n\n" +
                        $"Producto: {materialDB.NombreArticulo}\n" +
                        $"Historial: {cantidadMovimientos + 1} movimientos\n" +
                        $"Nuevo ID: #{movimiento.Id}",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                catch (Exception ex)
                {
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
                // Intentar abrir ventana de escáner avanzada
                try
                {
                    var scannerWindow = new BarcodeScannerWindow(_context)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    scannerWindow.ShowDialog();
                }
                catch
                {
                    // Si no existe BarcodeScannerWindow, mostrar input manual
                    var inputWindow = new ManualBarcodeInputWindow(_context)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    if (inputWindow.ShowDialog() == true)
                    {
                        TxtStatus.Text = "✅ Código procesado correctamente";
                    }
                }

                // Refrescar datos después de usar el escáner
                _ = RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir escáner: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir escáner";
            }
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }
        #endregion

        #region Event Handlers - Búsqueda
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
        #endregion

        #region Manejo de Cierre de Ventana
        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                // Liberar recursos del contexto
                _context?.Dispose();

                System.Diagnostics.Debug.WriteLine("🚪 MateriaPrimaWindow cerrada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error al cerrar MateriaPrimaWindow: {ex.Message}");
            }

            base.OnClosing(e);
        }
        #endregion
    }

    #region Clase Auxiliar - ManualBarcodeInputWindow
    /// <summary>
    /// Ventana para ingreso manual de códigos de barras
    /// </summary>
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
                    var editWindow = new EditAddStockWindow(_context, existingMaterial)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
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
                    var selectorWindow = new TipoMaterialSelectorWindow(_context, CodigoIngresado)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
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
    #endregion
}