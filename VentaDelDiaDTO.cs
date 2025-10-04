using System;

namespace costbenefi.Models
{
    /// <summary>
    /// DTO para mostrar ventas del día con información completa
    /// </summary>
    public class VentaDelDiaDTO
    {
        public int VentaID { get; set; }
        public string NumeroTicket { get; set; }
        public DateTime FechaVenta { get; set; }

        // ✅ CORREGIDO: Setter público
        public string Hora { get; set; }

        public string Cliente { get; set; }
        public decimal Total { get; set; }
        public decimal CostoTotal { get; set; }

        // ✅ Ganancia REAL calculada (Venta - Costo)
        public decimal GananciaReal => Total - CostoTotal;

        public string FormaPago { get; set; }
        public string Usuario { get; set; }
        public int CantidadProductos { get; set; }

        // Indicadores visuales
        public string IconoFormaPago => FormaPago switch
        {
            var fp when fp?.Contains("Efectivo") == true => "💵",
            var fp when fp?.Contains("Tarjeta") == true => "💳",
            var fp when fp?.Contains("Transferencia") == true => "📱",
            var fp when fp?.Contains("Combinado") == true => "💰",
            _ => "💵"
        };

        public string ColorGanancia => GananciaReal > 0 ? "#10B981" : "#EF4444";

        public string ResumenVenta => $"#{NumeroTicket} - {Cliente} - {Total:C2}";
    }
}