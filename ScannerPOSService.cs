using System;
using System.Windows;

namespace costbenefi.Services
{
    public class ProductoEscaneadoEventArgs : EventArgs
    {
        public string CodigoBarras { get; set; }
    }

    public class ScannerPOSService : IDisposable
    {
        public event EventHandler<ProductoEscaneadoEventArgs> ProductoEscaneado;
        public event EventHandler<string> ErrorEscaneo;

        public void MostrarVentanaEscaneo()
        {
            try
            {
                // Crear ventana simple para input
                var inputWindow = new InputWindow("Ingrese código de barras:", "Escáner POS");
                if (inputWindow.ShowDialog() == true)
                {
                    var codigo = inputWindow.InputText;
                    if (!string.IsNullOrWhiteSpace(codigo))
                    {
                        ProductoEscaneado?.Invoke(this, new ProductoEscaneadoEventArgs
                        {
                            CodigoBarras = codigo.Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorEscaneo?.Invoke(this, ex.Message);
            }
        }

        public void Dispose() { }
    }

    // Ventana simple para input
    internal class InputWindow : Window
    {
        public string InputText { get; private set; }

        public InputWindow(string prompt, string title)
        {
            Title = title;
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());

            var label = new System.Windows.Controls.Label
            {
                Content = prompt,
                Margin = new Thickness(10)
            };
            System.Windows.Controls.Grid.SetRow(label, 0);

            var textBox = new System.Windows.Controls.TextBox
            {
                Margin = new Thickness(10),
                Padding = new Thickness(5)
            };
            System.Windows.Controls.Grid.SetRow(textBox, 1);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 75,
                Margin = new Thickness(5),
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                InputText = textBox.Text;
                DialogResult = true;
                Close();
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 75,
                Margin = new Thickness(5),
                IsCancel = true
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            Content = grid;
            textBox.Focus();
        }
    }
}
