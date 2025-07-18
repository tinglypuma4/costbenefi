using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace costbenefi.Models
{
    public class DetalleVenta
    {
        [Key]
        public int Id { get; set; }

        // ===== RELACIONES =====
        public int VentaId { get; set; }
        public virtual Venta Venta { get; set; }

        public int RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; }

        // ===== DATOS DEL PRODUCTO VENDIDO =====

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoUnitario { get; set; }

        // ===== CAMPOS ADICIONALES PARA EL TICKET =====

        [Required]
        [StringLength(200)]
        public string NombreProducto { get; set; }

        [Required]
        [StringLength(20)]
        public string UnidadMedida { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeIVA { get; set; } = 16.0m;

        [Column(TypeName = "decimal(18,4)")]
        public decimal DescuentoAplicado { get; set; } = 0;

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Ganancia de esta línea de venta
        /// </summary>
        [NotMapped]
        public decimal GananciaLinea => SubTotal - (CostoUnitario * Cantidad);

        /// <summary>
        /// Margen de ganancia en porcentaje para esta línea
        /// </summary>
        [NotMapped]
        public decimal MargenPorcentaje
        {
            get
            {
                if (SubTotal <= 0) return 0;
                return (GananciaLinea / SubTotal) * 100;
            }
        }

        /// <summary>
        /// IVA de esta línea de venta
        /// </summary>
        [NotMapped]
        public decimal IVALinea => SubTotal * (PorcentajeIVA / 100);

        /// <summary>
        /// Total con IVA incluido para esta línea
        /// </summary>
        [NotMapped]
        public decimal TotalConIVA => SubTotal + IVALinea;

        /// <summary>
        /// Precio unitario con IVA
        /// </summary>
        [NotMapped]
        public decimal PrecioUnitarioConIVA => PrecioUnitario * (1 + (PorcentajeIVA / 100));

        /// <summary>
        /// Valor total sin descuentos
        /// </summary>
        [NotMapped]
        public decimal ValorSinDescuento => Cantidad * PrecioUnitario;

        /// <summary>
        /// Indica si tiene descuento aplicado
        /// </summary>
        [NotMapped]
        public bool TieneDescuento => DescuentoAplicado > 0;

        // ===== MÉTODOS =====

        /// <summary>
        /// Calcula el subtotal basado en cantidad y precio
        /// </summary>
        public void CalcularSubTotal()
        {
            SubTotal = (Cantidad * PrecioUnitario) - DescuentoAplicado;

            // Asegurar que no sea negativo
            if (SubTotal < 0) SubTotal = 0;
        }

        /// <summary>
        /// Aplica un descuento a esta línea específica
        /// </summary>
        public void AplicarDescuento(decimal descuento)
        {
            DescuentoAplicado = descuento;
            CalcularSubTotal();
        }

        /// <summary>
        /// Aplica un descuento por porcentaje
        /// </summary>
        public void AplicarDescuentoPorcentaje(decimal porcentaje)
        {
            var valorSinDescuento = Cantidad * PrecioUnitario;
            DescuentoAplicado = valorSinDescuento * (porcentaje / 100);
            CalcularSubTotal();
        }

        /// <summary>
        /// Actualiza la cantidad y recalcula
        /// </summary>
        public void ActualizarCantidad(decimal nuevaCantidad)
        {
            Cantidad = nuevaCantidad;
            CalcularSubTotal();
        }

        /// <summary>
        /// Actualiza el precio unitario y recalcula
        /// </summary>
        public void ActualizarPrecio(decimal nuevoPrecio)
        {
            PrecioUnitario = nuevoPrecio;
            CalcularSubTotal();
        }

        /// <summary>
        /// Valida que los datos del detalle sean correctos
        /// </summary>
        public bool ValidarDetalle()
        {
            return Cantidad > 0 &&
                   PrecioUnitario > 0 &&
                   !string.IsNullOrEmpty(NombreProducto) &&
                   !string.IsNullOrEmpty(UnidadMedida) &&
                   RawMaterialId > 0;
        }

        /// <summary>
        /// Obtiene descripción formateada para el ticket
        /// </summary>
        public string ObtenerDescripcionTicket()
        {
            var descuento = TieneDescuento ? $" (Desc: {DescuentoAplicado:C2})" : "";
            return $"{NombreProducto} - {Cantidad:F2} {UnidadMedida} × {PrecioUnitario:C2}{descuento}";
        }

        /// <summary>
        /// Copia datos del producto para el detalle
        /// </summary>
        public void CopiarDatosProducto(RawMaterial producto)
        {
            RawMaterialId = producto.Id;
            NombreProducto = producto.NombreArticulo;
            UnidadMedida = producto.UnidadMedida;
            PrecioUnitario = producto.PrecioVentaFinal;
            CostoUnitario = producto.PrecioConIVA;
            PorcentajeIVA = producto.PorcentajeIVA;
        }
    }
}