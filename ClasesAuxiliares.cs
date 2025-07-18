using System;
using System.Text;

namespace costbenefi.Views
{
    #region CLASES AUXILIARES ORIGINALES

    public class ProductoSimplificado
    {
        public string Presentacion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public decimal CantidadPorUnidad { get; set; }
        public decimal CantidadComprada { get; set; } = 1;

        public bool EsEmpaquetado { get; set; }
        public decimal UnidadesPorPaquete { get; set; } = 1;

        public decimal PrecioTotal { get; set; }
        public bool IncluyeIVA { get; set; } = true;
        public decimal PorcentajeIVA { get; set; } = 16;

        public decimal StockAnterior { get; set; }

        public bool EsValido()
        {
            return CantidadPorUnidad > 0 && CantidadComprada > 0 && PrecioTotal > 0 &&
                   !string.IsNullOrEmpty(Presentacion) && !string.IsNullOrEmpty(UnidadMedida);
        }
    }

    public class ResultadoCalculoInteligente
    {
        public bool Exitoso { get; set; }
        public string Error { get; set; } = "";

        public decimal UnidadesTotales { get; set; }
        public decimal ContenidoTotalAgregado { get; set; }
        public decimal StockFinal { get; set; }

        public decimal PrecioSinIVA { get; set; }
        public decimal PrecioConIVA { get; set; }
        public decimal CostoPorUnidad { get; set; }
        public decimal CostoPorUnidadBase { get; set; }

        public string AnalisisCompleto { get; set; } = "";
    }

    public static class CalculadoraInteligente
    {
        public static ResultadoCalculoInteligente Calcular(ProductoSimplificado producto)
        {
            try
            {
                var resultado = new ResultadoCalculoInteligente { Exitoso = true };

                // 1. CALCULAR UNIDADES TOTALES
                resultado.UnidadesTotales = producto.CantidadComprada;
                if (producto.EsEmpaquetado)
                {
                    resultado.UnidadesTotales *= producto.UnidadesPorPaquete;
                }

                // 2. CALCULAR CONTENIDO TOTAL
                resultado.ContenidoTotalAgregado = resultado.UnidadesTotales * producto.CantidadPorUnidad;
                resultado.StockFinal = producto.StockAnterior + resultado.ContenidoTotalAgregado;

                // 3. CALCULAR PRECIOS
                if (producto.IncluyeIVA)
                {
                    resultado.PrecioConIVA = producto.PrecioTotal;
                    resultado.PrecioSinIVA = producto.PrecioTotal / (1 + producto.PorcentajeIVA / 100m);
                }
                else
                {
                    resultado.PrecioSinIVA = producto.PrecioTotal;
                    resultado.PrecioConIVA = producto.PrecioTotal * (1 + producto.PorcentajeIVA / 100m);
                }

                // 4. CALCULAR COSTOS UNITARIOS
                resultado.CostoPorUnidad = resultado.UnidadesTotales > 0 ? resultado.PrecioSinIVA / resultado.UnidadesTotales : 0;
                resultado.CostoPorUnidadBase = resultado.ContenidoTotalAgregado > 0 ? resultado.PrecioSinIVA / resultado.ContenidoTotalAgregado : 0;

                // 5. GENERAR ANÁLISIS
                resultado.AnalisisCompleto = GenerarAnalisis(producto, resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                return new ResultadoCalculoInteligente
                {
                    Exitoso = false,
                    Error = $"Error en cálculo: {ex.Message}"
                };
            }
        }

        private static string GenerarAnalisis(ProductoSimplificado producto, ResultadoCalculoInteligente resultado)
        {
            var analisis = new StringBuilder();

            try
            {
                // Header
                analisis.AppendLine($"🛒 COMPRA: {producto.CantidadComprada} {producto.Presentacion.ToLower()}");

                if (producto.EsEmpaquetado)
                {
                    analisis.AppendLine($"📦 EMPAQUE: {producto.UnidadesPorPaquete} unidades por paquete");
                    analisis.AppendLine($"📊 TOTAL UNIDADES: {resultado.UnidadesTotales:F0}");
                }

                string unidad = ObtenerTextoUnidad(producto.UnidadMedida);
                analisis.AppendLine($"⚖️ CONTENIDO: {producto.CantidadPorUnidad:F2} {unidad} por unidad");
                analisis.AppendLine($"📐 CONTENIDO TOTAL: {resultado.ContenidoTotalAgregado:F2} {unidad}");

                if (producto.StockAnterior > 0)
                {
                    analisis.AppendLine($"📦 STOCK ANTERIOR: {producto.StockAnterior:F2} {unidad}");
                }
                analisis.AppendLine($"📈 STOCK FINAL: {resultado.StockFinal:F2} {unidad}");

                analisis.AppendLine($"💰 PRECIO CON IVA: {resultado.PrecioConIVA:C2}");
                analisis.AppendLine($"💸 PRECIO SIN IVA: {resultado.PrecioSinIVA:C2}");

                // Conversiones de costo
                string costoTexto = GenerarTextoCosto(producto.UnidadMedida, resultado.CostoPorUnidadBase);
                analisis.AppendLine($"💵 {costoTexto}");

                // Análisis de márgenes
                string marginTexto = GenerarAnalisisMargen(producto.UnidadMedida, resultado.CostoPorUnidadBase);
                analisis.AppendLine($"📈 {marginTexto}");
            }
            catch (Exception ex)
            {
                analisis.AppendLine($"Error generando análisis: {ex.Message}");
            }

            return analisis.ToString();
        }

        private static string ObtenerTextoUnidad(string unidad)
        {
            return unidad switch
            {
                "Mililitros" => "ml",
                "Litros" => "litros",
                "Gramos" => "g",
                "Kilos" => "kg",
                "Piezas" => "piezas",
                "ml" => "ml",
                "L" => "litros",
                "g" => "g",
                "kg" => "kg",
                "pzs" => "piezas",
                _ => unidad?.ToLower() ?? "unidades"
            };
        }

        private static string GenerarTextoCosto(string unidad, decimal costo)
        {
            return unidad switch
            {
                "Mililitros" or "ml" => $"COSTO: {costo:C4}/ml | {costo * 1000:C2}/litro",
                "Litros" or "L" => $"COSTO: {costo:C2}/litro | {costo / 1000:C4}/ml",
                "Gramos" or "g" => $"COSTO: {costo:C4}/g | {costo * 1000:C2}/kg",
                "Kilos" or "kg" => $"COSTO: {costo:C2}/kg | {costo / 1000:C4}/g",
                "Piezas" or "pzs" => $"COSTO: {costo:C4}/pieza",
                _ => $"COSTO: {costo:C4}"
            };
        }

        private static string GenerarAnalisisMargen(string unidad, decimal costo)
        {
            string unidadCorta = ObtenerTextoUnidad(unidad);
            if (unidad == "Mililitros" || unidad == "ml") unidadCorta = "ml";
            if (unidad == "Gramos" || unidad == "g") unidadCorta = "g";

            decimal margen50 = costo * 1.5m;
            decimal margen100 = costo * 2.0m;

            return $"MÁRGENES: 50% = {margen50:C4}/{unidadCorta} | 100% = {margen100:C4}/{unidadCorta}";
        }
    }

    #endregion
}