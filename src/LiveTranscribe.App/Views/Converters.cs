using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LiveTranscribe.App.Views;

/// <summary>Maps the mic level (0–1) to a vertical scale factor so the waveform reacts to volume.</summary>
public sealed class LevelToScaleConverter : IValueConverter
{
    public double Min { get; set; } = 0.25;
    public double Max { get; set; } = 1.0;

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value is double d ? d : 0;
        if (level < 0) level = 0;
        if (level > 1) level = 1;
        return Min + (Max - Min) * level;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

/// <summary>True → Collapsed, False → Visible. The inverse of BooleanToVisibilityConverter.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

/// <summary>Visible only when the bound string is non-empty (used for the live transcript line).</summary>
public sealed class NonEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
