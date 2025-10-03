using System;
using System.Collections.Generic;
using System.Linq;
using costbenefi.Models;

namespace costbenefi.Models
{
    /// <summary>
    /// Clase de análisis profesional para reportes de cortes de caja
    /// Proporciona estadísticas, métricas y análisis detallado
    /// </summary>
    public class ReporteCortesCaja
    {
        #region Propiedades Principales

        public List<CorteCaja> Cortes { get; private set; }
        public PeriodoReporte Periodo { get; private set; }
        public DateTime FechaInicio { get; private set; }
        public DateTime FechaFin { get; private set; }

        #endregion

        #region Métricas Generales

        public int TotalCortes => Cortes.Count;
        public int CortesCompletados => Cortes.Count(c => c.Estado == "Completado");
        public int CortesPendientes => Cortes.Count(c => c.Estado == "Pendiente");
        public int CortesCancelados => Cortes.Count(c => c.Estado == "Cancelado");

        public decimal TotalVentasDelPeriodo => Cortes.Sum(c => c.TotalVentasCalculado);
        public int TotalTicketsProcesados => Cortes.Sum(c => c.CantidadTickets);

        public decimal TotalEfectivoEsperado => Cortes.Sum(c => c.EfectivoCalculado);
        public decimal TotalEfectivoContado => Cortes.Sum(c => c.EfectivoContado);
        public decimal DiferenciaGlobalEfectivo => TotalEfectivoContado - TotalEfectivoEsperado;

        public decimal TotalTarjetasDelPeriodo => Cortes.Sum(c => c.TarjetaCalculado);
        public decimal TotalTransferenciasDelPeriodo => Cortes.Sum(c => c.TransferenciaCalculado);

        public decimal TotalComisionesDelPeriodo => Cortes.Sum(c => c.ComisionesTotalesCalculadas);
        public decimal TotalComisionesBase => Cortes.Sum(c => c.ComisionesCalculadas);
        public decimal TotalIVAComisiones => Cortes.Sum(c => c.IVAComisionesCalculado);

        public decimal GananciaBrutaTotal => Cortes.Sum(c => c.GananciaBrutaCalculada);
        public decimal GananciaNetaTotal => Cortes.Sum(c => c.GananciaNetaCalculada);

        public decimal PromedioVentasPorDia => TotalCortes > 0 ? TotalVentasDelPeriodo / TotalCortes : 0;
        public decimal PromedioTicketsPorDia => TotalCortes > 0 ? (decimal)TotalTicketsProcesados / TotalCortes : 0;
        public decimal PromedioVentaPorTicket => TotalTicketsProcesados > 0 ? TotalVentasDelPeriodo / TotalTicketsProcesados : 0;

        #endregion

        #region Análisis de Diferencias

        public int CortesConDiferencias => Cortes.Count(c => !c.DiferenciaAceptable && c.Estado == "Completado");
        public int CortesConSobrante => Cortes.Count(c => c.TieneSobrante && c.Estado == "Completado");
        public int CortesConFaltante => Cortes.Count(c => c.TieneFaltante && c.Estado == "Completado");
        public int CortesExactos => Cortes.Count(c => c.DiferenciaAceptable && c.Estado == "Completado");

        public decimal TotalSobrantes => Cortes.Where(c => c.TieneSobrante && c.Estado == "Completado")
                                                .Sum(c => c.DiferenciaEfectivo);
        public decimal TotalFaltantes => Math.Abs(Cortes.Where(c => c.TieneFaltante && c.Estado == "Completado")
                                                        .Sum(c => c.DiferenciaEfectivo));

        public decimal PorcentajeCortesExactos => CortesCompletados > 0
            ? ((decimal)CortesExactos / CortesCompletados) * 100 : 0;

        public decimal PorcentajeCortesConDiferencias => CortesCompletados > 0
            ? ((decimal)CortesConDiferencias / CortesCompletados) * 100 : 0;

        #endregion

        #region Análisis de Formas de Pago

        public decimal PorcentajeEfectivo => TotalVentasDelPeriodo > 0
            ? (TotalEfectivoEsperado / TotalVentasDelPeriodo) * 100 : 0;

        public decimal PorcentajeTarjeta => TotalVentasDelPeriodo > 0
            ? (TotalTarjetasDelPeriodo / TotalVentasDelPeriodo) * 100 : 0;

        public decimal PorcentajeTransferencia => TotalVentasDelPeriodo > 0
            ? (TotalTransferenciasDelPeriodo / TotalVentasDelPeriodo) * 100 : 0;

        #endregion

        #region Análisis de Rentabilidad

        public decimal MargenBrutoPromedio => TotalVentasDelPeriodo > 0
            ? (GananciaBrutaTotal / TotalVentasDelPeriodo) * 100 : 0;

