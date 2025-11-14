using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using costbenefi.Data;
using costbenefi.Models;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio para gestión completa de cortes de caja
    /// Integrado con el sistema POS existente
    /// </summary>
    public class CorteCajaService
    {
        private readonly AppDbContext _context;

        public CorteCajaService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ===== MÉTODOS PRINCIPALES =====

        /// <summary>
        /// Verifica si ya existe un corte para la fecha especificada
        /// </summary>
        public async Task<bool> ExisteCorteDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            return await _context.CortesCaja
                .AnyAsync(c => c.FechaCorte == fechaSolo);
        }

        /// <summary>
        /// Obtiene el corte del día si existe
        /// </summary>
        public async Task<CorteCaja?> ObtenerCorteDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            return await _context.CortesCaja
                .Include(c => c.VentasDelDia)
                .FirstOrDefaultAsync(c => c.FechaCorte == fechaSolo);
        }

        /// <summary>
        /// Inicia un nuevo corte de caja calculando automáticamente los totales
        /// </summary>
        /// <summary>
        /// Inicia un nuevo corte de caja calculando automáticamente los totales
        /// </summary>
        public async Task<CorteCaja> IniciarCorteDelDiaAsync(DateTime fecha, string usuario = null)
        {
            var fechaSolo = fecha.Date;

            // Validar que no exista un corte del mismo día
            if (await ExisteCorteDelDiaAsync(fechaSolo))
            {
                throw new InvalidOperationException($"Ya existe un corte para el día {fechaSolo:dd/MM/yyyy}");
            }

            // Obtener ventas del día
            var ventasDelDia = await _context.GetVentasDelDia(fechaSolo)
                .Include(v => v.DetallesVenta)
                .ToListAsync();

            // ✅ OBTENER USUARIO AUTENTICADO CON ROL Y NOMBRE
            var usuarioActual = UserService.UsuarioActual;
            var usuarioCorte = usuarioActual != null
                ? $"{usuarioActual.Rol} - {usuarioActual.NombreCompleto}"
                : usuario ?? Environment.UserName;

            // Crear nuevo corte
            var nuevoCorte = new CorteCaja
            {
                FechaCorte = fechaSolo,
                FechaHoraCorte = DateTime.Now,
                UsuarioCorte = usuarioCorte, // ✅ Ejemplo: "Cajero - Juan Pérez"
                Estado = "Pendiente"
            };

            // Calcular totales automáticamente
            nuevoCorte.CalcularTotalesAutomaticos(ventasDelDia);

            // ✅ CALCULAR GASTOS DEL DÍA
            nuevoCorte.GastosTotalesCalculados = await CalcularGastosDelDiaAsync(fechaSolo);

            // Obtener fondo de caja del día anterior
            var fondoAnterior = await ObtenerFondoCajaAnteriorAsync(fechaSolo);
            nuevoCorte.FondoCajaInicial = fondoAnterior;

            return nuevoCorte;
        }
        /// <summary>
        /// Procesa y completa un corte de caja
        /// </summary>
        public async Task<CorteCaja> CompletarCorteAsync(CorteCaja corte, decimal efectivoContado,
            decimal fondoCajaSiguiente, string observaciones = "", bool realizarDeposito = false,
            decimal montoDeposito = 0, string referenciaDeposito = "")
        {
            // Validaciones básicas
            if (corte == null)
                throw new ArgumentNullException(nameof(corte));

            if (efectivoContado < 0)
                throw new ArgumentException("El efectivo contado no puede ser negativo");

            if (fondoCajaSiguiente < 0)
                throw new ArgumentException("El fondo de caja para el día siguiente no puede ser negativo");

            // Establecer conteo físico
            corte.EstablecerConteoFisico(efectivoContado, corte.FondoCajaInicial);
            corte.FondoCajaSiguiente = fondoCajaSiguiente;

            // Información del depósito
            if (realizarDeposito)
            {
                corte.DepositoRealizado = true;
                corte.MontoDepositado = montoDeposito > 0 ? montoDeposito : corte.EfectivoParaDepositar;
                corte.ReferenciaDeposito = referenciaDeposito;
            }

            // Completar corte
            corte.CompletarCorte(observaciones, realizarDeposito, referenciaDeposito, corte.MontoDepositado);

            // Guardar en base de datos
            if (corte.Id == 0)
            {
                _context.CortesCaja.Add(corte);
            }
            else
            {
                _context.CortesCaja.Update(corte);
            }

            await _context.SaveChangesAsync();

            return corte;
        }

        /// <summary>
        /// Cancela un corte de caja
        /// </summary>
        public async Task<bool> CancelarCorteAsync(int corteId, string motivo, string usuario)
        {
            var corte = await _context.CortesCaja.FindAsync(corteId);
            if (corte == null)
                return false;

            // Solo se pueden cancelar cortes pendientes
            if (corte.Estado != "Pendiente")
                throw new InvalidOperationException("Solo se pueden cancelar cortes pendientes");

            corte.CancelarCorte($"{motivo} - Cancelado por {usuario}");
            await _context.SaveChangesAsync();

            return true;
        }

        // ===== CONSULTAS Y REPORTES =====

        /// <summary>
        /// Obtiene estadísticas rápidas del día para el dashboard POS
        /// </summary>
        public async Task<dynamic> ObtenerEstadisticasDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            var ventasDelDia = await _context.GetVentasDelDia(fechaSolo).ToListAsync();

            // Calcular totales
            var totalVentas = ventasDelDia.Sum(v => v.Total);
            var cantidadTickets = ventasDelDia.Count;
            var efectivoTotal = ventasDelDia.Sum(v => v.MontoEfectivo);
            var tarjetaTotal = ventasDelDia.Sum(v => v.MontoTarjeta);
            var transferenciaTotal = ventasDelDia.Sum(v => v.MontoTransferencia);
            var comisionesTotal = ventasDelDia.Sum(v => v.ComisionTotal);
            var gananciaTotal = ventasDelDia.Sum(v => v.GananciaNeta);

            // Verificar si existe corte
            var existeCorte = await ExisteCorteDelDiaAsync(fechaSolo);

            return new
            {
                Fecha = fechaSolo,
                TotalVentas = totalVentas,
                CantidadTickets = cantidadTickets,
                EfectivoTotal = efectivoTotal,
                TarjetaTotal = tarjetaTotal,
                TransferenciaTotal = transferenciaTotal,
                ComisionesTotal = comisionesTotal,
                GananciaTotal = gananciaTotal,
                ExisteCorte = existeCorte,
                PuedeHacerCorte = !existeCorte && cantidadTickets > 0
            };
        }

        /// <summary>
        /// Obtiene el historial de cortes de caja
        /// </summary>
        public async Task<List<CorteCaja>> ObtenerHistorialCortesAsync(DateTime? desde = null, DateTime? hasta = null, int take = 50)
        {
            var query = _context.CortesCaja.AsQueryable();

            if (desde.HasValue)
                query = query.Where(c => c.FechaCorte >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(c => c.FechaCorte <= hasta.Value.Date);

            return await query
                .OrderByDescending(c => c.FechaCorte)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene estadísticas de cortes para análisis
        /// </summary>
        public async Task<dynamic> ObtenerAnalisisCortesAsync(DateTime desde, DateTime hasta)
        {
            var cortes = await _context.CortesCaja
                .Where(c => c.FechaCorte >= desde.Date && c.FechaCorte <= hasta.Date && c.Estado == "Completado")
                .ToListAsync();

            if (!cortes.Any())
            {
                return new
                {
                    Periodo = $"{desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}",
                    CantidadCortes = 0,
                    TotalVentas = 0m,
                    TotalComisiones = 0m,
                    TotalGanancias = 0m,
                    PromedioDiario = 0m,
                    DiferienciasDetectadas = 0,
                    SobrantesTotal = 0m,
                    FaltantesTotal = 0m
                };
            }

            var totalVentas = cortes.Sum(c => c.TotalVentasCalculado);
            var totalComisiones = cortes.Sum(c => c.ComisionesTotalesCalculadas);
            var totalGanancias = cortes.Sum(c => c.GananciaNetaCalculada);
            var promedioDiario = totalVentas / cortes.Count;

            var cortesConDiferencias = cortes.Where(c => !c.DiferenciaAceptable).ToList();
            var sobrantes = cortes.Where(c => c.TieneSobrante).Sum(c => c.DiferenciaEfectivo);
            var faltantes = Math.Abs(cortes.Where(c => c.TieneFaltante).Sum(c => c.DiferenciaEfectivo));

            return new
            {
                Periodo = $"{desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}",
                CantidadCortes = cortes.Count,
                TotalVentas = totalVentas,
                TotalComisiones = totalComisiones,
                TotalGanancias = totalGanancias,
                PromedioDiario = promedioDiario,
                DiferienciasDetectadas = cortesConDiferencias.Count,
                SobrantesTotal = sobrantes,
                FaltantesTotal = faltantes,
                PorcentajeExactitud = ((decimal)(cortes.Count - cortesConDiferencias.Count) / cortes.Count) * 100
            };
        }

        /// <summary>
        /// Busca cortes con diferencias significativas para auditoría
        /// </summary>
        public async Task<List<CorteCaja>> ObtenerCortesConDiferenciasAsync(decimal margenMaximo = 10)
        {
            var cortes = await _context.CortesCaja
                .Where(c => c.Estado == "Completado")
                .ToListAsync();

            // Filtrar en memoria ya que usamos propiedades calculadas
            return cortes
                .Where(c => Math.Abs(c.DiferenciaEfectivo) > margenMaximo)
                .OrderByDescending(c => Math.Abs(c.DiferenciaEfectivo))
                .ToList();
        }

        // ===== MÉTODOS DE APOYO =====

        /// <summary>
        /// Calcula los gastos del día desde los movimientos registrados
        /// </summary>
        public async Task<decimal> CalcularGastosDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            var fechaFin = fechaSolo.AddDays(1);

            // Obtener movimientos del día que representen gastos
            // Excluimos "Venta" porque esos ya están en las ventas
            var gastosDelDia = await _context.Set<Movimiento>()
                .Where(m => m.FechaMovimiento >= fechaSolo &&
                           m.FechaMovimiento < fechaFin &&
                           (m.TipoMovimiento == "Salida" ||
                            m.TipoMovimiento == "Merma" ||
                            m.TipoMovimiento == "Gasto"))
                .ToListAsync();

            // Calcular total de gastos
            var totalGastos = gastosDelDia.Sum(m => m.ValorTotalConIVA);

            return totalGastos;
        }

        /// <summary>
        /// Obtiene el fondo de caja del día anterior
        /// </summary>
        private async Task<decimal> ObtenerFondoCajaAnteriorAsync(DateTime fecha)
        {
            var fechaAnterior = fecha.Date.AddDays(-1);

            var corteAnterior = await _context.CortesCaja
                .Where(c => c.FechaCorte == fechaAnterior && c.Estado == "Completado")
                .FirstOrDefaultAsync();

            return corteAnterior?.FondoCajaSiguiente ?? 1000; // Valor por defecto configurable
        }

        /// <summary>
        /// Valida que la fecha de corte sea válida
        /// </summary>
        public bool ValidarFechaCorte(DateTime fecha)
        {
            var hoy = DateTime.Today;
            var fechaCorte = fecha.Date;

            // No permitir cortes futuros más allá de hoy
            if (fechaCorte > hoy)
                return false;

            // No permitir cortes muy antiguos (más de 30 días)
            if (fechaCorte < hoy.AddDays(-30))
                return false;

            return true;
        }

        /// <summary>
        /// Obtiene el próximo número de folio para cortes
        /// </summary>
        public async Task<string> GenerarFolioCorteAsync(DateTime fecha)
        {
            var año = fecha.Year;
            var cantidadCortes = await _context.CortesCaja
                .CountAsync(c => c.FechaCorte.Year == año);

            return $"CC{año}{(cantidadCortes + 1):D4}"; // Ejemplo: CC20241
        }

        /// <summary>
        /// Genera reporte de ventas específico para corte de caja
        /// </summary>
        public async Task<string> GenerarReporteVentasParaCorteAsync(DateTime fecha)
        {
            var ventasDelDia = await _context.GetVentasDelDia(fecha)
                .Include(v => v.DetallesVenta)
                .OrderBy(v => v.FechaVenta)
                .ToListAsync();

            if (!ventasDelDia.Any())
                return "No hay ventas registradas para esta fecha.";

            var reporte = $"📊 DETALLE DE VENTAS - {fecha:dd/MM/yyyy}\n\n";

            // Resumen por forma de pago
            var totalEfectivo = ventasDelDia.Sum(v => v.MontoEfectivo);
            var totalTarjeta = ventasDelDia.Sum(v => v.MontoTarjeta);
            var totalTransferencia = ventasDelDia.Sum(v => v.MontoTransferencia);

            reporte += $"💰 RESUMEN POR FORMA DE PAGO:\n";
            reporte += $"   • Efectivo: {totalEfectivo:C2}\n";
            reporte += $"   • Tarjeta: {totalTarjeta:C2}\n";
            reporte += $"   • Transferencia: {totalTransferencia:C2}\n";
            reporte += $"   • TOTAL: {ventasDelDia.Sum(v => v.Total):C2}\n\n";

            // Detalle por horarios
            var ventasPorHora = ventasDelDia
                .GroupBy(v => v.FechaVenta.Hour)
                .OrderBy(g => g.Key);

            reporte += $"🕐 VENTAS POR HORA:\n";
            foreach (var grupo in ventasPorHora)
            {
                var hora = grupo.Key;
                var cantidad = grupo.Count();
                var total = grupo.Sum(v => v.Total);
                reporte += $"   • {hora:D2}:00 - {cantidad} tickets - {total:C2}\n";
            }

            // Comisiones si las hay
            var totalComisiones = ventasDelDia.Sum(v => v.ComisionTotal);
            if (totalComisiones > 0)
            {
                reporte += $"\n🏦 COMISIONES DE TARJETA:\n";
                reporte += $"   • Comisión base: {ventasDelDia.Sum(v => v.ComisionTarjeta):C2}\n";
                reporte += $"   • IVA sobre comisión: {ventasDelDia.Sum(v => v.IVAComision):C2}\n";
                reporte += $"   • Total comisiones: {totalComisiones:C2}\n";
            }

            // ✅ GASTOS DEL DÍA
            var gastosDelDia = await ObtenerDetalleGastosDelDiaAsync(fecha);
            if (gastosDelDia.Any())
            {
                var totalGastos = gastosDelDia.Sum(g => g.ValorTotalConIVA);
                reporte += $"\n💸 GASTOS DEL DÍA:\n";
                foreach (var gasto in gastosDelDia.OrderBy(g => g.FechaMovimiento))
                {
                    reporte += $"   • {gasto.TipoMovimiento}: {gasto.Motivo} - {gasto.ValorTotalConIVA:C2}\n";
                }
                reporte += $"   • TOTAL GASTOS: {totalGastos:C2}\n";
            }

            return reporte;
        }

        /// <summary>
        /// Obtiene el detalle de gastos del día para mostrar en reportes
        /// </summary>
        public async Task<List<Movimiento>> ObtenerDetalleGastosDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            var fechaFin = fechaSolo.AddDays(1);

            return await _context.Set<Movimiento>()
                .Include(m => m.RawMaterial)
                .Where(m => m.FechaMovimiento >= fechaSolo &&
                           m.FechaMovimiento < fechaFin &&
                           (m.TipoMovimiento == "Salida" ||
                            m.TipoMovimiento == "Merma" ||
                            m.TipoMovimiento == "Gasto"))
                .OrderBy(m => m.FechaMovimiento)
                .ToListAsync();
        }

        /// <summary>
        /// Recalcula un corte existente (útil para correcciones)
        /// </summary>
        public async Task<CorteCaja> RecalcularCorteAsync(int corteId)
        {
            var corte = await _context.CortesCaja.FindAsync(corteId);
            if (corte == null)
                throw new ArgumentException("Corte no encontrado");

            // Obtener ventas del día nuevamente
            var ventasDelDia = await _context.GetVentasDelDia(corte.FechaCorte)
                .Include(v => v.DetallesVenta)
                .ToListAsync();

            // Recalcular totales
            corte.CalcularTotalesAutomaticos(ventasDelDia);

            // ✅ RECALCULAR GASTOS
            corte.GastosTotalesCalculados = await CalcularGastosDelDiaAsync(corte.FechaCorte);

            // Actualizar timestamp
            corte.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return corte;
        }
        public void Dispose()
        {
            // Método para limpiar recursos si es necesario
            // Por ahora puede estar vacío
        }
    }
}