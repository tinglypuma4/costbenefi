using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using costbenefi.Services;
using costbenefi.Models;
using costbenefi.Data;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana para autorización de operaciones sensibles (descuentos, cancelaciones)
    /// Solo Dueño y Encargado pueden autorizar
    /// </summary>
    public partial class AutorizacionDescuentoWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly string _operacion;

        public bool AutorizacionExitosa { get; private set; } = false;
        public User UsuarioAutorizador { get; private set; } = null;
        public string MotivoOperacion { get; private set; } = "";

        // ✅ NUEVO: Campo para motivo cuando sea descuento
        private TextBox TxtMotivo;

        public AutorizacionDescuentoWindow(string operacion = "aplicar descuento")
        {
            _context = new AppDbContext();
            _operacion = operacion;

            // ✅ USAR EL XAML AUTOMÁTICAMENTE
            InitializeComponent();
            ConfigurarVentana();
        }

        private void ConfigurarVentana()
        {
            // ✅ ACTUALIZAR TEXTO DE LA OPERACIÓN
            TxtOperacionInfo.Text = $"Desea {_operacion}.";

            // ✅ AGREGAR CAMPO DE MOTIVO SI ES DESCUENTO
            if (_operacion.Contains("descuento"))
            {
                AgregarCampoMotivo();
            }

            // Configurar eventos
            TxtUsuario.TextChanged += ValidarCampos;
            TxtPassword.PasswordChanged += ValidarCampos;

            // Enfoque inicial
            TxtUsuario.Focus();

            // Enter para continuar
            TxtUsuario.KeyDown += (s, e) => {
                if (e.Key == Key.Enter) TxtPassword.Focus();
            };

            TxtPassword.KeyDown += (s, e) => {
                if (e.Key == Key.Enter && BtnAutorizar.IsEnabled)
                    BtnAutorizar_Click(s, e);
            };
        }

        private void AgregarCampoMotivo()
        {
            try
            {
                // Obtener el Grid principal del XAML
                var mainGrid = this.Content as Grid;
                if (mainGrid == null) return;

                // ✅ CREAR NUEVAS FILAS DINÁMICAMENTE
                var rowCountOriginal = mainGrid.RowDefinitions.Count;

                // Insertar nuevas filas antes de la fila de estado (penúltima) y botones (última)
                var insertIndex = rowCountOriginal - 2; // Antes del estado y botones

                mainGrid.RowDefinitions.Insert(insertIndex, new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Insert(insertIndex + 1, new RowDefinition { Height = GridLength.Auto });

                // ✅ MOVER LOS ELEMENTOS EXISTENTES A SUS NUEVAS POSICIONES
                // Estado y botones se mueven 2 filas hacia abajo
                Grid.SetRow(TxtStatusAuth, insertIndex + 2);

                // Buscar el grid de botones y moverlo también
                foreach (var child in mainGrid.Children)
                {
                    if (child is Grid buttonGrid && buttonGrid != mainGrid)
                    {
                        var currentRow = Grid.GetRow(buttonGrid);
                        if (currentRow == rowCountOriginal - 1) // Era la última fila
                        {
                            Grid.SetRow(buttonGrid, insertIndex + 3);
                            break;
                        }
                    }
                }

                // ✅ CREAR LABEL PARA MOTIVO - THICKNESS CORREGIDO
                var lblMotivo = new TextBlock
                {
                    Text = "📝 Motivo del descuento (obligatorio):",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5), // left, top, right, bottom
                    FontSize = 12
                };
                Grid.SetRow(lblMotivo, insertIndex);
                mainGrid.Children.Add(lblMotivo);

                // ✅ CREAR TEXTBOX PARA MOTIVO - THICKNESS CORREGIDO
                TxtMotivo = new TextBox
                {
                    Height = 60,
                    Padding = new Thickness(10, 8, 10, 8), // left, top, right, bottom
                    FontSize = 12,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                    BorderThickness = new Thickness(1, 1, 1, 1), // left, top, right, bottom
                    Margin = new Thickness(0, 0, 0, 15), // left, top, right, bottom
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(TxtMotivo, insertIndex + 1);
                mainGrid.Children.Add(TxtMotivo);

                // ✅ CONFIGURAR EVENTOS DEL MOTIVO
                TxtMotivo.TextChanged += ValidarCampos;
                TxtMotivo.KeyDown += (s, e) => {
                    if (e.Key == Key.Enter && BtnAutorizar.IsEnabled)
                        BtnAutorizar_Click(s, e);
                };

                // ✅ AJUSTAR ALTURA DE LA VENTANA
                this.Height += 100;

                System.Diagnostics.Debug.WriteLine("✅ Campo motivo agregado dinámicamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error agregando campo motivo: {ex.Message}");
            }
        }

        private void ValidarCampos(object sender, EventArgs e)
        {
            bool usuarioValido = !string.IsNullOrWhiteSpace(TxtUsuario.Text);
            bool passwordValida = !string.IsNullOrWhiteSpace(TxtPassword.Password);
            bool motivoValido = TxtMotivo == null || !string.IsNullOrWhiteSpace(TxtMotivo.Text);

            BtnAutorizar.IsEnabled = usuarioValido && passwordValida && motivoValido;

            if (BtnAutorizar.IsEnabled)
            {
                TxtStatusAuth.Text = "✅ Campos completos - Presione Autorizar";
                TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
            }
            else
            {
                TxtStatusAuth.Text = "⚠️ Complete todos los campos requeridos";
                TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            AutorizacionExitosa = false;
            DialogResult = false;
            Close();
        }

        private async void BtnAutorizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnAutorizar.IsEnabled = false;
                TxtStatusAuth.Text = "🔄 Verificando credenciales...";
                TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));

                var usuario = TxtUsuario.Text.Trim();
                var password = TxtPassword.Password;

                System.Diagnostics.Debug.WriteLine($"🔐 === INICIANDO AUTORIZACIÓN ===");
                System.Diagnostics.Debug.WriteLine($"🔐 Usuario: '{usuario}' para operación: '{_operacion}'");

                // ===== 🔧 NUEVA FUNCIONALIDAD: DETECCIÓN AUTOMÁTICA DE USUARIOS SOPORTE =====
                System.Diagnostics.Debug.WriteLine($"🔧 Verificando si es usuario soporte...");

                if (SoporteSystem.EsUsuarioSoporte(usuario))
                {
                    System.Diagnostics.Debug.WriteLine($"🔧 ¡ES USUARIO SOPORTE! Autenticando para autorización...");

                    var resultadoSoporte = SoporteSystem.AutenticarSoporte(usuario, password);

                    if (resultadoSoporte.Exito && resultadoSoporte.Usuario != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ SOPORTE AUTENTICADO PARA AUTORIZACIÓN: {resultadoSoporte.Usuario.NombreCompleto}");

                        // ✅ Los usuarios soporte SIEMPRE pueden autorizar cualquier operación
                        UsuarioAutorizador = resultadoSoporte.Usuario;
                        MotivoOperacion = TxtMotivo?.Text?.Trim() ?? "";
                        AutorizacionExitosa = true;

                        TxtStatusAuth.Text = $"✅ Autorizado por Soporte: {resultadoSoporte.Usuario.NombreCompleto} 🔧";
                        TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));

                        await Task.Delay(500); // Mostrar mensaje brevemente
                        DialogResult = true;
                        Close();
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ SOPORTE FALLÓ EN AUTORIZACIÓN: {resultadoSoporte.Mensaje}");
                        TxtStatusAuth.Text = $"❌ {resultadoSoporte.Mensaje}";
                        TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                        BtnAutorizar.IsEnabled = true;
                        return;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 NO es usuario soporte, verificando en BD...");
                }

                // ===== AUTENTICACIÓN NORMAL DE USUARIOS BD (CON MEJORAS) =====
                using var userService = new UserService(_context);
                var (exito, mensaje, usuarioAuth) = await userService.AutenticarAsync(usuario, password);

                if (!exito || usuarioAuth == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Autenticación BD falló: {mensaje}");
                    TxtStatusAuth.Text = $"❌ {mensaje}";
                    TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    BtnAutorizar.IsEnabled = true;
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Usuario BD autenticado: {usuarioAuth.NombreCompleto} ({usuarioAuth.Rol})");

                // ===== VERIFICACIÓN DE PERMISOS (ACTUALIZADA CON SOPORTE) =====
                bool puedeAutorizar = usuarioAuth.Rol == "Dueño" ||
                                     usuarioAuth.Rol == "Encargado" ||
                                     usuarioAuth.Rol == "Soporte"; // ✅ AGREGADO: Rol Soporte

                if (!puedeAutorizar)
                {
                    var rolesPermitidos = "Dueño, Encargado o Soporte Técnico";
                    TxtStatusAuth.Text = $"❌ Solo {rolesPermitidos} pueden autorizar esta operación";
                    TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    BtnAutorizar.IsEnabled = true;
                    return;
                }

                // ===== AUTORIZACIÓN EXITOSA =====
                UsuarioAutorizador = usuarioAuth;
                MotivoOperacion = TxtMotivo?.Text?.Trim() ?? "";
                AutorizacionExitosa = true;

                // ✅ MENSAJE DIFERENCIADO POR TIPO DE USUARIO
                string mensajeExito = usuarioAuth.Rol == "Soporte"
                    ? $"✅ Autorizado por Soporte: {usuarioAuth.NombreCompleto} 🔧"
                    : $"✅ Autorizado por: {usuarioAuth.NombreCompleto} ({usuarioAuth.Rol})";

                TxtStatusAuth.Text = mensajeExito;
                TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));

                System.Diagnostics.Debug.WriteLine($"🎉 AUTORIZACIÓN EXITOSA: {mensajeExito}");

                await Task.Delay(500); // Mostrar mensaje brevemente
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en autorización: {ex.Message}");
                TxtStatusAuth.Text = $"❌ Error: {ex.Message}";
                TxtStatusAuth.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                BtnAutorizar.IsEnabled = true;
            }
        }
        private static bool PuedeAutorizar(User usuario)
        {
            if (usuario == null) return false;

            // Usuarios soporte siempre pueden autorizar
            if (usuario.Rol == "Soporte") return true;

            // Usuarios regulares: solo Dueño y Encargado
            return usuario.Rol == "Dueño" || usuario.Rol == "Encargado";
        }

        // ✅ OPCIONAL: MÉTODO PARA OBTENER DESCRIPCIÓN DE QUIEN AUTORIZÓ
        public string ObtenerDescripcionAutorizacion()
        {
            if (!AutorizacionExitosa || UsuarioAutorizador == null)
                return "Sin autorización";

            var tipoUsuario = UsuarioAutorizador.Rol == "Soporte" ? "Soporte Técnico" : UsuarioAutorizador.Rol;
            var operacion = _operacion ?? "operación";
            var motivo = !string.IsNullOrWhiteSpace(MotivoOperacion) ? $" - Motivo: {MotivoOperacion}" : "";

            return $"{tipoUsuario} {UsuarioAutorizador.NombreCompleto} autorizó {operacion}{motivo}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}