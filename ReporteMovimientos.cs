using System;
using System.Collections.Generic;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Modelo para análisis de movimientos de stock - VERSIÓN SIMPLIFICADA
    /// Compatible con el modelo Movimiento actualizado (EsEntrada, EsSalida)
    /// Enfocado específicamente en reportes PDF/Excel
    /// </summary>
    public class ReporteMovimientos
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
        /// Lista de productos incluidos en el análisis
        /// </summary>
        public List<RawMaterial> Productos { get; set; }

        /// <summary>
        /// Lista de movimientos del período
        /// </summary>
        public List<Movimiento> Movimientos { get; set; }

        #endregion

        #region Estadísticas Calculadas - Movimientos

        /// <summary>
        /// Estadísticas detalladas por producto
        /// </summary>
        public List<EstadisticaMovimiento> EstadisticasPorProducto => GenerarEstadisticasPorProducto();

        /// <summary>
        /// Total de productos que tuvieron movimientos
        /// </summary>
        public int ProductosConMovimientos => EstadisticasPorProducto.Count;

        /// <summary>
        /// Suma total de todas las entradas del período
        /// </summary>
        public decimal TotalEntradas => EstadisticasPorProducto.Sum(e => e.Entradas);

        /// <summary>
        /// Suma total de todas las salidas del período
        /// </summary>
        public decimal TotalSalidas => EstadisticasPorProducto.Sum(e => e.Salidas);

        /// <summary>
        /// Diferencia neta total (Entradas - Salidas)
        /// </summary>
        public decimal DiferenciaNeta => TotalEntradas - TotalSalidas;

        /// <summary>
        /// Valor total movido en el período
        /// </summary>
        public decimal ValorTotalMovido => EstadisticasPorProducto.Sum(e => e.ValorMovido);

        /// <summary>
        /// Cantidad total de movimientos registrados
        /// </summary>
        public int TotalMovimientos => Movimientos?.Count ?? 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor que inicializa el reporte con valores por defecto
        /// </summary>
        public ReporteMovimientos()
        {
            Productos = new List<RawMaterial>();
            Movimientos = new List<Movimiento>();
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="productos">Lista de productos para analizar</param>
        /// <param name="movimientos">Lista de movimientos del período</param>
        /// <param name="periodo">Período del análisis</param>
        public ReporteMovimientos(List<RawMaterial> productos, List<Movimiento> movimientos, PeriodoReporte periodo)
            : this()
        {
            Productos = productos ?? new List<RawMaterial>();
            Movimientos = movimientos ?? new List<Movimiento>();
            Periodo = periodo;
            ConfigurarFechasSegunPeriodo(periodo);
        }

        #endregion

        #region Métodos Privados para Generar Estadísticas

        /// <summary>
        /// Genera estadísticas detalladas por cada producto
        /// ✅ CORREGIDO: Compatible con propiedades EsEntrada y EsSalida del modelo Movimiento actualizado
        /// </summary>
        private List<EstadisticaMovimiento> GenerarEstadisticasPorProducto()
        {
            if (Productos == null || !Productos.Any() || Movimientos == null)
                return new List<EstadisticaMovimiento>();

            var estadisticas = new List<EstadisticaMovimiento>();

            foreach (var producto in Productos)
            {
                var movimientosProducto = Movimientos.Where(m => m.RawMaterialId == producto.Id).ToList();

                // Solo incluir productos que tuvieron movimientos
                if (!movimientosProducto.Any()) continue;

                // ✅ CORREGIDO: Usar las propiedades EsEntrada y EsSalida del modelo actualizado
                var entradas = movimientosProducto
                    .Where(m => m.EsEntrada)
                    .Sum(m => m.Cantidad);

                var salidas = movimientosProducto
                    .Where(m => m.EsSalida)
                    .Sum(m => m.Cantidad);

                // ✅ CORREGIDO: Usar ValorTotalConIVA (propiedad calculada del modelo actualizado)
                var valorMovido = movimientosProducto
                    .Sum(m => m.ValorTotalConIVA);

                // Calcular stock inicial (stock actual - movimientos netos del período)
                var stockInicial = Math.Max(0, producto.StockTotal - (entradas - salidas));

                estadisticas.Add(new EstadisticaMovimiento
                {
                    ProductoId = producto.Id,
                    NombreProducto = producto.NombreArticulo,
                    Categoria = producto.Categoria,
                    UnidadMedida = producto.UnidadMedida,
                    StockInicial = stockInicial,
                    Entradas = entradas,
                    Salidas = salidas,
                    StockFinal = producto.StockTotal,
                    ValorMovido = valorMovido,
                    CantidadMovimientos = movimientosProducto.Count
                });
            }

            return estadisticas.OrderByDescending(e => e.ValorMovido).ToList();
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
            return Productos != null &&
                   Productos.Any() &&
                   EstadisticasPorProducto.Any() &&
                   FechaInicio <= FechaFin;
        }

        /// <summary>
        /// Obtiene resumen de texto para mostrar en PDF
        /// </summary>
        public string ObtenerResumenTexto()
        {
            if (!EsValidoParaGenerar())
                return "Sin movimientos en el período";

            return $"Período: {ObtenerNombrePeriodo()} | " +
                   $"Productos: {ProductosConMovimientos} | " +
                   $"Entradas: {TotalEntradas:F1} | " +
                   $"Salidas: {TotalSalidas:F1} | " +
                   $"Valor movido: ${ValorTotalMovido:N2}";
        }

        /// <summary>
        /// ✅ NUEVO: Obtiene información del período para mostrar en reportes
        /// </summary>
        public string ObtenerInfoPeriodo()
        {
            var diasAnalizados = (int)Math.Ceiling((FechaFin - FechaInicio).TotalDays);
            var promedioMovimientosPorDia = diasAnalizados > 0 ? (decimal)TotalMovimientos / diasAnalizados : 0;

            return $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy} | " +
                   $"{diasAnalizados} días | " +
                   $"{promedioMovimientosPorDia:F1} mov/día";
        }

        /// <summary>
        /// ✅ NUEVO: Obtiene resumen rápido para mostrar en UI
        /// </summary>
        public string ObtenerResumenRapido()
        {
            return $"📊 {TotalMovimientos:N0} movimientos | " +
                   $"📦 {ProductosConMovimientos:N0} productos | " +
                   $"💰 {ValorTotalMovido:C0} | " +
                   $"⚖️ {(DiferenciaNeta >= 0 ? "+" : "")}{DiferenciaNeta:F1} unidades";
        }

        /// <summary>
        /// ✅ NUEVO: Análisis de tipos de movimientos más activos
        /// </summary>
        public List<(string Tipo, int Cantidad, decimal Valor)> ObtenerResumenPorTipos()
        {
            if (Movimientos == null || !Movimientos.Any())
                return new List<(string, int, decimal)>();

            return Movimientos
                .GroupBy(m => m.TipoMovimiento ?? "Desconocido")
                .Select(g => (
                    Tipo: g.Key,
                    Cantidad: g.Count(),
                    Valor: g.Sum(m => m.ValorTotalConIVA)
                ))
                .OrderByDescending(x => x.Cantidad)
                .ToList();
        }

        /// <summary>
        /// ✅ NUEVO: Usuarios más activos en el período
        /// </summary>
        public List<(string Usuario, int Movimientos, decimal Valor)> ObtenerUsuariosMasActivos(int top = 5)
        {
            if (Movimientos == null || !Movimientos.Any())
                return new List<(string, int, decimal)>();

            return Movimientos
                .Where(m => !string.IsNullOrEmpty(m.Usuario))
                .GroupBy(m => m.Usuario)
                .Select(g => (
                    Usuario: g.Key,
                    Movimientos: g.Count(),
                    Valor: g.Sum(m => m.ValorTotalConIVA)
                ))
                .OrderByDescending(x => x.Movimientos)
                .Take(top)
                .ToList();
        }

        #endregion
    }

    #region Clase de Apoyo

    /// <summary>
    /// Estadísticas de movimientos por producto individual
    /// ✅ MANTIENE LA ESTRUCTURA ORIGINAL: Compatible con servicios PDF/Excel existentes
    /// </summary>
    public class EstadisticaMovimiento
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockInicial { get; set; }
        public decimal Entradas { get; set; }
        public decimal Salidas { get; set; }
        public decimal StockFinal { get; set; }
        public decimal ValorMovido { get; set; }
        public int CantidadMovimientos { get; set; }

        /// <summary>
        /// Diferencia neta del período (Entradas - Salidas)
        /// </summary>
        public decimal DiferenciaNeta => Entradas - Salidas;

        /// <summary>
        /// Indica si hubo actividad en el período
        /// </summary>
        public bool TuvoActividad => Entradas > 0 || Salidas > 0;

        /// <summary>
        /// Porcentaje de cambio respecto al stock inicial
        /// </summary>
        public decimal PorcentajeCambio => StockInicial > 0 ? (DiferenciaNeta / StockInicial * 100) : 0;

        /// <summary>
        /// ✅ NUEVO: Valor promedio por movimiento
        /// </summary>
        public decimal ValorPromedioPorMovimiento
        {
            get
            {
                if (CantidadMovimientos <= 0) return 0;
                return ValorMovido / CantidadMovimientos;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Indica si tuvo más entradas que salidas
        /// </summary>
        public bool TuvoMasEntradas => Entradas > Salidas;

        /// <summary>
        /// ✅ NUEVO: Balance neto de movimientos
        /// </summary>
        public decimal BalanceNeto => Entradas - Salidas;
    }

    #endregion

    #region Enumeración PeriodoReporte (si no existe en otro lugar)

    /// <summary>
    /// Enumeración para períodos de reporte
    /// </summary>
    public enum PeriodoReporte
    {
        Dia,
        Semana,
        Mes,
        Año,
        Personalizado
    }

    #endregion
}