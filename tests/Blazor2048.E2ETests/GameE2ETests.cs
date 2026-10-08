using Microsoft.Playwright;

namespace Blazor2048.E2ETests;

public class GameE2ETests(SiteServer site) : AppTest(site)
{
    [E2EFact]
    public async Task App_Loads_And_Renders_Board()
    {
        var page = await OpenAsync();

        Assert.Equal("2048", await page.TitleAsync());
        Assert.Equal(16, await page.Locator(".board .cell").CountAsync());
        Assert.Equal(2, await page.Locator(".board .tile").CountAsync());
        Assert.Equal("game", await page.EvaluateAsync<string>("document.activeElement.className"));
    }

    [E2EFact]
    public async Task Arrow_Keys_Change_The_Board()
    {
        var page = await OpenAsync();
        var before = await BoardAsync(page);

        var changed = false;
        foreach (var key in new[] { "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown" })
        {
            await page.Keyboard.PressAsync(key);
            await page.WaitForTimeoutAsync(150);
            if (!(await BoardAsync(page)).SequenceEqual(before)) { changed = true; break; }
        }

        Assert.True(changed, "No arrow key changed the board.");
        Assert.True(await page.Locator(".board .tile").CountAsync() >= 2);
    }

    [E2EFact]
    public async Task Mobile_Viewport_Fits_Without_Scrolling()
    {
        foreach (var (w, h) in new[] { (390, 844), (375, 667) }) // iPhone 14 and iPhone SE
        {
            var page = await OpenAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = w, Height = h },
                DeviceScaleFactor = 3,
                IsMobile = true,
                HasTouch = true,
            });

            var fits = await page.EvaluateAsync<bool[]>(@"() => {
                const d = document.documentElement, b = document.querySelector('.board').getBoundingClientRect();
                const btn = document.querySelector('.sub .btn').getBoundingClientRect();
                return [d.scrollHeight <= innerHeight, d.scrollWidth <= innerWidth,
                        b.bottom <= innerHeight && b.right <= innerWidth && b.left >= 0, btn.top >= 0];
            }");

            Assert.All(fits, ok => Assert.True(ok, $"Layout overflows at {w}x{h}"));
        }
    }
}
