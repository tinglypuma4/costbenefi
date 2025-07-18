using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using costbenefi.Models;

namespace costbenefi.Models
{
  
    /// <summary>
    /// Enumeración para los diferentes tipos de formato de reporte
    /// </summary>
   
    /// <summary>
    /// Modelo principal para la generación de reportes de stock
    /// Contiene toda la información necesaria para generar un PDF profesional
    /// </summary>
    public class ReporteStock
    {
        #region Propiedades Básicas del Reporte

        /// <summary>
        /// Título principal del reporte
        /// </summary>
        public string TituloReporte { get; set; }

        /// <summary>
        /// Subtítulo descriptivo del reporte
        /// </summary>
        public string SubtituloReporte { get; set; }

        /// <summary>
        /// Fecha de inicio del período del reporte
        /// </summary>
        public DateTime FechaInicio { get; set; }

        /// <summary>
        /// Fecha de fin del período del reporte
        /// </summary>
        public DateTime FechaFin { get; set; }

        /// <summary>
        /// Fecha y hora de generación del reporte
        /// </summary>
        public DateTime FechaGeneracion { get; set; }

        /// <summary>
        /// Período de tiempo que abarca el reporte
        /// </summary>
        public PeriodoReporte Periodo { get; set; }

        /// <summary>
        /// Tipo de formato del reporte
        /// </summary>
        public TipoFormatoReporte TipoFormato { get; set; }

        /// <summary>
        /// Usuario que generó el reporte
        /// </summary>
        public string UsuarioGenerador { get; set; }

        #endregion

        #region Datos del Reporte

        /// <summary>
        /// Lista de productos incluidos en el reporte
        /// </summary>
        public List<RawMaterial> Productos { get; set; }

        /// <summary>
        /// Filtros aplicados al reporte (para mostrar en el PDF)
        /// </summary>
        public FiltrosAplicados FiltrosAplicados { get; set; }

        #endregion

        #region Estadísticas Calculadas

        /// <summary>
        /// Número total de productos en el reporte
        /// </summary>
        public int TotalProductos => Productos?.Count ?? 0;

        /// <summary>
        /// Valor total del inventario con IVA
        /// </summary>
        public decimal ValorTotalConIVA => Productos?.Sum(p => p.ValorTotalConIVA) ?? 0m;

        /// <summary>
        /// Valor total del inventario sin IVA
        /// </summary>
        public decimal ValorTotalSinIVA => Productos?.Sum(p => p.ValorTotalSinIVA) ?? 0m;

        /// <summary>
        /// Valor total del IVA
        /// </summary>
        public decimal ValorTotalIVA => ValorTotalConIVA - ValorTotalSinIVA;

        /// <summary>
        /// Número de productos con stock bajo
        /// </summary>
        public int ProductosStockBajo => Productos?.Count(p => p.TieneStockBajo) ?? 0;

        /// <summary>
        /// Porcentaje de productos con stock bajo
        /// </summary>
        public decimal PorcentajeStockBajo => TotalProductos > 0 ? (decimal)ProductosStockBajo / TotalProductos * 100 : 0;

        /// <summary>
        /// Valor promedio por producto
        /// </summary>
        public decimal ValorPromedioPorProducto => TotalProductos > 0 ? ValorTotalConIVA / TotalProductos : 0;

        /// <summary>
        /// Producto de mayor valor
        /// </summary>
        public RawMaterial ProductoMayorValor => Productos?.OrderByDescending(p => p.ValorTotalConIVA).FirstOrDefault();

        /// <summary>
        /// Producto de menor valor
        /// </summary>
        public RawMaterial ProductoMenorValor => Productos?.OrderBy(p => p.ValorTotalConIVA).FirstOrDefault();

        #endregion

        #region Resúmenes por Agrupación

        /// <summary>
        /// Resumen de productos agrupados por categoría
        /// </summary>
        public List<ResumenCategoria> ResumenPorCategorias => GenerarResumenPorCategorias();

        /// <summary>
        /// Resumen de productos agrupados por proveedor
        /// </summary>
        public List<ResumenProveedor> ResumenPorProveedores => GenerarResumenPorProveedores();

