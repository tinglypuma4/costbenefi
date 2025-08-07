using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi
{
    public partial class CrearEditarProcesoWindow : Window
    {
        private AppDbContext _context;
        private ProcesoFabricacion _procesoActual;
        private List<RawMaterial> _materialesDisponibles = new();
        private List<RawMaterial> _materialesFiltrados = new();
        private List<RecetaDetalleExtendido> _ingredientesSeleccionados = new();
        private bool _esEdicion = false;
        private bool _controlesInicializados = false;

        /// <summary>
        /// Constructor para crear nuevo proceso
        /// </summary>
        public CrearEditarProcesoWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _procesoActual = new ProcesoFabricacion();
            _esEdicion = false;
            _controlesInicializados = true;

            InitializeAsync();
        }

        /// <summary>
        /// Constructor para editar proceso existente
        /// </summary>
        public CrearEditarProcesoWindow(ProcesoFabricacion proceso)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _procesoActual = proceso;
            _esEdicion = true;
            _controlesInicializados = true;

            TxtTituloVentana.Text = "✏️ Editar Proceso de Fabricación";
            this.Title = "✏️ Editar Proceso de Fabricación";

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                TxtEstadoFormulario.Text = "⏳ Cargando formulario...";

                // Cargar materiales disponibles del inventario
                await CargarMaterialesDisponibles();

                // Si es edición, cargar datos del proceso
                if (_esEdicion)
                {
                    await CargarDatosProceso();
                }

                // Configurar eventos para costos opcionales
                ConfigurarCostosOpcionales();

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
                _materialesDisponibles = await _context.RawMaterials
                    .Where(m => !m.Eliminado && (m.StockAntiguo + m.StockNuevo) > 0)
                    .OrderBy(m => m.NombreArticulo)
                    .ToListAsync();

                _materialesFiltrados = new List<RawMaterial>(_materialesDisponibles);

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
        /// Carga los datos del proceso para edición
        /// </summary>
        private async System.Threading.Tasks.Task CargarDatosProceso()
        {
            try
            {
                // Cargar datos básicos
                TxtNombreProducto.Text = _procesoActual.NombreProducto ?? "";
                TxtDescripcion.Text = _procesoActual.Descripcion ?? "";
                CmbCategoriaProducto.Text = _procesoActual.CategoriaProducto ?? "";
                TxtNotasEspeciales.Text = _procesoActual.NotasEspeciales ?? "";

                // Cargar especificaciones
                TxtRendimientoEsperado.Text = _procesoActual.RendimientoEsperado.ToString("F2");
                CmbUnidadMedidaProducto.Text = _procesoActual.UnidadMedidaProducto ?? "";
                TxtTiempoFabricacionMinutos.Text = _procesoActual.TiempoFabricacionMinutos.ToString();
                TxtPorcentajeMerma.Text = _procesoActual.PorcentajeMerma.ToString("F2");

                // Cargar costos
                TxtCostoManoObra.Text = _procesoActual.CostoManoObra.ToString("F2");
                TxtMargenObjetivo.Text = _procesoActual.MargenObjetivo.ToString("F1");

                // Cargar costos opcionales
                ChkIncluirCostoEnergia.IsChecked = _procesoActual.IncluirCostoEnergia;
                TxtCostoEnergia.Text = _procesoActual.CostoEnergia.ToString("F2");

                ChkIncluirCostoTransporte.IsChecked = _procesoActual.IncluirCostoTransporte;
                TxtCostoTransporte.Text = _procesoActual.CostoTransporte.ToString("F2");

                ChkIncluirCostoEmpaque.IsChecked = _procesoActual.IncluirCostoEmpaque;
                TxtCostoEmpaque.Text = _procesoActual.CostoEmpaque.ToString("F2");

                ChkIncluirOtrosCostos.IsChecked = _procesoActual.IncluirOtrosCostos;
                TxtOtrosCostos.Text = _procesoActual.OtrosCostos.ToString("F2");
                TxtDescripcionOtrosCostos.Text = _procesoActual.DescripcionOtrosCostos ?? "";

                // Configuración
                CmbTipoFabricacion.Text = _procesoActual.TipoFabricacion ?? "Lote";
                ChkActivoProceso.IsChecked = _procesoActual.Activo;

                // Cargar ingredientes del proceso
                var ingredientes = await _context.RecetaDetalles
                    .Include(r => r.RawMaterial)
                    .Where(r => r.ProcesoFabricacionId == _procesoActual.Id)
                    .OrderBy(r => r.OrdenAdicion)
                    .ToListAsync();

                _ingredientesSeleccionados.Clear();
                foreach (var ingrediente in ingredientes)
                {
                    var ingredienteExtendido = new RecetaDetalleExtendido
                    {
                        Id = ingrediente.Id,
                        ProcesoFabricacionId = ingrediente.ProcesoFabricacionId,
                        RawMaterialId = ingrediente.RawMaterialId,
                        RawMaterial = ingrediente.RawMaterial,
                        CantidadRequerida = ingrediente.CantidadRequerida,
                        UnidadMedida = ingrediente.UnidadMedida,
                        CostoUnitario = ingrediente.CostoUnitario,
                        EsIngredientePrincipal = ingrediente.EsIngredientePrincipal,
                        OrdenAdicion = ingrediente.OrdenAdicion,
                        NotasIngrediente = ingrediente.NotasIngrediente
                    };
                    _ingredientesSeleccionados.Add(ingredienteExtendido);
                }

                DgIngredientesSeleccionados.ItemsSource = _ingredientesSeleccionados;

                System.Diagnostics.Debug.WriteLine($"✅ Proceso cargado: {_procesoActual.NombreProducto} con {ingredientes.Count} ingredientes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando proceso: {ex.Message}");
                MessageBox.Show($"Error al cargar datos del proceso:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Configuración de Controles

        private void ConfigurarCostosOpcionales()
        {
            // Configurar habilitación inicial de campos de costos opcionales
            OnCostoOpcionalChanged(null, null);
        }

        #endregion

        #region Eventos de Costos Opcionales

        private void OnCostoOpcionalChanged(object sender, RoutedEventArgs e)
        {
            if (!_controlesInicializados) return;

            try
            {
                // Costo Energía
                if (TxtCostoEnergia != null && ChkIncluirCostoEnergia != null)
                    TxtCostoEnergia.IsEnabled = ChkIncluirCostoEnergia.IsChecked == true;

                // Costo Transporte
                if (TxtCostoTransporte != null && ChkIncluirCostoTransporte != null)
                    TxtCostoTransporte.IsEnabled = ChkIncluirCostoTransporte.IsChecked == true;

                // Costo Empaque
                if (TxtCostoEmpaque != null && ChkIncluirCostoEmpaque != null)
                    TxtCostoEmpaque.IsEnabled = ChkIncluirCostoEmpaque.IsChecked == true;

                // Otros Costos
                if (TxtOtrosCostos != null && ChkIncluirOtrosCostos != null)
                {
                    bool habilitarOtros = ChkIncluirOtrosCostos.IsChecked == true;
                    TxtOtrosCostos.IsEnabled = habilitarOtros;
                    if (TxtDescripcionOtrosCostos != null)
                        TxtDescripcionOtrosCostos.IsEnabled = habilitarOtros;
                }

                CalcularTotales();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnCostoOpcionalChanged: {ex.Message}");
            }
        }

        #endregion

        #region Eventos de Ingredientes

        /// <summary>
        /// Filtrar materiales por búsqueda
        /// </summary>
        private void TxtBuscarMaterial_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (TxtBuscarMaterial == null) return;

                string textoBusqueda = TxtBuscarMaterial.Text.ToLower().Trim();

                if (string.IsNullOrEmpty(textoBusqueda))
                {
                    _materialesFiltrados = new List<RawMaterial>(_materialesDisponibles);
                }
                else
                {
                    _materialesFiltrados = _materialesDisponibles.Where(m =>
                        m.NombreArticulo.ToLower().Contains(textoBusqueda) ||
                        (m.Categoria?.ToLower().Contains(textoBusqueda) ?? false)
                    ).ToList();
                }

                DgMaterialesDisponibles.ItemsSource = null;
                DgMaterialesDisponibles.ItemsSource = _materialesFiltrados;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en filtrado: {ex.Message}");
            }
        }

        /// <summary>
        /// Agregar ingrediente al proceso (doble click)
        /// </summary>
        private void DgMaterialesDisponibles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DgMaterialesDisponibles?.SelectedItem is RawMaterial material)
            {
                AgregarIngredienteAlProceso(material);
            }
        }

        /// <summary>
        /// Agregar ingrediente al proceso (botón)
        /// </summary>
        private void BtnAgregarIngrediente_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RawMaterial material)
            {
                AgregarIngredienteAlProceso(material);
            }
        }

        /// <summary>
        /// Lógica para agregar ingrediente al proceso
        /// </summary>
        private void AgregarIngredienteAlProceso(RawMaterial material)
        {
            try
            {
                // Verificar si ya está agregado
                if (_ingredientesSeleccionados.Any(i => i.RawMaterialId == material.Id))
                {
                    MessageBox.Show($"El ingrediente '{material.NombreArticulo}' ya está agregado al proceso.",
                                  "Ingrediente Duplicado", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Crear nuevo RecetaDetalleExtendido
                var ingrediente = new RecetaDetalleExtendido
                {
                    RawMaterialId = material.Id,
                    RawMaterial = material,
                    CantidadRequerida = 0m, // Usuario debe especificar
                    UnidadMedida = material.UnidadMedida,
                    CostoUnitario = material.PrecioConIVA,
                    OrdenAdicion = _ingredientesSeleccionados.Count + 1,
                    EsIngredientePrincipal = _ingredientesSeleccionados.Count == 0 // Primer ingrediente es principal
                };

                _ingredientesSeleccionados.Add(ingrediente);

                // Actualizar interfaz
                DgIngredientesSeleccionados.ItemsSource = null;
                DgIngredientesSeleccionados.ItemsSource = _ingredientesSeleccionados;

                ActualizarContadores();
                CalcularTotales();

                TxtEstadoFormulario.Text = $"✅ Ingrediente '{material.NombreArticulo}' agregado";
            }
            catch (Exception ex)
            {
                TxtEstadoFormulario.Text = "❌ Error al agregar ingrediente";
                MessageBox.Show($"Error al agregar ingrediente:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Quitar ingrediente del proceso
        /// </summary>
        private void BtnQuitarIngrediente_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RecetaDetalleExtendido ingrediente)
            {
                try
                {
                    _ingredientesSeleccionados.Remove(ingrediente);

                    // Reordenar ingredientes
                    for (int i = 0; i < _ingredientesSeleccionados.Count; i++)
                    {
                        _ingredientesSeleccionados[i].OrdenAdicion = i + 1;
                    }

                    // Actualizar interfaz
                    DgIngredientesSeleccionados.ItemsSource = null;
                    DgIngredientesSeleccionados.ItemsSource = _ingredientesSeleccionados;

                    ActualizarContadores();
                    CalcularTotales();

                    TxtEstadoFormulario.Text = $"✅ Ingrediente '{ingrediente.NombreIngrediente}' removido";
                }
                catch (Exception ex)
                {
                    TxtEstadoFormulario.Text = "❌ Error al quitar ingrediente";
                    MessageBox.Show($"Error al quitar ingrediente:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Cantidad de ingrediente cambiada
        /// </summary>
        private void CantidadIngrediente_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
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

        #region Eventos de Campos

        /// <summary>
        /// Evento cuando cambia cualquier campo del formulario
        /// </summary>
        private void OnCampoChanged(object sender, EventArgs e)
        {
            if (!_controlesInicializados) return;

            try
            {
                CalcularTotales();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnCampoChanged: {ex.Message}");
            }
        }

        #endregion

        #region Cálculos y Validaciones

        /// <summary>
        /// Calcular totales del proceso
        /// </summary>
        private void CalcularTotales()
        {
            try
            {
                if (!_controlesInicializados) return;

                // Verificar que todos los controles necesarios existan
                if (TxtResumenMateriales == null || TxtResumenManoObra == null ||
                    TxtResumenCostosAdicionales == null || TxtResumenCostoTotal == null ||
                    TxtResumenCostoUnitario == null || TxtResumenPrecioSugerido == null ||
                    TxtResumenPuedeFabricarse == null)
                {
                    return;
                }

                // Calcular costo de materiales
                decimal costoMateriales = _ingredientesSeleccionados.Sum(i => i.CostoTotal);

                // Obtener costo de mano de obra
                decimal.TryParse(TxtCostoManoObra?.Text ?? "0", out decimal costoManoObra);

                // Calcular costos adicionales
                decimal costosAdicionales = 0;

                if (ChkIncluirCostoEnergia?.IsChecked == true)
                {
                    decimal.TryParse(TxtCostoEnergia?.Text ?? "0", out decimal costoEnergia);
                    costosAdicionales += costoEnergia;
                }

                if (ChkIncluirCostoTransporte?.IsChecked == true)
                {
                    decimal.TryParse(TxtCostoTransporte?.Text ?? "0", out decimal costoTransporte);
                    costosAdicionales += costoTransporte;
                }

                if (ChkIncluirCostoEmpaque?.IsChecked == true)
                {
                    decimal.TryParse(TxtCostoEmpaque?.Text ?? "0", out decimal costoEmpaque);
                    costosAdicionales += costoEmpaque;
                }

                if (ChkIncluirOtrosCostos?.IsChecked == true)
                {
                    decimal.TryParse(TxtOtrosCostos?.Text ?? "0", out decimal otrosCostos);
                    costosAdicionales += otrosCostos;
                }

                // Calcular costo total por lote
                decimal costoTotalPorLote = costoMateriales + costoManoObra + costosAdicionales;

                // Obtener rendimiento esperado y merma
                decimal.TryParse(TxtRendimientoEsperado?.Text ?? "0", out decimal rendimiento);
                decimal.TryParse(TxtPorcentajeMerma?.Text ?? "0", out decimal porcentajeMerma);

                // Calcular rendimiento con merma
                decimal rendimientoConMerma = rendimiento * (1 - porcentajeMerma / 100);

                // Calcular costo unitario
                decimal costoUnitario = rendimientoConMerma > 0 ? costoTotalPorLote / rendimientoConMerma : 0;

                // Obtener margen objetivo y calcular precio sugerido
                decimal.TryParse(TxtMargenObjetivo?.Text ?? "30", out decimal margenObjetivo);
                decimal precioSugerido = costoUnitario * (1 + (margenObjetivo / 100));

                // Verificar si puede fabricarse
                bool puedeFabricarse = _ingredientesSeleccionados.Any() &&
                                      _ingredientesSeleccionados.All(i => i.PuedeUsarse);

                // Actualizar interfaz
                TxtResumenMateriales.Text = costoMateriales.ToString("C2");
                TxtResumenManoObra.Text = costoManoObra.ToString("C2");
                TxtResumenCostosAdicionales.Text = costosAdicionales.ToString("C2");
                TxtResumenCostoTotal.Text = costoTotalPorLote.ToString("C2");
                TxtResumenCostoUnitario.Text = costoUnitario.ToString("C4");
                TxtResumenPrecioSugerido.Text = precioSugerido.ToString("C2");

                // Actualizar indicador de fabricabilidad
                if (puedeFabricarse && _ingredientesSeleccionados.Any())
                {
                    TxtResumenPuedeFabricarse.Text = "✅ SÍ";
                    TxtResumenPuedeFabricarse.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    TxtResumenPuedeFabricarse.Text = "❌ NO";
                    TxtResumenPuedeFabricarse.Foreground = System.Windows.Media.Brushes.Red;
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
            if (TxtCountIngredientes != null)
            {
                TxtCountIngredientes.Text = $"{_ingredientesSeleccionados.Count} ingredientes";
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
                if (string.IsNullOrWhiteSpace(TxtNombreProducto?.Text))
                {
                    MessageBox.Show("El nombre del producto es obligatorio.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNombreProducto?.Focus();
                    return false;
                }

                if (CmbCategoriaProducto?.SelectedItem == null)
                {
                    MessageBox.Show("Debe seleccionar una categoría.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbCategoriaProducto?.Focus();
                    return false;
                }

                // Validar rendimiento esperado
                if (!decimal.TryParse(TxtRendimientoEsperado?.Text ?? "0", out decimal rendimiento) || rendimiento <= 0)
                {
                    MessageBox.Show("El rendimiento esperado debe ser un número mayor a 0.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtRendimientoEsperado?.Focus();
                    return false;
                }

                if (CmbUnidadMedidaProducto?.SelectedItem == null)
                {
                    MessageBox.Show("Debe seleccionar una unidad de medida.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbUnidadMedidaProducto?.Focus();
                    return false;
                }

                // Validar que tenga al menos un ingrediente
                if (!_ingredientesSeleccionados.Any())
                {
                    var resultado = MessageBox.Show(
                        "No ha seleccionado ningún ingrediente para el proceso.\n\n¿Desea continuar sin ingredientes?",
                        "Sin Ingredientes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.No)
                        return false;
                }
                else
                {
                    // Validar que todos los ingredientes tengan cantidad > 0
                    var ingredientesSinCantidad = _ingredientesSeleccionados.Where(i => i.CantidadRequerida <= 0).ToList();
                    if (ingredientesSinCantidad.Any())
                    {
                        MessageBox.Show($"Los siguientes ingredientes no tienen cantidad especificada:\n\n" +
                                       string.Join("\n", ingredientesSinCantidad.Select(i => $"• {i.NombreIngrediente}")),
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
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
        /// Recalcular costos
        /// </summary>
        private void BtnRecalcularCostos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Actualizar costos de ingredientes con precios actuales
                foreach (var ingrediente in _ingredientesSeleccionados)
                {
                    if (ingrediente.RawMaterial != null)
                    {
                        ingrediente.CostoUnitario = ingrediente.RawMaterial.PrecioConIVA;
                    }
                }

                // Actualizar interfaz
                DgIngredientesSeleccionados.ItemsSource = null;
                DgIngredientesSeleccionados.ItemsSource = _ingredientesSeleccionados;

                CalcularTotales();

                TxtEstadoFormulario.Text = "💡 Costos actualizados con precios actuales";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al recalcular costos:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Probar receta
        /// </summary>
        private void BtnProbarReceta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_ingredientesSeleccionados.Any())
                {
                    MessageBox.Show("No hay ingredientes en la receta para probar.",
                                  "Sin Ingredientes", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var ingredientesConProblemas = _ingredientesSeleccionados.Where(i => !i.PuedeUsarse).ToList();

                if (ingredientesConProblemas.Any())
                {
                    string mensaje = "⚠️ PROBLEMAS DETECTADOS EN LA RECETA:\n\n";
                    foreach (var ingrediente in ingredientesConProblemas)
                    {
                        mensaje += $"❌ {ingrediente.NombreIngrediente}:\n" +
                                  $"   Necesario: {ingrediente.CantidadRequerida:F2} {ingrediente.UnidadMedida}\n" +
                                  $"   Disponible: {ingrediente.StockDisponible:F2} {ingrediente.UnidadMedida}\n" +
                                  $"   Faltante: {Math.Max(0, ingrediente.CantidadRequerida - ingrediente.StockDisponible):F2}\n\n";
                    }

                    MessageBox.Show(mensaje, "Problemas en la Receta",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("✅ ¡La receta está lista para fabricación!\n\n" +
                                   "Todos los ingredientes están disponibles en las cantidades necesarias.",
                                   "Receta Lista", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al probar receta:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Vista previa del proceso
        /// </summary>
        private void BtnVistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nombreProducto = TxtNombreProducto?.Text ?? "";
                string categoria = CmbCategoriaProducto?.Text ?? "";
                decimal.TryParse(TxtRendimientoEsperado?.Text ?? "0", out decimal rendimiento);
                string unidad = CmbUnidadMedidaProducto?.Text ?? "";
                decimal costoMateriales = _ingredientesSeleccionados.Sum(i => i.CostoTotal);
                decimal.TryParse(TxtCostoManoObra?.Text ?? "0", out decimal costoManoObra);

                // Calcular costos adicionales para la vista previa
                decimal costosAdicionales = 0;
                if (ChkIncluirCostoEnergia?.IsChecked == true)
                    decimal.TryParse(TxtCostoEnergia?.Text ?? "0", out costosAdicionales);
                if (ChkIncluirCostoTransporte?.IsChecked == true)
                {
                    decimal.TryParse(TxtCostoTransporte?.Text ?? "0", out decimal costoTransporte);
                    costosAdicionales += costoTransporte;
                }
                if (ChkIncluirCostoEmpaque?.IsChecked == true)
                {
                    decimal.TryParse(TxtCostoEmpaque?.Text ?? "0", out decimal costoEmpaque);
                    costosAdicionales += costoEmpaque;
                }
                if (ChkIncluirOtrosCostos?.IsChecked == true)
                {
                    decimal.TryParse(TxtOtrosCostos?.Text ?? "0", out decimal otrosCostos);
                    costosAdicionales += otrosCostos;
                }

                decimal costoTotal = costoMateriales + costoManoObra + costosAdicionales;
                decimal.TryParse(TxtPorcentajeMerma?.Text ?? "0", out decimal merma);
                decimal rendimientoReal = rendimiento * (1 - merma / 100);
                decimal costoUnitario = rendimientoReal > 0 ? costoTotal / rendimientoReal : 0;
                decimal.TryParse(TxtMargenObjetivo?.Text ?? "30", out decimal margen);
                decimal precioSugerido = costoUnitario * (1 + margen / 100);

                string preview = $"👁️ VISTA PREVIA DEL PROCESO DE FABRICACIÓN\n\n" +
                               $"📋 INFORMACIÓN BÁSICA:\n" +
                               $"   • Nombre: {nombreProducto}\n" +
                               $"   • Categoría: {categoria}\n" +
                               $"   • Rendimiento: {rendimiento:F2} {unidad}\n" +
                               $"   • Merma esperada: {merma:F1}%\n" +
                               $"   • Rendimiento real: {rendimientoReal:F2} {unidad}\n" +
                               $"   • Tiempo estimado: {TxtTiempoFabricacionMinutos?.Text ?? "0"} minutos\n\n" +
                               $"💰 ANÁLISIS FINANCIERO:\n" +
                               $"   • Costo materiales: {costoMateriales:C2}\n" +
                               $"   • Costo mano de obra: {costoManoObra:C2}\n" +
                               $"   • Costos adicionales: {costosAdicionales:C2}\n" +
                               $"   • Costo total por lote: {costoTotal:C2}\n" +
                               $"   • Costo unitario: {costoUnitario:C4} por {unidad}\n" +
                               $"   • Precio sugerido: {precioSugerido:C2} por {unidad}\n" +
                               $"   • Margen objetivo: {margen:F1}%\n\n" +
                               $"🧪 INGREDIENTES NECESARIOS:\n";

                if (_ingredientesSeleccionados.Any())
                {
                    foreach (var ingrediente in _ingredientesSeleccionados.OrderBy(i => i.OrdenAdicion))
                    {
                        string estado = ingrediente.PuedeUsarse ? "✅" : "❌";
                        preview += $"   {ingrediente.OrdenAdicion}. {estado} {ingrediente.NombreIngrediente}: " +
                                  $"{ingrediente.CantidadRequerida:F2} {ingrediente.UnidadMedida}\n";
                    }
                }
                else
                {
                    preview += "   Sin ingredientes configurados\n";
                }

                MessageBox.Show(preview, "Vista Previa del Proceso",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en vista previa:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Guardar proceso
        /// </summary>
        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarFormulario())
                    return;

                BtnGuardar.IsEnabled = false;
                TxtEstadoFormulario.Text = "💾 Guardando proceso...";

                // Usar contexto fresco para evitar conflictos
                using var contextGuardado = new AppDbContext();
                ProcesoFabricacion procesoGuardar;

                if (_esEdicion)
                {
                    // Buscar el proceso en el contexto fresco
                    procesoGuardar = await contextGuardado.ProcesosFabricacion
                        .FirstOrDefaultAsync(p => p.Id == _procesoActual.Id);

                    if (procesoGuardar == null)
                    {
                        throw new InvalidOperationException("Proceso no encontrado en la base de datos");
                    }
                }
                else
                {
                    // Nuevo proceso
                    procesoGuardar = new ProcesoFabricacion();
                    contextGuardado.ProcesosFabricacion.Add(procesoGuardar);
                }

                // Actualizar datos del proceso
                procesoGuardar.NombreProducto = TxtNombreProducto?.Text?.Trim() ?? "";
                procesoGuardar.Descripcion = TxtDescripcion?.Text?.Trim() ?? "";
                procesoGuardar.CategoriaProducto = CmbCategoriaProducto?.Text ?? "";
                procesoGuardar.NotasEspeciales = TxtNotasEspeciales?.Text?.Trim() ?? "";

                decimal.TryParse(TxtRendimientoEsperado?.Text ?? "0", out decimal rendimiento);
                procesoGuardar.RendimientoEsperado = rendimiento;
                procesoGuardar.UnidadMedidaProducto = CmbUnidadMedidaProducto?.Text ?? "";

                int.TryParse(TxtTiempoFabricacionMinutos?.Text ?? "60", out int tiempo);
                procesoGuardar.TiempoFabricacionMinutos = tiempo;

                decimal.TryParse(TxtPorcentajeMerma?.Text ?? "0", out decimal merma);
                procesoGuardar.PorcentajeMerma = merma;

                decimal.TryParse(TxtCostoManoObra?.Text ?? "0", out decimal costoManoObra);
                procesoGuardar.CostoManoObra = costoManoObra;

                decimal.TryParse(TxtMargenObjetivo?.Text ?? "30", out decimal margen);
                procesoGuardar.MargenObjetivo = margen;

                // Costos opcionales
                procesoGuardar.IncluirCostoEnergia = ChkIncluirCostoEnergia?.IsChecked ?? false;
                decimal.TryParse(TxtCostoEnergia?.Text ?? "0", out decimal costoEnergia);
                procesoGuardar.CostoEnergia = costoEnergia;

                procesoGuardar.IncluirCostoTransporte = ChkIncluirCostoTransporte?.IsChecked ?? false;
                decimal.TryParse(TxtCostoTransporte?.Text ?? "0", out decimal costoTransporte);
                procesoGuardar.CostoTransporte = costoTransporte;

                procesoGuardar.IncluirCostoEmpaque = ChkIncluirCostoEmpaque?.IsChecked ?? false;
                decimal.TryParse(TxtCostoEmpaque?.Text ?? "0", out decimal costoEmpaque);
                procesoGuardar.CostoEmpaque = costoEmpaque;

                procesoGuardar.IncluirOtrosCostos = ChkIncluirOtrosCostos?.IsChecked ?? false;
                decimal.TryParse(TxtOtrosCostos?.Text ?? "0", out decimal otrosCostos);
                procesoGuardar.OtrosCostos = otrosCostos;
                procesoGuardar.DescripcionOtrosCostos = TxtDescripcionOtrosCostos?.Text?.Trim() ?? "";

                procesoGuardar.TipoFabricacion = CmbTipoFabricacion?.Text ?? "Lote";
                procesoGuardar.Activo = ChkActivoProceso?.IsChecked ?? true;

                if (!_esEdicion)
                {
                    procesoGuardar.UsuarioCreador = Environment.UserName;
                }

                // Guardar proceso primero
                await contextGuardado.SaveChangesAsync();

                // Manejar ingredientes
                if (_esEdicion)
                {
                    // Eliminar ingredientes existentes
                    var ingredientesExistentes = await contextGuardado.RecetaDetalles
                        .Where(r => r.ProcesoFabricacionId == procesoGuardar.Id)
                        .ToListAsync();

                    if (ingredientesExistentes.Any())
                    {
                        contextGuardado.RecetaDetalles.RemoveRange(ingredientesExistentes);
                        await contextGuardado.SaveChangesAsync();
                    }
                }

                // Crear ingredientes nuevos
                foreach (var ingrediente in _ingredientesSeleccionados)
                {
                    var nuevoIngrediente = new RecetaDetalle
                    {
                        ProcesoFabricacionId = procesoGuardar.Id,
                        RawMaterialId = ingrediente.RawMaterialId,
                        CantidadRequerida = ingrediente.CantidadRequerida,
                        UnidadMedida = ingrediente.UnidadMedida,
                        CostoUnitario = ingrediente.CostoUnitario,
                        EsIngredientePrincipal = ingrediente.EsIngredientePrincipal,
                        OrdenAdicion = ingrediente.OrdenAdicion,
                        NotasIngrediente = ingrediente.NotasIngrediente ?? ""
                    };

                    contextGuardado.RecetaDetalles.Add(nuevoIngrediente);
                }

                // Guardar ingredientes
                await contextGuardado.SaveChangesAsync();

                // Actualizar el proceso actual con el ID
                if (!_esEdicion)
                {
                    _procesoActual.Id = procesoGuardar.Id;
                }

                MessageBox.Show($"✅ Proceso '{procesoGuardar.NombreProducto}' guardado exitosamente!\n\n" +
                              $"ID: {procesoGuardar.Id}\n" +
                              $"Rendimiento: {procesoGuardar.RendimientoEsperado:F2} {procesoGuardar.UnidadMedidaProducto}\n" +
                              $"Ingredientes: {_ingredientesSeleccionados.Count}\n" +
                              $"Costo unitario: {(procesoGuardar.CostoUnitarioEstimado > 0 ? procesoGuardar.CostoUnitarioEstimado.ToString("C4") : "Pendiente")}",
                              "Proceso Guardado", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtEstadoFormulario.Text = "❌ Error al guardar proceso";

                string errorDetallado = $"Error al guardar proceso:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetallado += $"\n\nDetalle: {ex.InnerException.Message}";
                }

                MessageBox.Show(errorDetallado, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ ERROR GUARDANDO PROCESO: {ex}");
            }
            finally
            {
                BtnGuardar.IsEnabled = true;
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
                System.Diagnostics.Debug.WriteLine($"Error al cerrar CrearEditarProcesoWindow: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Clase extendida para RecetaDetalle con propiedades calculadas
    /// </summary>
    public class RecetaDetalleExtendido : INotifyPropertyChanged
    {
        private decimal _cantidadRequerida;

        public int Id { get; set; }
        public int ProcesoFabricacionId { get; set; }
        public int RawMaterialId { get; set; }
        public RawMaterial RawMaterial { get; set; }

        public decimal CantidadRequerida
        {
            get => _cantidadRequerida;
            set
            {
                _cantidadRequerida = value;
                OnPropertyChanged(nameof(CantidadRequerida));
                OnPropertyChanged(nameof(CostoTotal));
                OnPropertyChanged(nameof(PuedeUsarse));
            }
        }

        public string UnidadMedida { get; set; }
        public decimal CostoUnitario { get; set; }
        public bool EsIngredientePrincipal { get; set; }
        public int OrdenAdicion { get; set; }
        public string NotasIngrediente { get; set; }

        // Propiedades calculadas
        public string NombreIngrediente => RawMaterial?.NombreArticulo ?? "";
        public decimal StockDisponible => RawMaterial?.StockTotal ?? 0;
        public decimal CostoTotal => CantidadRequerida * CostoUnitario;
        public bool PuedeUsarse => CantidadRequerida > 0 && StockDisponible >= CantidadRequerida;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}