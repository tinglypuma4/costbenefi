using System;
using System.Windows;
using costbenefi.Data;

namespace costbenefi.Views
{
    public partial class ReporteSelectorWindow : Window
    {
        private readonly AppDbContext _context;

        public ReporteSelectorWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
        }

        private void BtnReporteMovimientos_Click(object sender, RoutedEventArgs e)
        {
            var movimientosWindow = new ReporteMovimientosWindow(_context);
            movimientosWindow.ShowDialog();
        }

        private void BtnReporteStock_Click(object sender, RoutedEventArgs e)
        {
            var stockWindow = new ReporteStockWindow(_context);
            stockWindow.ShowDialog();
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}