using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;

namespace costbenefi.Views
{
    public partial class GestionUsuariosWindow : Window
    {
        private AppDbContext? _context;
        private UserService? _userService;
        private List<UsuarioExtendido> _todosLosUsuarios = new();
        private List<UsuarioExtendido> _usuariosFiltrados = new();
        private DispatcherTimer? _timerActualizacion;

        public GestionUsuariosWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => InicializarVentana();
        }

        #region INICIALIZACIÓN

        private async void InicializarVentana()
        {
            try
            {
                TxtStatus.Text = "⏳ Inicializando sistema de usuarios...";

                _context = new AppDbContext();
                _userService = new UserService(_context);

                // Cargar usuarios
                await CargarUsuarios();

                // Configurar auto-actualización cada 30 segundos
                ConfigurarActualizacionAutomatica();

                TxtStatus.Text = "✅ Sistema de usuarios listo";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar:\n\n{ex.Message}",
                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al inicializar";
            }
        }

        private void ConfigurarActualizacionAutomatica()
        {
            _timerActualizacion = new DispatcherTimer();
            _timerActualizacion.Interval = TimeSpan.FromSeconds(30);
            _timerActualizacion.Tick += async (s, e) => await ActualizarDatos();
            _timerActualizacion.Start();
        }

        #endregion

        #region CARGA DE DATOS

        private async Task CargarUsuarios()
        {
            try
            {
                if (_context == null) return;

                TxtStatus.Text = "⏳ Cargando usuarios...";

                var usuarios = await _context.Users
                    .Where(u => !u.Eliminado)
                    .OrderBy(u => u.NombreCompleto)
                    .ToListAsync();

                _todosLosUsuarios.Clear();

                foreach (var usuario in usuarios)
                {
                    // Cargar información adicional para cada usuario
                    var sesiones = await _context.UserSessions
                        .Where(s => s.UserId == usuario.Id)
                        .ToListAsync();

                    var ultimaSesion = sesiones
                        .OrderByDescending(s => s.FechaInicio)
                        .FirstOrDefault();

                    var sesionesActivas = sesiones.Count(s => s.EstaActiva);

                    var usuarioExtendido = new UsuarioExtendido
                    {
                        Usuario = usuario,
                        CantidadSesiones = sesiones.Count,
                        SesionesActivas = sesionesActivas,
                        UltimaSesion = ultimaSesion?.FechaInicio,
                        TiempoTotalConectado = TimeSpan.FromMinutes(
                            sesiones.Where(s => !s.EstaActiva).Sum(s => s.DuracionSesion.TotalMinutes))
                    };

                    _todosLosUsuarios.Add(usuarioExtendido);
                }

                // Aplicar filtros actuales
                AplicarFiltros();

                // Actualizar estadísticas
                ActualizarEstadisticas();

                TxtStatus.Text = $"✅ {_todosLosUsuarios.Count} usuarios cargados";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios:\n\n{ex.Message}",
                    "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al cargar usuarios";
            }
        }

        private void AplicarFiltros()
        {
            var textoBusqueda = TxtBuscar.Text?.ToLower().Trim() ?? "";
            var filtroRol = (CmbFiltroRol.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Todos";
            var filtroEstado = (CmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Todos";

            _usuariosFiltrados = _todosLosUsuarios.Where(u =>
            {
                // Filtro por texto
                bool coincideTexto = string.IsNullOrEmpty(textoBusqueda) ||
                    u.Usuario.NombreCompleto.ToLower().Contains(textoBusqueda) ||
                    u.Usuario.NombreUsuario.ToLower().Contains(textoBusqueda) ||
                    u.Usuario.Email.ToLower().Contains(textoBusqueda) ||
                    u.Usuario.Rol.ToLower().Contains(textoBusqueda);

                // Filtro por rol
                bool coincideRol = filtroRol == "Todos" || u.Usuario.Rol == filtroRol;

                // Filtro por estado
                bool coincideEstado = filtroEstado switch
                {
                    "Activos" => u.Usuario.Activo && !u.Usuario.EstaBloqueado,
                    "Inactivos" => !u.Usuario.Activo,
                    "Bloqueados" => u.Usuario.EstaBloqueado,
                    _ => true
                };

                return coincideTexto && coincideRol && coincideEstado;
            }).ToList();

            DgUsuarios.ItemsSource = null;
            DgUsuarios.ItemsSource = _usuariosFiltrados;

            TxtContadorUsuarios.Text = $"{_usuariosFiltrados.Count} de {_todosLosUsuarios.Count} usuarios mostrados";
        }

        private void ActualizarEstadisticas()
        {
            var activos = _todosLosUsuarios.Count(u => u.Usuario.Activo && !u.Usuario.EstaBloqueado);
            var inactivos = _todosLosUsuarios.Count(u => !u.Usuario.Activo);
            var bloqueados = _todosLosUsuarios.Count(u => u.Usuario.EstaBloqueado);
            var sesionesActivas = _todosLosUsuarios.Sum(u => u.SesionesActivas);

            TxtUsuariosActivos.Text = activos.ToString();
            TxtUsuariosInactivos.Text = inactivos.ToString();
            TxtUsuariosBloqueados.Text = bloqueados.ToString();
            TxtSesionesActivas.Text = sesionesActivas.ToString();
        }

        private async Task ActualizarDatos()
        {
            try
            {
                await CargarUsuarios();
                TxtStatus.Text = $"🔄 Actualizado automáticamente - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "❌ Error en actualización automática";
            }
        }

        #endregion

        #region EVENTOS DE FILTROS

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            TxtBuscar.Focus();
        }

        private void CmbFiltroRol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                AplicarFiltros();
        }

