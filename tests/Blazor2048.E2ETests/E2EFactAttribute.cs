using System.Runtime.CompilerServices;

namespace Blazor2048.E2ETests;

/// <summary>
/// End-to-end tests need a real browser, so they are opt-in.
/// Set RUN_E2E=1 to run them (CI does this in its e2e job).
/// </summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public static bool Enabled => Environment.GetEnvironmentVariable("RUN_E2E") == "1";

    public E2EFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!Enabled) Skip = "E2E tests are opt-in. Set RUN_E2E=1 to run them.";
    }
}
