using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi
{
    public partial class FabricacionProceso : Window
    {
        private AppDbContext _context;
        private List<ProcesoFabricacion> _procesosOriginales = new();
        private List<ProcesoFabricacion> _procesosFiltrados = new();

        public FabricacionProceso()
        {
            InitializeComponent();
            _context = new AppDbContext();
            this.Loaded += FabricacionProceso_Loaded;
            this.Unloaded += FabricacionProceso_Unloaded;
        }

        #region INICIALIZACIÓN

        private async void FabricacionProceso_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ ASEGURARSE DE QUE TODOS LOS CONTROLES ESTÉN INICIALIZADOS
                if (TxtEstadoVentana != null)
                    TxtEstadoVentana.Text = "⏳ Cargando procesos de fabricación...";

                // ✅ VERIFICAR CONTROLES CRÍTICOS
                if (DgProcesos == null)
                {
                    MessageBox.Show("Error: La interfaz no se ha inicializado correctamente. Intente cerrar y abrir nuevamente la ventana.",
                                   "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await CargarProcesos();
                await ActualizarEstadisticas();

                if (TxtEstadoVentana != null)
                    TxtEstadoVentana.Text = "✅ Gestión de Fabricación cargada correctamente";
            }
            catch (Exception ex)
            {
                if (TxtEstadoVentana != null)
                    TxtEstadoVentana.Text = "❌ Error al cargar procesos";

                MessageBox.Show($"Error al cargar la ventana de fabricación:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task CargarProcesos()
        {
            try
            {
                // ✅ VERIFICAR QUE DgProcesos ESTÉ INICIALIZADO
                if (DgProcesos == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ DgProcesos es null - esperando inicialización");
                    return;
                }

                _procesosOriginales = await _context.ProcesosFabricacion
                    .Include(p => p.Ingredientes)
                        .ThenInclude(i => i.RawMaterial)
                    .OrderByDescending(p => p.FechaActualizacion)
                    .ToListAsync();

                _procesosFiltrados = new List<ProcesoFabricacion>(_procesosOriginales);

                // ✅ VERIFICAR NUEVAMENTE ANTES DE ASIGNAR
                if (DgProcesos != null)
                {
                    DgProcesos.ItemsSource = _procesosFiltrados;
                    ActualizarInfoFiltros();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar procesos: {ex.Message}");
            }
        }
        private async System.Threading.Tasks.Task ActualizarEstadisticas()
        {
            try
            {
                // Calcular estadísticas básicas
                var totalProcesos = _procesosOriginales.Count;
                var procesosActivos = _procesosOriginales.Count(p => p.Activo);

                // Calcular lotes de hoy
                var hoy = DateTime.Today;
                var lotesHoy = await _context.LotesFabricacion
                    .Where(l => l.FechaInicio >= hoy && l.FechaInicio < hoy.AddDays(1))
                    .CountAsync();

                // Lotes en proceso (usando el enum EstadoLote)
                var lotesEnProceso = await _context.LotesFabricacion
                    .Where(l => l.Estado == EstadoLote.EnProceso.ToString())
                    .CountAsync();

                // Calcular procesos fabricables
                var fabricables = _procesosOriginales.Count(p => p.Activo && p.PuedeFabricarse);

                // Actualizar UI
                TxtTotalProcesos.Text = totalProcesos.ToString();
                TxtProcesosActivos.Text = procesosActivos.ToString();
                TxtLotesHoy.Text = lotesHoy.ToString();
                TxtLotesEnProceso.Text = lotesEnProceso.ToString();
                TxtProcesosFabricables.Text = fabricables.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar estadísticas: {ex.Message}");
                // Valores por defecto si hay error
                TxtTotalProcesos.Text = _procesosOriginales.Count.ToString();
                TxtProcesosActivos.Text = _procesosOriginales.Count(p => p.Activo).ToString();
                TxtLotesHoy.Text = "0";
                TxtLotesEnProceso.Text = "0";
                TxtProcesosFabricables.Text = _procesosOriginales.Count(p => p.Activo && p.PuedeFabricarse).ToString();
            }
        }

        #endregion

        #region EVENTOS DE BOTONES PRINCIPALES

        private void BtnNuevoProceso_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtEstadoVentana.Text = "🆕 Abriendo formulario para nuevo proceso...";


                TxtEstadoVentana.Text = "✅ Gestión de Fabricación";


                var crearWindow = new CrearEditarProcesoWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (crearWindow.ShowDialog() == true)
                {
                    _ = CargarProcesos();
                    _ = ActualizarEstadisticas();
                    TxtEstadoVentana.Text = "✅ Nuevo proceso creado exitosamente";
                }
                else
                {
                    TxtEstadoVentana.Text = "✅ Gestión de Fabricación";
                }
                
            }
            catch (Exception ex)
            {
                TxtEstadoVentana.Text = "❌ Error al abrir formulario";
                MessageBox.Show($"Error al abrir formulario de nuevo proceso:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnActualizar.IsEnabled = false;
                BtnActualizar.Content = "⏳";
                TxtEstadoVentana.Text = "🔄 Actualizando información...";

                await CargarProcesos();
                await ActualizarEstadisticas();

                TxtEstadoVentana.Text = $"✅ Información actualizada - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                TxtEstadoVentana.Text = "❌ Error al actualizar";
                MessageBox.Show($"Error al actualizar:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnActualizar.IsEnabled = true;
                BtnActualizar.Content = "🔄 Actualizar";
            }
        }

        private void BtnVerHistorial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtEstadoVentana.Text = "📊 Abriendo historial de fabricación...";

                // TODO: Crear la ventana HistorialFabricacionWindow
                MessageBox.Show("Función próximamente disponible.\n\nCrearemos la ventana 'HistorialFabricacionWindow' más adelante.",
                               "Historial", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtEstadoVentana.Text = "✅ Gestión de Fabricación";

                /*
                var historialWindow = new HistorialFabricacionWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                historialWindow.ShowDialog();
                TxtEstadoVentana.Text = "✅ Gestión de Fabricación";
                */
            }
            catch (Exception ex)
            {
                TxtEstadoVentana.Text = "❌ Error al abrir historial";
                MessageBox.Show($"Error al abrir historial:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region EVENTOS DE GRILLA

        private void BtnVerDetalles_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProcesoFabricacion proceso)
            {
                try
                {
                    MostrarDetallesProceso(proceso);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al mostrar detalles:\n\n{ex.Message}",
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProcesoFabricacion proceso)
            {
                try
                {
                    TxtEstadoVentana.Text = $"✏️ Editando proceso: {proceso.NombreProducto}";

                    // ✅ CREAR Y ABRIR CrearEditarProcesoWindow
                    var editarWindow = new CrearEditarProcesoWindow(proceso)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (editarWindow.ShowDialog() == true)
                    {
                        _ = CargarProcesos();
                        _ = ActualizarEstadisticas();
                        TxtEstadoVentana.Text = "✅ Proceso editado exitosamente";
                    }
                    else
                    {
                        TxtEstadoVentana.Text = "✅ Gestión de Fabricación";
                    }
                }
                catch (Exception ex)
                {
                    TxtEstadoVentana.Text = "❌ Error al editar proceso";
                    MessageBox.Show($"Error al editar proceso:\n\n{ex.Message}",
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnFabricar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProcesoFabricacion proceso)
            {
                try
                {
                    if (!proceso.PuedeFabricarse)
                    {
                        MessageBox.Show($"No se puede fabricar '{proceso.NombreProducto}' en este momento.\n\n" +
                                       "Verifica que tengas suficientes materias primas disponibles.",
                                       "No se puede fabricar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    TxtEstadoVentana.Text = $"🏭 Iniciando fabricación: {proceso.NombreProducto}";

                    // ✅ CREAR Y ABRIR EjecutarFabricacionWindow
                    var fabricarWindow = new EjecutarFabricacionWindow(proceso)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (fabricarWindow.ShowDialog() == true)
                    {
                        _ = CargarProcesos();
                        _ = ActualizarEstadisticas();
                        TxtEstadoVentana.Text = "✅ Fabricación completada exitosamente";
                    }
                    else
                    {
                        TxtEstadoVentana.Text = "✅ Gestión de Fabricación";
                    }
                }
                catch (Exception ex)
                {
                    TxtEstadoVentana.Text = "❌ Error en fabricación";
                    MessageBox.Show($"Error al iniciar fabricación:\n\n{ex.Message}",
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async void BtnActivarDesactivar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProcesoFabricacion proceso)
            {
                try
                {
                    string accion = proceso.Activo ? "desactivar" : "activar";
                    var resultado = MessageBox.Show(
                        $"¿Está seguro que desea {accion} el proceso '{proceso.NombreProducto}'?",
                        $"Confirmar {accion}",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        using var context = new AppDbContext();
                        var procesoActualizar = await context.ProcesosFabricacion
                            .FirstOrDefaultAsync(p => p.Id == proceso.Id);

                        if (procesoActualizar != null)
                        {
                            procesoActualizar.Activo = !procesoActualizar.Activo;
                            await context.SaveChangesAsync();

                            await CargarProcesos();
                            await ActualizarEstadisticas();

                            TxtEstadoVentana.Text = $"✅ Proceso {(procesoActualizar.Activo ? "activado" : "desactivado")} correctamente";
                        }
                    }
                }
                catch (Exception ex)
                {
                    TxtEstadoVentana.Text = "❌ Error al cambiar estado";
                    MessageBox.Show($"Error al cambiar estado del proceso:\n\n{ex.Message}",
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region FILTROS Y BÚSQUEDA

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void CmbEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            try
            {
                // ✅ VERIFICAR QUE DGPROCESOS ESTÉ DISPONIBLE
                if (DgProcesos == null || _procesosOriginales == null)
                    return;

                var procesosTemp = new List<ProcesoFabricacion>(_procesosOriginales);

                // Filtro por texto de búsqueda
                if (!string.IsNullOrWhiteSpace(TxtBuscar?.Text))
                {
                    string textoBusqueda = TxtBuscar.Text.ToLower().Trim();
                    procesosTemp = procesosTemp.Where(p =>
                        (p.NombreProducto?.ToLower().Contains(textoBusqueda) ?? false) ||
                        (p.Descripcion?.ToLower().Contains(textoBusqueda) ?? false) ||
                        (p.CategoriaProducto?.ToLower().Contains(textoBusqueda) ?? false)
                    ).ToList();
                }

                // Filtro por categoría
                if (CmbCategoria?.SelectedItem is ComboBoxItem categoriaItem &&
                    categoriaItem.Content?.ToString() != "Todas las categorías")
                {
                    string categoria = categoriaItem.Content.ToString();
                    procesosTemp = procesosTemp.Where(p =>
                        p.CategoriaProducto?.Equals(categoria, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                // Filtro por estado
                if (CmbEstado?.SelectedItem is ComboBoxItem estadoItem)
                {
                    string estado = estadoItem.Content?.ToString();
                    procesosTemp = estado switch
                    {
                        "Activos" => procesosTemp.Where(p => p.Activo).ToList(),
                        "Inactivos" => procesosTemp.Where(p => !p.Activo).ToList(),
                        "Fabricables" => procesosTemp.Where(p => p.Activo && p.PuedeFabricarse).ToList(),
                        _ => procesosTemp
                    };
                }

                _procesosFiltrados = procesosTemp;
                DgProcesos.ItemsSource = _procesosFiltrados;
                ActualizarInfoFiltros();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en filtros: {ex.Message}");

                // En caso de error, mostrar todos los procesos originales
                if (DgProcesos != null && _procesosOriginales != null)
                {
                    _procesosFiltrados = new List<ProcesoFabricacion>(_procesosOriginales);
                    DgProcesos.ItemsSource = _procesosFiltrados;
                }
            }
        }

        private void ActualizarInfoFiltros()
        {
            try
            {
                // ✅ VERIFICACIONES DE SEGURIDAD
                if (TxtInfoFiltros == null || _procesosFiltrados == null || _procesosOriginales == null)
                    return;

                if (_procesosFiltrados.Count == _procesosOriginales.Count)
                {
                    TxtInfoFiltros.Text = $"Mostrando todos los procesos ({_procesosOriginales.Count})";
                }
                else
                {
                    TxtInfoFiltros.Text = $"Mostrando {_procesosFiltrados.Count} de {_procesosOriginales.Count} procesos";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarInfoFiltros: {ex.Message}");
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        private void MostrarDetallesProceso(ProcesoFabricacion proceso)
        {
            var detalles = $"🏭 DETALLES DEL PROCESO\n\n" +
                          $"📋 INFORMACIÓN BÁSICA:\n" +
                          $"   • Nombre: {proceso.NombreProducto}\n" +
                          $"   • Categoría: {proceso.CategoriaProducto}\n" +
                          $"   • Descripción: {proceso.Descripcion}\n" +
                          $"   • Rendimiento: {proceso.RendimientoEsperado:F2} {proceso.UnidadMedidaProducto}\n" +
                          $"   • Tiempo estimado: {proceso.TiempoFabricacionMinutos} min\n" +
                          $"   • Merma esperada: {proceso.PorcentajeMerma:F1}%\n\n" +

                          $"💰 COSTOS:\n" +
                          $"   • Materiales: {proceso.CostoMaterialesTotales:C2}\n" +
                          $"   • Mano de obra: {proceso.CostoManoObra:C2}\n" +
                          $"   • Costos adicionales: {proceso.CostosAdicionalesTotal:C2}\n" +
                          $"   • COSTO TOTAL: {proceso.CostoTotalPorLote:C2}\n" +
                          $"   • Costo unitario: {proceso.CostoUnitarioEstimado:C4}\n" +
                          $"   • Precio sugerido: {proceso.PrecioSugeridoVenta:C2}\n\n" +

                          $"📦 INGREDIENTES ({proceso.Ingredientes?.Count ?? 0}):\n";

            if (proceso.Ingredientes?.Any() == true)
            {
                foreach (var ingrediente in proceso.Ingredientes.OrderBy(i => i.OrdenAdicion))
                {
                    var disponible = ingrediente.PuedeUsarse ? "✅" : "❌";
                    detalles += $"   {ingrediente.OrdenAdicion}. {disponible} {ingrediente.NombreIngrediente}: " +
                               $"{ingrediente.CantidadRequerida:F2} {ingrediente.UnidadMedida} " +
                               $"(Stock: {ingrediente.StockDisponible:F2})\n";
                }
            }
            else
            {
                detalles += "   Sin ingredientes configurados\n";
            }

            detalles += $"\n📊 ESTADO:\n" +
                       $"   • Activo: {(proceso.Activo ? "Sí" : "No")}\n" +
                       $"   • Puede fabricarse: {(proceso.PuedeFabricarse ? "Sí" : "No")}\n" +
                       $"   • Creado: {proceso.FechaCreacion:dd/MM/yyyy HH:mm}\n" +
                       $"   • Actualizado: {proceso.FechaActualizacion:dd/MM/yyyy HH:mm}\n";

            if (!string.IsNullOrWhiteSpace(proceso.NotasEspeciales))
            {
                detalles += $"\n📝 NOTAS:\n{proceso.NotasEspeciales}";
            }

            MessageBox.Show(detalles, $"Detalles: {proceso.NombreProducto}",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region OTROS EVENTOS

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Implementar exportación a Excel/PDF
                MessageBox.Show("Función de exportación próximamente disponible.",
                               "Exportar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            var ayuda = "🏭 AYUDA - GESTIÓN DE FABRICACIÓN\n\n" +
                       "📋 FUNCIONES PRINCIPALES:\n" +
                       "• Crear proceso: Define recetas para fabricar productos\n" +
                       "• Editar proceso: Modifica ingredientes y costos\n" +
                       "• Fabricar: Ejecuta la producción y actualiza inventario\n" +
                       "• Ver historial: Consulta lotes fabricados anteriormente\n\n" +

                       "🔍 FILTROS DISPONIBLES:\n" +
                       "• Búsqueda por nombre, descripción o categoría\n" +
                       "• Filtrar por categoría específica\n" +
                       "• Ver solo activos, inactivos o fabricables\n\n" +

                       "📊 ESTADÍSTICAS:\n" +
                       "• Total de procesos configurados\n" +
                       "• Procesos activos y fabricables\n" +
                       "• Lotes fabricados hoy y en proceso\n\n" +

                       "💡 CONSEJOS:\n" +
                       "• Un proceso está 'Fabricable' cuando hay stock suficiente\n" +
                       "• Los costos se calculan automáticamente\n" +
                       "• Puedes activar/desactivar procesos sin eliminarlos\n" +
                       "• Los precios sugeridos incluyen tu margen objetivo";

            MessageBox.Show(ayuda, "Ayuda - Gestión de Fabricación",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region INTEGRACIÓN CON MAIN

        /// <summary>
        /// Método público para abrir desde ProcesosMainControl
        /// </summary>
        public static void AbrirFabricacion(Window owner = null)
        {
            try
            {
                var fabricacionWindow = new FabricacionProceso();

                if (owner != null)
                {
                    fabricacionWindow.Owner = owner;
                    fabricacionWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    fabricacionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                fabricacionWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir módulo de fabricación:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region LIMPIEZA

        private void FabricacionProceso_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _context?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar recursos: {ex.Message}");
            }
        }

        #endregion
    }
}