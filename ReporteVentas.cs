using System;
using System.Collections.Generic;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Modelo para análisis completo de ventas - Compatible con servicios PDF/Excel
    /// Enfocado específicamente en reportes de ventas con análisis de rentabilidad
    /// </summary>
    public class ReporteVentas
    {
        #region Propiedades Básicas del Reporte

        /// <summary>
        /// Fecha de inicio del período analizado
        /// </summary>
        public DateTime FechaInicio { get; set; }

        /// <summary>
        /// Fecha de fin del período analizado
        /// </summary>
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Período de tiempo que abarca el análisis
        /// </summary>
        public PeriodoReporte Periodo { get; set; }

        /// <summary>
        /// Lista de ventas incluidas en el análisis
        /// </summary>
        public List<Venta> Ventas { get; set; }

        /// <summary>
        /// Lista de detalles de venta para análisis de productos
        /// </summary>
        public List<DetalleVenta> DetallesVenta { get; set; }

        #endregion

        #region Estadísticas Calculadas - Ventas Generales

        /// <summary>
        /// Cantidad total de ventas en el período
        /// </summary>
        public int TotalVentas => Ventas?.Count ?? 0;

        /// <summary>
        /// Suma total de ingresos brutos
        /// </summary>
        public decimal TotalIngresos => Ventas?.Sum(v => v.Total) ?? 0;

        /// <summary>
        /// Suma total de costos de productos vendidos
        /// </summary>
        public decimal TotalCostos => Ventas?.Sum(v => v.CostoTotal) ?? 0;

        /// <summary>
        /// Ganancia bruta total (sin considerar comisiones)
        /// </summary>
        public decimal GananciaBrutaTotal => Ventas?.Sum(v => v.GananciaBruta) ?? 0;

        /// <summary>
        /// Ganancia neta total (considerando comisiones)
        /// </summary>
        public decimal GananciaNetaTotal => Ventas?.Sum(v => v.GananciaNeta) ?? 0;

        /// <summary>
        /// Total de comisiones pagadas
        /// </summary>
        public decimal TotalComisiones => Ventas?.Sum(v => v.ComisionTotal) ?? 0;

        /// <summary>
        /// Promedio de venta por ticket
        /// </summary>
        public decimal PromedioVentaPorTicket => TotalVentas > 0 ? TotalIngresos / TotalVentas : 0;

        /// <summary>
        /// Margen promedio de ganancia bruta
        /// </summary>
        public decimal MargenPromedioBruto => TotalIngresos > 0 ? (GananciaBrutaTotal / TotalIngresos) * 100 : 0;

        /// <summary>
        /// Margen promedio de ganancia neta
        /// </summary>
        public decimal MargenPromedioNeto => TotalIngresos > 0 ? (GananciaNetaTotal / TotalIngresos) * 100 : 0;

        #endregion

        #region Estadísticas por Formas de Pago

        /// <summary>
        /// Análisis detallado por formas de pago
        /// </summary>
        public List<AnalisisFormaPago> AnalisisPorFormaPago => GenerarAnalisisFormaPago();

        /// <summary>
        /// Total cobrado en efectivo
        /// </summary>
        public decimal TotalEfectivo => Ventas?.Sum(v => v.MontoEfectivo) ?? 0;

        /// <summary>
        /// Total cobrado con tarjeta
        /// </summary>
        public decimal TotalTarjeta => Ventas?.Sum(v => v.MontoTarjeta) ?? 0;

        /// <summary>
        /// Total cobrado por transferencia
        /// </summary>
        public decimal TotalTransferencia => Ventas?.Sum(v => v.MontoTransferencia) ?? 0;

        #endregion

        #region Estadísticas por Productos

        /// <summary>
        /// Análisis detallado por productos vendidos
        /// </summary>
        public List<AnalisisProductoVendido> AnalisisPorProducto => GenerarAnalisisProductos();

        /// <summary>
        /// Cantidad total de productos vendidos (unidades)
        /// </summary>
        public decimal TotalUnidadesVendidas => DetallesVenta?.Sum(d => d.Cantidad) ?? 0;

        /// <summary>
        /// Cantidad de productos diferentes vendidos
        /// </summary>
        public int ProductosDiferentesVendidos => DetallesVenta?.Select(d => d.RawMaterialId).Distinct().Count() ?? 0;

        #endregion

        #region Estadísticas por Clientes

        /// <summary>
        /// Análisis detallado por clientes
        /// </summary>
        public List<AnalisisCliente> AnalisisPorCliente => GenerarAnalisisClientes();

        /// <summary>
        /// Cantidad de clientes únicos
        /// </summary>
        public int ClientesUnicos => Ventas?.Select(v => v.Cliente).Distinct().Count() ?? 0;

        #endregion

        #region Estadísticas por Usuario/Vendedor

        /// <summary>
        /// Análisis detallado por usuarios vendedores
        /// </summary>
        public List<AnalisisUsuarioVendedor> AnalisisPorUsuario => GenerarAnalisisUsuarios();

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor que inicializa el reporte con valores por defecto
        /// </summary>
        public ReporteVentas()
        {
            Ventas = new List<Venta>();
            DetallesVenta = new List<DetalleVenta>();
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="ventas">Lista de ventas para analizar</param>
        /// <param name="periodo">Período del análisis</param>
        public ReporteVentas(List<Venta> ventas, PeriodoReporte periodo)
            : this()
        {
            Ventas = ventas ?? new List<Venta>();
            DetallesVenta = ventas?.SelectMany(v => v.DetallesVenta ?? new List<DetalleVenta>()).ToList() ?? new List<DetalleVenta>();
            Periodo = periodo;
            ConfigurarFechasSegunPeriodo(periodo);
        }

        /// <summary>
        /// Constructor completo con fechas específicas
        /// </summary>
        public ReporteVentas(List<Venta> ventas, DateTime fechaInicio, DateTime fechaFin)
            : this()
        {
            Ventas = ventas ?? new List<Venta>();
            DetallesVenta = ventas?.SelectMany(v => v.DetallesVenta ?? new List<DetalleVenta>()).ToList() ?? new List<DetalleVenta>();
            FechaInicio = fechaInicio;
            FechaFin = fechaFin;
            Periodo = PeriodoReporte.Personalizado;
        }

        #endregion

        #region Métodos Privados para Generar Estadísticas

        private List<AnalisisFormaPago> GenerarAnalisisFormaPago()
        {
            if (Ventas == null || !Ventas.Any())
                return new List<AnalisisFormaPago>();

            var efectivo = new AnalisisFormaPago
            {
                FormaPago = "💵 Efectivo",
                CantidadTransacciones = Ventas.Count(v => v.MontoEfectivo > 0),
                MontoTotal = TotalEfectivo,
                PromedioTransaccion = TotalEfectivo > 0 && Ventas.Count(v => v.MontoEfectivo > 0) > 0
                    ? TotalEfectivo / Ventas.Count(v => v.MontoEfectivo > 0) : 0,
                PorcentajeDelTotal = TotalIngresos > 0 ? (TotalEfectivo / TotalIngresos) * 100 : 0,
                ComisionTotal = 0 // Efectivo no tiene comisión
            };

            var tarjeta = new AnalisisFormaPago
            {
                FormaPago = "💳 Tarjeta",
                CantidadTransacciones = Ventas.Count(v => v.MontoTarjeta > 0),
                MontoTotal = TotalTarjeta,
                PromedioTransaccion = TotalTarjeta > 0 && Ventas.Count(v => v.MontoTarjeta > 0) > 0
                    ? TotalTarjeta / Ventas.Count(v => v.MontoTarjeta > 0) : 0,
                PorcentajeDelTotal = TotalIngresos > 0 ? (TotalTarjeta / TotalIngresos) * 100 : 0,
                ComisionTotal = Ventas.Sum(v => v.ComisionTotal)
            };

            var transferencia = new AnalisisFormaPago
            {
                FormaPago = "📱 Transferencia",
                CantidadTransacciones = Ventas.Count(v => v.MontoTransferencia > 0),
                MontoTotal = TotalTransferencia,
                PromedioTransaccion = TotalTransferencia > 0 && Ventas.Count(v => v.MontoTransferencia > 0) > 0
                    ? TotalTransferencia / Ventas.Count(v => v.MontoTransferencia > 0) : 0,
                PorcentajeDelTotal = TotalIngresos > 0 ? (TotalTransferencia / TotalIngresos) * 100 : 0,
                ComisionTotal = 0 // Transferencia no tiene comisión
            };

            return new List<AnalisisFormaPago> { efectivo, tarjeta, transferencia }
                .Where(a => a.CantidadTransacciones > 0)
                .OrderByDescending(a => a.MontoTotal)
                .ToList();
        }

        private List<AnalisisProductoVendido> GenerarAnalisisProductos()
        {
            if (DetallesVenta == null || !DetallesVenta.Any())
                return new List<AnalisisProductoVendido>();

            return DetallesVenta
                .GroupBy(d => new { d.RawMaterialId, d.NombreProducto })
                .Select(g => new AnalisisProductoVendido
                {
                    ProductoId = g.Key.RawMaterialId ?? 0,
                    NombreProducto = g.Key.NombreProducto,
                    CantidadVendida = g.Sum(d => d.Cantidad),
                    VentasRealizadas = g.Count(),
                    IngresoTotal = g.Sum(d => d.SubTotal),
                    CostoTotal = g.Sum(d => d.CostoUnitario * d.Cantidad),
                    GananciaTotal = g.Sum(d => d.GananciaLinea),
                    PrecioPromedioVenta = g.Average(d => d.PrecioUnitario),
                    MargenPromedio = g.Sum(d => d.SubTotal) > 0 ? (g.Sum(d => d.GananciaLinea) / g.Sum(d => d.SubTotal)) * 100 : 0
                })
                .OrderByDescending(p => p.IngresoTotal)
                .ToList();
        }

        private List<AnalisisCliente> GenerarAnalisisClientes()
        {
            if (Ventas == null || !Ventas.Any())
                return new List<AnalisisCliente>();

            return Ventas
                .GroupBy(v => v.Cliente)
                .Select(g => new AnalisisCliente
                {
                    NombreCliente = g.Key,
                    CantidadCompras = g.Count(),
                    TotalGastado = g.Sum(v => v.Total),
                    PromedioCompra = g.Average(v => v.Total),
                    UltimaCompra = g.Max(v => v.FechaVenta),
                    GananciaGenerada = g.Sum(v => v.GananciaNeta),
                    ProductosComprados = g.SelectMany(v => v.DetallesVenta ?? new List<DetalleVenta>()).Select(d => d.NombreProducto).Distinct().Count()
                })
                .OrderByDescending(c => c.TotalGastado)
                .ToList();
        }

        private List<AnalisisUsuarioVendedor> GenerarAnalisisUsuarios()
        {
            if (Ventas == null || !Ventas.Any())
                return new List<AnalisisUsuarioVendedor>();

            return Ventas
                .GroupBy(v => v.Usuario)
                .Select(g => new AnalisisUsuarioVendedor
                {
                    NombreUsuario = g.Key,
                    VentasRealizadas = g.Count(),
                    TotalVendido = g.Sum(v => v.Total),
                    PromedioVenta = g.Average(v => v.Total),
                    GananciaGenerada = g.Sum(v => v.GananciaNeta),
                    ComisionesTotales = g.Sum(v => v.ComisionTotal),
                    ClientesAtendidos = g.Select(v => v.Cliente).Distinct().Count(),
                    ProductosVendidos = g.SelectMany(v => v.DetallesVenta ?? new List<DetalleVenta>()).Sum(d => d.Cantidad)
                })
                .OrderByDescending(u => u.TotalVendido)
                .ToList();
        }

        #endregion

        #region Métodos de Configuración

        /// <summary>
        /// Configura las fechas del reporte según el período seleccionado
        /// </summary>
        private void ConfigurarFechasSegunPeriodo(PeriodoReporte periodo)
        {
            var ahora = DateTime.Now;

            switch (periodo)
            {
                case PeriodoReporte.Dia:
                    FechaInicio = ahora.Date;
                    FechaFin = ahora.Date.AddDays(1).AddTicks(-1);
                    break;

                case PeriodoReporte.Semana:
                    var inicioSemana = ahora.Date.AddDays(-(int)ahora.DayOfWeek);
                    FechaInicio = inicioSemana;
                    FechaFin = inicioSemana.AddDays(7).AddTicks(-1);
                    break;

                case PeriodoReporte.Mes:
                    FechaInicio = new DateTime(ahora.Year, ahora.Month, 1);
                    FechaFin = FechaInicio.AddMonths(1).AddTicks(-1);
                    break;

                case PeriodoReporte.Año:
                    FechaInicio = new DateTime(ahora.Year, 1, 1);
                    FechaFin = FechaInicio.AddYears(1).AddTicks(-1);
                    break;

                default:
                    // Para período personalizado, usar las fechas actuales
                    FechaInicio = ahora.Date.AddDays(-30);
                    FechaFin = ahora.Date.AddDays(1).AddTicks(-1);
                    break;
            }
        }

        #endregion

        #region Métodos de Utilidad

        /// <summary>
        /// Obtiene el nombre descriptivo del período
        /// </summary>
        public string ObtenerNombrePeriodo()
        {
            return Periodo switch
            {
                PeriodoReporte.Dia => "Diario",
                PeriodoReporte.Semana => "Semanal",
                PeriodoReporte.Mes => "Mensual",
                PeriodoReporte.Año => "Anual",
                _ => "Personalizado"
            };
        }

        /// <summary>
        /// Valida que el reporte tenga datos suficientes para generar
        /// </summary>
        public bool EsValidoParaGenerar()
        {
            return Ventas != null &&
                   Ventas.Any() &&
                   FechaInicio <= FechaFin;
        }

        /// <summary>
        /// Obtiene resumen de texto para mostrar en PDF
        /// </summary>
        public string ObtenerResumenTexto()
        {
            if (!EsValidoParaGenerar())
                return "Sin ventas en el período";

            return $"Período: {ObtenerNombrePeriodo()} | " +
                   $"Ventas: {TotalVentas} | " +
                   $"Ingresos: ${TotalIngresos:N2} | " +
                   $"Ganancia: ${GananciaNetaTotal:N2} | " +
                   $"Margen: {MargenPromedioNeto:F1}%";
        }

        /// <summary>
        /// Obtiene información del período para mostrar en reportes
        /// </summary>
        public string ObtenerInfoPeriodo()
        {
            var diasAnalizados = (int)Math.Ceiling((FechaFin - FechaInicio).TotalDays);
            var promedioVentasPorDia = diasAnalizados > 0 ? (decimal)TotalVentas / diasAnalizados : 0;

            return $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy} | " +
                   $"{diasAnalizados} días | " +
                   $"{promedioVentasPorDia:F1} ventas/día";
        }

        /// <summary>
        /// Obtiene resumen rápido para mostrar en UI
        /// </summary>
        public string ObtenerResumenRapido()
        {
            return $"🎯 {TotalVentas:N0} ventas | " +
                   $"💰 {TotalIngresos:C0} | " +
                   $"📈 {GananciaNetaTotal:C0} ganancia | " +
                   $"📊 {MargenPromedioNeto:F1}% margen";
        }

        /// <summary>
        /// Análisis de horarios de mayor actividad
        /// </summary>
        public List<(int Hora, int CantidadVentas, decimal MontoVentas)> ObtenerVentasPorHora()
        {
            if (Ventas == null || !Ventas.Any())
                return new List<(int, int, decimal)>();

            return Ventas
                .GroupBy(v => v.FechaVenta.Hour)
                .Select(g => (
                    Hora: g.Key,
                    CantidadVentas: g.Count(),
                    MontoVentas: g.Sum(v => v.Total)
                ))
                .OrderBy(x => x.Hora)
                .ToList();
        }

        /// <summary>
        /// Productos con mayor margen de ganancia
        /// </summary>
        public List<AnalisisProductoVendido> ObtenerProductosMayorMargen(int top = 10)
        {
            return AnalisisPorProducto
                .Where(p => p.MargenPromedio > 0)
                .OrderByDescending(p => p.MargenPromedio)
                .Take(top)
                .ToList();
        }

        /// <summary>
        /// Clientes más frecuentes
        /// </summary>
        public List<AnalisisCliente> ObtenerClientesMasFrecuentes(int top = 10)
        {
            return AnalisisPorCliente
                .OrderByDescending(c => c.CantidadCompras)
                .Take(top)
                .ToList();
        }

        #endregion
    }

    #region Clases de Apoyo

    /// <summary>
    /// Análisis específico por forma de pago
    /// </summary>
    public class AnalisisFormaPago
    {
        public string FormaPago { get; set; } = string.Empty;
        public int CantidadTransacciones { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal PromedioTransaccion { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
        public decimal ComisionTotal { get; set; }

        /// <summary>
        /// Monto real recibido después de comisiones
        /// </summary>
        public decimal MontoNeto => MontoTotal - ComisionTotal;

        /// <summary>
        /// Porcentaje de comisión sobre el monto
        /// </summary>
        public decimal PorcentajeComision => MontoTotal > 0 ? (ComisionTotal / MontoTotal) * 100 : 0;
    }

    /// <summary>
    /// Análisis específico por producto vendido
    /// </summary>
    public class AnalisisProductoVendido
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public decimal CantidadVendida { get; set; }
        public int VentasRealizadas { get; set; }
        public decimal IngresoTotal { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal GananciaTotal { get; set; }
        public decimal PrecioPromedioVenta { get; set; }
        public decimal MargenPromedio { get; set; }

        /// <summary>
        /// Cantidad promedio por venta
        /// </summary>
        public decimal CantidadPromedioPorVenta => VentasRealizadas > 0 ? CantidadVendida / VentasRealizadas : 0;

        /// <summary>
        /// Indica si es un producto rentable
        /// </summary>
        public bool EsRentable => MargenPromedio > 0;
    }

    /// <summary>
    /// Análisis específico por cliente
    /// </summary>
    public class AnalisisCliente
    {
        public string NombreCliente { get; set; } = string.Empty;
        public int CantidadCompras { get; set; }
        public decimal TotalGastado { get; set; }
        public decimal PromedioCompra { get; set; }
        public DateTime UltimaCompra { get; set; }
        public decimal GananciaGenerada { get; set; }
        public int ProductosComprados { get; set; }

        /// <summary>
        /// Días desde la última compra
        /// </summary>
        public int DiasSinComprar => (int)(DateTime.Now - UltimaCompra).TotalDays;

        /// <summary>
        /// Indica si es un cliente frecuente (compra mensualmente)
        /// </summary>
        public bool EsClienteFrecuente => DiasSinComprar <= 30;

        /// <summary>
        /// Indica si es un cliente valioso (alta ganancia o muchas compras)
        /// </summary>
        public bool EsClienteValioso => GananciaGenerada > 1000 || CantidadCompras > 10;
    }

    /// <summary>
    /// Análisis específico por usuario vendedor
    /// </summary>
    public class AnalisisUsuarioVendedor
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public int VentasRealizadas { get; set; }
        public decimal TotalVendido { get; set; }
        public decimal PromedioVenta { get; set; }
        public decimal GananciaGenerada { get; set; }
        public decimal ComisionesTotales { get; set; }
        public int ClientesAtendidos { get; set; }
        public decimal ProductosVendidos { get; set; }

        /// <summary>
        /// Margen promedio de sus ventas
        /// </summary>
        public decimal MargenPromedio => TotalVendido > 0 ? (GananciaGenerada / TotalVendido) * 100 : 0;

        /// <summary>
        /// Promedio de productos por venta
        /// </summary>
        public decimal ProductosPromedioPorVenta => VentasRealizadas > 0 ? ProductosVendidos / VentasRealizadas : 0;

        /// <summary>
        /// Indica si es un vendedor eficiente
        /// </summary>
        public bool EsVendedorEficiente => MargenPromedio > 20 && PromedioVenta > 100;
    }

    #endregion
}