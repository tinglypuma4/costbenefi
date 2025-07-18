using System.Windows;
using costbenefi.Data;

namespace costbenefi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializar base de datos
            try
            {
                using var context = new AppDbContext();
                context.Database.EnsureCreated();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error al inicializar la base de datos: {ex.Message}",
                              "Error de inicio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}