        /// <summary>
        /// Lista de alertas de stock crítico
        /// </summary>
        public List<AlertaStock> AlertasStock => GenerarAlertasStock();

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor que inicializa el reporte con valores por defecto
        /// </summary>
        public ReporteStock()
        {
            FechaGeneracion = DateTime.Now;
            Productos = new List<RawMaterial>();
            FiltrosAplicados = new FiltrosAplicados();
            UsuarioGenerador = Environment.UserName;
            TipoFormato = TipoFormatoReporte.Estandar;
        }

        /// <summary>
        /// Constructor con parámetros básicos
        /// </summary>
        /// <param name="productos">Lista de productos para el reporte</param>
        /// <param name="periodo">Período del reporte</param>
        /// <param name="tipoFormato">Tipo de formato del reporte</param>
        public ReporteStock(List<RawMaterial> productos, PeriodoReporte periodo, TipoFormatoReporte tipoFormato = TipoFormatoReporte.Estandar)
            : this()
        {
            Productos = productos ?? new List<RawMaterial>();
            Periodo = periodo;
            TipoFormato = tipoFormato;
            ConfigurarFechasSegunPeriodo(periodo);
            GenerarTitulosAutomaticos();
        }

        #endregion

        #region Métodos Privados para Generar Resúmenes

        /// <summary>
        /// Genera el resumen agrupado por categorías
        /// </summary>
        private List<ResumenCategoria> GenerarResumenPorCategorias()
        {
            if (Productos == null || !Productos.Any())
                return new List<ResumenCategoria>();

            return Productos
                .GroupBy(p => p.Categoria ?? "Sin Categoría")
                .Select(grupo => new ResumenCategoria
                {
                    NombreCategoria = grupo.Key,
                    CantidadProductos = grupo.Count(),
                    ValorTotal = grupo.Sum(p => p.ValorTotalConIVA),
                    ProductosStockBajo = grupo.Count(p => p.TieneStockBajo),
                    PorcentajeDelTotal = ValorTotalConIVA > 0 ? (grupo.Sum(p => p.ValorTotalConIVA) / ValorTotalConIVA * 100) : 0
                })
                .OrderByDescending(r => r.ValorTotal)
                .ToList();
        }

        /// <summary>
        /// Genera el resumen agrupado por proveedores
        /// </summary>
        private List<ResumenProveedor> GenerarResumenPorProveedores()
        {
            if (Productos == null || !Productos.Any())
                return new List<ResumenProveedor>();

            return Productos
                .GroupBy(p => p.Proveedor ?? "Sin Proveedor")
                .Select(grupo => new ResumenProveedor
                {
                    NombreProveedor = grupo.Key,
                    CantidadProductos = grupo.Count(),
                    ValorTotal = grupo.Sum(p => p.ValorTotalConIVA),
                    ProductosStockBajo = grupo.Count(p => p.TieneStockBajo),
                    PorcentajeDelTotal = ValorTotalConIVA > 0 ? (grupo.Sum(p => p.ValorTotalConIVA) / ValorTotalConIVA * 100) : 0,
                    CategoriasQueProvee = grupo.Select(p => p.Categoria ?? "Sin Categoría").Distinct().ToList()
                })
                .OrderByDescending(r => r.ValorTotal)
                .ToList();
        }

        /// <summary>
        /// Genera las alertas de stock
        /// </summary>
        private List<AlertaStock> GenerarAlertasStock()
        {
            if (Productos == null || !Productos.Any())
                return new List<AlertaStock>();

            var alertas = new List<AlertaStock>();

            // Productos con stock bajo
            foreach (var producto in Productos.Where(p => p.TieneStockBajo))
            {
                var nivelPrioridad = producto.StockTotal <= 0 ? 4 : // Crítico
                                   producto.StockTotal <= producto.AlertaStockBajo * 0.5m ? 3 : // Alto
                                   2; // Medio

                alertas.Add(new AlertaStock
                {
                    ProductoId = producto.Id,
                    NombreProducto = producto.NombreArticulo,
                    TipoAlerta = producto.StockTotal <= 0 ? "Stock Agotado" : "Stock Bajo",
                    NivelPrioridad = nivelPrioridad,
                    StockActual = producto.StockTotal,
                    StockMinimo = producto.AlertaStockBajo,
                    ValorAfectado = producto.ValorTotalConIVA,
                    Descripcion = GenerarDescripcionAlerta(producto, nivelPrioridad)
                });
            }

            return alertas.OrderByDescending(a => a.NivelPrioridad).ToList();
        }