        public decimal MargenNetoPromedio => TotalVentasDelPeriodo > 0
            ? (GananciaNetaTotal / TotalVentasDelPeriodo) * 100 : 0;

        public decimal ImpactoComisiones => TotalVentasDelPeriodo > 0
            ? (TotalComisionesDelPeriodo / TotalVentasDelPeriodo) * 100 : 0;

        public decimal CostosTotales => TotalVentasDelPeriodo - GananciaBrutaTotal;

        #endregion

        #region Usuarios

        public int UsuariosUnicos => Cortes.Select(c => c.UsuarioCorte).Distinct().Count();

        #endregion

        #region Constructor

        public ReporteCortesCaja(List<CorteCaja> cortes, PeriodoReporte periodo = PeriodoReporte.Personalizado)
        {
            Cortes = cortes ?? new List<CorteCaja>();
            Periodo = periodo;

            if (Cortes.Any())
            {
                FechaInicio = Cortes.Min(c => c.FechaCorte);
                FechaFin = Cortes.Max(c => c.FechaCorte);
            }
            else
            {
                FechaInicio = DateTime.Today;
                FechaFin = DateTime.Today;
            }
        }

        #endregion

        #region Métodos de Análisis

        /// <summary>
        /// Obtiene el mejor día de ventas del período
        /// </summary>
        public CorteCaja ObtenerMejorDia()
        {
            return Cortes.Where(c => c.Estado == "Completado")
                        .OrderByDescending(c => c.TotalVentasCalculado)
                        .FirstOrDefault();
        }

        /// <summary>
        /// Obtiene el peor día de ventas del período
        /// </summary>
        public CorteCaja ObtenerPeorDia()
        {
            return Cortes.Where(c => c.Estado == "Completado")
                        .OrderBy(c => c.TotalVentasCalculado)
                        .FirstOrDefault();
        }

        /// <summary>
        /// Obtiene cortes ordenados por monto de ventas
        /// </summary>
        public List<CorteCaja> ObtenerCortesOrdenadosPorVentas()
        {
            return Cortes.Where(c => c.Estado == "Completado")
                        .OrderByDescending(c => c.TotalVentasCalculado)
                        .ToList();
        }

        /// <summary>
        /// Obtiene cortes con mayores diferencias
        /// </summary>
        public List<CorteCaja> ObtenerCortesMayoresDiferencias(int cantidad = 10)
        {
            return Cortes.Where(c => c.Estado == "Completado")
                        .OrderByDescending(c => Math.Abs(c.DiferenciaEfectivo))
                        .Take(cantidad)
                        .ToList();
        }

        /// <summary>
        /// Análisis por usuario
        /// </summary>
        public List<AnalisisUsuarioCorte> ObtenerAnalisisPorUsuario()
        {
            return Cortes.Where(c => c.Estado == "Completado")
                        .GroupBy(c => c.UsuarioCorte)
                        .Select(g => new AnalisisUsuarioCorte
                        {
                            NombreUsuario = g.Key,
                            CantidadCortes = g.Count(),
                            TotalVentas = g.Sum(c => c.TotalVentasCalculado),
                            PromedioVentas = g.Average(c => c.TotalVentasCalculado),
                            TotalComisiones = g.Sum(c => c.ComisionesTotalesCalculadas),
                            GananciaGenerada = g.Sum(c => c.GananciaNetaCalculada),
                            CortesConDiferencias = g.Count(c => !c.DiferenciaAceptable),
                            DiferenciaTotal = g.Sum(c => c.DiferenciaEfectivo),
                            PorcentajeExactitud = g.Count() > 0
                                ? ((decimal)g.Count(c => c.DiferenciaAceptable) / g.Count()) * 100 : 0
                        })
                        .OrderByDescending(a => a.TotalVentas)
                        .ToList();
        }

        /// <summary>
        /// Análisis temporal por día de la semana
        /// </summary>
        public List<AnalisisDiaSemana> ObtenerAnalisisPorDiaSemana()
        {
            return Cortes.Where(c => c.Estado == "Completado")
                        .GroupBy(c => c.FechaCorte.DayOfWeek)
                        .Select(g => new AnalisisDiaSemana
                        {
                            DiaSemana = g.Key,
                            NombreDia = ObtenerNombreDia(g.Key),
                            CantidadCortes = g.Count(),
                            VentasPromedio = g.Average(c => c.TotalVentasCalculado),
                            VentasTotal = g.Sum(c => c.TotalVentasCalculado),
                            ComisionesTotal = g.Sum(c => c.ComisionesTotalesCalculadas)
                        })
                        .OrderBy(a => a.DiaSemana)
                        .ToList();
        }

