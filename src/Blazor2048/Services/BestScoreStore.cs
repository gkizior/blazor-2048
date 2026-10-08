using Microsoft.JSInterop;

namespace Blazor2048.Services;

/// <summary>
/// Persists the best score in the browser's localStorage.
/// This is the app's only JS interop: WebAssembly has no direct access to
/// localStorage, so we call the built-in browser API through IJSRuntime
/// (no custom JavaScript file).
/// </summary>
public sealed class BestScoreStore(IJSRuntime js)
{
    private const string Key = "blazor2048.best";

    public async Task<int> LoadAsync()
    {
        try
        {
            var stored = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            return int.TryParse(stored, out var best) ? best : 0;
        }
        catch (JSException)
        {
            return 0; // storage unavailable (e.g. private mode)
        }
    }

    public async Task SaveAsync(int best)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key, best.ToString());
        }
        catch (JSException)
        {
            // ignore: best score just won't persist
        }
    }
}
