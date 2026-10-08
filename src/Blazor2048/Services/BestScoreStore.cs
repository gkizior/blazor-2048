using System.Globalization;

namespace Blazor2048.Services;

/// <summary>Persists the best score in localStorage (through <see cref="BrowserStorage"/>).</summary>
public sealed class BestScoreStore(BrowserStorage storage)
{
    public const string Key = "blazor2048.best";

    public async Task<int> LoadAsync() =>
        int.TryParse(await storage.GetAsync(Key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var best) ? best : 0;

    public Task SaveAsync(int best) => storage.SetAsync(Key, best.ToString(CultureInfo.InvariantCulture));
}
