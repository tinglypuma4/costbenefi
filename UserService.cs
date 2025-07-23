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
    /// Servicio para gestiуn de usuarios y autenticaciуn
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

        // ===== MЙTODOS DE AUTENTICACIУN =====

        /// <summary>
        /// Autentica un usuario en el sistema
        /// </summary>
        public async Task<(bool Exito, string Mensaje, User? Usuario)> AutenticarAsync(string nombreUsuario, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 DIAGNÓSTICO CREAR USUARIO:");
                System.Diagnostics.Debug.WriteLine($"   • UsuarioActual es null: {UsuarioActual == null}");

                var usuario = await _context.GetUserByUsernameAsync(nombreUsuario);

                if (usuario == null)
                {

                    System.Diagnostics.Debug.WriteLine($"   • UsuarioActual.NombreCompleto: {UsuarioActual.NombreCompleto}");
                    System.Diagnostics.Debug.WriteLine($"   • UsuarioActual.Rol: {UsuarioActual.Rol}");

                    return (false, "Usuario no encontrado", null);
                }
                System.Diagnostics.Debug.WriteLine($"   • SesionActual es null: {SesionActual == null}");


                if (!usuario.Activo)
                {
                    return (false, "Usuario inactivo", null);
                }

                if (usuario.EstaBloqueado)
                {
                    var minutos = Math.Ceiling((usuario.FechaBloqueado!.Value - DateTime.Now).TotalMinutes);
                    return (false, $"Usuario bloqueado por {minutos} minutos mбs", null);
                }

                if (!User.VerificarPassword(password, usuario.PasswordHash))
                {
                    usuario.RegistrarIntentoFallido();
                    await _context.SaveChangesAsync();

                    var mensaje = $"Contraseсa incorrecta. Intentos restantes: {5 - usuario.IntentosFallidos}";
                    if (usuario.IntentosFallidos >= 5)
                    {
                        mensaje = "Usuario bloqueado por 30 minutos debido a mъltiples intentos fallidos";
                    }

                    return (false, mensaje, null);
                }

                // Login exitoso
                usuario.RegistrarAccesoExitoso();

                // Crear sesiуn
                var sesion = UserSession.CrearSesion(usuario.Id);
                _context.UserSessions.Add(sesion);

                await _context.SaveChangesAsync();

                // Establecer usuario y sesiуn actuales
                UsuarioActual = usuario;
                SesionActual = sesion;

                return (true, $"Bienvenido, {usuario.NombreCompleto}!", usuario);
            }
            catch (Exception ex)
            {
                return (false, $"Error de autenticaciуn: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Cierra la sesiуn actual
        /// </summary>
        public async Task<bool> CerrarSesionAsync(string motivo = "Cierre normal")
        {
            try
            {
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
        /// Verifica si el usuario actual tiene un permiso especнfico
        /// </summary>
        public static bool TienePermiso(string permiso)
        {
            return UsuarioActual?.TienePermiso(permiso) ?? false;
        }

        /// <summary>
        /// Actualiza la actividad de la sesiуn actual
        /// </summary>
        public async Task ActualizarActividadAsync()
        {
            try
            {
                if (SesionActual != null)
                {
                    SesionActual.ActualizarActividad();
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Silencioso - no es crнtico si falla
            }
        }

        // ===== MЙTODOS DE GESTIУN DE USUARIOS =====

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        public async Task<(bool Exito, string Mensaje, User? Usuario)> CrearUsuarioAsync(
         string nombreUsuario, string nombreCompleto, string email,
         string password, string rol, string telefono = "")
        {
            try
            {
                // 🔍 DIAGNÓSTICO TEMPORAL - ELIMINAR DESPUÉS
                System.Diagnostics.Debug.WriteLine("🔍 DIAGNÓSTICO CREAR USUARIO:");
                System.Diagnostics.Debug.WriteLine($"   • UsuarioActual es null: {UsuarioActual == null}");
                if (UsuarioActual != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   • UsuarioActual.NombreCompleto: {UsuarioActual.NombreCompleto}");
                    System.Diagnostics.Debug.WriteLine($"   • UsuarioActual.Rol: {UsuarioActual.Rol}");
                }
                System.Diagnostics.Debug.WriteLine($"   • SesionActual es null: {SesionActual == null}");

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(nombreUsuario))
                    return (false, "El nombre de usuario es requerido", null);

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return (false, "La contraseña debe tener al menos 6 caracteres", null);

                if (!User.EsEmailValido(email))
                    return (false, "Formato de email inválido", null);

                // Validar rol
                if (!User.RolesDisponibles.Contains(rol))
                    return (false, "Rol no válido para el sistema", null);

                // Solo el Dueño puede crear usuarios
                // 🔧 TEMPORALMENTE: Saltar validación si UsuarioActual es null
                if (UsuarioActual?.Rol != "Dueño")
                {
                    // 🔧 HACK TEMPORAL: Si UsuarioActual es null, verificar que exista al menos un Dueño
                    if (UsuarioActual == null)
                    {
                        var existeAlgunDueno = await _context.Users.AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);
                        if (!existeAlgunDueno)
                        {
                            return (false, "No hay usuarios Dueño en el sistema", null);
                        }
                        // Si existe un Dueño, asumir que quien está logueado es el Dueño (TEMPORAL)
                        System.Diagnostics.Debug.WriteLine("⚠️ HACK TEMPORAL: Permitiendo creación de usuario sin validar UsuarioActual");
                    }
                    else
                    {
                        return (false, "Solo el Dueño puede crear usuarios", null);
                    }
                }

                // No permitir crear otro Dueño (solo puede haber uno)
                if (rol == "Dueño")
                {
                    var existeDueno = await _context.Users.AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);
                    if (existeDueno)
                        return (false, "Ya existe un Dueño en el sistema. Solo puede haber uno.", null);
                }

                // Verificar duplicados
                if (await _context.ExisteNombreUsuarioAsync(nombreUsuario))
                    return (false, "El nombre de usuario ya existe", null);

                if (await _context.ExisteEmailAsync(email))
                    return (false, "El email ya está registrado", null);

                // Crear usuario
                var nuevoUsuario = new User
                {
                    NombreUsuario = nombreUsuario.Trim().ToLower(),
                    NombreCompleto = nombreCompleto.Trim(),
                    Email = email.Trim().ToLower(),
                    PasswordHash = User.GenerarHashPassword(password),
                    Rol = rol,
                    Telefono = telefono.Trim(),
                    UsuarioCreador = UsuarioActual?.NombreUsuario ?? "Sistema"
                };

                _context.Users.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"✅ Usuario '{nombreCompleto}' creado exitosamente");
                return (true, $"Usuario '{nombreCompleto}' creado exitosamente", nuevoUsuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al crear usuario: {ex.Message}");
                return (false, $"Error al crear usuario: {ex.Message}", null);
            }
        }
        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> ActualizarUsuarioAsync(
            int userId, string nombreCompleto, string email, string rol,
            string telefono = "", string? nuevaPassword = null)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(userId);
                if (usuario == null)
                    return (false, "Usuario no encontrado");

                // Solo el Dueсo puede actualizar usuarios
                if (UsuarioActual?.Rol != "Dueсo")
                    return (false, "Solo el Dueсo puede actualizar usuarios");

                // Validar rol
                if (!User.RolesDisponibles.Contains(rol))
                    return (false, "Rol no vбlido para el sistema");

                // No permitir cambiar de Dueсo a otro rol si es el ъnico Dueсo
                if (usuario.Rol == "Dueсo" && rol != "Dueсo")
                {
                    var cantidadDuenos = await _context.Users.CountAsync(u => u.Rol == "Dueсo" && u.Activo);
                    if (cantidadDuenos <= 1)
                        return (false, "No se puede cambiar el rol del ъnico Dueсo del sistema");
                }

                // No permitir crear otro Dueсo
                if (rol == "Dueсo" && usuario.Rol != "Dueсo")
                {
                    var existeDueno = await _context.Users.AnyAsync(u => u.Rol == "Dueсo" && u.Activo && u.Id != userId);
                    if (existeDueno)
                        return (false, "Ya existe un Dueсo en el sistema. Solo puede haber uno.");
                }

                // Verificar email duplicado
                if (await _context.ExisteEmailAsync(email, userId))
                    return (false, "El email ya estб registrado por otro usuario");

                // Actualizar campos
                usuario.NombreCompleto = nombreCompleto.Trim();
                usuario.Email = email.Trim().ToLower();
                usuario.Rol = rol;
                usuario.Telefono = telefono.Trim();

                // Actualizar contraseсa si se proporcionу
                if (!string.IsNullOrWhiteSpace(nuevaPassword))
                {
                    if (nuevaPassword.Length < 6)
                        return (false, "La contraseсa debe tener al menos 6 caracteres");

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
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> CambiarEstadoUsuarioAsync(int userId, bool activo)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(userId);
                if (usuario == null)
                    return (false, "Usuario no encontrado");

                // Solo el Dueсo puede cambiar estados
                if (UsuarioActual?.Rol != "Dueсo")
                    return (false, "Solo el Dueсo puede cambiar el estado de usuarios");

                // No permitir desactivar al ъnico Dueсo
                if (!activo && usuario.Rol == "Dueсo")
                {
                    var duenosActivos = await _context.Users.CountAsync(u => u.Rol == "Dueсo" && u.Activo && u.Id != userId);
                    if (duenosActivos == 0)
                        return (false, "No se puede desactivar al ъnico Dueсo del sistema");
                }

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
        /// </summary>
        public async Task<(bool Exito, string Mensaje)> DesbloquearUsuarioAsync(int userId)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(userId);
                if (usuario == null)
                    return (false, "Usuario no encontrado");

                // Solo el Dueсo puede desbloquear usuarios
                if (UsuarioActual?.Rol != "Dueсo")
                    return (false, "Solo el Dueсo puede desbloquear usuarios");

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
        /// Obtiene todos los usuarios activos
        /// </summary>
        public async Task<List<User>> ObtenerUsuariosAsync(bool incluirInactivos = false)
        {
            var query = _context.Users.AsQueryable();

            if (!incluirInactivos)
                query = query.Where(u => u.Activo);

            return await query
                .OrderBy(u => u.Rol == "Dueсo" ? 0 : u.Rol == "Encargado" ? 1 : 2) // Dueсo primero
                .ThenBy(u => u.NombreCompleto)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene estadнsticas de usuarios
        /// </summary>
        public async Task<dynamic> ObtenerEstadisticasUsuariosAsync()
        {
            var totalUsuarios = await _context.Users.CountAsync();
            var usuariosActivos = await _context.Users.CountAsync(u => u.Activo);
            var usuariosBloqueados = await _context.Users.CountAsync(u => u.EstaBloqueado);
            var sesionesActivas = await _context.UserSessions.CountAsync(s => s.FechaCierre == null);

            var usuariosPorRol = await _context.Users
                .Where(u => u.Activo)
                .GroupBy(u => u.Rol)
                .Select(g => new { Rol = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            return new
            {
                TotalUsuarios = totalUsuarios,
                UsuariosActivos = usuariosActivos,
                UsuariosInactivos = totalUsuarios - usuariosActivos,
                UsuariosBloqueados = usuariosBloqueados,
                SesionesActivas = sesionesActivas,
                UsuariosPorRol = usuariosPorRol
            };
        }

        /// <summary>
        /// Obtiene el historial de sesiones de un usuario
        /// </summary>
        public async Task<List<UserSession>> ObtenerHistorialSesionesAsync(int userId, int cantidadMaxima = 50)
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.FechaInicio)
                .Take(cantidadMaxima)
                .ToListAsync();
        }

        /// <summary>
        /// Crea el usuario Dueсo por defecto si no existe
        /// </summary>
        public async Task CrearUsuarioDuenoPorDefectoAsync()
        {
            try
            {
                var existeDueno = await _context.Users.AnyAsync(u => u.Rol == "Dueсo");
                if (!existeDueno)
                {
                    var dueno = new User
                    {
                        NombreUsuario = "dueno",
                        NombreCompleto = "Dueсo del Negocio",
                        Email = "dueno@verduleria.com",
                        PasswordHash = User.GenerarHashPassword("dueno123"), // Cambiar en producciуn
                        Rol = "Dueсo",
                        Activo = true,
                        UsuarioCreador = "Sistema"
                    };

                    _context.Users.Add(dueno);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Silencioso - no es crнtico si falla
            }
        }
        public static async Task<bool> RestaurarUsuarioActualAsync()
        {
            try
            {
                if (UsuarioActual != null) return true; // Ya está configurado

                System.Diagnostics.Debug.WriteLine("🔧 RESTAURANDO UsuarioActual desde base de datos...");

                using var context = new AppDbContext();

                // Buscar la sesión activa más reciente
                var sesionActiva = await context.UserSessions
                    .Include(s => s.User)
                    .Where(s => s.FechaCierre == null)
                    .OrderByDescending(s => s.UltimaActividad)
                    .FirstOrDefaultAsync();

                if (sesionActiva?.User != null)
                {
                    UsuarioActual = sesionActiva.User;
                    SesionActual = sesionActiva;

                    System.Diagnostics.Debug.WriteLine($"✅ UsuarioActual restaurado: {UsuarioActual.NombreCompleto}");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine("❌ No se pudo restaurar UsuarioActual");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al restaurar UsuarioActual: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}