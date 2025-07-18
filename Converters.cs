using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace costbenefi.Converters
{
    /// <summary>
    /// Convierte un valor booleano de stock bajo a color de fondo
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasLowStock)
            {
                return hasLowStock
                    ? new SolidColorBrush(Color.FromRgb(220, 53, 69))   // Rojo para stock bajo
                    : new SolidColorBrush(Color.FromRgb(40, 167, 69));  // Verde para stock normal
            }

            return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gris por defecto
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un valor booleano de stock bajo a texto descriptivo
    /// </summary>
    public class BoolToStockTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasLowStock)
            {
                return hasLowStock ? "BAJO" : "OK";
            }

            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un valor decimal a color según el rango (para valores monetarios)
    /// </summary>
    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                if (amount <= 0)
                    return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gris para cero o negativo
                else if (amount < 100)
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69));   // Verde para valores bajos
                else if (amount < 1000)
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7));   // Amarillo para valores medios
                else
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69));   // Rojo para valores altos
            }

            return new SolidColorBrush(Color.FromRgb(108, 117, 125));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convierte un porcentaje de IVA a texto formateado
    /// </summary>
    public class PercentageToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal percentage)
            {
                if (percentage <= 0)
                    return "0%";
                else
                    return $"{percentage:F1}%";
            }

            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}