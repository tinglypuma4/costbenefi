using System.Collections.Generic;
using System.Linq;

namespace costbenefi.Helpers
{
    public class UnitsData
    {
        public string Name { get; set; } = "";
        public string BaseUnit { get; set; } = "";
        public decimal ConversionFactor { get; set; } = 1;
        public string Category { get; set; } = "";
        public string Symbol { get; set; } = "";
    }

    public static class UnitsHelper
    {
        public static readonly List<UnitsData> AvailableUnits = new List<UnitsData>
        {
            new UnitsData { Name = "Gramos", BaseUnit = "Gramos", ConversionFactor = 1, Category = "Peso", Symbol = "g" },
            new UnitsData { Name = "Kilogramos", BaseUnit = "Gramos", ConversionFactor = 1000, Category = "Peso", Symbol = "kg" },
            new UnitsData { Name = "Libras", BaseUnit = "Gramos", ConversionFactor = 453.592m, Category = "Peso", Symbol = "lb" },

            new UnitsData { Name = "Mililitros", BaseUnit = "Mililitros", ConversionFactor = 1, Category = "Volumen", Symbol = "ml" },
            new UnitsData { Name = "Litros", BaseUnit = "Mililitros", ConversionFactor = 1000, Category = "Volumen", Symbol = "L" },
            new UnitsData { Name = "Galones", BaseUnit = "Mililitros", ConversionFactor = 3785.41m, Category = "Volumen", Symbol = "gal" },

            new UnitsData { Name = "Unidades", BaseUnit = "Unidades", ConversionFactor = 1, Category = "Cantidad", Symbol = "u" },
            new UnitsData { Name = "Piezas", BaseUnit = "Unidades", ConversionFactor = 1, Category = "Cantidad", Symbol = "pzs" },
            new UnitsData { Name = "Cajas", BaseUnit = "Unidades", ConversionFactor = 1, Category = "Cantidad", Symbol = "cajas" },
            new UnitsData { Name = "Paquetes", BaseUnit = "Unidades", ConversionFactor = 1, Category = "Cantidad", Symbol = "paq" },

            new UnitsData { Name = "Metros", BaseUnit = "Metros", ConversionFactor = 1, Category = "Longitud", Symbol = "m" },
            new UnitsData { Name = "Centímetros", BaseUnit = "Centímetros", ConversionFactor = 1, Category = "Longitud", Symbol = "cm" }
        };

        public static List<string> GetCategories()
        {
            return AvailableUnits.Select(u => u.Category).Distinct().OrderBy(c => c).ToList();
        }

        public static List<UnitsData> GetUnitsByCategory(string category)
        {
            return AvailableUnits.Where(u => u.Category == category).ToList();
        }

        public static UnitsData GetUnitByName(string unitName)
        {
            return AvailableUnits.FirstOrDefault(u => u.Name == unitName);
        }

        public static decimal ConvertToBase(decimal value, string unitName)
        {
            var unit = GetUnitByName(unitName);
            return unit != null ? value * unit.ConversionFactor : value;
        }

        public static decimal ConvertFromBase(decimal baseValue, string unitName)
        {
            var unit = GetUnitByName(unitName);
            return unit != null && unit.ConversionFactor != 0 ? baseValue / unit.ConversionFactor : baseValue;
        }

        public static string GetDisplayFormat(string unitName)
        {
            var unit = GetUnitByName(unitName);
            return unit != null ? $"{unit.Name} ({unit.Symbol})" : unitName;
        }

        public static bool RequiresConversion(string unitName)
        {
            var unit = GetUnitByName(unitName);
            return unit != null && unit.ConversionFactor != 1;
        }
    }
}