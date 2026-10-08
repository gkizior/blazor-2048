using System.Buffers.Binary;
using System.Text.Json;

namespace Blazor2048.Tests;

/// <summary>The app icons (specs/012-purple-app-icons): present, declared and the right size.</summary>
public class AppIconTests
{
    private static readonly string WwwRoot = Path.Combine(RepoPaths.Root, "src", "Blazor2048", "wwwroot");

    /// <summary>Width and height from a PNG's IHDR chunk.</summary>
    private static (int W, int H) PngSize(string path)
    {
        var b = File.ReadAllBytes(path);
        Assert.True(b.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }), $"{path} is not a PNG");
        return (BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20)));
    }

    [Fact]
    public void Manifest_Icons_Exist_At_Their_Declared_Sizes_Including_Maskable()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(WwwRoot, "manifest.webmanifest")));
        var icons = manifest.RootElement.GetProperty("icons").EnumerateArray().ToList();
        foreach (var icon in icons)
        {
            var (w, h) = PngSize(Path.Combine(WwwRoot, icon.GetProperty("src").GetString()!));
            Assert.Equal(icon.GetProperty("sizes").GetString(), $"{w}x{h}");
        }
        var maskable = icons.Where(i => i.TryGetProperty("purpose", out var p) && p.GetString() == "maskable")
            .Select(i => i.GetProperty("sizes").GetString()).ToHashSet();
        Assert.Equal(["192x192", "512x512"], maskable.Order());
    }

    [Fact]
    public void Favicons_And_Touch_Icon_Are_Linked_And_Sized()
    {
        var html = File.ReadAllText(Path.Combine(WwwRoot, "index.html"));
        Assert.Contains("href=\"favicon.ico\"", html);
        Assert.Contains("href=\"favicon.png\"", html);
        Assert.Contains("rel=\"apple-touch-icon\" sizes=\"180x180\" href=\"apple-touch-icon.png\"", html);
        Assert.Equal((32, 32), PngSize(Path.Combine(WwwRoot, "favicon.png")));
        Assert.Equal((180, 180), PngSize(Path.Combine(WwwRoot, "apple-touch-icon.png")));

        // ICONDIR: reserved 0, type 1, count; then 16-byte entries (width/height bytes).
        var ico = File.ReadAllBytes(Path.Combine(WwwRoot, "favicon.ico"));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2)));
        var count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4));
        var sizes = Enumerable.Range(0, count).Select(i => (int)ico[6 + 16 * i]).Order().ToArray();
        Assert.Equal([16, 32, 48], sizes);
    }

    [Fact]
    public void Service_Worker_Cache_Has_A_Revision()
    {
        var sw = File.ReadAllText(Path.Combine(WwwRoot, "service-worker.published.js"));
        Assert.Contains("const cacheRevision = 2;", sw);
        Assert.Contains("`${cacheNamePrefix}r${cacheRevision}-${self.assetsManifest.version}`", sw);
    }
}
