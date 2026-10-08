using System.Reflection;
using Blazor2048.Components;
using Blazor2048.Services;
using Bunit;

namespace Blazor2048.ComponentTests;

public class FooterTests : AppTestContext
{
    [Fact]
    public void Shows_Name_Version_Runtime_Commit_And_Date()
    {
        var cut = Render<AppFooter>();
        var text = cut.Find("footer").TextContent;

        Assert.Contains("Garrett Kizior", text);
        Assert.Contains("v1.2.3", text);
        Assert.Contains(".NET 10.0.0", text);
        Assert.Contains("2026-10-08", text);
        Assert.Equal("2026-10-08T01:02:03Z", cut.Find("time").GetAttribute("datetime"));
    }

    [Fact]
    public void Short_Sha_Links_To_The_Commit_On_GitHub()
    {
        var cut = Render<AppFooter>();
        var link = cut.Find("a.footer-sha");

        Assert.Equal("0123456", link.TextContent);
        Assert.Equal("https://github.com/gkizior/blazor-2048/commit/0123456789abcdef0123456789abcdef01234567", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener", link.GetAttribute("rel"));
    }

    [Fact]
    public void Unknown_Commit_Is_Not_A_Link()
    {
        var info = new BuildInfo("Garrett Kizior", "1.0.0", ".NET 10.0.0", "unknown", "unknown", "https://github.com/gkizior/blazor-2048");
        Assert.Null(info.CommitUrl);
        Assert.Equal("unknown", info.ShortCommit);
    }

    [Fact]
    public void BuildInfo_Reads_Assembly_Metadata_Stamped_By_MSBuild()
    {
        var info = BuildInfo.FromAssembly(typeof(App).Assembly);

        Assert.Equal("Garrett Kizior", info.Author);
        Assert.Matches(@"^\d+\.\d+\.\d+", info.Version);
        Assert.DoesNotContain("+", info.Version);
        Assert.StartsWith(".NET 10", info.Runtime);
        Assert.Matches("^([0-9a-f]{40}|unknown)$", info.Commit);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}", info.BuildDate);
        Assert.Equal("https://github.com/gkizior/blazor-2048", info.RepositoryUrl);
        Assert.Contains(typeof(App).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>(), a => a.Key == "BuildCommit");
    }
}
