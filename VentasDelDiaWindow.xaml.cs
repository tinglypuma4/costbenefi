using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class VentasDelDiaWindow : Window
    {
        private readonly AppDbContext _context;
        private DateTime _fechaDesde;
        private DateTime _fechaHasta;
        private string _filtroActivo = "Hoy";

        public VentasDelDiaWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;

            // Por defecto: hoy
            _fechaDesde = DateTime.Today;
            _fechaHasta = DateTime.Today;

            TxtFechaVentas.Text = DateTime.Today.ToString("dddd, dd 'de' MMMM 'de' yyyy");

            Loaded += async (s, e) => await CargarVentas();
        }

        private async System.Threading.Tasks.Task CargarVentas()
        {
            try
            {
                var fechaInicio = _fechaDesde.Date;
                var fechaFin = _fechaHasta.Date.AddDays(1);

                var ventas = await _context.Ventas
                    .Include(v => v.DetallesVenta)
                        .ThenInclude(dv => dv.RawMaterial)
                    .Where(v => v.FechaVenta >= fechaInicio && v.FechaVenta < fechaFin)
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                var ventasDTO = ventas.Select(v => new VentaDelDiaDTO
                {
                    VentaID = v.Id,
                    NumeroTicket = v.NumeroTicket.ToString(),
                    FechaVenta = v.FechaVenta,
                    Hora = v.FechaVenta.ToString("HH:mm:ss"),
                    Cliente = v.Cliente ?? "Público General",
                    Total = v.Total,
                    CostoTotal = v.DetallesVenta?.Sum(dv => dv.CostoUnitario * dv.Cantidad) ?? 0,
                    FormaPago = v.FormaPago ?? "Efectivo",
                    Usuario = v.Usuario ?? "Sistema",
                    CantidadProductos = v.DetallesVenta?.Sum(dv => (int)Math.Ceiling(dv.Cantidad)) ?? 0
                }).ToList();

                if (!ventasDTO.Any())
                {
                    TxtTotalVentas.Text = "0";
                    TxtMontoTotal.Text = "$0.00";
                    TxtGananciaTotal.Text = "$0.00";
                    TxtInfoAdicional.Text = $"No hay ventas en el período seleccionado ({_filtroActivo})";
                    ListaVentas.ItemsSource = null;
                    return;
                }

                // Actualizar resumen
                TxtTotalVentas.Text = ventasDTO.Count.ToString();
                TxtMontoTotal.Text = ventasDTO.Sum(v => v.Total).ToString("C2");
                TxtGananciaTotal.Text = ventasDTO.Sum(v => v.GananciaReal).ToString("C2");

                TxtInfoAdicional.Text = $"{_filtroActivo}: {ventasDTO.Count} ventas | " +
                                       $"Productos: {ventasDTO.Sum(v => v.CantidadProductos)} | " +
                                       $"Del {_fechaDesde:dd/MM/yyyy} al {_fechaHasta:dd/MM/yyyy}";

                ListaVentas.ItemsSource = ventasDTO;

                Debug.WriteLine($"✅ Ventas cargadas: {ventasDTO.Count} ({_filtroActivo})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"❌ Error: {ex}");
            }
        }

        private void ActualizarBotonesFiltro(Button botonActivo)
        {
            // Resetear todos los botones
            foreach (var btn in new[] { BtnFiltroHoy, BtnFiltroSemana, BtnFiltroMes, BtnFiltroTodas })
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)); // Transparente
                btn.BorderBrush = Brushes.White;
                btn.BorderThickness = new Thickness(1);
            }

            // Marcar el botón activo
            botonActivo.Background = new SolidColorBrush(Color.FromRgb(4, 120, 87));
            botonActivo.BorderThickness = new Thickness(0);
        }

        private async void BtnFiltroHoy_Click(object sender, RoutedEventArgs e)
        {
            _filtroActivo = "Hoy";
            _fechaDesde = DateTime.Today;
            _fechaHasta = DateTime.Today;

            TxtFechaVentas.Text = DateTime.Today.ToString("dddd, dd 'de' MMMM 'de' yyyy");
            ActualizarBotonesFiltro(BtnFiltroHoy);

            DpFechaDesde.SelectedDate = null;
            DpFechaHasta.SelectedDate = null;

            await CargarVentas();
        }

        private async void BtnFiltroSemana_Click(object sender, RoutedEventArgs e)
        {
            _filtroActivo = "Esta Semana";

            // Calcular inicio de semana (lunes)
            var hoy = DateTime.Today;
            var diaSemana = (int)hoy.DayOfWeek;
            var diasDesdeInicio = diaSemana == 0 ? 6 : diaSemana - 1; // Lunes = inicio

            _fechaDesde = hoy.AddDays(-diasDesdeInicio);
            _fechaHasta = _fechaDesde.AddDays(6);

            TxtFechaVentas.Text = $"Semana del {_fechaDesde:dd/MM} al {_fechaHasta:dd/MM/yyyy}";
            ActualizarBotonesFiltro(BtnFiltroSemana);

            DpFechaDesde.SelectedDate = null;
            DpFechaHasta.SelectedDate = null;

            await CargarVentas();
        }

        private async void BtnFiltroMes_Click(object sender, RoutedEventArgs e)
        {
            _filtroActivo = "Este Mes";

            _fechaDesde = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _fechaHasta = _fechaDesde.AddMonths(1).AddDays(-1);

            TxtFechaVentas.Text = DateTime.Today.ToString("MMMM 'de' yyyy");
            ActualizarBotonesFiltro(BtnFiltroMes);

            DpFechaDesde.SelectedDate = null;
            DpFechaHasta.SelectedDate = null;

            await CargarVentas();
        }

        private async void BtnFiltroTodas_Click(object sender, RoutedEventArgs e)
        {
            _filtroActivo = "Todas";

            // Desde hace 1 año hasta hoy
            _fechaDesde = DateTime.Today.AddYears(-1);
            _fechaHasta = DateTime.Today;

            TxtFechaVentas.Text = "Todas las ventas (último año)";
            ActualizarBotonesFiltro(BtnFiltroTodas);

            DpFechaDesde.SelectedDate = null;
            DpFechaHasta.SelectedDate = null;

            await CargarVentas();
        }

        private async void FechaPersonalizada_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (DpFechaDesde.SelectedDate.HasValue && DpFechaHasta.SelectedDate.HasValue)
            {
                _filtroActivo = "Personalizado";
                _fechaDesde = DpFechaDesde.SelectedDate.Value;
                _fechaHasta = DpFechaHasta.SelectedDate.Value;

                // Validar que fecha desde sea menor o igual a fecha hasta
                if (_fechaDesde > _fechaHasta)
                {
                    MessageBox.Show("La fecha inicial debe ser anterior a la fecha final.",
                                  "Fechas Inválidas", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TxtFechaVentas.Text = $"Del {_fechaDesde:dd/MM/yyyy} al {_fechaHasta:dd/MM/yyyy}";

                // Resetear botones
                foreach (var btn in new[] { BtnFiltroHoy, BtnFiltroSemana, BtnFiltroMes, BtnFiltroTodas })
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    btn.BorderBrush = Brushes.White;
                    btn.BorderThickness = new Thickness(1);
                }

                await CargarVentas();
            }
        }

        private async void BtnActualizarVentas_Click(object sender, RoutedEventArgs e)
        {
            await CargarVentas();
        }

        private async void BtnVerTicket_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag == null) return;

                int ventaID = (int)button.Tag;

                var venta = await _context.Ventas
                    .Include(v => v.DetallesVenta)
                        .ThenInclude(dv => dv.RawMaterial)
                    .FirstOrDefaultAsync(v => v.Id == ventaID);

                if (venta == null)
                {
                    MessageBox.Show("No se encontró la venta.", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal costoTotal = venta.DetallesVenta?
                    .Sum(dv => dv.CostoUnitario * dv.Cantidad) ?? 0;

                string ticket = GenerarTicket(venta, costoTotal);

                MessageBox.Show(ticket, $"Ticket #{venta.NumeroTicket}",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ticket: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"❌ Error: {ex}");
            }
        }

        private string GenerarTicket(Venta venta, decimal costoTotal)
        {
            var ticket = "═══════════════════════════════\n";
            ticket += $"       TICKET #{venta.NumeroTicket}\n";
            ticket += "═══════════════════════════════\n\n";

            ticket += $"📅 Fecha: {venta.FechaVenta:dd/MM/yyyy HH:mm:ss}\n";
            ticket += $"👤 Cliente: {venta.Cliente ?? "Público General"}\n";
            ticket += $"👨‍💼 Atendió: {venta.Usuario ?? "Sistema"}\n";
            ticket += $"💳 Pago: {venta.FormaPago ?? "Efectivo"}\n\n";

            ticket += "PRODUCTOS:\n";
            ticket += "───────────────────────────────\n";

            if (venta.DetallesVenta?.Any() == true)
            {
                foreach (var detalle in venta.DetallesVenta)
                {
                    var nombreProducto = detalle.NombreProducto ?? "Producto";
                    var costoLinea = detalle.CostoUnitario * detalle.Cantidad;
                    var gananciaLinea = detalle.SubTotal - costoLinea;

                    ticket += $"{nombreProducto}\n";
                    ticket += $"  {detalle.Cantidad:F2} x {detalle.PrecioUnitario:C2} = {detalle.SubTotal:C2}\n";

                    if (detalle.TieneDescuentoManual)
                    {
                        ticket += $"  (Desc: {detalle.TotalDescuentoLinea:C2} - {detalle.MotivoDescuentoDetalle})\n";
                    }

                    ticket += $"  (Costo: {costoLinea:C2} | Ganancia: {gananciaLinea:C2})\n";
                }
            }

            ticket += "───────────────────────────────\n";
            ticket += $"Subtotal:     {venta.SubTotal:C2}\n";

            if (venta.Descuento > 0)
                ticket += $"Descuento:   -{venta.Descuento:C2}\n";

            ticket += $"TOTAL:        {venta.Total:C2}\n\n";

            if (venta.ComisionTotal > 0)
            {
                ticket += "🏦 COMISIONES:\n";
                ticket += $"Comisión:     {venta.ComisionTarjeta:C2}\n";
                if (venta.IVAComision > 0)
                    ticket += $"IVA Comisión: {venta.IVAComision:C2}\n";
                ticket += $"Total Com.:   {venta.ComisionTotal:C2}\n";
                ticket += $"Total Neto:   {venta.TotalRealRecibido:C2}\n\n";
            }

            ticket += "═══════════════════════════════\n";
            ticket += $"💰 Costo Total:  {costoTotal:C2}\n";
            ticket += $"📈 GANANCIA:     {(venta.Total - costoTotal):C2}\n";
            ticket += $"📊 Margen:       {(venta.Total > 0 ? ((venta.Total - costoTotal) / venta.Total * 100) : 0):F1}%\n";
            ticket += "═══════════════════════════════\n";

            return ticket;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}