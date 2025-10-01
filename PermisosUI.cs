using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Services;

namespace costbenefi.Models
{
    /// <summary>
    /// Gestiona los permisos de la interfaz de usuario basándose en roles
    /// </summary>
    public static class PermisosUI
    {
        // ===== DEFINICIÓN DE PERMISOS ESPECÍFICOS =====
        public static class Permisos
        {
            // Módulos principales
            public const string MATERIA_PRIMA = "Inventario";
            public const string PUNTO_VENTA = "POS";
            public const string REPORTES = "Reportes";
            public const string PROCESOS = "Inventario";
            public const string ANALISIS = "Reportes";
            public const string CONFIGURACION = "Usuarios";

            // Acciones específicas en Materia Prima
            public const string CREAR_PRODUCTO = "Inventario";
            public const string EDITAR_PRODUCTO = "Inventario";
            public const string ELIMINAR_PRODUCTO = "Eliminacion";
            public const string VER_PRECIOS = "Inventario";

            // Acciones específicas en POS
            public const string PROCESAR_VENTAS = "POS";
            public const string APLICAR_DESCUENTOS = "POS";
            public const string CORTE_CAJA = "CorteCaja";
            public const string VER_VENTAS = "POS";
            public const string CONFIGURAR_PRECIOS = "Inventario";
            public const string CONFIGURAR_COMISIONES = "Inventario";
            public const string CONFIGURAR_IMPRESORA = "POS";
            public const string CONFIGURAR_BASCULA = "POS";

            // Acciones específicas en Reportes
            public const string VER_REPORTES_VENTAS = "Reportes";
            public const string VER_REPORTES_STOCK = "Reportes";
            public const string VER_HISTORIAL_SESIONES = "Usuarios";
            public const string VER_CORTES_CAJA = "CorteCaja";

            // Gestión de usuarios
            public const string GESTIONAR_USUARIOS = "Usuarios";
            public const string VER_CONFIGURACION_SISTEMA = "Usuarios";
        }

        // ===== MÉTODOS PRINCIPALES =====

        /// <summary>
        /// Verifica si el usuario actual tiene un permiso específico
        /// </summary>
        public static bool TienePermiso(string permiso)
        {
            return UserService.TienePermiso(permiso);
        }

        /// <summary>
        /// Obtiene el rol del usuario actual
        /// </summary>
        public static string RolActual
        {
            get { return UserService.UsuarioActual?.Rol ?? "Sin Rol"; }
        }

        /// <summary>
        /// Verifica si el usuario actual es Dueño
        /// </summary>
        public static bool EsDueño
        {
            get { return RolActual == "Dueño"; }
        }

        /// <summary>
        /// Verifica si el usuario actual es Encargado o superior
        /// </summary>
        public static bool EsEncargadoOSuperior
        {
            get { return RolActual == "Dueño" || RolActual == "Encargado"; }
        }

        /// <summary>
        /// Verifica si el usuario actual es Cajero
        /// </summary>
        public static bool EsCajero
        {
            get { return RolActual == "Cajero"; }
        }

        // ===== APLICACIÓN DE PERMISOS A PESTAÑAS =====

