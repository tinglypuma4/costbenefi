using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace costbenefi.Models
{
    /// <summary>
    /// Entidad para guardar la configuración de báscula en la base de datos
    /// </summary>
    public class ConfiguracionBascula
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = "";

        [Required]
        [StringLength(10)]
        public string Puerto { get; set; } = "COM1";

        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public int Paridad { get; set; } = 0; // None = 0, Odd = 1, Even = 2
        public int StopBits { get; set; } = 1; // One = 1, Two = 2
        public int ControlFlujo { get; set; } = 0; // None = 0, XOnXOff = 1, RequestToSend = 2

        public int TimeoutLectura { get; set; } = 1000;
        public int TimeoutEscritura { get; set; } = 1000;
        public int IntervaloLectura { get; set; } = 1000;

        [StringLength(10)]
        public string UnidadPeso { get; set; } = "kg";

        [StringLength(5)]
        public string TerminadorComando { get; set; } = "\r\n";

        public bool RequiereSolicitudPeso { get; set; } = false;

        [StringLength(20)]
        public string ComandoSolicitarPeso { get; set; } = "P";

        [StringLength(20)]
        public string ComandoTara { get; set; } = "T";

        [StringLength(50)]
        public string ComandoInicializacion { get; set; } = "";

        [StringLength(200)]
        public string PatronExtraccion { get; set; } = "";

        public bool EsConfiguracionActiva { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // Propiedades calculadas
        [NotMapped]
        public string DescripcionCompleta => $"{Nombre} - {Puerto} @ {BaudRate}";

        [NotMapped]
        public bool EstaConfigurada => !string.IsNullOrEmpty(Puerto) && !string.IsNullOrEmpty(Nombre);

        // Métodos de configuración predefinida
        public static ConfiguracionBascula ConfiguracionOhaus()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Báscula OHAUS",
                BaudRate = 2400,
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "P",
                ComandoTara = "T",
                PatronExtraccion = @"(\d+\.?\d*)\s*g",
                UnidadPeso = "g"
            };
        }

        public static ConfiguracionBascula ConfiguracionMettler()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Báscula Mettler Toledo",
                BaudRate = 9600,
                RequiereSolicitudPeso = true,
                ComandoSolicitarPeso = "S",
                ComandoTara = "T",
                PatronExtraccion = @"S\s+S\s+(\d+\.?\d*)\s*g",
                UnidadPeso = "g"
            };
        }

        public static ConfiguracionBascula ConfiguracionGenerica()
        {
            return new ConfiguracionBascula
            {
                Nombre = "Báscula Genérica",
                BaudRate = 9600,
                RequiereSolicitudPeso = false,
                PatronExtraccion = @"(\d+\.?\d*)",
                UnidadPeso = "kg"
            };
        }
    }
}