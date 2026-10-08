using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Blazor2048.E2ETests;

/// <summary>
/// Base class for E2E tests. Microsoft.Playwright.Xunit.v3's <see cref="BrowserTest"/> owns the
/// Playwright/browser lifecycle (BROWSER and HEADED env vars work as usual); this adds the
/// local site and a helper that opens the game and waits for Blazor to render.
/// </summary>
public abstract class AppTest(SiteServer site) : BrowserTest
{
    protected SiteServer Site { get; } = site;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Optional: point at an installed Chrome instead of Playwright's bundled Chromium.
    public override Task<BrowserTypeLaunchOptions?> LaunchOptionsAsync() =>
        Task.FromResult(Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE") is { Length: > 0 } exe
            ? new BrowserTypeLaunchOptions { Headless = true, ExecutablePath = exe }
            : null);

    protected async Task<IPage> OpenAsync(BrowserNewContextOptions? options = null, string path = "")
    {
        var context = await NewContext(options ?? new BrowserNewContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Site.BaseUrl + path);
        await page.Locator(".board .cell").First.WaitForAsync(new() { Timeout = 60_000 });
        return page;
    }

    protected static async Task<string[]> BoardAsync(IPage page) =>
        (await page.Locator(".board .cell").AllInnerTextsAsync()).Select(s => s.Trim()).ToArray();
}