        /// <summary>
        /// Configura la visibilidad de las pestañas del TabControl principal
        /// </summary>
        public static void ConfigurarPestañas(TabControl tabControl)
        {
            if (tabControl?.Items == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 Configurando pestañas para rol: {RolActual}");

                for (int i = 0; i < tabControl.Items.Count; i++)
                {
                    if (tabControl.Items[i] is TabItem tab)
                    {
                        var header = tab.Header?.ToString() ?? "";
                        bool visible = true;

                        // Determinar visibilidad por pestaña
                        switch (i)
                        {
                            case 0: // 📦 Materia Prima
                                visible = TienePermiso(Permisos.MATERIA_PRIMA);
                                break;

                            case 1: // 💰 Punto de Venta
                                visible = TienePermiso(Permisos.PUNTO_VENTA);
                                break;

                            case 2: // 📊 Reportes
                                visible = TienePermiso(Permisos.REPORTES);
                                break;

                            case 3: // ⚙️ Procesos
                                visible = TienePermiso(Permisos.PROCESOS);
                                break;

                            case 4: // 📈 Análisis
                                visible = TienePermiso(Permisos.ANALISIS);
                                break;

                            case 5: // ⚙️ Configuración
                                visible = TienePermiso(Permisos.CONFIGURACION);
                                break;

                            case 6: // 👨‍💻 Mi Información
                                visible = true; // Siempre visible
                                break;

                            case 7: // 🔧 Sistema
                                visible = true; // Siempre visible para cerrar sesión
                                break;

                            default:
                                visible = true;
                                break;
                        }

                        // Aplicar visibilidad
                        tab.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

                        System.Diagnostics.Debug.WriteLine($"   • {header}: {(visible ? "Visible" : "Oculta")}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Pestañas configuradas para rol: {RolActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando pestañas: {ex.Message}");
            }
        }

        // ===== APLICACIÓN DE PERMISOS A CONTROLES ESPECÍFICOS =====

        /// <summary>
        /// Configura los permisos de los controles en la pestaña Materia Prima
        /// </summary>
        public static void ConfigurarMateriaPrima(FrameworkElement root)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 Configurando permisos Materia Prima para: {RolActual}");

                // Botones principales
                ConfigurarControl(root, "BtnAgregar", Permisos.CREAR_PRODUCTO);
                ConfigurarControl(root, "BtnEditar", Permisos.EDITAR_PRODUCTO);
                ConfigurarControl(root, "BtnEliminar", Permisos.ELIMINAR_PRODUCTO);

                // Cajeros solo pueden ver, no modificar
                if (EsCajero)
                {
                    System.Diagnostics.Debug.WriteLine("   🔒 Cajero: Solo lectura en Materia Prima");
                    OcultarControl(root, "BtnAgregar");
                    OcultarControl(root, "BtnEditar");
                    OcultarControl(root, "BtnEliminar");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Materia Prima configurada para: {RolActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando Materia Prima: {ex.Message}");
            }
        }

        /// <summary>
        /// Configura los permisos de los controles en la pestaña POS
        /// </summary>
        public static void ConfigurarPuntoVenta(FrameworkElement root)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 Configurando permisos POS para: {RolActual}");

                // Configuraciones avanzadas (solo Dueño y Encargado)
                ConfigurarControl(root, "BtnConfigurarPrecios", Permisos.CONFIGURAR_PRECIOS);
                ConfigurarControl(root, "BtnConfigComisiones", Permisos.CONFIGURAR_COMISIONES);
                ConfigurarControl(root, "BtnBascula", Permisos.CONFIGURAR_BASCULA);
                ConfigurarControl(root, "BtnImpresora", Permisos.CONFIGURAR_IMPRESORA);

                // Corte de caja (Encargado y Dueño)
                ConfigurarControl(root, "BtnCorteCaja", Permisos.CORTE_CAJA);

                // Cajeros: Ocultar configuraciones avanzadas
                if (EsCajero)
                {
                    System.Diagnostics.Debug.WriteLine("   🔒 Cajero: Ocultando configuraciones avanzadas");
                    OcultarControl(root, "BtnConfigurarPrecios");
                    OcultarControl(root, "BtnConfigComisiones");
                    OcultarControl(root, "BtnBascula");
                    OcultarControl(root, "BtnImpresora");
                }

                System.Diagnostics.Debug.WriteLine($"✅ POS configurado para: {RolActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando POS: {ex.Message}");
            }
        }

        /// <summary>
        /// Configura los permisos de los controles en la pestaña Reportes
        /// </summary>
        public static void ConfigurarReportes(FrameworkElement root)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 Configurando permisos Reportes para: {RolActual}");

                // Reportes básicos (todos pueden ver)
                ConfigurarControl(root, "BtnReporteVentas", Permisos.VER_REPORTES_VENTAS);
                ConfigurarControl(root, "BtnReporteStock", Permisos.VER_REPORTES_STOCK);

