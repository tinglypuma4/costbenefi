using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using costbenefi.Models;

namespace costbenefi
{
    /// <summary>
    /// Helper class optimizado para identificar y manejar tipos de objetos en el sistema POS
    /// Permite distinguir entre RawMaterial y ServicioVenta en templates universales
    /// Incluye converters específicos para el sistema de ventas
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Converter principal para verificar tipos en XAML
        /// Uso: Converter={x:Static local:TypeHelper.IsTypeConverter}, ConverterParameter="RawMaterial"
        /// </summary>
        public static IsTypeConverter IsTypeConverter { get; } = new IsTypeConverter();

        /// <summary>
        /// Converter para obtener nombres descriptivos de tipos
        /// Uso: Converter={x:Static local:TypeHelper.TypeNameConverter}
        /// </summary>
        public static TypeNameConverter TypeNameConverter { get; } = new TypeNameConverter();

        /// <summary>
        /// Converter para obtener iconos según el tipo
        /// Uso: Converter={x:Static local:TypeHelper.TypeIconConverter}
        /// </summary>
        public static TypeIconConverter TypeIconConverter { get; } = new TypeIconConverter();

        /// <summary>
        /// Converter para obtener colores según el tipo
        /// Uso: Converter={x:Static local:TypeHelper.TypeColorConverter}
        /// </summary>
        public static TypeColorConverter TypeColorConverter { get; } = new TypeColorConverter();

        /// <summary>
        /// Converter específico para precios (maneja tanto productos como servicios)
        /// Uso: Converter={x:Static local:TypeHelper.PriceConverter}
        /// </summary>
        public static PriceConverter PriceConverter { get; } = new PriceConverter();

        /// <summary>
        /// Converter para disponibilidad (stock para productos, disponibilidad para servicios)
        /// Uso: Converter={x:Static local:TypeHelper.AvailabilityConverter}
        /// </summary>
        public static AvailabilityConverter AvailabilityConverter { get; } = new AvailabilityConverter();

        // ===== MÉTODOS HELPER ESTÁTICOS =====

        /// <summary>
        /// Verifica si un objeto es un producto (RawMaterial)
        /// </summary>
        public static bool IsProduct(object item)
        {
            return item is RawMaterial;
        }

        /// <summary>
        /// Verifica si un objeto es un servicio (ServicioVenta)
        /// </summary>
        public static bool IsService(object item)
        {
            return item is ServicioVenta;
        }

        /// <summary>
        /// Obtiene el precio de venta de cualquier item (producto o servicio)
        /// </summary>
        public static decimal GetSalePrice(object item)
        {
            return item switch
            {
                RawMaterial producto => producto.PrecioVentaFinal,
                ServicioVenta servicio => servicio.PrecioServicio,
                _ => 0
            };
        }

        /// <summary>
        /// Obtiene el nombre del item (producto o servicio)
        /// </summary>
        public static string GetItemName(object item)
        {
            return item switch
            {
                RawMaterial producto => producto.NombreArticulo,
                ServicioVenta servicio => servicio.NombreServicio,
                _ => "Item desconocido"
            };
        }

        /// <summary>
        /// Obtiene la categoría del item
        /// </summary>
        public static string GetItemCategory(object item)
        {
            return item switch
            {
                RawMaterial producto => producto.Categoria,
                ServicioVenta servicio => servicio.CategoriaServicio,
                _ => "Sin categoría"
            };
        }

