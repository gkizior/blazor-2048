using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Blazor2048.Services;

/// <summary>Table of contents produced at build time by tools/DocsBuilder.</summary>
public sealed record DocsIndex(
    [property: JsonPropertyName("sections")] IReadOnlyList<DocsSection> Sections)
{
    public IEnumerable<DocsPage> Pages => Sections.SelectMany(s => s.Pages);

    public DocsPage? Find(string? slug) =>
        Pages.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
}

public sealed record DocsSection(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("pages")] IReadOnlyList<DocsPage> Pages);

public sealed record DocsPage(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("headings")] IReadOnlyList<DocsHeading> Headings,
    [property: JsonPropertyName("level")] int Level = 0,
    [property: JsonPropertyName("parent")] string? Parent = null)
{
    /// <summary>Title with its parent page, for places shown out of the sidebar's context.</summary>
    public string FullTitle => Parent is null ? Title : $"{Parent}: {Title}";
}

public sealed record DocsHeading(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("level")] int Level);

/// <summary>Where the docs UI gets its content. Swapped for a fake in bUnit tests.</summary>
public interface IDocsSource
{
    Task<DocsIndex> GetIndexAsync();
    Task<string> GetPageHtmlAsync(string slug);
}

/// <summary>
/// Loads the pre-rendered docs (wwwroot/docs-content) with HttpClient, so nothing is downloaded
/// until the docs are opened. The service worker caches these files for offline use.
/// </summary>
public sealed class HttpDocsSource(HttpClient http) : IDocsSource
{
    public const string ContentPath = "docs-content/";

    private Task<DocsIndex>? index;
    private readonly Dictionary<string, Task<string>> pages = new(StringComparer.OrdinalIgnoreCase);

    public Task<DocsIndex> GetIndexAsync() =>
        index ??= LoadIndexAsync();

    public Task<string> GetPageHtmlAsync(string slug)
    {
        if (!pages.TryGetValue(slug, out var page))
            pages[slug] = page = http.GetStringAsync($"{ContentPath}{Uri.EscapeDataString(slug)}.html");
        return page;
    }

    private async Task<DocsIndex> LoadIndexAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<DocsIndex>($"{ContentPath}index.json") ?? new DocsIndex([]);
        }
        catch
        {
            index = null; // allow a retry (e.g. when coming back online)
            throw;
        }
    }
}
