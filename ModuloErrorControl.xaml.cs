using System;
using System.Windows;
using System.Windows.Controls;

namespace costbenefi.Views
{
    /// <summary>
    /// UserControl para mostrar errores en los módulos
    /// </summary>
    public partial class ModuloErrorControl : UserControl
    {
        #region Propiedades
        public string NombreModulo { get; private set; }
        public string MensajeError { get; private set; }
        public Exception ExcepcionOriginal { get; private set; }
        public DateTime FechaError { get; private set; }
        public string IdError { get; private set; }
        #endregion

        #region Constructores
        public ModuloErrorControl(string nombreModulo, string mensajeError, Exception excepcion = null)
        {
            InitializeComponent();

            NombreModulo = nombreModulo ?? "Módulo Desconocido";
            MensajeError = mensajeError ?? "Error no especificado";
            ExcepcionOriginal = excepcion;
            FechaError = DateTime.Now;
            IdError = Guid.NewGuid().ToString("N")[0..8].ToUpper();

            ConfigurarError();
        }

        // Constructor simplificado
        public ModuloErrorControl(string nombreModulo, string mensajeError) : this(nombreModulo, mensajeError, null)
        {
        }
        #endregion

        #region Configuración
        private void ConfigurarError()
        {
            try
            {
                // Configurar información básica del error
                TxtModuloError.Text = NombreModulo;
                TxtDescripcionError.Text = MensajeError;
                TxtHoraError.Text = FechaError.ToString("dd/MM/yyyy HH:mm:ss");
                TxtIdError.Text = IdError;
                TxtHoraReporte.Text = $"Error #{IdError} - {FechaError:HH:mm:ss}";

                // Configurar tipo de error basado en la excepción
                if (ExcepcionOriginal != null)
                {
                    TxtTipoError.Text = ExcepcionOriginal.GetType().Name;
                    TxtStackTrace.Text = ExcepcionOriginal.StackTrace ?? "Stack trace no disponible";

                    // Descripción más detallada si hay excepción
                    if (string.IsNullOrEmpty(MensajeError) || MensajeError == "Error no especificado")
                    {
                        TxtDescripcionError.Text = ExcepcionOriginal.Message;
                    }
                }
                else
                {
                    TxtTipoError.Text = "Error de Módulo";
                    TxtStackTrace.Text = "No hay información técnica disponible";
                }

                // Configurar soluciones específicas según el tipo de error
                ConfigurarSolucionesEspecificas();

                // Log del error
                System.Diagnostics.Debug.WriteLine($"❌ ERROR EN MÓDULO: {NombreModulo}");
                System.Diagnostics.Debug.WriteLine($"   💬 Mensaje: {MensajeError}");
                System.Diagnostics.Debug.WriteLine($"   🆔 ID: {IdError}");
                System.Diagnostics.Debug.WriteLine($"   🕒 Hora: {FechaError}");

                if (ExcepcionOriginal != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   ⚡ Excepción: {ExcepcionOriginal.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando ModuloErrorControl: {ex.Message}");
                // Error en el control de error - usar configuración mínima
                TxtModuloError.Text = NombreModulo;
                TxtDescripcionError.Text = "Error en el sistema de manejo de errores";
                TxtTipoError.Text = "Error Crítico";
            }
        }

        private void ConfigurarSolucionesEspecificas()
        {
            try
            {
                // Limpiar soluciones por defecto
                PanelSoluciones.Children.Clear();

                if (ExcepcionOriginal != null)
                {
                    // Soluciones específicas según el tipo de excepción
                    switch (ExcepcionOriginal)
                    {
                        case UnauthorizedAccessException:
                            AgregarSolucion("🔐", "Verifique que tiene permisos suficientes para acceder al módulo");
                            AgregarSolucion("👤", "Contacte al administrador para revisar sus privilegios de usuario");
                            break;

                        case System.IO.FileNotFoundException:
                        case System.IO.DirectoryNotFoundException:
                            AgregarSolucion("📁", "Verifique que todos los archivos del sistema estén presentes");
                            AgregarSolucion("💾", "Reinstale la aplicación si el problema persiste");
                            break;

                        case OutOfMemoryException:
                            AgregarSolucion("💾", "Cierre otras aplicaciones para liberar memoria");
                            AgregarSolucion("🔄", "Reinicie la aplicación para limpiar la memoria");
                            break;

                        case TimeoutException:
                            AgregarSolucion("🌐", "Verifique su conexión a internet o red local");
                            AgregarSolucion("⏱️", "El servidor puede estar sobrecargado, intente más tarde");
                            break;

                        default:
                            AgregarSolucionesGenericas();
                            break;
                    }
                }
                else
                {
                    // Soluciones específicas según el nombre del módulo
                    switch (NombreModulo?.ToLower())
                    {
                        case "rentabilidad":
                        case "rentabilidadmodulocontrol":
                            AgregarSolucion("📊", "Verifique que hay datos de ventas disponibles en el período seleccionado");
                            AgregarSolucion("💰", "Confirme que los productos tienen precios y costos configurados");
                            break;

                        case "abc":
                        case "analisisabc":
                            AgregarSolucion("🔤", "Asegúrese de que hay suficientes productos para el análisis ABC");
                            AgregarSolucion("📈", "Verifique que hay datos de ventas para clasificar");
                            break;

                        case "financiero":
                        case "financieroavanzado":
                            AgregarSolucion("💎", "Confirme que los datos financieros están correctamente configurados");
                            AgregarSolucion("📊", "Verifique los parámetros de cálculo financiero");
                            break;

                        default:
                            AgregarSolucionesGenericas();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error configurando soluciones específicas: {ex.Message}");
                AgregarSolucionesGenericas();
            }
        }

        private void AgregarSolucionesGenericas()
        {
            AgregarSolucion("🔄", "Intente recargar el módulo usando el botón 'Reintentar'");
            AgregarSolucion("🔍", "Verifique que tiene los permisos necesarios para acceder al módulo");
            AgregarSolucion("🏠", "Pruebe con un módulo diferente para verificar el funcionamiento general");
            AgregarSolucion("🚀", "Si el problema persiste, reinicie la aplicación");
        }

        private void AgregarSolucion(string emoji, string descripcion)
        {
            try
            {
                var panelSolucion = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var iconoSolucion = new TextBlock
                {
                    Text = emoji,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var textoSolucion = new TextBlock
                {
                    Text = descripcion,
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.DarkGreen,
                    TextWrapping = TextWrapping.Wrap
                };

                panelSolucion.Children.Add(iconoSolucion);
                panelSolucion.Children.Add(textoSolucion);
                PanelSoluciones.Children.Add(panelSolucion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error agregando solución: {ex.Message}");
            }
        }
        #endregion

        #region Eventos
        private void BtnReintentar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Reintentando cargar módulo: {NombreModulo}");

                // Buscar el AnalisisMainControl padre e intentar recargar el módulo
                var mainControl = BuscarControlPadre<AnalisisMainControl>(this);
                if (mainControl != null)
                {
                    // Intentar recargar el módulo que falló
                    var metodoCargarModulo = mainControl.GetType().GetMethod("CargarModulo",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (metodoCargarModulo != null)
                    {
                        // Determinar el nombre del módulo para el método CargarModulo
                        string nombreModuloParaCargar = NombreModulo switch
                        {
                            var n when n.ToLower().Contains("rentabilidad") => "Rentabilidad",
                            var n when n.ToLower().Contains("abc") => "AnalisisABC",
                            var n when n.ToLower().Contains("financiero") => "FinancieroAvanzado",
                            var n when n.ToLower().Contains("punto") => "PuntoEquilibrio",
                            var n when n.ToLower().Contains("metrica") => "MetricasAvanzadas",
                            var n when n.ToLower().Contains("tendencia") => "Tendencias",
                            _ => NombreModulo
                        };

                        // Invocar el método de forma asíncrona
                        var task = metodoCargarModulo.Invoke(mainControl, new object[] { nombreModuloParaCargar }) as System.Threading.Tasks.Task;

                        MessageBox.Show($"🔄 Reintentando cargar el módulo {NombreModulo}...",
                                      "Reintentando", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No se pudo acceder al método de recarga. Use los botones de navegación superiores.",
                                      "Recarga Manual", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Use los botones de navegación en la parte superior para recargar el módulo.",
                                  "Recarga Manual", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error reintentando carga de módulo: {ex.Message}");
                MessageBox.Show("No se pudo reintentar automáticamente. Use los botones de navegación superiores.",
                              "Error de Recarga", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnVolverInicio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🏠 Volviendo a pantalla de inicio");

                // Buscar el AnalisisMainControl padre y mostrar pantalla de bienvenida
                var mainControl = BuscarControlPadre<AnalisisMainControl>(this);
                if (mainControl != null)
                {
                    var contentPresenter = mainControl.FindName("ContentPresenterModulos") as ContentPresenter;
                    var pantallaBienvenida = mainControl.FindName("PantallaBienvenida") as Border;

                    if (contentPresenter != null && pantallaBienvenida != null)
                    {
                        contentPresenter.Content = null;
                        pantallaBienvenida.Visibility = Visibility.Visible;

                        System.Diagnostics.Debug.WriteLine("🏠 Pantalla de bienvenida mostrada");
                    }
                }
                else
                {
                    MessageBox.Show("✅ Use los botones de navegación superiores para cambiar de módulo.",
                                  "Navegación", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error volviendo al inicio: {ex.Message}");
                MessageBox.Show("Use los botones de navegación superiores para cambiar de módulo.",
                              "Navegación", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnReportarError_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📞 Generando reporte de error para: {NombreModulo}");

                string reporteError = $"📞 REPORTE DE ERROR - ID: {IdError}\n\n" +
                                     $"📋 Información del Error:\n" +
                                     $"• Módulo: {NombreModulo}\n" +
                                     $"• Fecha/Hora: {FechaError:dd/MM/yyyy HH:mm:ss}\n" +
                                     $"• Tipo: {TxtTipoError.Text}\n" +
                                     $"• Descripción: {MensajeError}\n\n" +
                                     $"💻 Información Técnica:\n" +
                                     $"• ID Error: {IdError}\n" +
                                     $"• Versión: {TxtVersionApp.Text}\n";

                if (ExcepcionOriginal != null)
                {
                    reporteError += $"• Excepción: {ExcepcionOriginal.GetType().Name}\n" +
                                   $"• Stack Trace: {ExcepcionOriginal.StackTrace?[0..100]}...\n";
                }

                reporteError += $"\n📧 Envíe este reporte al soporte técnico para una solución rápida.\n" +
                               $"📱 Incluya cualquier información adicional sobre lo que estaba haciendo cuando ocurrió el error.";

                // Copiar al portapapeles para facilitar el envío
                try
                {
                    Clipboard.SetText(reporteError);
                    MessageBox.Show($"📋 Reporte de error copiado al portapapeles.\n\n{reporteError}",
                                  "Reporte Generado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show(reporteError, "Reporte de Error", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error generando reporte: {ex.Message}");
                MessageBox.Show($"Error generando reporte:\nMódulo: {NombreModulo}\nError: {MensajeError}\nHora: {FechaError}",
                              "Reporte Básico", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region Métodos auxiliares
        private T BuscarControlPadre<T>(DependencyObject hijo) where T : DependencyObject
        {
            var padre = LogicalTreeHelper.GetParent(hijo);
            if (padre == null) return null;

            if (padre is T)
                return padre as T;
            else
                return BuscarControlPadre<T>(padre);
        }

        /// <summary>
        /// Método público para actualizar información del error
        /// </summary>
        public void ActualizarInformacionError(string nuevoMensaje, Exception nuevaExcepcion = null)
        {
            try
            {
                MensajeError = nuevoMensaje ?? MensajeError;
                ExcepcionOriginal = nuevaExcepcion ?? ExcepcionOriginal;

                TxtDescripcionError.Text = MensajeError;

                if (ExcepcionOriginal != null)
                {
                    TxtTipoError.Text = ExcepcionOriginal.GetType().Name;
                    TxtStackTrace.Text = ExcepcionOriginal.StackTrace ?? "Stack trace no disponible";
                }

                ConfigurarSolucionesEspecificas();

                System.Diagnostics.Debug.WriteLine($"🔄 Información de error actualizada para: {NombreModulo}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando información de error: {ex.Message}");
            }
        }

        /// <summary>
        /// Método para agregar soluciones personalizadas
        /// </summary>
        public void AgregarSolucionPersonalizada(string icono, string descripcion)
        {
            try
            {
                AgregarSolucion(icono, descripcion);
                System.Diagnostics.Debug.WriteLine($"➕ Solución personalizada agregada: {descripcion}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error agregando solución personalizada: {ex.Message}");
            }
        }
        #endregion
    }
}