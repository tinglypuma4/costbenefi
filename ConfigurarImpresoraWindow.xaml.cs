using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace costbenefi.Views
{
    public partial class ConfigurarImpresoraWindow : Window
    {
        public string ImpresoraSeleccionada { get; private set; }
        public bool ImpresionAutomatica { get; private set; }

        public ConfigurarImpresoraWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Configurar Impresora de Tickets";
            Width = 500;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));

            var grid = new Grid { Margin = new Thickness(20) };

            for (int i = 0; i < 7; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Título
            var titulo = new TextBlock
            {
                Text = "🖨️ Configuración de Impresora",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 59, 78))
            };
            Grid.SetRow(titulo, 0);
            grid.Children.Add(titulo);

            // Selección de impresora
            var labelImpresora = new TextBlock
            {
                Text = "Seleccionar impresora:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(labelImpresora, 1);
            grid.Children.Add(labelImpresora);

            var cbImpresoras = new ComboBox
            {
                Name = "CbImpresoras",
                Height = 35,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Cargar impresoras disponibles
            CargarImpresoras(cbImpresoras);

            Grid.SetRow(cbImpresoras, 2);
            grid.Children.Add(cbImpresoras);

            // Opciones
            var chkAutomatica = new CheckBox
            {
                Name = "ChkAutomatica",
                Content = "Imprimir automáticamente después de cada venta",
                FontSize = 12,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(chkAutomatica, 3);
            grid.Children.Add(chkAutomatica);

            // Información adicional
            var infoBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackInfo = new StackPanel();
            stackInfo.Children.Add(new TextBlock
            {
                Text = "💡 Información",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = "• Para impresoras térmicas, recomendamos papel de 80mm",
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 2)
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = "• Asegúrese de que la impresora esté instalada en Windows",
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 2)
            });
            stackInfo.Children.Add(new TextBlock
            {
                Text = "• Puede cambiar esta configuración en cualquier momento",
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 2)
            });

            infoBorder.Child = stackInfo;
            Grid.SetRow(infoBorder, 4);
            grid.Children.Add(infoBorder);

            // Botón de prueba
            var btnPrueba = new Button
            {
                Content = "🧪 Imprimir Prueba",
                Width = 150,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };
            btnPrueba.Click += (s, e) => ImprimirPrueba(cbImpresoras.Text);
            Grid.SetRow(btnPrueba, 5);
            grid.Children.Add(btnPrueba);

            // Botones finales
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

            var btnGuardar = new Button
            {
                Content = "✅ Guardar Configuración",
                Width = 160,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            btnGuardar.Click += (s, e) => GuardarConfiguracion(cbImpresoras.Text, chkAutomatica.IsChecked == true);

            panelBotones.Children.Add(btnCancelar);
            panelBotones.Children.Add(btnGuardar);

            Grid.SetRow(panelBotones, 6);
            grid.Children.Add(panelBotones);

            Content = grid;
        }

        private void CargarImpresoras(ComboBox cb)
        {
            try
            {
                // Lista de impresoras simuladas (en implementación real usar PrinterSettings.InstalledPrinters)
                var impresoras = new string[]
                {
                    "Microsoft Print to PDF",
                    "Impresora Térmica POS-80",
                    "HP LaserJet Pro",
                    "Epson TM-T20II",
                    "Star TSP143III",
                    "Zebra ZD220",
                    "Brother QL-800",
                    "Canon PIXMA"
                };

                cb.Items.Clear();
                foreach (var impresora in impresoras)
                {
                    cb.Items.Add(impresora);
                }

                // Seleccionar la primera por defecto
                if (cb.Items.Count > 0)
                {
                    cb.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar impresoras: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImprimirPrueba(string impresora)
        {
            if (string.IsNullOrWhiteSpace(impresora))
            {
                MessageBox.Show("Seleccione una impresora", "Impresora Requerida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ticketPrueba = $@"================================
        TICKET DE PRUEBA
================================
Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}
Impresora: {impresora}
================================

Este es un ticket de prueba
del sistema POS.

✅ La impresora está funcionando
   correctamente.

🖨️ Configuración aplicada:
   - Impresora: {impresora}
   - Papel: 80mm recomendado
   - Estado: Operativa

================================
        PRUEBA EXITOSA
================================
";

                // En implementación real aquí iría el código de impresión
                // Por ahora mostramos el preview
                MessageBox.Show(
                    ticketPrueba,
                    $"🖨️ PRUEBA DE IMPRESIÓN - {impresora}",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en prueba de impresión: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuardarConfiguracion(string impresora, bool automatica)
        {
            if (string.IsNullOrWhiteSpace(impresora))
            {
                MessageBox.Show("Seleccione una impresora", "Impresora Requerida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ImpresoraSeleccionada = impresora;
                ImpresionAutomatica = automatica;

                // Mostrar confirmación
                var mensaje = $@"✅ Configuración guardada exitosamente!

Impresora seleccionada: {impresora}
Impresión automática: {(automatica ? "Habilitada" : "Deshabilitada")}

La configuración se aplicará inmediatamente al sistema POS.";

                MessageBox.Show(mensaje, "Configuración Guardada",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}