        /// <summary>
        /// Análisis de formas de pago detallado
        /// </summary>
        public AnalisisFormasPagoCorte ObtenerAnalisisFormasPago()
        {
            return new AnalisisFormasPagoCorte
            {
                TotalEfectivo = TotalEfectivoEsperado,
                TotalTarjeta = TotalTarjetasDelPeriodo,
                TotalTransferencia = TotalTransferenciasDelPeriodo,
                PorcentajeEfectivo = PorcentajeEfectivo,
                PorcentajeTarjeta = PorcentajeTarjeta,
                PorcentajeTransferencia = PorcentajeTransferencia,
                ComisionesTotales = TotalComisionesDelPeriodo,
                PromedioEfectivoPorCorte = TotalCortes > 0 ? TotalEfectivoEsperado / TotalCortes : 0,
                PromedioTarjetaPorCorte = TotalCortes > 0 ? TotalTarjetasDelPeriodo / TotalCortes : 0
            };
        }

        /// <summary>
        /// Obtiene resumen ejecutivo en texto
        /// </summary>
        public string ObtenerResumenEjecutivo()
        {
            var mejorDia = ObtenerMejorDia();
            var peorDia = ObtenerPeorDia();

            return $"En el período se procesaron {TotalCortes} cortes de caja con un total de " +
                   $"{TotalVentasDelPeriodo:C0} en ventas. El promedio diario fue de {PromedioVentasPorDia:C0}. " +
                   $"Se detectaron {CortesConDiferencias} cortes con diferencias significativas. " +
                   $"El mejor día fue {mejorDia?.FechaCorte:dd/MM/yyyy} con {mejorDia?.TotalVentasCalculado:C0}.";
        }

        /// <summary>
        /// Obtiene información del período
        /// </summary>
        public string ObtenerInfoPeriodo()
        {
            return $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}";
        }

        /// <summary>
        /// Obtiene nombre descriptivo del período
        /// </summary>
        public string ObtenerNombrePeriodo()
        {
            return Periodo switch
            {
                PeriodoReporte.Dia => $"Diario - {FechaInicio:dd/MM/yyyy}",
                PeriodoReporte.Semana => $"Semanal - Semana del {FechaInicio:dd/MM/yyyy}",
                PeriodoReporte.Mes => $"Mensual - {FechaInicio:MMMM yyyy}",
                PeriodoReporte.Año => $"Anual - {FechaInicio:yyyy}",
                _ => $"Personalizado - {ObtenerInfoPeriodo()}"
            };
        }

        #endregion

        #region Métodos de Utilidad

        private string ObtenerNombreDia(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => dia.ToString()
            };
        }

        #endregion
    }

    #region Clases de Análisis Auxiliares

    /// <summary>
    /// Análisis de usuario que realiza cortes
    /// </summary>
    public class AnalisisUsuarioCorte
    {
        public string NombreUsuario { get; set; }
        public int CantidadCortes { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal PromedioVentas { get; set; }
        public decimal TotalComisiones { get; set; }
        public decimal GananciaGenerada { get; set; }
        public int CortesConDiferencias { get; set; }
        public decimal DiferenciaTotal { get; set; }
        public decimal PorcentajeExactitud { get; set; }

        public bool EsUsuarioEficiente => PorcentajeExactitud >= 80 && CantidadCortes >= 3;
        public string ClasificacionUsuario => PorcentajeExactitud >= 90 ? "Excelente" :
                                             PorcentajeExactitud >= 80 ? "Bueno" :
                                             PorcentajeExactitud >= 70 ? "Regular" : "Requiere Capacitación";
    }

    /// <summary>
    /// Análisis por día de la semana
    /// </summary>
    public class AnalisisDiaSemana
    {
        public DayOfWeek DiaSemana { get; set; }
        public string NombreDia { get; set; }
        public int CantidadCortes { get; set; }
        public decimal VentasPromedio { get; set; }
        public decimal VentasTotal { get; set; }
        public decimal ComisionesTotal { get; set; }

        public bool EsDiaPico => VentasPromedio > 0; // Se calculará en contexto del reporte
    }

    /// <summary>
    /// Análisis de formas de pago en cortes
    /// </summary>
    public class AnalisisFormasPagoCorte
    {
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalTransferencia { get; set; }
        public decimal PorcentajeEfectivo { get; set; }
        public decimal PorcentajeTarjeta { get; set; }
        public decimal PorcentajeTransferencia { get; set; }
        public decimal ComisionesTotales { get; set; }
        public decimal PromedioEfectivoPorCorte { get; set; }
        public decimal PromedioTarjetaPorCorte { get; set; }

        public string FormaPagoPreferida
        {
            get
            {
                if (PorcentajeEfectivo > PorcentajeTarjeta && PorcentajeEfectivo > PorcentajeTransferencia)
                    return "Efectivo";
                else if (PorcentajeTarjeta > PorcentajeTransferencia)
                    return "Tarjeta";
                else
                    return "Transferencia";
            }
        }
    }

    #endregion
}