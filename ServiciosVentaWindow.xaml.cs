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
    public partial class ServiciosVentaWindow : Window
    {
        private AppDbContext _context;
        private List<ServicioVenta> _servicios = new();
        private List<ServicioVenta> _serviciosFiltrados = new();
        private List<PromocionVenta> _promociones = new();

        public ServiciosVentaWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                TxtStatusServicios.Text = "⏳ Cargando servicios de venta...";

                // Cargar datos reales desde base de datos
                await CargarServicios();
                await CargarPromociones();
                await ActualizarEstadisticas();

                TxtStatusServicios.Text = "✅ Sistema de Servicios listo";
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error al cargar sistema de servicios";
                System.Diagnostics.Debug.WriteLine($"Error en ServiciosVentaWindow: {ex.Message}");

                MessageBox.Show($"Error al inicializar ventana de servicios:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Eventos de Botones Principales

        /// <summary>
        /// Abre la ventana para crear un nuevo servicio
        /// </summary>
        private async void BtnNuevoServicio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusServicios.Text = "➕ Abriendo formulario de nuevo servicio...";

                var ventanaCrear = new CrearEditarServicioWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                bool? resultado = ventanaCrear.ShowDialog();

                if (resultado == true)
                {
                    // Servicio creado exitosamente, recargar datos
                    await CargarServicios();
                    await ActualizarEstadisticas();
                    TxtStatusServicios.Text = "✅ Nuevo servicio creado exitosamente";
                }
                else
                {
                    TxtStatusServicios.Text = "ℹ️ Creación de servicio cancelada";
                }
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error al abrir formulario";
                MessageBox.Show($"Error al abrir formulario de creación:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Abre la ventana para crear una nueva promoción
        /// </summary>
        private async void BtnNuevaPromocion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusServicios.Text = "🎁 Creando nueva promoción...";

                // Crear promoción de ejemplo
                var nuevaPromocion = new PromocionVenta
                {
                    NombrePromocion = "Promoción Especial",
                    TipoPromocion = "DescuentoPorcentaje",
                    ValorPromocion = 15.0m,
                    FechaInicio = DateTime.Now,
                    FechaFin = DateTime.Now.AddDays(30),
                    CategoriaPromocion = "General",
                    UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema"
                };

                // Generar código automático
                nuevaPromocion.CodigoPromocion = nuevaPromocion.GenerarCodigoPromocion();

                _context.PromocionesVenta.Add(nuevaPromocion);
                _context.SaveChanges();

                MessageBox.Show($"🎁 Promoción creada exitosamente!\n\n" +
                              $"Nombre: {nuevaPromocion.NombrePromocion}\n" +
                              $"Descuento: {nuevaPromocion.ValorPromocion}%\n" +
                              $"Vigencia: {nuevaPromocion.DiasRestantes} días\n" +
                              $"Código: {nuevaPromocion.CodigoPromocion}\n\n" +
                              "La promoción ha sido guardada en la base de datos.",
                              "Promoción Creada", MessageBoxButton.OK, MessageBoxImage.Information);

                // Recargar lista
                await CargarPromociones();
                TxtStatusServicios.Text = "✅ Nueva promoción creada y guardada";
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error al crear promoción";
                MessageBox.Show($"Error al crear promoción:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edita el servicio seleccionado
        /// </summary>
        private async void BtnEditarServicio_Click(object sender, RoutedEventArgs e)
        {
            if (DgServicios.SelectedItem is ServicioVenta servicioSeleccionado)
            {
                try
                {
                    TxtStatusServicios.Text = $"✏️ Abriendo editor para: {servicioSeleccionado.NombreServicio}";

                    // Buscar el servicio completo en la base de datos
                    var servicio = await _context.ServiciosVenta
                        .Include(s => s.MaterialesNecesarios)
                            .ThenInclude(m => m.RawMaterial)
                        .FirstOrDefaultAsync(s => s.Id == servicioSeleccionado.Id);

                    if (servicio != null)
                    {
                        var ventanaEditar = new CrearEditarServicioWindow(servicio)
                        {
                            Owner = this,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };

                        bool? resultado = ventanaEditar.ShowDialog();

                        if (resultado == true)
                        {
                            // Servicio editado exitosamente, recargar datos
                            await CargarServicios();
                            await ActualizarEstadisticas();
                            TxtStatusServicios.Text = "✅ Servicio actualizado exitosamente";
                        }
                        else
                        {
                            TxtStatusServicios.Text = "ℹ️ Edición de servicio cancelada";
                        }
                    }
                    else
                    {
                        MessageBox.Show("No se encontró el servicio en la base de datos.",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtStatusServicios.Text = "❌ Servicio no encontrado";
                    }
                }
                catch (Exception ex)
                {
                    TxtStatusServicios.Text = "❌ Error al abrir editor";
                    MessageBox.Show($"Error al abrir editor de servicio:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un servicio para editar.",
                              "Selección Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Elimina el servicio seleccionado (eliminación lógica)
        /// </summary>
        private async void BtnEliminarServicio_Click(object sender, RoutedEventArgs e)
        {
            if (DgServicios.SelectedItem is ServicioVenta servicioSeleccionado)
            {
                try
                {
                    var resultado = MessageBox.Show(
                        $"¿Eliminar el servicio '{servicioSeleccionado.NombreServicio}'?\n\n" +
                        $"Precio: {servicioSeleccionado.PrecioServicio:C2}\n" +
                        $"Categoría: {servicioSeleccionado.CategoriaServicio}\n\n" +
                        $"El servicio será marcado como eliminado pero se mantendrá en el historial.",
                        "Confirmar Eliminación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        // Buscar el servicio en la base de datos
                        var servicio = _context.ServiciosVenta.Find(servicioSeleccionado.Id);
                        if (servicio != null)
                        {
                            // Eliminación lógica
                            string usuario = UserService.UsuarioActual?.NombreUsuario ?? "Sistema";
                            servicio.MarcarComoEliminado(usuario, "Eliminación desde interfaz de servicios");

                            _context.SaveChanges();

                            MessageBox.Show($"✅ Servicio '{servicio.NombreServicio}' eliminado correctamente.\n\n" +
                                          $"El servicio ha sido marcado como eliminado y removido de las listas activas.",
                                          "Servicio Eliminado", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Recargar datos
                            await CargarServicios();
                            TxtStatusServicios.Text = $"🗑️ Servicio '{servicio.NombreServicio}' eliminado";
                        }
                        else
                        {
                            MessageBox.Show("No se encontró el servicio en la base de datos.",
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TxtStatusServicios.Text = "❌ Error al eliminar servicio";
                    MessageBox.Show($"Error al eliminar servicio:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione un servicio para eliminar.",
                              "Selección Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Eventos de Búsqueda y Filtros

        private void TxtBuscarServicio_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarServicios();
        }

        private void BtnBuscarServicio_Click(object sender, RoutedEventArgs e)
        {
            TxtBuscarServicio.Focus();
        }

        private void FiltrarServicios()
        {
            try
            {
                string textoBusqueda = TxtBuscarServicio.Text.ToLower().Trim();

                if (string.IsNullOrEmpty(textoBusqueda))
                {
                    _serviciosFiltrados = new List<ServicioVenta>(_servicios);
                }
                else
                {
                    _serviciosFiltrados = _servicios.Where(s =>
                        s.NombreServicio.ToLower().Contains(textoBusqueda) ||
                        s.CategoriaServicio.ToLower().Contains(textoBusqueda) ||
                        s.Descripcion.ToLower().Contains(textoBusqueda)
                    ).ToList();
                }

                ActualizarGridServicios();
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error al filtrar servicios";
                System.Diagnostics.Debug.WriteLine($"Error en filtrado: {ex.Message}");
            }
        }

        #endregion

        #region Eventos del Grid y Detalles

        private void DgServicios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgServicios.SelectedItem is ServicioVenta servicioSeleccionado)
            {
                MostrarDetallesServicio(servicioSeleccionado);
            }
            else
            {
                OcultarDetallesServicio();
            }
        }

        private async void MostrarDetallesServicio(ServicioVenta servicio)
        {
            try
            {
                // Mostrar información básica
                TxtNombreServicioDetalle.Text = servicio.NombreServicio;
                TxtDescripcionServicio.Text = servicio.Descripcion;
                TxtPrecioDetalle.Text = servicio.PrecioServicio.ToString("C2");
                TxtDuracionDetalle.Text = servicio.DuracionEstimada;
                TxtMargenDetalle.Text = $"{servicio.MargenReal:F1}%";

                // Cargar materiales reales desde base de datos
                var materialesReales = await _context.GetMaterialesServicioAsync(servicio.Id);
                LstMaterialesServicio.ItemsSource = materialesReales;

                // Mostrar paneles
                TxtMensajeSeleccionServicio.Visibility = Visibility.Collapsed;
                PanelInfoServicio.Visibility = Visibility.Visible;
                PanelBotonesServicio.Visibility = Visibility.Visible;

                TxtStatusServicios.Text = $"🔍 Mostrando detalles de: {servicio.NombreServicio}";
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error al mostrar detalles";
                System.Diagnostics.Debug.WriteLine($"Error mostrando detalles: {ex.Message}");
            }
        }

        private void OcultarDetallesServicio()
        {
            TxtMensajeSeleccionServicio.Visibility = Visibility.Visible;
            PanelInfoServicio.Visibility = Visibility.Collapsed;
            PanelBotonesServicio.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Eventos de Botones de Servicio Individual

        /// <summary>
        /// Activa el servicio seleccionado en el punto de venta
        /// </summary>
        private async void BtnActivarPOS_Click(object sender, RoutedEventArgs e)
        {
            if (DgServicios.SelectedItem is ServicioVenta servicio)
            {
                try
                {
                    TxtStatusServicios.Text = $"💰 Activando en POS: {servicio.NombreServicio}";

                    // Buscar el servicio en la base de datos
                    var servicioDb = _context.ServiciosVenta.Find(servicio.Id);
                    if (servicioDb != null)
                    {
                        // Configurar para POS
                        servicioDb.ConfigurarParaPOS(true);
                        _context.SaveChanges();

                        MessageBox.Show($"💰 Servicio activado en POS!\n\n" +
                                      $"Servicio: {servicioDb.NombreServicio}\n" +
                                      $"Precio: {servicioDb.PrecioServicio:C2}\n" +
                                      $"Código POS: {servicioDb.CodigoServicio}\n" +
                                      $"Estado: {(servicioDb.IntegradoPOS ? "✅ Integrado" : "❌ No integrado")}\n\n" +
                                      "El servicio ahora aparecerá en el punto de venta.",
                                      "Activado en POS", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Recargar datos
                        await CargarServicios();
                        TxtStatusServicios.Text = "✅ Servicio activado en POS";
                    }
                }
                catch (Exception ex)
                {
                    TxtStatusServicios.Text = "❌ Error al activar en POS";
                    MessageBox.Show($"Error al activar en POS:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Duplica el servicio seleccionado
        /// </summary>
        private async void BtnDuplicarServicio_Click(object sender, RoutedEventArgs e)
        {
            if (DgServicios.SelectedItem is ServicioVenta servicio)
            {
                try
                {
                    // Buscar el servicio original en la base de datos
                    var servicioOriginal = _context.ServiciosVenta
                        .Include(s => s.MaterialesNecesarios)
                        .FirstOrDefault(s => s.Id == servicio.Id);

                    if (servicioOriginal != null)
                    {
                        // Crear copia del servicio
                        var servicioDuplicado = new ServicioVenta
                        {
                            NombreServicio = $"{servicioOriginal.NombreServicio} (Copia)",
                            Descripcion = servicioOriginal.Descripcion,
                            CategoriaServicio = servicioOriginal.CategoriaServicio,
                            PrecioBase = servicioOriginal.PrecioBase,
                            PrecioServicio = servicioOriginal.PrecioServicio,
                            DuracionEstimada = servicioOriginal.DuracionEstimada,
                            CostoMateriales = servicioOriginal.CostoMateriales,
                            CostoManoObra = servicioOriginal.CostoManoObra,
                            MargenObjetivo = servicioOriginal.MargenObjetivo,
                            PorcentajeIVA = servicioOriginal.PorcentajeIVA,
                            UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema"
                        };

                        _context.ServiciosVenta.Add(servicioDuplicado);
                        _context.SaveChanges();

                        // Duplicar materiales necesarios
                        foreach (var material in servicioOriginal.MaterialesNecesarios)
                        {
                            var materialDuplicado = new MaterialServicio
                            {
                                ServicioVentaId = servicioDuplicado.Id,
                                RawMaterialId = material.RawMaterialId,
                                CantidadNecesaria = material.CantidadNecesaria,
                                UnidadMedida = material.UnidadMedida,
                                CostoUnitario = material.CostoUnitario,
                                PorcentajeDesperdicio = material.PorcentajeDesperdicio,
                                EsOpcional = material.EsOpcional,
                                UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema"
                            };

                            _context.MaterialesServicio.Add(materialDuplicado);
                        }

                        _context.SaveChanges();

                        MessageBox.Show($"📋 Servicio duplicado exitosamente!\n\n" +
                                      $"Original: {servicioOriginal.NombreServicio}\n" +
                                      $"Copia: {servicioDuplicado.NombreServicio}\n" +
                                      $"Materiales copiados: {servicioOriginal.MaterialesNecesarios.Count}\n\n" +
                                      "Puede editar la copia independientemente del original.",
                                      "Servicio Duplicado", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Recargar datos
                        await CargarServicios();
                        TxtStatusServicios.Text = "📋 Servicio duplicado exitosamente";
                    }
                }
                catch (Exception ex)
                {
                    TxtStatusServicios.Text = "❌ Error al duplicar servicio";
                    MessageBox.Show($"Error al duplicar servicio:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Muestra análisis de margen del servicio
        /// </summary>
        private void BtnAnalisisMargen_Click(object sender, RoutedEventArgs e)
        {
            if (DgServicios.SelectedItem is ServicioVenta servicio)
            {
                try
                {
                    // Usar el análisis financiero del modelo real
                    string analisis = servicio.ObtenerAnalisisFinanciero();

                    MessageBox.Show(analisis, "Análisis Financiero Completo",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtStatusServicios.Text = "📊 Análisis financiero mostrado";
                }
                catch (Exception ex)
                {
                    TxtStatusServicios.Text = "❌ Error en análisis financiero";
                    MessageBox.Show($"Error en análisis:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Eventos de Pestañas Adicionales

        /// <summary>
        /// Crea un nuevo combo de productos
        /// </summary>
        private async void BtnNuevoCombo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Crear promoción tipo combo
                var comboPromocion = new PromocionVenta
                {
                    NombrePromocion = "Combo Especial",
                    TipoPromocion = "Combo",
                    ValorPromocion = 20.0m, // 20% descuento
                    CategoriaPromocion = "Combos",
                    FechaInicio = DateTime.Now,
                    FechaFin = DateTime.Now.AddDays(15),
                    MontoMinimo = 200.00m,
                    CantidadMinima = 2,
                    UsuarioCreador = UserService.UsuarioActual?.NombreUsuario ?? "Sistema"
                };

                comboPromocion.CodigoPromocion = comboPromocion.GenerarCodigoPromocion();

                _context.PromocionesVenta.Add(comboPromocion);
                _context.SaveChanges();

                MessageBox.Show($"📦 Combo creado exitosamente!\n\n" +
                              $"Nombre: {comboPromocion.NombrePromocion}\n" +
                              $"Descuento: {comboPromocion.ValorPromocion}%\n" +
                              $"Código: {comboPromocion.CodigoPromocion}\n" +
                              $"Monto mínimo: {comboPromocion.MontoMinimo:C2}\n" +
                              $"Productos mínimos: {comboPromocion.CantidadMinima}\n\n" +
                              "El combo ha sido guardado como promoción.",
                              "Combo Creado", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusServicios.Text = "📦 Combo creado como promoción";
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error al crear combo";
                MessageBox.Show($"Error al crear combo:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Configura la integración con el punto de venta
        /// </summary>
        private async void BtnConfigurarPOS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener estadísticas reales
                var estadisticas = await _context.GetEstadisticasServiciosAsync();

                string configuracion = $"⚙️ CONFIGURACIÓN DE INTEGRACIÓN POS\n\n" +
                                      $"🎯 SERVICIOS ACTUALES:\n" +
                                      $"  • Total servicios: {estadisticas.TotalServicios}\n" +
                                      $"  • Servicios activos: {estadisticas.ServiciosActivos}\n" +
                                      $"  • Integrados en POS: {estadisticas.ServiciosIntegrados}\n" +
                                      $"  • Promociones vigentes: {estadisticas.PromocionesVigentes}\n\n" +
                                      $"💡 CONFIGURACIONES DISPONIBLES:\n" +
                                      $"  • Mostrar servicios como productos especiales\n" +
                                      $"  • Descuento automático de materiales\n" +
                                      $"  • Aplicación automática de promociones\n" +
                                      $"  • Categorización en POS\n" +
                                      $"  • Alertas de stock para servicios\n\n" +
                                      $"📊 REPORTES INTEGRADOS:\n" +
                                      $"  • Servicios más vendidos\n" +
                                      $"  • Margen por servicio\n" +
                                      $"  • Consumo de materiales por servicio";

                MessageBox.Show(configuracion, "Configuración POS - Estado Actual",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusServicios.Text = "⚙️ Configuración POS mostrada";
            }
            catch (Exception ex)
            {
                TxtStatusServicios.Text = "❌ Error en configuración POS";
                MessageBox.Show($"Error en configuración POS:\n\n{ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Métodos de Datos y Actualización

        /// <summary>
        /// Carga servicios reales desde la base de datos
        /// </summary>
        private async System.Threading.Tasks.Task CargarServicios()
        {
            try
            {
                // Cargar servicios activos desde la base de datos
                _servicios = await _context.ServiciosVenta
                    .Include(s => s.MaterialesNecesarios)
                        .ThenInclude(m => m.RawMaterial)
                    .Where(s => !s.Eliminado) // Solo servicios no eliminados
                    .OrderBy(s => s.NombreServicio)
                    .ToListAsync();

                _serviciosFiltrados = new List<ServicioVenta>(_servicios);
                ActualizarGridServicios();

                System.Diagnostics.Debug.WriteLine($"✅ Servicios cargados: {_servicios.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando servicios: {ex.Message}");
                _servicios = new List<ServicioVenta>();
                _serviciosFiltrados = new List<ServicioVenta>();

                // Mostrar mensaje de error
                MessageBox.Show($"Error al cargar servicios desde la base de datos:\n\n{ex.Message}",
                              "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Carga promociones reales desde la base de datos
        /// </summary>
        private async System.Threading.Tasks.Task CargarPromociones()
        {
            try
            {
                // Cargar promociones vigentes desde la base de datos
                _promociones = await _context.GetPromocionesVigentes()
                    .OrderBy(p => p.NombrePromocion)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"✅ Promociones cargadas: {_promociones.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando promociones: {ex.Message}");
                _promociones = new List<PromocionVenta>();

                MessageBox.Show($"Error al cargar promociones desde la base de datos:\n\n{ex.Message}",
                              "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ActualizarGridServicios()
        {
            DgServicios.ItemsSource = null;
            DgServicios.ItemsSource = _serviciosFiltrados;

            TxtCountServicios.Text = $"{_serviciosFiltrados.Count} servicios";
            TxtTotalServicios.Text = $"{_servicios.Count} servicios";
        }

        /// <summary>
        /// Actualiza estadísticas reales desde la base de datos
        /// </summary>
        private async System.Threading.Tasks.Task ActualizarEstadisticas()
        {
            try
            {
                // Obtener estadísticas reales
                var estadisticas = await _context.GetEstadisticasServiciosAsync();

                TxtServiciosActivos.Text = $"{estadisticas.ServiciosActivos} activos";
                TxtServiciosVendidos.Text = "Vendidos hoy: 0"; // TODO: Implementar cuando tengamos ventas de servicios
                TxtIngresoServicios.Text = "Ingresos: $0.00"; // TODO: Implementar cuando tengamos ventas de servicios

                // Estadísticas adicionales
                var serviciosMasRentables = await _context.GetServiciosMasRentables(3).ToListAsync();
                if (serviciosMasRentables.Any())
                {
                    var mejorServicio = serviciosMasRentables.First();
                    TxtStatusServicios.Text = $"💰 Servicio más rentable: {mejorServicio.NombreServicio} ({mejorServicio.MargenReal:F1}%)";
                }

                System.Diagnostics.Debug.WriteLine($"✅ Estadísticas actualizadas - Activos: {estadisticas.ServiciosActivos}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando estadísticas: {ex.Message}");

                // Estadísticas por defecto en caso de error
                TxtServiciosActivos.Text = "0 activos";
                TxtServiciosVendidos.Text = "Vendidos hoy: 0";
                TxtIngresoServicios.Text = "Ingresos: $0.00";
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Limpiar contexto propio
                _context?.Dispose();
                _context = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar ServiciosVentaWindow: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}