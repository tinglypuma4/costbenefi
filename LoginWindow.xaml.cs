using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using costbenefi.Data;
using costbenefi.Services;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana de inicio de sesión con detección automática de usuarios soporte
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Enfocar el campo de usuario al cargar
            Loaded += (s, e) => TxtUsuario.Focus();

            // Permitir login con Enter en ambos campos
            TxtUsuario.KeyDown += (s, e) => { if (e.Key == Key.Enter) PwdPassword.Focus(); };
            PwdPassword.KeyDown += async (s, e) => { if (e.Key == Key.Enter) await RealizarLogin(); };
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            await RealizarLogin();
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro de que desea salir del sistema?",
                "Confirmar Salida",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private async Task RealizarLogin()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔐 Iniciando proceso de login...");

                // ===== VALIDACIONES BÁSICAS =====
                var usuario = TxtUsuario.Text.Trim();
                var password = PwdPassword.Password;

                System.Diagnostics.Debug.WriteLine($"🔐 Usuario ingresado: '{usuario}'");
                System.Diagnostics.Debug.WriteLine($"🔐 Password length: {password?.Length ?? 0}");

                if (string.IsNullOrEmpty(usuario))
                {
                    MostrarError("Por favor ingrese su nombre de usuario.");
                    TxtUsuario.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    MostrarError("Por favor ingrese su contraseña.");
                    PwdPassword.Focus();
                    return;
                }

                // ===== DESHABILITAR INTERFAZ DURANTE LOGIN =====
                BtnLogin.IsEnabled = false;
                BtnLogin.Content = "⏳ Verificando...";
                TxtStatus.Text = "🔐 Verificando credenciales...";
                TxtUsuario.IsEnabled = false;
                PwdPassword.IsEnabled = false;

                System.Diagnostics.Debug.WriteLine("🔐 Interfaz deshabilitada, iniciando autenticación...");

                // ===== AUTENTICACIÓN CON DETECCIÓN AUTOMÁTICA DE SOPORTE =====
                using var context = new AppDbContext();
                using var userService = new UserService(context);

                System.Diagnostics.Debug.WriteLine("🔐 Contexto y servicio creados, llamando AutenticarAsync...");
                var (exito, mensaje, usuarioAutenticado) = await userService.AutenticarAsync(usuario, password);

                System.Diagnostics.Debug.WriteLine($"🔐 Resultado autenticación - Éxito: {exito}, Mensaje: '{mensaje}'");
                System.Diagnostics.Debug.WriteLine($"🔐 Usuario autenticado: {usuarioAutenticado?.NombreCompleto ?? "null"}");

                if (exito && usuarioAutenticado != null)
                {
                    // ===== LOGIN EXITOSO =====
                    System.Diagnostics.Debug.WriteLine("✅ Login exitoso, actualizando interfaz...");

                    // ===== 🔧 DETECTAR SI ES USUARIO SOPORTE =====
                    bool esSoporte = SoporteSystem.UsuarioActualEsSoporte();
                    string tipoUsuario = esSoporte ? "Soporte Técnico 🔧" : usuarioAutenticado.Rol;
                    string colorStatus = esSoporte ? "#FF6B35" : "#28A745"; // Naranja para soporte, verde para normal

                    TxtStatus.Text = $"✅ ¡Bienvenido, {usuarioAutenticado.NombreCompleto}! ({tipoUsuario})";
                    TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStatus));

                    // ===== 🔧 MENSAJE ESPECIAL PARA SOPORTE =====
                    if (esSoporte)
                    {
                        System.Diagnostics.Debug.WriteLine("🔧 ACCESO DE SOPORTE DETECTADO");

                        // Mostrar mensaje discreto para soporte
                        MessageBox.Show($"🔧 ACCESO DE SOPORTE CONCEDIDO\n\n" +
                                       $"Usuario: {usuarioAutenticado.NombreCompleto}\n" +
                                       $"Nivel: {SoporteSystem.ObtenerUsuarioSoporteActual()?.Nivel ?? NivelSoporte.Basico}\n" +
                                       $"Permisos: TOTAL\n\n" +
                                       $"• Acceso invisible al sistema\n" +
                                       $"• No aparece en estadísticas\n" +
                                       $"• Puede gestionar todo sin restricciones",
                            "🔧 Acceso Soporte - CostBenefi",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Mensaje normal para usuarios regulares
                        System.Diagnostics.Debug.WriteLine($"✅ Bienvenido: {usuarioAutenticado.NombreCompleto} ({usuarioAutenticado.Rol})");
                    }

                    // Breve pausa para mostrar el mensaje de bienvenida
                    await Task.Delay(800);

                    // CRÍTICO: Establecer DialogResult = true
                    System.Diagnostics.Debug.WriteLine("✅ Estableciendo DialogResult = true...");
                    this.DialogResult = true;

                    System.Diagnostics.Debug.WriteLine("✅ Cerrando LoginWindow...");
                    this.Close();

                    System.Diagnostics.Debug.WriteLine("✅ LoginWindow cerrada exitosamente");
                }
                else
                {
                    // ===== LOGIN FALLIDO =====
                    System.Diagnostics.Debug.WriteLine($"❌ Login fallido: {mensaje}");
                    MostrarError(mensaje);

                    // Limpiar contraseña para reintento
                    PwdPassword.Password = "";
                    PwdPassword.Focus();

                    // Delay de seguridad
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR EN REALIZARLOGIN: {ex}");
                MostrarError($"Error durante el inicio de sesión: {ex.Message}");
            }
            finally
            {
                // ===== RESTAURAR INTERFAZ =====
                System.Diagnostics.Debug.WriteLine("🔐 Restaurando interfaz...");
                BtnLogin.IsEnabled = true;
                BtnLogin.Content = "🚀 Iniciar Sesión";
                TxtUsuario.IsEnabled = true;
                PwdPassword.IsEnabled = true;

                // Restaurar color normal del status
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125));

                System.Diagnostics.Debug.WriteLine("✅ Interfaz restaurada");
            }
        }

        private void MostrarError(string mensaje)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Mostrando error: {mensaje}");

            TxtStatus.Text = $"❌ {mensaje}";
            TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));

            // MessageBox simple
            MessageBox.Show($"❌ {mensaje}", "Error de Acceso",
                           MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Maneja el evento de cierre de ventana
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Si no se completó el login exitosamente, preguntar antes de cerrar
            if (DialogResult != true)
            {
                var result = MessageBox.Show(
                    "¿Está seguro de que desea salir del sistema sin iniciar sesión?",
                    "Confirmar Cierre",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }

        // ===== 🔧 MÉTODOS ADICIONALES PARA SOPORTE (OPCIONALES) =====

        /// <summary>
        /// Método para mostrar información de usuarios soporte (para desarrollo)
        /// Activar con Ctrl + Shift + I
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Easter egg para mostrar info de soporte en desarrollo
            if (e.Key == Key.I &&
                Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftShift))
            {
                var info = "🔧 INFORMACIÓN DE SOPORTE\n\n" +
                          "Usuarios disponibles:\n" +
                          "• soporte_admin (Super Admin)\n" +
                          "• dev_access (Desarrollo)\n\n" +
                          "Funciones especiales:\n" +
                          "• Acceso total sin restricciones\n" +
                          "• Invisible para sistema normal\n" +
                          "• No afecta validaciones de negocio\n\n" +
                          "Uso: Simplemente login normal con credenciales soporte";

                MessageBox.Show(info, "🔧 Info Soporte - CostBenefi",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
    }
}