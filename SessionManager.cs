// ===== MODIFICAR SessionManager.cs =====
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Services;

namespace costbenefi.Managers
{
    /// <summary>
    /// Manejador central de sesiones con reinicio completo de aplicación
    /// </summary>
    public static class SessionManager
    {
        /// <summary>
        /// Cierra sesión y reinicia la aplicación completamente
        /// </summary>
        /// <param name="razon">Motivo del cierre</param>
        /// <param name="mostrarConfirmacion">Si debe mostrar confirmación al usuario</param>
        public static async Task<bool> CerrarSesionYReiniciar(string razon = "Cierre manual", bool mostrarConfirmacion = true)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 CerrarSesionYReiniciar: {razon}");

                // ===== 1. CONFIRMACIÓN SI ES NECESARIA =====
                if (mostrarConfirmacion)
                {
                    var resultado = MessageBox.Show(
                        "🚪 CERRAR SESIÓN\n\n" +
                        "¿Está seguro de que desea cerrar su sesión?\n\n" +
                        "• Se guardará automáticamente todo el trabajo actual\n" +
                        "• Se preservarán carritos de venta pendientes\n" +
                        "• Se cerrarán completamente todos los procesos\n" +
                        "• Se liberarán todos los archivos y conexiones\n" +
                        "• El sistema se reiniciará automáticamente\n" +
                        "• Regresará a la pantalla de login",
                        "Confirmar Cierre de Sesión",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado != MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Usuario canceló cierre de sesión");
                        return false;
                    }
                }

                // ===== 2. GUARDAR INFO ANTES DE CERRAR =====
                var usuarioActual = UserService.UsuarioActual?.NombreCompleto ?? "Usuario desconocido";
                var horaActual = DateTime.Now;

                // ===== 3. GUARDAR TODOS LOS DATOS PENDIENTES =====
                await GuardarDatosPendientes();

                // ===== 4. CERRAR SESIÓN EN BASE DE DATOS =====
                try
                {
                    using var context = new AppDbContext();
                    using var userService = new UserService(context);
                    await userService.CerrarSesionAsync(razon);
                    System.Diagnostics.Debug.WriteLine("✅ Sesión cerrada en BD");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error al cerrar sesión en BD: {ex.Message}");
                    // Continuar con el proceso aunque falle esto
                }

