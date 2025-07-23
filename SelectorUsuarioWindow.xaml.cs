using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class SelectorUsuarioWindow : Window
    {
        private AppDbContext? _context;
        private List<UsuarioConEstadisticas> _todosLosUsuarios = new();
        private List<UsuarioConEstadisticas> _usuariosFiltrados = new();
        public User? UsuarioSeleccionado { get; private set; }

        public SelectorUsuarioWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => CargarUsuarios();
        }

        #region CARGA DE DATOS

        private async void CargarUsuarios()
        {
            try
            {
                // ✅ CORRECCIÓN: Crear propio contexto
                _context = new AppDbContext();

                TxtContador.Text = "⏳ Cargando...";

                // Cargar usuarios con sus estadísticas
                var usuarios = await _context.Users
                    .Where(u => !u.Eliminado)
                    .OrderBy(u => u.NombreCompleto)
                    .ToListAsync();

                _todosLosUsuarios.Clear();

                foreach (var usuario in usuarios)
                {
                    // Cargar estadísticas de sesiones
                    var cantidadSesiones = await _context.UserSessions
                        .CountAsync(s => s.UserId == usuario.Id);

                    var ultimaSesion = await _context.UserSessions
                        .Where(s => s.UserId == usuario.Id)
                        .OrderByDescending(s => s.FechaInicio)
                        .FirstOrDefaultAsync();

                    var usuarioConStats = new UsuarioConEstadisticas
                    {
                        Usuario = usuario,
                        CantidadSesiones = cantidadSesiones,
                        UltimaSesion = ultimaSesion?.FechaInicio
                    };

                    _todosLosUsuarios.Add(usuarioConStats);
                }

                // Aplicar filtro inicial
                AplicarFiltro("");
                ActualizarContador();

                TxtContador.Text = $"{_todosLosUsuarios.Count} usuarios";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios:\n\n{ex.Message}",
                    "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtContador.Text = "❌ Error";
            }
        }

        private void AplicarFiltro(string filtro)
        {
            if (string.IsNullOrWhiteSpace(filtro))
            {
                _usuariosFiltrados = new List<UsuarioConEstadisticas>(_todosLosUsuarios);
            }
            else
            {
                var filtroLower = filtro.ToLower();
                _usuariosFiltrados = _todosLosUsuarios.Where(u =>
                    u.Usuario.NombreCompleto.ToLower().Contains(filtroLower) ||
                    u.Usuario.NombreUsuario.ToLower().Contains(filtroLower) ||
                    u.Usuario.Email.ToLower().Contains(filtroLower) ||
                    u.Usuario.Rol.ToLower().Contains(filtroLower)
                ).ToList();
            }

            LstUsuarios.ItemsSource = null;
            LstUsuarios.ItemsSource = _usuariosFiltrados;
            ActualizarContador();
        }

        private void ActualizarContador()
        {
            TxtContador.Text = $"{_usuariosFiltrados.Count} de {_todosLosUsuarios.Count} usuarios";
        }

        #endregion

        #region EVENTOS

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltro(TxtBuscar.Text);
        }

        private void LstUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstUsuarios.SelectedItem is UsuarioConEstadisticas usuarioConStats)
            {
                UsuarioSeleccionado = usuarioConStats.Usuario;
                BtnVerHistorial.IsEnabled = true;

                // Actualizar información del usuario seleccionado
                var info = $"👤 {usuarioConStats.Usuario.NombreCompleto} ({usuarioConStats.Usuario.Rol})";
                if (usuarioConStats.CantidadSesiones > 0)
                {
                    info += $" • {usuarioConStats.CantidadSesiones} sesiones";
                }
                else
                {
                    info += " • Sin sesiones registradas";
                }

                TxtUsuarioSeleccionado.Text = info;
            }
            else
            {
                UsuarioSeleccionado = null;
                BtnVerHistorial.IsEnabled = false;
                TxtUsuarioSeleccionado.Text = "Seleccione un usuario de la lista";
            }
        }

        private void LstUsuarios_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UsuarioSeleccionado != null)
            {
                BtnVerHistorial_Click(this, new RoutedEventArgs());
            }
        }

        private void BtnVerHistorial_Click(object sender, RoutedEventArgs e)
        {
            if (UsuarioSeleccionado != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Seleccione un usuario de la lista.",
                    "Usuario Requerido", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }

    /// <summary>
    /// Clase auxiliar para mostrar usuarios con estadísticas en el ListBox
    /// </summary>
    public class UsuarioConEstadisticas
    {
        public User Usuario { get; set; } = null!;
        public int CantidadSesiones { get; set; }
        public DateTime? UltimaSesion { get; set; }

        // Propiedades para binding XAML
        public string NombreCompleto => Usuario?.NombreCompleto ?? "";
        public string NombreUsuario => Usuario?.NombreUsuario ?? "";
        public string Email => Usuario?.Email ?? "";
        public string Rol => Usuario?.Rol ?? "";

        public string RolIcon => Usuario?.Rol switch
        {
            "Dueño" => "👑",
            "Encargado" => "👨‍💼",
            "Cajero" => "👨‍💻",
            _ => "👤"
        };

        public Brush RolColor => Usuario?.Rol switch
        {
            "Dueño" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),     // Rojo
            "Encargado" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Amarillo
            "Cajero" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),     // Verde
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))           // Gris
        };

        public string EstadoTexto => Usuario?.Activo == true ? "Activo" : "Inactivo";

        public Brush EstadoColor => Usuario?.Activo == true
            ? new SolidColorBrush(Color.FromRgb(40, 167, 69))      // Verde
            : new SolidColorBrush(Color.FromRgb(108, 117, 125));   // Gris

        public string UltimoAccesoTexto => UltimaSesion?.ToString("dd/MM/yyyy HH:mm") ?? "Sin acceso";

        public string CantidadSesionesTexto => CantidadSesiones == 0
            ? "Sin sesiones"
            : $"{CantidadSesiones} sesión{(CantidadSesiones == 1 ? "" : "es")}";



    }
}