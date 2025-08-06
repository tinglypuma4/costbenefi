using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

// ===== USING PARA EL SISTEMA PDF Y EXCEL =====
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class ReporteVentasWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly VentasPDFService _pdfService;
        private readonly VentasExcelService _excelService;
        private List<Venta> _todasLasVentas;
        private List<Venta> _ventasFiltradas;
        private bool _cargaCompleta = false;
        private bool _filtrosExpandidos = false;

        public ReporteVentasWindow(AppDbContext context)
        {
            _context = context;
            _pdfService = new VentasPDFService(_context);
            _excelService = new VentasExcelService(_context);
            InitializeComponent();
            CargarDatosIniciales();
        }

        private async void CargarDatosIniciales()
        {
            try
            {
                // Cargar todas las ventas con sus detalles
                _todasLasVentas = await _context.Ventas
                    .Include(v => v.DetallesVenta)
                        .ThenInclude(d => d.RawMaterial)
                    .Where(v => v.Estado == "Completada")
                    .OrderByDescending(v => v.FechaVenta)
                    .ToListAsync();

                if (!_todasLasVentas.Any())
                {
                    MessageBox.Show("No hay ventas registradas en el sistema.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Cargar los controles de filtrado
                CargarFiltrosAvanzados();

                // Mostrar todos los datos inicialmente
                _ventasFiltradas = new List<Venta>(_todasLasVentas);
                ActualizarDataGrid();
                ActualizarEstadisticas();

                // Marcar como carga completa
                _cargaCompleta = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarFiltrosAvanzados()
        {
            try
            {
                // Cargar clientes en el popup
                PanelClientes.Children.Clear();
                var clientes = _todasLasVentas
                    .Select(v => v.Cliente)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c);

                foreach (var cliente in clientes)
                {
                    var checkbox = new CheckBox
                    {
                        Content = cliente,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        FontSize = 11
                    };
                    checkbox.Checked += FiltroCheckbox_Changed;
                    checkbox.Unchecked += FiltroCheckbox_Changed;
                    PanelClientes.Children.Add(checkbox);
                }

                // Cargar usuarios únicos
                PanelUsuarios.Children.Clear();
                var usuarios = _todasLasVentas
                    .Select(v => v.Usuario)
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Distinct()
                    .OrderBy(u => u);

                foreach (var usuario in usuarios)
                {
                    var checkbox = new CheckBox
                    {
                        Content = usuario,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        FontSize = 11
                    };
                    checkbox.Checked += FiltroCheckbox_Changed;
                    checkbox.Unchecked += FiltroCheckbox_Changed;
                    PanelUsuarios.Children.Add(checkbox);
                }

                // Cargar productos únicos vendidos
                PanelProductos.Children.Clear();
                var productosVendidos = _todasLasVentas
                    .SelectMany(v => v.DetallesVenta)
                    .Select(d => d.NombreProducto)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .OrderBy(p => p);

                foreach (var producto in productosVendidos)
                {
                    var checkbox = new CheckBox
                    {
                        Content = producto,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        Tag = producto,
                        FontSize = 11
                    };
                    checkbox.Checked += ProductoCheckbox_Changed;
                    checkbox.Unchecked += ProductoCheckbox_Changed;
                    PanelProductos.Children.Add(checkbox);
                }

                ActualizarContadorProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar filtros: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Event Handlers

        private void BtnToggleFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtrosExpandidos = !_filtrosExpandidos;
            PanelFiltrosDetallados.Visibility = _filtrosExpandidos ? Visibility.Visible : Visibility.Collapsed;
            BtnToggleFiltros.Content = _filtrosExpandidos ? "🔍 Ocultar Filtros" : "🔍 Mostrar Filtros";
        }

        private void BtnFiltroTodos_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
        }

        private void BtnFiltroHoy_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            var hoy = DateTime.Today;
            DpFechaInicio.SelectedDate = hoy;
            DpFechaFin.SelectedDate = hoy;
            ChkFiltrarPorFecha.IsChecked = true;
            AplicarFiltros();
        }

        private void BtnFiltroSemana_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            var inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            DpFechaInicio.SelectedDate = inicioSemana;
            DpFechaFin.SelectedDate = inicioSemana.AddDays(6);
            ChkFiltrarPorFecha.IsChecked = true;
            AplicarFiltros();
        }

        private void BtnFiltroMes_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DpFechaInicio.SelectedDate = inicioMes;
            DpFechaFin.SelectedDate = inicioMes.AddMonths(1).AddDays(-1);
            ChkFiltrarPorFecha.IsChecked = true;
            AplicarFiltros();
        }

        private void BtnFiltroVentasAltas_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            if (_todasLasVentas?.Any() == true)
            {
                var promedio = _todasLasVentas.Average(v => v.Total);
                ChkFiltrarPorMonto.IsChecked = true;
                TxtMontoMin.Text = (promedio * 1.5m).ToString("F2");
                AplicarFiltros();
            }
        }

        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {
            PopupClientes.IsOpen = !PopupClientes.IsOpen;
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            PopupUsuarios.IsOpen = !PopupUsuarios.IsOpen;
        }

        private void BtnProductos_Click(object sender, RoutedEventArgs e)
        {
            PopupProductos.IsOpen = !PopupProductos.IsOpen;
        }

        private void BtnAplicarClientes_Click(object sender, RoutedEventArgs e)
        {
            PopupClientes.IsOpen = false;
            AplicarFiltros();
        }

        private void BtnAplicarUsuarios_Click(object sender, RoutedEventArgs e)
        {
            PopupUsuarios.IsOpen = false;
            AplicarFiltros();
        }

        private void BtnAplicarProductos_Click(object sender, RoutedEventArgs e)
        {
            PopupProductos.IsOpen = false;
            AplicarFiltros();
        }

        #endregion

        private void ProductoCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_cargaCompleta && PanelProductos != null)
            {
                ActualizarContadorProductos();
            }
        }

        private void FiltroCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_cargaCompleta)
            {
                AplicarFiltros();
            }
        }

        private void ActualizarContadorProductos()
        {
            if (PanelProductos == null || TxtProductosSeleccionados == null) return;

            try
            {
                var seleccionados = PanelProductos.Children.OfType<CheckBox>()
                    .Count(cb => cb.IsChecked == true);
                TxtProductosSeleccionados.Text = seleccionados.ToString();

                // Actualizar texto del botón
                if (BtnProductos != null)
                {
                    var total = PanelProductos.Children.Count;
                    BtnProductos.Content = seleccionados == total ? "Todos seleccionados" :
                                          seleccionados == 0 ? "Ninguno seleccionado" :
                                          $"{seleccionados} de {total}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar contador: {ex.Message}");
            }
        }

        private void ChkTodosProductos_Checked(object sender, RoutedEventArgs e)
        {
            if (!_cargaCompleta || PanelProductos == null) return;

            try
            {
                foreach (CheckBox cb in PanelProductos.Children.OfType<CheckBox>())
                {
                    cb.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar todos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkTodosProductos_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_cargaCompleta || PanelProductos == null) return;

            try
            {
                foreach (CheckBox cb in PanelProductos.Children.OfType<CheckBox>())
                {
                    cb.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al deseleccionar todos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSeleccionarTodos_Click(object sender, RoutedEventArgs e)
        {
            ChkTodosProductos.IsChecked = true;
        }

        private void BtnDeseleccionarTodos_Click(object sender, RoutedEventArgs e)
        {
            ChkTodosProductos.IsChecked = false;
        }

        private void BtnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
        }

        private void AplicarFiltros()
        {
            if (!_cargaCompleta || _todasLasVentas == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 === APLICANDO FILTROS DE VENTAS ===");
                var ventasFiltradas = new List<Venta>(_todasLasVentas);
                var totalInicial = ventasFiltradas.Count;

                // ===== FILTRAR POR RANGO DE FECHAS =====
                if (ChkFiltrarPorFecha?.IsChecked == true)
                {
                    if (DpFechaInicio?.SelectedDate.HasValue == true)
                    {
                        var fechaInicio = DpFechaInicio.SelectedDate.Value.Date;
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.FechaVenta.Date >= fechaInicio)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"📅 Filtro fecha inicio: {ventasFiltradas.Count} ventas");
                    }

                    if (DpFechaFin?.SelectedDate.HasValue == true)
                    {
                        var fechaFin = DpFechaFin.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.FechaVenta <= fechaFin)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"📅 Filtro fecha fin: {ventasFiltradas.Count} ventas");
                    }
                }

                // ===== FILTRAR POR CLIENTES SELECCIONADOS =====
                if (PanelClientes != null)
                {
                    var clientesSeleccionados = PanelClientes.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();

                    if (clientesSeleccionados.Any())
                    {
                        var totalClientes = PanelClientes.Children.OfType<CheckBox>().Count();

                        // Solo filtrar si no están todos seleccionados
                        if (clientesSeleccionados.Count < totalClientes)
                        {
                            ventasFiltradas = ventasFiltradas
                                .Where(v => clientesSeleccionados.Contains(v.Cliente))
                                .ToList();
                            System.Diagnostics.Debug.WriteLine($"👥 Filtro clientes ({clientesSeleccionados.Count}): {ventasFiltradas.Count} ventas");
                        }
                    }
                }

                // ===== FILTRAR POR USUARIOS SELECCIONADOS =====
                if (PanelUsuarios != null)
                {
                    var usuariosSeleccionados = PanelUsuarios.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();

                    if (usuariosSeleccionados.Any())
                    {
                        var totalUsuarios = PanelUsuarios.Children.OfType<CheckBox>().Count();

                        // Solo filtrar si no están todos seleccionados
                        if (usuariosSeleccionados.Count < totalUsuarios)
                        {
                            ventasFiltradas = ventasFiltradas
                                .Where(v => usuariosSeleccionados.Contains(v.Usuario))
                                .ToList();
                            System.Diagnostics.Debug.WriteLine($"👤 Filtro usuarios ({usuariosSeleccionados.Count}): {ventasFiltradas.Count} ventas");
                        }
                    }
                }

                // ===== FILTRAR POR PRODUCTOS SELECCIONADOS =====
                if (PanelProductos != null)
                {
                    var productosSeleccionados = PanelProductos.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true && cb.Tag is string)
                        .Select(cb => cb.Tag.ToString())
                        .ToList();

                    if (productosSeleccionados.Any())
                    {
                        var totalProductos = PanelProductos.Children.OfType<CheckBox>().Count();

                        // Solo filtrar si no están todos seleccionados
                        if (productosSeleccionados.Count < totalProductos)
                        {
                            ventasFiltradas = ventasFiltradas
                                .Where(v => v.DetallesVenta.Any(d => productosSeleccionados.Contains(d.NombreProducto)))
                                .ToList();
                            System.Diagnostics.Debug.WriteLine($"📦 Filtro productos ({productosSeleccionados.Count}): {ventasFiltradas.Count} ventas");
                        }
                    }
                }

                // ===== FILTROS DE DESCUENTOS =====

                // ✅ FILTRAR SOLO VENTAS CON DESCUENTOS
                if (ChkSoloConDescuentos?.IsChecked == true)
                {
                    ventasFiltradas = ventasFiltradas
                        .Where(v => v.TieneDescuentosAplicados)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"🎁 Filtro solo con descuentos: {ventasFiltradas.Count} ventas");
                }

                // ✅ FILTRAR POR TIPO DE AUTORIZADOR DE DESCUENTOS
                var tipoAutorizadorSeleccionado = CmbTipoAutorizador?.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(tipoAutorizadorSeleccionado) && tipoAutorizadorSeleccionado != "Todos")
                {
                    ventasFiltradas = ventasFiltradas
                        .Where(v => v.TieneDescuentosAplicados && v.TipoUsuarioAutorizador == tipoAutorizadorSeleccionado)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"👨‍💼 Filtro autorizador {tipoAutorizadorSeleccionado}: {ventasFiltradas.Count} ventas");
                }

                // ✅ FILTRAR POR RANGO DE PORCENTAJE DE DESCUENTO
                if (ChkFiltrarPorPorcentajeDescuento?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtPorcentajeDescuentoMin?.Text, out decimal porcentajeMin))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.PorcentajeDescuentoTotal >= porcentajeMin)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"📊 Filtro % desc. mín {porcentajeMin}%: {ventasFiltradas.Count} ventas");
                    }

                    if (decimal.TryParse(TxtPorcentajeDescuentoMax?.Text, out decimal porcentajeMax))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.PorcentajeDescuentoTotal <= porcentajeMax)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"📊 Filtro % desc. máx {porcentajeMax}%: {ventasFiltradas.Count} ventas");
                    }
                }

                // ✅ FILTRAR POR RANGO DE MONTO DE DESCUENTO
                if (ChkFiltrarPorMontoDescuento?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtMontoDescuentoMin?.Text, out decimal montoDescMin))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.TotalDescuentosAplicados >= montoDescMin)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"💰 Filtro monto desc. mín ${montoDescMin}: {ventasFiltradas.Count} ventas");
                    }

                    if (decimal.TryParse(TxtMontoDescuentoMax?.Text, out decimal montoDescMax))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.TotalDescuentosAplicados <= montoDescMax)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"💰 Filtro monto desc. máx ${montoDescMax}: {ventasFiltradas.Count} ventas");
                    }
                }

                // ✅ FILTRAR POR MOTIVO DE DESCUENTO (BÚSQUEDA DE TEXTO)
                if (!string.IsNullOrWhiteSpace(TxtBuscarMotivoDescuento?.Text))
                {
                    var motivoBusqueda = TxtBuscarMotivoDescuento.Text.Trim().ToLower();
                    ventasFiltradas = ventasFiltradas
                        .Where(v => v.TieneDescuentosAplicados &&
                                   !string.IsNullOrEmpty(v.MotivoDescuentoGeneral) &&
                                   v.MotivoDescuentoGeneral.ToLower().Contains(motivoBusqueda))
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"🔍 Filtro motivo '{motivoBusqueda}': {ventasFiltradas.Count} ventas");
                }

                // ✅ FILTRAR SOLO DESCUENTOS DE ALTO IMPACTO (MÁS DEL 15%)
                if (ChkSoloDescuentosAltoImpacto?.IsChecked == true)
                {
                    ventasFiltradas = ventasFiltradas
                        .Where(v => v.TieneDescuentosAplicados && v.PorcentajeDescuentoTotal > 15)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"⚡ Filtro alto impacto (>15%): {ventasFiltradas.Count} ventas");
                }

                // ===== FILTROS TRADICIONALES =====

                // ✅ FILTRAR POR RANGO DE MONTO TOTAL
                if (ChkFiltrarPorMonto?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtMontoMin?.Text, out decimal montoMin))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.Total >= montoMin)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"💰 Filtro monto mín ${montoMin}: {ventasFiltradas.Count} ventas");
                    }

                    if (decimal.TryParse(TxtMontoMax?.Text, out decimal montoMax))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.Total <= montoMax)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"💰 Filtro monto máx ${montoMax}: {ventasFiltradas.Count} ventas");
                    }
                }

                // ✅ FILTRAR POR RANGO DE MARGEN
                if (ChkFiltrarPorMargen?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtMargenMin?.Text, out decimal margenMin))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.MargenNeto >= margenMin)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"📈 Filtro margen mín {margenMin}%: {ventasFiltradas.Count} ventas");
                    }

                    if (decimal.TryParse(TxtMargenMax?.Text, out decimal margenMax))
                    {
                        ventasFiltradas = ventasFiltradas
                            .Where(v => v.MargenNeto <= margenMax)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"📈 Filtro margen máx {margenMax}%: {ventasFiltradas.Count} ventas");
                    }
                }

                // ✅ FILTRAR SOLO VENTAS CON COMISIÓN
                if (ChkSoloConComision?.IsChecked == true)
                {
                    ventasFiltradas = ventasFiltradas
                        .Where(v => v.ComisionTotal > 0)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"🏦 Filtro solo con comisión: {ventasFiltradas.Count} ventas");
                }

                // ✅ FILTRAR SOLO VENTAS RENTABLES
                if (ChkSoloRentables?.IsChecked == true)
                {
                    ventasFiltradas = ventasFiltradas
                        .Where(v => v.GananciaNeta > 0)
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"📈 Filtro solo rentables: {ventasFiltradas.Count} ventas");
                }

                // ✅ FILTRAR POR FORMA DE PAGO
                var formaPagoSeleccionada = CmbFormaPago?.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(formaPagoSeleccionada) && formaPagoSeleccionada != "Todas las formas")
                {
                    ventasFiltradas = ventasFiltradas.Where(v =>
                    {
                        return formaPagoSeleccionada switch
                        {
                            "💵 Solo Efectivo" => v.MontoEfectivo > 0 && v.MontoTarjeta == 0 && v.MontoTransferencia == 0,
                            "💳 Solo Tarjeta" => v.MontoTarjeta > 0 && v.MontoEfectivo == 0 && v.MontoTransferencia == 0,
                            "📱 Solo Transferencia" => v.MontoTransferencia > 0 && v.MontoEfectivo == 0 && v.MontoTarjeta == 0,
                            "🔄 Pago Combinado" => (v.MontoEfectivo > 0 ? 1 : 0) + (v.MontoTarjeta > 0 ? 1 : 0) + (v.MontoTransferencia > 0 ? 1 : 0) > 1,
                            _ => true
                        };
                    }).ToList();
                    System.Diagnostics.Debug.WriteLine($"💳 Filtro forma pago {formaPagoSeleccionada}: {ventasFiltradas.Count} ventas");
                }

                // ===== APLICAR FILTROS FINALES =====
                _ventasFiltradas = ventasFiltradas;

                System.Diagnostics.Debug.WriteLine($"🎯 RESULTADO FINAL: {ventasFiltradas.Count} de {totalInicial} ventas mostradas");

                // ✅ CALCULAR ESTADÍSTICAS DE DESCUENTOS PARA DEBUG
                var ventasConDescuento = ventasFiltradas.Count(v => v.TieneDescuentosAplicados);
                var totalDescuentos = ventasFiltradas.Sum(v => v.TotalDescuentosAplicados);
                var promedioDescuento = ventasConDescuento > 0 ? totalDescuentos / ventasConDescuento : 0;

                System.Diagnostics.Debug.WriteLine($"🎁 ESTADÍSTICAS DESCUENTOS:");
                System.Diagnostics.Debug.WriteLine($"   • Ventas con descuento: {ventasConDescuento}");
                System.Diagnostics.Debug.WriteLine($"   • Total descuentos: ${totalDescuentos:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Promedio descuento: ${promedioDescuento:F2}");

                ActualizarDataGrid();
                ActualizarEstadisticas();

                System.Diagnostics.Debug.WriteLine("✅ Filtros aplicados y interfaz actualizada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando filtros: {ex}");
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LimpiarFiltros()
        {
            if (!_cargaCompleta) return;

            try
            {
                // Limpiar filtros de fecha
                if (ChkFiltrarPorFecha != null)
                {
                    ChkFiltrarPorFecha.IsChecked = false;
                }
                if (DpFechaInicio != null) DpFechaInicio.SelectedDate = null;
                if (DpFechaFin != null) DpFechaFin.SelectedDate = null;

                // Seleccionar todos los clientes
                if (PanelClientes != null)
                {
                    foreach (CheckBox cb in PanelClientes.Children.OfType<CheckBox>())
                    {
                        cb.IsChecked = true;
                    }
                }

                // Seleccionar todos los usuarios
                if (PanelUsuarios != null)
                {
                    foreach (CheckBox cb in PanelUsuarios.Children.OfType<CheckBox>())
                    {
                        cb.IsChecked = true;
                    }
                }

                // Seleccionar todos los productos
                if (ChkTodosProductos != null)
                {
                    ChkTodosProductos.IsChecked = true;
                }

                // Limpiar filtros de rango
                if (ChkFiltrarPorMonto != null)
                {
                    ChkFiltrarPorMonto.IsChecked = false;
                }
                if (TxtMontoMin != null) TxtMontoMin.Text = "";
                if (TxtMontoMax != null) TxtMontoMax.Text = "";

                if (ChkFiltrarPorMargen != null)
                {
                    ChkFiltrarPorMargen.IsChecked = false;
                }
                if (TxtMargenMin != null) TxtMargenMin.Text = "";
                if (TxtMargenMax != null) TxtMargenMax.Text = "";

                if (ChkSoloConComision != null)
                {
                    ChkSoloConComision.IsChecked = false;
                }

                if (ChkSoloRentables != null)
                {
                    ChkSoloRentables.IsChecked = false;
                }

                // Cerrar popups
                if (PopupClientes != null) PopupClientes.IsOpen = false;
                if (PopupUsuarios != null) PopupUsuarios.IsOpen = false;
                if (PopupProductos != null) PopupProductos.IsOpen = false;

                // Aplicar filtros limpios
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al limpiar filtros: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarDataGrid()
        {
            if (DgVentas == null || _ventasFiltradas == null) return;

            try
            {
                var datosReporte = _ventasFiltradas.Select(v => new
                {
                    Ticket = v.NumeroTicket,
                    Fecha = v.FechaVenta.ToString("dd/MM/yyyy HH:mm"),
                    Cliente = v.Cliente ?? "Sin cliente",
                    Usuario = v.Usuario ?? "Sin usuario",
                    SubTotal = v.SubTotal,
                    Total = v.Total,
                    TipoPago = DeterminarTipoPago(v),
                    MontoEfectivo = v.MontoEfectivo,
                    MontoTarjeta = v.MontoTarjeta,
                    MontoTransferencia = v.MontoTransferencia,
                    Ganancia = v.GananciaNeta,
                    Margen = v.MargenNeto,
                    Comision = v.ComisionTotal,

                    // ✅ COLUMNAS DE DESCUENTOS USANDO PROPIEDADES EXISTENTES
                    TieneDescuento = v.TieneDescuentosAplicados ? "🎁 Sí" : "❌ No",
                    TotalDescuento = v.TotalDescuentosAplicados,
                    PorcentajeDescuento = v.PorcentajeDescuentoTotal,
                    AutorizadoPor = v.UsuarioAutorizadorDescuento ?? "",
                    TipoAutorizador = v.TipoUsuarioAutorizador ?? "",
                    MotivoDescuento = v.MotivoDescuentoGeneral ?? "",
                    ItemsConDescuento = v.CantidadItemsConDescuento,
                    FechaDescuento = v.FechaHoraDescuento?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    ResumenCompleto = v.ResumenDescuentos
                }).ToList();

                DgVentas.ItemsSource = datosReporte;

                // ✅ ACTUALIZAR RESUMEN CON ESTADÍSTICAS DE DESCUENTOS
                var totalDescuentos = _ventasFiltradas.Sum(v => v.TotalDescuentosAplicados);
                var ventasConDescuento = _ventasFiltradas.Count(v => v.TieneDescuentosAplicados);
                var porcentajeVentasConDescuento = _ventasFiltradas.Any()
                    ? (decimal)ventasConDescuento / _ventasFiltradas.Count * 100
                    : 0;

                if (TxtResumenDetalle != null)
                {
                    TxtResumenDetalle.Text = $"Mostrando {datosReporte.Count} de {_todasLasVentas.Count} ventas | " +
                                           $"{ventasConDescuento} con descuentos ({porcentajeVentasConDescuento:F1}%) | " +
                                           $"Total desc.: ${totalDescuentos:F2}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar datos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string DeterminarTipoPago(Venta venta)
        {
            var metodos = new List<string>();
            if (venta.MontoEfectivo > 0) metodos.Add("💵");
            if (venta.MontoTarjeta > 0) metodos.Add("💳");
            if (venta.MontoTransferencia > 0) metodos.Add("📱");

            return metodos.Count switch
            {
                0 => "❓ Sin datos",
                1 => metodos[0] + " Único",
                _ => "🔄 Combinado"
            };
        }
        private string DeterminarFormaPago(Venta venta)
        {
            var formas = new List<string>();
            if (venta.MontoEfectivo > 0) formas.Add("💵");
            if (venta.MontoTarjeta > 0) formas.Add("💳");
            if (venta.MontoTransferencia > 0) formas.Add("📱");

            if (formas.Count > 1)
                return "🔄 Combinado";
            else if (formas.Count == 1)
                return formas[0] + " " + (venta.MontoEfectivo > 0 ? "Efectivo" : 
                                         venta.MontoTarjeta > 0 ? "Tarjeta" : "Transferencia");
            else
                return "❓ Desconocido";
        }

        private void ActualizarEstadisticas()
        {
            try
            {
                if (_ventasFiltradas?.Any() == true)
                {
                    var totalVentas = _ventasFiltradas.Count;
                    var totalIngresos = _ventasFiltradas.Sum(v => v.Total);
                    var gananciaTotal = _ventasFiltradas.Sum(v => v.GananciaNeta);
                    var comisionTotal = _ventasFiltradas.Sum(v => v.ComisionTotal);
                    var margenPromedio = totalIngresos > 0 ? (gananciaTotal / totalIngresos) * 100 : 0;
                    var promedioTicket = totalVentas > 0 ? totalIngresos / totalVentas : 0;

                    if (TxtEstadisticas != null)
                    {
                        TxtEstadisticas.Text = $"🎯 Ventas: {totalVentas} | 💰 Ingresos: {totalIngresos:C0} | 📈 Ganancia: {gananciaTotal:C0}";
                    }

                    // Mostrar alertas de comisión si hay una cantidad significativa
                    if (comisionTotal > 0)
                    {
                        if (TxtAlertaComision != null)
                        {
                            TxtAlertaComision.Text = $"💳 Comisiones: {comisionTotal:C2}";
                        }
                        if (BorderAlertaComision != null)
                        {
                            BorderAlertaComision.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (BorderAlertaComision != null)
                        {
                            BorderAlertaComision.Visibility = Visibility.Collapsed;
                        }
                    }

                    Title = $"🎯 Reporte de Ventas - {totalVentas} ventas | {totalIngresos:C0} | Margen: {margenPromedio:F1}%";
                }
                else
                {
                    if (TxtEstadisticas != null)
                    {
                        TxtEstadisticas.Text = "🎯 Ventas: 0 | 💰 Ingresos: $0.00";
                    }
                    if (BorderAlertaComision != null)
                    {
                        BorderAlertaComision.Visibility = Visibility.Collapsed;
                    }
                    Title = "🎯 Reporte de Ventas - Sin datos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar estadísticas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region ===== EXPORTACIÓN PDF =====

        private async void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validar que hay datos para exportar
                if (_ventasFiltradas == null || !_ventasFiltradas.Any())
                {
                    MessageBox.Show("No hay ventas para exportar.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Obtener configuración del reporte
                var periodo = ObtenerPeriodoSeleccionado();
                var tipoFormato = ObtenerTipoFormatoSeleccionado();

                // 3. Preparar filtros aplicados
                var filtrosAplicados = PrepararFiltrosAplicados();

                // 4. Mostrar confirmación
                var confirmacion = MostrarConfirmacionExportacion(periodo, tipoFormato, filtrosAplicados);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                // 5. Mostrar indicador de carga
                MostrarIndicadorCarga(true);

                try
                {
                    // 6. Generar PDF
                    var rutaPDF = await _pdfService.GenerarReportePDFAsync(
                        _ventasFiltradas,
                        periodo,
                        tipoFormato,
                        filtrosAplicados);

                    if (!string.IsNullOrEmpty(rutaPDF))
                    {
                        // 7. Abrir PDF automáticamente
                        _pdfService.AbrirPDF(rutaPDF);

                        // 8. Mostrar mensaje de éxito
                        MostrarMensajeExito(periodo, tipoFormato, _ventasFiltradas.Count);
                    }
                    else
                    {
                        MessageBox.Show("Error al generar el reporte PDF.\n\nPor favor, verifique:\n" +
                                      "• Que tenga permisos de escritura en la carpeta seleccionada\n" +
                                      "• Que no haya archivos PDF abiertos con el mismo nombre\n" +
                                      "• Que el servicio PDF esté funcionando correctamente",
                            "Error de Generación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Error de permisos: No se puede escribir en la ubicación seleccionada.\n\n" +
                                  "Por favor, elija una carpeta diferente o ejecute la aplicación como administrador.",
                        "Error de Permisos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    var mensajeError = $"Error inesperado al generar el PDF:\n\n" +
                                     $"Tipo: {ex.GetType().Name}\n" +
                                     $"Mensaje: {ex.Message}\n\n" +
                                     "Por favor, contacte al administrador del sistema.";

                    MessageBox.Show(mensajeError, "Error de Generación",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    System.Diagnostics.Debug.WriteLine($"Error PDF Ventas: {ex}");
                }
                finally
                {
                    MostrarIndicadorCarga(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                MostrarIndicadorCarga(false);
            }
        }

        #endregion

        #region ===== EXPORTACIÓN EXCEL =====

        private async void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validar que hay datos para exportar
                if (_ventasFiltradas == null || !_ventasFiltradas.Any())
                {
                    MessageBox.Show("No hay ventas para exportar.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Obtener configuración del reporte
                var periodo = ObtenerPeriodoSeleccionado();
                var tipoFormato = ObtenerTipoFormatoSeleccionado();

                // 3. Preparar filtros aplicados
                var filtrosAplicados = PrepararFiltrosAplicados();

                // 4. Mostrar confirmación
                var confirmacion = MostrarConfirmacionExportacionExcel(periodo, tipoFormato, filtrosAplicados);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                // 5. Mostrar indicador de carga
                MostrarIndicadorCargaExcel(true);

                try
                {
                    // 6. Generar Excel
                    var rutaExcel = await _excelService.GenerarReporteExcelAsync(
                        _ventasFiltradas,
                        periodo,
                        tipoFormato,
                        filtrosAplicados);

                    if (!string.IsNullOrEmpty(rutaExcel))
                    {
                        // 7. Abrir Excel automáticamente
                        _excelService.AbrirExcel(rutaExcel);

                        // 8. Mostrar mensaje de éxito
                        MostrarMensajeExitoExcel(periodo, tipoFormato, _ventasFiltradas.Count);
                    }
                    else
                    {
                        MessageBox.Show("Error al generar el reporte Excel.\n\nPor favor, verifique:\n" +
                                      "• Que tenga permisos de escritura en la carpeta seleccionada\n" +
                                      "• Que no haya archivos Excel abiertos con el mismo nombre\n" +
                                      "• Que ClosedXML esté instalado correctamente",
                            "Error de Generación", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    var mensajeError = $"Error inesperado al generar el Excel:\n\n" +
                                     $"Tipo: {ex.GetType().Name}\n" +
                                     $"Mensaje: {ex.Message}\n\n" +
                                     "Por favor, contacte al administrador del sistema.";

                    MessageBox.Show(mensajeError, "Error de Generación",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    System.Diagnostics.Debug.WriteLine($"Error Excel Ventas: {ex}");
                }
                finally
                {
                    MostrarIndicadorCargaExcel(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                MostrarIndicadorCargaExcel(false);
            }
        }

        #endregion

        #region Métodos de Utilidad

        private PeriodoReporte ObtenerPeriodoSeleccionado()
        {
            return CmbPeriodo?.SelectedIndex switch
            {
                0 => PeriodoReporte.Dia,
                1 => PeriodoReporte.Semana,
                2 => PeriodoReporte.Mes,
                3 => PeriodoReporte.Año,
                _ => PeriodoReporte.Mes
            };
        }

        private TipoFormatoReporte ObtenerTipoFormatoSeleccionado()
        {
            return CmbTipoReporte?.SelectedIndex switch
            {
                0 => TipoFormatoReporte.Estandar,
                1 => TipoFormatoReporte.Ejecutivo,
                2 => TipoFormatoReporte.Detallado,
                3 => TipoFormatoReporte.PorProductos,
                4 => TipoFormatoReporte.PorClientes,
                5 => TipoFormatoReporte.Financiero,
                _ => TipoFormatoReporte.Estandar
            };
        }

        private FiltrosAplicados PrepararFiltrosAplicados()
        {
            var filtros = new FiltrosAplicados();

            try
            {
                // Obtener clientes seleccionados
                if (PanelClientes != null)
                {
                    filtros.ClientesSeleccionados = PanelClientes.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();
                }

                // Obtener usuarios seleccionados
                if (PanelUsuarios != null)
                {
                    filtros.UsuariosSeleccionados = PanelUsuarios.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();
                }

                // Obtener productos seleccionados
                if (PanelProductos != null)
                {
                    filtros.ProductosSeleccionados = PanelProductos.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true && cb.Tag is string)
                        .Select(cb => cb.Tag.ToString())
                        .ToList();
                }

                // Filtros adicionales
                filtros.SoloVentasConComision = ChkSoloConComision?.IsChecked == true;
                filtros.SoloVentasRentables = ChkSoloRentables?.IsChecked == true;

                if (decimal.TryParse(TxtMontoMin?.Text, out decimal montoMin))
                    filtros.MontoMinimo = montoMin;

                if (decimal.TryParse(TxtMontoMax?.Text, out decimal montoMax))
                    filtros.MontoMaximo = montoMax;

                if (decimal.TryParse(TxtMargenMin?.Text, out decimal margenMin))
                    filtros.MargenMinimo = margenMin;

                if (decimal.TryParse(TxtMargenMax?.Text, out decimal margenMax))
                    filtros.MargenMaximo = margenMax;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al preparar filtros: {ex.Message}");
            }

            return filtros;
        }

        private MessageBoxResult MostrarConfirmacionExportacion(PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros)
        {
            var mensaje = new StringBuilder();
            mensaje.AppendLine("¿Generar reporte PDF de ventas con la siguiente configuración?");
            mensaje.AppendLine();
            mensaje.AppendLine($"📊 Formato: {ObtenerNombreFormato(tipoFormato)}");
            mensaje.AppendLine($"📅 Período: {ObtenerNombrePeriodo(periodo)}");
            mensaje.AppendLine($"🎯 Ventas: {_ventasFiltradas.Count:N0}");
            mensaje.AppendLine($"💰 Ingresos totales: {_ventasFiltradas.Sum(v => v.Total):C2}");
            mensaje.AppendLine($"📈 Ganancia neta: {_ventasFiltradas.Sum(v => v.GananciaNeta):C2}");

            var comisionesTotal = _ventasFiltradas.Sum(v => v.ComisionTotal);
            if (comisionesTotal > 0)
            {
                mensaje.AppendLine($"💳 Comisiones: {comisionesTotal:C2}");
            }

            if (filtros.TieneFiltrosAplicados)
            {
                mensaje.AppendLine();
                mensaje.AppendLine("🔍 Filtros aplicados:");
                if (filtros.ClientesSeleccionados.Any())
                    mensaje.AppendLine($"   • Clientes: {filtros.ClientesSeleccionados.Count}");
                if (filtros.UsuariosSeleccionados.Any())
                    mensaje.AppendLine($"   • Usuarios: {filtros.UsuariosSeleccionados.Count}");
                if (filtros.ProductosSeleccionados.Any())
                    mensaje.AppendLine($"   • Productos: {filtros.ProductosSeleccionados.Count}");
                if (filtros.SoloVentasRentables)
                    mensaje.AppendLine($"   • Solo ventas rentables");
                if (filtros.SoloVentasConComision)
                    mensaje.AppendLine($"   • Solo ventas con comisión");
            }

            return MessageBox.Show(mensaje.ToString(), "Confirmar Exportación PDF",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private MessageBoxResult MostrarConfirmacionExportacionExcel(PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicados filtros)
        {
            var mensaje = new StringBuilder();
            mensaje.AppendLine("¿Generar reporte Excel de ventas con la siguiente configuración?");
            mensaje.AppendLine();
            mensaje.AppendLine($"📊 Formato: {ObtenerNombreFormato(tipoFormato)}");
            mensaje.AppendLine($"📅 Período: {ObtenerNombrePeriodo(periodo)}");
            mensaje.AppendLine($"🎯 Ventas: {_ventasFiltradas.Count:N0}");
            mensaje.AppendLine($"💰 Ingresos totales: {_ventasFiltradas.Sum(v => v.Total):C2}");

            mensaje.AppendLine();
            mensaje.AppendLine("📋 El Excel incluirá las siguientes hojas:");
            mensaje.AppendLine("   • 📊 Resumen Ejecutivo");
            mensaje.AppendLine("   • 💰 Detalle de Ventas");
            mensaje.AppendLine("   • 📦 Análisis por Productos");
            mensaje.AppendLine("   • 👥 Análisis por Clientes");
            mensaje.AppendLine("   • 👤 Análisis por Usuarios");
            mensaje.AppendLine("   • 💳 Formas de Pago");
            mensaje.AppendLine("   • 📅 Análisis Temporal");
            mensaje.AppendLine("   • 📈 Rentabilidad");

            return MessageBox.Show(mensaje.ToString(), "Confirmar Exportación Excel",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private void MostrarMensajeExito(PeriodoReporte periodo, TipoFormatoReporte formato, int totalVentas)
        {
            var mensaje = $"✅ ¡Reporte PDF de ventas generado exitosamente!\n\n" +
                          $"📄 Formato: {ObtenerNombreFormato(formato)}\n" +
                          $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                          $"🎯 Ventas incluidas: {totalVentas:N0}\n" +
                          $"💰 Ingresos totales: {_ventasFiltradas.Sum(v => v.Total):C2}\n\n" +
                          $"El archivo PDF se abrió automáticamente.";

            MessageBox.Show(mensaje, "PDF Generado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MostrarMensajeExitoExcel(PeriodoReporte periodo, TipoFormatoReporte formato, int totalVentas)
        {
            var mensaje = $"✅ ¡Reporte Excel de ventas generado exitosamente!\n\n" +
                          $"📊 Formato: {ObtenerNombreFormato(formato)}\n" +
                          $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                          $"🎯 Ventas incluidas: {totalVentas:N0}\n" +
                          $"💰 Ingresos totales: {_ventasFiltradas.Sum(v => v.Total):C2}\n\n" +
                          $"📋 Análisis completo con 8 hojas de datos.\n\n" +
                          $"El archivo Excel se abrió automáticamente.";

            MessageBox.Show(mensaje, "Excel Generado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MostrarIndicadorCarga(bool mostrar)
        {
            Cursor = mostrar ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;

            if (BtnExportarPDF != null)
            {
                BtnExportarPDF.IsEnabled = !mostrar;
                BtnExportarPDF.Content = mostrar ? "⏳ Generando PDF..." : "📄 Exportar PDF";
            }

            if (CmbPeriodo != null) CmbPeriodo.IsEnabled = !mostrar;
            if (CmbTipoReporte != null) CmbTipoReporte.IsEnabled = !mostrar;
        }

        private void MostrarIndicadorCargaExcel(bool mostrar)
        {
            Cursor = mostrar ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;

            if (BtnExportarExcel != null)
            {
                BtnExportarExcel.IsEnabled = !mostrar;
                BtnExportarExcel.Content = mostrar ? "⏳ Generando Excel..." : "📊 Exportar Excel";
            }

            if (CmbPeriodo != null) CmbPeriodo.IsEnabled = !mostrar;
            if (CmbTipoReporte != null) CmbTipoReporte.IsEnabled = !mostrar;
            if (BtnExportarPDF != null) BtnExportarPDF.IsEnabled = !mostrar;
        }

        private string ObtenerNombreFormato(TipoFormatoReporte formato)
        {
            return formato switch
            {
                TipoFormatoReporte.Ejecutivo => "Ejecutivo",
                TipoFormatoReporte.Detallado => "Detallado",
                TipoFormatoReporte.PorProductos => "Por Productos",
                TipoFormatoReporte.PorClientes => "Por Clientes",
                TipoFormatoReporte.PorUsuarios => "Por Usuarios",
                TipoFormatoReporte.Financiero => "Financiero",
                _ => "Estándar"
            };
        }

        private string ObtenerNombrePeriodo(PeriodoReporte periodo)
        {
            return periodo switch
            {
                PeriodoReporte.Dia => "Diario",
                PeriodoReporte.Semana => "Semanal",
                PeriodoReporte.Mes => "Mensual",
                PeriodoReporte.Año => "Anual",
                _ => "Personalizado"
            };
        }

        #endregion

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}