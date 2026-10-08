namespace Blazor2048.E2ETests;

/// <summary>
/// End-to-end tests need a real browser, so they are opt-in.
/// Set RUN_E2E=1 to run them (CI does this in its e2e job).
/// </summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_E2E") != "1")
            Skip = "E2E tests are opt-in. Set RUN_E2E=1 to run them.";
    }
}
