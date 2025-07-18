using System;
using System.Collections.Generic;

namespace costbenefi.Models
{
    public class MaterialCalculator
    {
        public static CalculationResult Calculate(MaterialInput input)
        {
            var result = new CalculationResult();

            // Precio sin IVA usando el IVA configurable
            result.PrecioSinIVA = input.IncludeIVA
                ? input.PrecioTotal / (1 + input.IVARate)
                : input.PrecioTotal;

            // Precio con IVA (para mostrar sugerencia)
            result.PrecioConIVA = input.IncludeIVA
                ? input.PrecioTotal
                : input.PrecioTotal * (1 + input.IVARate);

            // Precio por unidad de presentación
            result.PrecioPorUnidad = input.Cantidad > 0
                ? result.PrecioSinIVA / input.Cantidad
                : 0;

            // Conversión a unidad base
            decimal factorConversion = GetFactorConversion(input.UnidadBase);
            decimal unidadesBaseTotales = input.Cantidad * input.EquivalenciaNumero * factorConversion;

            // Precio por unidad base (ml, gramo, cm, etc.)
            result.PrecioPorUnidadBase = unidadesBaseTotales > 0
                ? result.PrecioSinIVA / unidadesBaseTotales
                : 0;

            // CALCULAR PRECIOS BASE CON/SIN IVA
            result.PrecioBaseSinIVA = result.PrecioPorUnidadBase;
            result.PrecioBaseConIVA = result.PrecioBaseSinIVA * (1 + input.IVARate);

            // Información detallada
            result.UnidadBaseFinal = GetUnidadBaseFinal(input.UnidadBase);
            result.TotalUnidadesBase = unidadesBaseTotales;
            result.DescripcionCalculo = GenerarDescripcion(input, result);

            return result;
        }

        private static decimal GetFactorConversion(string unidadBase)
        {
            return unidadBase switch
            {
                "Litros" => 1000m,      // 1 litro = 1000 ml
                "Kilos" => 1000m,       // 1 kilo = 1000 gramos
                "Metros" => 100m,       // 1 metro = 100 cm
                "Mililitros" => 1m,
                "Gramos" => 1m,
                "Centimetros" => 1m,
                "Piezas" => 1m,
                _ => 1m
            };
        }

        private static string GetUnidadBaseFinal(string unidadBase)
        {
            return unidadBase switch
            {
                "Litros" => "ml",
                "Kilos" => "gr",
                "Metros" => "cm",
                "Mililitros" => "ml",
                "Gramos" => "gr",
                "Centimetros" => "cm",
                "Piezas" => "pza",
                _ => unidadBase.ToLower()
            };
        }

        private static string GenerarDescripcion(MaterialInput input, CalculationResult result)
        {
            string tipoProducto = input.TipoProducto.ToLower();
            string unidadBaseFinal = result.UnidadBaseFinal;
            decimal ivaPercent = input.IVARate * 100m;

            var descripcion = new List<string>();

            // Información básica
            if (input.Cantidad > 1)
            {
                descripcion.Add($"📦 Comprando {input.Cantidad} {tipoProducto}s");
            }
            else
            {
                descripcion.Add($"📦 Comprando 1 {tipoProducto}");
            }

            // Equivalencia
            descripcion.Add($"📏 Cada {tipoProducto} = {input.EquivalenciaNumero} {input.UnidadBase.ToLower()}");

            // Total de unidades base
            descripcion.Add($"📊 Total: {result.TotalUnidadesBase:N2} {unidadBaseFinal}");

            // Precio con información de IVA
            if (input.IncludeIVA)
            {
                descripcion.Add($"💰 Precio con IVA ({ivaPercent:F1}%): ${input.PrecioTotal:N2}");
                descripcion.Add($"💰 Precio sin IVA: ${result.PrecioSinIVA:N2}");
            }
            else
            {
                descripcion.Add($"💰 Precio sin IVA: ${result.PrecioSinIVA:N2}");
                descripcion.Add($"💡 Con IVA ({ivaPercent:F1}%) sería: ${result.PrecioConIVA:N2}");
            }

            // Cálculo final
            descripcion.Add($"🎯 Costo por {unidadBaseFinal}: ${result.PrecioPorUnidadBase:N4}");

            // Análisis CEO
            string analisisCEO = GenerarAnalisisCEO(result, unidadBaseFinal);
            if (!string.IsNullOrEmpty(analisisCEO))
            {
                descripcion.Add(analisisCEO);
            }

            return string.Join("\n", descripcion);
        }

        private static string GenerarAnalisisCEO(CalculationResult result, string unidad)
        {
            if (result.PrecioPorUnidadBase <= 0) return "";

            // Proyecciones de rentabilidad
            decimal margen50 = result.PrecioPorUnidadBase * 1.5m;
            decimal margen100 = result.PrecioPorUnidadBase * 2m;

            return $"💼 ANÁLISIS CEO: Para 50% margen vender a ${margen50:N4}/{unidad}, " +
                   $"para 100% margen a ${margen100:N4}/{unidad}";
        }

        public static List<string> GetTiposProducto()
        {
            return new List<string>
            {
                "Botella", "Envase", "Caja", "Paquete", "Bolsa",
                "Frasco", "Tubo", "Lata", "Sobre", "Unidad"
            };
        }

        public static List<string> GetUnidadesBase()
        {
            return new List<string>
            {
                "Mililitros", "Litros", "Gramos", "Kilos", "Centimetros", "Metros", "Piezas","centimetros"
            };
        }
    }

    public class MaterialInput
    {
        public string TipoProducto { get; set; } = "";
        public decimal EquivalenciaNumero { get; set; }
        public string UnidadBase { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioTotal { get; set; }
        public bool IncludeIVA { get; set; }
        public decimal IVARate { get; set; } = 0.16m; // IVA por defecto 16%
    }

    public class CalculationResult
    {
        public decimal PrecioSinIVA { get; set; }
        public decimal PrecioConIVA { get; set; }
        public decimal PrecioPorUnidad { get; set; }
        public decimal PrecioPorUnidadBase { get; set; }

        // NUEVOS CAMPOS PARA PRECIOS BASE CON/SIN IVA
        public decimal PrecioBaseConIVA { get; set; }
        public decimal PrecioBaseSinIVA { get; set; }

        public string UnidadBaseFinal { get; set; } = "";
        public decimal TotalUnidadesBase { get; set; }
        public string DescripcionCalculo { get; set; } = "";
    }
}