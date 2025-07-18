using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using costbenefi.Data;

namespace costbenefi.Views
{
    /// <summary>
    /// Interaction logic for TipoMaterialSelectorWindow.xaml
    /// </summary>
    public partial class TipoMaterialSelectorWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly string _codigoBarras;

        public TipoMaterialSelectorWindow(AppDbContext context, string codigoBarras = "")
        {
            InitializeComponent();
            _context = context;
            _codigoBarras = codigoBarras;
        }

        #region EVENTOS DE HOVER

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                border.BorderThickness = new Thickness(3);
            }
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.White;
                border.BorderThickness = new Thickness(2);
            }
        }

        #endregion

        #region EVENTOS DE CLICK

        private void BtnGranel_Click(object sender, MouseButtonEventArgs e)
        {
            AbrirFormularioGranel();
        }

        private void BtnPiezas_Click(object sender, MouseButtonEventArgs e)
        {
            AbrirFormularioPiezas();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region LÓGICA DE NAVEGACIÓN

        private void AbrirFormularioGranel()
        {
            try
            {
                var granelWindow = new AddMaterialGranelWindow(_context, _codigoBarras);
                if (granelWindow.ShowDialog() == true)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al abrir formulario a granel: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AbrirFormularioPiezas()
        {
            try
            {
                var piezasWindow = new AddMaterialPiezasWindow(_context, _codigoBarras);
                if (piezasWindow.ShowDialog() == true)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al abrir formulario de piezas: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region EVENTOS DE TECLADO

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D1:
                case Key.NumPad1:
                    AbrirFormularioGranel();
                    e.Handled = true;
                    break;
                case Key.D2:
                case Key.NumPad2:
                    AbrirFormularioPiezas();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    DialogResult = false;
                    Close();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        #endregion
    }
}