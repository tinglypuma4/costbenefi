using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Views;
using costbenefi.Services;

namespace costbenefi
{
    public partial class App : Application
    {
        public App()
        {
            // 🔧 SOLUCIÓN: Configurar ShutdownMode explícitamente
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 🛡️ Configurar manejo de excepciones no controladas
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            System.Diagnostics.Debug.WriteLine("🚀 App constructor ejecutado - Exception handlers configurados");

            // 🔧 SOLUCIÓN: Prevenir cierre por SessionEnding
            this.SessionEnding += (s, e) => {
                System.Diagnostics.Debug.WriteLine($"⚠️ SessionEnding detectado: {e.ReasonSessionEnding} - CANCELADO");
                e.Cancel = true; // Cancelar para evitar cierre inesperado
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Iniciando aplicación...");

                // 1. Inicializar base de datos
                await InicializarBaseDatos();

                // 2. Verificar si es primera vez (crear usuario dueño)
                var esConfiguracionInicial = await VerificarConfiguracionInicial();

                if (esConfiguracionInicial)
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Configuración inicial requerida");

                    var setupWindow = new FirstTimeSetupWindow();
                    if (setupWindow.ShowDialog() != true)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Usuario canceló configuración inicial");
                        Shutdown(0);
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Configuración inicial completada");
                }

                // 3. Mostrar login
                System.Diagnostics.Debug.WriteLine("🔐 Mostrando ventana de login...");

                var loginWindow = new LoginWindow();

                if (loginWindow.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Login exitoso - Creando ventana principal");

                    // 4. Login exitoso - mostrar ventana principal
                    var mainWindow = new MainWindow();

                    MainWindow = mainWindow;
                    mainWindow.Show();

                    System.Diagnostics.Debug.WriteLine("🎉 Aplicación iniciada correctamente");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Login cancelado o falló");
                    Shutdown(0);
                    return;
                }

                // ✅ LÍNEA COMENTADA - Esta era parte del problema original
                // base.OnStartup(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error crítico en OnStartup: {ex}");

                MessageBox.Show(
                    $"❌ Error crítico al iniciar la aplicación:\n\n{ex.Message}\n\n" +
                    $"La aplicación se cerrará. Si el problema persiste, contacte al soporte técnico.",
                    "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        private async Task InicializarBaseDatos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📂 Inicializando base de datos...");

                using var context = new AppDbContext();
                await context.Database.EnsureCreatedAsync();

                // Verificar conectividad
                var canConnect = await context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    throw new Exception("No se puede conectar a la base de datos");
                }

                System.Diagnostics.Debug.WriteLine("✅ Base de datos inicializada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en base de datos: {ex.Message}");
                throw new ApplicationException($"Error al inicializar la base de datos: {ex.Message}", ex);
            }
        }

        private async Task<bool> VerificarConfiguracionInicial()
        {
            try
            {
                using var context = new AppDbContext();

                var existeDueno = await context.Users
                    .AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);

                System.Diagnostics.Debug.WriteLine($"🔍 ¿Existe usuario Dueño? {existeDueno}");

                return !existeDueno;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al verificar configuración: {ex.Message}");
                throw new ApplicationException($"Error al verificar la configuración inicial: {ex.Message}", ex);
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Cerrando aplicación...");

                // Cerrar sesión actual si existe
                if (UserService.UsuarioActual != null)
                {
                    System.Diagnostics.Debug.WriteLine($"👤 Cerrando sesión de: {UserService.UsuarioActual.NombreCompleto}");

                    using var context = new AppDbContext();
                    using var userService = new UserService(context);

                    await userService.CerrarSesionAsync("Cierre de aplicación");
                }

                System.Diagnostics.Debug.WriteLine("✅ Aplicación cerrada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error durante cierre: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        // 🛡️ Manejo de excepciones no controladas
        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"💥 Excepción no controlada: {e.Exception}");

                var mensaje = $"❌ ERROR INESPERADO\n\n" +
                             $"Se produjo un error inesperado:\n\n" +
                             $"{e.Exception.Message}\n\n" +
                             $"¿Desea continuar ejecutando la aplicación?";

                var resultado = MessageBox.Show(mensaje, "Error No Controlado",
                                              MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (resultado == MessageBoxResult.Yes)
                {
                    e.Handled = true;
                    System.Diagnostics.Debug.WriteLine("⚠️ Usuario decidió continuar después del error");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("💀 Usuario decidió cerrar la aplicación");
                    Shutdown(2);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💀 Error fatal en manejo de excepciones: {ex}");
                Shutdown(3);
            }
        }

        // 🔧 Método para debugging del estado de la aplicación
        public static string GetDebugInfo()
        {
            try
            {
                var info = $"🔍 DEBUG INFO - {DateTime.Now:HH:mm:ss}\n";
                info += $"{'='}{new string('=', 30)}\n\n";

                // Usuario actual
                if (UserService.UsuarioActual != null)
                {
                    info += $"👤 Usuario: {UserService.UsuarioActual.NombreCompleto}\n";
                    info += $"🎯 Rol: {UserService.UsuarioActual.Rol}\n";

                    if (UserService.SesionActual != null)
                    {
                        info += $"🕐 Sesión: {UserService.SesionActual.DuracionFormateada}\n";
                        info += $"💻 Máquina: {UserService.SesionActual.NombreMaquina}\n";
                    }
                }
                else
                {
                    info += "👤 Usuario: NO LOGUEADO\n";
                }

                // Ventanas
                info += $"\n🪟 Ventanas:\n";
                info += $"   • Principal: {(Current.MainWindow != null ? "✅" : "❌")}\n";
                info += $"   • Total abiertas: {Current.Windows.Count}\n";

                return info;
            }
            catch (Exception ex)
            {
                return $"❌ Error en debug: {ex.Message}";
            }
        }
    }
}