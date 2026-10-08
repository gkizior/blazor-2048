using Microsoft.Playwright;

namespace Blazor2048.E2ETests;

/// <summary>Theme, footer, animation and input checks against the published site.</summary>
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
    public async Task No_Console_Errors_On_The_Game()
    {
        var context = await NewContext();
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.Console += (_, msg) => { if (msg.Type == "error") errors.Add(msg.Text); };
        page.PageError += (_, err) => errors.Add(err);

        await page.GotoAsync(Site.BaseUrl);
        await page.Locator(".board .cell").First.WaitForAsync(new() { Timeout = 60_000 });
        await page.Keyboard.PressAsync("ArrowDown");

        Assert.Empty(errors);
    }
}
