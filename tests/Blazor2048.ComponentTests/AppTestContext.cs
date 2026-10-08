using Blazor2048.Services;
using Bunit;
using Game2048.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor2048.ComponentTests;

/// <summary>bUnit context with the app's services registered the way Program.cs does.</summary>
public abstract class AppTestContext : BunitContext
{
    protected Game Game { get; } = new(4, new Random(7));

    protected static readonly BuildInfo TestBuild = new(
        Author: "Garrett Kizior",
        Version: "1.2.3",
        Runtime: ".NET 10.0.0",
        Commit: "0123456789abcdef0123456789abcdef01234567",
        BuildDate: "2026-10-08T01:02:03Z",
        RepositoryUrl: "https://github.com/gkizior/blazor-2048");

    protected AppTestContext()
    {
        // FocusAsync and localStorage go through JS interop; answer them without a browser.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<BrowserStorage>();
        Services.AddScoped<BestScoreStore>();
        Services.AddScoped<ThemeService>();
        Services.AddSingleton(Game);
        Services.AddSingleton(TestBuild);
    }
}

