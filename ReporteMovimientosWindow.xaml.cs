using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using System.IO;
using System.Diagnostics;

namespace costbenefi.Views
{
    public partial class ReporteMovimientosWindow : Window
    {
        private readonly AppDbContext _context;
        private List<Movimiento> _allMovimientos;
        private List<Movimiento> _filteredMovimientos;

        public ReporteMovimientosWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            InitializeDefaults();
            LoadData();
        }

        private void InitializeDefaults()
        {
            // Inicializar listas vacías para evitar errores de colección nula
            _allMovimientos = new List<Movimiento>();
            _filteredMovimientos = new List<Movimiento>();
        }

        private async void LoadData()
        {
            try
            {
                // Cargar productos para el filtro
                var productos = await _context.RawMaterials
                    .OrderBy(m => m.NombreArticulo)
                    .Select(m => m.NombreArticulo)
                    .Distinct()
                    .ToListAsync();

                CmbProducto.Items.Clear();
                CmbProducto.Items.Add("Todos");
                foreach (var producto in productos)
                {
                    CmbProducto.Items.Add(producto);
                }
                CmbProducto.SelectedIndex = 0;

                // Seleccionar "Todos" por defecto en los otros ComboBox
                if (CmbTipoMovimiento.Items.Count > 0)
                    CmbTipoMovimiento.SelectedIndex = 0;

                if (CmbPeriodo.Items.Count > 0)
                    CmbPeriodo.SelectedIndex = 0;

                // Cargar todos los movimientos
                _allMovimientos = await _context.Movimientos
                    .Include(m => m.RawMaterial)
                    .OrderByDescending(m => m.FechaMovimiento)
                    .ToListAsync();

                // Verificar si hay datos
                if (_allMovimientos == null)
                    _allMovimientos = new List<Movimiento>();

                _filteredMovimientos = new List<Movimiento>(_allMovimientos);
                UpdateDataGrid();

                // Mostrar mensaje si no hay datos
                if (!_allMovimientos.Any())
                {
                    MessageBox.Show("No hay movimientos registrados en la base de datos.", "Información",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos: {ex.Message}\n\nDetalles: {ex.InnerException?.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Asegurar que las listas no sean nulas
                if (_allMovimientos == null) _allMovimientos = new List<Movimiento>();
                if (_filteredMovimientos == null) _filteredMovimientos = new List<Movimiento>();
                UpdateDataGrid();
            }
        }

        private void UpdateDataGrid()
        {
            try
            {
                DgMovimientos.ItemsSource = null;

                // Verificar que la lista no sea nula
                if (_filteredMovimientos != null)
                {
                    DgMovimientos.ItemsSource = _filteredMovimientos;
                }
                else
                {
                    DgMovimientos.ItemsSource = new List<Movimiento>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar la tabla: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FiltrarMovimientos(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar que tenemos datos para filtrar
                if (_allMovimientos == null || !_allMovimientos.Any())
                {
                    _filteredMovimientos = new List<Movimiento>();
                    UpdateDataGrid();
                    return;
                }

                _filteredMovimientos = new List<Movimiento>(_allMovimientos);

                // Filtrar por producto
                if (CmbProducto.SelectedItem != null && CmbProducto.SelectedItem.ToString() != "Todos")
                {
                    string producto = CmbProducto.SelectedItem.ToString();
                    _filteredMovimientos = _filteredMovimientos
                        .Where(m => m.RawMaterial?.NombreArticulo == producto)
                        .ToList();
                }

                // Filtrar por tipo de movimiento
                if (CmbTipoMovimiento.SelectedItem != null &&
                    CmbTipoMovimiento.SelectedItem is ComboBoxItem tipoItem &&
                    tipoItem.Content.ToString() != "Todos")
                {
                    string tipo = tipoItem.Content.ToString();
                    _filteredMovimientos = _filteredMovimientos
                        .Where(m => m.TipoMovimiento == tipo)
                        .ToList();
                }

                // Filtrar por período y fecha
                bool tienePeriodo = CmbPeriodo.SelectedItem != null &&
                                   CmbPeriodo.SelectedItem is ComboBoxItem periodoItem &&
                                   periodoItem.Content.ToString() != "Todos";

                bool tieneFecha = DpFecha.SelectedDate.HasValue;

                if (tienePeriodo && tieneFecha)
                {
                    DateTime fechaSeleccionada = DpFecha.SelectedDate.Value;
                    string periodo = ((ComboBoxItem)CmbPeriodo.SelectedItem).Content.ToString();

                    switch (periodo)
                    {
                        case "Diario":
                            _filteredMovimientos = _filteredMovimientos
                                .Where(m => m.FechaMovimiento.Date == fechaSeleccionada.Date)
                                .ToList();
                            break;

                        case "Semanal":
                            DateTime inicioSemana = fechaSeleccionada.AddDays(-(int)fechaSeleccionada.DayOfWeek + 1);
                            if (fechaSeleccionada.DayOfWeek == DayOfWeek.Sunday)
                                inicioSemana = inicioSemana.AddDays(-7);
                            DateTime finSemana = inicioSemana.AddDays(7);

                            _filteredMovimientos = _filteredMovimientos
                                .Where(m => m.FechaMovimiento >= inicioSemana && m.FechaMovimiento < finSemana)
                                .ToList();
                            break;

                        case "Mensual":
                            DateTime inicioMes = new DateTime(fechaSeleccionada.Year, fechaSeleccionada.Month, 1);
                            DateTime finMes = inicioMes.AddMonths(1);
                            _filteredMovimientos = _filteredMovimientos
                                .Where(m => m.FechaMovimiento >= inicioMes && m.FechaMovimiento < finMes)
                                .ToList();
                            break;

                        case "Anual":
                            DateTime inicioAnio = new DateTime(fechaSeleccionada.Year, 1, 1);
                            DateTime finAnio = new DateTime(fechaSeleccionada.Year + 1, 1, 1);
                            _filteredMovimientos = _filteredMovimientos
                                .Where(m => m.FechaMovimiento >= inicioAnio && m.FechaMovimiento < finAnio)
                                .ToList();
                            break;
                    }
                }

                UpdateDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al filtrar datos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // En caso de error, mostrar todos los datos disponibles
                _filteredMovimientos = new List<Movimiento>(_allMovimientos ?? new List<Movimiento>());
                UpdateDataGrid();
            }
        }

        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_filteredMovimientos == null || _filteredMovimientos.Count == 0)
                {
                    MessageBox.Show("No hay datos para exportar.", "Información",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string latexContent = GenerateLatexContent();
                string tempPath = Path.Combine(Path.GetTempPath(), "ReporteMovimientos.tex");
                File.WriteAllText(tempPath, latexContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "pdflatex",
                    Arguments = $"-output-directory={Path.GetTempPath()} \"{tempPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        string pdfPath = Path.ChangeExtension(tempPath, ".pdf");
                        Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
                        MessageBox.Show("✅ Reporte generado y abierto.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        MessageBox.Show($"Error al generar PDF: {error}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar PDF: {ex.Message}\n\nAsegúrese de tener pdflatex instalado.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateLatexContent()
        {
            string rows = "";

            if (_filteredMovimientos != null && _filteredMovimientos.Any())
            {
                rows = string.Join("\n", _filteredMovimientos.Select((m, index) =>
                    $"{m.Id} & {m.RawMaterial?.NombreArticulo?.Replace("&", "\\&").Replace("%", "\\%") ?? "N/A"} & " +
                    $"{m.TipoMovimiento?.Replace("&", "\\&") ?? "N/A"} & {m.Cantidad:F2} & {m.UnidadMedida?.Replace("&", "\\&") ?? "N/A"} & " +
                    $"\\${m.PrecioConIVA:F2} & \\${m.PrecioSinIVA:F2} & " +
                    $"{m.Motivo?.Replace("&", "\\&").Replace("%", "\\%") ?? "N/A"} & {m.Usuario?.Replace("&", "\\&") ?? "N/A"} & " +
                    $"{m.FechaMovimiento:yyyy-MM-dd HH:mm} \\\\" +
                    (index % 2 == 0 ? " \\rowcolor{rowgray}" : "")));
            }

            decimal totalEntradas = _filteredMovimientos?
                .Where(m => m.TipoMovimiento == "Entrada")
                .Sum(m => m.Cantidad) ?? 0;

            decimal totalSalidas = _filteredMovimientos?
                .Where(m => m.TipoMovimiento == "Salida")
                .Sum(m => m.Cantidad) ?? 0;

            // Obtener información de filtros aplicados
            string filtrosAplicados = "";
            if (CmbProducto.SelectedItem?.ToString() != "Todos")
                filtrosAplicados += $"Producto: {CmbProducto.SelectedItem} | ";
            if (CmbTipoMovimiento.SelectedItem is ComboBoxItem tipoItem && tipoItem.Content.ToString() != "Todos")
                filtrosAplicados += $"Tipo: {tipoItem.Content} | ";
            if (CmbPeriodo.SelectedItem is ComboBoxItem periodoItem && periodoItem.Content.ToString() != "Todos" && DpFecha.SelectedDate.HasValue)
                filtrosAplicados += $"Período: {periodoItem.Content} ({DpFecha.SelectedDate.Value:dd/MM/yyyy}) | ";

            if (string.IsNullOrEmpty(filtrosAplicados))
                filtrosAplicados = "Sin filtros aplicados";
            else
                filtrosAplicados = filtrosAplicados.TrimEnd(' ', '|');

            return $@"
\documentclass[a4paper,11pt]{{article}}
\usepackage[utf8]{{inputenc}}
\usepackage[T1]{{fontenc}}
\usepackage{{lmodern}}
\usepackage{{geometry}}
\geometry{{margin=0.8in}}
\usepackage{{booktabs}}
\usepackage{{longtable}}
\usepackage{{pdflscape}}
\usepackage{{xcolor}}
\usepackage{{datetime}}

\definecolor{{headerblue}}{{RGB}}{{30,60,114}}
\definecolor{{rowgray}}{{RGB}}{{245,247,250}}

\title{{\textbf{{Reporte de Movimientos - Sistema Costo-Beneficio}}}}
\date{{\today}}

\begin{{document}}

\maketitle

\section*{{Filtros Aplicados}}
{filtrosAplicados.Replace("&", "\\&")}

\section*{{Resumen}}
\begin{{itemize}}
    \item \textbf{{Total de movimientos:}} {_filteredMovimientos?.Count ?? 0}
    \item \textbf{{Total de entradas:}} {totalEntradas:F2} unidades
    \item \textbf{{Total de salidas:}} {totalSalidas:F2} unidades
    \item \textbf{{Diferencia neta:}} {(totalEntradas - totalSalidas):F2} unidades
\end{{itemize}}

\newpage
\section*{{Detalle de Movimientos}}
\begin{{landscape}}
\begin{{longtable}}{{|p{{0.8cm}}|p{{2.5cm}}|p{{1.5cm}}|p{{1.2cm}}|p{{1.2cm}}|p{{1.8cm}}|p{{1.8cm}}|p{{3cm}}|p{{1.5cm}}|p{{2.5cm}}|}}
    \hline
    \rowcolor{{headerblue}}
    \textcolor{{white}}{{\textbf{{ID}}}} &
    \textcolor{{white}}{{\textbf{{Producto}}}} &
    \textcolor{{white}}{{\textbf{{Tipo}}}} &
    \textcolor{{white}}{{\textbf{{Cant.}}}} &
    \textcolor{{white}}{{\textbf{{Unidad}}}} &
    \textcolor{{white}}{{\textbf{{Precio Con IVA}}}} &
    \textcolor{{white}}{{\textbf{{Precio Sin IVA}}}} &
    \textcolor{{white}}{{\textbf{{Motivo}}}} &
    \textcolor{{white}}{{\textbf{{Usuario}}}} &
    \textcolor{{white}}{{\textbf{{Fecha}}}} \\
    \hline
    \endhead
    {rows}
    \hline
\end{{longtable}}
\end{{landscape}}

\end{{document}}";
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}