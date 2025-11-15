using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using costbenefi.Data;
using costbenefi.Models; // Asegúrate que 'User' está definido aquí
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel; // Para la lista de gastos

namespace costbenefi.Views
{
    public partial class GestionGastosWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly DateTime _fecha;

        // Propiedad pública para saber si se modificó algo
        public bool GastoModificado { get; private set; } = false;

        // Lista observable para el DataGrid
        public ObservableCollection<Movimiento> GastosDelDia { get; set; }

        public GestionGastosWindow(AppDbContext context, DateTime fecha)
        {
            InitializeComponent();
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fecha = fecha.Date;

            GastosDelDia = new ObservableCollection<Movimiento>();
            DgGastos.ItemsSource = GastosDelDia; // Enlazar el DataGrid

            Loaded += GestionGastosWindow_Loaded;
        }

        private async void GestionGastosWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarGastos();
        }

        private async Task CargarGastos()
        {
            try
            {
                ActualizarStatus("Cargando gastos...");
                var gastos = await _context.Set<Movimiento>()
                    .Where(m => m.TipoMovimiento == "Gasto" && m.FechaMovimiento.Date == _fecha)
                    .OrderBy(m => m.FechaMovimiento)
                    .ToListAsync();

                GastosDelDia.Clear();
                foreach (var gasto in gastos)
                {
                    GastosDelDia.Add(gasto);
                }
                ActualizarStatus($"Se encontraron {GastosDelDia.Count} gastos.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar gastos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ActualizarStatus($"Error: {ex.Message}");
            }
        }

        // ===== TU LÓGICA DE ELIMINACIÓN =====

        private async void BtnEliminarGasto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener el movimiento/gasto del Tag del botón
                if (!(sender is Button btn) || !(btn.Tag is Movimiento gasto))
                    return;

                // Validar que sea un gasto (no una venta u otro tipo de movimiento)
                if (gasto.TipoMovimiento != "Gasto")
                {
                    MessageBox.Show("Solo se pueden eliminar gastos.", "Información",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🗑️ Solicitando autorización para eliminar gasto: {gasto.Motivo}");

                // ✅ REQUERIR AUTORIZACIÓN
                // (Asegúrate que la clase AutorizacionDescuentoWindow exista en este proyecto)
                var autorizacionWindow = new AutorizacionDescuentoWindow($"eliminar el gasto '{gasto.Motivo}'")
                {
                    Owner = this, // Ahora el Owner es esta ventana
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (autorizacionWindow.ShowDialog() != true || !autorizacionWindow.AutorizacionExitosa)
                {
                    ActualizarStatus("❌ Autorización para eliminar gasto cancelada");
                    return;
                }

                // ✅ CONFIRMACIÓN ADICIONAL
                var mensaje = $"🗑️ ELIMINAR GASTO\n\n" +
                              $"Concepto: {gasto.Motivo}\n" +
                              $"Monto: {gasto.PrecioConIVA:C2}\n" +
                              $"Fecha: {gasto.FechaMovimiento:dd/MM/yyyy HH:mm}\n" +
                              $"Registrado por: {gasto.Usuario}\n\n" +
                              $"Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}\n\n" +
                              $"⚠️ Esta acción no se puede deshacer.\n\n" +
                              $"¿Confirmar eliminación?";

                var resultado = MessageBox.Show(mensaje, "Confirmar Eliminación de Gasto",
                                                MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Eliminar de la base de datos
                    _context.Set<Movimiento>().Remove(gasto);
                    await _context.SaveChangesAsync();

                    // Notificar que se hizo un cambio
                    GastoModificado = true;

                    // Registrar en el log o auditoría
                    // Esta línea (123 aprox) ahora es válida
                    await RegistrarAuditoriaEliminacion(gasto, autorizacionWindow.UsuarioAutorizador);

                    // Actualizar la UI (recargando la lista)
                    await CargarGastos();
                    ActualizarStatus($"✅ Gasto eliminado: {gasto.Motivo} - Autorizado por: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}");

                    System.Diagnostics.Debug.WriteLine($"✅ Gasto eliminado por autorización de: {autorizacionWindow.UsuarioAutorizador.NombreCompleto}");
                }
                else
                {
                    ActualizarStatus("❌ Eliminación de gasto cancelada");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al eliminar gasto: {ex.Message}");
                ActualizarStatus($"❌ Error al eliminar gasto: {ex.Message}");
                MessageBox.Show($"Error al eliminar el gasto:\n\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método auxiliar para registrar la auditoría
        // ===== CORRECCIÓN AQUÍ (Línea 146 aprox) =====
        // Se cambió 'Usuario' por 'User' para que coincida con tu modelo
        private async Task RegistrarAuditoriaEliminacion(Movimiento gasto, User autorizador)
        {
            try
            {
                // Opción: Registrar en un archivo de log
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "eliminaciones.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                  $"GASTO ELIMINADO - ID: {gasto.Id} | " +
                                  $"Concepto: {gasto.Motivo} | " +
                                  $"Monto: {gasto.PrecioConIVA:C2} | " +
                                  $"Autorizado por: {autorizador.NombreCompleto} | " +
                                  $"Usuario original: {gasto.Usuario}";

                await File.AppendAllTextAsync(logPath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Log pero no detener el proceso principal
                System.Diagnostics.Debug.WriteLine($"Error al registrar auditoría: {ex.Message}");
            }
        }

        // Método para actualizar el status 
        private void ActualizarStatus(string mensaje)
        {
            if (TxtStatus != null)
                TxtStatus.Text = mensaje;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // Notifica a CorteCajaWindow que chequee GastoModificado
            Close();
        }
    }
}