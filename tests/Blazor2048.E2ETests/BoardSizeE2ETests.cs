using Microsoft.Playwright;

namespace Blazor2048.E2ETests;

/// <summary>Board sizes end to end (specs/011-board-sizes): presets, custom, validation, persistence, smoothness.</summary>
public class BoardSizeE2ETests(SiteServer site) : AppTest(site)
{
    private static readonly (int Size, string Name)[] Presets =
        [(5, "4096"), (6, "8192"), (7, "16384"), (8, "32768"), (9, "65536"), (10, "131072"), (4, "2048")];

    private static async Task PickAsync(IPage page, string size)
    {
        await page.Locator("#size-menu-button").ClickAsync();
        await page.Locator($"#size-menu [data-size='{size}']").ClickAsync();
    }

    private static Task SeedSizeAsync(IBrowserContext context, int size) =>
        context.AddInitScriptAsync($"if (!sessionStorage.getItem('seeded')) {{ localStorage.setItem('blazor2048.size', '{size}'); sessionStorage.setItem('seeded', '1'); }}");

    private static Task<bool[]> FitsAsync(IPage page) => page.EvaluateAsync<bool[]>(@"() => {
        const d = document.documentElement, b = document.querySelector('.board').getBoundingClientRect();
        const t = document.querySelector('.top').getBoundingClientRect();
        return [d.scrollHeight <= innerHeight, d.scrollWidth <= innerWidth,
                b.bottom <= innerHeight && b.right <= innerWidth && b.left >= 0, t.right <= innerWidth];
    }");

