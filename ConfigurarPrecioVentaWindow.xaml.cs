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
        private bool _autoGuardadoActivo = true; // ✅ NUEVO: Control de auto-guardado
        private System.Timers.Timer _timerAutoGuardado; // ✅ NUEVO: Timer para debouncing

        // Referencias a controles
        private ComboBox CmbFiltroProductos;
        private DataGrid DgProductos;
        private TextBlock TxtEstadoHeader;
        private TextBox TxtMargenMasivo;
        private TextBlock TxtContadores;
        private ComboBox CmbTipoMargen;
        private TextBlock TxtExplicacionMargen;
        private TextBlock TxtEstadoAutoGuardado; // ✅ NUEVO: Indicador de auto-guardado

        public ConfigurarPrecioVentaWindow(AppDbContext context)
        {
            _context = context;
            _productos = new ObservableCollection<ProductoPrecio>();

            // ✅ NUEVO: Configurar timer para auto-guardado (debouncing de 2 segundos)
            _timerAutoGuardado = new System.Timers.Timer(2000);
            _timerAutoGuardado.Elapsed += TimerAutoGuardado_Elapsed;
            _timerAutoGuardado.AutoReset = false;

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
            DgProductos.Columns.Add(CreateTextColumn("Margen %", "MargenMostradoCompleto", 120, FontWeights.Bold, "#10B981", TextAlignment.Right)); // ✅ CAMBIADO
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
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ✅ FILA 1: Configuración de margen
            var margenGrid = new Grid();
            margenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            margenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            margenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            margenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            margenGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblMargen = new TextBlock
            {
                Text = "Aplicar margen:",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0)
            };

            TxtMargenMasivo = new TextBox
            {
                Width = 60,
                Height = 30,
                Text = "30",
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            TxtMargenMasivo.TextChanged += TxtMargenMasivo_TextChanged; // ✅ NUEVO

            var lblPorcentaje = new TextBlock
            {
                Text = "%",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0)
            };

            CmbTipoMargen = new ComboBox
            {
                Width = 130,
                Height = 30,
                Margin = new Thickness(0, 0, 15, 0)
            };
            CmbTipoMargen.Items.Add("sobre COSTO");
            CmbTipoMargen.Items.Add("sobre VENTA");
            CmbTipoMargen.SelectedIndex = 0; // Por defecto sobre costo
            CmbTipoMargen.SelectionChanged += CmbTipoMargen_SelectionChanged; // ✅ NUEVO

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

            Grid.SetColumn(lblMargen, 0);
            Grid.SetColumn(TxtMargenMasivo, 1);
            Grid.SetColumn(lblPorcentaje, 2);
            Grid.SetColumn(CmbTipoMargen, 3);
            Grid.SetColumn(btnAplicarMargen, 4);

            margenGrid.Children.Add(lblMargen);
            margenGrid.Children.Add(TxtMargenMasivo);
            margenGrid.Children.Add(lblPorcentaje);
            margenGrid.Children.Add(CmbTipoMargen);
            margenGrid.Children.Add(btnAplicarMargen);

            // ✅ FILA 2: Explicación y acciones
            var accionesGrid = new Grid();
            accionesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            accionesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TxtExplicacionMargen = new TextBlock
            {
                Text = "💡 Margen sobre COSTO: Costo $100 + 30% = Precio $130 (margen 30%)",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            var accionesStack = new StackPanel { Orientation = Orientation.Horizontal };
            var btnActivarTodos = new Button
            {
                Content = "✅ Activar POS",
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
                Content = "❌ Desactivar POS",
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(15, 0, 15, 0)
            };
            btnDesactivarTodos.Click += BtnDesactivarTodos_Click;

            accionesStack.Children.Add(btnActivarTodos);
            accionesStack.Children.Add(btnDesactivarTodos);

            Grid.SetColumn(TxtExplicacionMargen, 0);
            Grid.SetColumn(accionesStack, 1);

            accionesGrid.Children.Add(TxtExplicacionMargen);
            accionesGrid.Children.Add(accionesStack);

            Grid.SetRow(margenGrid, 0);
            Grid.SetRow(accionesGrid, 1);

            grid.Children.Add(margenGrid);
            grid.Children.Add(accionesGrid);

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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ✅ NUEVO: Indicador de auto-guardado
            var estadoStack = new StackPanel { Orientation = Orientation.Horizontal };

            var estadoText = new TextBlock
            {
                Text = "🔄 Auto-guardado activado",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Margin = new Thickness(0, 0, 15, 0)
            };

            TxtEstadoAutoGuardado = new TextBlock
            {
                Text = "✅ Todos los cambios guardados",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };

            estadoStack.Children.Add(estadoText);
            estadoStack.Children.Add(TxtEstadoAutoGuardado);

            // ✅ MODIFICADO: Toggle para auto-guardado
            var toggleStack = new StackPanel { Orientation = Orientation.Horizontal };

            var lblAutoGuardado = new TextBlock
            {
                Text = "Auto-guardado:",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var chkAutoGuardado = new CheckBox
            {
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 20, 0)
            };
            chkAutoGuardado.Checked += (s, e) => { _autoGuardadoActivo = true; ActualizarEstadoAutoGuardado("🔄 Auto-guardado activado"); };
            chkAutoGuardado.Unchecked += (s, e) => { _autoGuardadoActivo = false; ActualizarEstadoAutoGuardado("⏸️ Auto-guardado desactivado"); };

            toggleStack.Children.Add(lblAutoGuardado);
            toggleStack.Children.Add(chkAutoGuardado);

            var botonesStack = new StackPanel { Orientation = Orientation.Horizontal };

            // ✅ MODIFICADO: Botón de guardado manual (por si acaso)
            var btnGuardarManual = new Button
            {
                Content = "💾 Guardar Manual",
                Width = 120,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnGuardarManual.Click += BtnGuardarManual_Click;

            var btnCerrar = new Button
            {
                Content = "✅ Cerrar",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                FontWeight = FontWeights.Bold
            };
            btnCerrar.Click += BtnCerrar_Click;

            botonesStack.Children.Add(btnGuardarManual);
            botonesStack.Children.Add(btnCerrar);

            Grid.SetColumn(estadoStack, 0);
            Grid.SetColumn(toggleStack, 1);
            Grid.SetColumn(botonesStack, 2);

            grid.Children.Add(estadoStack);
            grid.Children.Add(toggleStack);
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
                Binding = new Binding(binding) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }, // ✅ CORREGIDO
                Width = width
            };

            var style = new Style(typeof(CheckBox));
            style.Setters.Add(new Setter(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center));
            column.ElementStyle = style;

            return column;
        }

        #endregion

        #region Auto-Guardado

        // ✅ NUEVO: Método para programar auto-guardado con debouncing
        private void ProgramarAutoGuardado()
        {
            if (!_autoGuardadoActivo) return;

            _timerAutoGuardado.Stop();
            _timerAutoGuardado.Start();

            ActualizarEstadoAutoGuardado("⏳ Guardando en 2 segundos...");
        }

        // ✅ NUEVO: Event handler del timer
        private async void TimerAutoGuardado_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await GuardarCambiosAutomaticos();
            });
        }

        // ✅ NUEVO: Método para guardar cambios automáticamente
        private async Task GuardarCambiosAutomaticos()
        {
            try
            {
                ActualizarEstadoAutoGuardado("💾 Guardando...");

                var productosAActualizar = _productos.Where(p => p.HaCambiado).ToList();

                if (!productosAActualizar.Any())
                {
                    ActualizarEstadoAutoGuardado("✅ Todos los cambios guardados");
                    return;
                }

                int cambiosGuardados = 0;

                foreach (var productoPrecio in productosAActualizar)
                {
                    var producto = await _context.RawMaterials.FindAsync(productoPrecio.Id);
                    if (producto != null)
                    {
                        bool preciosCambiaron = Math.Abs(producto.PrecioVenta - productoPrecio.PrecioVenta) > 0.01m;
                        bool estadoCambio = producto.ActivoParaVenta != productoPrecio.ActivoParaVenta;

                        if (preciosCambiaron || estadoCambio)
                        {
                            productoPrecio.ActualizarProductoOriginal(producto);
                            _context.Entry(producto).State = EntityState.Modified;

                            if (preciosCambiaron)
                            {
                                _context.Entry(producto).Property(p => p.PrecioVenta).IsModified = true;
                                _context.Entry(producto).Property(p => p.PrecioVentaConIVA).IsModified = true;
                                _context.Entry(producto).Property(p => p.MargenObjetivo).IsModified = true;
                            }
                            if (estadoCambio)
                            {
                                _context.Entry(producto).Property(p => p.ActivoParaVenta).IsModified = true;
                            }
                            _context.Entry(producto).Property(p => p.FechaActualizacion).IsModified = true;

                            cambiosGuardados++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // ✅ Marcar productos como guardados
                foreach (var producto in productosAActualizar)
                {
                    producto.MarcarComoGuardado();
                }

                ActualizarEstadoAutoGuardado($"✅ {cambiosGuardados} cambios guardados");
                TxtEstadoHeader.Text = $"✅ Auto-guardado: {cambiosGuardados} cambios aplicados";
            }
            catch (Exception ex)
            {
                ActualizarEstadoAutoGuardado($"❌ Error: {ex.Message}");
                TxtEstadoHeader.Text = "❌ Error en auto-guardado";
            }
        }

        // ✅ NUEVO: Actualizar estado del auto-guardado en UI
        private void ActualizarEstadoAutoGuardado(string mensaje)
        {
            if (TxtEstadoAutoGuardado != null)
            {
                TxtEstadoAutoGuardado.Text = mensaje;

                // Cambiar color según el estado
                if (mensaje.Contains("✅"))
                    TxtEstadoAutoGuardado.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                else if (mensaje.Contains("⏳") || mensaje.Contains("💾"))
                    TxtEstadoAutoGuardado.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                else if (mensaje.Contains("❌"))
                    TxtEstadoAutoGuardado.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                else
                    TxtEstadoAutoGuardado.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
            }
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

                _productosOriginales = productos.Select(p =>
                {
                    var productoPrecio = new ProductoPrecio(p);
                    // ✅ NUEVO: Conectar evento de cambio para auto-guardado
                    productoPrecio.CambioRealizado += (producto) => ProgramarAutoGuardado();
                    return productoPrecio;
                }).ToList();

                AplicarFiltro();
                ActualizarContadores();
                ActualizarExplicacionMargen();

                TxtEstadoHeader.Text = $"✅ {productos.Count} productos cargados";
                ActualizarEstadoAutoGuardado("✅ Todos los cambios guardados");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoHeader.Text = "❌ Error al cargar";
                ActualizarEstadoAutoGuardado("❌ Error al cargar");
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
                    productosFiltrados = _productosOriginales.Where(p => p.MargenCalculadoSobreVenta < 20 && p.PrecioVenta > 0);
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

        // ✅ NUEVO: Actualizar explicación según tipo de margen
        private void ActualizarExplicacionMargen()
        {
            if (TxtExplicacionMargen == null || TxtMargenMasivo == null || CmbTipoMargen == null) return;

            if (!decimal.TryParse(TxtMargenMasivo.Text, out decimal margen)) margen = 30;

            var esSobreCosto = CmbTipoMargen.SelectedIndex == 0;
            decimal costoEjemplo = 100;
            decimal precioCalculado, margenMostrado;

            if (esSobreCosto)
            {
                // Margen sobre costo
                precioCalculado = costoEjemplo * (1 + margen / 100);
                margenMostrado = margen;
                TxtExplicacionMargen.Text = $"💡 Margen sobre COSTO: Costo ${costoEjemplo:F0} + {margen}% = Precio ${precioCalculado:F0} (margen {margenMostrado:F1}%)";
            }
            else
            {
                // Margen sobre venta
                if (margen < 100)
                {
                    precioCalculado = costoEjemplo / (1 - margen / 100);
                    var margenSobreCosto = ((precioCalculado - costoEjemplo) / costoEjemplo) * 100;
                    TxtExplicacionMargen.Text = $"💡 Margen sobre VENTA: Costo ${costoEjemplo:F0} → Precio ${precioCalculado:F0} (margen {margen}% de venta = {margenSobreCosto:F1}% sobre costo)";
                }
                else
                {
                    TxtExplicacionMargen.Text = "⚠️ Margen sobre venta debe ser menor a 100%";
                }
            }
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

        // ✅ NUEVO: Event handler para cambio de tipo de margen
        private void CmbTipoMargen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarExplicacionMargen();
        }

        // ✅ NUEVO: Event handler para cambio de texto del margen
        private void TxtMargenMasivo_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarExplicacionMargen();
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

            var tipoMargen = CmbTipoMargen.SelectedIndex == 0 ? "sobre costo" : "sobre venta";
            var resultado = MessageBox.Show(
                $"¿Calcular precios con {margen}% de margen {tipoMargen} para {productosValidos.Count} productos?\n\n" +
                "Esto sobrescribirá los precios existentes.",
                "Confirmar Cálculo Masivo", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                var tipoMargenEnum = CmbTipoMargen.SelectedIndex == 0 ? TipoMargen.SobreCosto : TipoMargen.SobreVenta;
                foreach (var producto in productosValidos)
                {
                    producto.AplicarMargen(margen, tipoMargenEnum);
                }

                _cambiosRealizados = true;
                ProgramarAutoGuardado(); // ✅ NUEVO: Auto-guardado

                MessageBox.Show($"✅ Precios calculados para {productosValidos.Count} productos con margen {tipoMargen}\n\n🔄 Los cambios se guardarán automáticamente",
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

            var tipoMargenEnum = CmbTipoMargen.SelectedIndex == 0 ? TipoMargen.SobreCosto : TipoMargen.SobreVenta;
            var tipoMargenTexto = CmbTipoMargen.SelectedIndex == 0 ? "sobre costo" : "sobre venta";

            foreach (var producto in seleccionados.Where(p => p.PrecioConIVA > 0))
            {
                producto.AplicarMargen(margen, tipoMargenEnum);
            }

            _cambiosRealizados = true;
            ProgramarAutoGuardado(); // ✅ NUEVO: Auto-guardado

            MessageBox.Show($"✅ Margen {tipoMargenTexto} aplicado a {seleccionados.Count} productos\n\n🔄 Los cambios se guardarán automáticamente",
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
            ProgramarAutoGuardado(); // ✅ NUEVO: Auto-guardado

            MessageBox.Show($"✅ {seleccionados.Count} productos activados para POS\n\n🔄 Los cambios se guardarán automáticamente",
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
            ProgramarAutoGuardado(); // ✅ NUEVO: Auto-guardado

            MessageBox.Show($"✅ {seleccionados.Count} productos desactivados del POS\n\n🔄 Los cambios se guardarán automáticamente",
                "Productos Desactivados", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ✅ NUEVO: Botón de guardado manual (backup)
        private async void BtnGuardarManual_Click(object sender, RoutedEventArgs e)
        {
            await GuardarCambiosAutomaticos();
            MessageBox.Show("✅ Guardado manual completado", "Guardado",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ✅ MODIFICADO: Botón de cerrar (reemplaza cancelar)
        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            // Verificar si hay cambios pendientes
            var cambiosPendientes = _productos.Any(p => p.HaCambiado);

            if (cambiosPendientes && _autoGuardadoActivo)
            {
                var resultado = MessageBox.Show(
                    "Hay cambios pendientes que se guardarán automáticamente.\n\n¿Desea cerrar ahora?",
                    "Cambios Pendientes", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No) return;

                // Forzar guardado antes de cerrar
                _ = Task.Run(async () => await GuardarCambiosAutomaticos());
            }
            else if (cambiosPendientes && !_autoGuardadoActivo)
            {
                var resultado = MessageBox.Show(
                    "Hay cambios no guardados (auto-guardado desactivado).\n\n¿Desea salir sin guardar?",
                    "Cambios Sin Guardar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.No) return;
            }

            // ✅ NUEVO: Cleanup del timer
            _timerAutoGuardado?.Stop();
            _timerAutoGuardado?.Dispose();

            DialogResult = true;
            Close();
        }

        // ✅ MANTENER: Método de guardado original para compatibilidad
        

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCerrar_Click(sender, e);
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

                // ✅ CORREGIDO: Buscar productos con cambios de manera más específica
                var productosAActualizar = _productos.Where(p => p.HaCambiado).ToList();

                if (!productosAActualizar.Any())
                {
                    MessageBox.Show("No hay cambios para guardar.", "Sin Cambios",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtEstadoHeader.Text = "Guardando cambios...";

                int productosActualizados = 0;
                int preciosActualizados = 0;
                int estadosActualizados = 0;

                foreach (var productoPrecio in productosAActualizar)
                {
                    var producto = await _context.RawMaterials.FindAsync(productoPrecio.Id);
                    if (producto != null)
                    {
                        // ✅ VERIFICAR QUÉ CAMBIÓ ESPECÍFICAMENTE
                        bool preciosCambiaron = Math.Abs(producto.PrecioVenta - productoPrecio.PrecioVenta) > 0.01m;
                        bool estadoCambio = producto.ActivoParaVenta != productoPrecio.ActivoParaVenta;

                        // ✅ ACTUALIZAR SOLO SI HAY CAMBIOS REALES
                        if (preciosCambiaron || estadoCambio)
                        {
                            productoPrecio.ActualizarProductoOriginal(producto);
                            productosActualizados++;

                            if (preciosCambiaron) preciosActualizados++;
                            if (estadoCambio) estadosActualizados++;

                            // ✅ FORZAR TRACKING DE CAMBIOS EXPLÍCITAMENTE
                            _context.Entry(producto).State = EntityState.Modified;

                            // ✅ EXTRA: Marcar propiedades específicas como modificadas
                            if (preciosCambiaron)
                            {
                                _context.Entry(producto).Property(p => p.PrecioVenta).IsModified = true;
                                _context.Entry(producto).Property(p => p.PrecioVentaConIVA).IsModified = true;
                                _context.Entry(producto).Property(p => p.MargenObjetivo).IsModified = true;
                            }
                            if (estadoCambio)
                            {
                                _context.Entry(producto).Property(p => p.ActivoParaVenta).IsModified = true;
                            }
                            _context.Entry(producto).Property(p => p.FechaActualizacion).IsModified = true;
                        }
                    }
                }

                // ✅ GUARDAR CAMBIOS CON CONFIRMACIÓN
                var cambiosGuardados = await _context.SaveChangesAsync();

                MessageBox.Show($"✅ Cambios guardados exitosamente!\n\n" +
                    $"📦 Productos actualizados: {productosActualizados}\n" +
                    $"💰 Precios modificados: {preciosActualizados}\n" +
                    $"🔄 Estados POS cambiados: {estadosActualizados}\n" +
                    $"💾 Registros en BD: {cambiosGuardados}",
                    "Guardado Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}\n\nDetalles: {ex.InnerException?.Message}",
                    "Error al Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEstadoHeader.Text = "❌ Error al guardar";
            }
        }

        #endregion
    }

    #region Enumeraciones y Clases Auxiliares

    public enum TipoMargen
    {
        SobreCosto,   // Margen sobre el costo (tradicional)
        SobreVenta    // Margen sobre el precio de venta
    }

    #endregion

    #region Clase ProductoPrecio CORREGIDA

    public class ProductoPrecio : INotifyPropertyChanged
    {
        private decimal _precioVenta;
        private bool _activoParaVenta;
        private bool _haCambiado;

        // ✅ NUEVOS: Valores originales para detectar cambios reales
        private decimal _precioVentaOriginal;
        private bool _activoParaVentaOriginal;

        // ✅ NUEVO: Evento para notificar cambios al contenedor
        public event Action<ProductoPrecio> CambioRealizado;

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
                    OnPropertyChanged(nameof(MargenCalculadoSobreCosto));
                    OnPropertyChanged(nameof(MargenCalculadoSobreVenta));
                    OnPropertyChanged(nameof(MargenMostrado));
                    OnPropertyChanged(nameof(MargenMostradoCompleto));
                    OnPropertyChanged(nameof(GananciaUnitaria));
                    OnPropertyChanged(nameof(EstadoPrecio));

                    // ✅ NUEVO: Notificar cambio para auto-guardado
                    CambioRealizado?.Invoke(this);
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

                    // ✅ NUEVO: Notificar cambio para auto-guardado
                    CambioRealizado?.Invoke(this);
                }
            }
        }

        // ✅ CORREGIDO: HaCambiado verifica cambios reales vs valores originales
        public bool HaCambiado =>
            Math.Abs(_precioVenta - _precioVentaOriginal) > 0.01m ||
            _activoParaVenta != _activoParaVentaOriginal;

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

        // ✅ Margen sobre COSTO (tradicional)
        public decimal MargenCalculadoSobreCosto
        {
            get
            {
                if (PrecioVenta <= 0 || PrecioConIVA <= 0) return 0;
                return ((PrecioVenta - PrecioConIVA) / PrecioConIVA) * 100;
            }
        }

        // ✅ Margen sobre VENTA 
        public decimal MargenCalculadoSobreVenta
        {
            get
            {
                if (PrecioVenta <= 0 || PrecioConIVA <= 0) return 0;
                return ((PrecioVenta - PrecioConIVA) / PrecioVenta) * 100;
            }
        }

        // ✅ NUEVO: Margen completo para mostrar en la UI
        public string MargenMostradoCompleto
        {
            get
            {
                if (PrecioVenta <= 0 || PrecioConIVA <= 0) return "N/A";
                var margenCosto = MargenCalculadoSobreCosto;
                var margenVenta = MargenCalculadoSobreVenta;
                return $"{margenCosto:F1}% / {margenVenta:F1}%";
            }
        }

        // ✅ Margen principal que se muestra (sobre costo por defecto)
        public decimal MargenMostrado => MargenCalculadoSobreCosto;

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
                if (MargenCalculadoSobreVenta < 15) return "⚠️ Margen Bajo";
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

            // ✅ Guardar valores originales
            _precioVenta = producto.PrecioVenta;
            _activoParaVenta = producto.ActivoParaVenta;
            _precioVentaOriginal = producto.PrecioVenta;
            _activoParaVentaOriginal = producto.ActivoParaVenta;

            _haCambiado = false;
        }

        // ✅ CORREGIDO: Método para aplicar margen con tipo específico
        public void AplicarMargen(decimal margenPorcentaje, TipoMargen tipoMargen = TipoMargen.SobreCosto)
        {
            if (PrecioConIVA > 0)
            {
                switch (tipoMargen)
                {
                    case TipoMargen.SobreCosto:
                        // Margen sobre costo: Precio = Costo × (1 + margen/100)
                        PrecioVenta = Math.Round(PrecioConIVA * (1 + (margenPorcentaje / 100)), 2);
                        break;

                    case TipoMargen.SobreVenta:
                        // Margen sobre venta: Precio = Costo / (1 - margen/100)
                        if (margenPorcentaje < 100) // Evitar división por cero
                        {
                            PrecioVenta = Math.Round(PrecioConIVA / (1 - (margenPorcentaje / 100)), 2);
                        }
                        break;
                }
            }
        }

        // ✅ NUEVO: Marcar como guardado (resetear valores originales)
        public void MarcarComoGuardado()
        {
            _precioVentaOriginal = _precioVenta;
            _activoParaVentaOriginal = _activoParaVenta;
            _haCambiado = false;
            OnPropertyChanged(nameof(HaCambiado));
        }

        public void ActualizarProductoOriginal(RawMaterial producto)
        {
            producto.PrecioVenta = PrecioVenta;
            producto.PrecioVentaConIVA = Math.Round(PrecioVenta * 1.16m, 2); // 16% IVA
            producto.ActivoParaVenta = ActivoParaVenta;
            producto.MargenObjetivo = MargenCalculadoSobreCosto; // ✅ CORREGIDO: Usar margen sobre costo como objetivo
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