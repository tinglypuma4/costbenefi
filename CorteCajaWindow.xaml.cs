using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class CorteCajaWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly CorteCajaService _corteCajaService;
        private CorteCaja _corteActual;
        private DateTime _fechaCorte;
        private bool _esModoEdicion = false;

        // Estado de validación
        private bool _conteoCompleto = false;
        private bool _diferenciasCalculadas = false;

        public CorteCajaWindow(AppDbContext context, DateTime? fecha = null)
        {
            InitializeComponent();
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _corteCajaService = new CorteCajaService(_context);
            _fechaCorte = fecha?.Date ?? DateTime.Today;

            Loaded += CorteCajaWindow_Loaded;
        }

        // Constructor para editar corte existente
        public CorteCajaWindow(AppDbContext context, CorteCaja corteExistente) : this(context, corteExistente.FechaCorte)
        {
            _esModoEdicion = true;
            _corteActual = corteExistente;
        }

        private async void CorteCajaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await InicializarCorte();
                InicializarInterfaz();
                ActualizarEstadoValidacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar corte de caja: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async Task InicializarCorte()
        {
            if (_esModoEdicion && _corteActual != null)
            {
                // Modo edición: cargar corte existente
                await CargarCorteExistente();
            }
            else
            {
                // Modo nuevo: verificar si ya existe corte para el día
                var corteExistente = await _corteCajaService.ObtenerCorteDelDiaAsync(_fechaCorte);
                if (corteExistente != null)
                {
                    var resultado = MessageBox.Show(
                        $"Ya existe un corte para el día {_fechaCorte:dd/MM/yyyy}.\n\n" +
                        $"¿Desea abrirlo para editarlo?",
                        "Corte Existente", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        _corteActual = corteExistente;
                        _esModoEdicion = true;
                        await CargarCorteExistente();
                    }
                    else
                    {
                        Close();
                        return;
                    }
                }
                else
                {
                    // Crear nuevo corte
                    await CrearNuevoCorte();
                }
            }
        }

        private async Task CrearNuevoCorte()
        {
            try
            {
                _corteActual = await _corteCajaService.IniciarCorteDelDiaAsync(_fechaCorte, Environment.UserName);

                // Mostrar información inicial
                MostrarTotalesCalculados();

                // Valores por defecto
                TxtFondoInicial.Text = _corteActual.FondoCajaInicial.ToString("F2");
                TxtFondoSiguiente.Text = _corteActual.FondoCajaSiguiente.ToString("F2");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear nuevo corte: {ex.Message}");
            }
        }

        private async Task CargarCorteExistente()
        {
            try
            {
                // Recalcular totales del sistema por si hay cambios
                var ventasDelDia = await _context.GetVentasDelDia(_corteActual.FechaCorte).ToListAsync();
                _corteActual.CalcularTotalesAutomaticos(ventasDelDia);

                // ✅ RECALCULAR GASTOS AL CARGAR CORTE EXISTENTE
                _corteActual.GastosTotalesCalculados = await _corteCajaService.CalcularGastosDelDiaAsync(_corteActual.FechaCorte);

                MostrarTotalesCalculados();
                CargarDatosExistentes();

                // Calcular diferencias si ya hay conteo
                if (_corteActual.EfectivoContado > 0)
                {
                    CalcularDiferencias();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar corte existente: {ex.Message}");
            }
        }

        private void InicializarInterfaz()
        {
            // Información del header
            TxtFechaCorte.Text = $"📅 Fecha: {_fechaCorte:dddd, dd/MM/yyyy}";

            // Usuario de la app y usuario de Windows (solo nombre, sin máquina)
            var usuarioApp = _corteActual.UsuarioCorte;
            var usuarioPC = Environment.UserName;

            TxtUsuarioCorte.Text = $"👤 Usuario: {usuarioApp} (PC: {usuarioPC})";

            TxtHoraCorte.Text = $"🕐 Hora: {_corteActual.FechaHoraCorte:HH:mm:ss}";

            // Estado del corte
            ActualizarEstadoCorte();

            // Mostrar u ocultar secciones según datos
            if (_corteActual.ComisionesTotalesCalculadas > 0)
            {
                CardComisiones.Visibility = Visibility.Visible;
            }

            // Configurar título de ventana
            if (_esModoEdicion)
            {
                Title = $"💰 Editar Corte de Caja - {_fechaCorte:dd/MM/yyyy}";
                BtnCompletarCorte.Content = _corteActual.Estado == "Completado" ? "💾 Actualizar" : "✅ Completar Corte";
            }
        }
        private void MostrarTotalesCalculados()
        {
            // Totales generales
            TxtCantidadTickets.Text = _corteActual.CantidadTickets.ToString();
            TxtTotalVentas.Text = _corteActual.TotalVentasCalculado.ToString("C2");
            TxtGananciaNeta.Text = _corteActual.GananciaNetaCalculada.ToString("C2");

            // Formas de pago
            TxtEfectivoCalculado.Text = _corteActual.EfectivoCalculado.ToString("C2");
            TxtTarjetaCalculado.Text = _corteActual.TarjetaCalculado.ToString("C2");
            TxtTransferenciaCalculado.Text = _corteActual.TransferenciaCalculado.ToString("C2");
            TxtComisionesTotal.Text = _corteActual.ComisionesTotalesCalculadas.ToString("C2");

            // Detalle de comisiones
            if (_corteActual.ComisionesTotalesCalculadas > 0)
            {
                TxtComisionBase.Text = _corteActual.ComisionesCalculadas.ToString("C2");
                TxtIVAComision.Text = _corteActual.IVAComisionesCalculado.ToString("C2");
                TxtTotalRealRecibido.Text = (_corteActual.TotalVentasCalculado - _corteActual.ComisionesTotalesCalculadas).ToString("C2");
                CardComisiones.Visibility = Visibility.Visible;
            }

            // ✅ GASTOS DEL DÍA
            if (_corteActual.GastosTotalesCalculados > 0)
            {
                TxtGastosTotales.Text = _corteActual.GastosTotalesCalculados.ToString("C2");
                TxtEfectivoSinGastos.Text = _corteActual.EfectivoRealDisponible.ToString("C2");
                CardGastos.Visibility = Visibility.Visible;

                // Mostrar ganancia neta final
                TxtGananciaNetaFinal.Text = _corteActual.GananciaNetaFinal.ToString("C2");
                TxtGananciaNetaFinal.Visibility = Visibility.Visible;
                TxtLabelGananciaFinal.Visibility = Visibility.Visible;
            }

            // Efectivo esperado
            ActualizarEfectivoEsperado();
        }

        private void CargarDatosExistentes()
        {
            // Cargar datos del conteo físico
            TxtFondoInicial.Text = _corteActual.FondoCajaInicial.ToString("F2");
            TxtEfectivoContado.Text = _corteActual.EfectivoContado.ToString("F2");
            TxtFondoSiguiente.Text = _corteActual.FondoCajaSiguiente.ToString("F2");

            // Observaciones
            TxtObservaciones.Text = _corteActual.Observaciones;

            // Información de depósito
            if (_corteActual.DepositoRealizado)
            {
                ChkRealizarDeposito.IsChecked = true;
                TxtMontoDeposito.Text = _corteActual.MontoDepositado.ToString("F2");
                TxtReferenciaDeposito.Text = _corteActual.ReferenciaDeposito;
                CardDeposito.Visibility = Visibility.Visible;
                PanelDeposito.Visibility = Visibility.Visible;
            }

            _conteoCompleto = _corteActual.EfectivoContado > 0;
        }

        private void ActualizarEstadoCorte()
        {
            switch (_corteActual.Estado)
            {
                case "Pendiente":
                    TxtEstadoCorte.Text = "⏳ PENDIENTE";
                    TxtEstadoCorte.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amarillo
                    break;
                case "Completado":
                    var estado = _corteActual.TieneSobrante ? "✅ COMPLETADO (SOBRANTE)" :
                                _corteActual.TieneFaltante ? "⚠️ COMPLETADO (FALTANTE)" :
                                "✅ COMPLETADO (EXACTO)";
                    TxtEstadoCorte.Text = estado;
                    TxtEstadoCorte.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Verde
                    break;
                case "Cancelado":
                    TxtEstadoCorte.Text = "❌ CANCELADO";
                    TxtEstadoCorte.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Rojo
                    break;
            }
            TxtEstadoCorte.Foreground = Brushes.White;
        }

        // ===== EVENT HANDLERS =====

        private void TxtFondoInicial_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarEfectivoEsperado();
            CalcularDiferencias();
        }

        private void TxtEfectivoContado_TextChanged(object sender, TextChangedEventArgs e)
        {
            _conteoCompleto = TryParseDecimal(TxtEfectivoContado.Text, out decimal contado) && contado >= 0;
            CalcularDiferencias();
            ActualizarEstadoValidacion();
        }

        private void CalcularDiferencias(object sender = null, TextChangedEventArgs e = null)
        {
            if (!_conteoCompleto) return;

            try
            {
                var fondoInicial = TryParseDecimal(TxtFondoInicial.Text, out decimal fondo) ? fondo : 1000;
                var efectivoContado = TryParseDecimal(TxtEfectivoContado.Text, out decimal contado) ? contado : 0;
                var fondoSiguiente = TryParseDecimal(TxtFondoSiguiente.Text, out decimal siguiente) ? siguiente : 1000;

                // Actualizar modelo temporal
                _corteActual.FondoCajaInicial = fondoInicial;
                _corteActual.EfectivoContado = efectivoContado;
                _corteActual.FondoCajaSiguiente = fondoSiguiente;

                // Mostrar cálculos
                TxtEfectivoEsperado.Text = _corteActual.EfectivoEsperado.ToString("C2");
                TxtDiferencia.Text = _corteActual.DiferenciaEfectivo.ToString("C2");
                TxtEfectivoParaDepositar.Text = _corteActual.EfectivoParaDepositar.ToString("C2");

                // Actualizar estado visual de la diferencia
                ActualizarEstadoDiferencia();

                // Mostrar opción de depósito si hay efectivo para depositar
                if (_corteActual.EfectivoParaDepositar > 100) // Mínimo para depósito
                {
                    CardDeposito.Visibility = Visibility.Visible;
                    TxtMontoDeposito.Text = _corteActual.EfectivoParaDepositar.ToString("F2");
                }

                _diferenciasCalculadas = true;
                ActualizarEstadoValidacion();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al calcular diferencias: {ex.Message}");
            }
        }

        private void ActualizarEfectivoEsperado()
        {
            var fondoInicial = TryParseDecimal(TxtFondoInicial.Text, out decimal fondo) ? fondo : 1000;
            var esperado = _corteActual.EfectivoCalculado + fondoInicial;
            TxtEfectivoEsperado.Text = esperado.ToString("C2");
        }

        private void ActualizarEstadoDiferencia()
        {
            var diferencia = _corteActual.DiferenciaEfectivo;

            if (Math.Abs(diferencia) <= 1) // Exacto
            {
                TxtEstadoDiferencia.Text = "✅ EXACTO - Sin diferencias";
                BorderEstadoDiferencia.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Verde
                TxtEstadoDiferencia.Foreground = Brushes.White;
                TxtDiferencia.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            else if (_corteActual.TieneSobrante)
            {
                TxtEstadoDiferencia.Text = _corteActual.DiferenciaAceptable ?
                    "📈 SOBRANTE (Aceptable)" : "📈 SOBRANTE (Revisar)";
                BorderEstadoDiferencia.Background = new SolidColorBrush(Color.FromRgb(23, 162, 184)); // Azul
                TxtEstadoDiferencia.Foreground = Brushes.White;
                TxtDiferencia.Foreground = new SolidColorBrush(Color.FromRgb(23, 162, 184));
            }
            else if (_corteActual.TieneFaltante)
            {
                TxtEstadoDiferencia.Text = _corteActual.DiferenciaAceptable ?
                    "📉 FALTANTE (Aceptable)" : "📉 FALTANTE (Revisar)";
                BorderEstadoDiferencia.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Rojo
                TxtEstadoDiferencia.Foreground = Brushes.White;
                TxtDiferencia.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
        }

        private void ActualizarEstadoValidacion()
        {
            var puedeCompletar = _conteoCompleto && _diferenciasCalculadas;

            BtnCompletarCorte.IsEnabled = puedeCompletar;

            if (puedeCompletar)
            {
                // ✅ CORREGIDO: Solo cambiar texto y color, sin .Kind
                IconEstado.Text = "✅";
                IconEstado.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                TxtEstadoValidacion.Text = "✅ Listo para completar";
                TxtEstadoValidacion.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            else if (!_conteoCompleto)
            {
                // ✅ CORREGIDO: Solo cambiar texto y color, sin .Kind  
                IconEstado.Text = "🕐";
                IconEstado.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                TxtEstadoValidacion.Text = "⏳ Complete el conteo físico";
                TxtEstadoValidacion.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            }
            else
            {
                // ✅ CORREGIDO: Solo cambiar texto y color, sin .Kind
                IconEstado.Text = "🧮";
                IconEstado.Foreground = new SolidColorBrush(Color.FromRgb(23, 162, 184));
                TxtEstadoValidacion.Text = "🧮 Calculando diferencias...";
                TxtEstadoValidacion.Foreground = new SolidColorBrush(Color.FromRgb(23, 162, 184));
            }
        }

        private void ChkRealizarDeposito_Checked(object sender, RoutedEventArgs e)
        {
            PanelDeposito.Visibility = Visibility.Visible;
            if (string.IsNullOrEmpty(TxtMontoDeposito.Text))
            {
                TxtMontoDeposito.Text = _corteActual.EfectivoParaDepositar.ToString("F2");
            }
        }

        private void ChkRealizarDeposito_Unchecked(object sender, RoutedEventArgs e)
        {
            PanelDeposito.Visibility = Visibility.Collapsed;
        }

        // ===== BOTONES =====

        private async void BtnVerDetalle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reporte = await _corteCajaService.GenerarReporteVentasParaCorteAsync(_fechaCorte);
                var ventanaDetalle = new Window
                {
                    Title = $"📊 Detalle de Ventas - {_fechaCorte:dd/MM/yyyy}",
                    Width = 600,
                    Height = 500,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = reporte,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 12,
                            Margin = new Thickness(15),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                };
                ventanaDetalle.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar reporte: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnVerDetalleGastos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gastosDelDia = await _corteCajaService.ObtenerDetalleGastosDelDiaAsync(_fechaCorte);

                if (!gastosDelDia.Any())
                {
                    MessageBox.Show("No hay gastos registrados para esta fecha.",
                                  "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Generar reporte de gastos
                var reporte = $"💸 DETALLE DE GASTOS - {_fechaCorte:dd/MM/yyyy}\n\n";
                reporte += $"Total de gastos: {gastosDelDia.Count}\n\n";
                reporte += "═══════════════════════════════════════════════\n\n";

                foreach (var gasto in gastosDelDia.OrderBy(g => g.FechaMovimiento))
                {
                    reporte += $"{gasto.TipoMovimientoIcon} {gasto.TipoMovimiento}\n";
                    reporte += $"   Hora: {gasto.FechaMovimiento:HH:mm:ss}\n";
                    reporte += $"   Motivo: {gasto.Motivo}\n";
                    if (gasto.RawMaterial != null)
                        reporte += $"   Material: {gasto.RawMaterial.NombreArticulo}\n";
                    reporte += $"   Cantidad: {gasto.Cantidad:F2} {gasto.UnidadMedida}\n";
                    reporte += $"   Valor: {gasto.ValorTotalConIVA:C2}\n";
                    reporte += $"   Usuario: {gasto.Usuario}\n";
                    reporte += "───────────────────────────────────────────\n";
                }

                reporte += $"\n💰 TOTAL GASTOS: {gastosDelDia.Sum(g => g.ValorTotalConIVA):C2}";

                var ventanaDetalle = new Window
                {
                    Title = $"💸 Detalle de Gastos - {_fechaCorte:dd/MM/yyyy}",
                    Width = 600,
                    Height = 500,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = reporte,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 12,
                            Margin = new Thickness(15),
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                };
                ventanaDetalle.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar reporte de gastos: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("¿Cerrar sin guardar cambios?", "Confirmar",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resultado == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private async void BtnCompletarCorte_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnCompletarCorte.IsEnabled = false;
                BtnCompletarCorte.Content = "⏳ Procesando...";

                // Validar datos
                if (!ValidarDatos())
                {
                    BtnCompletarCorte.IsEnabled = true;
                    BtnCompletarCorte.Content = "✅ Completar Corte";
                    return;
                }

                // Obtener datos del formulario
                var efectivoContado = decimal.Parse(TxtEfectivoContado.Text);
                var fondoSiguiente = decimal.Parse(TxtFondoSiguiente.Text);
                var observaciones = TxtObservaciones.Text.Trim();
                var realizarDeposito = ChkRealizarDeposito.IsChecked == true;
                var montoDeposito = realizarDeposito && TryParseDecimal(TxtMontoDeposito.Text, out decimal monto) ? monto : 0;
                var referenciaDeposito = realizarDeposito ? TxtReferenciaDeposito.Text.Trim() : "";

                // Completar corte
                var corteCompletado = await _corteCajaService.CompletarCorteAsync(
                    _corteActual, efectivoContado, fondoSiguiente, observaciones,
                    realizarDeposito, montoDeposito, referenciaDeposito);

                // Mostrar confirmación
                var mensaje = "✅ CORTE DE CAJA COMPLETADO EXITOSAMENTE!\n\n";
                mensaje += corteCompletado.ObtenerResumenCompleto();

                MessageBox.Show(mensaje, "Corte Completado", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al completar corte: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnCompletarCorte.IsEnabled = true;
                BtnCompletarCorte.Content = "✅ Completar Corte";
            }
        }

        // ===== VALIDACIONES =====

        private bool ValidarDatos()
        {
            if (!TryParseDecimal(TxtEfectivoContado.Text, out decimal efectivoContado) || efectivoContado < 0)
            {
                MessageBox.Show("Ingrese un monto válido para el efectivo contado.",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEfectivoContado.Focus();
                return false;
            }

            if (!TryParseDecimal(TxtFondoSiguiente.Text, out decimal fondoSiguiente) || fondoSiguiente < 0)
            {
                MessageBox.Show("Ingrese un monto válido para el fondo de caja del día siguiente.",
                              "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFondoSiguiente.Focus();
                return false;
            }

            if (ChkRealizarDeposito.IsChecked == true)
            {
                if (!TryParseDecimal(TxtMontoDeposito.Text, out decimal montoDeposito) || montoDeposito <= 0)
                {
                    MessageBox.Show("Ingrese un monto válido para el depósito.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtMontoDeposito.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(TxtReferenciaDeposito.Text))
                {
                    MessageBox.Show("Ingrese la referencia del depósito bancario.",
                                  "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtReferenciaDeposito.Focus();
                    return false;
                }
            }

            return true;
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permitir solo números, punto decimal y comas
            Regex regex = new Regex(@"^[0-9.,]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private bool TryParseDecimal(string text, out decimal result)
        {
            return decimal.TryParse(text?.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }
}