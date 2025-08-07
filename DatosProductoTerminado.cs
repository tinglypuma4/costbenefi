using System;

namespace costbenefi.Models
{
    /// <summary>
    /// ✅ CLASE PARA DATOS DEL PRODUCTO TERMINADO
    /// </summary>
    public class DatosProductoTerminado
    {
        public string NombreProducto { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string CodigoBarras { get; set; } = "";
        public bool ActivoParaVenta { get; set; } = true;
        public decimal PrecioVenta { get; set; } = 0;
        public decimal MargenObjetivo { get; set; } = 30;
        public decimal StockMinimoVenta { get; set; } = 1;
        public DateTime? FechaVencimiento { get; set; }

        /// <summary>
        /// Valida que los datos sean correctos
        /// </summary>
        public bool ValidarDatos()
        {
            return !string.IsNullOrWhiteSpace(NombreProducto) &&
                   !string.IsNullOrWhiteSpace(Categoria) &&
                   PrecioVenta >= 0 &&
                   MargenObjetivo >= 0 &&
                   StockMinimoVenta >= 0;
        }
    }
}