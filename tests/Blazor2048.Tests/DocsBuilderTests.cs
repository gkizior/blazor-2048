using System.Text.Json;
using DocsBuilder;

namespace Blazor2048.Tests;

/// <summary>The build-time docs generator: Markdown to HTML, diagrams, links, headings, index.</summary>
public sealed class DocsBuilderTests : IDisposable
{
    private readonly string repo = Path.Combine(Path.GetTempPath(), "docsbuilder-tests-" + Guid.NewGuid().ToString("N")[..8]);

    private const string Diagram = "flowchart LR\n    accTitle: Tiny flow\n    A --> B";

    public DocsBuilderTests()
    {
        Directory.CreateDirectory(Path.Combine(repo, "docs", "diagrams"));
        Directory.CreateDirectory(Path.Combine(repo, "docs", "images"));
        Directory.CreateDirectory(Path.Combine(repo, "src", "bin"));
        Directory.CreateDirectory(Path.Combine(repo, "tools"));
        File.WriteAllText(Path.Combine(repo, "README.md"), "# My Game\n\nSee [the guide](docs/guide.md#setup) and [code](src/Game.cs).\n\n![shot](docs/images/shot.png)\n");
        File.WriteAllText(Path.Combine(repo, "docs", "guide.md"),
            $"# Guide\n\n## Setup\n\nText with `code` and [top](#setup).\n\n### Details\n\n```mermaid\n{Diagram}\n```\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n[ext](https://example.com) [missing](nope.md)\n");
        File.WriteAllText(Path.Combine(repo, "tools", "NOTES.md"), "# Tool notes\n");
        File.WriteAllText(Path.Combine(repo, "src", "bin", "Ignored.md"), "# build output\n");
        File.WriteAllText(Path.Combine(repo, "src", "Game.cs"), "// code");
        File.WriteAllBytes(Path.Combine(repo, "docs", "images", "shot.png"), [1, 2, 3]);
        File.WriteAllText(Path.Combine(repo, "docs", "toc.yml"), "- name: Start\n  items:\n    - name: Welcome\n      href: ../README.md\n    - href: guide.md\n");
    }

    public void Dispose() => Directory.Delete(repo, recursive: true);

    private string Out => Path.Combine(repo, "out");

    private void AddDiagramSvgs()
    {
        var d = new MermaidDiagram(Diagram);
        File.WriteAllText(Path.Combine(repo, "docs", "diagrams", d.FileName("light")), "<svg/>");
        File.WriteAllText(Path.Combine(repo, "docs", "diagrams", d.FileName("dark")), "<svg/>");
    }

    private BuildResult Build() => DocsSite.Load(repo, "https://github.com/me/game").Write(Out);

