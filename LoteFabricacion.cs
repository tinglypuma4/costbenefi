using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace costbenefi.Models
{
    /// <summary>
    /// Registro de cada lote de fabricación realizado
    /// </summary>
    public class LoteFabricacion
    {
        [Key]
        public int Id { get; set; }

        public int ProcesoFabricacionId { get; set; }
        public virtual ProcesoFabricacion ProcesoFabricacion { get; set; }

        /// <summary>
        /// Número de lote único para identificación
        /// </summary>
        [Required]
        [StringLength(50)]
        public string NumeroLote { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad que se planeaba fabricar
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadPlanificada { get; set; }

        /// <summary>
        /// Cantidad realmente obtenida
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadObtenida { get; set; }

        /// <summary>
        /// Fecha y hora de inicio del proceso
        /// </summary>
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha y hora de finalización
        /// </summary>
        public DateTime? FechaFin { get; set; }

        /// <summary>
        /// Estado: "Planificado", "En Proceso", "Completado", "Cancelado"
        /// </summary>
        [StringLength(20)]
        public string Estado { get; set; } = "Planificado";

        /// <summary>
        /// Costo real de materiales usados
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoMaterialesReal { get; set; }

        /// <summary>
        /// Costo real de mano de obra
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoManoObraReal { get; set; }

        /// <summary>
        /// Costos adicionales reales (energía, transporte, empaque, otros)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostosAdicionalesReal { get; set; }

        [StringLength(100)]
        public string OperadorResponsable { get; set; } = string.Empty;

        [StringLength(1000)]
        public string NotasProduccion { get; set; } = string.Empty;

        /// <summary>
        /// ID del producto resultante creado en RawMaterials
        /// </summary>
        public int? ProductoResultanteId { get; set; }
        public virtual RawMaterial ProductoResultante { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // ===== PROPIEDADES CALCULADAS =====

        [NotMapped]
        public decimal CostoTotalReal => CostoMaterialesReal + CostoManoObraReal + CostosAdicionalesReal;

        [NotMapped]
        public decimal CostoUnitarioReal => CantidadObtenida > 0 ? CostoTotalReal / CantidadObtenida : 0;

        [NotMapped]
        public decimal PorcentajeMermaReal
        {
            get
            {
                if (CantidadPlanificada <= 0) return 0;
                return ((CantidadPlanificada - CantidadObtenida) / CantidadPlanificada) * 100;
            }
        }

        [NotMapped]
        public int TiempoTranscurrido
        {
            get
            {
                if (FechaFin.HasValue)
                    return (int)(FechaFin.Value - FechaInicio).TotalMinutes;
                return (int)(DateTime.Now - FechaInicio).TotalMinutes;
            }
        }

        [NotMapped]
        public bool EstaCompletado => Estado == "Completado";

        [NotMapped]
        public bool EstaEnProceso => Estado == "En Proceso";

        [NotMapped]
        public bool EstaCancelado => Estado == "Cancelado";

        [NotMapped]
        public bool EstaPlanificado => Estado == "Planificado";

        [NotMapped]
        public string EstadoDescripcion
        {
            get
            {
                return Estado switch
                {
                    "Planificado" => "📋 Planificado",
                    "En Proceso" => "⚙️ En Proceso",
                    "Completado" => "✅ Completado",
                    "Cancelado" => "❌ Cancelado",
                    _ => "❓ Desconocido"
                };
            }
        }

        [NotMapped]
        public decimal EficienciaLote
        {
            get
            {
                if (CantidadPlanificada <= 0) return 0;
                return (CantidadObtenida / CantidadPlanificada) * 100;
            }
        }

        [NotMapped]
        public string TiempoTranscurridoTexto
        {
            get
            {
                var minutos = TiempoTranscurrido;
                if (minutos < 60)
                    return $"{minutos} min";
                else if (minutos < 1440)
                    return $"{minutos / 60}h {minutos % 60}min";
                else
                    return $"{minutos / 1440} días";
            }
        }

        [NotMapped]
        public string NombreProceso => ProcesoFabricacion?.NombreProducto ?? "";

        // ===== MÉTODOS ÚTILES =====

        /// <summary>
        /// Inicia el proceso de fabricación
        /// </summary>
        public void IniciarProceso(string operador = "")
        {
            Estado = "En Proceso";
            FechaInicio = DateTime.Now;
            OperadorResponsable = operador;
        }

        /// <summary>
        /// Completa el proceso de fabricación
        /// </summary>
        public void CompletarProceso(decimal cantidadObtenida, string notas = "")
        {
            Estado = "Completado";
            FechaFin = DateTime.Now;
            CantidadObtenida = cantidadObtenida;
            if (!string.IsNullOrWhiteSpace(notas))
                NotasProduccion += $"\n[{DateTime.Now:dd/MM/yyyy HH:mm}] {notas}";
        }

        /// <summary>
        /// Cancela el proceso de fabricación
        /// </summary>
        public void CancelarProceso(string motivo)
        {
            Estado = "Cancelado";
            FechaFin = DateTime.Now;
            NotasProduccion += $"\n[{DateTime.Now:dd/MM/yyyy HH:mm}] CANCELADO: {motivo}";
        }

        /// <summary>
        /// Genera un número de lote único
        /// </summary>
        public static string GenerarNumeroLote(string prefijo = "FAB")
        {
            var fecha = DateTime.Now;
            return $"{prefijo}{fecha:yyMMdd}{fecha:HHmm}{new Random().Next(10, 99)}";
        }

        /// <summary>
        /// Agrega una nota al proceso
        /// </summary>
        public void AgregarNota(string nota)
        {
            if (!string.IsNullOrWhiteSpace(nota))
            {
                NotasProduccion += $"\n[{DateTime.Now:dd/MM/yyyy HH:mm}] {nota}";
            }
        }

        /// <summary>
        /// Calcula la diferencia entre costo estimado y real
        /// </summary>
        public decimal DiferenciaCosto(decimal costoEstimado)
        {
            return CostoTotalReal - costoEstimado;
        }

        /// <summary>
        /// Verifica si el lote fue rentable comparado con el costo estimado
        /// </summary>
        public bool FueRentable(decimal costoEstimado)
        {
            return CostoTotalReal <= costoEstimado;
        }
    }
}