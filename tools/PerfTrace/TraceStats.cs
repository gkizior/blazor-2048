using System.Text.Json;

/// <summary>Main-thread numbers pulled out of a Chromium trace (JSON trace event format).</summary>
sealed class TraceStats
{
    public List<double> KeydownMs { get; } = [];
    /// <summary>Script time inside each keydown dispatch: Blazor's event handling, render and DOM patch.</summary>
    public List<double> BlazorMs { get; } = [];
    /// <summary>Style/layout forced synchronously inside keydown dispatches (total).</summary>
    public double ForcedStyleLayoutMs { get; private set; }
    /// <summary>Compositor frame outcomes from PipelineReporter (cc frame reporting).</summary>
    public int FramesPresented { get; private set; }
    public int FramesDropped { get; private set; }
    public Dictionary<string, int> AnimationsByName { get; } = [];
    public double StyleMs { get; private set; }
    public double LayoutMs { get; private set; }
    /// <summary>All main-thread task time (RunTask) in the trace window.</summary>
    public double MainThreadMs { get; private set; }
    public double PaintMs { get; private set; }
    public int MainThreadAnimations { get; private set; }
    public int CompositedAnimations { get; private set; }
    public Dictionary<string, int> AnimationFailures { get; } = [];

    public static TraceStats Parse(byte[] json)
    {
        using var doc = JsonDocument.Parse(json);
        var events = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement : doc.RootElement.GetProperty("traceEvents");

        // Renderer main threads; the page's is the one that dispatched the key events.
        var mains = new HashSet<(int, int)>();
        foreach (var e in events.EnumerateArray())
            if (Str(e, "ph") == "M" && Str(e, "name") == "thread_name" && e.TryGetProperty("args", out var a) && Str(a, "name") == "CrRendererMain")
                mains.Add((e.GetProperty("pid").GetInt32(), e.GetProperty("tid").GetInt32()));
        var keyThread = events.EnumerateArray()
            .Where(e => Str(e, "name") == "EventDispatch" && mains.Contains(Thread(e)))
            .GroupBy(Thread).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();

        var s = new TraceStats();
        var seenAnimations = new Dictionary<string, bool>();
        var keydowns = new List<(double Start, double End)>();
        foreach (var e in events.EnumerateArray())
        {
            var name = Str(e, "name");
            if (name == "Animation" && e.TryGetProperty("args", out var aa) && aa.TryGetProperty("data", out var ad))
            {
                if (Str(e, "ph") == "b" && Str(ad, "displayName") is { } dn) s.AnimationsByName[dn] = s.AnimationsByName.GetValueOrDefault(dn) + 1;
                var id = e.TryGetProperty("id2", out var id2) ? id2.ToString() : e.TryGetProperty("id", out var idp) ? idp.ToString() : "";
                if (ad.TryGetProperty("compositeFailed", out var cf))
                {
                    var failed = cf.ValueKind == JsonValueKind.Number && cf.GetInt32() != 0;
                    if (!seenAnimations.ContainsKey(id) || failed) seenAnimations[id] = failed;
                    if (failed)
                    {
                        var reasons = ad.TryGetProperty("unsupportedProperties", out var up) && up.ValueKind == JsonValueKind.Array
                            ? string.Join(",", up.EnumerateArray().Select(x => x.GetString())) : "";
                        var key = $"0x{cf.GetInt32():x} {reasons}".Trim();
                        s.AnimationFailures[key] = s.AnimationFailures.GetValueOrDefault(key) + 1;
                    }
                }
                continue;
            }
            if (name == "PipelineReporter" && Str(e, "ph") == "b" && e.TryGetProperty("args", out var pa) && pa.TryGetProperty("frame_reporter", out var fr))
            {
                var state = Str(fr, "state");
                if (state == "STATE_PRESENTED_ALL") s.FramesPresented++;
                else if (state is "STATE_DROPPED" or "STATE_PRESENTED_PARTIAL") s.FramesDropped++;
                continue;
            }
            if (Thread(e) != keyThread || Str(e, "ph") != "X" || !e.TryGetProperty("dur", out var durEl)) continue;
            var ms = durEl.GetDouble() / 1000.0;
            switch (name)
            {
                case "EventDispatch":
                    if (e.TryGetProperty("args", out var ea) && ea.TryGetProperty("data", out var ed) && Str(ed, "type") == "keydown")
                    {
                        s.KeydownMs.Add(ms);
                        keydowns.Add((e.GetProperty("ts").GetDouble(), e.GetProperty("ts").GetDouble() + durEl.GetDouble()));
                    }
                    break;
                case "UpdateLayoutTree": s.StyleMs += ms; break;
                case "Layout": s.LayoutMs += ms; break;
                case "RunTask": s.MainThreadMs += ms; break;
                case "Paint": case "PrePaint": s.PaintMs += ms; break;
            }
        }
        // Second pass: what ran inside each keydown dispatch.
        foreach (var (start, end) in keydowns)
        {
            double script = 0;
            foreach (var e in events.EnumerateArray())
            {
                if (Thread(e) != keyThread || Str(e, "ph") != "X" || !e.TryGetProperty("dur", out var d) || !e.TryGetProperty("ts", out var tsEl)) continue;
                var ts = tsEl.GetDouble();
                if (ts < start || ts + d.GetDouble() > end) continue;
                var n = Str(e, "name");
                if (n == "FunctionCall") script += d.GetDouble() / 1000;
                else if (n is "UpdateLayoutTree" or "Layout") s.ForcedStyleLayoutMs += d.GetDouble() / 1000;
            }
            s.BlazorMs.Add(script);
        }
        s.MainThreadAnimations = seenAnimations.Count(kv => kv.Value);
        s.CompositedAnimations = seenAnimations.Count(kv => !kv.Value);
        return s;
    }

    static (int, int) Thread(JsonElement e) =>
        e.TryGetProperty("pid", out var p) && e.TryGetProperty("tid", out var t) && p.ValueKind == JsonValueKind.Number && t.ValueKind == JsonValueKind.Number
            ? (p.GetInt32(), t.GetInt32()) : (-1, -1);

    static string? Str(JsonElement e, string prop) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
