using DocsBuilder;

// Usage:
//   DocsBuilder build --repo <root> --out <dir> [--repo-url <url>]
//   DocsBuilder render-diagrams --repo <root>
var command = args.FirstOrDefault();
string Opt(string name, string? fallback = null)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1]
        : fallback ?? throw new ArgumentException($"Missing required option {name}");
}

try
{
    switch (command)
    {
        case "build":
        {
            var site = DocsSite.Load(Path.GetFullPath(Opt("--repo")), Opt("--repo-url", ""));
            var result = site.Write(Path.GetFullPath(Opt("--out")));
            foreach (var warning in result.Warnings)
                Console.WriteLine($"warning DOCS001: {warning}"); // MSBuild picks this up as a build warning
            Console.WriteLine($"DocsBuilder: {result.PageCount} pages, {result.DiagramCount} diagrams -> {Opt("--out")}");
            return 0;
        }
        case "render-diagrams":
        {
            var site = DocsSite.Load(Path.GetFullPath(Opt("--repo")), "");
            return await new MermaidRenderer(site.DiagramDir).RenderMissingAsync(site.Diagrams()) ? 0 : 1;
        }
        default:
            Console.Error.WriteLine("Usage: DocsBuilder build --repo <root> --out <dir> [--repo-url <url>] | render-diagrams --repo <root>");
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error DOCS000: {ex.Message}");
    return 1;
}
