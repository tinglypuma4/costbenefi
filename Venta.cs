using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Required]
        [StringLength(200)]
        public string Cliente { get; set; } = "Cliente General";

        [Required]
        [StringLength(100)]
        public string Usuario { get; set; } = Environment.UserName;

        [Column(TypeName = "decimal(18,4)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal IVA { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(50)]
        public string FormaPago { get; set; } = "Efectivo";

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Completada";

        // ===== CORREGIDO: CAMBIO DE INT A LONG =====
        public long NumeroTicket { get; set; }

        [StringLength(500)]
        public string Observaciones { get; set; } = "";

        [Column(TypeName = "decimal(18,4)")]
        public decimal Descuento { get; set; } = 0;

        // ===== PROPIEDADES PARA COMISIONES =====
        [Column(TypeName = "decimal(18,4)")]
        public decimal ComisionTarjeta { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeComisionTarjeta { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MontoTarjeta { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MontoEfectivo { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MontoTransferencia { get; set; } = 0;

        // ===== PROPIEDADES PARA IVA SOBRE COMISIÓN =====
        [Column(TypeName = "decimal(18,4)")]
        public decimal IVAComision { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal ComisionTotal { get; set; } = 0;

        // ===== NAVEGACIÓN =====
        public virtual ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Cantidad total de productos en la venta
        /// </summary>
        [NotMapped]
        public int CantidadItems => DetallesVenta?.Sum(d => (int)Math.Ceiling(d.Cantidad)) ?? 0;

        /// <summary>
        /// Ganancia bruta total de la venta (SIN considerar comisiones)
        /// </summary>
        [NotMapped]
        public decimal GananciaBruta => DetallesVenta?.Sum(d => d.GananciaLinea) ?? 0;

        /// <summary>
        /// Ganancia NETA total de la venta (considerando comisiones de tarjeta + IVA)
        /// </summary>
        [NotMapped]
        public decimal GananciaNeta => GananciaBruta - ComisionTotal;

        /// <summary>
        /// Margen promedio de ganancia en porcentaje (SIN comisiones)
        /// </summary>
        [NotMapped]
        public decimal MargenPromedio
        {
            get
            {
                if (SubTotal <= 0) return 0;
                return (GananciaBruta / SubTotal) * 100;
            }
        }

        /// <summary>
        /// Margen NETO de ganancia en porcentaje (CON comisiones + IVA)
        /// </summary>
        [NotMapped]
        public decimal MargenNeto
        {
            get
            {
                if (SubTotal <= 0) return 0;
                return (GananciaNeta / SubTotal) * 100;
            }
        }

        /// <summary>
        /// Costo total de los productos vendidos
        /// </summary>
        [NotMapped]
        public decimal CostoTotal => DetallesVenta?.Sum(d => d.CostoUnitario * d.Cantidad) ?? 0;

        /// <summary>
        /// Total real recibido después de todas las comisiones (base + IVA)
        /// </summary>
        [NotMapped]
        public decimal TotalRealRecibido => Total - ComisionTotal;

        // ===== MÉTODOS =====

        /// <summary>
        /// Calcula todos los totales de la venta - CORREGIDO: Sin IVA adicional
        /// </summary>
        public void CalcularTotales()
        {
            // ✅ CORRECCIÓN: Los precios YA incluyen IVA
            SubTotal = DetallesVenta?.Sum(d => d.SubTotal) ?? 0;

            // ✅ IVA es solo informativo (para saber cuánto IVA está incluido)
            // Asumiendo que los precios incluyen 16% de IVA
            IVA = SubTotal - (SubTotal / 1.16m); // IVA incluido en el precio

            // ✅ Total = SubTotal - Descuento (sin agregar IVA adicional)
            Total = SubTotal - Descuento;
        }

        /// <summary>
        /// Calcula las comisiones de tarjeta según los montos pagados
        /// </summary>
        public void CalcularComisiones(decimal porcentajeComision)
        {
            PorcentajeComisionTarjeta = porcentajeComision;
            ComisionTarjeta = MontoTarjeta * (porcentajeComision / 100);

            // La comisión total inicialmente es solo la comisión base
            ComisionTotal = ComisionTarjeta;
        }

        /// <summary>
        /// Calcula el IVA sobre la comisión y actualiza el total de comisiones
        /// </summary>
        public void CalcularIVAComision(bool terminalCobraIVA)
        {
            if (terminalCobraIVA && ComisionTarjeta > 0)
            {
                IVAComision = ComisionTarjeta * 0.16m; // 16% de IVA
                ComisionTotal = ComisionTarjeta + IVAComision;
            }
            else
            {
                IVAComision = 0;
                ComisionTotal = ComisionTarjeta;
            }
        }

        /// <summary>
        /// Establece los montos por forma de pago
        /// </summary>
        public void EstablecerFormasPago(decimal efectivo, decimal tarjeta, decimal transferencia)
        {
            MontoEfectivo = efectivo;
            MontoTarjeta = tarjeta;
            MontoTransferencia = transferencia;
        }

        /// <summary>
        /// Genera número de ticket único basado en fecha y hora - CORREGIDO
        /// </summary>
        public long GenerarNumeroTicket()
        {
            var fecha = DateTime.Now;

            // ✅ FORMATO ÚNICO: yyMMddHHmmss (incluye segundos)
            // Ejemplo: 251126143045 (25/11/26 a las 14:30:45)
            var numeroBase = long.Parse($"{fecha:yyMMdd}{fecha:HH}{fecha:mm}{fecha:ss}");

            // ✅ AGREGAR MILISEGUNDOS para máxima unicidad
            var milisegundos = fecha.Millisecond;
            NumeroTicket = numeroBase * 1000 + milisegundos;

            return NumeroTicket;
        }

        /// <summary>
        /// Agrega un producto al detalle de la venta
        /// </summary>
        public void AgregarDetalle(DetalleVenta detalle)
        {
            detalle.VentaId = this.Id;
            detalle.Venta = this;
            DetallesVenta.Add(detalle);
            detalle.CalcularSubTotal();
        }

        /// <summary>
        /// Aplica un descuento a toda la venta
        /// </summary>
        public void AplicarDescuento(decimal descuento)
        {
            Descuento = descuento;
            CalcularTotales(); // Recalcular con el descuento
        }

        /// <summary>
        /// Valida que la venta esté completa y correcta
        /// </summary>
        public bool ValidarVenta()
        {
            return DetallesVenta?.Any() == true &&
                   SubTotal > 0 &&
                   Total > 0 &&
                   !string.IsNullOrEmpty(Cliente) &&
                   !string.IsNullOrEmpty(Usuario);
        }

        /// <summary>
        /// Obtiene resumen de la venta para mostrar
        /// </summary>
        public string ObtenerResumen()
        {
            var resumen = $"Ticket #{NumeroTicket} - {Cliente} - {Total:C2} ({CantidadItems} productos)";

            // Agregar información de comisiones si aplica
            if (ComisionTotal > 0)
            {
                resumen += $" | Comisión: {ComisionTotal:C2} | Neto: {TotalRealRecibido:C2}";
            }

            return resumen;
        }

        /// <summary>
        /// Obtiene desglose detallado de análisis financiero
        /// </summary>
        public string ObtenerAnalisisFinanciero()
        {
            var analisis = $"ANÁLISIS FINANCIERO - Ticket #{NumeroTicket}\n\n" +
                          $"💰 INGRESOS:\n" +
                          $"   • Total venta: {Total:C2}\n" +
                          $"   • Descuento aplicado: {Descuento:C2}\n" +
                          $"   • IVA incluido: {IVA:C2}\n\n" +
                          $"💳 FORMAS DE PAGO:\n" +
                          $"   • Efectivo: {MontoEfectivo:C2}\n" +
                          $"   • Tarjeta: {MontoTarjeta:C2}\n" +
                          $"   • Transferencia: {MontoTransferencia:C2}\n\n" +
                          $"🏦 COMISIONES:\n" +
                          $"   • % Comisión tarjeta: {PorcentajeComisionTarjeta:F2}%\n" +
                          $"   • Comisión base: {ComisionTarjeta:C2}\n";

            // Agregar información de IVA si aplica
            if (IVAComision > 0)
            {
                analisis += $"   • IVA sobre comisión (16%): {IVAComision:C2}\n" +
                           $"   • Comisión total: {ComisionTotal:C2}\n";
            }
            else
            {
                analisis += $"   • Comisión total: {ComisionTotal:C2}\n";
            }

            analisis += $"   • Total real recibido: {TotalRealRecibido:C2}\n\n" +
                       $"📊 RENTABILIDAD:\n" +
                       $"   • Costo total: {CostoTotal:C2}\n" +
                       $"   • Ganancia bruta: {GananciaBruta:C2}\n" +
                       $"   • Ganancia neta: {GananciaNeta:C2}\n" +
                       $"   • Margen bruto: {MargenPromedio:F2}%\n" +
                       $"   • Margen neto: {MargenNeto:F2}%";

            return analisis;
        }

        /// <summary>
        /// Obtiene información completa de comisiones para reportes
        /// </summary>
        public string ObtenerDetalleComisiones()
        {
            if (ComisionTarjeta <= 0)
                return "Sin comisiones de tarjeta";

            var detalle = $"Comisión {PorcentajeComisionTarjeta:F2}% sobre {MontoTarjeta:C2} = {ComisionTarjeta:C2}";

            if (IVAComision > 0)
            {
                detalle += $" + IVA: {IVAComision:C2} = Total: {ComisionTotal:C2}";
            }

            return detalle;
        }
    }
}