using System.Windows;
using costbenefi.Data;

namespace costbenefi.Views
{
    public partial class ReporteCorteCajaWindow : Window
    {
        public ReporteCorteCajaWindow(AppDbContext context)
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "📊 Reportes de Corte de Caja";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            Content = new System.Windows.Controls.StackPanel
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Children =
                {
                    new System.Windows.Controls.TextBlock
                    {
                        Text = "📊",
                        FontSize = 48,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 0, 0, 20)
                    },
                    new System.Windows.Controls.TextBlock
                    {
                        Text = "Reportes de Corte de Caja",
                        FontSize = 24,
                        FontWeight = System.Windows.FontWeights.Bold,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new System.Windows.Thickness(0, 0, 0, 10)
                    },
                    new System.Windows.Controls.TextBlock
                    {
                        Text = "Esta funcionalidad estará completamente disponible en la próxima versión.",
                        FontSize = 14,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
                    }
                }
            };
        }
    }
}