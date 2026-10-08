using Microsoft.Playwright;

namespace Blazor2048.E2ETests;

/// <summary>Theme, docs, animation and input checks against the published site.</summary>
public class FeatureE2ETests(SiteServer site) : AppTest(site)
{
    private static Task<string> ThemeAsync(IPage page) => page.Locator(".app-root").GetAttributeAsync("data-theme")!;

    private static Task<string> BackgroundAsync(IPage page) =>
        page.EvaluateAsync<string>("getComputedStyle(document.querySelector('.app-root')).backgroundColor");

    private const string LightBg = "rgb(246, 243, 238)";
    private const string DarkBg = "rgb(18, 19, 24)";

    [E2EFact]
    public async Task Dark_Mode_Toggle_Applies_And_Persists_Across_Reload()
    {
        var page = await OpenAsync(new BrowserNewContextOptions { ColorScheme = ColorScheme.Light });
        Assert.Equal("system", await ThemeAsync(page));
        Assert.Equal(LightBg, await BackgroundAsync(page));

        await page.Locator(".theme-toggle").ClickAsync(); // light
        await page.Locator(".theme-toggle").ClickAsync(); // dark
        await Expect(page.Locator(".app-root")).ToHaveAttributeAsync("data-theme", "dark");
        await page.WaitForFunctionAsync($"getComputedStyle(document.querySelector('.app-root')).backgroundColor === '{DarkBg}'");
        Assert.Equal("dark", await page.EvaluateAsync<string>("localStorage.getItem('blazor2048.theme')"));
        // Tiles switch palette too (a 2 or 4 tile is dark-on-dark in the dark theme).
        var tileBg = await page.EvaluateAsync<string>("getComputedStyle(document.querySelector('.tile:not(.tile-retired) .tile-inner')).backgroundColor");
        Assert.Contains(tileBg, new[] { "rgb(62, 69, 86)", "rgb(80, 89, 113)" });

        await page.ReloadAsync();
        await page.Locator(".board .cell").First.WaitForAsync(new() { Timeout = 60_000 });
        await Expect(page.Locator(".app-root")).ToHaveAttributeAsync("data-theme", "dark");
        await page.WaitForFunctionAsync($"getComputedStyle(document.querySelector('.app-root')).backgroundColor === '{DarkBg}'");
        await Expect(page.Locator("meta[name=theme-color]")).ToHaveAttributeAsync("content", "#121318");
    }

    [E2EFact]
    public async Task System_Theme_Follows_Prefers_Color_Scheme()
    {
        var page = await OpenAsync(new BrowserNewContextOptions { ColorScheme = ColorScheme.Dark });

        Assert.Equal("system", await ThemeAsync(page));
        Assert.Equal(DarkBg, await BackgroundAsync(page));
    }

    [E2EFact]
    public async Task Footer_Shows_Name_Runtime_And_Commit_Link()
    {
        var page = await OpenAsync();
        var footer = page.Locator("footer.app-footer");

        await Expect(footer).ToContainTextAsync("Garrett Kizior");
        await Expect(footer).ToContainTextAsync(".NET 10");
        var sha = page.Locator("a.footer-sha");
        Assert.Matches("^[0-9a-f]{7}$", (await sha.InnerTextAsync()).Trim());
        Assert.Matches("^https://github.com/gkizior/blazor-2048/commit/[0-9a-f]{40}$", await sha.GetAttributeAsync("href"));
    }

    [E2EFact]
    public async Task Docs_Open_And_Navigate_To_A_Rendered_Mermaid_Diagram()
    {
        var page = await OpenAsync(new BrowserNewContextOptions { ColorScheme = ColorScheme.Light });
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowUp");
        var boardBefore = await BoardAsync(page);

        await page.Locator("a.docs-btn").ClickAsync();
        await page.WaitForURLAsync("**/docs");
        await Expect(page.Locator(".markdown-body h1")).ToBeVisibleAsync();
        Assert.True(await page.Locator(".toc-link").CountAsync() >= 8);

        await page.Locator(".toc-link", new() { HasText = "Architecture" }).ClickAsync();
        await page.WaitForURLAsync("**/docs/architecture");
        await Expect(page.Locator(".markdown-body h1")).ToHaveTextAsync("Architecture");

        var diagram = page.Locator("figure.diagram img.diagram-light").First;
        await Expect(diagram).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => { const i = document.querySelector('figure.diagram img.diagram-light'); return i && i.complete && i.naturalWidth > 0; }");
        Assert.False(await page.Locator("figure.diagram img.diagram-dark").First.IsVisibleAsync());

        // The SVG is a real Mermaid render served as a static file.
        var src = await diagram.EvaluateAsync<string>("i => i.src"); // absolute, resolved against <base href>
        Assert.StartsWith(Site.BaseUrl + "docs-content/diagrams/", src);
        var svg = await page.APIRequest.GetAsync(src);
        Assert.True(svg.Ok);
        Assert.Contains("<svg", await svg.TextAsync());

        // Switching to dark swaps in the dark diagram.
        await page.Locator(".docs-topbar .theme-toggle").ClickAsync(); // light
        await page.Locator(".docs-topbar .theme-toggle").ClickAsync(); // dark
        await Expect(page.Locator("figure.diagram img.diagram-dark").First).ToBeVisibleAsync();

        // Back to the game: same board (the game survives the trip).
        await page.Locator("a.back-to-game").ClickAsync();
        await page.Locator(".board .cell").First.WaitForAsync();
        Assert.Equal(boardBefore, await BoardAsync(page));
    }

