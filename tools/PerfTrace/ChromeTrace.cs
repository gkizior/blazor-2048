using System.Text;
using System.Text.Json;
using Microsoft.Playwright;

/// <summary>Chromium performance trace over CDP (the same data DevTools' Performance panel records).</summary>
sealed class ChromeTrace
{
    static readonly string[] Categories =
    [
        "devtools.timeline", "disabled-by-default-devtools.timeline", "disabled-by-default-devtools.timeline.frame",
        "blink.animations", "toplevel", "v8.execute", "blink.user_timing", "benchmark", "cc", "viz",
    ];

    readonly ICDPSession _cdp;
    readonly List<string> _chunks = [];
    readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

    ChromeTrace(ICDPSession cdp)
    {
        _cdp = cdp;
        _cdp.Event("Tracing.dataCollected").OnEvent += (_, e) =>
        {
            if (e is { } p && p.TryGetProperty("value", out var v))
                foreach (var ev in v.EnumerateArray()) _chunks.Add(ev.GetRawText());
        };
        _cdp.Event("Tracing.tracingComplete").OnEvent += (_, _) => _done.TrySetResult();
    }

    public static async Task<ChromeTrace> StartAsync(ICDPSession cdp)
    {
        var t = new ChromeTrace(cdp);
        await cdp.SendAsync("Tracing.start", new Dictionary<string, object>
        {
            ["transferMode"] = "ReportEvents",
            ["traceConfig"] = new Dictionary<string, object> { ["includedCategories"] = Categories, ["recordMode"] = "recordAsMuchAsPossible" },
        });
        return t;
    }

    public async Task<byte[]> StopAsync()
    {
        await _cdp.SendAsync("Tracing.end");
        await _done.Task.WaitAsync(TimeSpan.FromSeconds(60));
        return Encoding.UTF8.GetBytes("{\"traceEvents\":[" + string.Join(",", _chunks) + "]}");
    }
}
