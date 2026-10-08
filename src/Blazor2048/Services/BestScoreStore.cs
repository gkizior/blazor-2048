using System.Globalization;
using Game2048.Core;

namespace Blazor2048.Services;

/// <summary>
/// Persists the best score per board size in localStorage (through <see cref="BrowserStorage"/>):
/// <c>blazor2048.best.4x4</c>, <c>blazor2048.best.5x5</c>, ... The 4x4 best from before board sizes
/// existed lived in <c>blazor2048.best</c>; it is copied to the new key the first time it is read
/// (the old key is left in place, so an older version of the app still finds it).
/// </summary>
public sealed class BestScoreStore(BrowserStorage storage)
{
    public const string LegacyKey = "blazor2048.best";

    public static string KeyFor(int size) => $"blazor2048.best.{size}x{size}";

    public async Task<int> LoadAsync(int size)
    {
        if (TryParse(await storage.GetAsync(KeyFor(size)), out var best)) return best;

        if (size == BoardSize.Default && TryParse(await storage.GetAsync(LegacyKey), out var legacy))
        {
            await SaveAsync(size, legacy); // migrate once
            return legacy;
        }
        return 0;
    }

    public Task SaveAsync(int size, int best) => storage.SetAsync(KeyFor(size), best.ToString(CultureInfo.InvariantCulture));

    private static bool TryParse(string? text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
