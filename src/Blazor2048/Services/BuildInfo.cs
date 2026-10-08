using System.Reflection;
using System.Runtime.InteropServices;

namespace Blazor2048.Services;

/// <summary>
/// Build details shown in the footer. Commit, build date and repository URL are stamped into the
/// assembly as [AssemblyMetadata] by MSBuild (see Blazor2048.csproj: GITHUB_SHA in CI, git locally).
/// </summary>
public sealed record BuildInfo(
    string Author,
    string Version,
    string Runtime,
    string Commit,
    string BuildDate,
    string RepositoryUrl)
{
    public string ShortCommit => Commit.Length >= 7 && Commit != "unknown" ? Commit[..7] : Commit;

    public string? CommitUrl => Commit is "" or "unknown" || RepositoryUrl == "" ? null : $"{RepositoryUrl}/commit/{Commit}";

    /// <summary>Just the date part of <see cref="BuildDate"/> (yyyy-MM-dd).</summary>
    public string BuildDay => BuildDate.Length >= 10 ? BuildDate[..10] : BuildDate;

    public static BuildInfo FromAssembly(Assembly assembly)
    {
        var meta = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .GroupBy(a => a.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value ?? "");
        string Get(string key) => meta.TryGetValue(key, out var v) && v != "" ? v : "unknown";

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
        var plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus]; // strip any SourceLink "+sha" suffix

        return new BuildInfo(
            Author: "Garrett Kizior",
            Version: version,
            Runtime: RuntimeInformation.FrameworkDescription, // e.g. ".NET 10.0.1"
            Commit: Get("BuildCommit"),
            BuildDate: Get("BuildDate"),
            RepositoryUrl: meta.TryGetValue("RepositoryUrl", out var repo) ? repo.TrimEnd('/') : "");
    }
}
