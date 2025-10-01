using System;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Services;

namespace costbenefi.Models
{
    /// <summary>
    /// Configuración simple de permisos de interfaz basada en roles
    /// </summary>
    public static class PermisosSimples
    {
        /// <summary>
        /// Configura toda la interfaz de una vez basándose en el rol del usuario
        /// </summary>
        public static void ConfigurarInterfazPorRol(Window mainWindow)
        {
            try
            {
                var rol = UserService.UsuarioActual?.Rol ?? "";
                var esSoporte = SoporteSystem.UsuarioActualEsSoporte();

                System.Diagnostics.Debug.WriteLine($"🔐 Configurando interfaz para rol: {rol} (Soporte: {esSoporte})");

                // Si es soporte, mostrar todo
                if (esSoporte)
                {
                    MostrarTodoParaSoporte(mainWindow);
                    return;
                }

                // Configurar según rol específico
                switch (rol)
                {
                    case "Dueño":
                        ConfigurarParaDueño(mainWindow);
                        break;

                    case "Encargado":
                        ConfigurarParaEncargado(mainWindow);
                        break;

                    case "Cajero":
                        ConfigurarParaCajero(mainWindow);
                        break;

                    default:
                        OcultarTodo(mainWindow);
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Interfaz configurada para: {rol}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando interfaz: {ex.Message}");
            }
        }

        #region Configuraciones por Rol

        /// <summary>
        /// DUEÑO: Acceso total a todo
        /// </summary>
        private static void ConfigurarParaDueño(Window mainWindow)
        {
            // ===== PESTAÑAS: Todas visibles =====
            MostrarPestaña(mainWindow, "Materia Prima", true);
            MostrarPestaña(mainWindow, "Punto de Venta", true);
            MostrarPestaña(mainWindow, "Reportes", true);
            MostrarPestaña(mainWindow, "Procesos", true);
            MostrarPestaña(mainWindow, "Análisis", true);
            MostrarPestaña(mainWindow, "Configuración", true);

            // ===== BOTONES MATERIA PRIMA: Todos =====
            MostrarControl(mainWindow, "BtnAgregar", true);
            MostrarControl(mainWindow, "BtnEditar", true);
            MostrarControl(mainWindow, "BtnEliminar", true);  // Solo Dueño puede eliminar
            MostrarControl(mainWindow, "BtnEscaner", true);

            // ===== BOTONES POS: Todos =====
            MostrarControl(mainWindow, "BtnConfigurarPrecios", true);
            MostrarControl(mainWindow, "BtnConfigComisiones", true);
            MostrarControl(mainWindow, "BtnBascula", true);
            MostrarControl(mainWindow, "BtnImpresora", true);
            MostrarControl(mainWindow, "BtnCorteCaja", true);

            // ===== REPORTES: Todos =====
            MostrarControl(mainWindow, "BtnReporteVentas", true);
            MostrarControl(mainWindow, "BtnReporteStock", true);
            MostrarControl(mainWindow, "BtnHistorialSesiones", true);  // Solo Dueño

            // ===== CONFIGURACIÓN: Todo =====
            MostrarControl(mainWindow, "BtnGestionUsuarios", true);    // Solo Dueño
            MostrarControl(mainWindow, "BtnConfiguracionSistema", true);
        }

        /// <summary>
        /// ENCARGADO: Ve todo excepto: eliminar productos, configurar comisiones, historial sesiones, gestionar usuarios
        /// </summary>
        private static void ConfigurarParaEncargado(Window mainWindow)
        {
            // ===== PESTAÑAS: TODAS visibles =====
            MostrarPestaña(mainWindow, "Materia Prima", true);
            MostrarPestaña(mainWindow, "Punto de Venta", true);
            MostrarPestaña(mainWindow, "Reportes", true);
            MostrarPestaña(mainWindow, "Procesos", true);
            MostrarPestaña(mainWindow, "Análisis", true);
            MostrarPestaña(mainWindow, "Configuración", true);   // ✅ SÍ ve configuración

            // ===== BOTONES MATERIA PRIMA: Sin eliminar =====
            MostrarControl(mainWindow, "BtnAgregar", true);
            MostrarControl(mainWindow, "BtnEditar", true);
            MostrarControl(mainWindow, "BtnEliminar", false);   // ❌ No eliminar productos
            MostrarControl(mainWindow, "BtnEscaner", true);

            // ===== BOTONES POS: Sin comisiones =====
            MostrarControl(mainWindow, "BtnConfigurarPrecios", true);
            MostrarControl(mainWindow, "BtnConfigComisiones", false);  // ❌ No comisiones
            MostrarControl(mainWindow, "BtnBascula", true);
            MostrarControl(mainWindow, "BtnImpresora", true);
            MostrarControl(mainWindow, "BtnCorteCaja", true);

            // ===== REPORTES: Sin historial de sesiones =====
            MostrarControl(mainWindow, "BtnReporteVentas", true);
            MostrarControl(mainWindow, "BtnReporteStock", true);
            MostrarControl(mainWindow, "BtnHistorialSesiones", false);  // ❌ No historial sesiones

            // ===== CONFIGURACIÓN: Sin gestionar usuarios =====
            MostrarControl(mainWindow, "BtnGestionUsuarios", false);      // ❌ No gestionar usuarios
            MostrarControl(mainWindow, "BtnConfiguracionSistema", true);   // ✅ Sí configuración sistema
        }

        /// <summary>
        /// CAJERO: Solo ve Mi Información, Punto de Venta y Sistema. En POS no ve precios ni comisiones
        /// </summary>
        private static void ConfigurarParaCajero(Window mainWindow)
        {
            // ===== PESTAÑAS: Solo las básicas =====
            MostrarPestaña(mainWindow, "Materia Prima", false);   // ❌ No ve materia prima
            MostrarPestaña(mainWindow, "Punto de Venta", true);   // ✅ Su pestaña principal
            MostrarPestaña(mainWindow, "Reportes", false);        // ❌ No reportes
            MostrarPestaña(mainWindow, "Procesos", false);        // ❌ No procesos
            MostrarPestaña(mainWindow, "Análisis", false);        // ❌ No análisis
            MostrarPestaña(mainWindow, "Configuración", false);   // ❌ No configuración
            // Mi Información y Sistema siempre están visibles

            // ===== BOTONES MATERIA PRIMA: No aplica (no ve la pestaña) =====
            MostrarControl(mainWindow, "BtnAgregar", false);
            MostrarControl(mainWindow, "BtnEditar", false);
            MostrarControl(mainWindow, "BtnEliminar", false);
            MostrarControl(mainWindow, "BtnEscaner", false);

            // ===== BOTONES POS: Sin precios ni comisiones =====
            MostrarControl(mainWindow, "BtnConfigurarPrecios", false);   // ❌ No precios
            MostrarControl(mainWindow, "BtnConfigComisiones", false);    // ❌ No comisiones
            MostrarControl(mainWindow, "BtnBascula", true);              // ✅ Sí báscula
            MostrarControl(mainWindow, "BtnImpresora", true);            // ✅ Sí impresora
            MostrarControl(mainWindow, "BtnCorteCaja", true);            // ✅ Sí corte caja

            // ===== REPORTES: No ve la pestaña =====
            MostrarControl(mainWindow, "BtnReporteVentas", false);
            MostrarControl(mainWindow, "BtnReporteStock", false);
            MostrarControl(mainWindow, "BtnHistorialSesiones", false);

            // ===== CONFIGURACIÓN: No ve la pestaña =====
            MostrarControl(mainWindow, "BtnGestionUsuarios", false);
            MostrarControl(mainWindow, "BtnConfiguracionSistema", false);
        }

        /// <summary>
        /// SOPORTE: Acceso total a todo (como Dueño)
        /// </summary>
        private static void MostrarTodoParaSoporte(Window mainWindow)
        {
            System.Diagnostics.Debug.WriteLine("🔧 Configurando acceso total para usuario soporte");
            ConfigurarParaDueño(mainWindow); // Igual que Dueño
        }

        /// <summary>
        /// Sin usuario o rol no reconocido: Ocultar todo
        /// </summary>
        private static void OcultarTodo(Window mainWindow)
        {
            System.Diagnostics.Debug.WriteLine("🔒 Ocultando toda la interfaz - Sin usuario válido");

            // Ocultar todas las pestañas excepto Sistema
            MostrarPestaña(mainWindow, "Materia Prima", false);
            MostrarPestaña(mainWindow, "Punto de Venta", false);
            MostrarPestaña(mainWindow, "Reportes", false);
            MostrarPestaña(mainWindow, "Procesos", false);
            MostrarPestaña(mainWindow, "Análisis", false);
            MostrarPestaña(mainWindow, "Configuración", false);
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Muestra u oculta una pestaña por su header
        /// </summary>
        private static void MostrarPestaña(Window mainWindow, string headerParcial, bool visible)
        {
            try
            {
                var tabControl = mainWindow.FindName("MainTabControl") as TabControl;
                if (tabControl?.Items != null)
                {
                    foreach (TabItem tab in tabControl.Items)
                    {
                        if (tab.Header?.ToString()?.Contains(headerParcial) == true)
                        {
                            tab.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                            System.Diagnostics.Debug.WriteLine($"   📋 {headerParcial}: {(visible ? "Visible" : "Oculta")}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ Error con pestaña {headerParcial}: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra u oculta un control por su nombre
        /// </summary>
        private static void MostrarControl(Window mainWindow, string nombreControl, bool visible)
        {
            try
            {
                var control = mainWindow.FindName(nombreControl) as FrameworkElement;
                if (control != null)
                {
                    control.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

                    // Si es un botón, también controlar IsEnabled
                    if (control is Button btn)
                    {
                        btn.IsEnabled = visible;
                    }

                    System.Diagnostics.Debug.WriteLine($"   🔘 {nombreControl}: {(visible ? "Visible" : "Oculto")}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ Error con control {nombreControl}: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene información de diagnóstico del rol actual
        /// </summary>
        public static string ObtenerInfoRol()
        {
            var usuario = UserService.UsuarioActual;
            var esSoporte = SoporteSystem.UsuarioActualEsSoporte();

            if (esSoporte)
            {
                return "🔧 Usuario Soporte - Acceso Total";
            }

            if (usuario == null)
            {
                return "❌ Sin usuario logueado";
            }

            return usuario.Rol switch
            {
                "Dueño" => "👑 Dueño - Control Total",
                "Encargado" => "👔 Encargado - Operaciones Completas (sin eliminar, sin comisiones, sin gestionar usuarios)",
                "Cajero" => "🏪 Cajero - Solo Punto de Venta",
                _ => $"❓ Rol desconocido: {usuario.Rol}"
            };
        }

        #endregion
    }
}