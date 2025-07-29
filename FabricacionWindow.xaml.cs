using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using costbenefi.Data;

namespace costbenefi.Views
{
    public partial class FabricacionWindow : Window
    {
        private AppDbContext _context;
        private List<RecetaFabricacion> _recetas = new();
        private List<RecetaFabricacion> _recetasFiltradas = new();

        public FabricacionWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                TxtStatusFabricacion.Text = "⏳ Cargando recetas de fabricación...";

                // Cargar datos iniciales
                await CargarRecetas();
                await ActualizarEstadisticas();

                TxtStatusFabricacion.Text = "✅ Sistema de Fabricación listo";
            }
            catch (Exception ex)
            {
                TxtStatusFabricacion.Text = "❌ Error al cargar sistema de fabricación";
                System.Diagnostics.Debug.WriteLine($"Error en FabricacionWindow: {ex.Message}");

                MessageBox.Show($"Error al inicializar ventana de fabricación:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Eventos de Botones Principales

        /// <summary>
        /// Abre la ventana para crear una nueva receta
        /// </summary>
        private void BtnNuevaReceta_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusFabricacion.Text = "➕ Abriendo formulario de nueva receta...";

                // TODO: Crear ventana CrearRecetaWindow
                MessageBox.Show("🔧 Formulario de Nueva Receta\n\n" +
                              "Esta funcionalidad se implementará en el siguiente paso.\n\n" +
                              "Permitirá:\n" +
                              "• Seleccionar ingredientes del inventario\n" +
                              "• Definir cantidades necesarias\n" +
                              "• Especificar producto final\n" +
                              "• Calcular costos automáticamente",
                              "Próxima Implementación", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusFabricacion.Text = "✅ Funcionalidad en desarrollo";
            }
            catch (Exception ex)
            {
                TxtStatusFabricacion.Text = "❌ Error al abrir nueva receta";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Ejecuta proceso de fabricación masivo
        /// </summary>
        private void BtnEjecutarFabricacion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtStatusFabricacion.Text = "⚙️ Preparando ejecutor de fabricación...";

                // TODO: Crear ventana EjecutarFabricacionWindow
                MessageBox.Show("⚙️ Ejecutor de Fabricación\n\n" +
                              "Esta funcionalidad permitirá:\n\n" +
                              "• Seleccionar múltiples recetas\n" +
                              "• Definir cantidades a fabricar\n" +
                              "• Verificar disponibilidad de stock\n" +
                              "• Procesar en lotes\n" +
                              "• Actualizar inventario automáticamente\n" +
                              "• Generar reporte de fabricación",
                              "Próxima Implementación", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtStatusFabricacion.Text = "✅ Funcionalidad en desarrollo";
            }
            catch (Exception ex)
            {
                TxtStatusFabricacion.Text = "❌ Error al ejecutar fabricación";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edita la receta seleccionada
        /// </summary>
        private void BtnEditarReceta_Click(object sender, RoutedEventArgs e)
        {
            if (DgRecetas.SelectedItem is RecetaFabricacion recetaSeleccionada)
            {
                try
                {
                    TxtStatusFabricacion.Text = $"✏️ Editando receta: {recetaSeleccionada.NombreReceta}";

                    // TODO: Abrir ventana de edición
                    MessageBox.Show($"✏️ Editar Receta: {recetaSeleccionada.NombreReceta}\n\n" +
                                  "Funcionalidad en desarrollo...\n\n" +
                                  "Permitirá modificar:\n" +
                                  "• Ingredientes y cantidades\n" +
                                  "• Información del producto final\n" +
                                  "• Tiempos de fabricación\n" +
                                  "• Costos y márgenes",
                                  "Próxima Implementación", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtStatusFabricacion.Text = "✅ Funcionalidad en desarrollo";
                }
                catch (Exception ex)
                {
                    TxtStatusFabricacion.Text = "❌ Error al editar receta";
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione una receta para editar.",
                              "Selección Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Elimina la receta seleccionada
        /// </summary>
        private void BtnEliminarReceta_Click(object sender, RoutedEventArgs e)
        {
            if (DgRecetas.SelectedItem is RecetaFabricacion recetaSeleccionada)
            {
                try
                {
                    var resultado = MessageBox.Show(
                        $"¿Eliminar la receta '{recetaSeleccionada.NombreReceta}'?\n\n" +
                        $"Producto final: {recetaSeleccionada.ProductoFinal}\n" +
                        $"Esta acción no se puede deshacer.",
                        "Confirmar Eliminación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        // TODO: Implementar eliminación en base de datos
                        TxtStatusFabricacion.Text = $"🗑️ Receta '{recetaSeleccionada.NombreReceta}' eliminada";

                        MessageBox.Show("✅ Receta eliminada correctamente.\n\n" +
                                      "Nota: Esta funcionalidad se completará cuando se implementen los modelos de base de datos.",
                                      "Eliminación Simulada", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    TxtStatusFabricacion.Text = "❌ Error al eliminar receta";
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Seleccione una receta para eliminar.",
                              "Selección Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Eventos de Búsqueda y Filtros

        private void TxtBuscarReceta_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarRecetas();
        }

        private void BtnBuscarReceta_Click(object sender, RoutedEventArgs e)
        {
            TxtBuscarReceta.Focus();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _context?.Dispose();
                _context = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cerrar FabricacionWindow: {ex.Message}");
            }
            base.OnClosed(e);
        }
        private void FiltrarRecetas()
        {
            try
            {
                string textoBusqueda = TxtBuscarReceta.Text.ToLower().Trim();

                if (string.IsNullOrEmpty(textoBusqueda))
                {
                    _recetasFiltradas = new List<RecetaFabricacion>(_recetas);
                }
                else
                {
                    _recetasFiltradas = _recetas.Where(r =>
                        r.NombreReceta.ToLower().Contains(textoBusqueda) ||
                        r.ProductoFinal.ToLower().Contains(textoBusqueda) ||
                        r.Descripcion.ToLower().Contains(textoBusqueda)
                    ).ToList();
                }

                ActualizarGridRecetas();
            }
            catch (Exception ex)
            {
                TxtStatusFabricacion.Text = "❌ Error al filtrar recetas";
                System.Diagnostics.Debug.WriteLine($"Error en filtrado: {ex.Message}");
            }
        }

        #endregion

        #region Eventos del Grid y Detalles

        private void DgRecetas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgRecetas.SelectedItem is RecetaFabricacion recetaSeleccionada)
            {
                MostrarDetallesReceta(recetaSeleccionada);
            }
            else
            {
                OcultarDetallesReceta();
            }
        }

        private void MostrarDetallesReceta(RecetaFabricacion receta)
        {
            try
            {
                // Mostrar información básica
                TxtNombreRecetaDetalle.Text = receta.NombreReceta;
                TxtDescripcionReceta.Text = receta.Descripcion;
                TxtProduceDetalle.Text = $"{receta.CantidadProducida} {receta.UnidadProducida}";
                TxtTiempoDetalle.Text = receta.TiempoFabricacion;
                TxtCostoDetalle.Text = receta.CostoEstimado.ToString("C2");

                // Mostrar ingredientes (simulados por ahora)
                var ingredientesEjemplo = GenerarIngredientesEjemplo(receta);
                LstIngredientes.ItemsSource = ingredientesEjemplo;

                // Verificar disponibilidad de stock (simulado)
                VerificarDisponibilidadStock(receta);

                // Mostrar paneles
                TxtMensajeSeleccion.Visibility = Visibility.Collapsed;
                PanelInfoReceta.Visibility = Visibility.Visible;
                PanelBotonesReceta.Visibility = Visibility.Visible;

                TxtStatusFabricacion.Text = $"📋 Mostrando detalles de: {receta.NombreReceta}";
            }
            catch (Exception ex)
            {
                TxtStatusFabricacion.Text = "❌ Error al mostrar detalles";
                System.Diagnostics.Debug.WriteLine($"Error mostrando detalles: {ex.Message}");
            }
        }

        private void OcultarDetallesReceta()
        {
            TxtMensajeSeleccion.Visibility = Visibility.Visible;
            PanelInfoReceta.Visibility = Visibility.Collapsed;
            PanelBotonesReceta.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Eventos de Botones de Receta Individual

        private void BtnFabricarEstaReceta_Click(object sender, RoutedEventArgs e)
        {
            if (DgRecetas.SelectedItem is RecetaFabricacion receta)
            {
                try
                {
                    TxtStatusFabricacion.Text = $"⚙️ Fabricando: {receta.NombreReceta}";

                    MessageBox.Show($"⚙️ Fabricar: {receta.NombreReceta}\n\n" +
                                  $"Producto final: {receta.ProductoFinal}\n" +
                                  $"Cantidad: {receta.CantidadProducida} {receta.UnidadProducida}\n" +
                                  $"Tiempo estimado: {receta.TiempoFabricacion}\n" +
                                  $"Costo estimado: {receta.CostoEstimado:C2}\n\n" +
                                  "Esta funcionalidad ejecutará el proceso de fabricación completo:\n" +
                                  "• Verificar stock de ingredientes\n" +
                                  "• Descontar materiales del inventario\n" +
                                  "• Agregar producto final al inventario\n" +
                                  "• Registrar movimientos de fabricación",
                                  "Próxima Implementación", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtStatusFabricacion.Text = "✅ Proceso de fabricación preparado";
                }
                catch (Exception ex)
                {
                    TxtStatusFabricacion.Text = "❌ Error al fabricar receta";
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDuplicarReceta_Click(object sender, RoutedEventArgs e)
        {
            if (DgRecetas.SelectedItem is RecetaFabricacion receta)
            {
                try
                {
                    MessageBox.Show($"📋 Duplicar Receta: {receta.NombreReceta}\n\n" +
                                  "Se creará una copia de esta receta que podrá modificar independientemente.\n\n" +
                                  "Útil para crear variaciones o versiones mejoradas.",
                                  "Próxima Implementación", MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtStatusFabricacion.Text = "📋 Funcionalidad de duplicación en desarrollo";
                }
                catch (Exception ex)
                {
                    TxtStatusFabricacion.Text = "❌ Error al duplicar receta";
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAnalisisCosto_Click(object sender, RoutedEventArgs e)
        {
            if (DgRecetas.SelectedItem is RecetaFabricacion receta)
            {
                try
                {
                    string analisis = $"📊 ANÁLISIS COSTO-BENEFICIO\n" +
                                    $"Receta: {receta.NombreReceta}\n\n" +
                                    $"💰 COSTOS:\n" +
                                    $"  • Materias primas: {receta.CostoEstimado:C2}\n" +
                                    $"  • Mano de obra: {(receta.CostoEstimado * 0.3m):C2} (est.)\n" +
                                    $"  • Costos indirectos: {(receta.CostoEstimado * 0.1m):C2} (est.)\n" +
                                    $"  • COSTO TOTAL: {(receta.CostoEstimado * 1.4m):C2}\n\n" +
                                    $"📈 BENEFICIOS:\n" +
                                    $"  • Producto final: {receta.CantidadProducida} {receta.UnidadProducida}\n" +
                                    $"  • Valor estimado mercado: No disponible\n" +
                                    $"  • Margen estimado: Por calcular\n\n" +
                                    $"⏱️ TIEMPO:\n" +
                                    $"  • Fabricación: {receta.TiempoFabricacion}\n" +
                                    $"  • Costo por hora: Por definir\n\n" +
                                    $"💡 RECOMENDACIÓN:\n" +
                                    $"Configure precios de venta para análisis completo.";

                    MessageBox.Show(analisis, "Análisis Costo-Beneficio",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    TxtStatusFabricacion.Text = "📊 Análisis de costos mostrado";
                }
                catch (Exception ex)
                {
                    TxtStatusFabricacion.Text = "❌ Error en análisis de costos";
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Métodos de Datos y Actualización

        private async System.Threading.Tasks.Task CargarRecetas()
        {
            try
            {
                // TODO: Cargar desde base de datos cuando tengamos los modelos
                // Por ahora generar datos de ejemplo
                _recetas = GenerarRecetasEjemplo();
                _recetasFiltradas = new List<RecetaFabricacion>(_recetas);

                ActualizarGridRecetas();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando recetas: {ex.Message}");
                _recetas = new List<RecetaFabricacion>();
                _recetasFiltradas = new List<RecetaFabricacion>();
            }
        }

        private void ActualizarGridRecetas()
        {
            DgRecetas.ItemsSource = null;
            DgRecetas.ItemsSource = _recetasFiltradas;

            TxtCountRecetas.Text = $"{_recetasFiltradas.Count} recetas";
            TxtTotalRecetas.Text = $"{_recetas.Count} recetas";
        }

        private async System.Threading.Tasks.Task ActualizarEstadisticas()
        {
            try
            {
                // TODO: Implementar estadísticas reales
                TxtRecetasDisponibles.Text = $"Recetas: {_recetas.Count}";
                TxtFabricacionesHoy.Text = "0 hoy";
                TxtUltimaFabricacion.Text = "Última fabricación: Nunca";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando estadísticas: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Ejemplo (Temporal)

        private List<RecetaFabricacion> GenerarRecetasEjemplo()
        {
            return new List<RecetaFabricacion>
            {
                new RecetaFabricacion
                {
                    Id = 1,
                    NombreReceta = "Pan Integral Casero",
                    ProductoFinal = "Pan Integral",
                    CantidadProducida = 8,
                    UnidadProducida = "piezas",
                    TiempoFabricacion = "3 horas",
                    CostoEstimado = 45.50m,
                    Descripcion = "Receta tradicional de pan integral con semillas",
                    Activo = true
                },
                new RecetaFabricacion
                {
                    Id = 2,
                    NombreReceta = "Jabón Artesanal Lavanda",
                    ProductoFinal = "Jabón Lavanda",
                    CantidadProducida = 12,
                    UnidadProducida = "barras",
                    TiempoFabricacion = "4 horas + 24h secado",
                    CostoEstimado = 78.25m,
                    Descripcion = "Jabón natural con aceites esenciales de lavanda",
                    Activo = true
                },
                new RecetaFabricacion
                {
                    Id = 3,
                    NombreReceta = "Mesa de Pino Rústica",
                    ProductoFinal = "Mesa Comedor",
                    CantidadProducida = 1,
                    UnidadProducida = "pieza",
                    TiempoFabricacion = "2 días",
                    CostoEstimado = 850.00m,
                    Descripcion = "Mesa de comedor estilo rústico para 6 personas",
                    Activo = true
                }
            };
        }

        private List<IngredienteReceta> GenerarIngredientesEjemplo(RecetaFabricacion receta)
        {
            switch (receta.Id)
            {
                case 1: // Pan Integral
                    return new List<IngredienteReceta>
                    {
                        new IngredienteReceta { NombreIngrediente = "Harina integral", Cantidad = 500, Unidad = "gr" },
                        new IngredienteReceta { NombreIngrediente = "Agua tibia", Cantidad = 350, Unidad = "ml" },
                        new IngredienteReceta { NombreIngrediente = "Levadura", Cantidad = 10, Unidad = "gr" },
                        new IngredienteReceta { NombreIngrediente = "Sal", Cantidad = 8, Unidad = "gr" },
                        new IngredienteReceta { NombreIngrediente = "Aceite oliva", Cantidad = 30, Unidad = "ml" }
                    };

                case 2: // Jabón Lavanda
                    return new List<IngredienteReceta>
                    {
                        new IngredienteReceta { NombreIngrediente = "Aceite coco", Cantidad = 300, Unidad = "gr" },
                        new IngredienteReceta { NombreIngrediente = "Aceite oliva", Cantidad = 200, Unidad = "gr" },
                        new IngredienteReceta { NombreIngrediente = "Sosa cáustica", Cantidad = 65, Unidad = "gr" },
                        new IngredienteReceta { NombreIngrediente = "Aceite esencial lavanda", Cantidad = 15, Unidad = "ml" },
                        new IngredienteReceta { NombreIngrediente = "Agua destilada", Cantidad = 150, Unidad = "ml" }
                    };

                case 3: // Mesa Pino
                    return new List<IngredienteReceta>
                    {
                        new IngredienteReceta { NombreIngrediente = "Tablones pino", Cantidad = 8, Unidad = "piezas" },
                        new IngredienteReceta { NombreIngrediente = "Patas madera", Cantidad = 4, Unidad = "piezas" },
                        new IngredienteReceta { NombreIngrediente = "Tornillos madera", Cantidad = 32, Unidad = "piezas" },
                        new IngredienteReceta { NombreIngrediente = "Barniz natural", Cantidad = 0.5m, Unidad = "litros" },
                        new IngredienteReceta { NombreIngrediente = "Lija varios granos", Cantidad = 5, Unidad = "hojas" }
                    };

                default:
                    return new List<IngredienteReceta>();
            }
        }

        private void VerificarDisponibilidadStock(RecetaFabricacion receta)
        {
            // TODO: Verificar stock real contra base de datos
            // Por ahora simular verificación

            bool stockDisponible = new Random().Next(1, 10) > 3; // 70% probabilidad de tener stock

            if (stockDisponible)
            {
                BorderDisponibilidad.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)); // Verde claro
                TxtMensajeDisponibilidad.Text = "✅ Todos los ingredientes están disponibles";
                TxtMensajeDisponibilidad.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                BtnFabricarEstaReceta.IsEnabled = true;
            }
            else
            {
                BorderDisponibilidad.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // Rojo claro
                TxtMensajeDisponibilidad.Text = "⚠️ Algunos ingredientes no tienen stock suficiente";
                TxtMensajeDisponibilidad.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                BtnFabricarEstaReceta.IsEnabled = false;
            }
        }

        #endregion

    }
    #region Clases Temporales (Se moverán a Models después)

    public class RecetaFabricacion
    {
        public int Id { get; set; }
        public string NombreReceta { get; set; } = "";
        public string ProductoFinal { get; set; } = "";
        public decimal CantidadProducida { get; set; }
        public string UnidadProducida { get; set; } = "";
        public string TiempoFabricacion { get; set; } = "";
        public decimal CostoEstimado { get; set; }
        public string Descripcion { get; set; } = "";
        public bool Activo { get; set; } = true;
    }

    public class IngredienteReceta
    {
        public string NombreIngrediente { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; } = "";
    }

    #endregion
}