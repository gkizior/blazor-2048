using System.Globalization;
using Game2048.Core;

namespace Blazor2048.Services;

/// <summary>Remembers the last board size chosen (<c>blazor2048.size</c>); 4x4 when nothing valid is saved.</summary>
public sealed class BoardSizeStore(BrowserStorage storage)
{
    public const string Key = "blazor2048.size";

    public async Task<int> LoadAsync() =>
        int.TryParse(await storage.GetAsync(Key), NumberStyles.None, CultureInfo.InvariantCulture, out var size) && BoardSize.IsValid(size)
            ? size
            : BoardSize.Default;

    public Task SaveAsync(int size) => storage.SetAsync(Key, size.ToString(CultureInfo.InvariantCulture));
}