    [E2EFact]
    public async Task Every_Preset_Starts_Its_Board_And_Renames_The_Game()
    {
        var page = await OpenAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1280, Height = 800 } });
        var board = page.Locator(".board");

        foreach (var (size, name) in Presets)
        {
            await PickAsync(page, size.ToString());

            await Expect(board).ToHaveAttributeAsync("data-size", size.ToString());
            Assert.Equal(size * size, await page.Locator(".board .cell").CountAsync());
            Assert.Equal(2, await page.Locator(".board .tile").CountAsync());
            await Expect(page).ToHaveTitleAsync(name);
            await Expect(page.Locator("h1.title")).ToHaveTextAsync(name);
            await Expect(page.Locator(".hint")).ToContainTextAsync($"get to {name}!");
            Assert.Equal($"{name} board, {size} by {size}", await board.GetAttributeAsync("aria-label"));
            Assert.All(await FitsAsync(page), ok => Assert.True(ok, $"{size}x{size} overflows the viewport"));
            // Focus is back on the board: arrow keys play right away.
            Assert.Equal("game", await page.EvaluateAsync<string>("document.activeElement.className"));
        }
    }

    [E2EFact]
    public async Task Menu_Works_From_The_Keyboard()
    {
        var page = await OpenAsync();
        await page.Locator("#size-menu-button").FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown"); // opens on the checked item (4x4)
        await Expect(page.Locator("#size-menu")).ToBeVisibleAsync();
        await Expect(page.Locator("#size-menu-button")).ToHaveAttributeAsync("aria-expanded", "true");
        Assert.Equal("4", await page.EvaluateAsync<string>("document.activeElement.dataset.size"));

        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("6", await page.EvaluateAsync<string>("document.activeElement.dataset.size"));
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("#size-menu")).ToHaveCountAsync(0);
        Assert.Equal("size-menu-button", await page.EvaluateAsync<string>("document.activeElement.id"));
        Assert.Equal("4", await page.Locator(".board").GetAttributeAsync("data-size"));

        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.PressAsync("ArrowUp");
        Assert.Equal("10", await page.EvaluateAsync<string>("document.activeElement.dataset.size"));
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator(".board")).ToHaveAttributeAsync("data-size", "10");
        Assert.Equal(100, await page.Locator(".board .cell").CountAsync());
    }

    [E2EFact]
    public async Task Click_Outside_Closes_The_Menu_Without_Moving()
    {
        var page = await OpenAsync();
        await page.Locator("#size-menu-button").ClickAsync();
        await page.Keyboard.PressAsync("ArrowLeft"); // menu keys never reach the board
        await page.Mouse.ClickAsync(5, 5);
        await Expect(page.Locator("#size-menu")).ToHaveCountAsync(0);
        Assert.Equal("0", await page.Locator(".board").GetAttributeAsync("data-moves"));
    }

    [E2EFact]
    public async Task Custom_Size_Starts_A_Square_Board()
    {
        var page = await OpenAsync();
        await PickAsync(page, "custom");
        var input = page.Locator("#custom-size");
        await Expect(input).ToBeFocusedAsync();

        await input.FillAsync("12");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.Locator(".board")).ToHaveAttributeAsync("data-size", "12");
        Assert.Equal(144, await page.Locator(".board .cell").CountAsync());
        await Expect(page).ToHaveTitleAsync("524288"); // 2^(12+7)
        await Expect(page.Locator(".size-dialog")).ToHaveCountAsync(0);
    }

    [E2EFact]
    public async Task Invalid_Custom_Input_Is_Rejected_Inline()
    {
        var page = await OpenAsync();
        await PickAsync(page, "custom");
        var input = page.Locator("#custom-size");
        var error = page.Locator("#custom-size-error");

        foreach (var (text, message) in new[]
        {
            ("1", "Boards start at 2×2"), ("0", "Boards start at 2×2"), ("-4", "Boards start at 2×2"),
            ("abc", "is not a number"), ("7.5", "without decimals"), ("", "Enter a whole number"), ("999", "The largest board is"),
        })
        {
            await input.FillAsync(text);
            await page.Locator(".size-dialog button[type=submit]").ClickAsync();
            await Expect(error).ToContainTextAsync(message);
            await Expect(input).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(input).ToBeFocusedAsync();
        }
        Assert.Equal("4", await page.Locator(".board").GetAttributeAsync("data-size"));

        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator(".size-dialog")).ToHaveCountAsync(0);
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('blazor2048.size')"));
    }

    [E2EFact]
    public async Task Size_And_Per_Size_Best_Survive_A_Reload()
    {
        var page = await OpenAsync();
        await PickAsync(page, "6");

        // Score something on 6x6.
        foreach (var key in Enumerable.Repeat(new[] { "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown" }, 10).SelectMany(k => k))
        {
            await page.Keyboard.PressAsync(key);
            await page.WaitForTimeoutAsync(20);
        }
        var best = page.Locator(".score-box .value").Nth(1);
        await Expect(best).Not.ToHaveTextAsync("0");
        var best6 = await best.TextContentAsync();

        await page.ReloadAsync();
        await page.Locator(".board[data-size='6'] .cell").First.WaitForAsync();
        Assert.Equal(36, await page.Locator(".board .cell").CountAsync());
        await Expect(page).ToHaveTitleAsync("8192");
        await Expect(best).ToHaveTextAsync(best6!);
        Assert.Equal(best6, await page.EvaluateAsync<string>("localStorage.getItem('blazor2048.best.6x6')"));

        await PickAsync(page, "4"); // 4x4 has its own (empty) best
        await Expect(best).ToHaveTextAsync("0");
        await Expect(page).ToHaveTitleAsync("2048");
    }

    [E2EFact]
    public async Task Rapid_Input_On_10x10_Is_Smooth()
    {
        var context = await NewContext(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1280, Height = 860 } });
        await SeedSizeAsync(context, 10);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Site.BaseUrl);
        var board = page.Locator(".board[data-size='10']");
        await board.Locator(".cell").First.WaitForAsync(new() { Timeout = 60_000 });

        // Fill the board some (left/right only piles tiles up), then press keys every 40 ms.
        for (var i = 0; i < 60; i++) { await page.Keyboard.PressAsync(i % 2 == 0 ? "ArrowLeft" : "ArrowRight"); await page.WaitForTimeoutAsync(10); }
        await page.WaitForTimeoutAsync(300);
        var start = int.Parse((await board.GetAttributeAsync("data-moves"))!);
        await AnimationE2ETests.StartSamplingAsync(page);

        var keys = new[] { "ArrowDown", "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft", "ArrowUp", "ArrowRight" };
        foreach (var key in keys) { await page.Keyboard.PressAsync(key); await page.WaitForTimeoutAsync(40); }
        await page.WaitForTimeoutAsync(500);

        var applied = int.Parse((await board.GetAttributeAsync("data-moves"))!) - start;
        Assert.True(applied >= keys.Length - 1, $"Only {applied} of {keys.Length} keys moved the board."); // one may be a no-op
        var r = await AnimationE2ETests.AnalyzeAsync(page);
        Assert.True(r.Frames > 20, $"Only {r.Frames} frames sampled.");
        Assert.Equal(0, r.Teleports);
        Assert.Equal(0, r.ScaleSnaps);
    }

    [E2EFact]
    public async Task Phone_10x10_Fits_And_Swipes()
    {
        var context = await NewContext(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }, DeviceScaleFactor = 3, IsMobile = true, HasTouch = true,
        });
        await SeedSizeAsync(context, 10);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Site.BaseUrl);
        await page.Locator(".board[data-size='10'] .cell").First.WaitForAsync(new() { Timeout = 60_000 });

        Assert.All(await FitsAsync(page), ok => Assert.True(ok, "10x10 overflows a 390x844 phone"));
        var cell = await page.Locator(".board .cell").First.BoundingBoxAsync();
        Assert.True(cell!.Width >= 24, $"Cells are only {cell.Width:0.0}px wide");
        await Expect(page.Locator("h1.title")).ToHaveTextAsync("131072");

        // Swipe in all four directions with real touch events: at least one moves the board.
        var cdp = await context.NewCDPSessionAsync(page);
        var b = (await page.Locator(".board").BoundingBoxAsync())!;
        var (cx, cy) = (b.X + b.Width / 2, b.Y + b.Height / 2);
        foreach (var (dx, dy) in new[] { (-120.0, 0.0), (0.0, -120.0), (120.0, 0.0), (0.0, 120.0) })
        {
            await Touch(cdp, "touchStart", cx, cy);
            await Touch(cdp, "touchMove", cx + dx / 2, cy + dy / 2);
            await Touch(cdp, "touchMove", cx + dx, cy + dy);
            await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });
            await page.WaitForTimeoutAsync(150);
        }
        Assert.NotEqual("0", await page.Locator(".board").GetAttributeAsync("data-moves"));
    }

    private static Task Touch(ICDPSession cdp, string type, double x, double y) =>
        cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
        });
}
