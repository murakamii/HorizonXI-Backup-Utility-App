using Avalonia;
using Avalonia.Styling;

namespace HorizonXIBackupApp.Services;

public static class ThemeService
{
    public const string Dark = "Dark";
    public const string Light = "Light";
    public const string System = "System";

    public static readonly string[] Options = { Dark, Light, System };

    public static void Apply(string? themeName)
    {
        var app = Application.Current;
        if (app is null) return;
        app.RequestedThemeVariant = themeName switch
        {
            Light => ThemeVariant.Light,
            System => ThemeVariant.Default,
            _ => ThemeVariant.Dark,   // Dark is the default for null/unknown
        };
    }
}
