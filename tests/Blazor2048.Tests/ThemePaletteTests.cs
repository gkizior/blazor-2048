using System.Globalization;
using System.Text.RegularExpressions;

namespace Blazor2048.Tests;

/// <summary>
/// Guards the tile palette in app.css: every tile's text must keep WCAG AA contrast (4.5:1)
/// against its background, in the light and the dark theme.
/// </summary>
public partial class ThemePaletteTests
{
    private static readonly string Css = File.ReadAllText(Path.Combine(RepoPaths.Root, "src", "Blazor2048", "wwwroot", "css", "app.css"));

    private static readonly string[] Tiles = ["2", "4", "8", "16", "32", "64", "128", "256", "512", "1024", "2048", "super"];

    public static TheoryData<string> TileValues => new(Tiles);

    [Theory]
    [MemberData(nameof(TileValues))]
    public void Tile_Text_Has_AA_Contrast_In_Both_Themes(string tile)
    {
        var backgrounds = Colors($"--tile-{tile}-bg");
        var foregrounds = Colors($"--tile-{tile}-fg");

        foreach (var theme in new[] { 0, 1 }) // light, dark
        foreach (var bg in backgrounds[theme])
        {
            var ratio = Contrast(bg, foregrounds[theme][0]);
            Assert.True(ratio >= 4.5, $"tile-{tile} ({(theme == 0 ? "light" : "dark")}): {foregrounds[theme][0]} on {bg} is {ratio:F2}:1");
        }
    }

    [Fact]
    public void Every_Tile_Class_Uses_The_Palette()
    {
        foreach (var tile in Tiles)
            Assert.Matches($@"\.tile-{tile}\s*\{{\s*--tile-bg:\s*var\(--tile-{tile}-bg\);\s*--tile-fg:\s*var\(--tile-{tile}-fg\)", Css);
    }

    [Fact]
    public void Theme_Is_Applied_Through_Color_Scheme_On_The_App_Root()
    {
        Assert.Contains(""".app-root[data-theme="light"] { color-scheme: light; }""", Css);
        Assert.Contains(""".app-root[data-theme="dark"]  { color-scheme: dark; }""", Css);
        Assert.Contains("prefers-reduced-motion: reduce", Css);
    }

    [Fact]
    public void Tile_Motion_Uses_Only_Transform_And_Opacity()
    {
        // Slides transition transform; spawn and pop keyframes touch only transform and opacity, so
        // every tile animation can run on the compositor.
        Assert.Matches(@"\.tile\s*\{[^}]*transition:\s*transform var\(--slide\)", Css);

        // Spawn and pop start when the slide ends and hold an invisible first frame until then, so a
        // merged tile never shows up on top of its still-sliding sources.
        Assert.Matches(@"\.tile-new \.tile-inner\s*\{\s*animation: tile-spawn var\(--spawn\) \S+ var\(--slide\) backwards", Css);
        Assert.Matches(@"\.tile-merged \.tile-inner\s*\{\s*animation: tile-pop var\(--pop\) \S+ var\(--slide\) backwards", Css);
        Assert.Matches(@"@keyframes tile-spawn\s*\{\s*from\s*\{[^}]*opacity:\s*0", Css);
        Assert.Matches(@"@keyframes tile-pop\s*\{\s*0%\s*\{[^}]*opacity:\s*0", Css);
        foreach (var name in new[] { "tile-spawn", "tile-pop" })
        {
            var start = Css.IndexOf($"@keyframes {name}", StringComparison.Ordinal);
            Assert.True(start >= 0, name);
            var (open, depth, end) = (Css.IndexOf('{', start), 0, -1);
            for (var i = open; i < Css.Length && end < 0; i++)
                if (Css[i] == '{') depth++;
                else if (Css[i] == '}' && --depth == 0) end = i;
            var body = Css[open..end];
            var properties = System.Text.RegularExpressions.Regex.Matches(body, @"([a-z-]+)\s*:(?!\s*[^;]*\{)").Select(m => m.Groups[1].Value).Distinct();
            Assert.All(properties, p => Assert.Contains(p, new[] { "transform", "opacity" }));
        }
    }

    /// <summary>[light colors, dark colors] for a custom property (gradients yield several colors).</summary>
    private static List<string>[] Colors(string property)
    {
        var value = Regex.Match(Css, Regex.Escape(property) + @":\s*([^;]+);").Groups[1].Value.Trim();
        Assert.False(string.IsNullOrEmpty(value), $"{property} not found in app.css");

        var lightDark = LightDark().Match(value);
        if (lightDark.Success)
            return [Hex(lightDark.Groups[1].Value), Hex(lightDark.Groups[2].Value)];
        var all = Hex(value);
        return [all, all];
    }

    private static List<string> Hex(string text) => HexColor().Matches(text).Select(m => m.Value).ToList();

    private static double Contrast(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3) hex = string.Concat(hex.Select(c => $"{c}{c}"));
        double Channel(int i)
        {
            var c = int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
    }

    [GeneratedRegex(@"light-dark\(\s*([^,]+),\s*([^)]+)\)")]
    private static partial Regex LightDark();

    [GeneratedRegex("#[0-9a-fA-F]{6}\\b|#[0-9a-fA-F]{3}\\b")]
    private static partial Regex HexColor();
}
