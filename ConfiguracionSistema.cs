using System;

namespace costbenefi.Services
{
    /// <summary>
    /// Configuración singleton del sistema para sincronización
    /// </summary>
    public class ConfiguracionSistema
    {
        private static ConfiguracionSistema? _instance;
        private static readonly object _lock = new object();

        public static ConfiguracionSistema Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ConfiguracionSistema();
                    }
                }
                return _instance;
            }
        }

        private ConfiguracionSistema()
        {
            // Configuración por defecto
            Tipo = TipoInstalacion.Servidor;
            ServidorIP = "127.0.0.1";
            ServidorPuerto = 5000;
            NombreTerminal = Environment.MachineName;
            SincronizacionActiva = true;
            IntervaloSincronizacionMinutos = 5;
        }

        // Propiedades de configuración
        public TipoInstalacion Tipo { get; set; }
        public string ServidorIP { get; set; }
        public int ServidorPuerto { get; set; }
        public string NombreTerminal { get; set; }
        public bool SincronizacionActiva { get; set; }
        public int IntervaloSincronizacionMinutos { get; set; }

        // URL completa del servidor
        public string UrlServidor => $"http://{ServidorIP}:{ServidorPuerto}";

        /// <summary>
        /// Configura el sistema como servidor
        /// </summary>
        public void ConfigurarComoServidor(string ip = "0.0.0.0", int puerto = 5000)
        {
            Tipo = TipoInstalacion.Servidor;
            ServidorIP = ip;
            ServidorPuerto = puerto;
            SincronizacionActiva = true;
        }

        /// <summary>
        /// Configura el sistema como terminal
        /// </summary>
        public void ConfigurarComoTerminal(string servidorIp, int servidorPuerto = 5000, string nombreTerminal = "")
        {
            Tipo = TipoInstalacion.Terminal;
            ServidorIP = servidorIp;
            ServidorPuerto = servidorPuerto;
            NombreTerminal = string.IsNullOrEmpty(nombreTerminal) ? Environment.MachineName : nombreTerminal;
            SincronizacionActiva = true;
        }
    }

    public enum TipoInstalacion
    {
        Servidor,
        Terminal,
        Standalone
    }
}