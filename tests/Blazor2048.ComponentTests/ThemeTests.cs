using Blazor2048.Components;
using Blazor2048.Layout;
using Blazor2048.Services;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Blazor2048.ComponentTests;

public class ThemeTests : AppTestContext
{
    private IRenderedComponent<MainLayout> RenderLayoutWithToggle() =>
        Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b =>
        {
            b.OpenComponent<ThemeToggle>(0);
            b.CloseComponent();
        })));

    private static string Theme(IRenderedComponent<MainLayout> cut) => cut.Find(".app-root").GetAttribute("data-theme")!;

    [Fact]
    public void Defaults_To_System_Theme()
    {
        var cut = RenderLayoutWithToggle();

        Assert.Equal("system", Theme(cut));
        Assert.Contains("theme-system", cut.Find(".app-root").ClassName);
        Assert.Equal("system", cut.Find(".theme-toggle").GetAttribute("data-mode"));
    }

    [Fact]
    public void Toggle_Cycles_Light_Dark_System_And_Applies_It_To_The_Root()
    {
        var cut = RenderLayoutWithToggle();
        var expected = new[] { "light", "dark", "system", "light" };

        foreach (var mode in expected)
        {
            cut.Find(".theme-toggle").Click();
            Assert.Equal(mode, Theme(cut));
            Assert.Contains($"theme-{mode}", cut.Find(".app-root").ClassName);
            Assert.Equal(mode, cut.Find(".theme-toggle").GetAttribute("data-mode"));
        }
    }

    [Fact]
    public void Toggle_Persists_The_Choice_Through_LocalStorage()
    {
        var cut = RenderLayoutWithToggle();

        cut.Find(".theme-toggle").Click(); // light
        cut.Find(".theme-toggle").Click(); // dark

        var saves = JSInterop.Invocations.Where(i => i.Identifier == "localStorage.setItem").ToList();
        Assert.Equal(["blazor2048.theme", "dark"], saves.Last().Arguments);
    }

    [Fact]
    public void Restores_The_Saved_Theme_On_Start()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.theme").SetResult("dark");

        var cut = RenderLayoutWithToggle();

        cut.WaitForAssertion(() => Assert.Equal("dark", Theme(cut)));
        Assert.Equal("dark", cut.Find(".theme-toggle").GetAttribute("data-mode"));
    }

    [Fact]
    public void Ignores_An_Invalid_Saved_Theme()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.theme").SetResult("purple");

        var cut = RenderLayoutWithToggle();

        Assert.Equal("system", Theme(cut));
    }

    [Fact]
    public void Toggle_Has_An_Accessible_Label()
    {
        var cut = Render<ThemeToggle>();

        Assert.Equal("Theme: System (switch to Light)", cut.Find("button").GetAttribute("aria-label"));
        cut.Find("button").Click();
        Assert.Equal("Theme: Light (switch to Dark)", cut.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void Layout_Shows_The_Footer()
    {
        var cut = RenderLayoutWithToggle();

        Assert.Contains("Garrett Kizior", cut.Find(".app-root footer.app-footer").TextContent);
    }

    [Fact]
    public void Next_Cycles_Through_All_Modes()
    {
        Assert.Equal(ThemeMode.Light, ThemeService.Next(ThemeMode.System));
        Assert.Equal(ThemeMode.Dark, ThemeService.Next(ThemeMode.Light));
        Assert.Equal(ThemeMode.System, ThemeService.Next(ThemeMode.Dark));
        Assert.Equal("dark", ThemeService.Name(ThemeMode.Dark));
    }
}
