using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
