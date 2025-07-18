using System;
using System.Windows;

namespace costbenefi
{
    /// <summary>
    /// Ventana de información del desarrollador
    /// Autor: Esaú Villagrán - esau.villagran47@gmail.com
    /// </summary>
    public partial class MiInformacionWindow : Window
    {
        public MiInformacionWindow()
        {
            InitializeComponent();
            ConfigurarVentana();
        }

        private void ConfigurarVentana()
        {
            try
            {
                // Centrar respecto al owner
                if (Owner != null)
                {
                    Left = Owner.Left + (Owner.Width - Width) / 2;
                    Top = Owner.Top + (Owner.Height - Height) / 2;
                }

                ShowInTaskbar = false;

                // ESC para cerrar
                KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Escape)
                        Close();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando ventana: {ex.Message}");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}