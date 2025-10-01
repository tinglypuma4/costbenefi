using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Services;
using costbenefi.Views;
using System.Linq;
using costbenefi.Models;

namespace costbenefi
{
    public partial class App : Application
    {
        public App()
        {
            // 🔧 SOLUCIÓN: Configurar ShutdownMode explícitamente
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Configurar manejo de excepciones
            DispatcherUnhandledException += Application_DispatcherUnhandledException;

            // 🔧 SOLUCIÓN: Prevenir cierre por SessionEnding
            this.SessionEnding += (s, e) => e.Cancel = true;

            System.Diagnostics.Debug.WriteLine("✅ App inicializada - Método de reinicio simple");
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Iniciando aplicación...");

                await InicializarBaseDatos();

                // ===== 🔧 VERIFICAR PRIMERA VEZ (IGNORANDO USUARIOS SOPORTE) =====
                bool esPrimeraVez = await EsPrimeraVezDelSistema();

                if (esPrimeraVez)
                {
                    System.Diagnostics.Debug.WriteLine("🎉 Primera vez - Mostrando configuración inicial...");
                    MostrarConfiguracionInicial();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔐 Mostrando ventana de login...");
                    MostrarLogin();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al iniciar aplicación:\n\n{ex.Message}",
                    "Error de Inicio", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private async Task InicializarBaseDatos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📂 Inicializando base de datos...");

                using var context = new AppDbContext();
                await context.Database.EnsureCreatedAsync();

                System.Diagnostics.Debug.WriteLine("✅ Base de datos inicializada correctamente");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar base de datos: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si es la primera vez del sistema (ignorando usuarios soporte)
        /// </summary>
        private async Task<bool> EsPrimeraVezDelSistema()
        {
            try
            {
                using var context = new AppDbContext();

                // ===== 🔧 SOLO VERIFICAR USUARIOS REALES (NO SOPORTE) =====
                // Los usuarios soporte tienen ID = -1 y no existen en la BD
                // Solo contar usuarios reales con ID > 0
                var existeAlgunDuenoReal = await context.Users
                    .Where(u => u.Id > 0) // Solo usuarios reales de la BD
                    .AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);

                System.Diagnostics.Debug.WriteLine($"🔍 ¿Existe usuario Dueño REAL? {existeAlgunDuenoReal}");

                return !existeAlgunDuenoReal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error verificando primera vez: {ex.Message}");
                // En caso de error, asumir que es primera vez para seguridad
                return true;
            }
        }

        private void MostrarConfiguracionInicial()
        {
            var setupWindow = new FirstTimeSetupWindow();

            if (setupWindow.ShowDialog() == true)
            {
                // Setup exitoso, mostrar login
                MostrarLogin();
            }
            else
            {
                // Usuario canceló setup
                Application.Current.Shutdown();
            }
        }

        private void MostrarLogin()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔐 Creando y mostrando LoginWindow");

                var loginWindow = new LoginWindow();

                if (loginWindow.ShowDialog() == true)
                {
                    // Login exitoso, mostrar ventana principal
                    MostrarVentanaPrincipal();
                }
                else
                {
                    // Usuario canceló login o falló
                    System.Diagnostics.Debug.WriteLine("❌ Login cancelado o falló - Cerrando aplicación");
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en MostrarLogin: {ex}");
                MessageBox.Show($"Error al mostrar login:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void MostrarVentanaPrincipal()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("✅ Login exitoso - Creando ventana principal");

                var mainWindow = new MainWindow();
                PermisosSimples.ConfigurarInterfazPorRol(mainWindow);  // ← Solo agregar esta línea
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                System.Diagnostics.Debug.WriteLine("🎉 Aplicación iniciada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana principal:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"❌ Excepción no manejada: {e.Exception.Message}");

                MessageBox.Show($"Error inesperado en la aplicación:\n\n{e.Exception.Message}",
                    "Error de Aplicación", MessageBoxButton.OK, MessageBoxImage.Error);

                e.Handled = true; // Marcar como manejado para evitar cierre
            }
            catch
            {
                // Si falla el manejo de errores, permitir que la aplicación se cierre
                e.Handled = false;
            }
        }
    }
}