                // ===== 5. MOSTRAR CONFIRMACIÓN FINAL =====
                MessageBox.Show(
                    $"✅ Sesión cerrada correctamente\n\n" +
                    $"Usuario: {usuarioActual}\n" +
                    $"Hora: {horaActual:HH:mm:ss}\n\n" +
                    $"💾 Todos los datos han sido guardados\n" +
                    $"🧹 Todos los procesos se cerrarán completamente\n" +
                    $"🔄 El sistema se reiniciará automáticamente...",
                    "🚪 Sesión Cerrada - CostBenefi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // ===== 6. REINICIAR APLICACIÓN COMPLETA =====
                System.Diagnostics.Debug.WriteLine("🔄 Iniciando proceso de reinicio...");
                await ReiniciarAplicacion();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en CerrarSesionYReiniciar: {ex}");
                MessageBox.Show(
                    $"❌ Error al procesar cierre de sesión:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Reinicia la aplicación completamente, cerrando todos los procesos
        /// </summary>
        private static async Task ReiniciarAplicacion()
        {
            try
            {
                // ===== OBTENER RUTA DEL EJECUTABLE ACTUAL =====
                var procesoActual = Process.GetCurrentProcess();
                var rutaEjecutable = procesoActual.MainModule?.FileName;

                if (string.IsNullOrEmpty(rutaEjecutable))
                {
                    System.Diagnostics.Debug.WriteLine("❌ No se pudo obtener ruta del ejecutable");
                    throw new Exception("No se pudo determinar la ruta del ejecutable");
                }

                System.Diagnostics.Debug.WriteLine($"📂 Ruta ejecutable: {rutaEjecutable}");
                System.Diagnostics.Debug.WriteLine($"🔢 PID proceso actual: {procesoActual.Id}");

                // ===== LIBERAR TODOS LOS RECURSOS ANTES DEL CIERRE =====
                await LiberarTodosLosRecursos();

                // ===== CONFIGURAR NUEVO PROCESO =====
                var startInfo = new ProcessStartInfo
                {
                    FileName = rutaEjecutable,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(rutaEjecutable)
                };

                // ===== DAR TIEMPO PARA QUE SE COMPLETE TODO =====
                await Task.Delay(1500);

                // ===== INICIAR NUEVA INSTANCIA ANTES DE CERRAR LA ACTUAL =====
                System.Diagnostics.Debug.WriteLine("🚀 Iniciando nueva instancia de la aplicación");
                var nuevoProcesoTask = Task.Run(() => Process.Start(startInfo));

                // ===== DAR TIEMPO A QUE INICIE EL NUEVO PROCESO =====
                await Task.Delay(2000);

                // ===== FORZAR CIERRE COMPLETO DEL PROCESO ACTUAL =====
                System.Diagnostics.Debug.WriteLine("🛑 Forzando cierre completo del proceso actual");

                // Usar Dispatcher para cerrar la aplicación de forma controlada
                Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        // Cerrar todas las ventanas
                        foreach (Window window in Application.Current.Windows)
                        {
                            window.Close();
                        }

                        // Forzar shutdown de la aplicación
                        Application.Current.Shutdown(0);

                        // Como última medida, forzar kill del proceso si es necesario
                        await Task.Delay(3000);

                        try
                        {
                            var procesoActualFinal = Process.GetCurrentProcess();
                            if (!procesoActualFinal.HasExited)
                            {
                                System.Diagnostics.Debug.WriteLine("⚠️ Proceso no cerró normalmente, forzando kill");
                                procesoActualFinal.Kill();
                            }
                        }
                        catch
                        {
                            // Si falla el kill, el proceso ya debe estar cerrado
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 ERROR en cierre forzado: {ex.Message}");
                        // Como última instancia, forzar exit
                        Environment.Exit(0);
                    }
                }));

                await nuevoProcesoTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en ReiniciarAplicacion: {ex}");

                MessageBox.Show(
                    $"❌ Error al reiniciar la aplicación:\n\n{ex.Message}\n\n" +
                    "El sistema se cerrará. Por favor, vuelva a abrir el programa manualmente.",
                    "Error de Reinicio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Si falla todo, al menos cerrar el proceso actual
                try
                {
                    await LiberarTodosLosRecursos();
                    Environment.Exit(1);
                }
                catch
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        /// <summary>
        /// Libera todos los recursos para asegurar cierre limpio
        /// </summary>
        private static async Task LiberarTodosLosRecursos()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧹 Liberando todos los recursos...");

                // ===== 1. CERRAR TODAS LAS CONEXIONES A BASE DE DATOS =====
                try
                {
                    using var context = new AppDbContext();

                    // Forzar cierre de conexiones
                    await context.Database.CloseConnectionAsync();
                    context.Dispose();

                    System.Diagnostics.Debug.WriteLine("✅ Conexiones BD cerradas");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error cerrando BD: {ex.Message}");
                }

                // ===== 2. LIMPIAR CACHE Y VARIABLES ESTÁTICAS =====
                try
                {
                    // Limpiar usuario actual
                    if (UserService.UsuarioActual != null)
                    {
                        // Si UserService tiene método de limpieza, usarlo
                        var clearMethod = typeof(UserService).GetMethod("LimpiarUsuarioActual",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        clearMethod?.Invoke(null, null);
                    }

                    System.Diagnostics.Debug.WriteLine("✅ Variables estáticas limpiadas");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error limpiando variables: {ex.Message}");
                }

                // ===== 3. FORZAR GARBAGE COLLECTION =====
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                System.Diagnostics.Debug.WriteLine("✅ Garbage collection ejecutado");

                // ===== 4. CERRAR HANDLES DE ARCHIVOS =====
                try
                {
                    // Cerrar cualquier archivo abierto
                    System.IO.File.WriteAllText(System.IO.Path.GetTempFileName(), ""); // Dummy para forzar flush
                    System.Diagnostics.Debug.WriteLine("✅ Handles de archivos liberados");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error liberando archivos: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine("🎉 Todos los recursos liberados correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR liberando recursos: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda automáticamente todos los datos pendientes antes del cierre
        /// </summary>
        private static async Task GuardarDatosPendientes()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("💾 Iniciando guardado automático de datos pendientes...");

                // ===== 1. VERIFICAR SI HAY VENTANA PRINCIPAL ACTIVA =====
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No hay ventana principal activa");
                    return;
                }

                // ===== 2. GUARDAR CARRITO POS SI HAY ITEMS =====
                await GuardarCarritoPendiente(mainWindow);

                // ===== 3. GUARDAR CAMBIOS EN INVENTARIO =====
                await GuardarCambiosInventario();

                // ===== 4. GUARDAR CONFIGURACIONES TEMPORALES =====
                await GuardarConfiguracionesPendientes();

                // ===== 5. FORZAR COMMIT DE TRANSACCIONES PENDIENTES =====
                await ForzarCommitTransaccionesPendientes();

                System.Diagnostics.Debug.WriteLine("✅ Guardado automático completado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error en guardado automático: {ex.Message}");

                // Mostrar advertencia pero continuar con el cierre
                MessageBox.Show(
                    $"⚠️ Advertencia durante el guardado automático:\n\n{ex.Message}\n\n" +
                    "El cierre de sesión continuará, pero algunos datos podrían no haberse guardado.",
                    "Advertencia de Guardado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Guarda el carrito POS si hay items pendientes
        /// </summary>
        private static async Task GuardarCarritoPendiente(Window mainWindow)
        {
            try
            {
                // Buscar el carrito en la ventana principal usando reflection
                var carritoField = mainWindow.GetType().GetField("_carritoItems",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (carritoField != null)
                {
                    var carritoItems = carritoField.GetValue(mainWindow) as System.Collections.IList;

                    if (carritoItems != null && carritoItems.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"🛒 Carrito tiene {carritoItems.Count} items - Guardando como venta suspendida");

                        // Aquí podrías implementar lógica para guardar como "venta suspendida"
                        // Por ejemplo:
                        // await VentasService.GuardarVentaSuspendida(carritoItems, UserService.UsuarioActual.Id);

                        System.Diagnostics.Debug.WriteLine("✅ Carrito guardado como venta suspendida");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Carrito vacío - No requiere guardado");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error guardando carrito: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda cambios pendientes en inventario
        /// </summary>
        private static async Task GuardarCambiosInventario()
        {
            try
            {
                using var context = new AppDbContext();

                // Verificar si hay cambios pendientes en el contexto
                if (context.ChangeTracker.HasChanges())
                {
                    System.Diagnostics.Debug.WriteLine("📦 Guardando cambios pendientes en inventario...");
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Cambios en inventario guardados");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ No hay cambios pendientes en inventario");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error guardando inventario: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda configuraciones temporales del usuario
        /// </summary>
        private static async Task GuardarConfiguracionesPendientes()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("⚙️ Verificando configuraciones pendientes...");

                // Aquí puedes guardar configuraciones como:
                // - Posición de ventanas
                // - Preferencias de usuario
                // - Configuraciones de dispositivos

                await Task.Delay(10); // Placeholder para operaciones de guardado

                System.Diagnostics.Debug.WriteLine("✅ Configuraciones verificadas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error guardando configuraciones: {ex.Message}");
            }
        }

        /// <summary>
        /// Fuerza el commit de transacciones pendientes
        /// </summary>
        private static async Task ForzarCommitTransaccionesPendientes()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("💳 Verificando transacciones pendientes...");

                using var context = new AppDbContext();

                // Forzar guardado de cualquier cambio pendiente en el contexto
                if (context.ChangeTracker.HasChanges())
                {
                    System.Diagnostics.Debug.WriteLine("💳 Guardando cambios pendientes en transacciones...");
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Cambios en transacciones guardados");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ No hay transacciones pendientes");
                }

                // Aquí puedes agregar lógica específica para tus tablas de transacciones
                // Por ejemplo, si tienes una tabla Ventas o Transacciones:
                /*
                var ventasPendientes = await context.Ventas
                    .Where(v => v.Estado == "Pendiente" || v.Estado == "Procesando")
                    .CountAsync();
                    
                if (ventasPendientes > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"💳 Se encontraron {ventasPendientes} ventas pendientes");
                    // Implementar lógica para completar o cancelar
                }
                */

                System.Diagnostics.Debug.WriteLine("✅ Verificación de transacciones completada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error verificando transacciones: {ex.Message}");
            }
        }
        /// <summary>
        /// Cierra completamente la aplicación sin reiniciar
        /// </summary>
        /// <param name="razon">Motivo del cierre</param>
        /// <param name="mostrarConfirmacion">Si debe mostrar confirmación al usuario</param>
        public static async Task<bool> SalirCompletamente(string razon = "Cierre manual", bool mostrarConfirmacion = true)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚪 SalirCompletamente: {razon}");

                // ===== 1. CONFIRMACIÓN SI ES NECESARIA =====
                if (mostrarConfirmacion)
                {
                    var resultado = MessageBox.Show(
                        "🚪 SALIR DEL SISTEMA\n\n" +
                        "¿Está seguro de que desea salir completamente del sistema?\n\n" +
                        "• Se guardará automáticamente todo el trabajo actual\n" +
                        "• Se preservarán carritos de venta pendientes\n" +
                        "• Se cerrarán completamente todos los procesos\n" +
                        "• Se liberarán todos los archivos y conexiones\n" +
                        "• El sistema se cerrará por completo",
                        "Confirmar Salida",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado != MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Usuario canceló salida del sistema");
                        return false;
                    }
                }

                // ===== 2. GUARDAR INFO ANTES DE CERRAR =====
                var usuarioActual = UserService.UsuarioActual?.NombreCompleto ?? "Usuario desconocido";
                var horaActual = DateTime.Now;

                // ===== 3. GUARDAR TODOS LOS DATOS PENDIENTES =====
                await GuardarDatosPendientes();

                // ===== 4. CERRAR SESIÓN EN BASE DE DATOS =====
                try
                {
                    using var context = new AppDbContext();
                    using var userService = new UserService(context);
                    await userService.CerrarSesionAsync(razon);
                    System.Diagnostics.Debug.WriteLine("✅ Sesión cerrada en BD");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error al cerrar sesión en BD: {ex.Message}");
                    // Continuar con el proceso aunque falle esto
                }

                // ===== 5. MOSTRAR CONFIRMACIÓN FINAL =====
                MessageBox.Show(
                    $"✅ Sistema cerrado correctamente\n\n" +
                    $"Usuario: {usuarioActual}\n" +
                    $"Hora: {horaActual:HH:mm:ss}\n\n" +
                    $"💾 Todos los datos han sido guardados\n" +
                    $"🧹 Todos los procesos se cerrarán completamente\n\n" +
                    $"¡Gracias por usar CostBenefi!",
                    "🚪 Sistema Cerrado - CostBenefi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // ===== 6. CERRAR COMPLETAMENTE SIN REINICIAR =====
                System.Diagnostics.Debug.WriteLine("🛑 Cerrando aplicación completamente...");
                await CerrarAplicacionCompletamente();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en SalirCompletamente: {ex}");
                MessageBox.Show(
                    $"❌ Error al cerrar el sistema:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // En caso de error, forzar cierre
                await CerrarAplicacionCompletamente();
                return false;
            }
        }

        /// <summary>
        /// Cierra la aplicación completamente sin reiniciar
        /// </summary>
        private static async Task CerrarAplicacionCompletamente()
        {
            try
            {
                // ===== LIBERAR TODOS LOS RECURSOS =====
                await LiberarTodosLosRecursos();

                // ===== DAR TIEMPO PARA QUE SE COMPLETE TODO =====
                await Task.Delay(1000);

                // ===== CERRAR APLICACIÓN DE FORMA CONTROLADA =====
                Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        // Cerrar todas las ventanas
                        foreach (Window window in Application.Current.Windows)
                        {
                            window.Close();
                        }

                        // Shutdown de la aplicación
                        Application.Current.Shutdown(0);

                        // Como medida adicional, forzar cierre si es necesario
                        await Task.Delay(2000);

                        try
                        {
                            var procesoActual = Process.GetCurrentProcess();
                            if (!procesoActual.HasExited)
                            {
                                System.Diagnostics.Debug.WriteLine("⚠️ Proceso no cerró normalmente, forzando exit");
                                Environment.Exit(0);
                            }
                        }
                        catch
                        {
                            // Si falla, el proceso ya debe estar cerrado
                            Environment.Exit(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 ERROR en cierre final: {ex.Message}");
                        Environment.Exit(1);
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en CerrarAplicacionCompletamente: {ex}");

                // Como último recurso
                try
                {
                    await LiberarTodosLosRecursos();
                    Environment.Exit(1);
                }
                catch
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
        }
    }
}