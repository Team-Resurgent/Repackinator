using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Repackinator.Core.Logging;
using System;
using System.Globalization;

namespace Repackinator.Utils
{
    public class LogLevelColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isDarkMode = (Application.Current?.ActualThemeVariant.Key ?? "").Equals("Dark");
            var defaultForeground = isDarkMode ? Brushes.White : Brushes.Black;

            if (value is LogMessageLevel logMessageLevel)
            {
                if (logMessageLevel == LogMessageLevel.Warning)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 255, 192, 0));
                }
                else if (logMessageLevel == LogMessageLevel.Error)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 255, 64, 64));
                }
                else if (logMessageLevel == LogMessageLevel.Skipped)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                }
                else if (logMessageLevel == LogMessageLevel.NotFound)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 64, 64, 255));
                }
                else if (logMessageLevel == LogMessageLevel.Completed || logMessageLevel == LogMessageLevel.Done)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 64, 255, 64));
                }
            }

            return defaultForeground;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