                // Reportes avanzados (solo Dueño)
                ConfigurarControl(root, "BtnHistorialSesiones", Permisos.VER_HISTORIAL_SESIONES);

                // Cajeros: Solo reportes básicos
                if (EsCajero)
                {
                    System.Diagnostics.Debug.WriteLine("   🔒 Cajero: Solo reportes básicos");
                    OcultarControl(root, "BtnHistorialSesiones");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Reportes configurado para: {RolActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando Reportes: {ex.Message}");
            }
        }

        /// <summary>
        /// Configura los permisos de los controles en la pestaña Configuración
        /// </summary>
        public static void ConfigurarConfiguracion(FrameworkElement root)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 Configurando permisos Configuración para: {RolActual}");

                // Solo el Dueño puede gestionar usuarios
                ConfigurarControl(root, "BtnGestionUsuarios", Permisos.GESTIONAR_USUARIOS);
                ConfigurarControl(root, "BtnConfiguracionSistema", Permisos.VER_CONFIGURACION_SISTEMA);

                // Si no es Dueño, ocultar todo
                if (!EsDueño)
                {
                    System.Diagnostics.Debug.WriteLine("   🔒 No es Dueño: Ocultando gestión de usuarios");
                    OcultarControl(root, "BtnGestionUsuarios");
                    OcultarControl(root, "BtnConfiguracionSistema");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Configuración configurada para: {RolActual}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando Configuración: {ex.Message}");
            }
        }

        // ===== MÉTODOS AUXILIARES =====

