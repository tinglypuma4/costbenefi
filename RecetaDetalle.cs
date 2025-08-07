using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace costbenefi.Models
{
    /// <summary>
    /// Detalle de ingredientes necesarios para un proceso de fabricación
    /// </summary>
    public class RecetaDetalle
    {
        [Key]
        public int Id { get; set; }

        public int ProcesoFabricacionId { get; set; }
        public virtual ProcesoFabricacion ProcesoFabricacion { get; set; }

        public int RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; }

        /// <summary>
        /// Cantidad necesaria del ingrediente
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadRequerida { get; set; }

        [Required]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = string.Empty;

        /// <summary>
        /// Costo por unidad al momento de crear la receta (referencia)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoUnitario { get; set; }

        /// <summary>
        /// Ingrediente principal (afecta el nombre del producto final)
        /// </summary>
        public bool EsIngredientePrincipal { get; set; } = false;

        /// <summary>
        /// Orden de adición en el proceso (1, 2, 3...)
        /// </summary>
        public int OrdenAdicion { get; set; } = 1;

        [StringLength(300)]
        public string NotasIngrediente { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // ===== PROPIEDADES CALCULADAS =====

        [NotMapped]
        public decimal CostoTotal => CantidadRequerida * CostoUnitario;

        [NotMapped]
        public string NombreIngrediente => RawMaterial?.NombreArticulo ?? "";

        [NotMapped]
        public decimal StockDisponible => RawMaterial?.StockTotal ?? 0;

        [NotMapped]
        public bool PuedeUsarse => RawMaterial != null && StockDisponible >= CantidadRequerida;

        [NotMapped]
        public decimal StockRestante => Math.Max(0, StockDisponible - CantidadRequerida);

        [NotMapped]
        public string EstadoStock
        {
            get
            {
                if (RawMaterial == null) return "Sin asignar";
                if (StockDisponible >= CantidadRequerida) return "Disponible";
                if (StockDisponible > 0) return "Insuficiente";
                return "Sin stock";
            }
        }

        [NotMapped]
        public decimal PorcentajeStockDisponible
        {
            get
            {
                if (CantidadRequerida <= 0) return 100;
                return Math.Min(100, (StockDisponible / CantidadRequerida) * 100);
            }
        }

        [NotMapped]
        public decimal CostoActual => RawMaterial?.PrecioConIVA ?? 0;

        [NotMapped]
        public decimal DiferenciaCosto => CostoActual - CostoUnitario;

        [NotMapped]
        public bool TieneCambioDeCosto => Math.Abs(DiferenciaCosto) > 0.01m;

        // ===== MÉTODOS ÚTILES (SIN [NotMapped]) =====

        /// <summary>
        /// Actualiza el costo unitario con el precio actual del material
        /// </summary>
        public void ActualizarCosto()
        {
            if (RawMaterial != null)
            {
                CostoUnitario = RawMaterial.PrecioConIVA;
                UnidadMedida = RawMaterial.UnidadMedida;
                FechaActualizacion = DateTime.Now;
            }
        }

        /// <summary>
        /// Verifica si se puede usar una cantidad específica para fabricación
        /// </summary>
        public bool PuedeUsarCantidad(decimal cantidadNecesaria)
        {
            return RawMaterial != null && StockDisponible >= cantidadNecesaria;
        }

        /// <summary>
        /// Calcula el costo para una cantidad específica
        /// </summary>
        public decimal CalcularCostoParaCantidad(decimal cantidadNecesaria)
        {
            return cantidadNecesaria * CostoUnitario;
        }

        /// <summary>
        /// Obtiene la máxima cantidad que se puede fabricar con el stock disponible
        /// </summary>
        public decimal ObtenerMaximaCantidadPosible()
        {
            if (CantidadRequerida <= 0) return 0;
            return Math.Floor(StockDisponible / CantidadRequerida);
        }

        /// <summary>
        /// Valida que el ingrediente tenga toda la información necesaria
        /// </summary>
        public bool EsValido()
        {
            return RawMaterialId > 0 &&
                   CantidadRequerida > 0 &&
                   !string.IsNullOrWhiteSpace(UnidadMedida) &&
                   CostoUnitario >= 0;
        }
    }
}