using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;
using Microsoft.EntityFrameworkCore;

namespace costbenefi.Views
{
    public partial class CrearEditarUsuarioWindow : Window
    {
        private AppDbContext? _context;
        private UserService? _userService;
        private readonly User? _usuarioOriginal;
        private readonly bool _esNuevo;
        private bool _validandoFormulario = false;

        public CrearEditarUsuarioWindow(User? usuario = null)
        {
            InitializeComponent();
            _usuarioOriginal = usuario;
            _esNuevo = usuario == null;

            Loaded += (s, e) => InicializarFormulario();
        }

        #region INICIALIZACIÓN

        private void InicializarFormulario()
        {
            try
            {
                // 🔍 DIAGNÓSTICO TEMPORAL - AGREGAR ESTAS LÍNEAS
                System.Diagnostics.Debug.WriteLine("🔍 === VENTANA CREAR USUARIO INICIADA ===");
                System.Diagnostics.Debug.WriteLine($"🔍 UserService.UsuarioActual es null: {UserService.UsuarioActual == null}");
                if (UserService.UsuarioActual != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 UserService.UsuarioActual.NombreCompleto: {UserService.UsuarioActual.NombreCompleto}");
                    System.Diagnostics.Debug.WriteLine($"🔍 UserService.UsuarioActual.Rol: {UserService.UsuarioActual.Rol}");
                }
                System.Diagnostics.Debug.WriteLine($"🔍 UserService.SesionActual es null: {UserService.SesionActual == null}");
                System.Diagnostics.Debug.WriteLine("🔍 =======================================");
                // FIN DEL DIAGNÓSTICO

                _context = new AppDbContext();
                _userService = new UserService(_context);
                // 🔍 DIAGNÓSTICO TEMPORAL - AGREGAR ESTA LÍNEA
                System.Diagnostics.Debug.WriteLine($"🔍 VENTANA CREAR USUARIO - UsuarioActual es null: {UserService.UsuarioActual == null}");

                // Configurar header según el modo
                if (_esNuevo)
                {
                    TxtTitulo.Text = "👤 NUEVO USUARIO";
                    TxtSubtitulo.Text = "Complete la información del nuevo empleado";
                    Title = "Nuevo Usuario - Sistema POS";

                    System.Diagnostics.Debug.WriteLine($"🔍 VENTANA - UsuarioActual.Rol: {UserService.UsuarioActual.Rol}");

                }
                else
                {
                    TxtTitulo.Text = "✏️ EDITAR USUARIO";
                    TxtSubtitulo.Text = $"Modificar información de: {_usuarioOriginal?.NombreCompleto}";
                    Title = $"Editar Usuario: {_usuarioOriginal?.NombreCompleto}";
                }

                // Configurar ComboBox de roles
                CmbRol.SelectedIndex = 2; // Cajero por defecto

                // Cargar datos si es edición
                if (!_esNuevo && _usuarioOriginal != null)
                {
                    CargarDatosUsuario();
                    ConfigurarModoEdicion();
                }
                else
                {
                    ConfigurarModoCreacion();
                }

                // Actualizar descripción del rol por defecto
                ActualizarDescripcionRol();

                // Validar formulario inicial
                ValidarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar formulario:\n\n{ex.Message}",
                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
        private void CargarDatosUsuario()
        {
            if (_usuarioOriginal == null) return;

            TxtNombreUsuario.Text = _usuarioOriginal.NombreUsuario;
            TxtNombreCompleto.Text = _usuarioOriginal.NombreCompleto;
            TxtEmail.Text = _usuarioOriginal.Email;
            TxtTelefono.Text = _usuarioOriginal.Telefono;
            ChkUsuarioActivo.IsChecked = _usuarioOriginal.Activo;

            // Seleccionar rol
            foreach (ComboBoxItem item in CmbRol.Items)
            {
                if (item.Content.ToString() == _usuarioOriginal.Rol)
                {
                    CmbRol.SelectedItem = item;
                    break;
                }
            }
        }

        private void ConfigurarModoCreacion()
        {
            // Ocultar panel de estado
            PanelEstado.Visibility = Visibility.Collapsed;
            PanelCambiarPassword.Visibility = Visibility.Collapsed;

            // Mostrar campos de contraseña
            PanelPassword.Visibility = Visibility.Visible;
            LabelPassword.Text = "* Contraseña:";
        }

        private void ConfigurarModoEdicion()
        {
            // Mostrar panel de estado
            PanelEstado.Visibility = Visibility.Visible;
            PanelCambiarPassword.Visibility = Visibility.Visible;

            // Ocultar campos de contraseña inicialmente
            PanelPassword.Visibility = Visibility.Collapsed;

            // Deshabilitar cambio de usuario
            TxtNombreUsuario.IsReadOnly = true;
            TxtNombreUsuario.Background = System.Windows.Media.Brushes.LightGray;

            // Verificar si se puede editar este usuario
            if (_usuarioOriginal?.Rol == "Dueño")
            {
                var cantidadDuenos = 1; // Por ahora asumimos que es el único
                if (cantidadDuenos <= 1)
                {
                    CmbRol.IsEnabled = false;
                    CmbRol.ToolTip = "No se puede cambiar el rol del único Dueño del sistema";
                }
            }
        }

        #endregion

        #region VALIDACIÓN DEL FORMULARIO

        private void ValidarFormulario(object? sender = null, RoutedEventArgs? e = null)
        {
            if (_validandoFormulario || !IsLoaded) return;
            _validandoFormulario = true;

            try
            {
                bool formularioValido = true;

                // Validar nombre de usuario
                formularioValido &= ValidarNombreUsuario();

                // Validar nombre completo
                formularioValido &= ValidarNombreCompleto();

                // Validar email
                formularioValido &= ValidarEmail();

                // Validar contraseñas (solo si están visibles)
                if (PanelPassword.Visibility == Visibility.Visible)
                {
                    formularioValido &= ValidarPasswords();
                }

                // Validar rol
                formularioValido &= ValidarRol();

                // Actualizar botón guardar
                BtnGuardar.IsEnabled = formularioValido;

                // Actualizar resumen
                ActualizarResumen(formularioValido);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en validación: {ex.Message}");
            }
            finally
            {
                _validandoFormulario = false;
            }
        }

        private bool ValidarNombreUsuario()
        {
            var usuario = TxtNombreUsuario.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MostrarError(TxtErrorUsuario, "El nombre de usuario es requerido");
                return false;
            }

            if (usuario.Length < 3)
            {
                MostrarError(TxtErrorUsuario, "Mínimo 3 caracteres");
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(usuario, @"^[a-z0-9._]+$"))
            {
                MostrarError(TxtErrorUsuario, "Solo letras minúsculas, números, puntos y guiones bajos");
                return false;
            }

            OcultarError(TxtErrorUsuario);
            return true;
        }

        private bool ValidarNombreCompleto()
        {
            var nombre = TxtNombreCompleto.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MostrarError(TxtErrorNombre, "El nombre completo es requerido");
                return false;
            }

            if (nombre.Length < 3)
            {
                MostrarError(TxtErrorNombre, "Mínimo 3 caracteres");
                return false;
            }

            OcultarError(TxtErrorNombre);
            return true;
        }

