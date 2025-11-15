using System;
using System.Windows;
using costbenefi.Models;

namespace costbenefi.Views
{
    public partial class ActivationWindow : Window
    {
        public ActivationWindow()
        {
            InitializeComponent();
            txtLicenseKey.Focus();

            // Actualizar hint cuando cambia selección
            rbBasica.Checked += TipoLicencia_Changed;
            rbMedia.Checked += TipoLicencia_Changed;
            rbAvanzada.Checked += TipoLicencia_Changed;
            rbPorVida.Checked += TipoLicencia_Changed;
        }

        private void TipoLicencia_Changed(object sender, RoutedEventArgs e)
        {
            if (txtHint == null) return;

            string tipoSeleccionado = GetTipoLicenciaSeleccionado();

            txtHint.Text = tipoSeleccionado switch
            {
                "BASICA" => "💡 Pegue aquí su código de LICENCIA BÁSICA (1 año)",
                "MEDIA" => "💡 Pegue aquí su código de LICENCIA MEDIA (2 años)",
                "AVANZADA" => "💡 Pegue aquí su código de LICENCIA AVANZADA (3 años)",
                "PORVIDA" => "💡 Pegue aquí su código de LICENCIA DE POR VIDA",
                _ => "💡 Pegue aquí el código completo proporcionado por su proveedor"
            };
        }

        private string GetTipoLicenciaSeleccionado()
        {
            if (rbBasica.IsChecked == true) return "BASICA";
            if (rbMedia.IsChecked == true) return "MEDIA";
            if (rbAvanzada.IsChecked == true) return "AVANZADA";
            if (rbPorVida.IsChecked == true) return "PORVIDA";
            return string.Empty;
        }

        private void BtnActivar_Click(object sender, RoutedEventArgs e)
        {
            string licenseKey = txtLicenseKey.Text.Trim();
            string tipoEsperado = GetTipoLicenciaSeleccionado();

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                MostrarError("Por favor ingrese un código de licencia válido.");
                return;
            }

            if (string.IsNullOrEmpty(tipoEsperado))
            {
                MostrarError("Por favor seleccione el tipo de licencia.");
                return;
            }

            // Deshabilitar botón mientras procesa
            btnActivar.IsEnabled = false;
            btnActivar.Content = "⏳ Validando...";

            try
            {
                // Guardar la licencia en archivo
                if (!LicenseManager.SaveLicense(licenseKey))
                {
                    MostrarError("Error al guardar el archivo de licencia.\n\nVerifique los permisos de escritura.");
                    return;
                }

                // Validar inmediatamente
                var license = LicenseManager.ValidateLicense();

                if (license.IsValid)
                {
                    // Verificar que el tipo coincida con el seleccionado
                    if (license.Type.ToString() != tipoEsperado)
                    {
                        MostrarAdvertencia(
                            $"⚠️ ADVERTENCIA: Tipo de Licencia Incorrecto\n\n" +
                            $"Seleccionó:       {tipoEsperado}\n" +
                            $"Código corresponde a: {license.Type}\n\n" +
                            $"La licencia {license.Type} ha sido activada correctamente.\n\n" +
                            $"Empresa: {license.CompanyName}\n" +
                            $"Válida hasta: {license.ExpirationDate:dd/MM/yyyy}"
                        );
                    }
                    else
                    {
                        // Todo correcto - construir mensaje según tipo
                        string mensaje;
                        string tipoFormateado = FormatearTipoLicencia(license.Type);

                        if (license.Type == LicenseManager.LicenseType.PORVIDA)
                        {
                            mensaje = $"✅ LICENCIA ACTIVADA CORRECTAMENTE\n\n" +
                                     $"═══════════════════════════════════════\n\n" +
                                     $"   Tipo:         {tipoFormateado}\n" +
                                     $"   Empresa:      {license.CompanyName}\n" +
                                     $"   Vigencia:     Para Siempre ∞\n\n" +
                                     $"═══════════════════════════════════════\n\n" +
                                     "🎉 El sistema se iniciará ahora.\n\n" +
                                     "¡Disfrute de su licencia ilimitada!";
                        }
                        else
                        {
                            mensaje = $"✅ LICENCIA ACTIVADA CORRECTAMENTE\n\n" +
                                     $"═══════════════════════════════════════\n\n" +
                                     $"   Tipo:            {tipoFormateado}\n" +
                                     $"   Empresa:         {license.CompanyName}\n" +
                                     $"   Válida hasta:    {license.ExpirationDate:dd/MM/yyyy}\n" +
                                     $"   Días restantes:  {license.DaysRemaining:N0} días\n\n" +
                                     $"═══════════════════════════════════════\n\n" +
                                     "🎉 El sistema se iniciará ahora.";
                        }

                        MostrarExito(mensaje);
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    // Licencia inválida
                    MostrarError(
                        $"❌ CÓDIGO DE LICENCIA INVÁLIDO\n\n" +
                        $"Detalle: {license.Message}\n\n" +
                        "Por favor verifique:\n" +
                        "• Que haya seleccionado el tipo correcto\n" +
                        "• Que el código esté completo\n" +
                        "• Que no haya espacios adicionales\n\n" +
                        "Si el problema persiste, contacte a su proveedor."
                    );

                    // Eliminar archivo de licencia inválida
                    try
                    {
                        if (System.IO.File.Exists("license.key"))
                            System.IO.File.Delete("license.key");
                    }
                    catch { }

                    txtLicenseKey.Clear();
                    txtLicenseKey.Focus();
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al procesar la licencia:\n\n{ex.Message}");
            }
            finally
            {
                // Rehabilitar botón
                btnActivar.IsEnabled = true;
                btnActivar.Content = "✓ Activar Licencia";
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Está seguro que desea cancelar?\n\n" +
                "El sistema no podrá iniciarse sin una licencia válida.",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (resultado == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        // Métodos auxiliares para mostrar mensajes
        private void MostrarError(string mensaje)
        {
            MessageBox.Show(
                mensaje,
                "Error de Activación",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void MostrarAdvertencia(string mensaje)
        {
            MessageBox.Show(
                mensaje,
                "Advertencia",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        private void MostrarExito(string mensaje)
        {
            MessageBox.Show(
                mensaje,
                "Activación Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summar
        /// Formatear tipo de licencia para mostrar
        /// </summary>
        private string FormatearTipoLicencia(LicenseManager.LicenseType tipo)
        {
            return tipo switch
            {
                LicenseManager.LicenseType.BASICA => "Básica 🥉",
                LicenseManager.LicenseType.MEDIA => "Media 🥈",
                LicenseManager.LicenseType.AVANZADA => "Avanzada 🥇",
                LicenseManager.LicenseType.PORVIDA => "De Por Vida 💎",
                _ => tipo.ToString()
            };
        }
    }
}