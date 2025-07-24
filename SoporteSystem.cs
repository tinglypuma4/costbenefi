using System;
using System.Collections.Generic;
using costbenefi.Models;

namespace costbenefi.Services
{
    /// <summary>
    /// Sistema de usuarios especiales de soporte técnico
    /// Estos usuarios tienen acceso total pero no existen en la base de datos
    /// </summary>
    public static class SoporteSystem
    {
        // ===== 🔧 CONFIGURACIÓN CORREGIDA DE USUARIOS SOPORTE =====

        private static readonly Dictionary<string, UsuarioSoporte> _usuariosSoporte = new()
        {
            // ✅ CORREGIDO: Key del diccionario = NombreUsuario
            ["soporte"] = new UsuarioSoporte
            {
                NombreUsuario = "soporte",                              // ✅ Coincide con la key
                NombreCompleto = "Administrador de Soporte Técnico",
                Email = "soporte@costbenefi.com",
                PasswordHash = User.GenerarHashPassword("soporte2025"), // ✅ Tu contraseña
                Rol = "Soporte",
                Nivel = NivelSoporte.SuperAdmin,
                Descripcion = "Acceso total al sistema para soporte técnico"
            },

            // Usuario de desarrollo (opcional)
            ["dev_access"] = new UsuarioSoporte
            {
                NombreUsuario = "dev_access",
                NombreCompleto = "Acceso de Desarrollo",
                Email = "dev@costbenefi.com",
                PasswordHash = User.GenerarHashPassword("DevAccess2025!"),
                Rol = "Soporte",
                Nivel = NivelSoporte.Desarrollo,
                Descripcion = "Acceso para desarrollo y pruebas"
            }
        };

        // ===== MÉTODOS PÚBLICOS =====

        /// <summary>
        /// Verifica si un nombre de usuario es un usuario de soporte
        /// </summary>
        public static bool EsUsuarioSoporte(string nombreUsuario)
        {
            var userKey = nombreUsuario?.Trim().ToLower() ?? "";
            var esUsuarioSoporte = _usuariosSoporte.ContainsKey(userKey);

            // 🔧 DEBUG: Agregar logs para diagnóstico
            System.Diagnostics.Debug.WriteLine($"🔧 EsUsuarioSoporte('{nombreUsuario}') -> Key: '{userKey}' -> Resultado: {esUsuarioSoporte}");

            return esUsuarioSoporte;
        }

        /// <summary>
        /// Autentica un usuario de soporte
        /// </summary>
        public static (bool Exito, string Mensaje, User? Usuario) AutenticarSoporte(string nombreUsuario, string password)
        {
            try
            {
                var userKey = nombreUsuario?.Trim().ToLower() ?? "";

                // 🔧 DEBUG: Logs para diagnóstico
                System.Diagnostics.Debug.WriteLine($"🔧 AutenticarSoporte - Usuario: '{nombreUsuario}' -> Key: '{userKey}'");
                System.Diagnostics.Debug.WriteLine($"🔧 Usuarios disponibles: {string.Join(", ", _usuariosSoporte.Keys)}");

                if (!_usuariosSoporte.TryGetValue(userKey, out var usuarioSoporte))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Usuario soporte '{userKey}' no encontrado en diccionario");
                    return (false, "Usuario de soporte no encontrado", null);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Usuario soporte encontrado: {usuarioSoporte.NombreCompleto}");
                System.Diagnostics.Debug.WriteLine($"🔧 Verificando contraseña...");

                if (!User.VerificarPassword(password, usuarioSoporte.PasswordHash))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Contraseña incorrecta para usuario soporte");
                    return (false, "Contraseña incorrecta para usuario soporte", null);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Contraseña correcta - Creando usuario temporal");

                // Crear objeto User temporal para compatibilidad con el sistema
                var userTemporal = new User
                {
                    Id = -1, // ID especial para usuarios soporte
                    NombreUsuario = usuarioSoporte.NombreUsuario,
                    NombreCompleto = usuarioSoporte.NombreCompleto,
                    Email = usuarioSoporte.Email,
                    Rol = usuarioSoporte.Rol,
                    Activo = true,
                    UltimoAcceso = DateTime.Now,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreador = "Sistema"
                };

                System.Diagnostics.Debug.WriteLine($"🎉 Usuario soporte autenticado exitosamente: {userTemporal.NombreCompleto}");

                return (true, $"Bienvenido {usuarioSoporte.NombreCompleto} - Acceso de Soporte", userTemporal);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en AutenticarSoporte: {ex.Message}");
                return (false, $"Error en autenticación de soporte: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Verifica si el usuario actual es de soporte
        /// </summary>
        public static bool UsuarioActualEsSoporte()
        {
            var esSoporte = UserService.UsuarioActual?.Id == -1 && UserService.UsuarioActual?.Rol == "Soporte";
            System.Diagnostics.Debug.WriteLine($"🔧 UsuarioActualEsSoporte: {esSoporte}");
            return esSoporte;
        }

        /// <summary>
        /// Obtiene información del usuario soporte actual
        /// </summary>
        public static UsuarioSoporte? ObtenerUsuarioSoporteActual()
        {
            if (!UsuarioActualEsSoporte()) return null;

            var nombreUsuario = UserService.UsuarioActual?.NombreUsuario?.ToLower() ?? "";
            return _usuariosSoporte.TryGetValue(nombreUsuario, out var usuario) ? usuario : null;
        }

        /// <summary>
        /// Lista todos los usuarios de soporte (para administración)
        /// </summary>
        public static List<UsuarioSoporte> ListarUsuariosSoporte()
        {
            // Solo permitir si el usuario actual ya es soporte
            if (!UsuarioActualEsSoporte())
                return new List<UsuarioSoporte>();

            return new List<UsuarioSoporte>(_usuariosSoporte.Values);
        }

        /// <summary>
        /// Verifica si el usuario tiene permisos de super administrador
        /// </summary>
        public static bool TieneAccesoSuperAdmin()
        {
            var usuarioSoporte = ObtenerUsuarioSoporteActual();
            return usuarioSoporte?.Nivel == NivelSoporte.SuperAdmin;
        }
    }

    /// <summary>
    /// Clase para usuarios especiales de soporte
    /// </summary>
    public class UsuarioSoporte
    {
        public string NombreUsuario { get; set; } = "";
        public string NombreCompleto { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Rol { get; set; } = "Soporte";
        public NivelSoporte Nivel { get; set; } = NivelSoporte.Basico;
        public string Descripcion { get; set; } = "";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Permisos completos incluyendo los de Dueño
        /// </summary>
        public List<string> Permisos => new List<string>
        {
            "POS", "Inventario", "Ventas", "Reportes", "Usuarios",
            "Configuracion", "CorteCaja", "Eliminacion", 
            // Permisos especiales de soporte
            "SoporteTecnico", "AccesoTotal", "DebugMode", "DatabaseAccess"
        };

        /// <summary>
        /// Descripción del rol para mostrar en UI
        /// </summary>
        public string RolDescripcion => "Acceso completo de soporte técnico";

        /// <summary>
        /// Verifica si tiene un permiso específico (siempre true para soporte)
        /// </summary>
        public bool TienePermiso(string permiso) => true;
    }

    /// <summary>
    /// Niveles de acceso para usuarios soporte
    /// </summary>
    public enum NivelSoporte
    {
        Basico = 1,
        Avanzado = 2,
        Desarrollo = 3,
        SuperAdmin = 4
    }
}