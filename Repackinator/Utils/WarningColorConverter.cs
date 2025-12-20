using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Repackinator.Utils
{
    public class WarningColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isDarkMode = (Application.Current?.ActualThemeVariant.Key ?? "").Equals("Dark");
            var defaultForeground = isDarkMode ? Brushes.White : Brushes.Black;
            if (value is bool boolValue)
            {
                return boolValue ? defaultForeground : Brushes.Red;
            }
            return defaultForeground;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color == Colors.Red;
            }
            return false;
        }
    }
}