        /// <summary>
        /// Configura un control específico basándose en permisos
        /// </summary>
        private static void ConfigurarControl(FrameworkElement root, string nombreControl, string permisoRequerido)
        {
            try
            {
                var control = BuscarControl(root, nombreControl);
                if (control != null)
                {
                    bool tienePermiso = TienePermiso(permisoRequerido);
                    control.Visibility = tienePermiso ? Visibility.Visible : Visibility.Collapsed;

                    if (control is Button btn)
                    {
                        btn.IsEnabled = tienePermiso;
                    }

                    System.Diagnostics.Debug.WriteLine($"   • {nombreControl}: {(tienePermiso ? "Visible" : "Oculto")} (req: {permisoRequerido})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ Error configurando {nombreControl}: {ex.Message}");
            }
        }

        /// <summary>
        /// Oculta un control específico
        /// </summary>
        private static void OcultarControl(FrameworkElement root, string nombreControl)
        {
            try
            {
                var control = BuscarControl(root, nombreControl);
                if (control != null)
                {
                    control.Visibility = Visibility.Collapsed;
                    if (control is Button btn)
                    {
                        btn.IsEnabled = false;
                    }
                    System.Diagnostics.Debug.WriteLine($"   • {nombreControl}: Oculto forzosamente");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ Error ocultando {nombreControl}: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca un control por nombre en el árbol visual
        /// </summary>
        private static FrameworkElement BuscarControl(FrameworkElement root, string nombre)
        {
            if (root == null || string.IsNullOrEmpty(nombre)) return null;

            // Buscar por nombre en el elemento actual
            if (root.Name == nombre) return root;

            // Buscar en elementos hijos
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                if (System.Windows.Media.VisualTreeHelper.GetChild(root, i) is FrameworkElement child)
                {
                    var result = BuscarControl(child, nombre);
                    if (result != null) return result;
                }
            }

            return null;
        }

        // ===== MÉTODOS PARA VALIDACIONES DINÁMICAS =====

        /// <summary>
        /// Verifica si el usuario puede realizar una acción específica
        /// </summary>
        public static bool PuedeEjecutarAccion(string accion)
        {
            return accion switch
            {
                "CrearProducto" => TienePermiso(Permisos.CREAR_PRODUCTO),
                "EditarProducto" => TienePermiso(Permisos.EDITAR_PRODUCTO),
                "EliminarProducto" => TienePermiso(Permisos.ELIMINAR_PRODUCTO),
                "ProcesarVenta" => TienePermiso(Permisos.PROCESAR_VENTAS),
                "AplicarDescuento" => TienePermiso(Permisos.APLICAR_DESCUENTOS),
                "CorteCaja" => TienePermiso(Permisos.CORTE_CAJA),
                "VerReportes" => TienePermiso(Permisos.VER_REPORTES_VENTAS),
                "GestionarUsuarios" => TienePermiso(Permisos.GESTIONAR_USUARIOS),
                _ => false
            };
        }

        /// <summary>
        /// Obtiene un mensaje de error cuando no se tienen permisos
        /// </summary>
        public static string ObtenerMensajeError(string accion)
        {
            var rolRequerido = accion switch
            {
                "CrearProducto" => "Encargado o Dueño",
                "EditarProducto" => "Encargado o Dueño",
                "EliminarProducto" => "Dueño",
                "AplicarDescuento" => "Encargado o Dueño",
                "GestionarUsuarios" => "Dueño",
                _ => "permisos superiores"
            };

            return $"❌ ACCESO DENEGADO\n\n" +
                   $"Su rol actual: {RolActual}\n" +
                   $"Rol requerido: {rolRequerido}\n\n" +
                   $"Contacte al administrador si necesita realizar esta acción.";
        }

        /// <summary>
        /// Muestra un mensaje de error por falta de permisos
        /// </summary>
        public static void MostrarErrorPermisos(string accion)
        {
            MessageBox.Show(
                ObtenerMensajeError(accion),
                "Permisos Insuficientes",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        // ===== MÉTODO PRINCIPAL DE CONFIGURACIÓN =====

        /// <summary>
        /// Configura todos los permisos de la interfaz principal
        /// </summary>
        public static void ConfigurarTodosLosPermisos(Window mainWindow)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 === CONFIGURANDO PERMISOS PARA {RolActual} ===");

                if (mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ MainWindow es null");
                    return;
                }

                // Configurar pestañas principales
                var tabControl = mainWindow.FindName("MainTabControl") as TabControl;
                if (tabControl != null)
                {
                    ConfigurarPestañas(tabControl);
                }

                // Configurar controles específicos por pestaña
                ConfigurarMateriaPrima(mainWindow);
                ConfigurarPuntoVenta(mainWindow);
                ConfigurarReportes(mainWindow);
                ConfigurarConfiguracion(mainWindow);

                System.Diagnostics.Debug.WriteLine($"✅ === PERMISOS CONFIGURADOS COMPLETAMENTE ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando permisos: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reconfigura permisos cuando cambia el usuario
        /// </summary>
        public static void ActualizarPermisosPorCambioUsuario(Window mainWindow)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 Actualizando permisos por cambio de usuario a: {RolActual}");
            ConfigurarTodosLosPermisos(mainWindow);
        }

        /// <summary>
        /// Obtiene información de diagnóstico sobre permisos
        /// </summary>
        public static string ObtenerInfoDiagnostico()
        {
            var usuario = UserService.UsuarioActual;
            if (usuario == null) return "Sin usuario logueado";

            return $"DIAGNÓSTICO DE PERMISOS\n\n" +
                   $"Usuario: {usuario.NombreCompleto}\n" +
                   $"Rol: {usuario.Rol}\n" +
                   $"Activo: {usuario.Activo}\n" +
                   $"Bloqueado: {usuario.EstaBloqueado}\n\n" +
                   $"PERMISOS:\n" +
                   $"• Inventario: {TienePermiso("Inventario")}\n" +
                   $"• POS: {TienePermiso("POS")}\n" +
                   $"• Reportes: {TienePermiso("Reportes")}\n" +
                   $"• Usuarios: {TienePermiso("Usuarios")}\n" +
                   $"• Eliminación: {TienePermiso("Eliminacion")}\n" +
                   $"• Corte Caja: {TienePermiso("CorteCaja")}";
        }
    }
}