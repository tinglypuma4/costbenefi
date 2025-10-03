using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class ConfigurarComisionesWindow : Window
    {
        private readonly AppDbContext _context;
        private ConfiguracionComisiones? _configuracionActual;

        public ConfigurarComisionesWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            Loaded += ConfigurarComisionesWindow_Loaded;
        }

        private async void ConfigurarComisionesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarConfiguracionActual();
        }

        private async System.Threading.Tasks.Task CargarConfiguracionActual()
        {
            try
            {
                _configuracionActual = await _context.GetOrCreateConfiguracionComisionesAsync();

                TxtPorcentajeComision.Text = _configuracionActual.PorcentajeComisionTarjeta.ToString("F2");
                ChkCobraIVA.IsChecked = _configuracionActual.TerminalCobraIVA;
                TxtPorcentajeIVA.Text = _configuracionActual.PorcentajeIVA.ToString("F2");

                ActualizarVistaPreviaComision();

                if (!string.IsNullOrEmpty(_configuracionActual.UsuarioModificacion))
                {
                    TxtUltimaModificacion.Text = $"Última modificación: {_configuracionActual.FechaActualizacion:dd/MM/yyyy HH:mm} por {_configuracionActual.UsuarioModificacion}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarVistaPreviaComision()
        {
            try
            {
                // ✅ VALIDAR que los controles existan antes de usarlos
                if (TxtPorcentajeComision == null || TxtPorcentajeIVA == null ||
                    TxtVistaPrevia == null || TxtPorcentajeEfectivo == null)
                {
                    return; // Salir si los controles no están inicializados
                }

                if (decimal.TryParse(TxtPorcentajeComision.Text, out decimal porcentaje) &&
                    decimal.TryParse(TxtPorcentajeIVA.Text, out decimal porcentajeIVA))
                {
                    var cobraIVA = ChkCobraIVA?.IsChecked == true;

                    decimal montoEjemplo = 1000m;
                    decimal comisionBase = montoEjemplo * (porcentaje / 100);
                    decimal ivaComision = cobraIVA ? comisionBase * (porcentajeIVA / 100) : 0;
                    decimal comisionTotal = comisionBase + ivaComision;
                    decimal netoRecibido = montoEjemplo - comisionTotal;

                    TxtVistaPrevia.Text = $"💳 Ejemplo con venta de ${montoEjemplo:F2}:\n\n" +
                                         $"  • Comisión base: ${comisionBase:F2} ({porcentaje:F2}%)\n";

                    if (cobraIVA)
                    {
                        TxtVistaPrevia.Text += $"  • IVA sobre comisión: ${ivaComision:F2} ({porcentajeIVA:F2}%)\n";
                    }

                    TxtVistaPrevia.Text += $"  • Total comisión: ${comisionTotal:F2}\n" +
                                          $"  • Neto recibido: ${netoRecibido:F2}";

                    decimal porcentajeEfectivo = (comisionTotal / montoEjemplo) * 100;
                    TxtPorcentajeEfectivo.Text = $"Comisión efectiva total: {porcentajeEfectivo:F2}%";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarVistaPreviaComision: {ex.Message}");

                if (TxtVistaPrevia != null)
                    TxtVistaPrevia.Text = "⚠️ Ingrese valores válidos para ver el ejemplo";

                if (TxtPorcentajeEfectivo != null)
                    TxtPorcentajeEfectivo.Text = "";
            }
        }

        private void TxtPorcentajeComision_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarVistaPreviaComision();
        }

        private void TxtPorcentajeIVA_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarVistaPreviaComision();
        }

        private void ChkCobraIVA_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelIVA != null)
                PanelIVA.Visibility = Visibility.Visible;

            ActualizarVistaPreviaComision();
        }

        private void ChkCobraIVA_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PanelIVA != null)
                PanelIVA.Visibility = Visibility.Collapsed;

            ActualizarVistaPreviaComision();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(TxtPorcentajeComision.Text, out decimal porcentajeComision) ||
                    porcentajeComision < 0 || porcentajeComision > 100)
                {
                    MessageBox.Show("Ingrese un porcentaje de comisión válido (0-100).",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPorcentajeComision.Focus();
                    return;
                }

                var cobraIVA = ChkCobraIVA.IsChecked == true;
                decimal porcentajeIVA = 16.0m;

                if (cobraIVA)
                {
                    if (!decimal.TryParse(TxtPorcentajeIVA.Text, out porcentajeIVA) ||
                        porcentajeIVA < 0 || porcentajeIVA > 100)
                    {
                        MessageBox.Show("Ingrese un porcentaje de IVA válido (0-100).",
                                      "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtPorcentajeIVA.Focus();
                        return;
                    }
                }

                var mensaje = $"¿Guardar la configuración de comisiones?\n\n" +
                             $"💳 Comisión base: {porcentajeComision:F2}%\n" +
                             $"🏦 Cobra IVA: {(cobraIVA ? "Sí" : "No")}\n";

                if (cobraIVA)
                {
                    mensaje += $"📊 IVA: {porcentajeIVA:F2}%\n" +
                              $"⚡ Comisión efectiva: {porcentajeComision * (1 + porcentajeIVA / 100):F2}%";
                }

                var resultado = MessageBox.Show(mensaje, "Confirmar Configuración",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado != MessageBoxResult.Yes)
                    return;

                BtnGuardar.IsEnabled = false;
                BtnGuardar.Content = "⏳ Guardando...";

                _configuracionActual.PorcentajeComisionTarjeta = porcentajeComision;
                _configuracionActual.TerminalCobraIVA = cobraIVA;
                _configuracionActual.PorcentajeIVA = porcentajeIVA;
                _configuracionActual.FechaActualizacion = DateTime.Now;
                _configuracionActual.UsuarioModificacion = UserService.UsuarioActual?.NombreUsuario ?? Environment.UserName;

                await _context.SaveChangesAsync();

                MessageBox.Show($"✅ Configuración guardada exitosamente!\n\n" +
                               $"{_configuracionActual.ResumenConfiguracion}\n\n" +
                               "Esta configuración se aplicará a todas las ventas futuras.",
                               "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                BtnGuardar.IsEnabled = true;
                BtnGuardar.Content = "💾 Guardar Configuración";
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}