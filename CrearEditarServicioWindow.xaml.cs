using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class CrearEditarServicioWindow : Window
    {
        private AppDbContext _context;
        private ServicioVenta _servicioActual;
        private List<RawMaterial> _materialesDisponibles = new();
        private List<RawMaterial> _materialesFiltrados = new();
        private List<MaterialServicio> _materialesSeleccionados = new();
        private bool _esEdicion = false;
        private bool _controlesInicializados = false; // ✅ NUEVA VARIABLE DE CONTROL

        /// <summary>
        /// Constructor para crear nuevo servicio
        /// </summary>
        public CrearEditarServicioWindow()
        {
            InitializeComponent();
            _controlesInicializados = true; // ✅ MARCAR COMO INICIALIZADOS
            _context = new AppDbContext();
            _servicioActual = new ServicioVenta();
            _esEdicion = false;

            InitializeAsync();
        }

        /// <summary>
        /// Constructor para editar servicio existente
        /// </summary>
        public CrearEditarServicioWindow(ServicioVenta servicio)
        {
            InitializeComponent();
            _controlesInicializados = true; // ✅ MARCAR COMO INICIALIZADOS
            _context = new AppDbContext();
            _servicioActual = servicio;
            _esEdicion = true;

            TxtTituloVentana.Text = "🛍️ Editar Servicio";
            this.Title = "🛍️ Editar Servicio";

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                TxtEstadoFormulario.Text = "⏳ Cargando formulario...";

                // Cargar materiales disponibles del inventario
                await CargarMaterialesDisponibles();

                // Si es edición, cargar datos del servicio
                if (_esEdicion)
                {
                    await CargarDatosServicio();
                }

                // Actualizar interfaz
                ActualizarContadores();
                CalcularTotales();

                TxtEstadoFormulario.Text = "✅ Formulario listo para usar";
            }
            catch (Exception ex)
            {
                TxtEstadoFormulario.Text = "❌ Error al cargar formulario";
                MessageBox.Show($"Error al inicializar formulario:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Carga de Datos

        /// <summary>
        /// Carga los materiales disponibles del inventario
        /// </summary>
        private async System.Threading.Tasks.Task CargarMaterialesDisponibles()
        {
            try
            {
                // ✅ CORREGIDO: Usar campos reales en lugar de StockTotal (propiedad calculada)
                _materialesDisponibles = await _context.RawMaterials
                    .Where(m => !m.Eliminado && (m.StockAntiguo + m.StockNuevo) > 0)
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                _materialesFiltrados = new List<RawMaterial>(_materialesDisponibles);

                // ✅ VERIFICAR QUE EL CONTROL EXISTA
                if (DgMaterialesDisponibles != null)
                {
                    DgMaterialesDisponibles.ItemsSource = _materialesFiltrados;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Materiales cargados: {_materialesDisponibles.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando materiales: {ex.Message}");
                MessageBox.Show($"Error al cargar materiales del inventario:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Carga los datos del servicio para edición
        /// </summary>
        private async System.Threading.Tasks.Task CargarDatosServicio()
        {
            try
            {
                // ✅ VERIFICAR QUE LOS CONTROLES EXISTAN ANTES DE USARLOS
                if (TxtNombreServicio != null) TxtNombreServicio.Text = _servicioActual.NombreServicio;
                if (TxtDescripcion != null) TxtDescripcion.Text = _servicioActual.Descripcion;
                if (CmbCategoriaServicio != null) CmbCategoriaServicio.Text = _servicioActual.CategoriaServicio;
                if (TxtObservaciones != null) TxtObservaciones.Text = _servicioActual.Observaciones;
                if (TxtCostoManoObra != null) TxtCostoManoObra.Text = _servicioActual.CostoManoObra.ToString("F2");
                if (TxtMargenObjetivo != null) TxtMargenObjetivo.Text = _servicioActual.MargenObjetivo.ToString("F1");
                if (TxtPrecioServicio != null) TxtPrecioServicio.Text = _servicioActual.PrecioServicio.ToString("F2");

                if (ChkActivoParaVenta != null) ChkActivoParaVenta.IsChecked = _servicioActual.Activo;
                if (ChkIntegrarPOS != null) ChkIntegrarPOS.IsChecked = _servicioActual.IntegradoPOS;
                if (ChkRequiereConfirmacion != null) ChkRequiereConfirmacion.IsChecked = _servicioActual.RequiereConfirmacion;

                // Cargar materiales del servicio
                var materiales = await _context.MaterialesServicio
                    .Include(m => m.RawMaterial)
                    .Where(m => m.ServicioVentaId == _servicioActual.Id)
                    .ToListAsync();

                _materialesSeleccionados = materiales.ToList();

                if (DgMaterialesSeleccionados != null)
                {
                    DgMaterialesSeleccionados.ItemsSource = _materialesSeleccionados;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Servicio cargado: {_servicioActual.NombreServicio} con {materiales.Count} materiales");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando servicio: {ex.Message}");
                MessageBox.Show($"Error al cargar datos del servicio:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Eventos de Materiales

        /// <summary>
        /// Filtrar materiales por búsqueda
        /// </summary>
        private void TxtBuscarMaterial_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (TxtBuscarMaterial == null) return; // ✅ PROTECCIÓN NULL

                string textoBusqueda = TxtBuscarMaterial.Text.ToLower().Trim();

                if (string.IsNullOrEmpty(textoBusqueda))
                {
                    _materialesFiltrados = new List<RawMaterial>(_materialesDisponibles);
                }
                else
                {
                    _materialesFiltrados = _materialesDisponibles.Where(m =>
                        m.NombreArticulo.ToLower().Contains(textoBusqueda) ||
                        m.Categoria.ToLower().Contains(textoBusqueda)
                    ).ToList();
                }

                if (DgMaterialesDisponibles != null)
                {
                    DgMaterialesDisponibles.ItemsSource = null;
                    DgMaterialesDisponibles.ItemsSource = _materialesFiltrados;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en filtrado: {ex.Message}");
            }
        }

        /// <summary>
        /// Agregar material al servicio (doble click)
        /// </summary>
        private void DgMaterialesDisponibles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DgMaterialesDisponibles?.SelectedItem is RawMaterial material)
            {
                AgregarMaterialAlServicio(material);
            }
        }

        /// <summary>
        /// Agregar material al servicio (botón)
        /// </summary>
        private void BtnAgregarMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RawMaterial material)
            {
                AgregarMaterialAlServicio(material);
            }
        }

        /// <summary>
        /// Lógica para agregar material al servicio
        /// </summary>
        private void AgregarMaterialAlServicio(RawMaterial material)
        {
            try
            {
                // Verificar si ya está agregado
                if (_materialesSeleccionados.Any(m => m.RawMaterialId == material.Id))
                {
                    MessageBox.Show($"El material '{material.NombreArticulo}' ya está agregado al servicio.",
                                  "Material Duplicado", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Crear nuevo MaterialServicio
                var materialServicio = new MaterialServicio
                {
                    RawMaterialId = material.Id,
                    RawMaterial = material,
                    CantidadNecesaria = 0m, // ✅ CAMBIAR A 0 para que el usuario lo ponga
                    UnidadMedida = material.UnidadMedida,
                    CostoUnitario = material.PrecioConIVA,
                    VerificarDisponibilidad = true,
                    UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema"
                };

                _materialesSeleccionados.Add(materialServicio);

                // Actualizar interfaz
                if (DgMaterialesSeleccionados != null)
                {
                    DgMaterialesSeleccionados.ItemsSource = null;
                    DgMaterialesSeleccionados.ItemsSource = _materialesSeleccionados;
                }

                ActualizarContadores();
                CalcularTotales();

                if (TxtEstadoFormulario != null)
                {
                    TxtEstadoFormulario.Text = $"✅ Material '{material.NombreArticulo}' agregado";
                }
            }
            catch (Exception ex)
            {
                if (TxtEstadoFormulario != null)
                {
                    TxtEstadoFormulario.Text = "❌ Error al agregar material";
                }
                MessageBox.Show($"Error al agregar material:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Quitar material del servicio
        /// </summary>
        private void BtnQuitarMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MaterialServicio material)
            {
                try
                {
                    _materialesSeleccionados.Remove(material);

                    // Actualizar interfaz
                    if (DgMaterialesSeleccionados != null)
                    {
                        DgMaterialesSeleccionados.ItemsSource = null;
                        DgMaterialesSeleccionados.ItemsSource = _materialesSeleccionados;
                    }

                    ActualizarContadores();
                    CalcularTotales();

                    if (TxtEstadoFormulario != null)
                    {
                        TxtEstadoFormulario.Text = $"✅ Material '{material.NombreMaterial}' removido";
                    }
                }
                catch (Exception ex)
                {
                    if (TxtEstadoFormulario != null)
                    {
                        TxtEstadoFormulario.Text = "❌ Error al quitar material";
                    }
                    MessageBox.Show($"Error al quitar material:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Cantidad de material cambiada
        /// </summary>
        private void CantidadMaterial_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Recalcular totales cuando cambie la cantidad
                CalcularTotales();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cambiar cantidad: {ex.Message}");
            }
        }

        /// <summary>
        /// Seleccionar todo el texto cuando se enfoca el TextBox de cantidad
        /// </summary>
        private void CantidadTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        #endregion

        #region Cálculos y Validaciones

        /// <summary>
        /// Calcular totales del servicio
        /// </summary>
        private void CalcularTotales(object sender = null, TextChangedEventArgs e = null)
        {
            try
            {
                // ✅ VERIFICAR QUE LOS CONTROLES ESTÉN INICIALIZADOS
                if (!_controlesInicializados) return;

                // ✅ VERIFICAR QUE TODOS LOS CONTROLES NECESARIOS EXISTAN
                if (TxtCostoManoObra == null || TxtMargenObjetivo == null || TxtPrecioServicio == null ||
                    TxtPrecioBaseSugerido == null || TxtResumenMateriales == null || TxtResumenManoObra == null ||
                    TxtResumenCostoTotal == null || TxtResumenPrecio == null || TxtResumenMargen == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ CalcularTotales: Algunos controles son null, saltando cálculo");
                    return;
                }

                // Calcular costo de materiales
                decimal costoMateriales = _materialesSeleccionados.Sum(m => m.CostoTotal);

                // Obtener costo de mano de obra
                decimal.TryParse(TxtCostoManoObra.Text ?? "0", out decimal costoManoObra);

                // Calcular costo total
                decimal costoTotal = costoMateriales + costoManoObra;

                // Obtener margen objetivo
                decimal.TryParse(TxtMargenObjetivo.Text ?? "40", out decimal margenObjetivo);

                // Calcular precio sugerido
                decimal precioSugerido = costoTotal * (1 + (margenObjetivo / 100));

                // Obtener precio del servicio actual
                decimal.TryParse(TxtPrecioServicio.Text ?? "0", out decimal precioServicio);

                // Calcular margen real
                decimal margenReal = precioServicio > 0 ? ((precioServicio - costoTotal) / precioServicio) * 100 : 0;

                // Actualizar interfaz
                TxtPrecioBaseSugerido.Text = precioSugerido.ToString("F2");
                TxtResumenMateriales.Text = costoMateriales.ToString("C2");
                TxtResumenManoObra.Text = costoManoObra.ToString("C2");
                TxtResumenCostoTotal.Text = costoTotal.ToString("C2");
                TxtResumenPrecio.Text = precioServicio.ToString("C2");
                TxtResumenMargen.Text = $"{margenReal:F1}%";

                // Colorear margen según rentabilidad
                if (margenReal >= 30)
                {
                    TxtResumenMargen.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (margenReal >= 15)
                {
                    TxtResumenMargen.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    TxtResumenMargen.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en cálculos: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar contadores de interfaz
        /// </summary>
        private void ActualizarContadores()
        {
            if (TxtCountMateriales != null)
            {
                TxtCountMateriales.Text = $"{_materialesSeleccionados.Count} materiales";
            }
        }

        /// <summary>
        /// Validar datos del formulario
        /// </summary>
        private bool ValidarFormulario()
        {
            try
            {
                // Validar campos obligatorios
                if (string.IsNullOrWhiteSpace(TxtNombreServicio?.Text))
                {
                    MessageBox.Show("El nombre del servicio es obligatorio.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNombreServicio?.Focus();
                    return false;
                }

                if (CmbCategoriaServicio?.SelectedItem == null)
                {
                    MessageBox.Show("Debe seleccionar una categoría.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbCategoriaServicio?.Focus();
                    return false;
                }

                // Validar valores numéricos
                if (!decimal.TryParse(TxtPrecioServicio?.Text ?? "0", out decimal precio) || precio <= 0)
                {
                    MessageBox.Show("El precio del servicio debe ser un número mayor a 0.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPrecioServicio?.Focus();
                    return false;
                }

                // Validar que tenga al menos un material
                if (!_materialesSeleccionados.Any())
                {
                    var resultado = MessageBox.Show(
                        "No ha seleccionado ningún material para el servicio.\n\n¿Desea continuar sin materiales?",
                        "Sin Materiales", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.No)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en validación:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        #region Eventos de Botones

        /// <summary>
        /// Calcular precio sugerido
        /// </summary>
        private void BtnCalcularPrecio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtPrecioBaseSugerido != null && TxtPrecioServicio != null)
                {
                    decimal.TryParse(TxtPrecioBaseSugerido.Text, out decimal precioSugerido);
                    TxtPrecioServicio.Text = precioSugerido.ToString("F2");

                    CalcularTotales();

                    if (TxtEstadoFormulario != null)
                    {
                        TxtEstadoFormulario.Text = "💡 Precio actualizado con valor sugerido";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular precio:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Vista previa del servicio
        /// </summary>
        private void BtnVistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nombreServicio = TxtNombreServicio?.Text ?? "";
                string categoria = CmbCategoriaServicio?.Text ?? "";
                decimal.TryParse(TxtPrecioServicio?.Text ?? "0", out decimal precio);
                decimal costoMateriales = _materialesSeleccionados.Sum(m => m.CostoTotal);
                decimal.TryParse(TxtCostoManoObra?.Text ?? "0", out decimal costoManoObra);
                decimal costoTotal = costoMateriales + costoManoObra;
                decimal ganancia = precio - costoTotal;
                decimal margen = precio > 0 ? (ganancia / precio) * 100 : 0;

                string preview = $"👁️ VISTA PREVIA DEL SERVICIO\n\n" +
                               $"📋 INFORMACIÓN BÁSICA:\n" +
                               $"   • Nombre: {nombreServicio}\n" +
                               $"   • Categoría: {categoria}\n" +
                               $"   • Observaciones: {TxtObservaciones?.Text ?? ""}\n\n" +
                               $"💰 ANÁLISIS FINANCIERO:\n" +
                               $"   • Costo materiales: {costoMateriales:C2}\n" +
                               $"   • Costo mano de obra: {costoManoObra:C2}\n" +
                               $"   • Costo total: {costoTotal:C2}\n" +
                               $"   • Precio servicio: {precio:C2}\n" +
                               $"   • Ganancia: {ganancia:C2}\n" +
                               $"   • Margen: {margen:F1}%\n\n" +
                               $"📦 MATERIALES NECESARIOS:\n";

                foreach (var material in _materialesSeleccionados)
                {
                    preview += $"   • {material.NombreMaterial}: {material.CantidadNecesaria:F2} {material.UnidadMedida}\n";
                }

                MessageBox.Show(preview, "Vista Previa del Servicio",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en vista previa:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Guardar servicio
        /// </summary>
        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                if (BtnGuardar != null)
                {
                    BtnGuardar.IsEnabled = false;
                }

                if (TxtEstadoFormulario != null)
                {
                    TxtEstadoFormulario.Text = "💾 Guardando servicio...";
                }

                // Actualizar datos del servicio
                _servicioActual.NombreServicio = TxtNombreServicio?.Text?.Trim() ?? "";
                _servicioActual.Descripcion = TxtDescripcion?.Text?.Trim() ?? "";
                _servicioActual.CategoriaServicio = CmbCategoriaServicio?.Text ?? "";
                _servicioActual.Observaciones = TxtObservaciones?.Text?.Trim() ?? "";
                _servicioActual.DuracionEstimada = "Variable"; // Duración estándar

                decimal.TryParse(TxtCostoManoObra?.Text ?? "0", out decimal costoManoObra);
                _servicioActual.CostoManoObra = costoManoObra;

                decimal.TryParse(TxtMargenObjetivo?.Text ?? "40", out decimal margenObjetivo);
                _servicioActual.MargenObjetivo = margenObjetivo;

                decimal.TryParse(TxtPrecioServicio?.Text ?? "0", out decimal precioServicio);
                _servicioActual.PrecioServicio = precioServicio;
                _servicioActual.PrecioBase = precioServicio / (1 + (_servicioActual.PorcentajeIVA / 100));

                _servicioActual.Activo = ChkActivoParaVenta?.IsChecked ?? true;
                _servicioActual.IntegradoPOS = ChkIntegrarPOS?.IsChecked ?? true; // ✅ POR DEFECTO ACTIVADO
                _servicioActual.RequiereConfirmacion = ChkRequiereConfirmacion?.IsChecked ?? false;

                // Calcular costos
                _servicioActual.CostoMateriales = _materialesSeleccionados.Sum(m => m.CostoTotal);

                if (!_esEdicion)
                {
                    // Nuevo servicio
                    _servicioActual.UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema";
                    _context.ServiciosVenta.Add(_servicioActual);
                }

                await _context.SaveChangesAsync();

                // Guardar materiales
                if (_esEdicion)
                {
                    // Eliminar materiales existentes
                    var materialesExistentes = await _context.MaterialesServicio
                        .Where(m => m.ServicioVentaId == _servicioActual.Id)
                        .ToListAsync();
                    _context.MaterialesServicio.RemoveRange(materialesExistentes);
                }

                // Agregar materiales actuales
                foreach (var material in _materialesSeleccionados)
                {
                    material.ServicioVentaId = _servicioActual.Id;
                    _context.MaterialesServicio.Add(material);
                }

                await _context.SaveChangesAsync();

                // Configurar POS si está marcado
                if (_servicioActual.IntegradoPOS)
                {
                    _servicioActual.ConfigurarParaPOS(true);
                    await _context.SaveChangesAsync();
                }

                MessageBox.Show($"✅ Servicio '{_servicioActual.NombreServicio}' guardado exitosamente!\n\n" +
                              $"ID: {_servicioActual.Id}\n" +
                              $"Precio: {_servicioActual.PrecioServicio:C2}\n" +
                              $"Materiales: {_materialesSeleccionados.Count}\n" +
                              $"Margen: {_servicioActual.MargenReal:F1}%",
                              "Servicio Guardado", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                if (TxtEstadoFormulario != null)
                {
                    TxtEstadoFormulario.Text = "❌ Error al guardar servicio";
                }
                MessageBox.Show($"Error al guardar servicio:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (BtnGuardar != null)
                {
                    BtnGuardar.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Cancelar operación
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro que desea cancelar?\n\nSe perderán todos los cambios no guardados.",
                    "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cancelar:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _context?.Dispose();
                _context = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar CrearEditarServicioWindow: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}