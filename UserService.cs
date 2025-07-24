using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio para gestión de usuarios y autenticación
    /// VERSIÓN ULTRA CONSERVADORA - Solo agrega soporte SIN CAMBIAR FIRMAS
    /// </summary>
    public class UserService : IDisposable
    {
        private readonly AppDbContext _context;

        // Usuario actualmente logueado
        public static User? UsuarioActual { get; private set; }
        public static UserSession? SesionActual { get; private set; }

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // ===== 🔧 ÚNICO MÉTODO MODIFICADO: AutenticarAsync =====
        /// <summary>
        /// Autentica un usuario en el sistema (CON DETECCIÓN AUTOMÁTICA DE SOPORTE)
        /// </summary>
        public async Task<(bool Exito, string Mensaje, User? Usuario)> AutenticarAsync(string nombreUsuario, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 === INICIANDO AUTENTICACIÓN ===");
                System.Diagnostics.Debug.WriteLine($"🔐 Usuario: '{nombreUsuario}'");

                // ===== 🔧 NUEVA FUNCIONALIDAD: DETECCIÓN AUTOMÁTICA DE USUARIOS SOPORTE =====
                System.Diagnostics.Debug.WriteLine($"🔧 Verificando si es usuario soporte...");

                if (SoporteSystem.EsUsuarioSoporte(nombreUsuario))
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 ¡ES USUARIO SOPORTE! Autenticando...");

                    var resultadoSoporte = SoporteSystem.AutenticarSoporte(nombreUsuario, password);

                    if (resultadoSoporte.Exito && resultadoSoporte.Usuario != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ SOPORTE AUTENTICADO: {resultadoSoporte.Usuario.NombreCompleto}");

                        // Establecer usuario soporte como actual (sin crear sesión en BD)
                        UsuarioActual = resultadoSoporte.Usuario;
                        SesionActual = null; // Los usuarios soporte no tienen sesiones en BD

                        return (true, $"{resultadoSoporte.Mensaje} 🔧", resultadoSoporte.Usuario);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ SOPORTE FALLÓ: {resultadoSoporte.Mensaje}");
                        return (false, resultadoSoporte.Mensaje, null);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 NO es usuario soporte, buscando en BD...");
                }

                // ===== AUTENTICACIÓN NORMAL DE USUARIOS BD (SIN CAMBIOS) =====
                System.Diagnostics.Debug.WriteLine($"🔍 Llamando a GetUserByUsernameAsync...");

                var usuario = await _context.GetUserByUsernameAsync(nombreUsuario);

                if (usuario == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Usuario '{nombreUsuario}' no encontrado en BD");
                    return (false, "Usuario no encontrado", null);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Usuario encontrado: {usuario.NombreCompleto} ({usuario.Rol})");

                if (!usuario.Activo)
                {
                    return (false, "Usuario inactivo", null);
                }

                if (usuario.EstaBloqueado)
                {
                    var minutos = Math.Ceiling((usuario.FechaBloqueado!.Value - DateTime.Now).TotalMinutes);
                    return (false, $"Usuario bloqueado por {minutos} minutos más", null);
                }

                if (!User.VerificarPassword(password, usuario.PasswordHash))
                {
                    usuario.RegistrarIntentoFallido();
                    await _context.SaveChangesAsync();

                    var mensaje = $"Contraseña incorrecta. Intentos restantes: {5 - usuario.IntentosFallidos}";
                    if (usuario.IntentosFallidos >= 5)
                    {
                        mensaje = "Usuario bloqueado por 30 minutos debido a múltiples intentos fallidos";
                    }

                    return (false, mensaje, null);
                }

                // Login exitoso
                usuario.RegistrarAccesoExitoso();

                // Crear sesión
                var sesion = UserSession.CrearSesion(usuario.Id);
                _context.UserSessions.Add(sesion);

                await _context.SaveChangesAsync();

                // Establecer usuario y sesión actuales
                UsuarioActual = usuario;
                SesionActual = sesion;

                return (true, $"Bienvenido, {usuario.NombreCompleto}!", usuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR EN AUTENTICACIÓN: {ex.Message}");
                return (false, $"Error de autenticación: {ex.Message}", null);
            }
        }

        // ===== 🔧 MÉTODOS CON SOPORTE AGREGADO (MANTENIENDO FIRMAS EXACTAS) =====

        /// <summary>
        /// Cierra la sesión actual
        /// </summary>
        public async Task<bool> CerrarSesionAsync(string motivo = "Cierre normal")
        {
            try
            {
                // ✅ NUEVO: Si es usuario soporte, solo limpiar variables
                if (SoporteSystem.UsuarioActualEsSoporte())
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 Cerrando sesión de soporte");
                    UsuarioActual = null;
                    SesionActual = null;
                    return true;
                }

                // Para usuarios normales, cerrar sesión en BD (SIN CAMBIOS)
                if (SesionActual != null)
                {
                    SesionActual.CerrarSesion(motivo);
                    await _context.SaveChangesAsync();
                }

                UsuarioActual = null;
                SesionActual = null;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si el usuario actual tiene un permiso específico
        /// </summary>
        public static bool TienePermiso(string permiso)
        {
            // ✅ NUEVO: Los usuarios soporte siempre tienen todos los permisos
            if (SoporteSystem.UsuarioActualEsSoporte())
                return true;

            return UsuarioActual?.TienePermiso(permiso) ?? false;
        }

        /// <summary>
        /// Verifica si el usuario actual puede gestionar usuarios
        /// </summary>
        public static bool PuedeGestionarUsuarios()
        {
            // ✅ NUEVO: Usuarios soporte siempre pueden
            if (SoporteSystem.UsuarioActualEsSoporte())
                return true;

            return UsuarioActual?.Rol == "Dueño";
        }

        /// <summary>
        /// Actualiza la actividad de la sesión actual
        /// </summary>
        public async Task ActualizarActividadAsync()
        {
            try
            {
                // Solo para usuarios normales (no soporte)
                if (!SoporteSystem.UsuarioActualEsSoporte() && SesionActual != null)
                {
                    SesionActual.ActualizarActividad();
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Silencioso - no es crítico si falla
            }
        }

        // ===== 🔧 MÉTODOS ORIGINALES CON SOPORTE AGREGADO (MANTENIENDO FIRMAS EXACTAS) =====

        /// <summary>
        /// Crea un nuevo usuario
        /// ✅ FIRMAE EXACTA MANTENIDA: (bool exito, string mensaje, User? usuario)
        /// </summary>
        public async Task<(bool exito, string mensaje, User? usuario)> CrearUsuarioAsync(
            string nombreUsuario, string nombreCompleto, string email,
            string password, string rol, string telefono = "")
        {
            try
            {
                // Validaciones básicas (SIN CAMBIOS)
                if (string.IsNullOrWhiteSpace(nombreUsuario))
                    return (false, "El nombre de usuario es requerido", null);

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "La contraseña debe tener al menos 6 caracteres", null);

                if (!User.EsEmailValido(email))
                    return (false, "Formato de email inválido", null);

                if (!User.RolesDisponibles.Contains(rol))
                    return (false, "Rol no válido para el sistema", null);

                // ✅ MODIFICADO: Solo el Dueño O usuarios soporte pueden crear usuarios
                bool puedeCrear = UsuarioActual?.Rol == "Dueño" || SoporteSystem.UsuarioActualEsSoporte();

                if (!puedeCrear)
                    return (false, "Solo el Dueño puede crear usuarios", null);

                // ✅ MODIFICADO: Validación especial para crear Dueño (ignorar usuarios soporte)
                if (rol == "Dueño")
                {
                    var existeDuenoReal = await _context.Users
                        .Where(u => u.Id > 0) // Solo usuarios reales, no soporte
                        .AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);

                    if (existeDuenoReal)
                        return (false, "Ya existe un Dueño en el sistema. Solo puede haber uno.", null);
                }

                // Verificar duplicados (SIN CAMBIOS)
                if (await _context.ExisteNombreUsuarioAsync(nombreUsuario))
                    return (false, "El nombre de usuario ya existe", null);

                if (await _context.ExisteEmailAsync(email))
                    return (false, "El email ya está registrado", null);

                // Crear usuario (SIN CAMBIOS)
                var nuevoUsuario = new User
                {
                    NombreUsuario = nombreUsuario.Trim().ToLower(),
                    NombreCompleto = nombreCompleto.Trim(),
                    Email = email.Trim().ToLower(),
                    PasswordHash = User.GenerarHashPassword(password),
                    Rol = rol,
                    Telefono = telefono.Trim(),
                    UsuarioCreador = UsuarioActual?.NombreUsuario ?? "Soporte"
                };

                _context.Users.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                return (true, $"Usuario '{nombreCompleto}' creado exitosamente", nuevoUsuario);
            }
            catch (Exception ex)
            {
                return (false, $"Error al crear usuario: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// ✅ FIRMA EXACTA MANTENIDA: (bool exito, string mensaje)
        /// </summary>
        public async Task<(bool exito, string mensaje)> ActualizarUsuarioAsync(
            int userId, string nombreCompleto, string email, string rol,
            string telefono = "", string? nuevaPassword = null)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(userId);
                if (usuario == null)
                    return (false, "Usuario no encontrado");

                // ✅ MODIFICADO: Solo el Dueño O usuarios soporte pueden actualizar usuarios
                bool puedeActualizar = UsuarioActual?.Rol == "Dueño" || SoporteSystem.UsuarioActualEsSoporte();

                if (!puedeActualizar)
                    return (false, "Solo el Dueño puede actualizar usuarios");

                // Resto de validaciones (SIN CAMBIOS)
                if (!User.RolesDisponibles.Contains(rol))
                    return (false, "Rol no válido para el sistema");

                // Verificar email duplicado
                if (await _context.ExisteEmailAsync(email, userId))
                    return (false, "El email ya está registrado por otro usuario");

                // Actualizar campos
                usuario.NombreCompleto = nombreCompleto.Trim();
                usuario.Email = email.Trim().ToLower();
                usuario.Rol = rol;
                usuario.Telefono = telefono.Trim();

                // Actualizar contraseña si se proporcionó
                if (!string.IsNullOrWhiteSpace(nuevaPassword))
                {
                    if (nuevaPassword.Length < 6)
                        return (false, "La contraseña debe tener al menos 6 caracteres");

                    usuario.PasswordHash = User.GenerarHashPassword(nuevaPassword);
                }

                await _context.SaveChangesAsync();

                return (true, $"Usuario '{nombreCompleto}' actualizado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Cambia el estado activo/inactivo de un usuario
        /// ✅ FIRMA EXACTA MANTENIDA: (bool exito, string mensaje)
        /// </summary>
        public async Task<(bool exito, string mensaje)> CambiarEstadoUsuarioAsync(int userId, bool activo)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(userId);
                if (usuario == null)
                    return (false, "Usuario no encontrado");

                // ✅ MODIFICADO: Solo el Dueño O usuarios soporte pueden cambiar estados
                bool puedeCambiar = UsuarioActual?.Rol == "Dueño" || SoporteSystem.UsuarioActualEsSoporte();

                if (!puedeCambiar)
                    return (false, "Solo el Dueño puede cambiar el estado de usuarios");

                usuario.Activo = activo;
                await _context.SaveChangesAsync();

                var accion = activo ? "activado" : "desactivado";
                return (true, $"Usuario {accion} exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar estado: {ex.Message}");
            }
        }

        /// <summary>
        /// Desbloquea un usuario manualmente
        /// ✅ FIRMA EXACTA MANTENIDA: (bool exito, string mensaje)
        /// </summary>
        public async Task<(bool exito, string mensaje)> DesbloquearUsuarioAsync(int userId)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(userId);
                if (usuario == null)
                    return (false, "Usuario no encontrado");

                // ✅ MODIFICADO: Solo el Dueño O usuarios soporte pueden desbloquear usuarios
                bool puedeDesbloquear = UsuarioActual?.Rol == "Dueño" || SoporteSystem.UsuarioActualEsSoporte();

                if (!puedeDesbloquear)
                    return (false, "Solo el Dueño puede desbloquear usuarios");

                usuario.Desbloquear();
                await _context.SaveChangesAsync();

                return (true, "Usuario desbloqueado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al desbloquear usuario: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los usuarios activos (excluyendo usuarios soporte)
        /// </summary>
        public async Task<List<User>> ObtenerUsuariosAsync(bool incluirInactivos = false)
        {
            var query = _context.Users
                .Where(u => u.Id > 0 && !u.Eliminado); // ✅ MODIFICADO: Solo usuarios reales, no soporte

            if (!incluirInactivos)
                query = query.Where(u => u.Activo);

            return await query
                .OrderBy(u => u.Rol == "Dueño" ? 0 : u.Rol == "Encargado" ? 1 : 2)
                .ThenBy(u => u.NombreCompleto)
                .ToListAsync();
        }

        /// <summary>
        /// Restaura el usuario actual desde la base de datos
        /// </summary>
        public static async Task<bool> RestaurarUsuarioActualAsync()
        {
            try
            {
                if (UsuarioActual != null) return true;

                using var context = new AppDbContext();

                var sesionActiva = await context.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.FechaCierre == null && s.UserId > 0) // ✅ MODIFICADO: Excluir sesiones de soporte
                    .OrderByDescending(s => s.UltimaActividad)
                    .FirstOrDefaultAsync();

                if (sesionActiva?.User != null)
                {
                    UsuarioActual = sesionActiva.User;
                    SesionActual = sesionActiva;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // ===== AGREGAR CUALQUIER OTRO MÉTODO QUE TENGAS EN TU USERSERVICE ORIGINAL =====
        // (Mantener TODAS las firmas exactas)

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}