        private void CmbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                AplicarFiltros();
        }

        #endregion

        #region EVENTOS DE SELECCIÓN

        private void DgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgUsuarios.SelectedItem is UsuarioExtendido usuarioSeleccionado)
            {
                // Habilitar botones según el usuario seleccionado
                BtnEditarUsuario.IsEnabled = true;
                BtnCambiarEstado.IsEnabled = true;
                BtnDesbloquear.IsEnabled = usuarioSeleccionado.Usuario.EstaBloqueado;

                // Actualizar información del usuario seleccionado
                var info = $"👤 {usuarioSeleccionado.Usuario.NombreCompleto} ({usuarioSeleccionado.Usuario.Rol}) - ";
                info += $"{usuarioSeleccionado.Usuario.EstadoUsuario} | ";
                info += $"Sesiones: {usuarioSeleccionado.CantidadSesiones} | ";
                info += $"Último acceso: {usuarioSeleccionado.UltimoAccesoTexto}";

                TxtInfoUsuarioSeleccionado.Text = info;
            }
            else
            {
                // Deshabilitar botones
                BtnEditarUsuario.IsEnabled = false;
                BtnCambiarEstado.IsEnabled = false;
                BtnDesbloquear.IsEnabled = false;

                TxtInfoUsuarioSeleccionado.Text = "Seleccione un usuario para ver su información detallada";
            }
        }

        #endregion

        #region ACCIONES DE USUARIOS

        private async void BtnNuevoUsuario_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var crearUsuarioWindow = new CrearEditarUsuarioWindow()
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (crearUsuarioWindow.ShowDialog() == true)
                {
                    await CargarUsuarios();
                    TxtStatus.Text = "✅ Usuario creado exitosamente";

                    MessageBox.Show("✅ Usuario creado exitosamente!",
                        "Usuario Creado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear usuario:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al crear usuario";
            }
        }

        private async void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (DgUsuarios.SelectedItem is UsuarioExtendido usuarioSeleccionado)
            {
                await EditarUsuarioAsync(usuarioSeleccionado.Usuario);
            }
            else
            {
                MessageBox.Show("Seleccione un usuario para editar.",
                    "Usuario Requerido", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnEditarUsuarioRapido_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UsuarioExtendido usuarioExtendido)
            {
                await EditarUsuarioAsync(usuarioExtendido.Usuario);
            }
        }

        private async Task EditarUsuarioAsync(User usuario)
        {
            try
            {
                var editarUsuarioWindow = new CrearEditarUsuarioWindow(usuario)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (editarUsuarioWindow.ShowDialog() == true)
                {
                    await CargarUsuarios();
                    TxtStatus.Text = $"✅ Usuario {usuario.NombreCompleto} actualizado";

                    MessageBox.Show($"✅ Usuario {usuario.NombreCompleto} actualizado exitosamente!",
                        "Usuario Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al editar usuario:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "❌ Error al editar usuario";
            }
        }

        private async void BtnCambiarEstado_Click(object sender, RoutedEventArgs e)
        {
            if (DgUsuarios.SelectedItem is UsuarioExtendido usuarioSeleccionado && _userService != null)
            {
                try
                {
                    var usuario = usuarioSeleccionado.Usuario;
                    var nuevoEstado = !usuario.Activo;
                    var accion = nuevoEstado ? "activar" : "desactivar";

                    var resultado = MessageBox.Show(
                        $"¿Está seguro de que desea {accion} el usuario '{usuario.NombreCompleto}'?",
                        $"Confirmar {accion.ToUpper()}", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        var (exito, mensaje) = await _userService.CambiarEstadoUsuarioAsync(usuario.Id, nuevoEstado);

                        if (exito)
                        {
                            await CargarUsuarios();
                            TxtStatus.Text = $"✅ Usuario {(nuevoEstado ? "activado" : "desactivado")} correctamente";

                            MessageBox.Show($"✅ Usuario {usuario.NombreCompleto} {(nuevoEstado ? "activado" : "desactivado")} exitosamente!",
                                "Estado Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"❌ Error al cambiar estado:\n\n{mensaje}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cambiar estado:\n\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtStatus.Text = "❌ Error al cambiar estado";
                }
            }
        }

        private async void BtnDesbloquear_Click(object sender, RoutedEventArgs e)
        {
            if (DgUsuarios.SelectedItem is UsuarioExtendido usuarioSeleccionado && _userService != null)
            {
                try
                {
                    var usuario = usuarioSeleccionado.Usuario;

                    if (!usuario.EstaBloqueado)
                    {
                        MessageBox.Show("El usuario no está bloqueado.",
                            "Usuario No Bloqueado", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var resultado = MessageBox.Show(
                        $"¿Desbloquear el usuario '{usuario.NombreCompleto}'?\n\n" +
                        $"Se restablecerán los intentos fallidos y se permitirá el acceso.",
                        "Confirmar Desbloqueo", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        var (exito, mensaje) = await _userService.DesbloquearUsuarioAsync(usuario.Id);

                        if (exito)
                        {
                            await CargarUsuarios();
                            TxtStatus.Text = "✅ Usuario desbloqueado correctamente";

                            MessageBox.Show($"✅ Usuario {usuario.NombreCompleto} desbloqueado exitosamente!",
                                "Usuario Desbloqueado", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"❌ Error al desbloquear:\n\n{mensaje}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al desbloquear usuario:\n\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtStatus.Text = "❌ Error al desbloquear usuario";
                }
            }
        }

        private async void BtnVerHistorial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UsuarioExtendido usuarioExtendido && _context != null)
            {
                try
                {
                    var sesiones = await _context.UserSessions
                        .Where(s => s.UserId == usuarioExtendido.Usuario.Id)
                        .OrderByDescending(s => s.FechaInicio)
                        .ToListAsync();

                    var historialWindow = new HistorialSesionesWindow(usuarioExtendido.Usuario, sesiones)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    historialWindow.Show();
                    TxtStatus.Text = $"🕐 Historial abierto para: {usuarioExtendido.Usuario.NombreCompleto}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir historial:\n\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            await ActualizarDatos();
        }

        #endregion

        #region CLEANUP

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _timerActualizacion?.Stop();
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

    /// <summary>
    /// Clase auxiliar para mostrar usuarios con información extendida
    /// </summary>
    public class UsuarioExtendido
    {
        public User Usuario { get; set; } = null!;
        public int CantidadSesiones { get; set; }
        public int SesionesActivas { get; set; }
        public DateTime? UltimaSesion { get; set; }
        public TimeSpan TiempoTotalConectado { get; set; }

        // ===== PROPIEDADES PARA BINDING DIRECTO (delegación al objeto Usuario) =====
        public bool EstaBloqueado => Usuario?.EstaBloqueado ?? false;
        public bool Activo => Usuario?.Activo ?? false;
        public string Rol => Usuario?.Rol ?? "";
        public string NombreUsuario => Usuario?.NombreUsuario ?? "";
        public string NombreCompleto => Usuario?.NombreCompleto ?? "";
        public string Email => Usuario?.Email ?? "";
        public string Telefono => Usuario?.Telefono ?? "";
        public string EstadoUsuario => Usuario?.EstadoUsuario ?? "";
        public DateTime FechaCreacion => Usuario?.FechaCreacion ?? DateTime.MinValue;

        // ===== PROPIEDADES ADICIONALES PARA UI =====
        public string RolIcon => Usuario?.Rol switch
        {
            "Dueño" => "👑",
            "Encargado" => "👨‍💼",
            "Cajero" => "👨‍💻",
            _ => "👤"
        };

        public Brush EstadoColor => Usuario?.EstaBloqueado == true
            ? new SolidColorBrush(Color.FromRgb(239, 68, 68))   // Rojo - Bloqueado
            : Usuario?.Activo == true
                ? new SolidColorBrush(Color.FromRgb(16, 185, 129))  // Verde - Activo
                : new SolidColorBrush(Color.FromRgb(107, 114, 128)); // Gris - Inactivo

        public string UltimoAccesoTexto => UltimaSesion?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
    }
}