    [Fact]
    public void Index_Lists_Every_Markdown_File_In_Toc_Order_With_Unlisted_Ones_Under_More()
    {
        Build();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Out, "index.json")));
        var sections = json.RootElement.GetProperty("sections").EnumerateArray().ToList();

        Assert.Equal(["Start", "More"], sections.Select(s => s.GetProperty("title").GetString()));
        var pages = sections.SelectMany(s => s.GetProperty("pages").EnumerateArray()).ToList();
        Assert.Equal(["readme", "guide", "tools-notes"], pages.Select(p => p.GetProperty("slug").GetString()));
        Assert.Equal(["Welcome", "Guide", "Tool notes"], pages.Select(p => p.GetProperty("title").GetString()));
        Assert.Equal("docs/guide.md", pages[1].GetProperty("source").GetString());
        Assert.False(File.Exists(Path.Combine(Out, "src-bin-ignored.html"))); // bin/ and obj/ are skipped
    }

    [Fact]
    public void Collects_H2_And_H3_Headings_For_The_Outline()
    {
        Build();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Out, "index.json")));
        var guide = json.RootElement.GetProperty("sections")[0].GetProperty("pages")[1];

        Assert.Equal(["setup:Setup:2", "details:Details:3"], guide.GetProperty("headings").EnumerateArray()
            .Select(h => $"{h.GetProperty("id").GetString()}:{h.GetProperty("text").GetString()}:{h.GetProperty("level").GetInt32()}"));
    }

    [Fact]
    public void Renders_Markdown_To_Html()
    {
        Build();
        var html = File.ReadAllText(Path.Combine(Out, "guide.html"));

        Assert.Contains("<h1 id=\"guide\">Guide</h1>", html);
        Assert.Contains("<h2 id=\"setup\">Setup</h2>", html);
        Assert.Contains("<code>code</code>", html);
        Assert.Contains("<table>", html);
    }

    [Fact]
    public void Mermaid_Blocks_Become_Light_And_Dark_Images()
    {
        AddDiagramSvgs();
        var result = Build();
        var html = File.ReadAllText(Path.Combine(Out, "guide.html"));
        var d = new MermaidDiagram(Diagram);

        Assert.Contains($"<img class=\"diagram-light\" src=\"docs-content/diagrams/{d.FileName("light")}\" alt=\"Tiny flow\"", html);
        Assert.Contains($"<img class=\"diagram-dark\" src=\"docs-content/diagrams/{d.FileName("dark")}\" alt=\"Tiny flow\"", html);
        Assert.DoesNotContain("language-mermaid", html);
        Assert.True(File.Exists(Path.Combine(Out, "diagrams", d.FileName("dark"))));
        Assert.Equal(1, result.DiagramCount);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("Mermaid"));
    }

    [Fact]
    public void Missing_Diagram_Svg_Falls_Back_To_Source_With_A_Warning()
    {
        var result = Build();
        var html = File.ReadAllText(Path.Combine(Out, "guide.html"));

        Assert.Contains("class=\"diagram-missing\"", html);
        Assert.Contains("A --&gt; B", html);
        Assert.Contains(result.Warnings, w => w.Contains("render-diagrams"));
    }

    [Fact]
    public void Rewrites_Links_For_The_In_App_Viewer()
    {
        var result = Build();
        var readme = File.ReadAllText(Path.Combine(Out, "readme.html"));
        var guide = File.ReadAllText(Path.Combine(Out, "guide.html"));

        Assert.Contains("href=\"docs/guide#setup\"", readme);                                       // doc -> app route
        Assert.Contains("href=\"https://github.com/me/game/blob/main/src/Game.cs\"", readme);        // file -> GitHub
        Assert.Contains("src=\"docs-content/images/shot.png\"", readme);                             // shipped image
        Assert.True(File.Exists(Path.Combine(Out, "images", "shot.png")));
        Assert.Contains("href=\"docs/guide#setup\"", guide);                                        // in-page anchor
        Assert.Contains("href=\"https://example.com\" target=\"_blank\" rel=\"noopener\"", guide);  // external
        Assert.Contains(result.Warnings, w => w.Contains("broken link 'nope.md'"));
    }

    [Fact]
    public void Diagram_Hash_Depends_On_Source_Only_Not_Line_Endings()
    {
        Assert.Equal(new MermaidDiagram("graph TD\r\nA-->B\r\n").Hash, new MermaidDiagram("graph TD\nA-->B").Hash);
        Assert.NotEqual(new MermaidDiagram("graph TD\nA-->B").Hash, new MermaidDiagram("graph TD\nA-->C").Hash);
        Assert.Equal(12, new MermaidDiagram("graph TD").Hash.Length);
    }

    [Theory]
    [InlineData("README.md", "readme")]
    [InlineData("docs/build-and-deploy.md", "build-and-deploy")]
    [InlineData("tests/Blazor2048.E2ETests/README.md", "tests-blazor2048-e2etests-readme")]
    public void Slugs_Are_Url_Friendly(string source, string slug) => Assert.Equal(slug, DocsSite.Slugify(source));

    [Fact]
    public void The_Real_Repo_Docs_Build_Without_Warnings()
    {
        var output = Path.Combine(repo, "real-out");
        var result = DocsSite.Load(RepoPaths.Root, "https://github.com/gkizior/blazor-2048").Write(output);

        Assert.Empty(result.Warnings); // every diagram is pre-rendered, every link resolves
        Assert.True(result.PageCount >= 8);
        foreach (var doc in new[] { "readme", "architecture", "game-engine", "components", "theming-and-animations", "docs-system", "testing", "build-and-deploy" })
            Assert.True(File.Exists(Path.Combine(output, doc + ".html")), $"{doc}.html missing");
        Assert.True(result.DiagramCount >= 8);
    }
}

public class MermaidSvgTests
{
    [Fact]
    public void WithIntrinsicSize_Replaces_Percent_Width_With_The_ViewBox_Size()
    {
        var svg = "<svg id=\"x\" width=\"100%\" style=\"max-width: 488.5px;\" viewBox=\"4 4 488.5 1554.5\"><g/></svg>";

        var result = MermaidRenderer.WithIntrinsicSize(svg);

        Assert.StartsWith("<svg id=\"x\" width=\"489\" height=\"1555\"", result);
        Assert.EndsWith("<g/></svg>", result);
        Assert.Equal(result, MermaidRenderer.WithIntrinsicSize(result)); // idempotent
    }

    [Fact]
    public void Committed_Diagrams_Have_An_Intrinsic_Size_And_No_Embedded_Fonts()
    {
        var files = Directory.GetFiles(Path.Combine(RepoPaths.Root, "docs", "diagrams"), "*.svg");
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var svg = File.ReadAllText(file);
            Assert.DoesNotContain("width=\"100%\"", svg[..Math.Min(400, svg.Length)]);
            Assert.DoesNotContain("@font-face", svg);
        }
    }
}
