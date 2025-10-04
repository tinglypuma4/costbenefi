using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class ReporteCorteCajaWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly CorteCajaPDFService _pdfService;
        private readonly CorteCajaExcelService _excelService;
        private List<CorteCaja> _todosLosCortes;
        private List<CorteCaja> _cortesFiltrados;
        private bool _cargaCompleta = false;
        private bool _filtrosExpandidos = false;

        public ReporteCorteCajaWindow(AppDbContext context)
        {
            _context = context;
            _pdfService = new CorteCajaPDFService(_context);
            _excelService = new CorteCajaExcelService(_context);
            InitializeComponent();
            CargarDatosIniciales();
        }

        private async void CargarDatosIniciales()
        {
            try
            {
                // Cargar todos los cortes
                _todosLosCortes = await _context.CortesCaja
                    .OrderByDescending(c => c.FechaCorte)
                    .ToListAsync();

                if (!_todosLosCortes.Any())
                {
                    MessageBox.Show("No hay cortes de caja registrados en el sistema.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Cargar los controles de filtrado
                CargarFiltrosAvanzados();

                // Mostrar todos los datos inicialmente
                _cortesFiltrados = new List<CorteCaja>(_todosLosCortes);
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
                // Cargar usuarios en el popup
                PanelUsuarios.Children.Clear();
                var usuarios = _todosLosCortes
                    .Select(c => c.UsuarioCorte)
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

                // Cargar estados en el popup
                PanelEstados.Children.Clear();
                var estados = new[] { "Completado", "Pendiente", "Cancelado" };

                foreach (var estado in estados)
                {
                    var checkbox = new CheckBox
                    {
                        Content = estado,
                        IsChecked = true,
                        Margin = new Thickness(0, 2, 0, 2),
                        FontSize = 11
                    };
                    checkbox.Checked += FiltroCheckbox_Changed;
                    checkbox.Unchecked += FiltroCheckbox_Changed;
                    PanelEstados.Children.Add(checkbox);
                }
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

        private void BtnFiltroConDiferencias_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
            ChkSoloConDiferencias.IsChecked = true;
            AplicarFiltros();
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            PopupUsuarios.IsOpen = !PopupUsuarios.IsOpen;
        }

        private void BtnEstados_Click(object sender, RoutedEventArgs e)
        {
            PopupEstados.IsOpen = !PopupEstados.IsOpen;
        }

        private void BtnAplicarUsuarios_Click(object sender, RoutedEventArgs e)
        {
            PopupUsuarios.IsOpen = false;
            AplicarFiltros();
        }

        private void BtnAplicarEstados_Click(object sender, RoutedEventArgs e)
        {
            PopupEstados.IsOpen = false;
            AplicarFiltros();
        }

        private void FiltroCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (_cargaCompleta)
            {
                AplicarFiltros();
            }
        }

        private void BtnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFiltros();
        }

        #endregion

        private void AplicarFiltros()
        {
            if (!_cargaCompleta || _todosLosCortes == null) return;

            try
            {
                Debug.WriteLine("🔍 === APLICANDO FILTROS DE CORTES ===");
                var cortesFiltrados = new List<CorteCaja>(_todosLosCortes);
                var totalInicial = cortesFiltrados.Count;

                // ===== FILTRAR POR RANGO DE FECHAS =====
                if (ChkFiltrarPorFecha?.IsChecked == true)
                {
                    if (DpFechaInicio?.SelectedDate.HasValue == true)
                    {
                        var fechaInicio = DpFechaInicio.SelectedDate.Value.Date;
                        cortesFiltrados = cortesFiltrados
                            .Where(c => c.FechaCorte >= fechaInicio)
                            .ToList();
                        Debug.WriteLine($"📅 Filtro fecha inicio: {cortesFiltrados.Count} cortes");
                    }

                    if (DpFechaFin?.SelectedDate.HasValue == true)
                    {
                        var fechaFin = DpFechaFin.SelectedDate.Value.Date;
                        cortesFiltrados = cortesFiltrados
                            .Where(c => c.FechaCorte <= fechaFin)
                            .ToList();
                        Debug.WriteLine($"📅 Filtro fecha fin: {cortesFiltrados.Count} cortes");
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

                        if (usuariosSeleccionados.Count < totalUsuarios)
                        {
                            cortesFiltrados = cortesFiltrados
                                .Where(c => usuariosSeleccionados.Contains(c.UsuarioCorte))
                                .ToList();
                            Debug.WriteLine($"👤 Filtro usuarios ({usuariosSeleccionados.Count}): {cortesFiltrados.Count} cortes");
                        }
                    }
                }

                // ===== FILTRAR POR ESTADOS SELECCIONADOS =====
                if (PanelEstados != null)
                {
                    var estadosSeleccionados = PanelEstados.Children.OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();

                    if (estadosSeleccionados.Any())
                    {
                        var totalEstados = PanelEstados.Children.OfType<CheckBox>().Count();

                        if (estadosSeleccionados.Count < totalEstados)
                        {
                            cortesFiltrados = cortesFiltrados
                                .Where(c => estadosSeleccionados.Contains(c.Estado))
                                .ToList();
                            Debug.WriteLine($"📊 Filtro estados ({estadosSeleccionados.Count}): {cortesFiltrados.Count} cortes");
                        }
                    }
                }

                // ===== FILTRAR POR RANGO DE MONTO =====
                if (ChkFiltrarPorMonto?.IsChecked == true)
                {
                    if (decimal.TryParse(TxtMontoMin?.Text, out decimal montoMin))
                    {
                        cortesFiltrados = cortesFiltrados
                            .Where(c => c.TotalVentasCalculado >= montoMin)
                            .ToList();
                        Debug.WriteLine($"💰 Filtro monto mín ${montoMin}: {cortesFiltrados.Count} cortes");
                    }

                    if (decimal.TryParse(TxtMontoMax?.Text, out decimal montoMax))
                    {
                        cortesFiltrados = cortesFiltrados
                            .Where(c => c.TotalVentasCalculado <= montoMax)
                            .ToList();
                        Debug.WriteLine($"💰 Filtro monto máx ${montoMax}: {cortesFiltrados.Count} cortes");
                    }
                }

                // ===== FILTRAR SOLO CON DIFERENCIAS =====
                if (ChkSoloConDiferencias?.IsChecked == true)
                {
                    cortesFiltrados = cortesFiltrados
                        .Where(c => !c.DiferenciaAceptable && c.Estado == "Completado")
                        .ToList();
                    Debug.WriteLine($"⚠️ Filtro solo con diferencias: {cortesFiltrados.Count} cortes");
                }

                // ===== FILTRAR SOLO CON COMISIONES =====
                if (ChkSoloConComisiones?.IsChecked == true)
                {
                    cortesFiltrados = cortesFiltrados
                        .Where(c => c.ComisionesTotalesCalculadas > 0)
                        .ToList();
                    Debug.WriteLine($"🏦 Filtro solo con comisiones: {cortesFiltrados.Count} cortes");
                }

                // ===== FILTRAR SOLO COMPLETADOS =====
                if (ChkSoloCompletados?.IsChecked == true)
                {
                    cortesFiltrados = cortesFiltrados
                        .Where(c => c.Estado == "Completado")
                        .ToList();
                    Debug.WriteLine($"✅ Filtro solo completados: {cortesFiltrados.Count} cortes");
                }

                _cortesFiltrados = cortesFiltrados;

                Debug.WriteLine($"🎯 RESULTADO FINAL: {cortesFiltrados.Count} de {totalInicial} cortes mostrados");

                ActualizarDataGrid();
                ActualizarEstadisticas();

                Debug.WriteLine("✅ Filtros aplicados y interfaz actualizada");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error aplicando filtros: {ex}");
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

                // Seleccionar todos los usuarios
                if (PanelUsuarios != null)
                {
                    foreach (CheckBox cb in PanelUsuarios.Children.OfType<CheckBox>())
                    {
                        cb.IsChecked = true;
                    }
                }

                // Seleccionar todos los estados
                if (PanelEstados != null)
                {
                    foreach (CheckBox cb in PanelEstados.Children.OfType<CheckBox>())
                    {
                        cb.IsChecked = true;
                    }
                }

                // Limpiar filtros de rango
                if (ChkFiltrarPorMonto != null)
                {
                    ChkFiltrarPorMonto.IsChecked = false;
                }
                if (TxtMontoMin != null) TxtMontoMin.Text = "";
                if (TxtMontoMax != null) TxtMontoMax.Text = "";

                if (ChkSoloConDiferencias != null)
                {
                    ChkSoloConDiferencias.IsChecked = false;
                }

                if (ChkSoloConComisiones != null)
                {
                    ChkSoloConComisiones.IsChecked = false;
                }

                if (ChkSoloCompletados != null)
                {
                    ChkSoloCompletados.IsChecked = false;
                }

                // Cerrar popups
                if (PopupUsuarios != null) PopupUsuarios.IsOpen = false;
                if (PopupEstados != null) PopupEstados.IsOpen = false;

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
            if (DgCortes == null || _cortesFiltrados == null) return;

            try
            {
                var datosReporte = _cortesFiltrados.Select(c => new
                {
                    Fecha = c.FechaCorte.ToString("dd/MM/yyyy"),
                    Hora = c.FechaHoraCorte.ToString("HH:mm"),
                    Usuario = c.UsuarioCorte ?? "Sin usuario",
                    Estado = c.ObtenerEstadoDescriptivo(),
                    Tickets = c.CantidadTickets,
                    TotalVentas = c.TotalVentasCalculado,
                    EfectivoEsperado = c.EfectivoEsperado,
                    EfectivoContado = c.EfectivoContado,
                    Diferencia = c.DiferenciaEfectivo,
                    Tarjeta = c.TarjetaCalculado,
                    Transferencia = c.TransferenciaCalculado,
                    Comisiones = c.ComisionesTotalesCalculadas,
                    Ganancia = c.GananciaNetaCalculada,
                    Margen = c.TotalVentasCalculado > 0
                        ? (c.GananciaNetaCalculada / c.TotalVentasCalculado) * 100
                        : 0
                }).ToList();

                DgCortes.ItemsSource = datosReporte;

                // Actualizar resumen
                var totalDiferencias = _cortesFiltrados
                    .Where(c => !c.DiferenciaAceptable && c.Estado == "Completado")
                    .Sum(c => Math.Abs(c.DiferenciaEfectivo));

                if (TxtResumenDetalle != null)
                {
                    var cortesConDiferencias = _cortesFiltrados.Count(c => !c.DiferenciaAceptable && c.Estado == "Completado");
                    TxtResumenDetalle.Text = $"Mostrando {datosReporte.Count} de {_todosLosCortes.Count} cortes | " +
                                           $"{cortesConDiferencias} con diferencias | " +
                                           $"Total diferencias: ${totalDiferencias:F2}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar datos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarEstadisticas()
        {
            try
            {
                if (_cortesFiltrados?.Any() == true)
                {
                    var totalCortes = _cortesFiltrados.Count;
                    var totalVentas = _cortesFiltrados.Sum(c => c.TotalVentasCalculado);
                    var gananciaTotal = _cortesFiltrados.Sum(c => c.GananciaNetaCalculada);
                    var comisionTotal = _cortesFiltrados.Sum(c => c.ComisionesTotalesCalculadas);
                    var diferenciasTotal = _cortesFiltrados
                        .Where(c => c.Estado == "Completado")
                        .Sum(c => Math.Abs(c.DiferenciaEfectivo));

                    if (TxtEstadisticas != null)
                    {
                        TxtEstadisticas.Text = $"💰 Cortes: {totalCortes} | Total Ventas: {totalVentas:C0} | Ganancia: {gananciaTotal:C0}";
                    }

                    // Mostrar alertas de diferencias
                    if (diferenciasTotal > 10)
                    {
                        if (TxtAlertaDiferencias != null)
                        {
                            TxtAlertaDiferencias.Text = $"⚠️ Diferencias: {diferenciasTotal:C2}";
                        }
                        if (BorderAlertaDiferencias != null)
                        {
                            BorderAlertaDiferencias.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (BorderAlertaDiferencias != null)
                        {
                            BorderAlertaDiferencias.Visibility = Visibility.Collapsed;
                        }
                    }

                    Title = $"📊 Reporte de Cortes - {totalCortes} cortes | {totalVentas:C0}";
                }
                else
                {
                    if (TxtEstadisticas != null)
                    {
                        TxtEstadisticas.Text = "💰 Cortes: 0 | Total: $0.00";
                    }
                    if (BorderAlertaDiferencias != null)
                    {
                        BorderAlertaDiferencias.Visibility = Visibility.Collapsed;
                    }
                    Title = "📊 Reporte de Cortes - Sin datos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar estadísticas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Exportación PDF

        private async void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cortesFiltrados == null || !_cortesFiltrados.Any())
                {
                    MessageBox.Show("No hay cortes para exportar.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var periodo = ObtenerPeriodoSeleccionado();
                var tipoFormato = ObtenerTipoFormatoSeleccionado();
                var filtrosAplicados = PrepararFiltrosAplicados();

                var confirmacion = MostrarConfirmacionExportacion(periodo, tipoFormato, filtrosAplicados);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                MostrarIndicadorCarga(true);

                try
                {
                    var rutaPDF = await _pdfService.GenerarReportePDFAsync(
                        _cortesFiltrados,
                        periodo,
                        tipoFormato,
                        filtrosAplicados);

                    if (!string.IsNullOrEmpty(rutaPDF))
                    {
                        _pdfService.AbrirPDF(rutaPDF);
                        MostrarMensajeExito(periodo, tipoFormato, _cortesFiltrados.Count);
                    }
                    else
                    {
                        MessageBox.Show("Error al generar el reporte PDF.", "Error de Generación",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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

        #region Exportación Excel

        private async void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cortesFiltrados == null || !_cortesFiltrados.Any())
                {
                    MessageBox.Show("No hay cortes para exportar.", "Sin Datos",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var periodo = ObtenerPeriodoSeleccionado();
                var tipoFormato = ObtenerTipoFormatoSeleccionado();
                var filtrosAplicados = PrepararFiltrosAplicados();

                var confirmacion = MostrarConfirmacionExportacionExcel(periodo, tipoFormato, filtrosAplicados);
                if (confirmacion != MessageBoxResult.Yes)
                    return;

                MostrarIndicadorCargaExcel(true);

                try
                {
                    var rutaExcel = await _excelService.GenerarReporteExcelAsync(
                        _cortesFiltrados,
                        periodo,
                        tipoFormato,
                        filtrosAplicados);

                    if (!string.IsNullOrEmpty(rutaExcel))
                    {
                        _excelService.AbrirExcel(rutaExcel);
                        MostrarMensajeExitoExcel(periodo, tipoFormato, _cortesFiltrados.Count);
                    }
                    else
                    {
                        MessageBox.Show("Error al generar el reporte Excel.", "Error de Generación",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                3 => TipoFormatoReporte.PorUsuarios,
                4 => TipoFormatoReporte.Financiero,
                _ => TipoFormatoReporte.Estandar
            };
        }

        private FiltrosAplicadosCorte PrepararFiltrosAplicados()
        {
            var filtros = new FiltrosAplicadosCorte();

            try
            {
                if (PanelUsuarios != null)
                {
                    filtros.UsuariosSeleccionados = PanelUsuarios.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();
                }

                if (PanelEstados != null)
                {
                    filtros.EstadosSeleccionados = PanelEstados.Children
                        .OfType<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Content.ToString())
                        .ToList();
                }

                filtros.SoloConDiferencias = ChkSoloConDiferencias?.IsChecked == true;
                filtros.SoloConComisiones = ChkSoloConComisiones?.IsChecked == true;

                if (decimal.TryParse(TxtMontoMin?.Text, out decimal montoMin))
                    filtros.MontoMinimo = montoMin;

                if (decimal.TryParse(TxtMontoMax?.Text, out decimal montoMax))
                    filtros.MontoMaximo = montoMax;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al preparar filtros: {ex.Message}");
            }

            return filtros;
        }

        private MessageBoxResult MostrarConfirmacionExportacion(PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicadosCorte filtros)
        {
            var mensaje = $"¿Generar reporte PDF de cortes con la siguiente configuración?\n\n" +
                         $"📊 Formato: {ObtenerNombreFormato(tipoFormato)}\n" +
                         $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                         $"💰 Cortes: {_cortesFiltrados.Count:N0}\n" +
                         $"💵 Total Ventas: {_cortesFiltrados.Sum(c => c.TotalVentasCalculado):C2}\n" +
                         $"📈 Ganancia Neta: {_cortesFiltrados.Sum(c => c.GananciaNetaCalculada):C2}";

            var comisionesTotal = _cortesFiltrados.Sum(c => c.ComisionesTotalesCalculadas);
            if (comisionesTotal > 0)
            {
                mensaje += $"\n🏦 Comisiones: {comisionesTotal:C2}";
            }

            return MessageBox.Show(mensaje, "Confirmar Exportación PDF",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private MessageBoxResult MostrarConfirmacionExportacionExcel(PeriodoReporte periodo, TipoFormatoReporte tipoFormato, FiltrosAplicadosCorte filtros)
        {
            var mensaje = $"¿Generar reporte Excel de cortes con la siguiente configuración?\n\n" +
                         $"📊 Formato: {ObtenerNombreFormato(tipoFormato)}\n" +
                         $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                         $"💰 Cortes: {_cortesFiltrados.Count:N0}\n\n" +
                         $"📋 El Excel incluirá 8 hojas:\n" +
                         $"   • Resumen Ejecutivo\n" +
                         $"   • Detalle de Cortes\n" +
                         $"   • Por Usuarios\n" +
                         $"   • Análisis Temporal\n" +
                         $"   • Formas de Pago\n" +
                         $"   • Comisiones\n" +
                         $"   • Diferencias\n" +
                         $"   • Rentabilidad";

            return MessageBox.Show(mensaje, "Confirmar Exportación Excel",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private void MostrarMensajeExito(PeriodoReporte periodo, TipoFormatoReporte formato, int totalCortes)
        {
            var mensaje = $"✅ ¡Reporte PDF de cortes generado exitosamente!\n\n" +
                          $"📄 Formato: {ObtenerNombreFormato(formato)}\n" +
                          $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                          $"💰 Cortes incluidos: {totalCortes:N0}\n" +
                          $"💵 Total Ventas: {_cortesFiltrados.Sum(c => c.TotalVentasCalculado):C2}\n\n" +
                          $"El archivo PDF se abrió automáticamente.";

            MessageBox.Show(mensaje, "PDF Generado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MostrarMensajeExitoExcel(PeriodoReporte periodo, TipoFormatoReporte formato, int totalCortes)
        {
            var mensaje = $"✅ ¡Reporte Excel de cortes generado exitosamente!\n\n" +
                          $"📊 Formato: {ObtenerNombreFormato(formato)}\n" +
                          $"📅 Período: {ObtenerNombrePeriodo(periodo)}\n" +
                          $"💰 Cortes incluidos: {totalCortes:N0}\n" +
                          $"💵 Total Ventas: {_cortesFiltrados.Sum(c => c.TotalVentasCalculado):C2}\n\n" +
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