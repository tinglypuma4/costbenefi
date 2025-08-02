using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class BarcodeScannerWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly List<string> _codigosRecientes = new();
        private int _contadorCodigos = 0;

        // NUEVAS VARIABLES PARA ESTADÍSTICAS MEJORADAS
        private int _codigosNuevos = 0;
        private int _codigosExistentes = 0;
        private DateTime _inicioSesion;
        private DispatcherTimer _timerSesion;

        // Variables para detección automática de escáner
        private string _barcodeBuffer = "";
        private DateTime _lastKeyPress = DateTime.MinValue;
        private const int SCANNER_TIMEOUT_MS = 100;

        public BarcodeScannerWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;

            ConfigurarEventos();
            InicializarVentana();
            InicializarTimer(); // NUEVO
        }

        #region CONFIGURACIÓN INICIAL

        private void ConfigurarEventos()
        {
            // Capturar entrada de escáner automáticamente
            this.PreviewKeyDown += OnPreviewKeyDown;
            this.KeyDown += OnKeyDown;

            // Enfocar el textbox al cargar
            this.Loaded += (s, e) => TxtCodigoBarras.Focus();
        }

        private void InicializarVentana()
        {
            _inicioSesion = DateTime.Now; // NUEVO
            ActualizarEstadisticas(); // NUEVO
            TxtStatus.Text = "✅ Escáner listo - Enfoque en el campo de código o use escáner USB";
            ActualizarIndicador("🔍 Esperando código...", Colors.Gray);
        }

        // NUEVO MÉTODO PARA TIMER
        private void InicializarTimer()
        {
            _timerSesion = new DispatcherTimer();
            _timerSesion.Interval = TimeSpan.FromSeconds(1);
            _timerSesion.Tick += (s, e) => ActualizarTiempoSesion();
            _timerSesion.Start();
        }

        // NUEVO MÉTODO PARA ACTUALIZAR TIEMPO
        private void ActualizarTiempoSesion()
        {
            var tiempoTranscurrido = DateTime.Now - _inicioSesion;
            if (TxtTiempoSesion != null)
                TxtTiempoSesion.Text = $"Sesión: {tiempoTranscurrido:hh\\:mm\\:ss}";
        }

        #endregion

        #region DETECCIÓN AUTOMÁTICA DE ESCÁNER

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Si el foco está en el TextBox, dejar que maneje la entrada normalmente
            if (Keyboard.FocusedElement == TxtCodigoBarras) return;

            ProcesarEntradaEscaner(e);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Solo procesar si la ventana tiene el foco pero no el TextBox
            if (Keyboard.FocusedElement == this)
            {
                ProcesarEntradaEscaner(e);
            }
        }

        private void ProcesarEntradaEscaner(KeyEventArgs e)
        {
            var now = DateTime.Now;

            // Si ha pasado mucho tiempo, reiniciar el buffer
            if ((now - _lastKeyPress).TotalMilliseconds > SCANNER_TIMEOUT_MS)
            {
                _barcodeBuffer = "";
            }

            _lastKeyPress = now;

            if (e.Key == Key.Enter)
            {
                // Procesar el código si tiene longitud suficiente
                if (_barcodeBuffer.Length > 3)
                {
                    TxtCodigoBarras.Text = _barcodeBuffer;
                    _ = ProcesarCodigoAsync(_barcodeBuffer);
                    e.Handled = true;
                }
                _barcodeBuffer = "";
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                // Agregar dígitos al buffer
                _barcodeBuffer += (e.Key - Key.D0).ToString();
            }
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                // Agregar letras al buffer
                _barcodeBuffer += e.Key.ToString();
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                // Agregar dígitos del teclado numérico
                _barcodeBuffer += (e.Key - Key.NumPad0).ToString();
            }
        }

        #endregion

        #region EVENTOS DE INTERFAZ

        private async void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string codigo = TxtCodigoBarras.Text.Trim();
                if (!string.IsNullOrEmpty(codigo))
                {
                    await ProcesarCodigoAsync(codigo);
                }
            }
        }

        private async void BtnProcesar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = TxtCodigoBarras.Text.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                ActualizarIndicador("⚠️ Ingrese un código", Colors.Orange);
                TxtStatus.Text = "⚠️ Debe ingresar un código antes de procesar";
                TxtCodigoBarras.Focus();
                return;
            }

            await ProcesarCodigoAsync(codigo);
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            TxtCodigoBarras.Text = "";
            _barcodeBuffer = "";
            ActualizarIndicador("🔍 Esperando código...", Colors.Gray);
            TxtStatus.Text = "✅ Campo limpiado - Listo para nuevo código";
            TxtCodigoBarras.Focus();
        }

        // NUEVO MÉTODO REQUERIDO POR EL XAML
        private void BtnLimpiarHistorial_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro de que desea limpiar el historial de esta sesión?",
                "Confirmar Limpieza",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _codigosRecientes.Clear();
                TxtCodigosRecientes.Text = "Historial limpiado...";
                TxtStatus.Text = "🧹 Historial de códigos limpiado";
            }
        }

        // NUEVO MÉTODO REQUERIDO POR EL XAML
        private void BtnAyuda_Click(object sender, RoutedEventArgs e)
        {
            string ayuda = "🔧 AYUDA DEL ESCÁNER DE CÓDIGOS\n\n" +
                          "⌨️ ATAJOS DE TECLADO:\n" +
                          "• F1 - Limpiar campo\n" +
                          "• F5 - Enfocar campo de código\n" +
                          "• ESC - Cerrar ventana\n" +
                          "• ENTER - Procesar código\n\n" +
                          "📱 USO DEL ESCÁNER:\n" +
                          "• Conecte un escáner USB\n" +
                          "• El escáner funciona automáticamente\n" +
                          "• También puede escribir códigos manualmente\n\n" +
                          "📊 ESTADÍSTICAS:\n" +
                          "• Se muestran en tiempo real\n" +
                          "• Diferencia entre códigos nuevos y existentes\n" +
                          "• Historial de la sesión actual\n\n" +
                          "💡 CONSEJOS:\n" +
                          "• Use códigos de 4+ caracteres\n" +
                          "• Revise el historial para verificar procesamiento\n" +
                          "• El sistema detecta automáticamente productos existentes";

            MessageBox.Show(ayuda, "Ayuda del Escáner", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        #endregion

        #region LÓGICA DE PROCESAMIENTO

        private async Task ProcesarCodigoAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;

            try
            {
                // Actualizar UI
                ActualizarIndicador("🔄 Procesando...", Colors.Blue);
                TxtStatus.Text = "⏳ Buscando código en base de datos...";

                BtnProcesar.IsEnabled = false;

                // Buscar en base de datos
                var material = await _context.RawMaterials
                    .FirstOrDefaultAsync(m => m.CodigoBarras == codigo);

                bool esNuevo = material == null; // NUEVO
                if (material != null)
                {
                    // Código existente
                    _codigosExistentes++;
                    ActualizarIndicador("✅ Código encontrado!", Colors.Green);
                    TxtStatus.Text = $"✅ Encontrado: {material.NombreArticulo}";

                    // ✅ MENSAJE MEJORADO - MÁS CLARO SOBRE QUE ABRIRÁ EDICIÓN
                    var result = MessageBox.Show(
                        $"📦 PRODUCTO ENCONTRADO\n\n" +
                        $"📝 Nombre: {material.NombreArticulo}\n" +
                        $"🏷️ Categoría: {material.Categoria}\n" +
                        $"📊 Stock actual: {material.StockTotal:F2} {material.UnidadMedida}\n" +
                        $"💰 Precio: {material.PrecioConIVA:C2}\n" +
                        $"🏪 Proveedor: {material.Proveedor}\n\n" +
                        $"✅ ¿Abrir formulario de gestión de stock?\n" +
                        $"(Podrá agregar stock, quitar stock o editar información)",
                        "Código Existente - Abrir Gestión",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await AbrirFormularioParaProducto(codigo, material);
                    }
                }
                else
                {
                    // Código nuevo
                    _codigosNuevos++; // NUEVO
                    ActualizarIndicador("🆕 Código nuevo", Colors.Purple);
                    TxtStatus.Text = "🆕 Código nuevo - Abriendo formulario de creación...";

                    MessageBox.Show(
                        $"🆕 CÓDIGO NUEVO\n\n" +
                        $"Código: {codigo}\n\n" +
                        "Este código no existe en el sistema.\n" +
                        "Se abrirá el formulario para crear el producto.",
                        "Código Nuevo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await AbrirFormularioParaProducto(codigo, null);
                }

                // Actualizar estadísticas y historial
                _contadorCodigos++;
                AgregarCodigoReciente(codigo, material?.NombreArticulo, esNuevo); // MEJORADO
                ActualizarEstadisticas(); // NUEVO

                // Limpiar campo para siguiente código
                TxtCodigoBarras.Text = "";
                _barcodeBuffer = "";

                await Task.Delay(1500); // Pausa para mostrar el resultado
                ActualizarIndicador("🔍 Listo para siguiente código", Colors.Gray);
                TxtStatus.Text = "✅ Código procesado - Listo para el siguiente";
            }
            catch (Exception ex)
            {
                ActualizarIndicador("❌ Error al procesar", Colors.Red);
                TxtStatus.Text = $"❌ Error: {ex.Message}";

                MessageBox.Show($"Error al procesar código: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnProcesar.IsEnabled = true;
                TxtCodigoBarras.Focus();
            }
        }

        private async Task AbrirFormularioParaProducto(string codigo, RawMaterial materialExistente)
        {
            try
            {
                if (materialExistente != null)
                {
                    // ✅ PRODUCTO EXISTENTE - ABRIR FORMULARIO DE EDICIÓN/STOCK
                    System.Diagnostics.Debug.WriteLine($"📝 Abriendo formulario de edición para: {materialExistente.NombreArticulo}");

                    var editWindow = new EditAddStockWindow(_context, materialExistente)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (editWindow.ShowDialog() == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Stock actualizado para: {materialExistente.NombreArticulo}");
                        TxtStatus.Text = $"✅ Stock actualizado: {materialExistente.NombreArticulo}";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Edición cancelada para: {materialExistente.NombreArticulo}");
                        TxtStatus.Text = $"❌ Edición cancelada: {materialExistente.NombreArticulo}";
                    }
                }
                else
                {
                    // ✅ CÓDIGO NUEVO - MOSTRAR SELECTOR DE TIPO (COMPORTAMIENTO ORIGINAL)
                    System.Diagnostics.Debug.WriteLine($"🆕 Abriendo selector para código nuevo: {codigo}");

                    var selectorWindow = new TipoMaterialSelectorWindow(_context, codigo)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (selectorWindow.ShowDialog() == true)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Producto nuevo creado con código: {codigo}");
                        TxtStatus.Text = $"✅ Producto nuevo creado: {codigo}";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Creación cancelada para código: {codigo}");
                        TxtStatus.Text = $"❌ Creación cancelada: {codigo}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en AbrirFormularioParaProducto: {ex.Message}");
                MessageBox.Show($"Error al abrir formulario: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al abrir formulario";
            }
        }

        #endregion

        #region ACTUALIZACIÓN DE UI

        private void ActualizarIndicador(string mensaje, Color color)
        {
            TxtIndicador.Text = mensaje;
            TxtIndicador.Foreground = new SolidColorBrush(color);

            // Cambiar color del borde también
            BorderIndicador.BorderBrush = new SolidColorBrush(color);
        }

        // MÉTODO MEJORADO PARA ESTADÍSTICAS
        private void ActualizarEstadisticas()
        {
            if (TxtTotalEscaneados != null)
                TxtTotalEscaneados.Text = _contadorCodigos.ToString();
            if (TxtCodigosNuevos != null)
                TxtCodigosNuevos.Text = _codigosNuevos.ToString();
            if (TxtCodigosExistentes != null)
                TxtCodigosExistentes.Text = _codigosExistentes.ToString();
            if (TxtContadorCodigos != null)
                TxtContadorCodigos.Text = $"Códigos procesados en esta sesión: {_contadorCodigos}";
        }

        // MÉTODO MEJORADO CON TIPO DE CÓDIGO
        private void AgregarCodigoReciente(string codigo, string nombreProducto, bool esNuevo)
        {
            string estado = esNuevo ? "🆕 NUEVO" : "🔄 EXISTENTE";
            string entrada = $"[{DateTime.Now:HH:mm:ss}] {codigo} - {estado}";

            if (!string.IsNullOrEmpty(nombreProducto))
            {
                entrada += $"\n    📦 {nombreProducto}";
            }

            _codigosRecientes.Insert(0, entrada);

            // Mantener solo los últimos 15 códigos
            if (_codigosRecientes.Count > 15)
            {
                _codigosRecientes.RemoveRange(15, _codigosRecientes.Count - 15);
            }

            // Actualizar la vista
            TxtCodigosRecientes.Text = _codigosRecientes.Count > 0
                ? string.Join("\n\n", _codigosRecientes)
                : "Ningún código procesado aún en esta sesión...";

            // Scroll automático
            if (ScrollCodigos != null)
                ScrollCodigos.ScrollToTop();
        }

        #endregion

        #region EVENTOS DE VENTANA

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            TxtCodigoBarras.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Atajos de teclado
            switch (e.Key)
            {
                case Key.F1:
                    BtnLimpiar_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F5:
                    TxtCodigoBarras.Focus();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    BtnCerrar_Click(null, null);
                    e.Handled = true;
                    break;
            }

            base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _timerSesion?.Stop(); // NUEVO
            base.OnClosed(e);
        }

        #endregion
    }
}