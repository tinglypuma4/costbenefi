using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Configuraci�n global de comisiones para terminales de pago
    /// </summary>
    public class ConfiguracionComisiones
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Porcentaje de comisi�n que cobra el terminal
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeComisionTarjeta { get; set; } = 3.5m;

        /// <summary>
        /// �El terminal cobra IVA adicional sobre la comisi�n?
        /// </summary>
        public bool TerminalCobraIVA { get; set; } = true;

        /// <summary>
        /// Porcentaje de IVA (generalmente 16%)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeIVA { get; set; } = 16.0m;

        /// <summary>
        /// Descripci�n adicional de la configuraci�n
        /// </summary>
        [StringLength(500)]
        public string Descripcion { get; set; } = "Configuraci�n por defecto";

        /// <summary>
        /// �Esta configuraci�n est� activa?
        /// </summary>
        public bool Activa { get; set; } = true;

        // ===== AUDITOR�A =====
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UsuarioModificacion { get; set; } = "";

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Comisi�n total considerando IVA
        /// </summary>
        [NotMapped]
        public decimal ComisionTotalEfectiva => TerminalCobraIVA
            ? PorcentajeComisionTarjeta * (1 + (PorcentajeIVA / 100))
            : PorcentajeComisionTarjeta;

        /// <summary>
        /// Texto descriptivo de la configuraci�n
        /// </summary>
        [NotMapped]
        public string ResumenConfiguracion => TerminalCobraIVA
            ? $"{PorcentajeComisionTarjeta:F2}% + {PorcentajeIVA:F2}% IVA = {ComisionTotalEfectiva:F2}% total"
            : $"{PorcentajeComisionTarjeta:F2}% (sin IVA adicional)";

        // ===== M�TODOS DE UTILIDAD =====

        /// <summary>
        /// Calcula la comisi�n para un monto espec�fico
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
        /// Calcula el desglose completo de una comisi�n
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