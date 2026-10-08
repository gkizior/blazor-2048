using System.Text.RegularExpressions;

namespace Blazor2048.Tests;

/// <summary>
/// Keeps specs/ in the Spec Kit shape (see docs/spec-driven-development.md): every feature folder is
/// complete, named NNN-feature-name, has the required sections, and shows up in the docs sidebar.
/// </summary>
public partial class SpecsTests
{
    private static readonly string SpecsDir = Path.Combine(RepoPaths.Root, "specs");

    public static TheoryData<string> SpecFolders()
    {
        var data = new TheoryData<string>();
        foreach (var dir in Directory.GetDirectories(SpecsDir).Order(StringComparer.Ordinal))
            data.Add(Path.GetFileName(dir));
        return data;
    }

    [GeneratedRegex(@"^\d{3}-[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex FolderName();

    [Fact]
    public void Constitution_And_Templates_Exist()
    {
        foreach (var file in new[] { "memory/constitution.md", "templates/spec-template.md", "templates/plan-template.md", "templates/tasks-template.md" })
            Assert.True(File.Exists(Path.Combine(RepoPaths.Root, ".specify", file)), $".specify/{file} is missing");
        Assert.Contains("## Original Request", File.ReadAllText(Path.Combine(RepoPaths.Root, ".specify", "templates", "spec-template.md")));
    }

    [Fact]
    public void Spec_Numbers_Are_Unique_And_Have_No_Gaps()
    {
        var numbers = Directory.GetDirectories(SpecsDir).Select(d => int.Parse(Path.GetFileName(d)[..3])).Order().ToList();
        Assert.NotEmpty(numbers);
        Assert.Equal(Enumerable.Range(1, numbers.Count), numbers);
    }

    [Theory]
    [MemberData(nameof(SpecFolders))]
    public void Every_Spec_Folder_Is_Complete(string folder)
    {
        Assert.Matches(FolderName(), folder);
        var dir = Path.Combine(SpecsDir, folder);
        foreach (var file in new[] { "spec.md", "plan.md", "tasks.md" })
            Assert.True(File.Exists(Path.Combine(dir, file)), $"specs/{folder}/{file} is missing");
        Assert.Equal(["plan.md", "spec.md", "tasks.md"],
            Directory.GetFileSystemEntries(dir).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(SpecFolders))]
    public void Every_Spec_Has_The_Required_Sections(string folder)
    {
        var spec = File.ReadAllText(Path.Combine(SpecsDir, folder, "spec.md"));
        Assert.Contains($"`{folder}`", spec);
        Assert.Contains("## Original Request", spec);
        Assert.Matches(@"\*\*Source\*\*:", spec);
        Assert.Matches(@"\*\*Date\*\*: \d{4}-\d{2}-\d{2}", spec);
        Assert.Matches(@"\*\*Given\*\* .+, \*\*When\*\* .+, \*\*Then\*\* ", spec);
        Assert.Matches(@"\*\*FR-\d{3}\*\*", spec);
        Assert.Matches(@"\*\*SC-\d{3}\*\*", spec);

        var plan = File.ReadAllText(Path.Combine(SpecsDir, folder, "plan.md"));
        Assert.Contains("## Constitution Check", plan);
        Assert.Contains("[spec.md](spec.md)", plan);

        var tasks = File.ReadAllText(Path.Combine(SpecsDir, folder, "tasks.md"));
        Assert.Matches(@"(?m)^- \[[ x]\] T\d{3} ", tasks);
        Assert.DoesNotContain("SPECKIT_SHA", tasks);
    }

    [Theory]
    [MemberData(nameof(SpecFolders))]
    public void Every_Spec_Is_In_The_Docs_Sidebar(string folder)
    {
        var toc = File.ReadAllText(Path.Combine(RepoPaths.Root, "docs", "toc.yml"));
        foreach (var file in new[] { "spec.md", "plan.md", "tasks.md" })
            Assert.Contains($"href: ../specs/{folder}/{file}", toc);

        var index = File.ReadAllText(Path.Combine(RepoPaths.Root, "docs", "spec-driven-development.md"));
        Assert.Contains($"../specs/{folder}/spec.md", index);
    }
}
