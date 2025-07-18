using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace costbenefi.Models
{
    public class Movimiento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RawMaterialId { get; set; }

        [ForeignKey("RawMaterialId")]
        public RawMaterial RawMaterial { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoMovimiento { get; set; } // Creación, Edición, Eliminación, Entrada, Salida, Venta

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        [StringLength(500)]
        public string Motivo { get; set; }

        [StringLength(100)]
        public string Usuario { get; set; }

        public DateTime FechaMovimiento { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioConIVA { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioSinIVA { get; set; }

        [StringLength(50)]
        public string UnidadMedida { get; set; }

        // ========== ✅ NUEVOS CAMPOS PARA POS ==========

        /// <summary>
        /// Número de documento relacionado (ticket, factura, etc.)
        /// </summary>
        [StringLength(50)]
        public string NumeroDocumento { get; set; } = "";

        /// <summary>
        /// Proveedor en caso de entradas, Cliente en caso de ventas
        /// </summary>
        [StringLength(200)]
        public string Proveedor { get; set; } = "";

        /// <summary>
        /// Cliente en caso de ventas
        /// </summary>
        [StringLength(200)]
        public string Cliente { get; set; } = "";

        /// <summary>
        /// Stock anterior antes del movimiento
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal StockAnterior { get; set; } = 0;

        /// <summary>
        /// Stock posterior después del movimiento
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal StockPosterior { get; set; } = 0;

        // ========== ✅ PROPIEDADES CALCULADAS ==========

        /// <summary>
        /// Valor total del movimiento con IVA
        /// </summary>
        [NotMapped]
        public decimal ValorTotalConIVA => Cantidad * PrecioConIVA;

        /// <summary>
        /// Valor total del movimiento sin IVA
        /// </summary>
        [NotMapped]
        public decimal ValorTotalSinIVA => Cantidad * PrecioSinIVA;

        /// <summary>
        /// Diferencia de IVA en el movimiento
        /// </summary>
        [NotMapped]
        public decimal DiferenciaIVA => ValorTotalConIVA - ValorTotalSinIVA;

        /// <summary>
        /// Icono para mostrar en UI según tipo de movimiento
        /// </summary>
        [NotMapped]
        public string TipoMovimientoIcon
        {
            get
            {
                return TipoMovimiento?.ToLower() switch
                {
                    "creación" or "creacion" => "➕",
                    "edición" or "edicion" => "✏️",
                    "eliminación" or "eliminacion" => "🗑️",
                    "entrada" => "📦",
                    "salida" => "📤",
                    "venta" => "💰",
                    "ajuste" => "⚖️",
                    "transferencia" => "🔄",
                    "merma" => "📉",
                    "devolución" or "devolucion" => "↩️",
                    _ => "📝"
                };
            }
        }

        /// <summary>
        /// Color para mostrar en UI según tipo de movimiento
        /// </summary>
        [NotMapped]
        public string ColorMovimiento
        {
            get
            {
                return TipoMovimiento?.ToLower() switch
                {
                    "creación" or "creacion" => "#10B981", // Verde
                    "entrada" => "#10B981", // Verde
                    "edición" or "edicion" => "#3B82F6", // Azul
                    "venta" => "#059669", // Verde oscuro
                    "salida" => "#F59E0B", // Amarillo
                    "eliminación" or "eliminacion" => "#EF4444", // Rojo
                    "merma" => "#EF4444", // Rojo
                    "ajuste" => "#8B5CF6", // Púrpura
                    "transferencia" => "#6366F1", // Índigo
                    "devolución" or "devolucion" => "#06B6D4", // Cian
                    _ => "#6B7280" // Gris
                };
            }
        }

        /// <summary>
        /// Resumen del movimiento para mostrar
        /// </summary>
        [NotMapped]
        public string ResumenMovimiento
        {
            get
            {
                var resumen = $"{TipoMovimientoIcon} {TipoMovimiento}: {Cantidad:F2} {UnidadMedida}";

                if (!string.IsNullOrEmpty(NumeroDocumento))
                {
                    resumen += $" (Doc: {NumeroDocumento})";
                }

                if (!string.IsNullOrEmpty(Cliente) && TipoMovimiento?.ToLower() == "venta")
                {
                    resumen += $" - {Cliente}";
                }

                return resumen;
            }
        }

        /// <summary>
        /// Indica si es un movimiento de entrada (aumenta stock)
        /// </summary>
        [NotMapped]
        public bool EsEntrada
        {
            get
            {
                var tipo = TipoMovimiento?.ToLower();
                return tipo == "creación" || tipo == "creacion" ||
                       tipo == "entrada" || tipo == "devolución" || tipo == "devolucion";
            }
        }

        /// <summary>
        /// Indica si es un movimiento de salida (reduce stock)
        /// </summary>
        [NotMapped]
        public bool EsSalida
        {
            get
            {
                var tipo = TipoMovimiento?.ToLower();
                return tipo == "venta" || tipo == "salida" ||
                       tipo == "merma" || tipo == "eliminación" || tipo == "eliminacion";
            }
        }

        // ========== ✅ MÉTODOS ESTÁTICOS PARA CREAR MOVIMIENTOS ==========

        /// <summary>
        /// Crea un movimiento específico para ventas POS
        /// </summary>
        public static Movimiento CrearMovimientoVenta(
            int rawMaterialId,
            decimal cantidad,
            string motivo,
            string usuario,
            decimal precio,
            string unidadMedida,
            string numeroDocumento,
            string cliente = "Cliente General")
        {
            return new Movimiento
            {
                RawMaterialId = rawMaterialId,
                TipoMovimiento = "Venta",
                Cantidad = cantidad,
                Motivo = motivo,
                Usuario = usuario,
                PrecioConIVA = precio,
                PrecioSinIVA = precio / 1.16m, // Calcular sin IVA (16%)
                UnidadMedida = unidadMedida,
                NumeroDocumento = numeroDocumento,
                Cliente = cliente,
                FechaMovimiento = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un movimiento de entrada de inventario
        /// </summary>
        public static Movimiento CrearMovimientoEntrada(
            int rawMaterialId,
            decimal cantidad,
            string motivo,
            string usuario,
            decimal precioConIVA,
            decimal precioSinIVA,
            string unidadMedida,
            string proveedor = "",
            string numeroDocumento = "")
        {
            return new Movimiento
            {
                RawMaterialId = rawMaterialId,
                TipoMovimiento = "Entrada",
                Cantidad = cantidad,
                Motivo = motivo,
                Usuario = usuario,
                PrecioConIVA = precioConIVA,
                PrecioSinIVA = precioSinIVA,
                UnidadMedida = unidadMedida,
                Proveedor = proveedor,
                NumeroDocumento = numeroDocumento,
                FechaMovimiento = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un movimiento de salida de inventario
        /// </summary>
        public static Movimiento CrearMovimientoSalida(
            int rawMaterialId,
            decimal cantidad,
            string motivo,
            string usuario,
            decimal precio,
            string unidadMedida,
            string numeroDocumento = "")
        {
            return new Movimiento
            {
                RawMaterialId = rawMaterialId,
                TipoMovimiento = "Salida",
                Cantidad = cantidad,
                Motivo = motivo,
                Usuario = usuario,
                PrecioConIVA = precio,
                PrecioSinIVA = precio / 1.16m,
                UnidadMedida = unidadMedida,
                NumeroDocumento = numeroDocumento,
                FechaMovimiento = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un movimiento de ajuste de inventario
        /// </summary>
        public static Movimiento CrearMovimientoAjuste(
            int rawMaterialId,
            decimal cantidadAjuste,
            string motivo,
            string usuario,
            decimal precio,
            string unidadMedida,
            decimal stockAnterior,
            decimal stockPosterior)
        {
            return new Movimiento
            {
                RawMaterialId = rawMaterialId,
                TipoMovimiento = "Ajuste",
                Cantidad = Math.Abs(cantidadAjuste),
                Motivo = motivo,
                Usuario = usuario,
                PrecioConIVA = precio,
                PrecioSinIVA = precio / 1.16m,
                UnidadMedida = unidadMedida,
                StockAnterior = stockAnterior,
                StockPosterior = stockPosterior,
                FechaMovimiento = DateTime.Now
            };
        }

        /// <summary>
        /// Crea un movimiento de merma o pérdida
        /// </summary>
        public static Movimiento CrearMovimientoMerma(
            int rawMaterialId,
            decimal cantidad,
            string motivo,
            string usuario,
            decimal precio,
            string unidadMedida)
        {
            return new Movimiento
            {
                RawMaterialId = rawMaterialId,
                TipoMovimiento = "Merma",
                Cantidad = cantidad,
                Motivo = motivo,
                Usuario = usuario,
                PrecioConIVA = precio,
                PrecioSinIVA = precio / 1.16m,
                UnidadMedida = unidadMedida,
                FechaMovimiento = DateTime.Now
            };
        }

        // ========== ✅ MÉTODOS DE INSTANCIA ==========

        /// <summary>
        /// Actualiza los stocks anterior y posterior
        /// </summary>
        public void ActualizarStocks(decimal stockAnterior, decimal stockPosterior)
        {
            StockAnterior = stockAnterior;
            StockPosterior = stockPosterior;
        }

        /// <summary>
        /// Valida que el movimiento tenga datos correctos
        /// </summary>
        public bool ValidarMovimiento()
        {
            return RawMaterialId > 0 &&
                   Cantidad > 0 &&
                   !string.IsNullOrEmpty(TipoMovimiento) &&
                   !string.IsNullOrEmpty(Usuario) &&
                   !string.IsNullOrEmpty(UnidadMedida);
        }

        /// <summary>
        /// Obtiene descripción detallada del movimiento
        /// </summary>
        public string ObtenerDescripcionDetallada()
        {
            var descripcion = $"{ResumenMovimiento}\n";
            descripcion += $"Fecha: {FechaMovimiento:dd/MM/yyyy HH:mm}\n";
            descripcion += $"Usuario: {Usuario}\n";

            if (!string.IsNullOrEmpty(Motivo))
                descripcion += $"Motivo: {Motivo}\n";

            descripcion += $"Valor: {ValorTotalConIVA:C2}";

            if (StockAnterior > 0 || StockPosterior > 0)
            {
                descripcion += $"\nStock: {StockAnterior:F2} → {StockPosterior:F2}";
            }

            return descripcion;
        }

        /// <summary>
        /// Copia datos básicos a otro movimiento
        /// </summary>
        public void CopiarDatosBasicos(Movimiento destino)
        {
            destino.RawMaterialId = this.RawMaterialId;
            destino.TipoMovimiento = this.TipoMovimiento;
            destino.Usuario = this.Usuario;
            destino.PrecioConIVA = this.PrecioConIVA;
            destino.PrecioSinIVA = this.PrecioSinIVA;
            destino.UnidadMedida = this.UnidadMedida;
        }
    }
}