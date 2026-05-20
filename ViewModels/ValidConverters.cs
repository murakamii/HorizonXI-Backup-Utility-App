using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HorizonXIBackupApp.ViewModels;

public static class ValidConverters
{
    public static readonly IValueConverter YesNo = new YesNoConverter();
}

public sealed class YesNoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? (b ? "Detected" : "NOT FOUND - check install path in Settings") : "?";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
