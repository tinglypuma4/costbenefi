using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Proceso de Fabricación - La "receta" para crear un producto
    /// </summary>
    public class ProcesoFabricacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreProducto { get; set; } = string.Empty;

        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoriaProducto { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad que se espera obtener al completar la receta
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal RendimientoEsperado { get; set; }

        /// <summary>
        /// Unidad de medida del producto final (L, kg, piezas, etc.)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string UnidadMedidaProducto { get; set; } = "L";

        /// <summary>
        /// Tiempo estimado de fabricación en minutos
        /// </summary>
        public int TiempoFabricacionMinutos { get; set; } = 60;

        /// <summary>
        /// Porcentaje de pérdida esperado (merma/desperdicio)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeMerma { get; set; } = 0;

        /// <summary>
        /// Costo base de mano de obra por lote
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoManoObra { get; set; } = 0;

        // ===== COSTOS OPCIONALES CON CHECKBOXES =====
        public bool IncluirCostoEnergia { get; set; } = false;
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoEnergia { get; set; } = 0;

        public bool IncluirCostoTransporte { get; set; } = false;
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoTransporte { get; set; } = 0;

        public bool IncluirCostoEmpaque { get; set; } = false;
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoEmpaque { get; set; } = 0;

        public bool IncluirOtrosCostos { get; set; } = false;
        [Column(TypeName = "decimal(18,4)")]
        public decimal OtrosCostos { get; set; } = 0;
        [StringLength(200)]
        public string DescripcionOtrosCostos { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de fabricación: "Lote", "Cantidad", "Continua"
        /// </summary>
        [StringLength(50)]
        public string TipoFabricacion { get; set; } = "Lote";

        /// <summary>
        /// Margen de ganancia objetivo en porcentaje
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal MargenObjetivo { get; set; } = 30;

        public bool Activo { get; set; } = true;

        [StringLength(1000)]
        public string NotasEspeciales { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UsuarioCreador { get; set; } = string.Empty;

        // ===== RELACIONES =====
        public virtual ICollection<RecetaDetalle> Ingredientes { get; set; } = new List<RecetaDetalle>();
        public virtual ICollection<LoteFabricacion> LotesFabricados { get; set; } = new List<LoteFabricacion>();

        // ===== PROPIEDADES CALCULADAS =====
        [NotMapped]
        public decimal CostoMaterialesTotales => Ingredientes?.Sum(i => i.CostoTotal) ?? 0;

        [NotMapped]
        public decimal CostosAdicionalesTotal
        {
            get
            {
                decimal total = 0;
                if (IncluirCostoEnergia) total += CostoEnergia;
                if (IncluirCostoTransporte) total += CostoTransporte;
                if (IncluirCostoEmpaque) total += CostoEmpaque;
                if (IncluirOtrosCostos) total += OtrosCostos;
                return total;
            }
        }

        [NotMapped]
        public decimal CostoTotalPorLote => CostoMaterialesTotales + CostoManoObra + CostosAdicionalesTotal;

        [NotMapped]
        public decimal CostoUnitarioEstimado
        {
            get
            {
                var rendimientoConMerma = RendimientoEsperado * (1 - PorcentajeMerma / 100);
                return rendimientoConMerma > 0 ? CostoTotalPorLote / rendimientoConMerma : 0;
            }
        }

        [NotMapped]
        public decimal PrecioSugeridoVenta => CostoUnitarioEstimado * (1 + MargenObjetivo / 100);

        [NotMapped]
        public bool PuedeFabricarse => Ingredientes?.All(i => i.PuedeUsarse) ?? false;

        [NotMapped]
        public string EstadoProceso => Activo ? "Activo" : "Inactivo";

        // ===== MÉTODOS ÚTILES =====

        /// <summary>
        /// Actualiza los costos de los ingredientes con precios actuales
        /// </summary>
        public void ActualizarCostosIngredientes()
        {
            if (Ingredientes != null)
            {
                foreach (var ingrediente in Ingredientes)
                {
                    if (ingrediente.RawMaterial != null)
                    {
                        ingrediente.CostoUnitario = ingrediente.RawMaterial.PrecioConIVA;
                        ingrediente.FechaActualizacion = DateTime.Now;
                    }
                }
            }
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Verifica si se puede fabricar una cantidad específica
        /// </summary>
        public bool PuedeFabricar(decimal cantidadDeseada)
        {
            if (Ingredientes == null || !Ingredientes.Any()) return false;

            var factor = cantidadDeseada / RendimientoEsperado;
            return Ingredientes.All(i => i.RawMaterial != null &&
                                         i.RawMaterial.StockTotal >= (i.CantidadRequerida * factor));
        }

        /// <summary>
        /// Calcula el costo para una cantidad específica
        /// </summary>
        public decimal CalcularCostoParaCantidad(decimal cantidadDeseada)
        {
            if (RendimientoEsperado <= 0) return 0;

            var factor = cantidadDeseada / RendimientoEsperado;
            return CostoTotalPorLote * factor;
        }
    }
}