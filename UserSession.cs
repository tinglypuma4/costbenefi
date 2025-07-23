using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace costbenefi.Models
{
    /// <summary>
    /// Registra las sesiones de usuario en el sistema
    /// </summary>
    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del usuario que inició sesión
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Token único de la sesión
        /// </summary>
        [Required]
        [StringLength(100)]
        public string SessionToken { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de inicio de sesión
        /// </summary>
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de cierre de sesión (null si está activa)
        /// </summary>
        public DateTime? FechaCierre { get; set; }

        /// <summary>
        /// IP desde la cual se conectó (para futuras implementaciones web)
        /// </summary>
        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Nombre de la máquina/computadora
        /// </summary>
        [StringLength(100)]
        public string NombreMaquina { get; set; } = Environment.MachineName;

        /// <summary>
        /// Versión de la aplicación usada
        /// </summary>
        [StringLength(20)]
        public string VersionApp { get; set; } = "1.0.0";

        /// <summary>
        /// Última actividad registrada en la sesión
        /// </summary>
        public DateTime UltimaActividad { get; set; } = DateTime.Now;

        /// <summary>
        /// Motivo de cierre de sesión
        /// </summary>
        [StringLength(100)]
        public string? MotivoCierre { get; set; }

        // ===== NAVEGACIÓN =====
        public virtual User? User { get; set; }

        // ===== PROPIEDADES CALCULADAS =====

        [NotMapped]
        public bool EstaActiva => !FechaCierre.HasValue;

        [NotMapped]
        public TimeSpan DuracionSesion
        {
            get
            {
                var fechaFin = FechaCierre ?? DateTime.Now;
                return fechaFin - FechaInicio;
            }
        }

        [NotMapped]
        public string DuracionFormateada
        {
            get
            {
                var duracion = DuracionSesion;
                if (duracion.TotalDays >= 1)
                    return $"{duracion.Days}d {duracion.Hours}h {duracion.Minutes}m";
                else if (duracion.TotalHours >= 1)
                    return $"{duracion.Hours}h {duracion.Minutes}m";
                else
                    return $"{duracion.Minutes}m {duracion.Seconds}s";
            }
        }

        [NotMapped]
        public TimeSpan TiempoInactividad => DateTime.Now - UltimaActividad;

        [NotMapped]
        public bool SesionExpirada => TiempoInactividad.TotalHours > 8; // 8 horas de inactividad

        // ===== MÉTODOS =====

        /// <summary>
        /// Genera un token único para la sesión
        /// </summary>
        public static string GenerarToken()
        {
            return Guid.NewGuid().ToString("N")[..32]; // 32 caracteres hexadecimales
        }

        /// <summary>
        /// Actualiza la última actividad de la sesión
        /// </summary>
        public void ActualizarActividad()
        {
            UltimaActividad = DateTime.Now;
        }

        /// <summary>
        /// Cierra la sesión con un motivo
        /// </summary>
        public void CerrarSesion(string motivo = "Cierre normal")
        {
            FechaCierre = DateTime.Now;
            MotivoCierre = motivo;
        }

        /// <summary>
        /// Crea una nueva sesión para un usuario
        /// </summary>
        public static UserSession CrearSesion(int userId, string version = "1.0.0")
        {
            return new UserSession
            {
                UserId = userId,
                SessionToken = GenerarToken(),
                FechaInicio = DateTime.Now,
                UltimaActividad = DateTime.Now,
                NombreMaquina = Environment.MachineName,
                VersionApp = version
            };
        }

        public override string ToString()
        {
            var estado = EstaActiva ? "Activa" : "Cerrada";
            return $"Sesión {SessionToken[..8]}... ({estado}) - {DuracionFormateada}";
        }
    }
}