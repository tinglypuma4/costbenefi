using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    /// <summary>
    /// Ventana para configuración inicial del sistema - Crear primer usuario Dueño
    /// (Ignora usuarios soporte para validaciones)
    /// </summary>
    public partial class FirstTimeSetupWindow : Window
    {
        public FirstTimeSetupWindow()
        {
            InitializeComponent();

            // Enfocar el primer campo al cargar
            Loaded += (s, e) => TxtNombreCompleto.Focus();
        }

        private async void BtnCrearPropietario_Click(object sender, RoutedEventArgs e)
        {
            await CrearUsuarioPropietario();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro de que desea salir?\n\n" +
                "Sin un usuario propietario no podrá usar el sistema.",
                "Confirmar Salida",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private async Task CrearUsuarioPropietario()
        {
            try
            {
                // ===== VALIDACIONES =====
                if (!ValidarFormulario())
                    return;

                // Deshabilitar botón y mostrar progreso
                BtnCrearPropietario.IsEnabled = false;
                BtnCrearPropietario.Content = "⏳ Creando usuario...";
                TxtStatus.Text = "⏳ Validando información y creando usuario...";

                // ===== CREAR USUARIO EN BASE DE DATOS =====
                using var context = new AppDbContext();

                // ===== 🔧 VERIFICAR DUEÑO IGNORANDO USUARIOS SOPORTE =====
                // Solo verificar usuarios REALES de la base de datos (no soporte)
                var existeDuenoReal = await context.Users
                    .Where(u => u.Id > 0) // Excluir usuarios soporte (ID = -1)
                    .AnyAsync(u => u.Rol == "Dueño" && u.Activo && !u.Eliminado);

                if (existeDuenoReal)
                {
                    MessageBox.Show(
                        "⚠️ Ya existe un usuario Dueño en el sistema.\n\n" +
                        "Solo puede haber un propietario. Si necesita cambiar el usuario propietario, " +
                        "contacte al administrador actual.",
                        "Usuario Dueño Existente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    DialogResult = true; // Continuar al login
                    Close();
                    return;
                }

                // ===== 🔧 VERIFICAR DUPLICADOS IGNORANDO SOPORTE =====
                // Solo verificar usuarios reales de la BD
                var nombreUsuarioExiste = await context.Users
                    .Where(u => u.Id > 0) // Excluir usuarios soporte
                    .AnyAsync(u => u.NombreUsuario.ToLower() == TxtNombreUsuario.Text.Trim().ToLower());

                if (nombreUsuarioExiste)
                {
                    MessageBox.Show(
                        "❌ El nombre de usuario ya está en uso.\n\nPor favor elija otro nombre de usuario.",
                        "Usuario Duplicado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    RestaurarBoton();
                    TxtNombreUsuario.Focus();
                    return;
                }

                var emailExiste = await context.Users
                    .Where(u => u.Id > 0) // Excluir usuarios soporte
                    .AnyAsync(u => u.Email.ToLower() == TxtEmail.Text.Trim().ToLower());

                if (emailExiste)
                {
                    MessageBox.Show(
                        "❌ El correo electrónico ya está registrado.\n\nPor favor use otro correo.",
                        "Email Duplicado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    RestaurarBoton();
                    TxtEmail.Focus();
                    return;
                }

                // ===== CREAR USUARIO PROPIETARIO =====
                var usuarioPropietario = new User
                {
                    NombreCompleto = TxtNombreCompleto.Text.Trim(),
                    NombreUsuario = TxtNombreUsuario.Text.Trim().ToLower(),
                    Email = TxtEmail.Text.Trim().ToLower(),
                    Telefono = TxtTelefono.Text.Trim(),
                    PasswordHash = User.GenerarHashPassword(PwdPassword.Password),
                    Rol = "Dueño",
                    Activo = true,
                    UsuarioCreador = "Sistema - Configuración Inicial",
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                };

                // Guardar en base de datos
                context.Users.Add(usuarioPropietario);
                await context.SaveChangesAsync();

                // ===== CONFIRMACIÓN EXITOSA =====
                TxtStatus.Text = "✅ Usuario propietario creado exitosamente";

                var mensajeExito = "🎉 ¡CONFIGURACIÓN INICIAL COMPLETADA!\n\n" +
                                 $"👤 Usuario Propietario: {usuarioPropietario.NombreCompleto}\n" +
                                 $"🏷️ Nombre de usuario: {usuarioPropietario.NombreUsuario}\n" +
                                 $"📧 Email: {usuarioPropietario.Email}\n" +
                                 $"👑 Rol: {usuarioPropietario.Rol}\n\n" +
                                 "✅ El sistema está listo para usar.\n" +
                                 "Ahora será redirigido a la pantalla de inicio de sesión.\n\n" +
                                 "💡 Consejo: Guarde sus credenciales en un lugar seguro.";

                MessageBox.Show(mensajeExito, "¡Configuración Exitosa!",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Cerrar ventana con éxito
                DialogResult = true;
                Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Error inesperado al crear el usuario propietario:\n\n{ex.Message}\n\n" +
                    "Por favor intente nuevamente o contacte al soporte técnico si el problema persiste.",
                    "Error de Creación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                TxtStatus.Text = "❌ Error al crear usuario - Intente nuevamente";
                RestaurarBoton();
            }
        }

        private bool ValidarFormulario()
        {
            // ===== 🔧 VALIDACIÓN ESPECIAL PARA USUARIOS SOPORTE =====
            // Si el nombre de usuario es de soporte, mostrar advertencia
            var nombreUsuario = TxtNombreUsuario.Text?.Trim() ?? "";
            if (SoporteSystem.EsUsuarioSoporte(nombreUsuario))
            {
                MostrarError("Este nombre de usuario está reservado para soporte técnico.\nPor favor elija otro nombre.", TxtNombreUsuario);
                return false;
            }

            // Validar nombre completo
            if (string.IsNullOrWhiteSpace(TxtNombreCompleto.Text))
            {
                MostrarError("El nombre completo es obligatorio.", TxtNombreCompleto);
                return false;
            }

            if (TxtNombreCompleto.Text.Trim().Length < 3)
            {
                MostrarError("El nombre completo debe tener al menos 3 caracteres.", TxtNombreCompleto);
                return false;
            }

            // Validar nombre de usuario
            if (string.IsNullOrWhiteSpace(TxtNombreUsuario.Text))
            {
                MostrarError("El nombre de usuario es obligatorio.", TxtNombreUsuario);
                return false;
            }

            if (nombreUsuario.Length < 3 || nombreUsuario.Length > 20)
            {
                MostrarError("El nombre de usuario debe tener entre 3 y 20 caracteres.", TxtNombreUsuario);
                return false;
            }

            if (!Regex.IsMatch(nombreUsuario, @"^[a-zA-Z0-9_]+$"))
            {
                MostrarError("El nombre de usuario solo puede contener letras, números y guiones bajos.", TxtNombreUsuario);
                return false;
            }

            // Validar email
            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                MostrarError("El correo electrónico es obligatorio.", TxtEmail);
                return false;
            }

            if (!User.EsEmailValido(TxtEmail.Text.Trim()))
            {
                MostrarError("Por favor ingrese un correo electrónico válido.", TxtEmail);
                return false;
            }

            // Validar contraseña
            if (string.IsNullOrEmpty(PwdPassword.Password))
            {
                MostrarError("La contraseña es obligatoria.", PwdPassword);
                return false;
            }

            if (PwdPassword.Password.Length < 6)
            {
                MostrarError("La contraseña debe tener al menos 6 caracteres.", PwdPassword);
                return false;
            }

            if (PwdPassword.Password != PwdConfirmarPassword.Password)
            {
                MostrarError("Las contraseñas no coinciden.", PwdConfirmarPassword);
                return false;
            }

            // Validar teléfono (opcional)
            if (!string.IsNullOrWhiteSpace(TxtTelefono.Text))
            {
                var telefono = TxtTelefono.Text.Trim();
                if (telefono.Length < 8 || !Regex.IsMatch(telefono, @"^[\d\s\-\(\)\+]+$"))
                {
                    MostrarError("Formato de teléfono inválido. Use solo números, espacios, guiones y paréntesis.", TxtTelefono);
                    return false;
                }
            }

            return true;
        }

        private void MostrarError(string mensaje, FrameworkElement elemento)
        {
            MessageBox.Show($"❌ {mensaje}", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtStatus.Text = $"⚠️ {mensaje}";
            elemento.Focus();
        }

        private void RestaurarBoton()
        {
            BtnCrearPropietario.IsEnabled = true;
            BtnCrearPropietario.Content = "✅ Crear Usuario Propietario";
        }
    }
}