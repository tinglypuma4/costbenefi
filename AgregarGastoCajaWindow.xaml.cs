using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using System.Threading.Tasks; // Asegúrate de tener este using para Tareas Asíncronas

namespace costbenefi.Views
{
    public partial class AgregarGastoCajaWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly DateTime _fecha;
        private bool _actualizandoMontos = false;

        public bool GastoGuardado { get; private set; } = false;

        public AgregarGastoCajaWindow(AppDbContext context, DateTime fecha)
        {
            InitializeComponent();
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fecha = fecha.Date;

            TxtFecha.Text = $"Fecha: {_fecha:dddd, dd/MM/yyyy}";
            TxtConcepto.Focus();
        }

        // ===== VALIDACIÓN NUMÉRICA =====

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite números, punto decimal y comas (para miles, aunque TryParseDecimal las quita)
            Regex regex = new Regex(@"^[0-9.,]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private bool TryParseDecimal(string text, out decimal result)
        {
            // Limpia comas de miles y usa InvariantCulture para aceptar el punto decimal
            return decimal.TryParse(text?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        // ===== EVENTOS DE CAMBIO =====

        private void TxtMontoTotal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TryParseDecimal(TxtMontoTotal.Text, out decimal monto))
            {
                TxtMontoFormateado.Text = monto.ToString("C2");
                TxtMontoFormateado.Foreground = monto > 0 ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : Brushes.Gray;
            }
            else
            {
                TxtMontoFormateado.Text = "$0.00";
                TxtMontoFormateado.Foreground = Brushes.Gray;
            }

            ValidarFormulario();
            // Validar sumas parciales si el panel combinado está visible
            if (RbCombinado.IsChecked == true)
            {
                ValidarSumaParciales();
            }
        }

        private void FormaPago_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelPagoCombinado == null) return;

            PanelPagoCombinado.Visibility = RbCombinado.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (RbCombinado.IsChecked == false)
            {
                // Limpiar campos parciales
                _actualizandoMontos = true; // Evitar que los eventos TextChanged se disparen en cadena
                TxtEfectivoParcial.Text = "";
                TxtTarjetaParcial.Text = "";
                TxtTransferenciaParcial.Text = "";
                _actualizandoMontos = false;
            }

