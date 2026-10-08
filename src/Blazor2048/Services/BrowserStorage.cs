using Microsoft.JSInterop;

namespace Blazor2048.Services;

/// <summary>
/// The app's only JS interop: string get/set on the browser's built-in localStorage.
/// WebAssembly has no direct access to localStorage, so this calls the browser API
/// through IJSRuntime (no custom JavaScript file). Everything that persists
/// (best score, theme) goes through this class.
/// </summary>
public sealed class BrowserStorage(IJSRuntime js)
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (JSException)
        {
            return null; // storage unavailable (e.g. private mode)
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (JSException)
        {
            // ignore: the value just won't persist
        }
    }
}
