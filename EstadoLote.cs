namespace costbenefi.Models
{
    /// <summary>
    /// ✅ Estados posibles de un lote de fabricación
    /// </summary>
    public enum EstadoLote
    {
        /// <summary>
        /// Lote planificado pero no iniciado
        /// </summary>
        Planificado = 0,

        /// <summary>
        /// Lote en proceso de fabricación
        /// </summary>
        EnProceso = 1,

        /// <summary>
        /// Lote completado exitosamente
        /// </summary>
        Completado = 2,

        /// <summary>
        /// Lote cancelado
        /// </summary>
        Cancelado = 3,

        /// <summary>
        /// Lote pausado temporalmente
        /// </summary>
        Pausado = 4,

        /// <summary>
        /// Lote con errores
        /// </summary>
        ConErrores = 5
    }

    /// <summary>
    /// ✅ Métodos de extensión para EstadoLote
    /// </summary>
    public static class EstadoLoteExtensions
    {
        /// <summary>
        /// Obtiene la descripción del estado
        /// </summary>
        public static string ObtenerDescripcion(this EstadoLote estado)
        {
            return estado switch
            {
                EstadoLote.Planificado => "📋 Planificado",
                EstadoLote.EnProceso => "⚙️ En Proceso",
                EstadoLote.Completado => "✅ Completado",
                EstadoLote.Cancelado => "❌ Cancelado",
                EstadoLote.Pausado => "⏸️ Pausado",
                EstadoLote.ConErrores => "⚠️ Con Errores",
                _ => "❓ Desconocido"
            };
        }

        /// <summary>
        /// Obtiene el color asociado al estado
        /// </summary>
        public static string ObtenerColor(this EstadoLote estado)
        {
            return estado switch
            {
                EstadoLote.Planificado => "#3B82F6", // Azul
                EstadoLote.EnProceso => "#F59E0B", // Amarillo
                EstadoLote.Completado => "#10B981", // Verde
                EstadoLote.Cancelado => "#EF4444", // Rojo
                EstadoLote.Pausado => "#8B5CF6", // Púrpura
                EstadoLote.ConErrores => "#F97316", // Naranja
                _ => "#6B7280" // Gris
            };
        }

        /// <summary>
        /// Indica si el lote está activo (no terminado)
        /// </summary>
        public static bool EstaActivo(this EstadoLote estado)
        {
            return estado == EstadoLote.Planificado ||
                   estado == EstadoLote.EnProceso ||
                   estado == EstadoLote.Pausado;
        }

        /// <summary>
        /// Indica si el lote está terminado
        /// </summary>
        public static bool EstaTerminado(this EstadoLote estado)
        {
            return estado == EstadoLote.Completado ||
                   estado == EstadoLote.Cancelado;
        }
    }
}