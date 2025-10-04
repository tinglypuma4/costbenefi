using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Services;
using costbenefi.Views;
using System.Linq;
using costbenefi.Models;  // ← LicenseManager está aquí

namespace costbenefi
{
    public partial class App : Application
    {
        // Licencia actual del sistema (accesible globalmente)
        public static LicenseManager.LicenseInfo CurrentLicense { get; private set; }

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

                // 🧪 TEMPORAL PARA PRUEBAS - Descomentar para borrar licencia
                // System.IO.File.Delete("license.key");

                // ═══════════════════════════════════════════════════════════
                // 🔐 PASO 1: VALIDAR LICENCIA (LO PRIMERO DE TODO)
                // ═══════════════════════════════════════════════════════════
                if (!ValidarLicencia())
                {
                    // No hay licencia válida, cerrar aplicación
                    System.Diagnostics.Debug.WriteLine("❌ Sin licencia válida - Cerrando aplicación");
                    Application.Current.Shutdown();
                    return;
                }

                // ═══════════════════════════════════════════════════════════
                // 🔐 PASO 2: MOSTRAR INFO DE LICENCIA
                // ═══════════════════════════════════════════════════════════
                MostrarInformacionLicencia();

                // ═══════════════════════════════════════════════════════════
                // 📂 PASO 3: INICIALIZAR BASE DE DATOS
                // ═══════════════════════════════════════════════════════════
                await InicializarBaseDatos();

                // ═══════════════════════════════════════════════════════════
                // 🔍 PASO 4: VERIFICAR SI ES PRIMERA VEZ
                // ═══════════════════════════════════════════════════════════
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

        /// <summary>
        /// 🔐 VALIDAR LICENCIA - SE EJECUTA PRIMERO
        /// </summary>
        private bool ValidarLicencia()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔐 Validando licencia del sistema...");

                // Validar licencia
                CurrentLicense = LicenseManager.ValidateLicense();

                if (!CurrentLicense.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Licencia inválida - Mostrando ventana de activación");

                    // Mostrar ventana de activación
                    var activationWindow = new ActivationWindow();

                    if (activationWindow.ShowDialog() == true)
                    {
                        // Re-validar después de activar
                        CurrentLicense = LicenseManager.ValidateLicense();

                        if (!CurrentLicense.IsValid)
                        {
                            MessageBox.Show(
                                "No se pudo activar la licencia correctamente.\n\n" +
                                "El sistema se cerrará.",
                                "Error de Activación",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return false;
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Licencia activada correctamente");
                        return true;
                    }
                    else
                    {
                        // Usuario canceló la activación
                        System.Diagnostics.Debug.WriteLine("❌ Usuario canceló activación");
                        return false;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Licencia válida: {CurrentLicense.Type} - {CurrentLicense.CompanyName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error validando licencia: {ex.Message}");
                MessageBox.Show(
                    $"Error al validar licencia:\n\n{ex.Message}\n\n" +
                    "El sistema se cerrará.",
                    "Error de Licencia",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }

        /// <summary>
        /// Mostrar información de la licencia activada
        /// </summary>
        private void MostrarInformacionLicencia()
        {
            if (CurrentLicense == null || !CurrentLicense.IsValid)
                return;

            try
            {
                string tipoIcono = CurrentLicense.Type switch
                {
                    LicenseManager.LicenseType.BASICA => "🥉",
                    LicenseManager.LicenseType.MEDIA => "🥈",
                    LicenseManager.LicenseType.AVANZADA => "🥇",
                    LicenseManager.LicenseType.PORVIDA => "💎",
                    _ => "✅"
                };

                string tipoFormateado = CurrentLicense.Type switch
                {
                    LicenseManager.LicenseType.BASICA => "Básica",
                    LicenseManager.LicenseType.MEDIA => "Media",
                    LicenseManager.LicenseType.AVANZADA => "Avanzada",
                    LicenseManager.LicenseType.PORVIDA => "De Por Vida",
                    _ => CurrentLicense.Type.ToString()
                };

                // Verificar si está por vencer
                if (LicenseManager.IsExpiringSoon(CurrentLicense))
                {
                    MessageBox.Show(
                        $"⚠️ AVISO: Su licencia expirará en {CurrentLicense.DaysRemaining} días\n\n" +
                        $"═══════════════════════════════════════\n\n" +
                        $"   Tipo:              {tipoFormateado}\n" +
                        $"   Fecha expiración:  {CurrentLicense.ExpirationDate:dd/MM/yyyy}\n\n" +
                        $"═══════════════════════════════════════\n\n" +
                        "Por favor contacte a su proveedor para renovar.",
                        "Licencia por Vencer",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    string mensaje;

                    if (CurrentLicense.Type == LicenseManager.LicenseType.PORVIDA)
                    {
                        mensaje = $"{tipoIcono} BIENVENIDO AL SISTEMA POS\n\n" +
                                 $"═══════════════════════════════════════\n\n" +
                                 $"   Empresa:      {CurrentLicense.CompanyName}\n" +
                                 $"   Licencia:     {tipoFormateado}\n" +
                                 $"   Vigencia:     Para Siempre ∞\n\n" +
                                 $"═══════════════════════════════════════\n\n" +
                                 "Sistema Costo-Beneficio\n" +
                                 "Versión 1.0 - Totalmente Activado\n\n" +
                                 "¡Gracias por confiar en nosotros!";
                    }
                    else
                    {
                        mensaje = $"{tipoIcono} BIENVENIDO AL SISTEMA POS\n\n" +
                                 $"═══════════════════════════════════════\n\n" +
                                 $"   Empresa:         {CurrentLicense.CompanyName}\n" +
                                 $"   Licencia:        {tipoFormateado}\n" +
                                 $"   Válida hasta:    {CurrentLicense.ExpirationDate:dd/MM/yyyy}\n" +
                                 $"   Días restantes:  {CurrentLicense.DaysRemaining:N0} días\n\n" +
                                 $"═══════════════════════════════════════\n\n" +
                                 "Sistema Costo-Beneficio\n" +
                                 "Versión 1.0 - Totalmente Activado\n\n" +
                                 "¡Gracias por confiar en nosotros!";
                    }

                    MessageBox.Show(
                        mensaje,
                        "Sistema Activado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                System.Diagnostics.Debug.WriteLine($"📋 Licencia: {CurrentLicense.Type} | Empresa: {CurrentLicense.CompanyName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error mostrando info de licencia: {ex.Message}");
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
                PermisosSimples.ConfigurarInterfazPorRol(mainWindow);
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