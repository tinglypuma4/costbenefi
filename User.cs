using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        /// <summary>
        /// Rol del usuario: Dueño, Encargado, Cajero
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Rol { get; set; } = "Cajero";

        /// <summary>
        /// Usuario está activo en el sistema
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Fecha de último acceso al sistema
        /// </summary>
        public DateTime? UltimoAcceso { get; set; }

        /// <summary>
        /// Intentos fallidos de login consecutivos
        /// </summary>
        public int IntentosFallidos { get; set; } = 0;

        /// <summary>
        /// Fecha hasta la cual está bloqueado (si aplica)
        /// </summary>
        public DateTime? FechaBloqueado { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string UsuarioCreador { get; set; } = string.Empty;

        // ========== ✅ AGREGADO: PROPIEDADES DE ELIMINACIÓN LÓGICA ==========
        /// <summary>
        /// Indica si el usuario ha sido eliminado lógicamente
        /// </summary>
        public bool Eliminado { get; set; } = false;

        /// <summary>
        /// Fecha en que fue eliminado el usuario
        /// </summary>
        public DateTime? FechaEliminacion { get; set; }

        /// <summary>
        /// Usuario que realizó la eliminación
        /// </summary>
        [StringLength(100)]
        public string? UsuarioEliminacion { get; set; }

        /// <summary>
        /// Motivo de la eliminación
        /// </summary>
        [StringLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ===== PROPIEDADES CALCULADAS EXISTENTES =====

        [NotMapped]
        public bool EstaBloqueado => FechaBloqueado.HasValue && FechaBloqueado > DateTime.Now;

        [NotMapped]
        public bool RequiereCambioPassword { get; set; } = false;

        [NotMapped]
        public string EstadoUsuario
        {
            get
            {
                if (Eliminado) return "Eliminado";
                if (!Activo) return "Inactivo";
                if (EstaBloqueado) return "Bloqueado";
                if (IntentosFallidos >= 3) return "Advertencia";
                return "Activo";
            }
        }

        [NotMapped]
        public List<string> Permisos
        {
            get
            {
                return Rol switch
                {
                    "Dueño" => new List<string>
                    {
                        "POS", "Inventario", "Ventas", "Reportes",
                        "Usuarios", "Configuracion", "CorteCaja", "Eliminacion"
                    },
                    "Encargado" => new List<string>
                    {
                        "POS", "Inventario", "Ventas", "Reportes", "CorteCaja"
                    },
                    "Cajero" => new List<string>
                    {
                        "POS", "CorteCaja", "InventarioBasico"
                    },
                    _ => new List<string> { "POS" }
                };
            }
        }

        [NotMapped]
        public string RolDescripcion
        {
            get
            {
                return Rol switch
                {
                    "Dueño" => "Control total del negocio",
                    "Encargado" => "Maneja operaciones cuando no está el dueño",
                    "Cajero" => "Atiende clientes y maneja caja",
                    _ => "Usuario básico"
                };
            }
        }

        [NotMapped]
        public static List<string> RolesDisponibles => new() { "Dueño", "Encargado", "Cajero" };

        // ========== ✅ AGREGADO: PROPIEDADES PARA COMPATIBILIDAD CON CÓDIGO EXISTENTE ==========
        [NotMapped]
        public bool IsDeleted
        {
            get => Eliminado;
            set => Eliminado = value;
        }

        [NotMapped]
        public DateTime? DeletedAt
        {
            get => FechaEliminacion;
            set => FechaEliminacion = value;
        }

        [NotMapped]
        public string? DeletedBy
        {
            get => UsuarioEliminacion;
            set => UsuarioEliminacion = value;
        }

        // ===== MÉTODOS EXISTENTES =====

        /// <summary>
        /// Verifica si el usuario tiene un permiso específico
        /// </summary>
        public bool TienePermiso(string permiso)
        {
            return Activo && !EstaBloqueado && !Eliminado && Permisos.Contains(permiso);
        }

        /// <summary>
        /// Verifica si puede gestionar usuarios (solo Dueño)
        /// </summary>
        public bool PuedeGestionarUsuarios()
        {
            return TienePermiso("Usuarios");
        }

        /// <summary>
        /// Verifica si puede ver reportes completos
        /// </summary>
        public bool PuedeVerReportes()
        {
            return TienePermiso("Reportes");
        }

        /// <summary>
        /// Verifica si puede gestionar inventario completo
        /// </summary>
        public bool PuedeGestionarInventario()
        {
            return TienePermiso("Inventario");
        }

        /// <summary>
        /// Registra acceso exitoso
        /// </summary>
        public void RegistrarAccesoExitoso()
        {
            UltimoAcceso = DateTime.Now;
            IntentosFallidos = 0;
            FechaBloqueado = null;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Registra intento fallido de login
        /// </summary>
        public void RegistrarIntentoFallido()
        {
            IntentosFallidos++;
            FechaActualizacion = DateTime.Now;

            // Bloquear después de 5 intentos por 30 minutos
            if (IntentosFallidos >= 5)
            {
                FechaBloqueado = DateTime.Now.AddMinutes(30);
            }
        }

        /// <summary>
        /// Desbloquea el usuario manualmente
        /// </summary>
        public void Desbloquear()
        {
            IntentosFallidos = 0;
            FechaBloqueado = null;
            FechaActualizacion = DateTime.Now;
        }

        // ========== ✅ AGREGADO: MÉTODOS DE ELIMINACIÓN LÓGICA ==========
        /// <summary>
        /// Marca el usuario como eliminado lógicamente
        /// </summary>
        public void MarcarComoEliminado(string usuarioEliminacion, string motivo = "Eliminación manual")
        {
            Eliminado = true;
            FechaEliminacion = DateTime.Now;
            UsuarioEliminacion = usuarioEliminacion;
            MotivoEliminacion = motivo;
            Activo = false; // También desactivar
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Restaura un usuario eliminado
        /// </summary>
        public void Restaurar(string usuarioRestauracion)
        {
            Eliminado = false;
            FechaEliminacion = null;
            UsuarioEliminacion = null;
            MotivoEliminacion = null;
            FechaActualizacion = DateTime.Now;
        }

        [NotMapped]
        public string EstadoEliminacion => Eliminado
            ? $"Eliminado el {FechaEliminacion:dd/MM/yyyy} por {UsuarioEliminacion}"
            : "Activo";

        /// <summary>
        /// Valida formato de email básico
        /// </summary>
        public static bool EsEmailValido(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Genera hash de contraseña usando BCrypt
        /// </summary>
        public static string GenerarHashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifica contraseña contra hash
        /// </summary>
        public static bool VerificarPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        public override string ToString()
        {
            return $"{NombreCompleto} ({NombreUsuario}) - {Rol}";
        }
    }
}