using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using costbenefi.Models;

namespace costbenefi.Data
{
    public static class AppDbContextExtensions
    {
        /// <summary>
        /// Obtiene las ventas del día con información detallada para reporte
        /// </summary>
        public static async Task<List<VentaDelDiaDTO>> ObtenerVentasDelDiaConDetallesAsync(
      this AppDbContext context, DateTime fecha)
        {
            var fechaInicio = fecha.Date;
            var fechaFin = fechaInicio.AddDays(1);

            var ventas = await context.Ventas
                .Include(v => v.DetallesVenta)
                    .ThenInclude(dv => dv.RawMaterial)
                .Where(v => v.FechaVenta >= fechaInicio && v.FechaVenta < fechaFin)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            return ventas.Select(v => new VentaDelDiaDTO
            {
                VentaID = v.Id,
                NumeroTicket = v.NumeroTicket.ToString(),
                FechaVenta = v.FechaVenta,
                Hora = v.FechaVenta.ToString("HH:mm:ss"), // ✅ Establecer explícitamente
                Cliente = v.Cliente ?? "Público General",
                Total = v.Total,
                CostoTotal = v.DetallesVenta?.Sum(dv => dv.CostoUnitario * dv.Cantidad) ?? 0,
                FormaPago = v.FormaPago ?? "Efectivo",
                Usuario = v.Usuario ?? "Sistema",
                CantidadProductos = v.DetallesVenta?.Sum(dv => (int)Math.Ceiling(dv.Cantidad)) ?? 0
            }).ToList();
        }
    }
}