namespace Blazor2048.Services;

public enum ThemeMode { System, Light, Dark }

/// <summary>
/// Holds the theme preference (System follows prefers-color-scheme), persists it through
/// <see cref="BrowserStorage"/>, and tells the layout when it changes. The layout applies it as
/// <c>data-theme</c> on its root element; CSS custom properties do the rest.
/// </summary>
public sealed class ThemeService(BrowserStorage storage)
{
    public const string Key = "blazor2048.theme";

    private bool loaded;

    public ThemeMode Mode { get; private set; } = ThemeMode.System;

    /// <summary>Lower-case mode name used in markup and storage: system, light or dark.</summary>
    public string ModeName => Name(Mode);

    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (loaded) return;
        loaded = true;
        var stored = await storage.GetAsync(Key);
        if (Enum.TryParse<ThemeMode>(stored, ignoreCase: true, out var mode) && Enum.IsDefined(mode) && mode != Mode)
        {
            Mode = mode;
            Changed?.Invoke();
        }
    }

    public async Task SetAsync(ThemeMode mode)
    {
        loaded = true; // an explicit choice wins over a load still in flight
        if (mode == Mode) return;
        Mode = mode;
        Changed?.Invoke();
        await storage.SetAsync(Key, Name(mode));
    }

    /// <summary>System → Light → Dark → System.</summary>
    public Task CycleAsync() => SetAsync(Next(Mode));

    public static ThemeMode Next(ThemeMode mode) => mode switch
    {
        ThemeMode.System => ThemeMode.Light,
        ThemeMode.Light => ThemeMode.Dark,
        _ => ThemeMode.System,
    };

    public static string Name(ThemeMode mode) => mode.ToString().ToLowerInvariant();
}
