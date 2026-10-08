using Blazor2048.Services;
using Bunit;
using Game2048.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor2048.ComponentTests;

/// <summary>bUnit context with the app's services registered the way Program.cs does.</summary>
public abstract class AppTestContext : BunitContext
{
    protected Game Game { get; } = new(4, new Random(7));

    protected FakeDocsSource DocsSource { get; } = new();

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
        Services.AddSingleton<IDocsSource>(DocsSource);
    }
}

/// <summary>In-memory docs so the docs page can be tested without HTTP.</summary>
public sealed class FakeDocsSource : IDocsSource
{
    public DocsIndex Index { get; set; } = new([
        new DocsSection("Overview", [
            new DocsPage("readme", "Introduction", "README.md", [new DocsHeading("features", "Features", 2)]),
            new DocsPage("architecture", "Architecture", "docs/architecture.md",
                [new DocsHeading("projects", "Projects", 2), new DocsHeading("runtime-view", "Runtime view", 2)]),
        ]),
        new DocsSection("Engineering", [
            new DocsPage("testing", "Testing strategy", "docs/testing.md", [new DocsHeading("e2e", "End-to-end tests", 2)]),
        ]),
    ]);

    public Dictionary<string, string> Html { get; } = new()
    {
        ["readme"] = "<h1 id=\"blazor-2048\">Blazor 2048</h1><p>Intro <strong>text</strong>.</p>",
        ["architecture"] = "<h1 id=\"architecture\">Architecture</h1><h2 id=\"projects\">Projects</h2>" +
                           "<figure class=\"diagram\" data-diagram=\"abc\"><img class=\"diagram-light\" src=\"docs-content/diagrams/abc-light.svg\" alt=\"Component tree\" />" +
                           "<img class=\"diagram-dark\" src=\"docs-content/diagrams/abc-dark.svg\" alt=\"Component tree\" /></figure>",
        ["testing"] = "<h1 id=\"testing\">Testing strategy</h1><pre><code class=\"language-bash\">dotnet test</code></pre>",
    };

    public List<string> Requested { get; } = [];

    public Task<DocsIndex> GetIndexAsync() => Task.FromResult(Index);

    public Task<string> GetPageHtmlAsync(string slug)
    {
        Requested.Add(slug);
        return Task.FromResult(Html[slug]);
    }
}
