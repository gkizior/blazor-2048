using Blazor2048.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor2048.ComponentTests;

public class DocsPageTests : AppTestContext
{
    private IRenderedComponent<Docs> RenderDocs(string? slug = null) =>
        Render<Docs>(p => p.Add(d => d.Slug, slug));

    [Fact]
    public void Lists_Every_Doc_In_The_Sidebar_Grouped_By_Section()
    {
        var cut = RenderDocs();

        Assert.Equal(["Overview", "Engineering"], cut.FindAll(".toc-title").Select(t => t.TextContent).Where(t => t != "On this page"));
        Assert.Equal(["Introduction", "Architecture", "Testing strategy"], cut.FindAll(".toc-link").Select(a => a.TextContent));
        Assert.Equal(["docs/readme", "docs/architecture", "docs/testing"], cut.FindAll(".toc-link").Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void Opens_The_First_Doc_By_Default_And_Renders_Its_Html()
    {
        var cut = RenderDocs();

        Assert.Equal("Blazor 2048", cut.Find(".markdown-body h1").TextContent);
        Assert.Equal("text", cut.Find(".markdown-body strong").TextContent);
        Assert.Equal("page", cut.Find(".toc-link.active").GetAttribute("aria-current"));
        Assert.Equal("Introduction", cut.Find(".toc-link.active").TextContent);
    }

    [Fact]
    public void Renders_A_Doc_With_Diagrams_Breadcrumb_Outline_And_Pager()
    {
        var cut = RenderDocs("architecture");

        Assert.Contains("Overview", cut.Find(".docs-breadcrumb").TextContent);
        var images = cut.FindAll(".markdown-body figure.diagram img");
        Assert.Equal(["docs-content/diagrams/abc-light.svg", "docs-content/diagrams/abc-dark.svg"], images.Select(i => i.GetAttribute("src")));
        Assert.Equal(["docs/architecture#projects", "docs/architecture#runtime-view"],
            cut.FindAll(".docs-outline a").Select(a => a.GetAttribute("href")));
        Assert.Equal("docs/readme", cut.Find(".pager-link.prev").GetAttribute("href"));
        Assert.Equal("docs/testing", cut.Find(".pager-link.next").GetAttribute("href"));
        Assert.Equal("https://github.com/gkizior/blazor-2048/blob/main/docs/architecture.md", cut.Find(".docs-source").GetAttribute("href"));
    }

    [Fact]
    public void Navigating_To_Another_Doc_Loads_It()
    {
        var cut = RenderDocs("readme");

        cut.Render(p => p.Add(d => d.Slug, "testing"));

        Assert.Equal("dotnet test", cut.Find(".markdown-body pre code").TextContent);
        Assert.Equal(["readme", "testing"], DocsSource.Requested);
    }

    [Fact]
    public void Filter_Narrows_The_Toc_By_Title_Or_Heading()
    {
        var cut = RenderDocs();

        cut.Find(".docs-filter input").Input("runtime"); // a heading in Architecture
        Assert.Equal(["Architecture"], cut.FindAll(".toc-link").Select(a => a.TextContent));

        cut.Find(".docs-filter input").Input("TEST");
        Assert.Equal(["Testing strategy"], cut.FindAll(".toc-link").Select(a => a.TextContent));

        cut.Find(".docs-filter input").Input("");
        Assert.Equal(3, cut.FindAll(".toc-link").Count);
    }

    [Fact]
    public void Unknown_Slug_Shows_Not_Found()
    {
        var cut = RenderDocs("nope");

        Assert.Contains("Page not found", cut.Find(".docs-message.not-found").TextContent);
        Assert.Equal(3, cut.FindAll(".toc-link").Count);
    }

    [Fact]
    public void Menu_Button_Toggles_The_Drawer()
    {
        var cut = RenderDocs();
        Assert.DoesNotContain("menu-open", cut.Find(".docs").ClassName);

        cut.Find(".docs-menu-btn").Click();
        Assert.Contains("menu-open", cut.Find(".docs").ClassName);
        Assert.Equal("true", cut.Find(".docs-menu-btn").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Has_Back_To_Game_Link_And_Theme_Toggle()
    {
        var cut = RenderDocs();

        Assert.Equal("./", cut.Find("a.back-to-game").GetAttribute("href"));
        Assert.NotNull(cut.Find(".docs-topbar .theme-toggle"));
    }

    [Fact]
    public void Shows_An_Error_When_Docs_Fail_To_Load()
    {
        Services.AddSingleton<Blazor2048.Services.IDocsSource>(new FailingDocs());

        var cut = RenderDocs();

        Assert.Contains("Couldn't load the docs", cut.Find(".docs-message.error").TextContent);
    }

    private sealed class FailingDocs : Blazor2048.Services.IDocsSource
    {
        public Task<Blazor2048.Services.DocsIndex> GetIndexAsync() => throw new HttpRequestException("offline");
        public Task<string> GetPageHtmlAsync(string slug) => throw new HttpRequestException("offline");
    }
}