        private bool ValidarEmail()
        {
            var email = TxtEmail.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                MostrarError(TxtErrorEmail, "El email es requerido");
                return false;
            }

            if (!User.EsEmailValido(email))
            {
                MostrarError(TxtErrorEmail, "Formato de email inválido");
                return false;
            }

            OcultarError(TxtErrorEmail);
            return true;
        }

        private bool ValidarPasswords()
        {
            var password = TxtPassword.Password;
            var confirmar = TxtConfirmarPassword.Password;

            // Validar contraseña
            if (string.IsNullOrWhiteSpace(password))
            {
                MostrarError(TxtErrorPassword, "La contraseña es requerida");
                return false;
            }

            if (password.Length < 6)
            {
                MostrarError(TxtErrorPassword, "Mínimo 6 caracteres");
                return false;
            }

            OcultarError(TxtErrorPassword);

            // Validar confirmación
            if (string.IsNullOrWhiteSpace(confirmar))
            {
                MostrarError(TxtErrorConfirmar, "Confirme la contraseña");
                return false;
            }

            if (password != confirmar)
            {
                MostrarError(TxtErrorConfirmar, "Las contraseñas no coinciden");
                return false;
            }

            OcultarError(TxtErrorConfirmar);
            return true;
        }

        private bool ValidarRol()
        {
            return CmbRol.SelectedItem != null;
        }

