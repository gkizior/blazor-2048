using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DocsBuilder;

public sealed record DocHeading(string Id, string Text, int Level);

/// <param name="Level">0 for top-level TOC items, 1 for items nested under another page.</param>
/// <param name="Parent">Title of the page this one is nested under, if any.</param>
public sealed record DocPage(string Slug, string Title, string Source, IReadOnlyList<DocHeading> Headings,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int Level = 0,
    string? Parent = null);

public sealed record DocSection(string Title, IReadOnlyList<DocPage> Pages);

public sealed record BuildResult(int PageCount, int DiagramCount, IReadOnlyList<string> Warnings);

/// <summary>
/// Every Markdown file in the repo, ordered by docs/toc.yml (DocFX style). Files not listed in the
/// TOC still show up, in a "More" section, so no doc is ever left out. A TOC item can have its own
/// <c>items</c> (one level deep), shown nested under it in the sidebar.
/// </summary>
public sealed class DocsSite
{
    private static readonly string[] IgnoredDirs = ["bin", "obj", "node_modules", ".git", "artifacts", "publish", "TestResults", ".vs", ".idea"];

    public string RepoRoot { get; }
    public string RepoUrl { get; }
    public string DiagramDir => Path.Combine(RepoRoot, "docs", "diagrams");

    /// <summary>Repo-relative source path ("docs/testing.md") to slug ("testing").</summary>
    public IReadOnlyDictionary<string, string> SlugsBySource { get; }

    private readonly List<(string Title, List<TocItem> Items)> layout;

    private DocsSite(string repoRoot, string repoUrl, List<(string Title, List<TocItem> Items)> layout)
    {
        RepoRoot = repoRoot;
        RepoUrl = repoUrl.TrimEnd('/');
        this.layout = layout;
        SlugsBySource = layout.SelectMany(s => s.Items).ToDictionary(i => i.Source, i => Slugify(i.Source), StringComparer.OrdinalIgnoreCase);
        var dupes = SlugsBySource.GroupBy(kv => kv.Value).Where(g => g.Count() > 1).ToList();
        if (dupes.Count > 0)
            throw new InvalidOperationException("Duplicate doc slugs: " + string.Join(", ", dupes.Select(g => g.Key)));
    }

    public static DocsSite Load(string repoRoot, string repoUrl)
    {
        var all = FindMarkdown(repoRoot).ToList();
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sections = new List<(string Title, List<TocItem> Items)>();

        var tocPath = Path.Combine(repoRoot, "docs", "toc.yml");
        if (File.Exists(tocPath))
        {
            var toc = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties().Build()
                .Deserialize<List<TocEntry>>(File.ReadAllText(tocPath)) ?? [];
            foreach (var section in toc)
            {
                var items = new List<TocItem>();
                foreach (var item in section.Items ?? [])
                {
                    if (!Add(item, 0, null)) continue;
                    var parentSource = items[^1].Source;
                    foreach (var child in item.Items ?? [])
                        Add(child, 1, parentSource);
                }

                bool Add(TocEntry item, int level, string? parent)
                {
                    if (item.Href is null) return false;
                    var source = Normalize(Path.GetRelativePath(repoRoot, Path.GetFullPath(Path.Combine(repoRoot, "docs", item.Href))));
                    if (!all.Contains(source, StringComparer.OrdinalIgnoreCase))
                        throw new FileNotFoundException($"docs/toc.yml points at {item.Href}, which doesn't exist.");
                    if (!listed.Add(source)) return false;
                    items.Add(new TocItem(source, item.Name, level, parent));
                    return true;
                }
                if (items.Count > 0) sections.Add((section.Name ?? "Docs", items));
            }
        }

        var rest = all.Where(s => !listed.Contains(s)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).Select(s => new TocItem(s, null, 0, null)).ToList();
        if (rest.Count > 0) sections.Add(("More", rest));

        return new DocsSite(repoRoot, repoUrl, sections);
    }

    /// <summary>All Mermaid diagrams used by the docs.</summary>
    public IEnumerable<MermaidDiagram> Diagrams() =>
        layout.SelectMany(s => s.Items)
            .SelectMany(i => MarkdownRenderer.FindMermaid(File.ReadAllText(Path.Combine(RepoRoot, i.Source))))
            .DistinctBy(d => d.Hash);

    public BuildResult Write(string outDir)
    {
        var warnings = new List<string>();
        var diagrams = new HashSet<string>();
        var renderer = new MarkdownRenderer(this, warnings, diagrams);
        var pages = new List<(DocPage Page, string Html, string Section)>();
        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (sectionTitle, items) in layout)
        foreach (var (source, tocTitle, level, parentSource) in items)
        {
            var markdown = File.ReadAllText(Path.Combine(RepoRoot, source));
            var doc = renderer.Render(markdown, source);
            var title = tocTitle ?? doc.Title ?? Path.GetFileNameWithoutExtension(source);
            titles[source] = title;
            var parent = parentSource is null ? null : titles[parentSource];
            pages.Add((new DocPage(SlugsBySource[source], title, source, doc.Headings, level, parent), doc.Html, sectionTitle));
        }

        // Fresh output every time so deleted docs and diagrams don't linger.
        if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        Directory.CreateDirectory(Path.Combine(outDir, "diagrams"));

        foreach (var (page, html, _) in pages)
            File.WriteAllText(Path.Combine(outDir, page.Slug + ".html"), html);

        // docs/images/** ships with the docs (screenshots, the social preview used by og:image, ...).
        var images = Path.Combine(RepoRoot, "docs", "images");
        if (Directory.Exists(images))
            foreach (var file in Directory.EnumerateFiles(images, "*", SearchOption.AllDirectories))
            {
                var dest = Path.Combine(outDir, "images", Path.GetRelativePath(images, file));
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest);
            }

        foreach (var file in diagrams)
            File.Copy(Path.Combine(DiagramDir, file), Path.Combine(outDir, "diagrams", file));

        var index = new
        {
            sections = pages.GroupBy(p => p.Section).Select(g => new { title = g.Key, pages = g.Select(p => p.Page) }),
        };
        File.WriteAllText(Path.Combine(outDir, "index.json"), JsonSerializer.Serialize(index, JsonOptions));

        return new BuildResult(pages.Count, diagrams.Count / 2, warnings);
    }

    public static string Slugify(string source)
    {
        var path = Normalize(source);
        if (path.StartsWith("docs/", StringComparison.OrdinalIgnoreCase)) path = path[5..];
        path = Path.ChangeExtension(path, null) ?? path;
        var slug = new string(path.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static IEnumerable<string> FindMarkdown(string root)
    {
        var stack = new Stack<string>([root]);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
                yield return Normalize(Path.GetRelativePath(root, file));
            foreach (var sub in Directory.EnumerateDirectories(dir))
                if (!IgnoredDirs.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase))
                    stack.Push(sub);
        }
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    /// <summary>A page in the sidebar; <c>ParentSource</c> is set for nested items.</summary>
    private sealed record TocItem(string Source, string? Title, int Level, string? ParentSource);

    private sealed class TocEntry
    {
        public string? Name { get; set; }
        public string? Href { get; set; }
        public List<TocEntry>? Items { get; set; }
    }
}
