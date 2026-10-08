using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: AssemblyFixture(typeof(Blazor2048.E2ETests.SiteServer))]

namespace Blazor2048.E2ETests;

/// <summary>
/// Serves the published app with Kestrel (static files only, like GitHub Pages).
/// Shared by every E2E test as an xUnit v3 assembly fixture.
/// Uses E2E_SITE_DIR if set; otherwise publishes src/Blazor2048 first.
/// </summary>
public sealed class SiteServer : IAsyncLifetime
{
    private WebApplication? _server;

    public string BaseUrl { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        if (!E2EFactAttribute.Enabled) return;

        var siteDir = Environment.GetEnvironmentVariable("E2E_SITE_DIR") ?? PublishApp();
        siteDir = Path.GetFullPath(siteDir);

        // Serve under the same path as <base href> (e.g. /blazor-2048/ after prepare-pages.py).
        var html = await File.ReadAllTextAsync(Path.Combine(siteDir, "index.html"));
        var basePath = Regex.Match(html, "<base href=\"([^\"]*)\"").Groups[1].Value.TrimEnd('/');

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        _server = builder.Build();
        var files = new PhysicalFileProvider(siteDir);
        _server.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files, RequestPath = basePath });
        _server.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            RequestPath = basePath,
            ServeUnknownFileTypes = true,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
        });
        await _server.StartAsync();
        BaseUrl = _server.Urls.First() + basePath + "/";
    }

    public async ValueTask DisposeAsync()
    {
        if (_server is not null) await _server.DisposeAsync();
    }

    private static string PublishApp()
    {
        var root = FindRepoRoot();
        var output = Path.Combine(root, "artifacts", "e2e-publish");
        var psi = new ProcessStartInfo("dotnet",
            $"publish \"{Path.Combine(root, "src", "Blazor2048", "Blazor2048.csproj")}\" -c Release -o \"{output}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        var log = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("dotnet publish failed:\n" + log);
        return Path.Combine(output, "wwwroot");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Blazor2048.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find Blazor2048.sln");
    }
}
