using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace costbenefi.Views
{
    public partial class IngresarCantidadWindow : Window
    {
        // ✅ PROPIEDADES PÚBLICAS QUE NECESITA MAINWINDOW
        public int CantidadIngresada { get; private set; } = 0;
        public bool SeConfirmo { get; private set; } = false;

        private readonly string _nombreProducto;
        private TextBox _txtCantidad;

        // ✅ CONSTRUCTOR QUE ACEPTA 1 PARÁMETRO (nombreProducto)
        public IngresarCantidadWindow(string nombreProducto)
        {
            _nombreProducto = nombreProducto ?? "Producto desconocido";
            InitializeComponent();
            CrearInterfaz();
        }

        private void CrearInterfaz()
        {
            // Configuración de la ventana
            this.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            this.ShowInTaskbar = false;
            this.Topmost = true;

            // Obtener el Grid del XAML
            var mainGrid = this.Content as Grid;
            mainGrid.Children.Clear();

            // Panel principal
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(30),
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Título
            var titulo = new TextBlock
            {
                Text = "➕ Ingresar Cantidad Adicional",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(titulo);

            // Nombre del producto
            var lblProducto = new TextBlock
            {
                Text = $"📦 {_nombreProducto}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300
            };
            mainPanel.Children.Add(lblProducto);

            // Pregunta
            var pregunta = new TextBlock
            {
                Text = "¿Cuántas unidades más desea agregar?",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
            mainPanel.Children.Add(pregunta);

            // Contenedor del TextBox
            var textBoxContainer = new Border
            {
                Width = 120,
                Height = 40,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 25),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _txtCantidad = new TextBox
            {
                Text = "1",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(5),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                MaxLength = 3 // Máximo 999 unidades
            };

            // Validación solo números
            _txtCantidad.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsNumeric(e.Text);
            };

            // Seleccionar todo al obtener foco
            _txtCantidad.GotFocus += (s, e) => _txtCantidad.SelectAll();

            textBoxContainer.Child = _txtCantidad;
            mainPanel.Children.Add(textBoxContainer);

            // Panel de botones
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnCancelar = new Button
            {
                Content = "❌ Cancelar",
                Width = 100,
                Height = 35,
                Margin = new Thickness(10, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Foreground = Brushes.White,
                FontSize = 12,
                BorderThickness = new Thickness(0)
            };
            btnCancelar.Click += (s, e) =>
            {
                SeConfirmo = false;
                DialogResult = false;
                Close();
            };

            var btnAceptar = new Button
            {
                Content = "✅ Agregar",
                Width = 100,
                Height = 35,
                Margin = new Thickness(10, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(5, 150, 105)),
                Foreground = Brushes.White,
                FontSize = 12,
                BorderThickness = new Thickness(0),
                IsDefault = true
            };
            btnAceptar.Click += (s, e) => ProcesarCantidad();

            buttonsPanel.Children.Add(btnCancelar);
            buttonsPanel.Children.Add(btnAceptar);
            mainPanel.Children.Add(buttonsPanel);

            // Agregar panel principal al Grid
            mainGrid.Children.Add(mainPanel);

            // Eventos de teclado
            this.KeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        ProcesarCantidad();
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        SeConfirmo = false;
                        DialogResult = false;
                        Close();
                        e.Handled = true;
                        break;
                }
            };

            // Foco inicial
            this.Loaded += (s, e) =>
            {
                _txtCantidad.Focus();
                _txtCantidad.SelectAll();
            };
        }

        private void ProcesarCantidad()
        {
            if (int.TryParse(_txtCantidad.Text, out int cantidad) && cantidad > 0)
            {
                CantidadIngresada = cantidad;
                SeConfirmo = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor ingrese un número entero mayor a 0.",
                               "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                _txtCantidad.Focus();
                _txtCantidad.SelectAll();
            }
        }

        private static bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, @"^[0-9]+$");
        }
    }
}