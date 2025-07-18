using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace costbenefi.Models
{
    public class RawMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreArticulo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UnidadMedida { get; set; } = string.Empty;

        /// <summary>
        /// Unidad base para cálculos internos (ej: gramos, mililitros)
        /// </summary>
        [StringLength(50)]
        public string UnidadBase { get; set; } = string.Empty;

        /// <summary>
        /// Factor de conversión entre UnidadMedida y UnidadBase
        /// Ejemplo: 1 Kilo = 1000 gramos → FactorConversion = 1000
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal FactorConversion { get; set; } = 1;

        /// <summary>
        /// Indica si la unidad principal es la misma que la unidad base
        /// </summary>
        [NotMapped]
        public bool EsUnidadPrincipal => FactorConversion == 1 || string.IsNullOrEmpty(UnidadBase);

        [Column(TypeName = "decimal(18,4)")]
        public decimal StockAntiguo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal StockNuevo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal StockTotal => StockAntiguo + StockNuevo;

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioPorUnidad { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioPorUnidadBase { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioTotal => StockTotal * PrecioPorUnidad;

        [StringLength(200)]
        public string Proveedor { get; set; } = string.Empty;

        [StringLength(500)]
        public string Observaciones { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal AlertaStockBajo { get; set; } = 0;

        public bool TieneStockBajo => StockTotal <= AlertaStockBajo && AlertaStockBajo > 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // CAMPOS PARA ESCÁNER Y PRECIOS CON IVA
        [StringLength(100)]
        public string CodigoBarras { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioConIVA { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioSinIVA { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioBaseConIVA { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioBaseSinIVA { get; set; }

        // ========== ✅ NUEVOS CAMPOS PARA POS ========== 
        // Solo agregar estos campos, no tocar nada más

        /// <summary>
        /// Precio de venta al público (sin IVA)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioVenta { get; set; } = 0;

        /// <summary>
        /// Precio de venta al público (con IVA incluido)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioVentaConIVA { get; set; } = 0;

        /// <summary>
        /// Indica si el producto está disponible para venta en POS
        /// </summary>
        public bool ActivoParaVenta { get; set; } = true;

        /// <summary>
        /// Stock mínimo requerido para permitir ventas
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal StockMinimoVenta { get; set; } = 1;

        /// <summary>
        /// Margen de ganancia objetivo en porcentaje
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal MargenObjetivo { get; set; } = 30;

        /// <summary>
        /// Precio con descuento aplicado
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioDescuento { get; set; } = 0;

        /// <summary>
        /// Porcentaje de descuento aplicado
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeDescuento { get; set; } = 0;

        /// <summary>
        /// Fecha de vencimiento del producto (opcional)
        /// </summary>
        public DateTime? FechaVencimiento { get; set; }

        /// <summary>
        /// Días antes del vencimiento para aplicar descuento
        /// </summary>
        public int DiasParaDescuento { get; set; } = 7;

        // ===== CAMPOS EXISTENTES PARA ELIMINACIÓN LÓGICA =====
        public bool Eliminado { get; set; } = false;
        public DateTime? FechaEliminacion { get; set; }
        [StringLength(100)]
        public string? UsuarioEliminacion { get; set; }
        [StringLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ===== PROPIEDADES CALCULADAS EXISTENTES =====
        [NotMapped]
        public bool EstaActivo => !Eliminado;

        [NotMapped]
        public decimal ValorTotalConIVA => StockTotal * PrecioConIVA;

        [NotMapped]
        public decimal ValorTotalSinIVA => StockTotal * PrecioSinIVA;

        [NotMapped]
        public decimal ValorTotalBaseConIVA => StockTotal * PrecioBaseConIVA;

        [NotMapped]
        public decimal ValorTotalBaseSinIVA => StockTotal * PrecioBaseSinIVA;

        [NotMapped]
        public decimal DiferenciaIVATotal => ValorTotalConIVA - ValorTotalSinIVA;

        [NotMapped]
        public decimal PorcentajeIVA => PrecioSinIVA > 0 ? ((PrecioConIVA - PrecioSinIVA) / PrecioSinIVA) * 100 : 0;

        [NotMapped]
        public bool TienePreciosIVA => PrecioConIVA > 0 || PrecioSinIVA > 0;

        [NotMapped]
        public decimal PrecioPromedio => PrecioConIVA > 0 && PrecioSinIVA > 0 ? (PrecioConIVA + PrecioSinIVA) / 2 : Math.Max(PrecioConIVA, PrecioSinIVA);

        // ========== ✅ NUEVAS PROPIEDADES CALCULADAS PARA POS ==========

        /// <summary>
        /// Indica si el producto está disponible para venta (activo, con stock, etc.)
        /// </summary>
        [NotMapped]
        public bool DisponibleParaVenta => ActivoParaVenta && !Eliminado && StockTotal >= StockMinimoVenta;

        /// <summary>
        /// Ganancia por unidad vendida
        /// </summary>
        [NotMapped]
        public decimal GananciaPorUnidad => PrecioVenta - PrecioConIVA;

        /// <summary>
        /// Margen real de ganancia en porcentaje
        /// </summary>
        [NotMapped]
        public decimal MargenReal
        {
            get
            {
                if (PrecioVenta <= 0) return 0;
                return ((PrecioVenta - PrecioConIVA) / PrecioVenta) * 100;
            }
        }

        /// <summary>
        /// Precio sugerido basado en costo + margen objetivo
        /// </summary>
        [NotMapped]
        public decimal PrecioSugerido => PrecioConIVA * (1 + (MargenObjetivo / 100));

        /// <summary>
        /// Precio final para mostrar en POS (con descuento si aplica)
        /// </summary>
        [NotMapped]
        public decimal PrecioVentaFinal => TieneDescuentoActivo ? PrecioDescuento : PrecioVenta;

        /// <summary>
        /// Indica si tiene descuento activo
        /// </summary>
        [NotMapped]
        public bool TieneDescuentoActivo => PrecioDescuento > 0 && PorcentajeDescuento > 0;

        /// <summary>
        /// Días hasta vencimiento
        /// </summary>
        [NotMapped]
        public int DiasHastaVencimiento
        {
            get
            {
                if (!FechaVencimiento.HasValue) return int.MaxValue;
                return (int)(FechaVencimiento.Value - DateTime.Now).TotalDays;
            }
        }

        /// <summary>
        /// Indica si está próximo a vencer
        /// </summary>
        [NotMapped]
        public bool ProximoAVencer => FechaVencimiento.HasValue && DiasHastaVencimiento <= DiasParaDescuento;

        /// <summary>
        /// Indica si está vencido
        /// </summary>
        [NotMapped]
        public bool Vencido => FechaVencimiento.HasValue && DiasHastaVencimiento < 0;

        /// <summary>
        /// Estado del producto para POS
        /// </summary>
        [NotMapped]
        public string EstadoProducto
        {
            get
            {
                if (Vencido) return "Vencido";
                if (ProximoAVencer) return "Próximo a vencer";
                if (TieneStockBajo) return "Stock bajo";
                if (!DisponibleParaVenta) return "No disponible";
                return "Disponible";
            }
        }

        // ===== MÉTODOS EXISTENTES PARA GESTIÓN DE ELIMINACIÓN =====
        public void MarcarComoEliminado(string usuario, string motivo = "Eliminación manual")
        {
            Eliminado = true;
            FechaEliminacion = DateTime.Now;
            UsuarioEliminacion = usuario;
            MotivoEliminacion = motivo;
            FechaActualizacion = DateTime.Now;
        }

        public void Restaurar(string usuario)
        {
            Eliminado = false;
            FechaEliminacion = null;
            UsuarioEliminacion = null;
            MotivoEliminacion = null;
            FechaActualizacion = DateTime.Now;
            Observaciones += $"\n[{DateTime.Now:yyyy-MM-dd HH:mm}] Producto restaurado por {usuario}";
        }

        [NotMapped]
        public string EstadoEliminacion => Eliminado
            ? $"Eliminado el {FechaEliminacion:dd/MM/yyyy} por {UsuarioEliminacion}"
            : "Activo";

        // ========== ✅ NUEVOS MÉTODOS PARA POS ==========

        /// <summary>
        /// Aplica descuento por porcentaje
        /// </summary>
        public void AplicarDescuentoPorcentaje(decimal porcentaje)
        {
            PorcentajeDescuento = porcentaje;
            PrecioDescuento = PrecioVenta * (1 - (porcentaje / 100));
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Aplica descuento fijo
        /// </summary>
        public void AplicarDescuentoFijo(decimal descuento)
        {
            PrecioDescuento = PrecioVenta - descuento;
            PorcentajeDescuento = PrecioVenta > 0 ? ((PrecioVenta - PrecioDescuento) / PrecioVenta) * 100 : 0;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Sugiere precio de venta basado en margen deseado
        /// </summary>
        public decimal SugerirPrecioVenta(decimal margenDeseado)
        {
            return PrecioConIVA * (1 + (margenDeseado / 100));
        }

        /// <summary>
        /// Actualiza precio de venta por margen
        /// </summary>
        public void ActualizarPrecioVentaPorMargen(decimal margenDeseado)
        {
            PrecioVenta = SugerirPrecioVenta(margenDeseado);
            PrecioVentaConIVA = PrecioVenta * (1 + (PorcentajeIVA / 100));
            MargenObjetivo = margenDeseado;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Reduce stock para ventas
        /// </summary>
        public bool ReducirStock(decimal cantidad)
        {
            if (StockTotal >= cantidad)
            {
                // Reducir primero del stock nuevo, luego del antiguo
                if (StockNuevo >= cantidad)
                {
                    StockNuevo -= cantidad;
                }
                else
                {
                    decimal restante = cantidad - StockNuevo;
                    StockNuevo = 0;
                    StockAntiguo -= restante;
                }
                FechaActualizacion = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Configura producto para venta por primera vez
        /// </summary>
        public void ConfigurarParaVenta(decimal margenDeseado = 30)
        {
            if (PrecioVenta <= 0)
            {
                ActualizarPrecioVentaPorMargen(margenDeseado);
            }
            ActivoParaVenta = true;
            StockMinimoVenta = UnidadMedida.Contains("kg") || UnidadMedida.Contains("gr") ? 0.1m : 1;
            FechaActualizacion = DateTime.Now; 
        }
    }
}