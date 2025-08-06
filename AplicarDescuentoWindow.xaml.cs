using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana para aplicar descuentos a la venta con autorización previa
    /// ✅ CON VERIFICACIONES NULL COMPLETAS
    /// </summary>
    public partial class AplicarDescuentoWindow : Window
    {
        public decimal TotalOriginal { get; private set; }
        public decimal DescuentoCalculado { get; private set; } = 0;
        public decimal NuevoTotal { get; private set; }
        public bool DescuentoAplicado { get; private set; } = false;
        public string UsuarioAutorizador { get; private set; }
        public string TipoDescuentoSeleccionado { get; private set; } = "Porcentaje";
        public decimal ValorDescuentoIngresado { get; private set; } = 0;
        public string MotivoDescuento { get; private set; } = "";

        // ✅ BANDERA PARA EVITAR LLAMADAS ANTES DE INICIALIZACIÓN COMPLETA
        private bool _inicializacionCompleta = false;

        public AplicarDescuentoWindow(decimal totalOriginal, string usuarioAutorizador)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 Iniciando AplicarDescuentoWindow...");

                // ✅ PRIMERO: InitializeComponent
                InitializeComponent();

                System.Diagnostics.Debug.WriteLine($"🔧 InitializeComponent completado");

                // ✅ SEGUNDO: Establecer propiedades
                TotalOriginal = totalOriginal;
                UsuarioAutorizador = usuarioAutorizador;

                System.Diagnostics.Debug.WriteLine($"🔧 Propiedades establecidas - Total: {totalOriginal:C2}, Usuario: {usuarioAutorizador}");

                // ✅ TERCERO: Configurar ventana de forma segura
                ConfigurarVentana();

                // ✅ CUARTO: Marcar como completamente inicializada
                _inicializacionCompleta = true;

                // ✅ QUINTO: Ahora sí actualizar cálculos
                ActualizarCalculos();

                System.Diagnostics.Debug.WriteLine($"✅ AplicarDescuentoWindow inicializada completamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en constructor AplicarDescuentoWindow: {ex.Message}");
                MessageBox.Show($"Error al inicializar ventana de descuento:\n\n{ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void ConfigurarVentana()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔧 Configurando ventana...");

                // ✅ VERIFICAR QUE TODOS LOS CONTROLES EXISTAN
                if (TxtTotalActual == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ TxtTotalActual es null");
                    return;
                }

                // ✅ ESTABLECER VALOR INICIAL DEL TOTAL
                TxtTotalActual.Text = TotalOriginal.ToString("C2");
                System.Diagnostics.Debug.WriteLine($"✅ TxtTotalActual establecido: {TxtTotalActual.Text}");

                // ✅ CONFIGURAR EVENTOS DE FORMA SEGURA
                if (TxtValorDescuento != null)
                {
                    TxtValorDescuento.Focus();

                    // Enter para continuar
                    TxtValorDescuento.KeyDown += (s, e) => {
                        if (e.Key == Key.Enter && BtnAplicar?.IsEnabled == true)
                            BtnAplicar_Click(s, e);
                    };
                }

                if (TxtMotivo != null)
                {
                    TxtMotivo.KeyDown += (s, e) => {
                        if (e.Key == Key.Enter && BtnAplicar?.IsEnabled == true)
                            BtnAplicar_Click(s, e);
                    };
                }

                System.Diagnostics.Debug.WriteLine($"✅ Ventana configurada correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ConfigurarVentana: {ex.Message}");
            }
        }

        private void TipoDescuento_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ VERIFICAR INICIALIZACIÓN COMPLETA
                if (!_inicializacionCompleta)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ TipoDescuento_Changed llamado antes de inicialización completa - ignorando");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 TipoDescuento_Changed ejecutándose...");

                // ✅ VERIFICACIONES NULL ANTES DE USAR CONTROLES
                if (RbPorcentaje?.IsChecked == true)
                {
                    TipoDescuentoSeleccionado = "Porcentaje";

                    if (LblValorDescuento != null)
                        LblValorDescuento.Text = "📊 Porcentaje de descuento:";

                    if (TxtUnidadDescuento != null)
                        TxtUnidadDescuento.Text = "%";

                    System.Diagnostics.Debug.WriteLine($"✅ Cambiado a Porcentaje");
                }
                else if (RbMontoFijo?.IsChecked == true)
                {
                    TipoDescuentoSeleccionado = "MontoFijo";

                    if (LblValorDescuento != null)
                        LblValorDescuento.Text = "💵 Monto de descuento:";

                    if (TxtUnidadDescuento != null)
                        TxtUnidadDescuento.Text = "$";

                    System.Diagnostics.Debug.WriteLine($"✅ Cambiado a Monto Fijo");
                }

                // ✅ ACTUALIZAR CÁLCULOS SOLO SI ESTÁ COMPLETAMENTE INICIALIZADA
                if (_inicializacionCompleta)
                {
                    ActualizarCalculos();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en TipoDescuento_Changed: {ex.Message}");
            }
        }

        private void TxtValorDescuento_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_inicializacionCompleta)
                {
                    ActualizarCalculos();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en TxtValorDescuento_TextChanged: {ex.Message}");
            }
        }

        private void TxtMotivo_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_inicializacionCompleta)
                {
                    ValidarCampos();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en TxtMotivo_TextChanged: {ex.Message}");
            }
        }

        private void ActualizarCalculos()
        {
            try
            {
                if (!_inicializacionCompleta)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ ActualizarCalculos llamado antes de inicialización completa - ignorando");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔧 ActualizarCalculos iniciado...");

                // ✅ VERIFICAR QUE EL CONTROL DE VALOR EXISTA
                if (TxtValorDescuento == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ TxtValorDescuento es null en ActualizarCalculos");
                    return;
                }

                // Obtener valor ingresado
                string textoValor = TxtValorDescuento.Text?.Trim() ?? "";
                System.Diagnostics.Debug.WriteLine($"🔧 Texto valor: '{textoValor}'");

                if (!decimal.TryParse(textoValor, out decimal valor))
                {
                    // Valor inválido
                    DescuentoCalculado = 0;
                    ValorDescuentoIngresado = 0;
                    System.Diagnostics.Debug.WriteLine($"⚠️ Valor inválido o vacío");
                }
                else
                {
                    ValorDescuentoIngresado = valor;
                    System.Diagnostics.Debug.WriteLine($"✅ Valor parseado: {valor}");

                    // Calcular descuento según el tipo
                    if (TipoDescuentoSeleccionado == "Porcentaje")
                    {
                        // Validar porcentaje
                        if (valor < 0 || valor > 100)
                        {
                            DescuentoCalculado = 0;
                            System.Diagnostics.Debug.WriteLine($"⚠️ Porcentaje fuera de rango: {valor}");
                        }
                        else
                        {
                            DescuentoCalculado = TotalOriginal * (valor / 100);
                            System.Diagnostics.Debug.WriteLine($"✅ Descuento por porcentaje: {DescuentoCalculado:C2}");
                        }
                    }
                    else // MontoFijo
                    {
                        // Validar monto fijo
                        if (valor < 0 || valor > TotalOriginal)
                        {
                            DescuentoCalculado = 0;
                            System.Diagnostics.Debug.WriteLine($"⚠️ Monto fijo fuera de rango: {valor}");
                        }
                        else
                        {
                            DescuentoCalculado = valor;
                            System.Diagnostics.Debug.WriteLine($"✅ Descuento monto fijo: {DescuentoCalculado:C2}");
                        }
                    }
                }

                // Calcular total final
                NuevoTotal = Math.Max(0, TotalOriginal - DescuentoCalculado);
                System.Diagnostics.Debug.WriteLine($"✅ Nuevo total: {NuevoTotal:C2}");

                // ✅ ACTUALIZAR INTERFAZ CON VERIFICACIONES NULL
                if (TxtDescuentoCalculado != null)
                {
                    TxtDescuentoCalculado.Text = DescuentoCalculado.ToString("C2");
                    System.Diagnostics.Debug.WriteLine($"✅ TxtDescuentoCalculado actualizado: {TxtDescuentoCalculado.Text}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ TxtDescuentoCalculado es null");
                }

                if (TxtNuevoTotal != null)
                {
                    TxtNuevoTotal.Text = NuevoTotal.ToString("C2");
                    System.Diagnostics.Debug.WriteLine($"✅ TxtNuevoTotal actualizado: {TxtNuevoTotal.Text}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ TxtNuevoTotal es null");
                }

                // Validar campos
                ValidarCampos();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en ActualizarCalculos: {ex.Message}");

                // ✅ VALORES SEGUROS EN CASO DE ERROR
                DescuentoCalculado = 0;
                NuevoTotal = TotalOriginal;

                if (TxtDescuentoCalculado != null)
                    TxtDescuentoCalculado.Text = "$0.00";

                if (TxtNuevoTotal != null)
                    TxtNuevoTotal.Text = TotalOriginal.ToString("C2");

                ValidarCampos();
            }
        }

        private void ValidarCampos()
        {
            try
            {
                if (!_inicializacionCompleta) return;

                bool valorValido = DescuentoCalculado > 0;
                bool motivoValido = !string.IsNullOrWhiteSpace(TxtMotivo?.Text);

                bool todosValidos = valorValido && motivoValido;

                // ✅ VERIFICAR QUE LOS CONTROLES EXISTAN
                if (BtnAplicar != null)
                {
                    BtnAplicar.IsEnabled = todosValidos;
                }

                if (TxtEstadoValidacion != null)
                {
                    // Actualizar mensaje de validación
                    if (todosValidos)
                    {
                        TxtEstadoValidacion.Text = "✅ Listo para aplicar descuento";
                        TxtEstadoValidacion.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                    }
                    else
                    {
                        if (!valorValido)
                        {
                            TxtEstadoValidacion.Text = "⚠️ Ingrese un descuento válido";
                        }
                        else if (!motivoValido)
                        {
                            TxtEstadoValidacion.Text = "⚠️ El motivo del descuento es obligatorio";
                        }
                        else
                        {
                            TxtEstadoValidacion.Text = "⚠️ Complete todos los campos para continuar";
                        }
                        TxtEstadoValidacion.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ValidarCampos: {ex.Message}");
                if (BtnAplicar != null)
                    BtnAplicar.IsEnabled = false;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"❌ Descuento cancelado por usuario");
                DescuentoAplicado = false;
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en BtnCancelar_Click: {ex.Message}");
                Close(); // Cerrar de todas formas
            }
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🎁 Aplicando descuento...");

                if (DescuentoCalculado <= 0)
                {
                    MessageBox.Show("El descuento debe ser mayor a $0.00", "Descuento Inválido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DescuentoCalculado >= TotalOriginal)
                {
                    MessageBox.Show("El descuento no puede ser igual o mayor al total original", "Descuento Excesivo",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (TxtMotivo == null)
                {
                    MessageBox.Show("Error interno: campo de motivo no disponible", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string motivoTexto = TxtMotivo.Text.Trim();
                if (string.IsNullOrWhiteSpace(motivoTexto))
                {
                    MessageBox.Show("El motivo del descuento es obligatorio", "Motivo Requerido",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtMotivo.Focus();
                    return;
                }

                // Confirmación final
                string tipoTexto = TipoDescuentoSeleccionado == "Porcentaje"
                    ? $"{ValorDescuentoIngresado}%"
                    : ValorDescuentoIngresado.ToString("C2");

                var mensaje = $"🎁 CONFIRMAR DESCUENTO\n\n" +
                             $"Total original: {TotalOriginal:C2}\n" +
                             $"Descuento: {tipoTexto} = {DescuentoCalculado:C2}\n" +
                             $"Nuevo total: {NuevoTotal:C2}\n\n" +
                             $"Motivo: {motivoTexto}\n" +
                             $"Autorizado por: {UsuarioAutorizador}\n\n" +
                             $"¿Aplicar este descuento?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Descuento",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    MotivoDescuento = motivoTexto;
                    DescuentoAplicado = true;
                    DialogResult = true;

                    System.Diagnostics.Debug.WriteLine($"✅ Descuento aplicado: {DescuentoCalculado:C2} - Motivo: {motivoTexto}");

                    Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en BtnAplicar_Click: {ex.Message}");
                MessageBox.Show($"Error al aplicar descuento: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Obtiene un resumen del descuento aplicado
        /// </summary>
        public string ObtenerResumenDescuento()
        {
            if (!DescuentoAplicado) return "Sin descuento";

            string tipo = TipoDescuentoSeleccionado == "Porcentaje"
                ? $"{ValorDescuentoIngresado}%"
                : $"${ValorDescuentoIngresado:F2}";

            return $"{tipo} = {DescuentoCalculado:C2} - Motivo: {MotivoDescuento} (Autorizado por: {UsuarioAutorizador})";
        }
    }
}