using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using System.Threading.Tasks;
namespace costbenefi
{
    public partial class EjecutarFabricacionWindow : Window
    {
        private AppDbContext _context;
        private ProcesoFabricacion _proceso;
        private List<IngredienteLoteExtendido> _ingredientesNecesarios = new();
        private bool _controlesInicializados = false;
        private decimal _cantidadMaximaFabricable = 0;

        /// <summary>
        /// Constructor para ejecutar fabricación de un proceso específico
        /// </summary>
        public EjecutarFabricacionWindow(ProcesoFabricacion proceso)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _proceso = proceso ?? throw new ArgumentNullException(nameof(proceso));

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                TxtEstadoVentana.Text = "⏳ Cargando proceso de fabricación...";

                // Cargar datos del proceso con ingredientes
                await CargarDatosProceso();

                // Generar número de lote inicial
                GenerarNumeroLote();

                // Calcular cantidad máxima fabricable
                await CalcularCantidadMaxima();

                // Configurar valores iniciales
                ConfigurarValoresIniciales();

                // Actualizar cálculos
                _controlesInicializados = true;
                ActualizarCalculos();

                TxtEstadoVentana.Text = "✅ Listo para configurar fabricación";
            }
            catch (Exception ex)
            {
                TxtEstadoVentana.Text = "❌ Error al cargar proceso";
                MessageBox.Show($"Error al inicializar fabricación:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Carga de Datos

        /// <summary>
        /// Carga los datos del proceso desde la base de datos
        /// </summary>
        private async System.Threading.Tasks.Task CargarDatosProceso()
        {
            try
            {
                // Recargar proceso con todas las relaciones
                _proceso = await _context.ProcesosFabricacion
                    .Include(p => p.Ingredientes)
                        .ThenInclude(i => i.RawMaterial)
                    .FirstOrDefaultAsync(p => p.Id == _proceso.Id);

                if (_proceso == null)
                {
                    throw new InvalidOperationException("Proceso no encontrado en la base de datos");
                }

                // Actualizar información básica
                TxtTituloFabricacion.Text = $"🏭 Fabricar: {_proceso.NombreProducto}";
                TxtSubtituloFabricacion.Text = $"Crear lote de {_proceso.CategoriaProducto} • {_proceso.TiempoFabricacionMinutos} min";
                TxtNombreProceso.Text = _proceso.NombreProducto;
                TxtCategoriaProceso.Text = $"{_proceso.CategoriaProducto} • {_proceso.TiempoFabricacionMinutos} min estimado";
                TxtRendimientoProceso.Text = $"{_proceso.RendimientoEsperado:F2} {_proceso.UnidadMedidaProducto}";
                TxtUnidadFabricar.Text = _proceso.UnidadMedidaProducto;

                // Cargar ingredientes necesarios
                _ingredientesNecesarios.Clear();
                if (_proceso.Ingredientes?.Any() == true)
                {
                    foreach (var ingrediente in _proceso.Ingredientes.OrderBy(i => i.OrdenAdicion))
                    {
                        var ingredienteExtendido = new IngredienteLoteExtendido
                        {
                            RecetaDetalle = ingrediente,
                            CantidadBase = ingrediente.CantidadRequerida,
                            CantidadNecesaria = ingrediente.CantidadRequerida // Se actualizará con el factor
                        };
                        _ingredientesNecesarios.Add(ingredienteExtendido);
                    }
                }

                DgIngredientesLote.ItemsSource = _ingredientesNecesarios;

                System.Diagnostics.Debug.WriteLine($"✅ Proceso cargado: {_proceso.NombreProducto} con {_ingredientesNecesarios.Count} ingredientes");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar datos del proceso: {ex.Message}");
            }
        }

        /// <summary>
        /// Calcula la cantidad máxima que se puede fabricar
        /// </summary>
        private async System.Threading.Tasks.Task CalcularCantidadMaxima()
        {
            try
            {
                // ✅ USAR CONTEXTO FRESCO para evitar cache obsoleto
                using var contextoFresco = new AppDbContext();

                // ✅ RECARGAR PROCESO CON DATOS FRESCOS
                var procesoFresco = await contextoFresco.ProcesosFabricacion
                    .Include(p => p.Ingredientes)
                        .ThenInclude(i => i.RawMaterial)
                    .FirstOrDefaultAsync(p => p.Id == _proceso.Id);

                if (procesoFresco?.Ingredientes?.Any() != true)
                {
                    _cantidadMaximaFabricable = 0;
                    TxtCantidadMaxima.Text = "❌ Sin ingredientes configurados";
                    return;
                }

                // ✅ CALCULAR MÁXIMO CON STOCK ACTUAL
                var cantidadesMaximas = new List<decimal>();

                foreach (var ingrediente in procesoFresco.Ingredientes)
                {
                    // ✅ RECARGAR MATERIAL CON STOCK FRESCO
                    var materialFresco = await contextoFresco.RawMaterials
                        .FirstOrDefaultAsync(m => m.Id == ingrediente.RawMaterialId);

                    if (materialFresco != null && ingrediente.CantidadRequerida > 0)
                    {
                        // ✅ CALCULAR CUÁNTAS VECES SE PUEDE USAR ESTE INGREDIENTE
                        decimal factorMaximo = materialFresco.StockTotal / ingrediente.CantidadRequerida;
                        decimal cantidadMaxima = factorMaximo * procesoFresco.RendimientoEsperado;
                        cantidadesMaximas.Add(cantidadMaxima);

                        System.Diagnostics.Debug.WriteLine($"🔍 INGREDIENTE: {materialFresco.NombreArticulo}");
                        System.Diagnostics.Debug.WriteLine($"   📦 Stock disponible: {materialFresco.StockTotal:F2}");
                        System.Diagnostics.Debug.WriteLine($"   📝 Cantidad requerida: {ingrediente.CantidadRequerida:F2}");
                        System.Diagnostics.Debug.WriteLine($"   🧮 Máximo fabricable: {cantidadMaxima:F2}");
                    }
                }

                // ✅ EL MÁXIMO ES EL MENOR DE TODOS LOS INGREDIENTES
                _cantidadMaximaFabricable = cantidadesMaximas.Any() ? cantidadesMaximas.Min() : 0;

                System.Diagnostics.Debug.WriteLine($"🎯 CANTIDAD MÁXIMA FINAL: {_cantidadMaximaFabricable:F2} {procesoFresco.UnidadMedidaProducto}");

                // ✅ ACTUALIZAR INTERFAZ
                TxtCantidadMaxima.Text = _cantidadMaximaFabricable > 0
                    ? $"Máximo fabricable: {_cantidadMaximaFabricable:F2} {procesoFresco.UnidadMedidaProducto}"
                    : "❌ Sin stock suficiente para fabricar";

                // ✅ ACTUALIZAR ESTILOS
                if (_cantidadMaximaFabricable <= 0)
                {
                    BorderCantidadMaxima.Style = (Style)FindResource("AlertaError");
                }
                else if (_cantidadMaximaFabricable < procesoFresco.RendimientoEsperado)
                {
                    BorderCantidadMaxima.Style = (Style)FindResource("AlertaAdvertencia");
                }
                else
                {
                    BorderCantidadMaxima.Style = (Style)FindResource("AlertaExito");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al calcular cantidad máxima: {ex.Message}");
                _cantidadMaximaFabricable = 0;
                TxtCantidadMaxima.Text = "❌ Error al calcular máximo";
            }
        }
        /// <summary>
        /// Configura valores iniciales del formulario
        /// </summary>
        private void ConfigurarValoresIniciales()
        {
            // Configurar cantidad inicial
            decimal cantidadInicial = Math.Min(_proceso.RendimientoEsperado, _cantidadMaximaFabricable);
            if (cantidadInicial > 0)
            {
                TxtCantidadFabricar.Text = cantidadInicial.ToString("F2");
            }

            // Configurar operador por defecto
            TxtOperadorResponsable.Text = Environment.UserName;

            // Configurar costos reales basados en estimados
            TxtCostoRealManoObra.Text = _proceso.CostoManoObra.ToString("F2");
            TxtCostoRealAdicionales.Text = _proceso.CostosAdicionalesTotal.ToString("F2");

            // Configurar información del producto resultante
            TxtProductoResultante.Text = $"{_proceso.NombreProducto} - Lote Fabricado";
        }

        #endregion

        #region Eventos de Cambios

        /// <summary>
        /// Evento cuando cambia la cantidad a fabricar
        /// </summary>
        private void TxtCantidadFabricar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_controlesInicializados) return;

            try
            {
                ActualizarCalculos();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cambiar cantidad: {ex.Message}");
            }
        }

        /// <summary>
        /// Evento cuando cambia cualquier campo
        /// </summary>
        private void OnCampoChanged(object sender, EventArgs e)
        {
            if (!_controlesInicializados) return;

            try
            {
                ActualizarCalculos();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnCampoChanged: {ex.Message}");
            }
        }

        #endregion

        #region Cálculos y Validaciones

        /// <summary>
        /// Actualiza todos los cálculos de la fabricación
        /// </summary>
        private void ActualizarCalculos()
        {
            try
            {
                if (!decimal.TryParse(TxtCantidadFabricar?.Text ?? "0", out decimal cantidadFabricar) || cantidadFabricar <= 0)
                {
                    cantidadFabricar = 0;
                }

                // Calcular factor de escalado
                decimal factor = _proceso.RendimientoEsperado > 0 ? cantidadFabricar / _proceso.RendimientoEsperado : 0;

                // Actualizar cantidades necesarias de ingredientes
                ActualizarIngredientesNecesarios(factor);

                // Calcular costos estimados
                CalcularCostosEstimados(factor);

                // Calcular costos reales
                CalcularCostosReales(factor);

                // Actualizar resumen
                ActualizarResumen(cantidadFabricar);

                // Validar fabricación
                ValidarFabricacion(cantidadFabricar);

                // Actualizar interfaz
                ActualizarInterfaz();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarCalculos: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza las cantidades necesarias de ingredientes
        /// </summary>
        private void ActualizarIngredientesNecesarios(decimal factor)
        {
            foreach (var ingrediente in _ingredientesNecesarios)
            {
                ingrediente.CantidadNecesaria = ingrediente.CantidadBase * factor;
            }

            // Refrescar DataGrid
            DgIngredientesLote.Items.Refresh();
        }

        /// <summary>
        /// Calcula los costos estimados
        /// </summary>
        private void CalcularCostosEstimados(decimal factor)
        {
            var costoMateriales = _ingredientesNecesarios.Sum(i => i.CostoTotal);
            var costoManoObra = _proceso.CostoManoObra * factor;
            var costosAdicionales = _proceso.CostosAdicionalesTotal * factor;
            var costoTotal = costoMateriales + costoManoObra + costosAdicionales;

            TxtCostoEstimadoMateriales.Text = costoMateriales.ToString("C2");
            TxtCostoEstimadoManoObra.Text = costoManoObra.ToString("C2");
            TxtCostoEstimadoAdicionales.Text = costosAdicionales.ToString("C2");
            TxtCostoEstimadoTotal.Text = costoTotal.ToString("C2");
        }

        /// <summary>
        /// Calcula los costos reales
        /// </summary>
        private void CalcularCostosReales(decimal factor)
        {
            decimal.TryParse(TxtCostoRealManoObra?.Text ?? "0", out decimal costoRealManoObra);
            decimal.TryParse(TxtCostoRealAdicionales?.Text ?? "0", out decimal costoRealAdicionales);

            var costoMateriales = _ingredientesNecesarios.Sum(i => i.CostoTotal);
            var costoTotal = costoMateriales + costoRealManoObra + costoRealAdicionales;

            TxtCostoRealTotal.Text = costoTotal.ToString("C2");
        }

        /// <summary>
        /// Actualiza el resumen del lote
        /// </summary>
        private void ActualizarResumen(decimal cantidadFabricar)
        {
            // Calcular costo total real
            decimal.TryParse(TxtCostoRealTotal?.Text?.Replace("$", "").Replace(",", "") ?? "0", out decimal costoTotalReal);

            // Calcular merma
            var cantidadConMerma = cantidadFabricar * (1 - _proceso.PorcentajeMerma / 100);

            // Calcular costo unitario
            var costoUnitario = cantidadConMerma > 0 ? costoTotalReal / cantidadConMerma : 0;

            // Calcular precio sugerido
            var precioSugerido = costoUnitario * (1 + _proceso.MargenObjetivo / 100);

            // Actualizar controles
            TxtResumenCantidad.Text = $"{cantidadFabricar:F2} {_proceso.UnidadMedidaProducto}";
            TxtResumenCostoLote.Text = costoTotalReal.ToString("C2");
            TxtResumenCostoUnitario.Text = costoUnitario.ToString("C4");
            TxtResumenPrecioSugerido.Text = precioSugerido.ToString("C2");
            TxtCantidadResultante.Text = $"{cantidadConMerma:F2} {_proceso.UnidadMedidaProducto}";
        }

        /// <summary>
        /// Valida si se puede realizar la fabricación
        /// </summary>
        private void ValidarFabricacion(decimal cantidadFabricar)
        {
            try
            {
                var problemasStock = new List<string>();
                bool puedeRealizar = true;

                // Validar cantidad
                if (cantidadFabricar <= 0)
                {
                    TxtResumenEstado.Text = "❌ Cantidad inválida";
                    TxtResumenEstado.Foreground = System.Windows.Media.Brushes.Red;
                    TxtResumenListoFabricar.Text = "❌ NO";
                    TxtResumenListoFabricar.Foreground = System.Windows.Media.Brushes.Red;
                    puedeRealizar = false;
                }
                else if (cantidadFabricar > _cantidadMaximaFabricable)
                {
                    TxtResumenEstado.Text = "⚠️ Excede cantidad máxima";
                    TxtResumenEstado.Foreground = System.Windows.Media.Brushes.Orange;
                    TxtResumenListoFabricar.Text = "⚠️ PARCIAL";
                    TxtResumenListoFabricar.Foreground = System.Windows.Media.Brushes.Orange;
                    puedeRealizar = false;
                }

                // Validar stock de ingredientes
                foreach (var ingrediente in _ingredientesNecesarios)
                {
                    if (!ingrediente.PuedeFabricarse)
                    {
                        problemasStock.Add($"• {ingrediente.NombreIngrediente}: Faltan {ingrediente.CantidadFaltante:F2} {ingrediente.UnidadMedida}");
                        puedeRealizar = false;
                    }
                }

                // Actualizar estado de stock
                if (problemasStock.Any())
                {
                    BorderAlertaStock.Visibility = Visibility.Visible;
                    TxtAlertaStock.Text = $"Los siguientes ingredientes no tienen stock suficiente:\n" +
                                         string.Join("\n", problemasStock);

                    BorderEstadoIngredientes.Background = System.Windows.Media.Brushes.LightCoral;
                    TxtEstadoIngredientes.Text = "❌ Stock insuficiente";
                    TxtEstadoIngredientes.Foreground = System.Windows.Media.Brushes.DarkRed;
                }
                else
                {
                    BorderAlertaStock.Visibility = Visibility.Collapsed;
                    BorderEstadoIngredientes.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 250, 229));
                    TxtEstadoIngredientes.Text = "✅ Todos disponibles";
                    TxtEstadoIngredientes.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(6, 95, 70));
                }

                // Estado final
                if (puedeRealizar && cantidadFabricar > 0)
                {
                    TxtResumenEstado.Text = "✅ Listo para fabricar";
                    TxtResumenEstado.Foreground = System.Windows.Media.Brushes.Green;
                    TxtResumenListoFabricar.Text = "✅ SÍ";
                    TxtResumenListoFabricar.Foreground = System.Windows.Media.Brushes.Green;
                }

                // Habilitar/deshabilitar botón de fabricar
                if (BtnIniciarFabricacion != null)
                {
                    BtnIniciarFabricacion.IsEnabled = puedeRealizar && cantidadFabricar > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ValidarFabricacion: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza la interfaz general
        /// </summary>
        private void ActualizarInterfaz()
        {
            // Actualizar título de ventana
            decimal.TryParse(TxtCantidadFabricar?.Text ?? "0", out decimal cantidad);
            this.Title = $"🏭 Fabricar: {_proceso.NombreProducto} - {cantidad:F2} {_proceso.UnidadMedidaProducto}";
        }

        #endregion

        #region Eventos de Botones

        /// <summary>
        /// Genera un nuevo número de lote
        /// </summary>
        private void BtnGenerarLote_Click(object sender, RoutedEventArgs e)
        {
            GenerarNumeroLote();
        }

        /// <summary>
        /// Establece la cantidad máxima fabricable
        /// </summary>
        private void BtnMaximoCantidad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cantidadMaximaFabricable > 0)
                {
                    TxtCantidadFabricar.Text = _cantidadMaximaFabricable.ToString("F2");
                    TxtEstadoVentana.Text = $"✅ Cantidad establecida al máximo: {_cantidadMaximaFabricable:F2}";
                }
                else
                {
                    MessageBox.Show("No hay stock suficiente para fabricar ninguna cantidad.",
                                  "Sin Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al establecer cantidad máxima:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Calcula los costos actualizados
        /// </summary>
        private void BtnCalcularCostos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ActualizarCalculos();
                TxtEstadoVentana.Text = "💡 Costos recalculados con datos actuales";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular costos:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Verifica el stock disponible
        /// </summary>
        private async void BtnVerificarStock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnVerificarStock.IsEnabled = false;
                BtnVerificarStock.Content = "⏳ Verificando...";
                TxtEstadoVentana.Text = "🔍 Verificando stock actual...";

                // ✅ RECARGAR TODO CON DATOS FRESCOS
                await CargarDatosProceso();  // Recargar proceso e ingredientes
                await CalcularCantidadMaxima(); // Recalcular con datos frescos

                // ✅ FORZAR ACTUALIZACIÓN DE CÁLCULOS
                _controlesInicializados = false; // Temporalmente
                ActualizarCalculos();
                _controlesInicializados = true;

                TxtEstadoVentana.Text = "✅ Stock verificado y actualizado";
            }
            catch (Exception ex)
            {
                TxtEstadoVentana.Text = "❌ Error al verificar stock";
                MessageBox.Show($"Error al verificar stock:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (BtnVerificarStock != null)
                {
                    BtnVerificarStock.IsEnabled = true;
                    BtnVerificarStock.Content = "🔄 Verificar Stock";
                }
            }
        }

        /// <summary>
        /// Muestra una previsualización del lote
        /// </summary>
        private void BtnPrevisualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal.TryParse(TxtCantidadFabricar?.Text ?? "0", out decimal cantidad);
                decimal.TryParse(TxtCostoRealTotal?.Text?.Replace("$", "").Replace(",", "") ?? "0", out decimal costoTotal);
                var cantidadConMerma = cantidad * (1 - _proceso.PorcentajeMerma / 100);
                var costoUnitario = cantidadConMerma > 0 ? costoTotal / cantidadConMerma : 0;

                var preview = $"👁️ PREVISUALIZACIÓN DEL LOTE\n\n" +
                             $"🏭 PROCESO:\n" +
                             $"   • Nombre: {_proceso.NombreProducto}\n" +
                             $"   • Número de lote: {TxtNumeroLote.Text}\n" +
                             $"   • Operador: {TxtOperadorResponsable.Text}\n\n" +

                             $"📊 CANTIDADES:\n" +
                             $"   • Cantidad a fabricar: {cantidad:F2} {_proceso.UnidadMedidaProducto}\n" +
                             $"   • Merma esperada: {_proceso.PorcentajeMerma:F1}%\n" +
                             $"   • Cantidad final estimada: {cantidadConMerma:F2} {_proceso.UnidadMedidaProducto}\n\n" +

                             $"💰 COSTOS:\n" +
                             $"   • Costo total del lote: {costoTotal:C2}\n" +
                             $"   • Costo unitario: {costoUnitario:C4}\n\n" +

                             $"📦 INGREDIENTES NECESARIOS:\n";

                foreach (var ingrediente in _ingredientesNecesarios.OrderBy(i => i.RecetaDetalle.OrdenAdicion))
                {
                    var estado = ingrediente.PuedeFabricarse ? "✅" : "❌";
                    preview += $"   {estado} {ingrediente.NombreIngrediente}: {ingrediente.CantidadNecesaria:F2} {ingrediente.UnidadMedida}\n";
                }

                preview += $"\n📝 NOTAS:\n{TxtNotasLote.Text}";

                MessageBox.Show(preview, "Previsualización del Lote",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en previsualización:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Abre el historial de fabricación
        /// </summary>
        private void BtnHistorial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Abrir HistorialFabricacionWindow filtrado por este proceso
                MessageBox.Show("Función próximamente disponible.\n\nSe abrirá el historial de fabricación filtrado por este proceso.",
                               "Historial", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir historial:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Inicia el proceso de fabricación
        /// </summary>
        private async void BtnIniciarFabricacion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones finales
                if (!ValidarDatosParaFabricacion())
                    return;

                // Confirmación del usuario
                if (!MostrarConfirmacionFabricacion())
                    return;

                // Deshabilitar controles
                DeshabilitarControles(true);
                TxtEstadoVentana.Text = "🏭 Ejecutando fabricación...";

                // Ejecutar fabricación
                var loteCreado = await EjecutarFabricacion();

                if (loteCreado != null)
                {
                    // Mostrar mensaje de éxito
                    MostrarExitoFabricacion(loteCreado);

                    // Cerrar ventana
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Error: No se pudo completar la fabricación.",
                                  "Error de Fabricación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                TxtEstadoVentana.Text = "❌ Error en fabricación";
                MessageBox.Show($"Error durante la fabricación:\n\n{ex.Message}",
                              "Error de Fabricación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DeshabilitarControles(false);
            }
        }

        /// <summary>
        /// Cancela la operación
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro que desea cancelar la fabricación?\n\nSe perderán todos los datos configurados.",
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

        #region Lógica de Fabricación

        private async Task NotificarVentanaFabricacion(string motivo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Buscando ventana de fabricación para actualizar: {motivo}");

                // ✅ BUSCAR VENTANA DE GESTIÓN DE FABRICACIÓN
                // Ajustar el nombre según tu ventana (puede ser GestionFabricacionWindow, FabricacionWindow, etc.)
                var fabricacionWindow = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.GetType().Name.Contains("Fabricacion") && w != this);

                if (fabricacionWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Ventana fabricación encontrada: {fabricacionWindow.GetType().Name}");

                    // ✅ USAR REFLECTION PARA LLAMAR MÉTODOS DE ACTUALIZACIÓN
                    var windowType = fabricacionWindow.GetType();

                    // Buscar métodos de actualización comunes
                    var cargarProcesosMethod = windowType.GetMethod("CargarProcesos",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                    var actualizarEstadisticasMethod = windowType.GetMethod("ActualizarEstadisticas",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                    // ✅ EJECUTAR EN EL HILO DE LA VENTANA
                    await fabricacionWindow.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            // Llamar CargarProcesos si existe
                            if (cargarProcesosMethod != null)
                            {
                                System.Diagnostics.Debug.WriteLine("🔄 Llamando CargarProcesos...");

                                if (cargarProcesosMethod.ReturnType == typeof(Task))
                                {
                                    await (Task)cargarProcesosMethod.Invoke(fabricacionWindow, null);
                                }
                                else
                                {
                                    cargarProcesosMethod.Invoke(fabricacionWindow, null);
                                }
                            }

                            // Llamar ActualizarEstadisticas si existe
                            if (actualizarEstadisticasMethod != null)
                            {
                                System.Diagnostics.Debug.WriteLine("📊 Llamando ActualizarEstadisticas...");

                                if (actualizarEstadisticasMethod.ReturnType == typeof(Task))
                                {
                                    await (Task)actualizarEstadisticasMethod.Invoke(fabricacionWindow, null);
                                }
                                else
                                {
                                    actualizarEstadisticasMethod.Invoke(fabricacionWindow, null);
                                }
                            }

                            // ✅ ACTUALIZAR STATUS EN LA VENTANA
                            var txtEstadoProperty = windowType.GetProperty("TxtEstadoVentana",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                            if (txtEstadoProperty != null)
                            {
                                var txtEstado = txtEstadoProperty.GetValue(fabricacionWindow) as TextBlock;
                                if (txtEstado != null)
                                {
                                    txtEstado.Text = $"✅ Actualizado: {motivo}";
                                }
                            }

                            System.Diagnostics.Debug.WriteLine("🎉 Ventana fabricación actualizada exitosamente");
                        }
                        catch (Exception invokeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error invocando métodos: {invokeEx.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Ventana de fabricación no encontrada");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error notificando ventana fabricación: {ex.Message}");
                // No lanzar excepción, solo registrar el error
            }
        }

        public async Task ActualizarDespuesDeFabricacion(string motivo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 FabricacionProceso actualizándose: {motivo}");

                // ✅ RECARGAR TODO AUTOMÁTICAMENTE
                try
                {
                    await NotificarVentanaFabricacion($"Producto fabricado: {_proceso.NombreProducto}");
                    System.Diagnostics.Debug.WriteLine("🔄 Ventana fabricación notificada");
                }
                catch (Exception fabricNotifEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error notificando fabricación: {fabricNotifEx.Message}");
                }
                // ✅ ACTUALIZAR STATUS
                TxtEstadoVentana.Text = $"✅ Actualizado: {motivo} - {DateTime.Now:HH:mm:ss}";

                System.Diagnostics.Debug.WriteLine("🎉 FabricacionProceso actualizado automáticamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando FabricacionProceso: {ex.Message}");
                TxtEstadoVentana.Text = "❌ Error en actualización automática";
            }
        }

        /// <summary>
        /// Ejecuta el proceso completo de fabricación
        /// </summary>


        private async Task<LoteFabricacion> EjecutarFabricacion()
        {
            // ✅ CREAR CONTEXTO FRESCO (igual que POS)
            using var fabricacionContext = new AppDbContext();
            using var transaction = await fabricacionContext.Database.BeginTransactionAsync();

            try
            {
                System.Diagnostics.Debug.WriteLine("🏭 === INICIANDO FABRICACIÓN COMPLETA ===");

                // ✅ PASO 1: Recargar proceso con datos frescos
                var procesoFresco = await fabricacionContext.ProcesosFabricacion
                    .Include(p => p.Ingredientes)
                        .ThenInclude(i => i.RawMaterial)
                    .FirstOrDefaultAsync(p => p.Id == _proceso.Id);

                if (procesoFresco == null)
                {
                    throw new InvalidOperationException("Proceso de fabricación no encontrado");
                }

                // ✅ PASO 2: Obtener cantidad a fabricar
                decimal.TryParse(TxtCantidadFabricar?.Text ?? "0", out decimal cantidadFabricar);

                if (cantidadFabricar <= 0)
                {
                    throw new InvalidOperationException("La cantidad a fabricar debe ser mayor a 0");
                }

                // ✅ PASO 3: Validar stock actual con datos frescos
                var stockInsuficiente = new List<string>();
                foreach (var ingrediente in procesoFresco.Ingredientes)
                {
                    var materialFresco = await fabricacionContext.RawMaterials.FindAsync(ingrediente.RawMaterialId);
                    if (materialFresco == null)
                    {
                        throw new InvalidOperationException($"Material {ingrediente.NombreIngrediente} no encontrado");
                    }

                    decimal cantidadNecesaria = ingrediente.CantidadRequerida * cantidadFabricar / procesoFresco.RendimientoEsperado;

                    if (materialFresco.StockTotal < cantidadNecesaria)
                    {
                        stockInsuficiente.Add($"{materialFresco.NombreArticulo}: Necesario {cantidadNecesaria:F2}, Disponible {materialFresco.StockTotal:F2}");
                    }
                }

                if (stockInsuficiente.Any())
                {
                    throw new InvalidOperationException($"Stock insuficiente:\n{string.Join("\n", stockInsuficiente)}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Stock validado para {cantidadFabricar:F2} {procesoFresco.UnidadMedidaProducto}");

                // ✅ PASO 4: Crear el lote de fabricación (ahora con parámetros correctos)
                var lote = await CrearLoteFabricacion(fabricacionContext, procesoFresco, cantidadFabricar);
                System.Diagnostics.Debug.WriteLine($"✅ Lote creado: {lote.NumeroLote}");

                // ✅ PASO 5: Descontar materias primas (usar método corregido)
                await DescontarMateriasPrivas(fabricacionContext, lote, procesoFresco, cantidadFabricar);
                System.Diagnostics.Debug.WriteLine("✅ Materias primas descontadas");

                // ✅ PASO 6: Crear producto terminado (usar método corregido)
                var productoTerminado = await CrearProductoResultante(fabricacionContext, lote, procesoFresco, cantidadFabricar);
                System.Diagnostics.Debug.WriteLine($"✅ Producto terminado creado: {productoTerminado.NombreArticulo}");

                // ✅ PASO 7: Actualizar lote con producto creado
                lote.ProductoResultanteId = productoTerminado.Id;
                var cantidadConMerma = cantidadFabricar * (1 - procesoFresco.PorcentajeMerma / 100);
                lote.CompletarProceso(cantidadConMerma,
                    $"Fabricación completada. Producto: {productoTerminado.NombreArticulo}");

                // ✅ PASO 8: Guardar todos los cambios
                await fabricacionContext.SaveChangesAsync();
                await transaction.CommitAsync();

                System.Diagnostics.Debug.WriteLine("🎉 FABRICACIÓN COMPLETADA EXITOSAMENTE");

                // ✅ ⭐ PASO 9: NOTIFICAR AL MAINWINDOW PARA ACTUALIZACIÓN AUTOMÁTICA ⭐
                try
                {
                    await NotificarActualizacionMainWindow($"Producto fabricado: {productoTerminado.NombreArticulo}");

                    System.Diagnostics.Debug.WriteLine("🔄 MainWindow notificado para actualización automática");
                }
                catch (Exception notifEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error al notificar MainWindow: {notifEx.Message}");
                    // No lanzar excepción por esto, la fabricación fue exitosa
                }
                try
                {
                    await NotificarVentanaFabricacion($"Producto fabricado: {productoTerminado.NombreArticulo}");
                    System.Diagnostics.Debug.WriteLine("🔄 Ventana fabricación notificada");
                }
                catch (Exception fabricNotifEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error notificando fabricación: {fabricNotifEx.Message}");
                }

                return lote;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR EN FABRICACIÓN: {ex.Message}");

                try
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine("🔄 Transacción revertida");
                }
                catch (Exception rollbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 ERROR EN ROLLBACK: {rollbackEx.Message}");
                }

                throw new Exception($"Error durante la fabricación: {ex.Message}");
            }
        }


        /// <summary>
        /// Crea el registro del lote de fabricación
        /// </summary>
        private async Task<LoteFabricacion> CrearLoteFabricacion(AppDbContext context, ProcesoFabricacion proceso, decimal cantidad)
        {
            decimal.TryParse(TxtCostoRealManoObra?.Text ?? "0", out decimal costoManoObra);
            decimal.TryParse(TxtCostoRealAdicionales?.Text ?? "0", out decimal costosAdicionales);

            // Calcular costo de materiales
            decimal costoMateriales = 0;
            foreach (var ingrediente in _ingredientesNecesarios)
            {
                costoMateriales += ingrediente.CostoTotal;
            }

            var lote = new LoteFabricacion
            {
                ProcesoFabricacionId = proceso.Id,
                NumeroLote = TxtNumeroLote.Text,
                CantidadPlanificada = cantidad,
                CantidadObtenida = 0, // Se actualizará al completar
                FechaInicio = DateTime.Now,
                Estado = EstadoLote.EnProceso.ToString(), // ✅ CAMBIO: Usar enum
                CostoMaterialesReal = costoMateriales,
                CostoManoObraReal = costoManoObra,
                CostosAdicionalesReal = costosAdicionales,
                OperadorResponsable = TxtOperadorResponsable.Text ?? Environment.UserName,
                NotasProduccion = TxtNotasLote.Text ?? ""
            };

            context.LotesFabricacion.Add(lote); // ✅ CAMBIO: Usar parámetro context
            await context.SaveChangesAsync(); // ✅ CAMBIO: Usar parámetro context

            return lote;
        }
        /// <summary>
        /// Descuenta las materias primas del inventario
        /// </summary>
        private async Task DescontarMateriasPrivas(AppDbContext context, LoteFabricacion lote, ProcesoFabricacion proceso, decimal cantidadFabricar)
        {
            decimal factor = proceso.RendimientoEsperado > 0 ? cantidadFabricar / proceso.RendimientoEsperado : 1;

            foreach (var ingrediente in proceso.Ingredientes)
            {
                var material = await context.RawMaterials.FindAsync(ingrediente.RawMaterialId);
                if (material == null)
                {
                    throw new InvalidOperationException($"Material {ingrediente.NombreIngrediente} no encontrado");
                }

                decimal cantidadNecesaria = ingrediente.CantidadRequerida * factor;

                // ✅ DESCONTAR STOCK (igual que POS)
                if (!material.ReducirStock(cantidadNecesaria))
                {
                    throw new InvalidOperationException($"No se pudo descontar stock de {material.NombreArticulo}");
                }

                // ✅ CREAR MOVIMIENTO DE SALIDA (igual que POS)
                var movimiento = new Movimiento
                {
                    RawMaterialId = material.Id,
                    TipoMovimiento = "Salida por Fabricación",
                    Cantidad = cantidadNecesaria,
                    PrecioConIVA = material.PrecioConIVA,
                    PrecioSinIVA = material.PrecioSinIVA,
                    UnidadMedida = material.UnidadMedida,
                    Motivo = $"Fabricación lote {lote.NumeroLote} - {proceso.NombreProducto}",
                    Usuario = lote.OperadorResponsable,
                    FechaMovimiento = DateTime.Now,
                    NumeroDocumento = lote.NumeroLote,
                    StockAnterior = material.StockTotal + cantidadNecesaria,
                    StockPosterior = material.StockTotal
                };

                context.Movimientos.Add(movimiento);

                System.Diagnostics.Debug.WriteLine($"📦 Descontado: {material.NombreArticulo} -{cantidadNecesaria:F2} {material.UnidadMedida}");
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Crea el producto resultante en el inventario
        /// </summary>
        private async Task<RawMaterial> CrearProductoResultante(AppDbContext context, LoteFabricacion lote, ProcesoFabricacion proceso, decimal cantidadFabricar)
        {
            try
            {
                // ✅ CALCULAR CANTIDADES Y COSTOS
                decimal cantidadFinal = cantidadFabricar * (1 - proceso.PorcentajeMerma / 100);
                decimal costoUnitario = cantidadFinal > 0 ? lote.CostoTotalReal / cantidadFinal : 0;

                System.Diagnostics.Debug.WriteLine($"🏭 CREANDO PRODUCTO TERMINADO UNIFICADO");
                System.Diagnostics.Debug.WriteLine($"   📦 Proceso: {proceso.NombreProducto}");
                System.Diagnostics.Debug.WriteLine($"   📊 Cantidad final: {cantidadFinal:F2} {proceso.UnidadMedidaProducto}");
                System.Diagnostics.Debug.WriteLine($"   💰 Costo unitario: ${costoUnitario:F4}");

                // ✅ PASO 1: BUSCAR PRODUCTO EXISTENTE PARA ESTE PROCESO
                // Usar nombre base sin número de lote para unificar productos
                string nombreProductoBase = $"{proceso.NombreProducto}";
                string categoriaBase = $"Fabricados - {proceso.CategoriaProducto}";

                var productoExistente = await context.RawMaterials
                    .FirstOrDefaultAsync(p =>
                        p.NombreArticulo == nombreProductoBase &&
                        p.Categoria == categoriaBase &&
                        p.Proveedor == "Fabricación Propia" &&
                        !p.Eliminado);

                RawMaterial productoFinal;

                if (productoExistente != null)
                {
                    // ✅ CASO 1: PRODUCTO YA EXISTE - AGREGAR STOCK
                    System.Diagnostics.Debug.WriteLine($"✅ PRODUCTO EXISTENTE ENCONTRADO: {productoExistente.NombreArticulo}");
                    System.Diagnostics.Debug.WriteLine($"   📦 Stock actual: {productoExistente.StockTotal:F2}");
                    System.Diagnostics.Debug.WriteLine($"   📦 Agregando: {cantidadFinal:F2}");
                    System.Diagnostics.Debug.WriteLine($"   📦 Stock nuevo: {productoExistente.StockTotal + cantidadFinal:F2}");

                    // ✅ ACTUALIZAR STOCK EXISTENTE
                    decimal stockAnteriorCompleto = productoExistente.StockTotal;
                    productoExistente.StockAntiguo = stockAnteriorCompleto;  // Todo lo anterior
                    productoExistente.StockNuevo = cantidadFinal;

                    // ✅ ACTUALIZAR COSTOS SI EL NUEVO ES MEJOR
                    if (costoUnitario > 0 && costoUnitario != productoExistente.PrecioConIVA)
                    {
                        System.Diagnostics.Debug.WriteLine($"   💰 Actualizando costos:");
                        System.Diagnostics.Debug.WriteLine($"      • Costo anterior: ${productoExistente.PrecioConIVA:F4}");
                        System.Diagnostics.Debug.WriteLine($"      • Costo nuevo: ${costoUnitario:F4}");

                        // Usar promedio ponderado de costos
                        decimal stockAnterior = productoExistente.StockAntiguo;
                        decimal costoAnterior = productoExistente.PrecioConIVA;
                        decimal stockTotal = stockAnterior + cantidadFinal;

                        decimal costoPromedio = stockTotal > 0
                            ? ((stockAnterior * costoAnterior) + (cantidadFinal * costoUnitario)) / stockTotal
                            : costoUnitario;

                        productoExistente.PrecioPorUnidad = costoPromedio;
                        productoExistente.PrecioConIVA = costoPromedio;
                        productoExistente.PrecioSinIVA = costoPromedio / 1.16m;
                        productoExistente.PrecioBaseConIVA = costoPromedio;
                        productoExistente.PrecioBaseSinIVA = costoPromedio / 1.16m;

                        System.Diagnostics.Debug.WriteLine($"      • Costo promedio final: ${costoPromedio:F4}");
                    }

                    // ✅ ACTUALIZAR INFORMACIÓN DE FABRICACIÓN
                    productoExistente.FechaActualizacion = DateTime.Now;

                    // ✅ AGREGAR INFORMACIÓN DEL LOTE A OBSERVACIONES
                    var infoLote = $"\n[{DateTime.Now:dd/MM/yyyy HH:mm}] Lote {lote.NumeroLote}: +{cantidadFinal:F2} {proceso.UnidadMedidaProducto} " +
                                  $"(Costo: ${costoUnitario:F4}) - {lote.OperadorResponsable}";

                    if (productoExistente.Observaciones == null || productoExistente.Observaciones.Length > 2000)
                    {
                        // Si las observaciones son muy largas, mantener solo las últimas
                        productoExistente.Observaciones = $"Producto fabricado - Histórico truncado{infoLote}";
                    }
                    else
                    {
                        productoExistente.Observaciones += infoLote;
                    }

                    productoFinal = productoExistente;

                    System.Diagnostics.Debug.WriteLine($"✅ STOCK AGREGADO AL PRODUCTO EXISTENTE");
                }
                else
                {
                    // ✅ CASO 2: PRODUCTO NUEVO - SOLICITAR DATOS Y CREAR
                    System.Diagnostics.Debug.WriteLine($"🆕 PRODUCTO NUEVO - Solicitando datos al usuario");

                    var datosProducto = await SolicitarDatosProductoTerminado(proceso, lote.NumeroLote, cantidadFinal, costoUnitario);

                    if (datosProducto == null)
                    {
                        throw new OperationCanceledException("Creación de producto cancelada por el usuario");
                    }

                    // ✅ CREAR PRODUCTO NUEVO
                    productoFinal = new RawMaterial
                    {
                        NombreArticulo = nombreProductoBase, // ✅ SIN número de lote para unificar
                        Categoria = categoriaBase,
                        UnidadMedida = proceso.UnidadMedidaProducto,

                        // ✅ STOCK INICIAL
                        StockAntiguo = 0,
                        StockNuevo = cantidadFinal,

                        // ✅ PRECIOS Y COSTOS
                        PrecioPorUnidad = costoUnitario,
                        PrecioConIVA = costoUnitario,
                        PrecioSinIVA = costoUnitario / 1.16m,
                        PrecioBaseConIVA = costoUnitario,
                        PrecioBaseSinIVA = costoUnitario / 1.16m,

                        // ✅ DATOS DEL PROVEEDOR Y ORIGEN
                        Proveedor = "Fabricación Propia",
                        CodigoBarras = datosProducto.CodigoBarras,

                        // ✅ OBSERVACIONES DETALLADAS
                        Observaciones = $"Producto fabricado el {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
                                       $"Primer lote: {lote.NumeroLote}\n" +
                                       $"Operador: {lote.OperadorResponsable}\n" +
                                       $"Proceso origen: {proceso.NombreProducto}\n" +
                                       $"Cantidad inicial: {cantidadFinal:F2} {proceso.UnidadMedidaProducto}\n" +
                                       $"Costo unitario: ${costoUnitario:F4}",

                        // ✅ CONFIGURACIÓN PARA VENTA
                        ActivoParaVenta = datosProducto.ActivoParaVenta,
                        PrecioVenta = datosProducto.PrecioVenta,
                        PrecioVentaConIVA = datosProducto.PrecioVenta * 1.16m,
                        MargenObjetivo = datosProducto.MargenObjetivo,
                        StockMinimoVenta = datosProducto.StockMinimoVenta,

                        // ✅ ALERTAS
                        AlertaStockBajo = Math.Max(1, cantidadFinal * 0.1m),

                        // ✅ FECHAS
                        FechaCreacion = DateTime.Now,
                        FechaActualizacion = DateTime.Now,
                        FechaVencimiento = datosProducto.FechaVencimiento
                    };

                    // ✅ AGREGAR A LA BASE DE DATOS
                    context.RawMaterials.Add(productoFinal);
                    System.Diagnostics.Debug.WriteLine($"🆕 PRODUCTO NUEVO CREADO: {productoFinal.NombreArticulo}");
                }

                // ✅ GUARDAR CAMBIOS
                await context.SaveChangesAsync();

                // ✅ CREAR MOVIMIENTO DE ENTRADA (SIEMPRE)
                var movimientoEntrada = new Movimiento
                {
                    RawMaterialId = productoFinal.Id,
                    TipoMovimiento = productoExistente != null ? "Entrada por Fabricación (Stock)" : "Entrada por Fabricación (Nuevo)",
                    Cantidad = cantidadFinal,
                    PrecioConIVA = costoUnitario,
                    PrecioSinIVA = costoUnitario / 1.16m,
                    UnidadMedida = productoFinal.UnidadMedida,
                    Motivo = $"Lote {lote.NumeroLote} - {proceso.NombreProducto} ({lote.OperadorResponsable})",
                    Usuario = lote.OperadorResponsable,
                    FechaMovimiento = DateTime.Now,
                    NumeroDocumento = lote.NumeroLote,
                    Proveedor = "Fabricación Propia",
                    StockAnterior = productoFinal.StockAntiguo,
                    StockPosterior = productoFinal.StockNuevo
                };

                context.Movimientos.Add(movimientoEntrada);
                await context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"✅ PRODUCTO TERMINADO PROCESADO:");
                System.Diagnostics.Debug.WriteLine($"   📦 Producto: {productoFinal.NombreArticulo} (ID: {productoFinal.Id})");
                System.Diagnostics.Debug.WriteLine($"   📊 Stock final: {productoFinal.StockNuevo:F2} {productoFinal.UnidadMedida}");
                System.Diagnostics.Debug.WriteLine($"   💰 Costo final: ${productoFinal.PrecioConIVA:F4}");
                System.Diagnostics.Debug.WriteLine($"   🔄 Movimiento: #{movimientoEntrada.Id}");

                return productoFinal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en CrearProductoResultante: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Actualiza el lote con el producto creado y lo completa
        /// </summary>
        private async System.Threading.Tasks.Task ActualizarLoteConProducto(LoteFabricacion lote)
        {
            var productoFinal = await _context.RawMaterials
                .Where(m => m.NombreArticulo.Contains($"Lote {lote.NumeroLote}"))
                .OrderByDescending(m => m.FechaCreacion)
                .FirstOrDefaultAsync();

            if (productoFinal != null)
            {
                lote.ProductoResultanteId = productoFinal.Id;
                lote.CantidadObtenida = productoFinal.StockTotal;
                lote.CompletarProceso(lote.CantidadObtenida,
                    $"Fabricación completada exitosamente. Producto creado: {productoFinal.NombreArticulo}");

                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Validaciones y Confirmaciones

        /// <summary>
        /// Valida que todos los datos estén correctos para fabricar
        /// </summary>
        private bool ValidarDatosParaFabricacion()
        {
            // Validar cantidad
            if (!decimal.TryParse(TxtCantidadFabricar?.Text ?? "0", out decimal cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Debe especificar una cantidad válida a fabricar.",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidadFabricar?.Focus();
                return false;
            }

            // Validar cantidad máxima
            if (cantidad > _cantidadMaximaFabricable)
            {
                MessageBox.Show($"La cantidad excede el máximo fabricable: {_cantidadMaximaFabricable:F2} {_proceso.UnidadMedidaProducto}",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCantidadFabricar?.Focus();
                return false;
            }

            // Validar stock de ingredientes
            if (_ingredientesNecesarios.Any(i => !i.PuedeFabricarse))
            {
                MessageBox.Show("Hay ingredientes con stock insuficiente. Verifique la lista de ingredientes.",
                              "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validar operador
            if (string.IsNullOrWhiteSpace(TxtOperadorResponsable?.Text))
            {
                MessageBox.Show("Debe especificar el operador responsable.",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtOperadorResponsable?.Focus();
                return false;
            }

            // Validar número de lote
            if (string.IsNullOrWhiteSpace(TxtNumeroLote?.Text))
            {
                MessageBox.Show("Debe especificar un número de lote.",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                GenerarNumeroLote();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Muestra confirmación antes de fabricar
        /// </summary>
        private bool MostrarConfirmacionFabricacion()
        {
            decimal.TryParse(TxtCantidadFabricar?.Text ?? "0", out decimal cantidad);
            decimal.TryParse(TxtCostoRealTotal?.Text?.Replace("$", "").Replace(",", "") ?? "0", out decimal costoTotal);
            var cantidadFinal = cantidad * (1 - _proceso.PorcentajeMerma / 100);

            var mensaje = $"¿Confirmar fabricación del siguiente lote?\n\n" +
                         $"🏭 PROCESO: {_proceso.NombreProducto}\n" +
                         $"📦 LOTE: {TxtNumeroLote.Text}\n" +
                         $"👤 OPERADOR: {TxtOperadorResponsable.Text}\n\n" +
                         $"📊 CANTIDADES:\n" +
                         $"   • A fabricar: {cantidad:F2} {_proceso.UnidadMedidaProducto}\n" +
                         $"   • Cantidad final (con merma): {cantidadFinal:F2} {_proceso.UnidadMedidaProducto}\n\n" +
                         $"💰 COSTO TOTAL: {costoTotal:C2}\n\n" +
                         $"⚠️ IMPORTANTE:\n" +
                         $"• Se descontarán las materias primas del inventario\n" +
                         $"• Se creará el producto final automáticamente\n" +
                         $"• Esta operación NO se puede deshacer";

            return MessageBox.Show(mensaje, "Confirmar Fabricación",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Muestra mensaje de éxito después de fabricar
        /// </summary>
        private void MostrarExitoFabricacion(LoteFabricacion lote)
        {
            var mensaje = $"✅ ¡FABRICACIÓN COMPLETADA EXITOSAMENTE!\n\n" +
                         $"📦 LOTE CREADO:\n" +
                         $"   • Número: {lote.NumeroLote}\n" +
                         $"   • Proceso: {_proceso.NombreProducto}\n" +
                         $"   • Cantidad obtenida: {lote.CantidadObtenida:F2} {_proceso.UnidadMedidaProducto}\n" +
                         $"   • Costo total: {lote.CostoTotalReal:C2}\n" +
                         $"   • Operador: {lote.OperadorResponsable}\n\n" +
                         $"📈 EFICIENCIA DEL LOTE: {lote.EficienciaLote:F1}%\n" +
                         $"⏱️ TIEMPO TRANSCURRIDO: {lote.TiempoTranscurridoTexto}\n\n" +
                         $"🎯 RESULTADOS:\n" +
                         $"   • Materias primas descontadas del inventario\n" +
                         $"   • Producto final agregado al inventario\n" +
                         $"   • Lote registrado en el historial\n" +
                         $"   • Producto disponible para venta en POS";

            MessageBox.Show(mensaje, "Fabricación Exitosa",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Genera un número de lote único
        /// </summary>
        private void GenerarNumeroLote()
        {
            TxtNumeroLote.Text = LoteFabricacion.GenerarNumeroLote();
        }

        /// <summary>
        /// Habilita/deshabilita controles durante la fabricación
        /// </summary>
        private void DeshabilitarControles(bool deshabilitar)
        {
            if (BtnIniciarFabricacion != null)
            {
                BtnIniciarFabricacion.IsEnabled = !deshabilitar;
                BtnIniciarFabricacion.Content = deshabilitar ? "⏳ Fabricando..." : "🏭 Iniciar Fabricación";
            }

            if (TxtCantidadFabricar != null) TxtCantidadFabricar.IsEnabled = !deshabilitar;
            if (TxtOperadorResponsable != null) TxtOperadorResponsable.IsEnabled = !deshabilitar;
            if (TxtCostoRealManoObra != null) TxtCostoRealManoObra.IsEnabled = !deshabilitar;
            if (TxtCostoRealAdicionales != null) TxtCostoRealAdicionales.IsEnabled = !deshabilitar;
            if (TxtNotasLote != null) TxtNotasLote.IsEnabled = !deshabilitar;
            if (BtnMaximoCantidad != null) BtnMaximoCantidad.IsEnabled = !deshabilitar;
            if (BtnGenerarLote != null) BtnGenerarLote.IsEnabled = !deshabilitar;
            if (BtnCalcularCostos != null) BtnCalcularCostos.IsEnabled = !deshabilitar;
            if (BtnVerificarStock != null) BtnVerificarStock.IsEnabled = !deshabilitar;
        }

        #endregion

        #region Limpieza

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _context?.Dispose();
                _context = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar EjecutarFabricacionWindow: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion

        /// <summary>
        /// ✅ AGREGAR ESTOS MÉTODOS A EjecutarFabricacionWindow.xaml.cs
        /// DENTRO de la clase EjecutarFabricacionWindow, ANTES del último }
        /// </summary>

        /// <summary>
        /// ✅ MÉTODO 1 - NotificarActualizacionMainWindow
        /// </summary>
        private async Task NotificarActualizacionMainWindow(string motivo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Buscando MainWindow para actualizar: {motivo}");

                // ✅ BUSCAR MAINWINDOW EN VENTANAS ABIERTAS
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

                if (mainWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ MainWindow encontrado - Notificando actualización");

                    // ✅ LLAMAR AL MÉTODO DE ACTUALIZACIÓN AUTOMÁTICA DEL MAINWINDOW
                    await mainWindow.RefrescarProductosAutomatico($"nuevo producto desde fabricación - {motivo}");

                    System.Diagnostics.Debug.WriteLine("🎉 MainWindow actualizado automáticamente");

                    // ✅ FORZAR ACTUALIZACIÓN ADICIONAL DEL DATAGRID
                    await mainWindow.Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            // Actualizar pestaña de Materia Prima
                            await mainWindow.RefreshData();

                            System.Diagnostics.Debug.WriteLine("✅ Pestaña Materia Prima actualizada");

                            // Si están en POS, también actualizar
                            if (mainWindow.MainTabControl?.SelectedIndex == 1)
                            {
                                await mainWindow.LoadDataPuntoVenta();
                                System.Diagnostics.Debug.WriteLine("✅ POS actualizado");
                            }

                            System.Diagnostics.Debug.WriteLine("🎉 Actualización completa del MainWindow terminada");
                        }
                        catch (Exception updateEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Error en actualización específica: {updateEx.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MainWindow no encontrado para actualización");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error notificando actualización: {ex.Message}");
                // No lanzar excepción, solo registrar el error
            }
        }

        /// <summary>
        /// ✅ MÉTODO 2 - SolicitarDatosProductoTerminado
        /// </summary>
        private async Task<DatosProductoTerminado> SolicitarDatosProductoTerminado(ProcesoFabricacion proceso, string numeroLote, decimal cantidadFinal, decimal costoUnitario)
        {
            // ✅ EJECUTAR EN HILO PRINCIPAL
            return await this.Dispatcher.InvokeAsync(() =>
            {
                var ventanaDatos = new CompletarProductoTerminadoWindow(proceso, numeroLote, cantidadFinal, costoUnitario)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (ventanaDatos.ShowDialog() == true)
                {
                    return ventanaDatos.DatosProducto;
                }

                return null;
            });
        }
    }
    
    /// <summary>
    /// Clase extendida para ingredientes del lote con propiedades calculadas
    /// </summary>
    public class IngredienteLoteExtendido : INotifyPropertyChanged
    {
        public RecetaDetalle RecetaDetalle { get; set; }
        public decimal CantidadBase { get; set; } // Cantidad base de la receta
        private decimal _cantidadNecesaria; // Cantidad necesaria para el lote actual

        public decimal CantidadNecesaria
        {
            get => _cantidadNecesaria;
            set
            {
                _cantidadNecesaria = value;
                OnPropertyChanged(nameof(CantidadNecesaria));
                OnPropertyChanged(nameof(CostoTotal));
                OnPropertyChanged(nameof(PuedeFabricarse));
                OnPropertyChanged(nameof(CantidadFaltante));
            }
        }

        // Propiedades calculadas
        public string NombreIngrediente => RecetaDetalle?.NombreIngrediente ?? "";
        public string UnidadMedida => RecetaDetalle?.UnidadMedida ?? "";
        public decimal StockDisponible => RecetaDetalle?.StockDisponible ?? 0;
        public decimal CostoTotal => CantidadNecesaria * (RecetaDetalle?.CostoUnitario ?? 0);
        public bool PuedeFabricarse => CantidadNecesaria > 0 && StockDisponible >= CantidadNecesaria;
        public decimal CantidadFaltante => Math.Max(0, CantidadNecesaria - StockDisponible);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
 
}