    [E2EFact]
    public async Task Docs_Deep_Link_Works_And_Links_Between_Docs_Navigate()
    {
        var context = await NewContext();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Site.BaseUrl + "docs/game-engine");

        await Expect(page.Locator(".markdown-body h1")).ToHaveTextAsync("Game engine", new() { Timeout = 60_000 });
        await Expect(page.Locator(".toc-link.active")).ToHaveTextAsync("Game engine");
        Assert.True(await page.Locator(".docs-outline a").CountAsync() >= 3);

        await page.Locator(".markdown-body a[href='docs/testing']").First.ClickAsync();
        await page.WaitForURLAsync("**/docs/testing");
        await Expect(page.Locator(".markdown-body h1")).ToHaveTextAsync("Testing strategy");
    }

    [E2EFact]
    public async Task Tiles_Slide_By_Transform_On_The_Same_Element()
    {
        var page = await OpenAsync();
        var tile = page.Locator(".tile:not(.tile-retired)").First;
        Assert.Equal("0.11s", await tile.EvaluateAsync<string>("t => getComputedStyle(t).transitionDuration"));
        Assert.Equal("transform", await tile.EvaluateAsync<string>("t => getComputedStyle(t).transitionProperty"));

        // Tag every tile element, move, then check that surviving tiles kept their DOM node.
        await page.EvaluateAsync("document.querySelectorAll('.tile').forEach(t => t.__tag = t.dataset.id)");
        foreach (var key in new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown" })
        {
            var before = await page.Locator(".board").GetAttributeAsync("data-moves");
            await page.Keyboard.PressAsync(key);
            await page.WaitForTimeoutAsync(50);
            if (await page.Locator(".board").GetAttributeAsync("data-moves") != before) break;
        }
        var kept = await page.EvaluateAsync<int>(
            "[...document.querySelectorAll('.tile')].filter(t => t.__tag !== undefined && t.__tag === t.dataset.id).length");
        Assert.True(kept >= 1, "No tile element was reused across the move.");
    }

    [E2EFact]
    public async Task Rapid_Key_Presses_During_Animations_Are_Not_Dropped()
    {
        var page = await OpenAsync();
        var board = page.Locator(".board");
        var start = int.Parse((await board.GetAttributeAsync("data-moves"))!);

        // A fresh board has a few small tiles, so alternating left/right always changes it.
        // No waits between presses: every one lands mid-animation.
        foreach (var key in new[] { "ArrowLeft", "ArrowRight", "ArrowLeft", "ArrowRight" })
            await page.Keyboard.PressAsync(key);

        await Expect(board).ToHaveAttributeAsync("data-moves", (start + 4).ToString(), new() { Timeout = 2_000 });
    }

    [E2EFact]
    public async Task Reduced_Motion_Turns_Off_Tile_Animations()
    {
        var page = await OpenAsync(new BrowserNewContextOptions { ReducedMotion = ReducedMotion.Reduce });

        var tile = page.Locator(".tile").First;
        Assert.Equal("0s", await tile.EvaluateAsync<string>("t => getComputedStyle(t).transitionDuration"));
        Assert.Equal("none", await tile.EvaluateAsync<string>("t => getComputedStyle(t.querySelector('.tile-inner')).animationName"));
    }

    [E2EFact]
    public async Task No_Console_Errors_On_Game_And_Docs()
    {
        var context = await NewContext();
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.Console += (_, msg) => { if (msg.Type == "error") errors.Add(msg.Text); };
        page.PageError += (_, err) => errors.Add(err);

        await page.GotoAsync(Site.BaseUrl);
        await page.Locator(".board .cell").First.WaitForAsync(new() { Timeout = 60_000 });
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Locator("a.docs-btn").ClickAsync();
        await page.Locator(".toc-link", new() { HasText = "Theming and animations" }).ClickAsync();
        await Expect(page.Locator("figure.diagram").First).ToBeVisibleAsync();

        Assert.Empty(errors);
    }
}