            ValidarFormulario();
        }

        private void MontosParciales_Changed(object sender, TextChangedEventArgs e)
        {
            if (_actualizandoMontos) return;

            ValidarSumaParciales();
            ValidarFormulario();
        }

        private void ValidarSumaParciales()
        {
            if (RbCombinado.IsChecked != true) return;

            var efectivo = TryParseDecimal(TxtEfectivoParcial.Text, out decimal ef) ? ef : 0;
            var tarjeta = TryParseDecimal(TxtTarjetaParcial.Text, out decimal tj) ? tj : 0;
            var transferencia = TryParseDecimal(TxtTransferenciaParcial.Text, out decimal tr) ? tr : 0;

            var sumaParcial = efectivo + tarjeta + transferencia;
            var montoTotal = TryParseDecimal(TxtMontoTotal.Text, out decimal total) ? total : 0;

            TxtSumaParcial.Text = $"Suma parcial: {sumaParcial:C2}";

            // Usar una tolerancia pequeña para comparar decimales
            if (Math.Abs(sumaParcial - montoTotal) < 0.01m)
            {
                TxtDiferenciaSuma.Text = "✅ La suma coincide con el total";
                TxtDiferenciaSuma.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                BorderValidacionSuma.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254)); // Azul claro
            }
            else
            {
                var diferencia = montoTotal - sumaParcial;
                TxtDiferenciaSuma.Text = $"❌ {(diferencia > 0 ? "Falta" : "Sobra")}: {Math.Abs(diferencia):C2}";
                TxtDiferenciaSuma.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Rojo
                BorderValidacionSuma.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Rojo claro
            }
        }

        // ===== VALIDACIÓN DEL FORMULARIO =====

        private void ValidarFormulario()
        {
            bool esValido = true;

            // Validar concepto
            if (string.IsNullOrWhiteSpace(TxtConcepto.Text))
            {
                esValido = false;
            }

            // Validar monto total
            if (!TryParseDecimal(TxtMontoTotal.Text, out decimal montoTotal) || montoTotal <= 0)
            {
                esValido = false;
            }

            // Validar pago combinado
            if (RbCombinado.IsChecked == true)
            {
                var efectivo = TryParseDecimal(TxtEfectivoParcial.Text, out decimal ef) ? ef : 0;
                var tarjeta = TryParseDecimal(TxtTarjetaParcial.Text, out decimal tj) ? tj : 0;
                var transferencia = TryParseDecimal(TxtTransferenciaParcial.Text, out decimal tr) ? tr : 0;
                var sumaParcial = efectivo + tarjeta + transferencia;

                // La suma debe ser mayor a cero y coincidir con el total
                if (sumaParcial <= 0 || Math.Abs(sumaParcial - montoTotal) >= 0.01m)
                {
                    esValido = false;
                }
            }

            BtnGuardar.IsEnabled = esValido;
        }

        // ===== ACCIONES DE BOTONES =====

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarDatos())
                    return;

                BtnGuardar.IsEnabled = false;
                BtnGuardar.Content = "⏳ Guardando...";

                // Obtener o crear material para gastos generales
                var materialGastos = await ObtenerMaterialGastosGeneralesAsync();

                // Obtener datos del formulario
                var concepto = TxtConcepto.Text.Trim();
                // Usar TryParseDecimal para seguridad, aunque ValidarDatos ya lo hizo
                TryParseDecimal(TxtMontoTotal.Text, out decimal montoTotal);
                var usuario = Environment.UserName; // O mejor, el usuario logueado si lo tienes

                // Crear movimiento(s) según forma de pago
                if (RbCombinado.IsChecked == true)
                {
                    // Guardar múltiples movimientos (uno por cada forma de pago)
                    await GuardarGastoCombinado(materialGastos, concepto, usuario);
                }
                else
                {
                    // Guardar un solo movimiento
                    await GuardarGastoSimple(materialGastos, concepto, montoTotal, usuario);
                }

                GastoGuardado = true;
                MessageBox.Show($"✅ Gasto registrado exitosamente\n\nConcepto: {concepto}\nMonto: {montoTotal:C2}",
                                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar gasto:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnGuardar.IsEnabled = true;
                BtnGuardar.Content = "✅ Guardar Gasto";
            }
        }

        private async Task<RawMaterial> ObtenerMaterialGastosGeneralesAsync()
        {
            // Buscar material especial para gastos
            var material = await _context.Set<RawMaterial>()
                .FirstOrDefaultAsync(m => m.NombreArticulo == "GASTOS GENERALES DE CAJA");

            if (material == null)
            {
                // Crear material especial
                material = new RawMaterial
                {
                    NombreArticulo = "GASTOS GENERALES DE CAJA",
                    Categoria = "GASTOS",
                    UnidadMedida = "Unidad",
                    UnidadBase = "Unidad",
                    FactorConversion = 1,
                    StockAntiguo = 0,
                    StockNuevo = 0,
                    PrecioVenta = 0,
                    PrecioVentaConIVA = 0,
                    PrecioPorUnidad = 0,
                    PrecioPorUnidadBase = 0,
                    PrecioConIVA = 0,
                    PrecioSinIVA = 0,
                    PrecioBaseConIVA = 0,
                    PrecioBaseSinIVA = 0,
                    ActivoParaVenta = false, // No es para venta
                    FechaVencimiento = DateTime.Now.AddYears(100),
                    Proveedor = "Sistema",
                    Observaciones = "Material especial para registro de gastos generales",
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                };

                _context.Set<RawMaterial>().Add(material);
                await _context.SaveChangesAsync();
            }

            return material;
        }

        private async Task GuardarGastoSimple(RawMaterial material, string concepto, decimal monto, string usuario)
        {
            var formaPago = RbEfectivo.IsChecked == true ? "Efectivo" :
                            RbTarjeta.IsChecked == true ? "Tarjeta" : "Transferencia";

            var movimiento = new Movimiento
            {
                RawMaterialId = material.Id,
                TipoMovimiento = "Gasto",
                Cantidad = 1, // Es un solo "gasto"
                Motivo = $"{concepto} (Forma de pago: {formaPago})",
                Usuario = usuario,
                FechaMovimiento = _fecha.Add(DateTime.Now.TimeOfDay), // Combina la fecha de caja con la hora actual
                PrecioConIVA = monto,
                PrecioSinIVA = monto / 1.16m, // Asumiendo IVA del 16%
                UnidadMedida = "Unidad",
                StockAnterior = 0,
                StockPosterior = 0
            };

            _context.Set<Movimiento>().Add(movimiento);
            await _context.SaveChangesAsync();
        }

        private async Task GuardarGastoCombinado(RawMaterial material, string concepto, string usuario)
        {
            var efectivo = TryParseDecimal(TxtEfectivoParcial.Text, out decimal ef) ? ef : 0;
            var tarjeta = TryParseDecimal(TxtTarjetaParcial.Text, out decimal tj) ? tj : 0;
            var transferencia = TryParseDecimal(TxtTransferenciaParcial.Text, out decimal tr) ? tr : 0;
            var fechaHora = _fecha.Add(DateTime.Now.TimeOfDay);

            if (efectivo > 0)
            {
                var movimiento = new Movimiento
                {
                    RawMaterialId = material.Id,
                    TipoMovimiento = "Gasto",
                    Cantidad = 1,
                    Motivo = $"{concepto} (Efectivo)",
                    Usuario = usuario,
                    FechaMovimiento = fechaHora,
                    PrecioConIVA = efectivo,
                    PrecioSinIVA = efectivo / 1.16m,
                    UnidadMedida = "Unidad",
                    StockAnterior = 0,
                    StockPosterior = 0
                };
                _context.Set<Movimiento>().Add(movimiento);
            }

            if (tarjeta > 0)
            {
                var movimiento = new Movimiento
                {
                    RawMaterialId = material.Id,
                    TipoMovimiento = "Gasto",
                    Cantidad = 1,
                    Motivo = $"{concepto} (Tarjeta)",
                    Usuario = usuario,
                    FechaMovimiento = fechaHora,
                    PrecioConIVA = tarjeta,
                    PrecioSinIVA = tarjeta / 1.16m,
                    UnidadMedida = "Unidad",
                    StockAnterior = 0,
                    StockPosterior = 0
                };
                _context.Set<Movimiento>().Add(movimiento);
            }

            if (transferencia > 0)
            {
                var movimiento = new Movimiento
                {
                    RawMaterialId = material.Id,
                    TipoMovimiento = "Gasto",
                    Cantidad = 1,
                    Motivo = $"{concepto} (Transferencia)",
                    Usuario = usuario,
                    FechaMovimiento = fechaHora,
                    PrecioConIVA = transferencia,
                    PrecioSinIVA = transferencia / 1.16m,
                    UnidadMedida = "Unidad",
                    StockAnterior = 0,
                    StockPosterior = 0
                };
                _context.Set<Movimiento>().Add(movimiento);
            }

            await _context.SaveChangesAsync();
        }

        private bool ValidarDatos()
        {
            if (string.IsNullOrWhiteSpace(TxtConcepto.Text))
            {
                MessageBox.Show("Ingrese el concepto del gasto.", "Validación",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtConcepto.Focus();
                return false;
            }

            if (!TryParseDecimal(TxtMontoTotal.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a 0.", "Validación",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMontoTotal.Focus();
                return false;
            }

            if (RbCombinado.IsChecked == true)
            {
                var efectivo = TryParseDecimal(TxtEfectivoParcial.Text, out decimal ef) ? ef : 0;
                var tarjeta = TryParseDecimal(TxtTarjetaParcial.Text, out decimal tj) ? tj : 0;
                var transferencia = TryParseDecimal(TxtTransferenciaParcial.Text, out decimal tr) ? tr : 0;
                var suma = efectivo + tarjeta + transferencia;

                if (Math.Abs(suma - monto) >= 0.01m)
                {
                    MessageBox.Show("La suma de los montos parciales debe coincidir con el monto total.",
                                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Método añadido para que coincida con el XAML
        private void BtnOtrosDetalles_Click(object sender, RoutedEventArgs e)
        {
            // Aquí puedes agregar la lógica para este botón, por ejemplo:
            MessageBox.Show("Aquí se podría abrir una ventana para agregar más detalles, " +
                            "como número de factura, proveedor, etc.",
                            "Otros Detalles", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
