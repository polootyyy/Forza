using System.Globalization;
using Polootyyy.Models;

namespace Polootyyy.Helpers;

public class LogLevelColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LogLevel level ? level switch
        {
            LogLevel.Success => Colors.LimeGreen,
            LogLevel.Warning => Colors.Orange,
            LogLevel.Error => Colors.Red,
            _ => Color.FromArgb("#A0A0B0")
        } : Color.FromArgb("#A0A0B0");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#FF2D55") : Color.FromArgb("#3A3A5C");
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "ON" : "OFF";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