        /// <summary>
        /// Genera la descripción de una alerta según el producto y prioridad
        /// </summary>
        private string GenerarDescripcionAlerta(RawMaterial producto, int prioridad)
        {
            return prioridad switch
            {
                4 => $"CRÍTICO: {producto.NombreArticulo} está completamente agotado",
                3 => $"URGENTE: {producto.NombreArticulo} tiene stock muy bajo ({producto.StockTotal:F2} {producto.UnidadMedida})",
                2 => $"ATENCIÓN: {producto.NombreArticulo} necesita reabastecimiento ({producto.StockTotal:F2} {producto.UnidadMedida})",
                _ => $"Revisar stock de {producto.NombreArticulo}"
            };
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
            }
        }

        /// <summary>
        /// Genera los títulos automáticamente según el tipo de reporte
        /// </summary>
        private void GenerarTitulosAutomaticos()
        {
            var cultura = CultureInfo.GetCultureInfo("es-ES");

            TituloReporte = TipoFormato switch
            {
                TipoFormatoReporte.Ejecutivo => "📊 REPORTE EJECUTIVO DE INVENTARIO",
                TipoFormatoReporte.Detallado => "📋 ANÁLISIS DETALLADO DE STOCK",
                TipoFormatoReporte.SoloStockBajo => "⚠️ REPORTE DE STOCK CRÍTICO",
                _ => "📦 REPORTE DE INVENTARIO"
            };

            SubtituloReporte = Periodo switch
            {
                PeriodoReporte.Dia => $"Reporte Diario - {FechaInicio:dddd, dd 'de' MMMM 'de' yyyy}",
                PeriodoReporte.Semana => $"Reporte Semanal - {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}",
                PeriodoReporte.Mes => $"Reporte Mensual - {FechaInicio:MMMM 'de' yyyy}",
                PeriodoReporte.Año => $"Reporte Anual - {FechaInicio:yyyy}",
                _ => $"Período: {FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}"
            };
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
        /// Genera un nombre de archivo único para el reporte
        /// </summary>
        public string GenerarNombreArchivo()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var tipoReporte = TipoFormato.ToString();
            var periodo = Periodo.ToString();

            return $"Reporte_Stock_{tipoReporte}_{periodo}_{timestamp}";
        }

        /// <summary>
        /// Valida que el reporte tenga datos suficientes para generar
        /// </summary>
        public bool EsValidoParaGenerar()
        {
            return Productos != null &&
                   Productos.Any() &&
                   !string.IsNullOrWhiteSpace(TituloReporte) &&
                   FechaInicio <= FechaFin;
        }

        #endregion
    }

    #region Clases de Apoyo

    /// <summary>
    /// Información sobre los filtros aplicados al reporte
    /// </summary>


    /// <summary>
    /// Resumen de una categoría de productos
    /// </summary>
    public class ResumenCategoria
    {
        public string NombreCategoria { get; set; }
        public int CantidadProductos { get; set; }
        public decimal ValorTotal { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
    }

    /// <summary>
    /// Resumen de un proveedor
    /// </summary>
    public class ResumenProveedor
    {
        public string NombreProveedor { get; set; }
        public int CantidadProductos { get; set; }
        public decimal ValorTotal { get; set; }
        public int ProductosStockBajo { get; set; }
        public decimal PorcentajeDelTotal { get; set; }
        public List<string> CategoriasQueProvee { get; set; } = new List<string>();
    }

    /// <summary>
    /// Información de una alerta de stock
    /// </summary>
    public class AlertaStock
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public string TipoAlerta { get; set; }
        public int NivelPrioridad { get; set; } // 1=Bajo, 2=Medio, 3=Alto, 4=Crítico
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal ValorAfectado { get; set; }
        public string Descripcion { get; set; }

        /// <summary>
        /// Color recomendado para mostrar la alerta
        /// </summary>
        public string ColorAlerta => NivelPrioridad switch
        {
            4 => "#DC2626", // Rojo crítico
            3 => "#EF4444", // Rojo alto
            2 => "#F59E0B", // Amarillo medio
            1 => "#10B981", // Verde bajo
            _ => "#6B7280"  // Gris normal
        };

        /// <summary>
        /// Icono recomendado para la alerta
        /// </summary>
        public string IconoAlerta => NivelPrioridad switch
        {
            4 => "🚨", // Crítico
            3 => "⚠️", // Alto
            2 => "⚡", // Medio
            1 => "ℹ️", // Bajo
            _ => "📋"  // Normal
        };
    }

    #endregion
}