using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DocsBuilder;

/// <summary>A ```mermaid block. Its hash (source + renderer settings) names the pre-rendered SVGs.</summary>
public sealed partial record MermaidDiagram(string Source)
{
    public string Normalized => Source.Replace("\r\n", "\n").Trim();

    public string Hash => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(Normalized + "\n" + MermaidRenderer.SettingsFingerprint)))[..12];

    /// <summary>accTitle from the diagram (Mermaid's accessibility title), used as alt text.</summary>
    public string? Title => AccTitle().Match(Normalized) is { Success: true } m ? m.Groups[1].Value.Trim() : null;

    public string FileName(string theme) => $"{Hash}-{theme}.svg";

    [GeneratedRegex(@"^\s*accTitle\s*:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex AccTitle();
}

/// <summary>
/// Pre-renders Mermaid diagrams to static SVGs with the official mermaid-cli (headless Chrome), once,
/// at authoring/CI time. The app then shows plain &lt;img&gt; files: no Mermaid JavaScript at runtime,
/// nothing extra to download until the docs open, and it works offline from the service worker cache.
/// SVGs are committed in docs/diagrams/ (named by content hash), so normal builds never need Node.
/// </summary>
public sealed class MermaidRenderer(string diagramDir)
{
    public const string MermaidCliVersion = "12.0.0";
    public const string PuppeteerVersion = "25.12.0";

    // Same font metrics on every OS (Arial / Liberation Sans / Helvetica), so labels never clip.
    private const string Font = "Arial, Helvetica, \\\"Liberation Sans\\\", sans-serif";

    // Palettes match wwwroot/css/app.css.
    public static readonly IReadOnlyDictionary<string, string> Configs = new Dictionary<string, string>
    {
        ["light"] = $$$"""
            {"theme":"base","look":"classic","htmlLabels":false,"fontFamily":"{{{Font}}}",
             "flowchart":{"htmlLabels":false,"curve":"basis"},
             "themeVariables":{"fontFamily":"{{{Font}}}","fontSize":"15px","background":"transparent",
               "primaryColor":"#ece7ff","primaryTextColor":"#2b2724","primaryBorderColor":"#6d4aff",
               "secondaryColor":"#ffe6cf","secondaryTextColor":"#2b2724","secondaryBorderColor":"#ff8a4c",
               "tertiaryColor":"#f6f3ee","tertiaryTextColor":"#2b2724","tertiaryBorderColor":"#cdc1b4",
               "lineColor":"#6f675e","textColor":"#2b2724","mainBkg":"#ece7ff","nodeBorder":"#6d4aff",
               "clusterBkg":"#f6f3ee","clusterBorder":"#cdc1b4","edgeLabelBackground":"#ffffff",
               "noteBkgColor":"#fff4d6","noteTextColor":"#2b2724","noteBorderColor":"#eeab22",
               "actorBkg":"#ece7ff","actorBorder":"#6d4aff","actorTextColor":"#2b2724","signalColor":"#3b3631",
               "signalTextColor":"#2b2724","labelBoxBkgColor":"#ece7ff","labelBoxBorderColor":"#6d4aff",
               "activationBkgColor":"#ffe6cf","activationBorderColor":"#ff8a4c","sequenceNumberColor":"#ffffff"}}
            """,
        ["dark"] = $$$"""
            {"theme":"base","look":"classic","htmlLabels":false,"fontFamily":"{{{Font}}}",
             "flowchart":{"htmlLabels":false,"curve":"basis"},
             "themeVariables":{"darkMode":true,"fontFamily":"{{{Font}}}","fontSize":"15px","background":"transparent",
               "primaryColor":"#2a2450","primaryTextColor":"#eceef3","primaryBorderColor":"#8f78ff",
               "secondaryColor":"#3a2a1f","secondaryTextColor":"#eceef3","secondaryBorderColor":"#ff8a4c",
               "tertiaryColor":"#232631","tertiaryTextColor":"#eceef3","tertiaryBorderColor":"#3d4354",
               "lineColor":"#a3a8b5","textColor":"#eceef3","mainBkg":"#2a2450","nodeBorder":"#8f78ff",
               "clusterBkg":"#1f2129","clusterBorder":"#3d4354","edgeLabelBackground":"#1b1d24",
               "noteBkgColor":"#3a3320","noteTextColor":"#eceef3","noteBorderColor":"#eeab22",
               "actorBkg":"#2a2450","actorBorder":"#8f78ff","actorTextColor":"#eceef3","signalColor":"#d5d8e0",
               "signalTextColor":"#eceef3","labelBoxBkgColor":"#2a2450","labelBoxBorderColor":"#8f78ff",
               "activationBkgColor":"#3a2a1f","activationBorderColor":"#ff8a4c","sequenceNumberColor":"#121318"}}
            """,
    };

    /// <summary>Changing the renderer or its settings changes every hash, so all diagrams re-render.</summary>
    public static string SettingsFingerprint => $"mmdc {MermaidCliVersion}\n{Configs["light"]}\n{Configs["dark"]}";

    public async Task<bool> RenderMissingAsync(IEnumerable<MermaidDiagram> diagrams)
    {
        Directory.CreateDirectory(diagramDir);
        var list = diagrams.ToList();
        var wanted = list.SelectMany(d => Configs.Keys.Select(d.FileName)).ToHashSet();

        // Remove SVGs no doc uses any more.
        foreach (var file in Directory.EnumerateFiles(diagramDir, "*.svg"))
            if (!wanted.Contains(Path.GetFileName(file)))
            {
                File.Delete(file);
                Console.WriteLine($"Removed unused {Path.GetFileName(file)}");
            }

        var work = Path.Combine(Path.GetTempPath(), "docsbuilder-mermaid-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(work);
        try
        {
            var puppeteerConfig = Path.Combine(work, "puppeteer.json");
            var chrome = FindChrome();
            await File.WriteAllTextAsync(puppeteerConfig, chrome is null
                ? """{"args":["--no-sandbox"]}"""
                : $$"""{"executablePath":"{{chrome.Replace("\\", "\\\\")}}","args":["--no-sandbox"]}""");

            var ok = true;
            foreach (var diagram in list)
            foreach (var (theme, config) in Configs)
            {
                var output = Path.Combine(diagramDir, diagram.FileName(theme));
                if (File.Exists(output)) continue;

                var input = Path.Combine(work, $"{diagram.Hash}.mmd");
                var configFile = Path.Combine(work, $"{theme}.json");
                await File.WriteAllTextAsync(input, diagram.Normalized);
                await File.WriteAllTextAsync(configFile, config);

                Console.WriteLine($"Rendering {diagram.FileName(theme)} ({diagram.Title ?? "untitled"})");
                var exit = await RunAsync("npx", [
                    "--yes", "-p", $"@mermaid-js/mermaid-cli@{MermaidCliVersion}", "-p", $"puppeteer@{PuppeteerVersion}",
                    "mmdc", "-i", input, "-o", output, "-c", configFile, "-p", puppeteerConfig,
                    "-b", "transparent", "--no-font-embed", "-I", $"d{diagram.Hash}-{theme}", "-q",
                ], chrome is not null);
                if (exit != 0 || !File.Exists(output))
                {
                    Console.Error.WriteLine($"error DOCS002: mermaid-cli failed for {diagram.FileName(theme)} (exit {exit}).");
                    ok = false;
                }
            }
            // Give every SVG an intrinsic size so <img> shows it at its natural size (not stretched).
            foreach (var file in Directory.EnumerateFiles(diagramDir, "*.svg"))
            {
                var svg = await File.ReadAllTextAsync(file);
                var fixedSvg = WithIntrinsicSize(svg);
                if (fixedSvg != svg) await File.WriteAllTextAsync(file, fixedSvg);
            }
            return ok;
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }

    /// <summary>
    /// mermaid-cli writes width="100%" plus style="max-width: Npx". Inline that is fine, but as an
    /// &lt;img&gt; it has no intrinsic size and stretches to the container. Use the real size instead.
    /// </summary>
    public static string WithIntrinsicSize(string svg)
    {
        var root = Regex.Match(svg, "<svg\\b[^>]*>");
        if (!root.Success || !root.Value.Contains("width=\"100%\"")) return svg;
        var viewBox = Regex.Match(root.Value, "viewBox=\"[-\\d.]+ [-\\d.]+ ([\\d.]+) ([\\d.]+)\"");
        if (!viewBox.Success) return svg;
        var width = double.Parse(viewBox.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var height = double.Parse(viewBox.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        var tag = root.Value.Replace("width=\"100%\"", FormattableString.Invariant($"width=\"{Math.Ceiling(width)}\" height=\"{Math.Ceiling(height)}\""));
        return svg[..root.Index] + tag + svg[(root.Index + root.Length)..];
    }

    private static string? FindChrome()
    {
        foreach (var env in new[] { "PUPPETEER_EXECUTABLE_PATH", "CHROME_PATH" })
            if (Environment.GetEnvironmentVariable(env) is { Length: > 0 } path && File.Exists(path)) return path;
        foreach (var path in new[] { "/usr/bin/google-chrome", "/usr/bin/google-chrome-stable", "/usr/bin/chromium", "/usr/bin/chromium-browser",
                                     @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                                     "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" })
            if (File.Exists(path)) return path;
        return null; // let puppeteer download its own browser
    }

    private static async Task<int> RunAsync(string file, string[] args, bool skipBrowserDownload)
    {
        var psi = new ProcessStartInfo(OperatingSystem.IsWindows() ? "npx.cmd" : file) { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (skipBrowserDownload) psi.Environment["PUPPETEER_SKIP_DOWNLOAD"] = "1";
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start npx. Is Node.js installed?");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
