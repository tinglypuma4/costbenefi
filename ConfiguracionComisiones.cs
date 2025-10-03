using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Configuración global de comisiones para terminales de pago
    /// </summary>
    public class ConfiguracionComisiones
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Porcentaje de comisión que cobra el terminal
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeComisionTarjeta { get; set; } = 3.5m;

        /// <summary>
        /// ¿El terminal cobra IVA adicional sobre la comisión?
        /// </summary>
        public bool TerminalCobraIVA { get; set; } = true;

        /// <summary>
        /// Porcentaje de IVA (generalmente 16%)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeIVA { get; set; } = 16.0m;

        /// <summary>
        /// Descripción adicional de la configuración
        /// </summary>
        [StringLength(500)]
        public string Descripcion { get; set; } = "Configuración por defecto";

        /// <summary>
        /// ¿Esta configuración está activa?
        /// </summary>
        public bool Activa { get; set; } = true;

        // ===== AUDITORÍA =====
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UsuarioModificacion { get; set; } = "";

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Comisión total considerando IVA
        /// </summary>
        [NotMapped]
        public decimal ComisionTotalEfectiva => TerminalCobraIVA
            ? PorcentajeComisionTarjeta * (1 + (PorcentajeIVA / 100))
            : PorcentajeComisionTarjeta;

        /// <summary>
        /// Texto descriptivo de la configuración
        /// </summary>
        [NotMapped]
        public string ResumenConfiguracion => TerminalCobraIVA
            ? $"{PorcentajeComisionTarjeta:F2}% + {PorcentajeIVA:F2}% IVA = {ComisionTotalEfectiva:F2}% total"
            : $"{PorcentajeComisionTarjeta:F2}% (sin IVA adicional)";

        // ===== MÉTODOS DE UTILIDAD =====

        /// <summary>
        /// Calcula la comisión para un monto específico
        /// </summary>
        public decimal CalcularComision(decimal montoTarjeta)
        {
            if (montoTarjeta <= 0) return 0;

            var comisionBase = montoTarjeta * (PorcentajeComisionTarjeta / 100);

            if (TerminalCobraIVA)
            {
                var ivaComision = comisionBase * (PorcentajeIVA / 100);
                return comisionBase + ivaComision;
            }

            return comisionBase;
        }

        /// <summary>
        /// Calcula el desglose completo de una comisión
        /// </summary>
        public (decimal comisionBase, decimal iva, decimal total) CalcularComisionDesglosada(decimal montoTarjeta)
        {
            if (montoTarjeta <= 0) return (0, 0, 0);

            var comisionBase = montoTarjeta * (PorcentajeComisionTarjeta / 100);
            var ivaComision = TerminalCobraIVA ? comisionBase * (PorcentajeIVA / 100) : 0;
            var comisionTotal = comisionBase + ivaComision;

            return (comisionBase, ivaComision, comisionTotal);
        }
    }
}