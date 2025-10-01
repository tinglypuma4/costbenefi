using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace costbenefi.Models
{
    public class ConfiguracionBascula
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(10)]
        public string Puerto { get; set; } = "COM1";

        [Required]
        public int BaudRate { get; set; } = 9600;

        // ✅ NUEVAS PROPIEDADES PARA CONFIGURACIÓN SERIE COMPLETA
        public int DataBits { get; set; } = 8;

        public Parity Parity { get; set; } = Parity.None;

        public StopBits StopBits { get; set; } = StopBits.One;

        public Handshake Handshake { get; set; } = Handshake.None;

        [Required]
        public int TimeoutLectura { get; set; } = 2000;

        [Required]
        public int IntervaloLectura { get; set; } = 1000;

        [StringLength(10)]
        public string UnidadPeso { get; set; } = "kg";

        public bool RequiereSolicitudPeso { get; set; } = true;

        [StringLength(20)]
        public string ComandoSolicitarPeso { get; set; } = "P";

        [StringLength(20)]
        public string ComandoTara { get; set; } = "T";

        [Required]
        [StringLength(200)]
        public string PatronExtraccion { get; set; } = @"(\d+\.?\d*)";

        [StringLength(10)]
        public string TerminadorLinea { get; set; } = "\r\n";

        public bool EsConfiguracionActiva { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaActualizacion { get; set; }

        [StringLength(100)]
        public string UsuarioCreacion { get; set; }

        // ✅ MÉTODOS DE CONFIGURACIÓN PREDEFINIDA PARA DIFERENTES MARCAS

        /// <summary>
        /// Configuración específica para RHINO BAR-8RS
        /// </summary>
        public static ConfiguracionBascula ConfiguracionRhino()
        {
            return new ConfiguracionBascula
            {
                Nombre = "RHINO BAR-8RS",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 2000,
                IntervaloLectura = 1000,
                UnidadPeso = "kg",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "P", // También acepta 80 decimal, 50 hex
                ComandoTara = "T",
                PatronExtraccion = @"(\d+\.?\d*)",
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        /// <summary>
        /// Configuración para básculas OHAUS
        /// </summary>
        public static ConfiguracionBascula ConfiguracionOhaus()
        {
            return new ConfiguracionBascula
            {
                Nombre = "OHAUS",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 2000,
                IntervaloLectura = 1500,
                UnidadPeso = "g",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "IP", // Immediate Print
                ComandoTara = "T",
                PatronExtraccion = @"(\d+\.?\d*)\s*g",
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        /// <summary>
        /// Configuración para básculas Mettler Toledo
        /// </summary>
        public static ConfiguracionBascula ConfiguracionMettler()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Mettler Toledo",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 2000,
                IntervaloLectura = 1200,
                UnidadPeso = "g",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "S", // Send stable weight
                ComandoTara = "T",
                PatronExtraccion = @"S\s+S\s+(\d+\.?\d*)\s*g",
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        /// <summary>
        /// Configuración para básculas Torrey (muy común en México)
        /// </summary>
        public static ConfiguracionBascula ConfiguracionTorrey()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Torrey",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 2000,
                IntervaloLectura = 1000,
                UnidadPeso = "kg",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "W", // Weight
                ComandoTara = "T",
                PatronExtraccion = @"(\d+\.?\d*)",
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        /// <summary>
        /// Configuración para básculas EXCELL
        /// </summary>
        public static ConfiguracionBascula ConfiguracionExcell()
        {
            return new ConfiguracionBascula
            {
                Nombre = "EXCELL",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 2500,
                IntervaloLectura = 1000,
                UnidadPeso = "kg",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "P",
                ComandoTara = "Z", // Zero/Tare
                PatronExtraccion = @"ST,GS,\+?\s*(\d+\.?\d*)", // Protocolo estándar
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        /// <summary>
        /// Configuración para básculas Toledo
        /// </summary>
        public static ConfiguracionBascula ConfiguracionToledo()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Toledo",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 2000,
                IntervaloLectura = 1000,
                UnidadPeso = "kg",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "W",
                ComandoTara = "Z",
                PatronExtraccion = @"ST,GS,\+?\s*(\d+\.?\d*)",
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        /// <summary>
        /// Configuración genérica universal (compatible con la mayoría de básculas)
        /// </summary>
        public static ConfiguracionBascula ConfiguracionGenerica()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Báscula Genérica Universal",
                Puerto = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                TimeoutLectura = 3000, // Mayor timeout para compatibilidad
                IntervaloLectura = 1500,
                UnidadPeso = "kg",
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "P", // Comando más universal
                ComandoTara = "T",
                PatronExtraccion = @"(\d+\.?\d*)", // Patrón simple pero efectivo
                TerminadorLinea = "\r\n",
                UsuarioCreacion = "Sistema"
            };
        }

        // ✅ MÉTODOS DE UTILIDAD

        /// <summary>
        /// Valida si la configuración es válida
        /// </summary>
        public bool ValidarConfiguracion()
        {
            try
            {
                if (string.IsNullOrEmpty(Puerto) ||
                    string.IsNullOrEmpty(Nombre) ||
                    BaudRate <= 0 ||
                    TimeoutLectura <= 0 ||
                    string.IsNullOrEmpty(PatronExtraccion))
                {
                    return false;
                }

                // Validar que el patrón regex es válido
                var regex = new Regex(PatronExtraccion);

                // Validar puerto COM
                if (!Puerto.StartsWith("COM") || Puerto.Length < 4)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene información de debug de la configuración
        /// </summary>
        public string ObtenerInfoDebug()
        {
            return $"{Nombre} - {Puerto}@{BaudRate} ({DataBits}-{Parity}-{StopBits})";
        }

        /// <summary>
        /// Obtiene información detallada de la configuración
        /// </summary>
        public string ObtenerInfoDetallada()
        {
            return $"📋 CONFIGURACIÓN DETALLADA\n" +
                   $"Nombre: {Nombre}\n" +
                   $"Puerto: {Puerto}\n" +
                   $"Velocidad: {BaudRate} bps\n" +
                   $"Formato: {DataBits} bits de datos, {Parity}, {StopBits} bit(s) de parada\n" +
                   $"Control de flujo: {Handshake}\n" +
                   $"Timeout: {TimeoutLectura} ms\n" +
                   $"Intervalo: {IntervaloLectura} ms\n" +
                   $"Unidad: {UnidadPeso}\n" +
                   $"Requiere solicitud: {(RequiereSolicitudPeso ? "Sí" : "No")}\n" +
                   $"Comando solicitar: '{ComandoSolicitarPeso}'\n" +
                   $"Comando tarar: '{ComandoTara}'\n" +
                   $"Patrón: {PatronExtraccion}\n" +
                   $"Terminador: {TerminadorLinea?.Replace("\r", "\\r").Replace("\n", "\\n")}";
        }

        /// <summary>
        /// Convierte terminadores de línea de texto a caracteres reales
        /// </summary>
        public string ObtenerTerminadorReal()
        {
            if (string.IsNullOrEmpty(TerminadorLinea))
                return "\r\n";

            return TerminadorLinea
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }

        /// <summary>
        /// Obtiene el comando como bytes si se especifica en formato especial
        /// </summary>
        public byte[] ObtenerComandoComoBytes()
        {
            if (string.IsNullOrEmpty(ComandoSolicitarPeso))
                return new byte[0];

            try
            {
                // Si el comando es un número (formato decimal)
                if (int.TryParse(ComandoSolicitarPeso, out int valorDecimal))
                {
                    return new byte[] { (byte)valorDecimal };
                }

                // Si el comando es hexadecimal (formato 0x50 o 50h)
                if (ComandoSolicitarPeso.StartsWith("0x") || ComandoSolicitarPeso.EndsWith("h"))
                {
                    string hexValue = ComandoSolicitarPeso.Replace("0x", "").Replace("h", "");
                    if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int valorHex))
                    {
                        return new byte[] { (byte)valorHex };
                    }
                }

                // Por defecto, convertir como texto ASCII
                return System.Text.Encoding.ASCII.GetBytes(ComandoSolicitarPeso);
            }
            catch
            {
                // En caso de error, devolver como ASCII
                return System.Text.Encoding.ASCII.GetBytes(ComandoSolicitarPeso);
            }
        }

        /// <summary>
        /// Crea una copia de la configuración
        /// </summary>
        public ConfiguracionBascula Clonar()
        {
            return new ConfiguracionBascula
            {
                Nombre = this.Nombre,
                Puerto = this.Puerto,
                BaudRate = this.BaudRate,
                DataBits = this.DataBits,
                Parity = this.Parity,
                StopBits = this.StopBits,
                Handshake = this.Handshake,
                TimeoutLectura = this.TimeoutLectura,
                IntervaloLectura = this.IntervaloLectura,
                UnidadPeso = this.UnidadPeso,
                RequiereSolicitudPeso = this.RequiereSolicitudPeso,
                ComandoSolicitarPeso = this.ComandoSolicitarPeso,
                ComandoTara = this.ComandoTara,
                PatronExtraccion = this.PatronExtraccion,
                TerminadorLinea = this.TerminadorLinea,
                UsuarioCreacion = this.UsuarioCreacion
            };
        }

        /// <summary>
        /// Verifica si es compatible con una configuración estándar
        /// </summary>
        public string DetectarTipoBascula()
        {
            if (ConfiguracionesCoinciden(this, ConfiguracionRhino()))
                return "RHINO BAR-8RS";

            if (ConfiguracionesCoinciden(this, ConfiguracionOhaus()))
                return "OHAUS";

            if (ConfiguracionesCoinciden(this, ConfiguracionMettler()))
                return "Mettler Toledo";

            if (ConfiguracionesCoinciden(this, ConfiguracionTorrey()))
                return "Torrey";

            if (ConfiguracionesCoinciden(this, ConfiguracionExcell()))
                return "EXCELL";

            if (ConfiguracionesCoinciden(this, ConfiguracionToledo()))
                return "Toledo";

            return "Personalizada";
        }

        /// <summary>
        /// Compara dos configuraciones
        /// </summary>
        private bool ConfiguracionesCoinciden(ConfiguracionBascula config1, ConfiguracionBascula config2)
        {
            return config1.BaudRate == config2.BaudRate &&
                   config1.DataBits == config2.DataBits &&
                   config1.Parity == config2.Parity &&
                   config1.StopBits == config2.StopBits &&
                   config1.ComandoSolicitarPeso == config2.ComandoSolicitarPeso &&
                   config1.PatronExtraccion == config2.PatronExtraccion;
        }

        /// <summary>
        /// Obtiene recomendaciones basadas en el tipo de báscula
        /// </summary>
        public string ObtenerRecomendaciones()
        {
            var tipo = DetectarTipoBascula();

            return tipo switch
            {
                "RHINO BAR-8RS" => "💡 Configuración optimizada para RHINO BAR-8RS.\n" +
                                  "• Asegúrese de que la báscula esté en modo RS232\n" +
                                  "• El comando 'P' solicita peso inmediato\n" +
                                  "• También acepta comandos 80 (decimal) o 50 (hex)",

                "OHAUS" => "💡 Configuración para básculas OHAUS de precisión.\n" +
                          "• Comando 'IP' solicita impresión inmediata\n" +
                          "• Configurar báscula en modo 'Continuous' o 'On Demand'\n" +
                          "• Verificar configuración de interface en menú báscula",

                "Mettler Toledo" => "💡 Configuración para Mettler Toledo.\n" +
                                   "• Comando 'S' solicita peso estable\n" +
                                   "• Configurar interface en modo MT-SICS\n" +
                                   "• Verificar que esté habilitada la comunicación serie",

                "Torrey" => "💡 Configuración para básculas Torrey.\n" +
                           "• Comando 'W' es estándar para solicitar peso\n" +
                           "• Muy común en México y Latinoamérica\n" +
                           "• Configurar en modo 'Continuous output'",

                "EXCELL" => "💡 Configuración para básculas EXCELL.\n" +
                           "• Protocolo estándar con formato ST,GS\n" +
                           "• Comando 'P' para peso, 'Z' para tarar\n" +
                           "• Básculas económicas pero funcionales",

                "Toledo" => "💡 Configuración para básculas Toledo.\n" +
                           "• Protocolo estándar con formato ST,GS\n" +
                           "• Configurar interface en modo 'Continuous'\n" +
                           "• Verificar configuración de decimales",

                _ => "💡 Configuración personalizada.\n" +
                     "• Verifique el manual de su báscula\n" +
                     "• Ajuste el patrón de extracción según la respuesta\n" +
                     "• Use el botón 'Diagnosticar' para más información"
            };
        }

        internal object ObtenerComandoComoBytes(string comandoTara)
        {
            throw new NotImplementedException();
        }
    }

    // ✅ CLASE PARA RESULTADO DE PRUEBA DE CONEXIÓN
    public class ResultadoPruebaConexion
    {
        public bool Exitoso { get; set; }
        public string MensajeError { get; set; }
        public string DatosRecibidos { get; set; }
        public decimal? PesoDetectado { get; set; }
        public TimeSpan TiempoRespuesta { get; set; }
        public string InformacionAdicional { get; set; }

        public static ResultadoPruebaConexion Exito(string datos = null, decimal? peso = null, TimeSpan? tiempo = null)
        {
            return new ResultadoPruebaConexion
            {
                Exitoso = true,
                DatosRecibidos = datos,
                PesoDetectado = peso,
                TiempoRespuesta = tiempo ?? TimeSpan.Zero
            };
        }

        public static ResultadoPruebaConexion Error(string mensaje, string info = null)
        {
            return new ResultadoPruebaConexion
            {
                Exitoso = false,
                MensajeError = mensaje,
                InformacionAdicional = info
            };
        }
    }
}