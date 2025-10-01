using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using costbenefi.Data;
using costbenefi.Models;
using costbenefi.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace costbenefi.Services.Sync.Controllers
{
    /// <summary>
    /// Controlador API para sincronización con terminales
    /// Proporciona endpoints REST para comunicación servidor-terminal
    /// </summary>
    [ApiController]
    [Route("api/sync")]
    [Produces("application/json")]
    public class SyncApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SyncApiController> _logger;
        private readonly ConfiguracionSistema _config;

        public SyncApiController(AppDbContext context, ILogger<SyncApiController> logger)
        {
            _context = context;
            _logger = logger;
            _config = ConfiguracionSistema.Instance;
        }

        #region Endpoints de Conectividad

        /// <summary>
        /// Ping para verificar conectividad
        /// GET /api/sync/ping
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                servidor = Environment.MachineName,
                timestamp = DateTime.Now,
                version = "1.0.0",
                status = "online"
            });
        }

        /// <summary>
        /// Health check completo del servidor
        /// GET /api/sync/health
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var estadoServidor = new
                {
                    servidor = Environment.MachineName,
                    timestamp = DateTime.Now,
                    version = "1.0.0",
                    baseDatos = await VerificarBaseDatos(),
                    estadisticas = await ObtenerEstadisticasServidor(),
                    configuracion = new
                    {
                        tipo = _config.Tipo.ToString(),
                        puerto = _config.ServidorPuerto,
                        ip = _config.ServidorIP
                    }
                };

                return Ok(estadoServidor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en health check");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        #endregion

        #region Endpoints de Autenticación

        /// <summary>
        /// Autentica un terminal
        /// POST /api/sync/auth
        /// </summary>
        [HttpPost("auth")]
        public async Task<IActionResult> AutenticarTerminal([FromBody] SolicitudAutenticacionTerminal solicitud)
        {
            try
            {
                _logger.LogInformation($"Solicitud de autenticación del terminal: {solicitud.TerminalId}");

                // Validar solicitud
                if (!ModelState.IsValid)
                {
                    return BadRequest(new RespuestaAutenticacion
                    {
                        Autorizado = false,
                        Mensaje = "Datos de solicitud inválidos"
                    });
                }

                // Verificar terminal (por simplicidad, permitir cualquier terminal por ahora)
                var autorizado = true; // TODO: Implementar lógica de autorización real

                if (!autorizado)
                {
                    return Unauthorized(new RespuestaAutenticacion
                    {
                        Autorizado = false,
                        Mensaje = "Terminal no autorizado"
                    });
                }

                // Generar token (simplificado)
                var token = GenerarTokenTerminal(solicitud.TerminalId);

                var respuesta = new RespuestaAutenticacion
                {
                    Autorizado = true,
                    Token = token,
                    FechaExpiracion = DateTime.Now.AddHours(24),
                    ServidorId = Environment.MachineName,
                    Mensaje = "Terminal autenticado exitosamente",
                    Configuracion = new ConfiguracionTerminalSync
                    {
                        IntervaloSyncSegundos = 30,
                        SincronizacionAutomatica = true,
                        PermiteVentasSinStock = false,
                        RequiereAutorizacionDescuentos = true,
                        LimiteDescuentoSinAutorizacion = 0,
                        FuncionesHabilitadas = new List<string> { "pos", "ventas", "consultas" }
                    }
                };

                _logger.LogInformation($"Terminal {solicitud.TerminalId} autenticado exitosamente");
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error autenticando terminal {solicitud?.TerminalId}");
                return StatusCode(500, new RespuestaAutenticacion
                {
                    Autorizado = false,
                    Mensaje = "Error interno del servidor"
                });
            }
        }

        #endregion

        #region Endpoints de Sincronización

        /// <summary>
        /// Obtiene cambios para un terminal
        /// POST /api/sync/cambios
        /// </summary>
        [HttpPost("cambios")]
        [Authorize] // TODO: Implementar middleware de autorización
        public async Task<IActionResult> ObtenerCambios([FromBody] SolicitudCambios solicitud)
        {
            try
            {
                _logger.LogInformation($"Solicitud de cambios del terminal: {solicitud.TerminalId} desde {solicitud.UltimaSync}");

                var cambios = new CambiosServidor
                {
                    FechaGeneracion = DateTime.Now,
                    ServidorId = Environment.MachineName
                };

                // Obtener productos actualizados
                if (solicitud.TiposRequeridos.Contains("productos"))
                {
                    cambios.Productos = await ObtenerProductosActualizados(solicitud.UltimaSync);
                }

                // Obtener servicios actualizados
                if (solicitud.TiposRequeridos.Contains("servicios"))
                {
                    cambios.Servicios = await ObtenerServiciosActualizados(solicitud.UltimaSync);
                }

                // Obtener promociones actualizadas
                if (solicitud.TiposRequeridos.Contains("promociones"))
                {
                    cambios.Promociones = await ObtenerPromocionesActualizadas(solicitud.UltimaSync);
                }

                // Obtener configuraciones actualizadas
                if (solicitud.TiposRequeridos.Contains("configuraciones"))
                {
                    cambios.Configuraciones = await ObtenerConfiguracionesActualizadas(solicitud.UltimaSync);
                }

                _logger.LogInformation($"Enviando {cambios.TotalCambios} cambios al terminal {solicitud.TerminalId}");
                return Ok(cambios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo cambios para terminal {solicitud?.TerminalId}");
                return StatusCode(500, new { error = "Error obteniendo cambios" });
            }
        }

        /// <summary>
        /// Recibe cambios desde un terminal
        /// POST /api/sync/recibir-cambios
        /// </summary>
        [HttpPost("recibir-cambios")]
        [Authorize]
        public async Task<IActionResult> RecibirCambios([FromBody] CambiosLocales cambios)
        {
            try
            {
                _logger.LogInformation($"Recibiendo {cambios.TotalCambios} cambios del terminal: {cambios.TerminalId}");

                var resultado = new RespuestaSincronizacion
                {
                    FechaProcesamiento = DateTime.Now
                };

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Procesar ventas
                    if (cambios.Ventas?.Any() == true)
                    {
                        var ventasProcesadas = await ProcesarVentasTerminal(cambios.Ventas);
                        resultado.CambiosProcesados += ventasProcesadas;
                    }

                    // Procesar movimientos de stock
                    if (cambios.MovimientosStock?.Any() == true)
                    {
                        var movimientosProcesados = await ProcesarMovimientosStock(cambios.MovimientosStock);
                        resultado.CambiosProcesados += movimientosProcesados;
                    }

                    // Procesar eventos
                    if (cambios.Eventos?.Any() == true)
                    {
                        var eventosProcesados = await ProcesarEventosTerminal(cambios.Eventos);
                        resultado.CambiosProcesados += eventosProcesados;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    resultado.Exitosa = true;
                    resultado.Mensaje = $"Se procesaron {resultado.CambiosProcesados} cambios exitosamente";

                    _logger.LogInformation($"Cambios del terminal {cambios.TerminalId} procesados exitosamente");
                    return Ok(resultado);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error procesando cambios del terminal {cambios?.TerminalId}");

                var error = new RespuestaSincronizacion
                {
                    Exitosa = false,
                    Mensaje = "Error procesando cambios",
                    ErroresEncontrados = 1,
                    Errores = new List<ErrorSincronizacion>
                    {
                        new ErrorSincronizacion
                        {
                            TipoEntidad = "general",
                            Error = ex.Message
                        }
                    }
                };

                return StatusCode(500, error);
            }
        }

        #endregion

        #region Endpoints de Consulta

        /// <summary>
        /// Obtiene estadísticas del servidor
        /// GET /api/sync/estadisticas
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            try
            {
                var estadisticas = await ObtenerEstadisticasServidor();
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas");
                return StatusCode(500, new { error = "Error obteniendo estadísticas" });
            }
        }

        /// <summary>
        /// Obtiene lista de terminales conectados
        /// GET /api/sync/terminales
        /// </summary>
        [HttpGet("terminales")]
        public async Task<IActionResult> ObtenerTerminales()
        {
            try
            {
                // TODO: Implementar registro de terminales activos
                var terminales = new List<object>
                {
                    new
                    {
                        id = "terminal-ejemplo",
                        ultimaActividad = DateTime.Now.AddMinutes(-5),
                        estado = "activo",
                        version = "1.0.0"
                    }
                };

                return Ok(terminales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo terminales");
                return StatusCode(500, new { error = "Error obteniendo terminales" });
            }
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Verifica el estado de la base de datos
        /// </summary>
        private async Task<object> VerificarBaseDatos()
        {
            try
            {
                var conteoProductos = await _context.RawMaterials.CountAsync();
                var conteoVentas = await _context.Ventas.CountAsync();

                return new
                {
                    estado = "disponible",
                    productos = conteoProductos,
                    ventas = conteoVentas,
                    ultimaConexion = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    estado = "error",
                    error = ex.Message
                };
            }
        }

        /// <summary>
        /// Obtiene estadísticas generales del servidor
        /// </summary>
        private async Task<object> ObtenerEstadisticasServidor()
        {
            try
            {
                var hoy = DateTime.Today;
                var ventasHoy = await _context.Ventas
                    .Where(v => v.FechaVenta >= hoy && v.Estado == "Completada")
                    .CountAsync();

                var totalVentasHoy = await _context.Ventas
                    .Where(v => v.FechaVenta >= hoy && v.Estado == "Completada")
                    .SumAsync(v => v.Total);

                return new
                {
                    ventasHoy,
                    totalVentasHoy,
                    productosActivos = await _context.RawMaterials.CountAsync(p => p.ActivoParaVenta),
                    serviciosActivos = await _context.ServiciosVenta.CountAsync(s => s.Activo),
                    promocionesVigentes = await _context.PromocionesVenta.CountAsync(p => p.Activa)
                };
            }
            catch
            {
                return new { error = "No se pudieron obtener estadísticas" };
            }
        }

        /// <summary>
        /// Genera un token simple para el terminal
        /// </summary>
        private string GenerarTokenTerminal(string terminalId)
        {
            // Implementación simplificada - en producción usar JWT o similar
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hash = $"{terminalId}-{timestamp}".GetHashCode();
            return $"terminal_{Math.Abs(hash)}_{timestamp}";
        }

        /// <summary>
        /// Obtiene productos actualizados desde una fecha
        /// </summary>
        private async Task<List<ProductoSync>> ObtenerProductosActualizados(DateTime desde)
        {
            var productos = await _context.RawMaterials
                .Where(p => p.FechaActualizacion > desde && p.ActivoParaVenta)
                .OrderBy(p => p.FechaActualizacion)
                .ToListAsync();

            return productos.Select(p => p.ToProductoSync()).ToList();
        }

        /// <summary>
        /// Obtiene servicios actualizados desde una fecha
        /// </summary>
        private async Task<List<ServicioSync>> ObtenerServiciosActualizados(DateTime desde)
        {
            var servicios = await _context.ServiciosVenta
                .Where(s => s.FechaActualizacion > desde && s.IntegradoPOS)
                .OrderBy(s => s.FechaActualizacion)
                .ToListAsync();

            return servicios.Select(s => s.ToServicioSync()).ToList();
        }

        /// <summary>
        /// Obtiene promociones actualizadas desde una fecha
        /// </summary>
        private async Task<List<PromocionSync>> ObtenerPromocionesActualizadas(DateTime desde)
        {
            var promociones = await _context.PromocionesVenta
                .Where(p => p.FechaActualizacion > desde && p.Activa)
                .OrderBy(p => p.FechaActualizacion)
                .ToListAsync();

            return promociones.Select(p => p.ToPromocionSync()).ToList();
        }

        /// <summary>
        /// Obtiene configuraciones actualizadas desde una fecha
        /// </summary>
        private async Task<List<ConfiguracionSync>> ObtenerConfiguracionesActualizadas(DateTime desde)
        {
            // TODO: Implementar tabla de configuraciones si es necesario
            return new List<ConfiguracionSync>();
        }

        /// <summary>
        /// Procesa ventas recibidas desde un terminal
        /// </summary>
        private async Task<int> ProcesarVentasTerminal(List<VentaSync> ventas)
        {
            int procesadas = 0;

            foreach (var ventaSync in ventas)
            {
                try
                {
                    // Verificar si la venta ya existe (evitar duplicados)
                    var numeroTicket = long.Parse(ventaSync.NumeroTicket);
                    var existeVenta = await _context.Ventas.AnyAsync(v => v.NumeroTicket == numeroTicket);

                    if (existeVenta)
                    {
                        _logger.LogWarning($"Venta con ticket {ventaSync.NumeroTicket} ya existe, omitiendo");
                        continue;
                    }

                    // Crear nueva venta
                    var venta = new Venta
                    {
                        NumeroTicket = long.Parse(ventaSync.NumeroTicket),
                        Cliente = ventaSync.Cliente,
                        Usuario = ventaSync.Usuario,
                        FormaPago = ventaSync.FormaPago,
                        Total = ventaSync.Total,
                        MontoEfectivo = ventaSync.MontoEfectivo,
                        MontoTarjeta = ventaSync.MontoTarjeta,
                        MontoTransferencia = ventaSync.MontoTransferencia,
                        ComisionTotal = ventaSync.ComisionTotal,
                        FechaVenta = ventaSync.FechaVenta,
                        Estado = "Completada",

                        // Información de descuentos
                        TieneDescuentosAplicados = ventaSync.TieneDescuentosAplicados,
                        TotalDescuentosAplicados = ventaSync.TotalDescuentosAplicados,
                        UsuarioAutorizadorDescuento = ventaSync.UsuarioAutorizadorDescuento,
                        MotivoDescuentoGeneral = ventaSync.MotivoDescuentoGeneral
                    };

                    // Agregar detalles
                    foreach (var detalleSync in ventaSync.Detalles)
                    {
                        var detalle = new DetalleVenta
                        {
                            RawMaterialId = detalleSync.RawMaterialId,
                            ServicioVentaId = detalleSync.ServicioVentaId,
                            NombreProducto = detalleSync.NombreProducto,
                            Cantidad = detalleSync.Cantidad,
                            PrecioUnitario = detalleSync.PrecioUnitario,
                            SubTotal = detalleSync.SubTotal,
                            UnidadMedida = detalleSync.UnidadMedida,

                            // Información de descuentos
                            PrecioOriginal = detalleSync.PrecioOriginal,
                            DescuentoUnitario = detalleSync.DescuentoUnitario,
                            TieneDescuentoManual = detalleSync.TieneDescuentoManual,
                            MotivoDescuentoDetalle = detalleSync.MotivoDescuentoDetalle
                        };

                        venta.AgregarDetalle(detalle);
                    }

                    _context.Ventas.Add(venta);
                    procesadas++;

                    _logger.LogInformation($"Venta {ventaSync.NumeroTicket} del terminal {ventaSync.TerminalOrigen} agregada");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error procesando venta {ventaSync.NumeroTicket}");
                }
            }

            return procesadas;
        }

        /// <summary>
        /// Procesa movimientos de stock desde terminales
        /// </summary>
        private async Task<int> ProcesarMovimientosStock(List<MovimientoStockSync> movimientos)
        {
            int procesados = 0;

            foreach (var movimientoSync in movimientos)
            {
                try
                {
                    var movimiento = new Movimiento
                    {
                        RawMaterialId = movimientoSync.ProductoId,
                        TipoMovimiento = movimientoSync.TipoMovimiento,
                        Cantidad = movimientoSync.CantidadMovida,
                        Motivo = $"Terminal {movimientoSync.TerminalOrigen}: {movimientoSync.Motivo}",
                        Usuario = movimientoSync.Usuario,
                        FechaMovimiento = movimientoSync.FechaMovimiento
                    };

                    _context.Movimientos.Add(movimiento);
                    procesados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error procesando movimiento de stock para producto {movimientoSync.ProductoId}");
                }
            }

            return procesados;
        }

        /// <summary>
        /// Procesa eventos de terminales para auditoría
        /// </summary>
        private async Task<int> ProcesarEventosTerminal(List<EventoTerminalSync> eventos)
        {
            // TODO: Implementar tabla de eventos/auditoría si es necesario
            // Por ahora solo logear los eventos

            foreach (var evento in eventos)
            {
                _logger.LogInformation($"Evento de terminal {evento.TerminalId}: {evento.TipoEvento} - {evento.Descripcion}");
            }

            return eventos.Count;
        }

        #endregion
    }
}