        private void MostrarError(TextBlock textBlock, string mensaje)
        {
            textBlock.Text = mensaje;
            textBlock.Visibility = Visibility.Visible;
        }

        private void OcultarError(TextBlock textBlock)
        {
            textBlock.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region EVENTOS

        private void CmbRol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarDescripcionRol();
            ValidarFormulario();
        }

        private void ActualizarDescripcionRol()
        {
            var rolSeleccionado = (CmbRol.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";

            switch (rolSeleccionado)
            {
                case "Dueño":
                    TxtNombreRol.Text = "Dueño";
                    TxtDescripcionRol.Text = "Control total del negocio";
                    ListaPermisos.ItemsSource = new[] { "POS", "Inventario", "Ventas", "Reportes", "Usuarios", "Configuración", "Corte de Caja", "Eliminación" };
                    AdvertenciaDueno.Visibility = Visibility.Visible;
                    break;
                case "Encargado":
                    TxtNombreRol.Text = "Encargado";
                    TxtDescripcionRol.Text = "Maneja operaciones cuando no está el dueño";
                    ListaPermisos.ItemsSource = new[] { "POS", "Inventario", "Ventas", "Reportes", "Corte de Caja" };
                    AdvertenciaDueno.Visibility = Visibility.Collapsed;
                    break;
                case "Cajero":
                    TxtNombreRol.Text = "Cajero";
                    TxtDescripcionRol.Text = "Atiende clientes y maneja caja";
                    ListaPermisos.ItemsSource = new[] { "POS", "Corte de Caja", "Inventario Básico" };
                    AdvertenciaDueno.Visibility = Visibility.Collapsed;
                    break;
                default:
                    TxtNombreRol.Text = "Seleccione un rol";
                    TxtDescripcionRol.Text = "";
                    ListaPermisos.ItemsSource = new string[0];
                    AdvertenciaDueno.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void ChkCambiarPassword_Checked(object sender, RoutedEventArgs e)
        {
            PanelPassword.Visibility = Visibility.Visible;
            LabelPassword.Text = "* Nueva Contraseña:";
            ValidarFormulario();
        }

        private void ChkCambiarPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            PanelPassword.Visibility = Visibility.Collapsed;
            TxtPassword.Clear();
            TxtConfirmarPassword.Clear();
            OcultarError(TxtErrorPassword);
            OcultarError(TxtErrorConfirmar);
            ValidarFormulario();
        }

        #endregion

        #region RESUMEN

        private void ActualizarResumen(bool formularioValido)
        {
            try
            {
                if (!formularioValido)
                {
                    TxtResumen.Text = "❌ Complete todos los campos requeridos correctamente";
                    return;
                }

                var nombreUsuario = TxtNombreUsuario.Text?.Trim() ?? "";
                var nombreCompleto = TxtNombreCompleto.Text?.Trim() ?? "";
                var email = TxtEmail.Text?.Trim() ?? "";
                var rol = (CmbRol.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                var telefono = TxtTelefono.Text?.Trim() ?? "";

                var resumen = $"✅ USUARIO VÁLIDO\n\n" +
                             $"👤 Usuario: {nombreUsuario}\n" +
                             $"📋 Nombre: {nombreCompleto}\n" +
                             $"📧 Email: {email}\n" +
                             $"🎯 Rol: {rol}\n";

                if (!string.IsNullOrEmpty(telefono))
                {
                    resumen += $"📞 Teléfono: {telefono}\n";
                }

                if (_esNuevo)
                {
                    resumen += "\n🔐 Se creará con contraseña segura";
                }
                else
                {
                    var activo = ChkUsuarioActivo?.IsChecked == true ? "Activo" : "Inactivo";
                    resumen += $"\n📊 Estado: {activo}";

                    if (PanelPassword.Visibility == Visibility.Visible)
                    {
                        resumen += "\n🔄 Se cambiará la contraseña";
                    }
                }

                TxtResumen.Text = resumen;
            }
            catch (Exception ex)
            {
                TxtResumen.Text = "❌ Error al generar resumen";
                System.Diagnostics.Debug.WriteLine($"Error en resumen: {ex.Message}");
            }
        }

        #endregion

        #region GUARDAR Y CANCELAR


        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnGuardar.IsEnabled = false;
                BtnGuardar.Content = "⏳ Guardando...";

                if (_esNuevo)
                {
                    await CrearUsuarioAsync();
                }
                else
                {
                    await ActualizarUsuarioAsync();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al guardar usuario:\n\n{ex.Message}",
                    "Error al Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnGuardar.IsEnabled = true;
                BtnGuardar.Content = "💾 Guardar Usuario";
            }
        }

        private async Task CrearUsuarioAsync()
        {
            if (_context == null || _userService == null) return;

            var nombreUsuario = TxtNombreUsuario.Text.Trim().ToLower();
            var nombreCompleto = TxtNombreCompleto.Text.Trim();
            var email = TxtEmail.Text.Trim();
            var password = TxtPassword.Password;
            var rol = (CmbRol.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            var telefono = TxtTelefono.Text.Trim();

            // Validaciones adicionales antes de crear
            if (await _context.ExisteNombreUsuarioAsync(nombreUsuario))
            {
                throw new Exception($"El nombre de usuario '{nombreUsuario}' ya está en uso.");
            }

            if (await _context.ExisteEmailAsync(email))
            {
                throw new Exception($"El email '{email}' ya está registrado.");
            }

            if (rol == "Dueño")
            {
                var existeDueno = await _context.Users.AnyAsync(u => u.Rol == "Dueño" && u.Activo);
                if (existeDueno)
                {
                    throw new Exception("Ya existe un Dueño en el sistema. Solo puede haber uno.");
                }
            }

            var (exito, mensaje, usuario) = await _userService.CrearUsuarioAsync(
                nombreUsuario, nombreCompleto, email, password, rol, telefono);

            if (!exito)
            {
                throw new Exception(mensaje);
            }
        }

        private async Task ActualizarUsuarioAsync()
        {
            if (_usuarioOriginal == null || _context == null || _userService == null) return;

            var nombreCompleto = TxtNombreCompleto.Text.Trim();
            var email = TxtEmail.Text.Trim();
            var rol = (CmbRol.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            var telefono = TxtTelefono.Text.Trim();
            var nuevaPassword = PanelPassword.Visibility == Visibility.Visible ? TxtPassword.Password : null;

            // Validaciones adicionales
            if (await _context.ExisteEmailAsync(email, _usuarioOriginal.Id))
            {
                throw new Exception($"El email '{email}' ya está registrado por otro usuario.");
            }

            var (exito, mensaje) = await _userService.ActualizarUsuarioAsync(
                _usuarioOriginal.Id, nombreCompleto, email, rol, telefono, nuevaPassword);

            if (!exito)
            {
                throw new Exception(mensaje);
            }

            // Cambiar estado si es necesario
            var nuevoEstado = ChkUsuarioActivo?.IsChecked == true;
            if (_usuarioOriginal.Activo != nuevoEstado)
            {
                var (exitoEstado, mensajeEstado) = await _userService.CambiarEstadoUsuarioAsync(
                    _usuarioOriginal.Id, nuevoEstado);

                if (!exitoEstado)
                {
                    throw new Exception($"Usuario actualizado, pero error al cambiar estado: {mensajeEstado}");
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Está seguro de que desea cancelar?\n\nSe perderán todos los cambios realizados.",
                "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        #endregion

        #region CLEANUP

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _userService?.Dispose();
                _context?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en cleanup: {ex.Message}");
            }

            base.OnClosed(e);
        }

        #endregion
    }
}