        /// <summary>
        /// Verifica si el item está disponible para venta
        /// </summary>
        public static bool IsAvailableForSale(object item)
        {
            return item switch
            {
                RawMaterial producto => producto.ActivoParaVenta && producto.StockTotal > 0,
                ServicioVenta servicio => servicio.DisponibleParaVenta,
                _ => false
            };
        }
    }

    /// <summary>
    /// Converter principal que determina si un objeto es de un tipo específico
    /// Optimizado para el sistema POS con mejor manejo de errores
    /// </summary>
    public class IsTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            try
            {
                string expectedTypeName = parameter.ToString();
                string actualTypeName = value.GetType().Name;

                // Verificar coincidencia exacta
                bool isMatch = string.Equals(actualTypeName, expectedTypeName, StringComparison.OrdinalIgnoreCase);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[TypeHelper] Objeto: {actualTypeName}, Esperado: {expectedTypeName}, Match: {isMatch}");
#endif

                return isMatch;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[TypeHelper] Error: {ex.Message}");
#endif
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack no está implementado para IsTypeConverter");
        }
    }

    /// <summary>
    /// Converter para obtener nombres descriptivos y amigables de tipos
    /// Especialmente útil para la interfaz de usuario del POS
    /// </summary>
    public class TypeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "Sin información";

            string typeName = value.GetType().Name;

            // Nombres específicos para el sistema POS
            return typeName switch
            {
                "RawMaterial" => "📦 Producto",
                "ServicioVenta" => "🛍️ Servicio",
                "DetalleVenta" => "🧾 Detalle de Venta",
                "Venta" => "💰 Venta",
                "Movimiento" => "📋 Movimiento",
                "MaterialServicio" => "🔧 Material de Servicio",
                _ => $"📄 {typeName}"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para obtener iconos específicos según el tipo de item
    /// Útil para mostrar iconos consistentes en toda la aplicación
    /// </summary>
    public class TypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "❓";

            return value switch
            {
                RawMaterial producto => producto.ActivoParaVenta ? "📦" : "📦🚫",
                ServicioVenta servicio => servicio.Activo ? "🛍️" : "🛍️🚫",
                DetalleVenta detalle => detalle.EsProducto ? "📦" : "🛍️",
                _ => "📄"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para obtener colores específicos según el tipo y estado
    /// Proporciona retroalimentación visual inmediata sobre el estado de los items
    /// </summary>
    public class TypeColorConverter : IValueConverter
    {
        // Colores predefinidos para consistencia visual
        private static readonly SolidColorBrush ProductColor = new SolidColorBrush(Color.FromRgb(34, 197, 94));      // Verde
        private static readonly SolidColorBrush ServiceColor = new SolidColorBrush(Color.FromRgb(59, 130, 246));     // Azul
        private static readonly SolidColorBrush InactiveColor = new SolidColorBrush(Color.FromRgb(156, 163, 175));   // Gris
        private static readonly SolidColorBrush WarningColor = new SolidColorBrush(Color.FromRgb(245, 158, 11));     // Amarillo
        private static readonly SolidColorBrush ErrorColor = new SolidColorBrush(Color.FromRgb(239, 68, 68));        // Rojo

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return InactiveColor;

            return value switch
            {
                RawMaterial producto => GetProductColor(producto),
                ServicioVenta servicio => GetServiceColor(servicio),
                DetalleVenta detalle => detalle.EsProducto ? ProductColor : ServiceColor,
                _ => InactiveColor
            };
        }

        private static SolidColorBrush GetProductColor(RawMaterial producto)
        {
            if (!producto.ActivoParaVenta) return InactiveColor;
            if (producto.StockTotal <= 0) return ErrorColor;
            if (producto.StockTotal <= producto.AlertaStockBajo) return WarningColor;
            return ProductColor;
        }

        private static SolidColorBrush GetServiceColor(ServicioVenta servicio)
        {
            if (!servicio.Activo) return InactiveColor;
            if (!servicio.DisponibleParaVenta) return ErrorColor;
            if (servicio.StockDisponible <= 1) return WarningColor;
            return ServiceColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter específico para precios que maneja tanto productos como servicios
    /// Incluye formateo consistente y manejo de diferentes tipos de precios
    /// </summary>
    public class PriceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "$0.00";

            try
            {
                decimal price = value switch
                {
                    RawMaterial producto => GetProductPrice(producto, parameter?.ToString()),
                    ServicioVenta servicio => GetServicePrice(servicio, parameter?.ToString()),
                    decimal directPrice => directPrice,
                    _ => 0
                };

                return price.ToString("C2", culture);
            }
            catch
            {
                return "$0.00";
            }
        }

        private static decimal GetProductPrice(RawMaterial producto, string priceType)
        {
            return priceType switch
            {
                "Costo" => producto.PrecioConIVA,
                "Venta" => producto.PrecioVentaFinal,
                "Base" => producto.PrecioVenta,
                "Descuento" => producto.PrecioDescuento,
                _ => producto.PrecioVentaFinal
            };
        }

        private static decimal GetServicePrice(ServicioVenta servicio, string priceType)
        {
            return priceType switch
            {
                "Costo" => servicio.CostoTotal,
                "Base" => servicio.PrecioBase,
                "Venta" => servicio.PrecioServicio,
                "Materiales" => servicio.CostoMateriales,
                "ManoObra" => servicio.CostoManoObra,
                _ => servicio.PrecioServicio
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para mostrar disponibilidad (stock/cantidad disponible)
    /// Maneja tanto productos como servicios con formato apropiado
    /// </summary>
    public class AvailabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "No disponible";

            return value switch
            {
                RawMaterial producto => GetProductAvailability(producto),
                ServicioVenta servicio => GetServiceAvailability(servicio),
                _ => "N/A"
            };
        }

        private static string GetProductAvailability(RawMaterial producto)
        {
            if (!producto.ActivoParaVenta) return "Inactivo";

            var stock = producto.StockTotal;
            var unidad = producto.UnidadMedida;

            if (stock <= 0) return "Agotado";
            if (stock <= producto.AlertaStockBajo) return $"⚠️ {stock:F2} {unidad}";

            return $"{stock:F2} {unidad}";
        }

        private static string GetServiceAvailability(ServicioVenta servicio)
        {
            if (!servicio.Activo) return "Inactivo";
            if (!servicio.DisponibleParaVenta) return "No disponible";

            var stock = servicio.StockDisponible;

            if (stock <= 0) return "No disponible";
            if (stock <= 5) return $"⚠️ Limitado ({stock})";
            if (stock >= 999) return "Disponible";

            return $"Disponible ({stock})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter para visibilidad condicional basada en tipo
    /// Útil para mostrar/ocultar elementos según el tipo de item
    /// </summary>
    public class TypeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string expectedType = parameter.ToString();
            string actualType = value.GetType().Name;

            bool isMatch = string.Equals(actualType, expectedType, StringComparison.OrdinalIgnoreCase);

            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}