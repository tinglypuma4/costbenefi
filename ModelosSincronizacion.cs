using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace costbenefi.Services
{
    #region Modelos de Autenticación

    public class SolicitudAutenticacionTerminal
    {
        [Required]
        public string TerminalId { get; set; } = "";

        [Required]
        public string ClaveTerminal { get; set; } = "";

        public string Version { get; set; } = "1.0.0";
        public string IP { get; set; } = "";
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
    }

    public class RespuestaAutenticacion
    {
        public bool Autorizado { get; set; }
        public string Token { get; set; } = "";
        public DateTime FechaExpiracion { get; set; }
        public string ServidorId { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public ConfiguracionTerminalSync Configuracion { get; set; } = new();
    }

    public class ConfiguracionTerminalSync
    {
        public int IntervaloSyncSegundos { get; set; } = 30;
        public bool SincronizacionAutomatica { get; set; } = true;
        public bool PermiteVentasSinStock { get; set; } = false;
        public bool RequiereAutorizacionDescuentos { get; set; } = true;
        public decimal LimiteDescuentoSinAutorizacion { get; set; } = 0;
        public List<string> FuncionesHabilitadas { get; set; } = new();
    }

    #endregion

    #region Modelos de Sincronización

    public class SolicitudCambios
    {
        [Required]
        public string TerminalId { get; set; } = "";

        public DateTime UltimaSync { get; set; }
        public List<string> TiposRequeridos { get; set; } = new();
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
    }

    public class CambiosServidor
    {
        public DateTime FechaGeneracion { get; set; }
        public string ServidorId { get; set; } = "";
        public List<ProductoSync> Productos { get; set; } = new();
        public List<ServicioSync> Servicios { get; set; } = new();
        public List<PromocionSync> Promociones { get; set; } = new();
        public List<ConfiguracionSync> Configuraciones { get; set; } = new();

        public int TotalCambios => Productos.Count + Servicios.Count + Promociones.Count + Configuraciones.Count;
    }

    public class CambiosLocales
    {
        [Required]
        public string TerminalId { get; set; } = "";

        public DateTime FechaGeneracion { get; set; }
        public List<VentaSync> Ventas { get; set; } = new();
        public List<MovimientoStockSync> MovimientosStock { get; set; } = new();
        public List<EventoTerminalSync> Eventos { get; set; } = new();

        public int TotalCambios => Ventas.Count + MovimientosStock.Count + Eventos.Count;
    }

    public class RespuestaSincronizacion
    {
        public bool Exitosa { get; set; }
        public string Mensaje { get; set; } = "";
        public DateTime FechaProcesamiento { get; set; }
        public int CambiosProcesados { get; set; }
        public int ErroresEncontrados { get; set; }
        public List<ErrorSincronizacion> Errores { get; set; } = new();
    }

    #endregion

    #region Modelos de Entidades Sync

    public class ProductoSync
    {
        public int Id { get; set; }
        public string CodigoBarras { get; set; } = "";
        public string NombreArticulo { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal PrecioUnitario { get; set; }
        public decimal PrecioCompra { get; set; }
        public int StockTotal { get; set; }
        public int StockMinimo { get; set; }
        public string UnidadMedida { get; set; } = "";
        public bool ActivoParaVenta { get; set; }
        public bool RequiereBascula { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public bool Eliminado { get; set; }

        // Información adicional para POS
        public string ImagenUrl { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public bool PermiteDescuento { get; set; } = true;
        public decimal MaximoDescuento { get; set; }
    }

    public class ServicioSync
    {
        public int Id { get; set; }
        public string NombreServicio { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Precio { get; set; }
        public int DuracionMinutos { get; set; }
        public bool Activo { get; set; }
        public bool IntegradoPOS { get; set; }
        public int PrioridadPOS { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public bool Eliminado { get; set; }

        // Información adicional
        public string ImagenUrl { get; set; } = "";
        public bool RequiereCita { get; set; }
        public string CategoriaNombre { get; set; } = "";
    }

    public class PromocionSync
    {
        public int Id { get; set; }
        public string NombrePromocion { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string TipoPromocion { get; set; } = "";
        public decimal ValorDescuento { get; set; }
        public bool EsPorcentaje { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public bool Eliminado { get; set; }

        // Condiciones
        public decimal MontoMinimoCompra { get; set; }
        public int CantidadMinimaProductos { get; set; }
        public List<int> ProductosAplicables { get; set; } = new();
        public List<int> ServiciosAplicables { get; set; } = new();
    }

    public class ConfiguracionSync
    {
        public string Clave { get; set; } = "";
        public string Valor { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime FechaActualizacion { get; set; }
        public bool Eliminado { get; set; }
    }

    #endregion

    #region Modelos de Transacciones

    public class VentaSync
    {
        public string NumeroTicket { get; set; } = "";
        public string TerminalOrigen { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string FormaPago { get; set; } = "";
        public decimal Total { get; set; }
        public decimal MontoEfectivo { get; set; }
        public decimal MontoTarjeta { get; set; }
        public decimal MontoTransferencia { get; set; }
        public decimal ComisionTotal { get; set; }
        public DateTime FechaVenta { get; set; }

        // Información de descuentos
        public bool TieneDescuentosAplicados { get; set; }
        public decimal TotalDescuentosAplicados { get; set; }
        public string UsuarioAutorizadorDescuento { get; set; } = "";
        public string MotivoDescuentoGeneral { get; set; } = "";

        public List<DetalleVentaSync> Detalles { get; set; } = new();
    }

    public class DetalleVentaSync
    {
        public int? RawMaterialId { get; set; }
        public int? ServicioVentaId { get; set; }
        public string NombreProducto { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal { get; set; }
        public string UnidadMedida { get; set; } = "";

        // Información de descuentos
        public decimal PrecioOriginal { get; set; }
        public decimal DescuentoUnitario { get; set; }
        public bool TieneDescuentoManual { get; set; }
        public string MotivoDescuentoDetalle { get; set; } = "";
    }

    public class MovimientoStockSync
    {
        public int ProductoId { get; set; }
        public string TipoMovimiento { get; set; } = "";
        public decimal CantidadMovida { get; set; }
        public string Motivo { get; set; } = "";
        public string Usuario { get; set; } = "";
        public DateTime FechaMovimiento { get; set; }
        public string TerminalOrigen { get; set; } = "";
    }

    public class EventoTerminalSync
    {
        public string TerminalId { get; set; } = "";
        public string TipoEvento { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Usuario { get; set; } = "";
        public DateTime FechaEvento { get; set; }
        public string DatosAdicionales { get; set; } = "";
    }

    public class EstadoTerminalSync
    {
        public string TerminalId { get; set; } = "";
        public DateTime UltimaActividad { get; set; }
        public string UsuarioActual { get; set; } = "";
        public bool EscaneadorConectado { get; set; }
        public bool ImpresoraConectada { get; set; }
        public bool BasculaConectada { get; set; }
        public int VentasDelDia { get; set; }
        public decimal TotalVentasDelDia { get; set; }
        public string Version { get; set; } = "";
        public string IP { get; set; } = "";
    }

    #endregion

    #region Modelos de Error

    public class ErrorSincronizacion
    {
        public string TipoEntidad { get; set; } = "";
        public string IdEntidad { get; set; } = "";
        public string Error { get; set; } = "";
        public string DetalleError { get; set; } = "";
        public DateTime FechaError { get; set; } = DateTime.Now;
    }

    #endregion
}