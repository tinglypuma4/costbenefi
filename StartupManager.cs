using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Views;
using costbenefi.Services;

namespace costbenefi.Core
{
    /// <summary>
    /// Maneja la configuración inicial del sistema y el flujo de inicio de sesión
    /// </summary>
    public static class StartupManager
    {
        /// <summary>
        /// Verifica la configuración del sistema y maneja el flujo de inicio
        /// </summary>
        public static async Task<bool> InitializeSystemAsync()
        {
            try
            {
                // 1. Verificar y crear base de datos
                await EnsureDatabaseCreatedAsync();

                // 2. Verificar si es la primera vez
                var esConfiguracionInicial = await IsFirstTimeSetupAsync();

                if (esConfiguracionInicial)
                {
                    // 3. Configuración inicial - crear usuario dueño
                    var setupCompletado = await HandleFirstTimeSetupAsync();
                    if (!setupCompletado)
                    {
                        return false; // Usuario canceló la configuración
                    }
                }

                // 4. Mostrar pantalla de login
                return await HandleLoginAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error crítico durante la inicialización del sistema:\n\n{ex.Message}\n\n" +
                    "El sistema no puede continuar. Contacte al soporte técnico.",
                    "Error de Inicialización",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        #region MÉTODOS PRIVADOS

        /// <summary>
        /// Asegura que la base de datos esté creada
        /// </summary>
        private static async Task EnsureDatabaseCreatedAsync()
        {
            using var context = new AppDbContext();

            try
            {
                // Crear base de datos si no existe
                await context.Database.EnsureCreatedAsync();

                // Verificar conectividad básica
                await context.Database.CanConnectAsync();

                System.Diagnostics.Debug.WriteLine("✅ Base de datos inicializada correctamente");
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al inicializar la base de datos: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica si es la primera configuración del sistema
        /// </summary>
        private static async Task<bool> IsFirstTimeSetupAsync()
        {
            using var context = new AppDbContext();

            try
            {
                // Verificar si existe al menos un usuario Dueño activo
                var existeDueno = await context.Users
                    .AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);

                System.Diagnostics.Debug.WriteLine($"🔍 ¿Existe usuario Dueño? {existeDueno}");

                return !existeDueno;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error al verificar configuración inicial: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maneja la configuración inicial del sistema
        /// </summary>
        private static async Task<bool> HandleFirstTimeSetupAsync()
        {
            try
            {
                var setupWindow = new FirstTimeSetupWindow();
                var resultado = setupWindow.ShowDialog();

                if (resultado == true)
                {
                    // Verificar que efectivamente se creó el usuario dueño
                    var setupExitoso = await VerifyOwnerCreatedAsync();

                    if (setupExitoso)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Configuración inicial completada exitosamente");
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ Error: No se pudo verificar la creación del usuario propietario.\n\n" +
                            "Por favor intente nuevamente.",
                            "Error de Configuración",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    // Usuario canceló la configuración
                    System.Diagnostics.Debug.WriteLine("⚠️ Usuario canceló la configuración inicial");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error durante la configuración inicial: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica que se haya creado correctamente el usuario dueño
        /// </summary>
        private static async Task<bool> VerifyOwnerCreatedAsync()
        {
            using var context = new AppDbContext();

            try
            {
                var dueno = await context.Users
                    .FirstOrDefaultAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);

                return dueno != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando usuario dueño: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Maneja el proceso de inicio de sesión
        /// </summary>
        private static async Task<bool> HandleLoginAsync()
        {
            try
            {
                var loginWindow = new LoginWindow();
                var resultado = loginWindow.ShowDialog();

                if (resultado == true)
                {
                    // Verificar que haya un usuario logueado
                    if (UserService.UsuarioActual != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Usuario logueado: {UserService.UsuarioActual.NombreCompleto}");

                        // Realizar tareas post-login
                        await PostLoginTasksAsync();

                        return true;
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ Error: No se pudo establecer la sesión del usuario.\n\n" +
                            "Por favor intente iniciar sesión nuevamente.",
                            "Error de Sesión",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return false;
                    }
                }
                else
                {
                    // Usuario canceló el login
                    System.Diagnostics.Debug.WriteLine("⚠️ Usuario canceló el inicio de sesión");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error durante el inicio de sesión: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tareas a realizar después del login exitoso
        /// </summary>
        private static async Task PostLoginTasksAsync()
        {
            try
            {
                using var context = new AppDbContext();

                // 1. Limpiar sesiones inactivas
                var sesionesLimpiadas = await context.CerrarSesionesInactivasAsync();
                if (sesionesLimpiadas > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🧹 Limpiadas {sesionesLimpiadas} sesiones inactivas");
                }

                // 2. Crear configuraciones por defecto si no existen
                await context.CrearConfiguracionBasculaPorDefectoAsync();

                // 3. Actualizar última actividad de la sesión
                if (UserService.SesionActual != null)
                {
                    UserService.SesionActual.ActualizarActividad();
                    await context.SaveChangesAsync();
                }

                System.Diagnostics.Debug.WriteLine("✅ Tareas post-login completadas");
            }
            catch (Exception ex)
            {
                // No es crítico si fallan las tareas post-login
                System.Diagnostics.Debug.WriteLine($"⚠️ Error en tareas post-login: {ex.Message}");
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS AUXILIARES

        /// <summary>
        /// Reinicia el sistema (útil para cambios de configuración)
        /// </summary>
        public static async Task<bool> RestartSystemAsync()
        {
            try
            {
                // Cerrar sesión actual si existe
                if (UserService.UsuarioActual != null)
                {
                    using var userService = new UserService(new AppDbContext());
                    await userService.CerrarSesionAsync("Reinicio del sistema");
                }

                // Reiniciar el flujo de inicialización
                return await InitializeSystemAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al reiniciar el sistema:\n\n{ex.Message}",
                    "Error de Reinicio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Verifica el estado del sistema (útil para diagnósticos)
        /// </summary>
        public static async Task<SystemStatus> GetSystemStatusAsync()
        {
            try
            {
                using var context = new AppDbContext();

                var status = new SystemStatus
                {
                    DatabaseConnected = await context.Database.CanConnectAsync(),
                    UsersCount = await context.Users.CountAsync(u => !u.Eliminado),
                    ActiveUsersCount = await context.Users.CountAsync(u => u.Activo && !u.Eliminado),
                    ActiveSessionsCount = await context.UserSessions.CountAsync(s => s.FechaCierre == null),
                    ProductsCount = await context.RawMaterials.CountAsync(),
                    LastBackup = null // TODO: Implementar sistema de backups
                };

                return status;
            }
            catch (Exception ex)
            {
                return new SystemStatus
                {
                    DatabaseConnected = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Estado del sistema para diagnósticos
    /// </summary>
    public class SystemStatus
    {
        public bool DatabaseConnected { get; set; }
        public int UsersCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int ActiveSessionsCount { get; set; }
        public int ProductsCount { get; set; }
        public DateTime? LastBackup { get; set; }
        public string? ErrorMessage { get; set; }

        public bool IsHealthy => DatabaseConnected &&
                                UsersCount > 0 &&
                                ActiveUsersCount > 0 &&
                                string.IsNullOrEmpty(ErrorMessage);

        public override string ToString()
        {
            if (!IsHealthy)
                return $"❌ Sistema con problemas: {ErrorMessage}";

            return $"✅ Sistema saludable: {UsersCount} usuarios, {ActiveSessionsCount} sesiones activas, {ProductsCount} productos";
        }
    }
}