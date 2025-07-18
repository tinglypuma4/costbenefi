using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class ConfigurarPrecioVentaWindow : Window
    {
        private readonly AppDbContext _context;
        private ObservableCollection<ProductoPrecio> _productos;
        private List<ProductoPrecio> _productosOriginales;
        private bool _cambiosRealizados = false;

        // Referencias a controles
        private ComboBox CmbFiltroProductos;
        private DataGrid DgProductos;
        private TextBlock TxtEstadoHeader;
        private TextBox TxtMargenMasivo;
        private TextBlock TxtContadores;

        public ConfigurarPrecioVentaWindow(AppDbContext context)
        {
            _context = context;
            _productos = new ObservableCollection<ProductoPrecio>();
            InitializeComponent();
            DgProductos.ItemsSource = _productos;
            _ = LoadProductosAsync();
        }

        private void InitializeComponent()
        {
            Title = "💰 Configurar Precios de Venta";
            Width = 1200;
            Height = 750;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var mainGrid = new Grid();

            // Definir filas
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Toolbar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataGrid
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Panel configuración masiva
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones

            // Header
            var headerPanel = CreateHeaderPanel();
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Toolbar
            var toolbarPanel = CreateToolbarPanel();
            Grid.SetRow(toolbarPanel, 1);
            mainGrid.Children.Add(toolbarPanel);

            // DataGrid
            var dataGridPanel = CreateDataGridPanel();
            Grid.SetRow(dataGridPanel, 2);
            mainGrid.Children.Add(dataGridPanel);

            // Panel configuración masiva
            var configPanel = CreateConfiguracionMasivaPanel();
            Grid.SetRow(configPanel, 3);
            mainGrid.Children.Add(configPanel);

            // Botones
            var botonesPanel = CreateBotonesPanel();
            Grid.SetRow(botonesPanel, 4);
            mainGrid.Children.Add(botonesPanel);

            Content = mainGrid;
        }

        private UIElement CreateHeaderPanel()
        {
            var headerPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 0)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };

            var iconText = new TextBlock
            {
                Text = "💰",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var titleStack = new StackPanel();
            var titleText = new TextBlock
            {
                Text = "Configurar Precios de Venta",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            var subtitleText = new TextBlock
            {
                Text = "Configure precios de venta y márgenes para productos a granel y por pieza",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(240, 253, 250)),
                Margin = new Thickness(0, 5, 0, 0)
            };

            titleStack.Children.Add(titleText);
            titleStack.Children.Add(subtitleText);

            headerStack.Children.Add(iconText);
            headerStack.Children.Add(titleStack);

            TxtEstadoHeader = new TextBlock
            {
                Text = "Cargando productos...",
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(headerStack, 0);
            Grid.SetColumn(TxtEstadoHeader, 1);

            headerGrid.Children.Add(headerStack);
            headerGrid.Children.Add(TxtEstadoHeader);

            headerPanel.Child = headerGrid;
            return headerPanel;
        }

        private UIElement CreateToolbarPanel()
        {
            var toolbarPanel = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 10, 20, 10)
            };

            var toolbarGrid = new Grid();
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Filtros y contadores
            var filtrosStack = new StackPanel { Orientation = Orientation.Horizontal };

            var lblFiltro = new TextBlock
            {
                Text = "Mostrar:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.SemiBold
            };

            CmbFiltroProductos = new ComboBox
            {
                Width = 200,
                Height = 30
            };
            CmbFiltroProductos.Items.Add("Todos los productos");
            CmbFiltroProductos.Items.Add("Sin precio configurado");
            CmbFiltroProductos.Items.Add("Con precio configurado");
            CmbFiltroProductos.Items.Add("Solo productos a granel");
            CmbFiltroProductos.Items.Add("Solo productos por pieza");
            CmbFiltroProductos.Items.Add("Margen bajo (<20%)");
            CmbFiltroProductos.Items.Add("Productos inactivos");
            CmbFiltroProductos.SelectedIndex = 1; // Por defecto "Sin precio configurado"
            CmbFiltroProductos.SelectionChanged += CmbFiltroProductos_SelectionChanged;

            TxtContadores = new TextBlock
            {
                Text = "Productos: 0 | Granel: 0 | Pieza: 0",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };

            filtrosStack.Children.Add(lblFiltro);
            filtrosStack.Children.Add(CmbFiltroProductos);
            filtrosStack.Children.Add(TxtContadores);

            // Botones de acción
            var accionesStack = new StackPanel { Orientation = Orientation.Horizontal };

            var btnActualizar = new Button
            {
                Content = "🔄 Actualizar",
                Width = 100,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnActualizar.Click += BtnActualizar_Click;

            var btnCalcularTodos = new Button
            {
                Content = "🧮 Calcular Todos",
                Width = 120,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0)
            };
            btnCalcularTodos.Click += BtnCalcularTodos_Click;

            accionesStack.Children.Add(btnActualizar);
            accionesStack.Children.Add(btnCalcularTodos);

            Grid.SetColumn(filtrosStack, 0);
            Grid.SetColumn(accionesStack, 1);

            toolbarGrid.Children.Add(filtrosStack);
            toolbarGrid.Children.Add(accionesStack);

            toolbarPanel.Child = toolbarGrid;
            return toolbarPanel;
        }

        private UIElement CreateDataGridPanel()
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Margin = new Thickness(10, 5, 10, 5)
            };

            DgProductos = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Extended,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                RowBackground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                FontSize = 11,
                RowHeight = 35,
                ColumnHeaderHeight = 40
            };

            // Estilo del header
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Color.FromRgb(55, 65, 81))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(10, 8, 10, 8)));
            DgProductos.ColumnHeaderStyle = headerStyle;

            // Columnas
            DgProductos.Columns.Add(CreateTextColumn("Producto", "NombreProducto", 160, FontWeights.SemiBold));
            DgProductos.Columns.Add(CreateTextColumn("Categoría", "Categoria", 100, FontWeights.Normal, "#6B7280"));
            DgProductos.Columns.Add(CreateTemplateColumn("Tipo", "TipoProducto", 80));
            DgProductos.Columns.Add(CreateTextColumn("Unidad", "UnidadMedida", 80, FontWeights.Normal, "#6B7280", TextAlignment.Center));
            DgProductos.Columns.Add(CreateTextColumn("Stock", "StockTotal", 70, FontWeights.Bold, "#374151", TextAlignment.Right, "F2"));
            DgProductos.Columns.Add(CreateTextColumn("Costo", "PrecioConIVA", 80, FontWeights.Normal, "#EF4444", TextAlignment.Right, "C2"));
            DgProductos.Columns.Add(CreateEditableColumn("Precio Venta", "PrecioVentaString", 100));
            DgProductos.Columns.Add(CreateTextColumn("Margen %", "MargenCalculado", 80, FontWeights.Bold, "#10B981", TextAlignment.Right, "F1"));
            DgProductos.Columns.Add(CreateTextColumn("Ganancia", "GananciaUnitaria", 80, FontWeights.Normal, "#059669", TextAlignment.Right, "C2"));
            DgProductos.Columns.Add(CreateTemplateColumn("Estado", "EstadoPrecio", 90));
            DgProductos.Columns.Add(CreateCheckboxColumn("Activo POS", "ActivoParaVenta", 80));

            border.Child = DgProductos;
            return border;
        }

        private UIElement CreateConfiguracionMasivaPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Configuración masiva por margen
            var margenStack = new StackPanel { Orientation = Orientation.Horizontal };
            var lblMargen = new TextBlock
            {
                Text = "Aplicar margen masivo:",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0)
            };

            TxtMargenMasivo = new TextBox
            {
                Width = 80,
                Height = 30,
                Text = "30",
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var lblPorcentaje = new TextBlock
            {
                Text = "%",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 10, 0)
            };

            var btnAplicarMargen = new Button
            {
                Content = "Aplicar a Seleccionados",
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(15, 0, 15, 0)
            };
            btnAplicarMargen.Click += BtnAplicarMargen_Click;

            margenStack.Children.Add(lblMargen);
            margenStack.Children.Add(TxtMargenMasivo);
            margenStack.Children.Add(lblPorcentaje);
            margenStack.Children.Add(btnAplicarMargen);

            // Acciones rápidas
            var accionesStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnActivarTodos = new Button
            {
                Content = "✅ Activar Seleccionados",
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 0, 15, 0)
            };
            btnActivarTodos.Click += BtnActivarTodos_Click;

            var btnDesactivarTodos = new Button
            {
                Content = "❌ Desactivar Seleccionados",
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(15, 0, 15, 0)
            };
            btnDesactivarTodos.Click += BtnDesactivarTodos_Click;

            accionesStack.Children.Add(btnActivarTodos);
            accionesStack.Children.Add(btnDesactivarTodos);

            // Información
            var infoStack = new StackPanel();
            var infoText = new TextBlock
            {
                Text = "💡 Seleccione productos para aplicar cambios masivos",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var tipoText = new TextBlock
            {
                Text = "🔸 Granel: precio por kg/lt | 🔹 Pieza: precio por unidad",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 3, 0, 0)
            };

            infoStack.Children.Add(infoText);
            infoStack.Children.Add(tipoText);

            Grid.SetColumn(margenStack, 0);
            Grid.SetColumn(accionesStack, 1);
            Grid.SetColumn(infoStack, 2);

            grid.Children.Add(margenStack);
            grid.Children.Add(accionesStack);
            grid.Children.Add(infoStack);

            panel.Child = grid;
            return panel;
        }

        private UIElement CreateBotonesPanel()
        {
            var panel = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var estadoText = new TextBlock
            {
                Text = "✅ Listo para configurar precios",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };

            var botonesStack = new StackPanel { Orientation = Orientation.Horizontal };

            var btnCancelar = new Button
            {
                Content = "❌ Cancelar",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnCancelar.Click += BtnCancelar_Click;

            var btnGuardar = new Button
            {
                Content = "💾 Guardar Cambios",
                Width = 130,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                FontWeight = FontWeights.Bold
            };
            btnGuardar.Click += BtnGuardar_Click;

            botonesStack.Children.Add(btnCancelar);
            botonesStack.Children.Add(btnGuardar);

            Grid.SetColumn(estadoText, 0);
            Grid.SetColumn(botonesStack, 1);

            grid.Children.Add(estadoText);
            grid.Children.Add(botonesStack);

            panel.Child = grid;
            return panel;
        }

        #region Métodos para crear columnas del DataGrid

        private DataGridTextColumn CreateTextColumn(string header, string binding, double width,
            FontWeight fontWeight = default, string foreground = "#374151",
            TextAlignment alignment = TextAlignment.Left, string format = null)
        {
            var column = new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(binding) { StringFormat = format },
                Width = width
            };

            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 0, 8, 0)));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));

            if (fontWeight != default(FontWeight))
                style.Setters.Add(new Setter(TextBlock.FontWeightProperty, fontWeight));

            if (foreground != "#374151")
                style.Setters.Add(new Setter(TextBlock.ForegroundProperty,
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground))));

            column.ElementStyle = style;
            return column;
        }

        private DataGridTemplateColumn CreateEditableColumn(string header, string binding, double width)
        {
            var column = new DataGridTemplateColumn
            {
                Header = header,
                Width = width
            };

            // Template para mostrar
            var cellTemplate = new DataTemplate();
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding(binding));
            textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(8, 0, 8, 0));
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            textBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            textBlockFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(16, 185, 129)));
            cellTemplate.VisualTree = textBlockFactory;

            // Template para editar
            var editTemplate = new DataTemplate();
            var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
            textBoxFactory.SetBinding(TextBox.TextProperty, new Binding(binding) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(5, 5, 5, 5));
            textBoxFactory.SetValue(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            textBoxFactory.SetValue(TextBox.TextAlignmentProperty, TextAlignment.Right);
            editTemplate.VisualTree = textBoxFactory;

            column.CellTemplate = cellTemplate;
            column.CellEditingTemplate = editTemplate;

            return column;
        }

        private DataGridTemplateColumn CreateTemplateColumn(string header, string binding, double width)
        {
            var column = new DataGridTemplateColumn
            {
                Header = header,
                Width = width
            };

            var cellTemplate = new DataTemplate();

            if (binding == "TipoProducto")
            {
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
                borderFactory.SetValue(Border.PaddingProperty, new Thickness(6, 2, 6, 2));
                borderFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding(binding));
                textBlockFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
                textBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                textBlockFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
                textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                // Trigger para tipo Granel
                var granelTrigger = new DataTrigger();
                granelTrigger.Binding = new Binding(binding);
                granelTrigger.Value = "🔸 Granel";
                granelTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(245, 158, 11))));

                // Trigger para tipo Pieza
                var piezaTrigger = new DataTrigger();
                piezaTrigger.Binding = new Binding(binding);
                piezaTrigger.Value = "🔹 Pieza";
                piezaTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(59, 130, 246))));

                var style = new Style(typeof(Border));
                style.Triggers.Add(granelTrigger);
                style.Triggers.Add(piezaTrigger);
                borderFactory.SetValue(Border.StyleProperty, style);

                borderFactory.AppendChild(textBlockFactory);
                cellTemplate.VisualTree = borderFactory;
            }
            else if (binding == "EstadoPrecio")
            {
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
                borderFactory.SetValue(Border.PaddingProperty, new Thickness(6, 2, 6, 2));
                borderFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding(binding));
                textBlockFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
                textBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                textBlockFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);

                // Triggers para diferentes estados
                var configuradoTrigger = new DataTrigger();
                configuradoTrigger.Binding = new Binding(binding);
                configuradoTrigger.Value = "✅ Configurado";
                configuradoTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(16, 185, 129))));

                var pendienteTrigger = new DataTrigger();
                pendienteTrigger.Binding = new Binding(binding);
                pendienteTrigger.Value = "⏳ Pendiente";
                pendienteTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(245, 158, 11))));

                var bajoTrigger = new DataTrigger();
                bajoTrigger.Binding = new Binding(binding);
                bajoTrigger.Value = "⚠️ Margen Bajo";
                bajoTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                    new SolidColorBrush(Color.FromRgb(239, 68, 68))));

                var style = new Style(typeof(Border));
                style.Triggers.Add(configuradoTrigger);
                style.Triggers.Add(pendienteTrigger);
                style.Triggers.Add(bajoTrigger);
                borderFactory.SetValue(Border.StyleProperty, style);

                borderFactory.AppendChild(textBlockFactory);
                cellTemplate.VisualTree = borderFactory;
            }

            column.CellTemplate = cellTemplate;
            return column;
        }

        private DataGridCheckBoxColumn CreateCheckboxColumn(string header, string binding, double width)
        {
            var column = new DataGridCheckBoxColumn
            {
                Header = header,
                Binding = new Binding(binding),
                Width = width
            };

            var style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center));
            column.ElementStyle = style;

            return column;
        }

        #endregion

        #region Carga de datos

        private async Task LoadProductosAsync()
        {
            try
            {
                TxtEstadoHeader.Text = "Cargando productos...";

                var productos = await _context.RawMaterials
                    .Where(p => !p.Eliminado)
                    .OrderBy(p => p.NombreArticulo)
                    .ToListAsync();

                _productosOriginales = productos.Select(p => new ProductoPrecio(p)).ToList();

                AplicarFiltro();
                ActualizarContadores();

                TxtEstadoHeader.Text = $"✅ {productos.Count} productos cargados";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoHeader.Text = "❌ Error al cargar";
            }
        }

        private void AplicarFiltro()
        {
            if (_productosOriginales == null) return;

            var filtro = CmbFiltroProductos.SelectedIndex;
            IEnumerable<ProductoPrecio> productosFiltrados = _productosOriginales;

            switch (filtro)
            {
                case 0: // Todos
                    break;
                case 1: // Sin precio configurado
                    productosFiltrados = _productosOriginales.Where(p => p.PrecioVenta <= 0);
                    break;
                case 2: // Con precio configurado
                    productosFiltrados = _productosOriginales.Where(p => p.PrecioVenta > 0);
                    break;
                case 3: // Solo granel
                    productosFiltrados = _productosOriginales.Where(p => p.EsProductoAGranel);
                    break;
                case 4: // Solo pieza
                    productosFiltrados = _productosOriginales.Where(p => !p.EsProductoAGranel);
                    break;
                case 5: // Margen bajo
                    productosFiltrados = _productosOriginales.Where(p => p.MargenCalculado < 20 && p.PrecioVenta > 0);
                    break;
                case 6: // Inactivos
                    productosFiltrados = _productosOriginales.Where(p => !p.ActivoParaVenta);
                    break;
            }

            _productos.Clear();
            foreach (var producto in productosFiltrados)
            {
                _productos.Add(producto);
            }

            ActualizarContadores();
        }

        private void ActualizarContadores()
        {
            var total = _productos.Count;
            var granel = _productos.Count(p => p.EsProductoAGranel);
            var pieza = _productos.Count(p => !p.EsProductoAGranel);

            TxtContadores.Text = $"Productos: {total} | Granel: {granel} | Pieza: {pieza}";
        }

        #endregion

        #region Event Handlers

        private void CmbFiltroProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await LoadProductosAsync();
        }

        private void BtnCalcularTodos_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtMargenMasivo.Text, out decimal margen) || margen <= 0)
            {
                MessageBox.Show("Ingrese un margen válido mayor a 0%", "Margen Inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var productosValidos = _productos.Where(p => p.PrecioConIVA > 0).ToList();

            if (!productosValidos.Any())
            {
                MessageBox.Show("No hay productos con costo válido para calcular precios.",
                    "Sin Productos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var resultado = MessageBox.Show(
                $"¿Calcular precios con {margen}% de margen para {productosValidos.Count} productos?\n\n" +
                "Esto sobrescribirá los precios existentes.",
                "Confirmar Cálculo Masivo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                foreach (var producto in productosValidos)
                {
                    producto.AplicarMargen(margen);
                }

                _cambiosRealizados = true;
                MessageBox.Show($"✅ Precios calculados para {productosValidos.Count} productos",
                    "Cálculo Completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnAplicarMargen_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = DgProductos.SelectedItems.Cast<ProductoPrecio>().ToList();

            if (!seleccionados.Any())
            {
                MessageBox.Show("Seleccione al menos un producto.", "Selección Requerida",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(TxtMargenMasivo.Text, out decimal margen) || margen <= 0)
            {
                MessageBox.Show("Ingrese un margen válido mayor a 0%", "Margen Inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var producto in seleccionados.Where(p => p.PrecioConIVA > 0))
            {
                producto.AplicarMargen(margen);
            }

            _cambiosRealizados = true;
            MessageBox.Show($"✅ Margen aplicado a {seleccionados.Count} productos",
                "Margen Aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnActivarTodos_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = DgProductos.SelectedItems.Cast<ProductoPrecio>().ToList();

            if (!seleccionados.Any())
            {
                MessageBox.Show("Seleccione al menos un producto.", "Selección Requerida",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var producto in seleccionados)
            {
                producto.ActivoParaVenta = true;
            }

            _cambiosRealizados = true;
            MessageBox.Show($"✅ {seleccionados.Count} productos activados para POS",
                "Productos Activados", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDesactivarTodos_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = DgProductos.SelectedItems.Cast<ProductoPrecio>().ToList();

            if (!seleccionados.Any())
            {
                MessageBox.Show("Seleccione al menos un producto.", "Selección Requerida",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var producto in seleccionados)
            {
                producto.ActivoParaVenta = false;
            }

            _cambiosRealizados = true;
            MessageBox.Show($"✅ {seleccionados.Count} productos desactivados del POS",
                "Productos Desactivados", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (_cambiosRealizados)
            {
                var resultado = MessageBox.Show(
                    "¿Salir sin guardar los cambios realizados?",
                    "Cambios Sin Guardar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No) return;
            }

            DialogResult = false;
            Close();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_cambiosRealizados)
                {
                    MessageBox.Show("No hay cambios para guardar.", "Sin Cambios",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var productosAActualizar = _productos.Where(p => p.HaCambiado).ToList();

                if (!productosAActualizar.Any())
                {
                    MessageBox.Show("No hay cambios para guardar.", "Sin Cambios",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtEstadoHeader.Text = "Guardando cambios...";

                foreach (var productoPrecio in productosAActualizar)
                {
                    var producto = await _context.RawMaterials.FindAsync(productoPrecio.Id);
                    if (producto != null)
                    {
                        productoPrecio.ActualizarProductoOriginal(producto);
                    }
                }

                await _context.SaveChangesAsync();

                MessageBox.Show($"✅ Cambios guardados exitosamente!\n\n" +
                    $"Productos actualizados: {productosAActualizar.Count}",
                    "Guardado Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error al Guardar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoHeader.Text = "❌ Error al guardar";
            }
        }

        #endregion
    }

    #region Clase ProductoPrecio

    public class ProductoPrecio : INotifyPropertyChanged
    {
        private decimal _precioVenta;
        private bool _activoParaVenta;
        private bool _haCambiado;

        public int Id { get; set; }
        public string NombreProducto { get; set; }
        public string Categoria { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockTotal { get; set; }
        public decimal PrecioConIVA { get; set; }
        public decimal MargenObjetivo { get; set; }

        public decimal PrecioVenta
        {
            get => _precioVenta;
            set
            {
                if (_precioVenta != value)
                {
                    _precioVenta = value;
                    _haCambiado = true;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PrecioVentaString));
                    OnPropertyChanged(nameof(MargenCalculado));
                    OnPropertyChanged(nameof(GananciaUnitaria));
                    OnPropertyChanged(nameof(EstadoPrecio));
                }
            }
        }

        public bool ActivoParaVenta
        {
            get => _activoParaVenta;
            set
            {
                if (_activoParaVenta != value)
                {
                    _activoParaVenta = value;
                    _haCambiado = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool HaCambiado => _haCambiado;

        public string PrecioVentaString
        {
            get => PrecioVenta > 0 ? PrecioVenta.ToString("C2") : "";
            set
            {
                if (decimal.TryParse(value.Replace("$", "").Replace(",", ""), out decimal precio))
                {
                    PrecioVenta = precio;
                }
            }
        }

        public decimal MargenCalculado
        {
            get
            {
                if (PrecioVenta <= 0 || PrecioConIVA <= 0) return 0;
                return ((PrecioVenta - PrecioConIVA) / PrecioVenta) * 100;
            }
        }

        public decimal GananciaUnitaria => Math.Max(0, PrecioVenta - PrecioConIVA);

        public bool EsProductoAGranel
        {
            get
            {
                var unidadesGranel = new[] { "kg", "kilogramo", "gr", "gramo", "gramos",
                    "lt", "litro", "litros", "ml", "mililitro", "mililitros" };
                return unidadesGranel.Any(u => UnidadMedida.ToLower().Contains(u));
            }
        }

        public string TipoProducto => EsProductoAGranel ? "🔸 Granel" : "🔹 Pieza";

        public string EstadoPrecio
        {
            get
            {
                if (PrecioVenta <= 0) return "⏳ Pendiente";
                if (MargenCalculado < 15) return "⚠️ Margen Bajo";
                return "✅ Configurado";
            }
        }

        public ProductoPrecio(RawMaterial producto)
        {
            Id = producto.Id;
            NombreProducto = producto.NombreArticulo;
            Categoria = producto.Categoria;
            UnidadMedida = producto.UnidadMedida;
            StockTotal = producto.StockTotal;
            PrecioConIVA = producto.PrecioConIVA;
            MargenObjetivo = producto.MargenObjetivo;
            _precioVenta = producto.PrecioVenta;
            _activoParaVenta = producto.ActivoParaVenta;
            _haCambiado = false;
        }

        public void AplicarMargen(decimal margenPorcentaje)
        {
            if (PrecioConIVA > 0)
            {
                PrecioVenta = PrecioConIVA * (1 + (margenPorcentaje / 100));
            }
        }

        public void ActualizarProductoOriginal(RawMaterial producto)
        {
            producto.PrecioVenta = PrecioVenta;
            producto.PrecioVentaConIVA = PrecioVenta * 1.16m; // Asumiendo 16% IVA
            producto.ActivoParaVenta = ActivoParaVenta;
            producto.MargenObjetivo = MargenCalculado;
            producto.FechaActualizacion = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}