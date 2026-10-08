using System.Net;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DocsBuilder;

public sealed record RenderedDoc(string Html, string? Title, IReadOnlyList<DocHeading> Headings);

/// <summary>
/// Markdown to HTML with Markdig, adapted for the in-app docs viewer:
/// - ```mermaid blocks become pre-rendered light/dark SVG images (see <see cref="MermaidRenderer"/>);
/// - links between .md files become in-app routes (docs/{slug}#fragment), which work under any base href;
/// - other repo-relative links point at the file on GitHub; external links open in a new tab;
/// - h2/h3 headings are collected for the "On this page" outline.
/// </summary>
public sealed class MarkdownRenderer(DocsSite site, List<string> warnings, HashSet<string> usedDiagrams)
{
    public const string ContentPath = "docs-content/";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions() // tables, task lists, auto-identifiers (GitHub-style), footnotes, ...
        .Build();

    public static IEnumerable<MermaidDiagram> FindMermaid(string markdown) =>
        Markdown.Parse(markdown, Pipeline).Descendants<FencedCodeBlock>()
            .Where(IsMermaid)
            .Select(b => new MermaidDiagram(b.Lines.ToString()));

    public RenderedDoc Render(string markdown, string source)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        var slug = site.SlugsBySource[source];

        // Mermaid fences -> <figure> with two <img> (light + dark); CSS shows the one for the theme.
        foreach (var block in document.Descendants<FencedCodeBlock>().Where(IsMermaid).ToList())
        {
            var html = new HtmlBlock(null) { };
            html.Lines = new Markdig.Helpers.StringLineGroup(DiagramHtml(new MermaidDiagram(block.Lines.ToString()), source));
            var parent = block.Parent!;
            parent[parent.IndexOf(block)] = html;
        }

        foreach (var link in document.Descendants<LinkInline>())
            RewriteLink(link, source, slug);

        string? title = null;
        var headings = new List<DocHeading>();
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var text = InlineText(heading.Inline);
            if (heading.Level == 1) title ??= text;
            var id = heading.GetAttributes().Id;
            if (heading.Level is 2 or 3 && id is not null) headings.Add(new DocHeading(id, text, heading.Level));
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        return new RenderedDoc(writer.ToString(), title, headings);
    }

    private string DiagramHtml(MermaidDiagram diagram, string source)
    {
        var light = diagram.FileName("light");
        var dark = diagram.FileName("dark");
        if (!File.Exists(Path.Combine(site.DiagramDir, light)) || !File.Exists(Path.Combine(site.DiagramDir, dark)))
        {
            warnings.Add($"{source}: Mermaid diagram {diagram.Hash} has no pre-rendered SVG yet. " +
                         "Run: dotnet run --project tools/DocsBuilder -- render-diagrams --repo .");
            return $"<pre class=\"diagram-missing\"><code class=\"language-mermaid\">{WebUtility.HtmlEncode(diagram.Source)}</code></pre>\n";
        }

        usedDiagrams.Add(light);
        usedDiagrams.Add(dark);
        var alt = WebUtility.HtmlEncode(diagram.Title ?? "Diagram");
        return $"<figure class=\"diagram\" data-diagram=\"{diagram.Hash}\">" +
               $"<img class=\"diagram-light\" src=\"{ContentPath}diagrams/{light}\" alt=\"{alt}\" loading=\"lazy\" decoding=\"async\" />" +
               $"<img class=\"diagram-dark\" src=\"{ContentPath}diagrams/{dark}\" alt=\"{alt}\" loading=\"lazy\" decoding=\"async\" />" +
               "</figure>\n";
    }

    private void RewriteLink(LinkInline link, string source, string slug)
    {
        var url = link.Url;
        if (string.IsNullOrEmpty(url)) return;

        if (Uri.TryCreate(url, UriKind.Absolute, out var abs) && abs.Scheme is "http" or "https" or "mailto")
        {
            if (abs.Scheme != "mailto")
            {
                link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
                link.GetAttributes().AddPropertyIfNotExist("rel", "noopener");
            }
            return;
        }

        // In-page anchors: with <base href> a bare "#x" would point at the app root, so make it explicit.
        if (url.StartsWith('#'))
        {
            link.Url = $"docs/{slug}{url}";
            return;
        }
        if (url.StartsWith('/')) return;

        var hash = url.IndexOf('#');
        var fragment = hash >= 0 ? url[hash..] : "";
        var path = hash >= 0 ? url[..hash] : url;
        var sourceDir = Path.GetDirectoryName(Path.Combine(site.RepoRoot, source))!;
        var full = Path.GetFullPath(Path.Combine(sourceDir, Uri.UnescapeDataString(path)));
        var relative = Path.GetRelativePath(site.RepoRoot, full).Replace('\\', '/');

        if (link.IsImage && relative.StartsWith("docs/images/", StringComparison.OrdinalIgnoreCase) && File.Exists(full))
        {
            link.Url = $"{ContentPath}images/{relative["docs/images/".Length..]}"; // copied into docs-content
        }
        else if (site.SlugsBySource.TryGetValue(relative, out var target))
        {
            link.Url = $"docs/{target}{fragment}";
        }
        else if (site.RepoUrl != "" && !relative.StartsWith("..") && (File.Exists(full) || Directory.Exists(full)))
        {
            var kind = Directory.Exists(full) ? "tree" : link.IsImage ? "raw" : "blob";
            link.Url = $"{site.RepoUrl}/{kind}/main/{relative}{fragment}";
            if (!link.IsImage)
            {
                link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
                link.GetAttributes().AddPropertyIfNotExist("rel", "noopener");
            }
        }
        else if (!File.Exists(full) && !Directory.Exists(full))
        {
            warnings.Add($"{source}: broken link '{url}'.");
        }
    }

    private static bool IsMermaid(FencedCodeBlock block) =>
        string.Equals(block.Info?.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase);

    private static string InlineText(ContainerInline? inline)
    {
        if (inline is null) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var child in inline.Descendants())
        {
            switch (child)
            {
                case LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                case CodeInline code: sb.Append(code.Content); break;
            }
        }
        return sb.ToString().Trim();
    }
}
