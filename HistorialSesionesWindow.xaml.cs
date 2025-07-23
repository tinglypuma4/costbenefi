using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class HistorialSesionesWindow : Window
    {
        private readonly User _usuario;
        private readonly List<UserSession> _sesiones;

        public HistorialSesionesWindow(User usuario, List<UserSession> sesiones)
        {
            InitializeComponent();
            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
            _sesiones = sesiones ?? new List<UserSession>();

            Loaded += (s, e) => CargarDatos();
        }

        #region CARGA DE DATOS

        private void CargarDatos()
        {
            try
            {
                // Configurar header
                TxtSubtitulo.Text = $"Usuario: {_usuario.NombreCompleto} ({_usuario.NombreUsuario})";
                Title = $"Historial de Sesiones - {_usuario.NombreCompleto}";

                // Cargar sesiones ordenadas por fecha más reciente
                var sesionesOrdenadas = _sesiones
                    .OrderByDescending(s => s.FechaInicio)
                    .ToList();

                DgSesiones.ItemsSource = sesionesOrdenadas;

                // Actualizar estadísticas
                ActualizarEstadisticas();

                // Configurar eventos
                DgSesiones.SelectionChanged += DgSesiones_SelectionChanged;

                // Mensaje si no hay sesiones
                if (!_sesiones.Any())
                {
                    MostrarMensajeSinSesiones();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial de sesiones:\n\n{ex.Message}",
                    "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarEstadisticas()
        {
            var totalSesiones = _sesiones.Count;
            var sesionesActivas = _sesiones.Count(s => s.EstaActiva);
            var sesionesCerradas = totalSesiones - sesionesActivas;

            TxtTotalSesiones.Text = $"{totalSesiones} sesiones";
            TxtSesionesActivas.Text = $"{sesionesActivas} activas";
            TxtSesionesCerradas.Text = $"{sesionesCerradas} cerradas";

            // Mostrar estadísticas adicionales si hay sesiones
            if (totalSesiones > 0)
            {
                MostrarEstadisticasDetalladas();
            }
        }

        private void MostrarEstadisticasDetalladas()
        {
            try
            {
                // ✅ CORECCIÓN: Calcular totalSesiones en este método también
                var totalSesiones = _sesiones.Count;
                var sesionesCompletas = _sesiones.Where(s => !s.EstaActiva).ToList();

                if (sesionesCompletas.Any())
                {
                    var duracionPromedio = sesionesCompletas.Average(s => s.DuracionSesion.TotalMinutes);
                    var ultimaSesion = _sesiones.OrderByDescending(s => s.FechaInicio).FirstOrDefault();

                    var estadisticas = $"📊 Estadísticas adicionales:\n" +
                                     $"• Duración promedio: {duracionPromedio:F0} minutos\n" +
                                     $"• Última conexión: {ultimaSesion?.FechaInicio:dd/MM/yyyy HH:mm}\n" +
                                     $"• Total de conexiones: {totalSesiones}";

                    // Mostrar esto en un tooltip
                    DgSesiones.ToolTip = estadisticas;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en estadísticas detalladas: {ex.Message}");
            }
        }

        private void MostrarMensajeSinSesiones()
        {
            // Crear un mensaje informativo cuando no hay sesiones
            var mensajePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(50)
            };

            var icono = new TextBlock
            {
                Text = "📭",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var titulo = new TextBlock
            {
                Text = "Sin Historial de Sesiones",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descripcion = new TextBlock
            {
                Text = $"El usuario {_usuario.NombreCompleto} aún no ha iniciado sesión en el sistema.",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            mensajePanel.Children.Add(icono);
            mensajePanel.Children.Add(titulo);
            mensajePanel.Children.Add(descripcion);

            // Agregar el mensaje al Grid principal (esto sería mejor hacerlo en XAML, pero funciona)
            var grid = (Grid)Content;
            Grid.SetRow(mensajePanel, 1);
            grid.Children.Add(mensajePanel);
        }

        #endregion

        #region EVENTOS

        private void DgSesiones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sesionSeleccionada = DgSesiones.SelectedItem as UserSession;

            if (sesionSeleccionada != null)
            {
                MostrarDetallesSesion(sesionSeleccionada);
            }
        }

        private void MostrarDetallesSesion(UserSession sesion)
        {
            try
            {
                var detalles = GenerarDetallesSesion(sesion);

                // Mostrar en un MessageBox simple (podrías crear una ventana más elegante)
                MessageBox.Show(detalles, $"Detalles de Sesión - ID: {sesion.Id}",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar detalles:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GenerarDetallesSesion(UserSession sesion)
        {
            var detalles = $"📊 INFORMACIÓN DETALLADA DE LA SESIÓN\n" +
                          $"{'='}{new string('=', 40)}\n\n" +
                          $"🆔 ID de Sesión: {sesion.Id}\n" +
                          $"🔑 Token: {sesion.SessionToken}\n\n" +
                          $"📅 FECHAS Y DURACIÓN:\n" +
                          $"• Inicio: {sesion.FechaInicio:dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm:ss}\n";

            if (sesion.EstaActiva)
            {
                detalles += $"• Estado: 🟢 SESIÓN ACTIVA\n" +
                           $"• Duración actual: {sesion.DuracionFormateada}\n";
            }
            else
            {
                detalles += $"• Cierre: {sesion.FechaCierre:dddd, dd 'de' MMMM 'de' yyyy 'a las' HH:mm:ss}\n" +
                           $"• Duración total: {sesion.DuracionFormateada}\n" +
                           $"• Motivo de cierre: {sesion.MotivoCierre ?? "No especificado"}\n";
            }

            detalles += $"\n🕐 ACTIVIDAD:\n" +
                       $"• Última actividad: {sesion.UltimaActividad:dd/MM/yyyy HH:mm:ss}\n";

            if (sesion.EstaActiva)
            {
                var tiempoInactivo = sesion.TiempoInactividad;
                detalles += $"• Tiempo inactivo: {(int)tiempoInactivo.TotalMinutes} minutos\n";

                if (sesion.SesionExpirada)
                {
                    detalles += $"• ⚠️ SESIÓN EXPIRADA (más de 8 horas inactiva)\n";
                }
            }

            detalles += $"\n💻 INFORMACIÓN TÉCNICA:\n" +
                       $"• Máquina: {sesion.NombreMaquina}\n" +
                       $"• Dirección IP: {sesion.IpAddress}\n" +
                       $"• Versión de la aplicación: {sesion.VersionApp}\n";

            return detalles;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region MÉTODOS AUXILIARES

        /// <summary>
        /// Exporta el historial de sesiones a texto plano
        /// </summary>
        public string ExportarHistorial()
        {
            try
            {
                var reporte = $"HISTORIAL DE SESIONES\n" +
                             $"Usuario: {_usuario.NombreCompleto} ({_usuario.NombreUsuario})\n" +
                             $"Rol: {_usuario.Rol}\n" +
                             $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                             $"{'='}{new string('=', 50)}\n\n";

                if (!_sesiones.Any())
                {
                    reporte += "No hay sesiones registradas para este usuario.\n";
                    return reporte;
                }

                reporte += $"RESUMEN:\n" +
                          $"• Total de sesiones: {_sesiones.Count}\n" +
                          $"• Sesiones activas: {_sesiones.Count(s => s.EstaActiva)}\n" +
                          $"• Sesiones cerradas: {_sesiones.Count(s => !s.EstaActiva)}\n\n" +
                          $"DETALLE DE SESIONES:\n" +
                          $"{'-'}{new string('-', 50)}\n";

                foreach (var sesion in _sesiones.OrderByDescending(s => s.FechaInicio))
                {
                    reporte += $"\nSesión ID: {sesion.Id}\n" +
                              $"Inicio: {sesion.FechaInicio:dd/MM/yyyy HH:mm:ss}\n";

                    if (sesion.EstaActiva)
                    {
                        reporte += $"Estado: ACTIVA (duración: {sesion.DuracionFormateada})\n";
                    }
                    else
                    {
                        reporte += $"Cierre: {sesion.FechaCierre:dd/MM/yyyy HH:mm:ss}\n" +
                                  $"Duración: {sesion.DuracionFormateada}\n" +
                                  $"Motivo: {sesion.MotivoCierre ?? "No especificado"}\n";
                    }

                    reporte += $"Máquina: {sesion.NombreMaquina}\n" +
                              $"IP: {sesion.IpAddress}\n" +
                              $"Versión App: {sesion.VersionApp}\n";
                }

                return reporte;
            }
            catch (Exception ex)
            {
                return $"Error al generar reporte: {ex.Message}";
            }
        }

        /// <summary>
        /// Obtiene estadísticas resumidas del usuario
        /// </summary>
        public dynamic ObtenerEstadisticasUsuario()
        {
            if (!_sesiones.Any())
            {
                return new
                {
                    TotalSesiones = 0,
                    SesionesActivas = 0,
                    DuracionPromedio = TimeSpan.Zero,
                    UltimaSesion = (DateTime?)null,
                    TiempoTotalConectado = TimeSpan.Zero
                };
            }

            var sesionesCompletas = _sesiones.Where(s => !s.EstaActiva).ToList();
            var duracionPromedio = sesionesCompletas.Any()
                ? TimeSpan.FromMinutes(sesionesCompletas.Average(s => s.DuracionSesion.TotalMinutes))
                : TimeSpan.Zero;

            var tiempoTotal = _sesiones.Aggregate(TimeSpan.Zero, (total, sesion) => total.Add(sesion.DuracionSesion));

            return new
            {
                TotalSesiones = _sesiones.Count,
                SesionesActivas = _sesiones.Count(s => s.EstaActiva),
                DuracionPromedio = duracionPromedio,
                UltimaSesion = _sesiones.Max(s => s.FechaInicio),
                TiempoTotalConectado = tiempoTotal
            };
        }

        #endregion

        #region EVENTOS DE TECLADO

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            // Cerrar con Escape
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
                e.Handled = true;
            }

            // Actualizar con F5
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                CargarDatos();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        #endregion
    }
}