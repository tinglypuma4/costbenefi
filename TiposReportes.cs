using System.Collections.Generic;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Tipos de formato disponibles para los reportes de ventas
    /// </summary>
    public enum TipoFormatoReporte
    {
        Estandar,
        Ejecutivo,
        Detallado,
        SoloStockBajo,
        PorProductos,
        PorClientes,
        PorUsuarios,
        Financiero
    }

    /// <summary>
    /// Clase que encapsula todos los filtros aplicables a los reportes (ventas y stock)
    /// </summary>
    public class FiltrosAplicados
    {
        // Filtros para ventas
        public List<string> ClientesSeleccionados { get; set; } = new();
        public List<string> UsuariosSeleccionados { get; set; } = new();
        public List<string> ProductosSeleccionados { get; set; } = new();
        public decimal? MontoMinimo { get; set; }
        public decimal? MontoMaximo { get; set; }
        public bool SoloVentasConComision { get; set; }
        public bool SoloVentasRentables { get; set; }

        // Filtros para stock
        public List<string> CategoriasSeleccionadas { get; set; } = new();
        public List<string> ProveedoresSeleccionados { get; set; } = new();
        public bool SoloStockBajo { get; set; }
        public decimal? StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public decimal? ValorMinimo { get; set; }
        public decimal? ValorMaximo { get; set; }

        /// <summary>
        /// Indica si se han aplicado filtros específicos al reporte
        /// </summary>
        public bool TieneFiltrosAplicados =>
            ClientesSeleccionados.Any() ||
            UsuariosSeleccionados.Any() ||
            ProductosSeleccionados.Any() ||
            CategoriasSeleccionadas.Any() ||
            ProveedoresSeleccionados.Any() ||
            MontoMinimo.HasValue ||
            MontoMaximo.HasValue ||
            StockMinimo.HasValue ||
            StockMaximo.HasValue ||
            ValorMinimo.HasValue ||
            ValorMaximo.HasValue ||
            SoloVentasConComision ||
            SoloVentasRentables ||
            SoloStockBajo;

        /// <summary>
        /// Obtiene una descripción textual de los filtros aplicados
        /// </summary>
        public string DescripcionFiltros
        {
            get
            {
                var descripciones = new List<string>();

                if (ClientesSeleccionados.Any())
                    descripciones.Add($"Clientes: {string.Join(", ", ClientesSeleccionados)}");

                if (UsuariosSeleccionados.Any())
                    descripciones.Add($"Usuarios: {string.Join(", ", UsuariosSeleccionados)}");

                if (ProductosSeleccionados.Any())
                    descripciones.Add($"Productos: {string.Join(", ", ProductosSeleccionados)}");

                if (MontoMinimo.HasValue || MontoMaximo.HasValue)
                {
                    if (MontoMinimo.HasValue && MontoMaximo.HasValue)
                        descripciones.Add($"Monto: ${MontoMinimo:F2} - ${MontoMaximo:F2}");
                    else if (MontoMinimo.HasValue)
                        descripciones.Add($"Monto mínimo: ${MontoMinimo:F2}");
                    else
                        descripciones.Add($"Monto máximo: ${MontoMaximo:F2}");
                }

                if (SoloVentasConComision)
                    descripciones.Add("Solo ventas con comisión");

                if (SoloVentasRentables)
                    descripciones.Add("Solo ventas rentables");

                return descripciones.Any() ? string.Join(" | ", descripciones) : "Sin filtros aplicados";
            }
        }

        public decimal MargenMinimo { get; internal set; }
        public decimal MargenMaximo { get; internal set; }
    }
}