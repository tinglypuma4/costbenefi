using System;
using System.Threading.Tasks;
using System.Windows;
using costbenefi.Models;

namespace costbenefi.Services
{
    public class TicketPrinter : IDisposable
    {
        public async Task ImprimirTicket(Venta venta, string impresora)
        {
            try
            {
                await Task.Delay(500); // Simular impresión

                var ticket = GenerarTicket(venta);

                MessageBox.Show(ticket, $"🖨️ TICKET IMPRESO - {impresora}",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al imprimir: {ex.Message}");
            }
        }

        private string GenerarTicket(Venta venta)
        {
            var ticket = $@"================================
        SISTEMA POS
================================
Ticket #: {venta.NumeroTicket}
Fecha: {venta.FechaVenta:dd/MM/yyyy HH:mm}
Cliente: {venta.Cliente}
================================

PRODUCTOS:
";
            foreach (var item in venta.DetallesVenta)
            {
                ticket += $"{item.NombreProducto}\n";
                ticket += $"{item.Cantidad:F2} x {item.PrecioUnitario:C2} = {item.SubTotal:C2}\n\n";
            }

            ticket += $@"================================
Subtotal: {venta.SubTotal:C2}
IVA: {venta.IVA:C2}
TOTAL: {venta.Total:C2}
================================";

            return ticket;
        }

        public void Dispose() { }
    }
}