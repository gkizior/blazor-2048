// PerfTrace: records how the 2048 board animates under rapid input.
//
//   dotnet run --project tools/PerfTrace -c Release -- --url http://127.0.0.1:8765/blazor-2048/ --label before --out /tmp/perf
//
// For each profile (desktop, desktop with 4x CPU throttling, mobile 390x844 with 4x throttling) and
// each input pace (rapid: a key every 50 ms, paced: every 250 ms) it:
//   1. records a Chromium performance trace (browser.StartTracingAsync) of the moves, and
//   2. records in-page frame timestamps (requestAnimationFrame), keydown and DOM-mutation times,
//      and long tasks;
// then, in a separate sampling pass, reads every tile's on-screen box each frame to find visual
// glitches. Results go to <out>/<label>.json and a Markdown table on stdout.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;

var opts = Options.Parse(args);
Directory.CreateDirectory(opts.Out);
var instrumentation = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Instrumentation.js"));

using var playwright = await Playwright.CreateAsync();
var launch = new BrowserTypeLaunchOptions();
if (Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE") is { Length: > 0 } exe) launch.ExecutablePath = exe;
await using var browser = await playwright.Chromium.LaunchAsync(launch);

var profiles = new List<Profile>
{
    new("desktop", 1280, 860, 1, false, 1),
    new("desktop-4x", 1280, 860, 1, false, 4),
    new("mobile-4x", 390, 844, 3, true, 4),
};
if (opts.Profiles is { } only) profiles = profiles.Where(p => only.Contains(p.Name)).ToList();
var paces = new (string Name, int Ms)[] { ("rapid", 50), ("paced", 250) };

var results = new List<RunResult>();
foreach (var profile in profiles)
foreach (var (paceName, paceMs) in paces)
{
    for (var run = 1; run <= opts.Runs; run++)
    {
        var timing = await RunAsync(profile, paceMs, sampling: false, Path.Combine(opts.Out, $"{opts.Label}-{profile.Name}-{paceName}-{run}.trace.json"));
        var visual = await RunAsync(profile, paceMs, sampling: true, tracePath: null);
        var r = Analyze(profile.Name, paceName, run, timing, visual);
        results.Add(r);
        Console.Error.WriteLine($"{profile.Name,-11} {paceName,-6} #{run}: dropped {r.DroppedFrames}/{r.Frames} frames, " +
            $"cc dropped {r.CcFramesDropped}, p95 {r.FrameP95Ms:0.0} ms, max {r.FrameMaxMs:0.0} ms, blazor {r.BlazorMedianMs:0.0}/{r.BlazorMaxMs:0.0} ms, keydown {r.KeydownMedianMs:0.0}/{r.KeydownMaxMs:0.0} ms, " +
            $"cut sources {r.MergeSourcesCut}/{r.MergeSources}, early pops {r.EarlyPops}/{r.Merges}, scale snaps {r.ScaleSnaps}, teleports {r.Teleports}");
    }
}

var summary = results.GroupBy(r => (r.Profile, r.Pace)).Select(g => new
{
    g.Key.Profile, g.Key.Pace,
    Frames = g.Sum(r => r.Frames), Dropped = g.Sum(r => r.DroppedFrames),
    DroppedPct = 100.0 * g.Sum(r => r.DroppedFrames) / Math.Max(1, g.Sum(r => r.Frames + r.DroppedFrames)),
    FrameP95 = g.Average(r => r.FrameP95Ms), FrameMax = g.Max(r => r.FrameMaxMs),
    KeydownMedian = g.Average(r => r.KeydownMedianMs), KeydownMax = g.Max(r => r.KeydownMaxMs),
    BlazorMedian = g.Average(r => r.BlazorMedianMs), BlazorMax = g.Max(r => r.BlazorMaxMs),
    CcDropped = g.Sum(r => r.CcFramesDropped), CcPresented = g.Sum(r => r.CcFramesPresented),
    InputToDomMedian = g.Average(r => r.InputToDomMedianMs),
    LongTasks = g.Sum(r => r.LongTasks),
    StyleMsPerMove = g.Average(r => r.StyleMsPerMove), LayoutMsPerMove = g.Average(r => r.LayoutMsPerMove), PaintMsPerMove = g.Average(r => r.PaintMsPerMove), MainMsPerMove = g.Average(r => r.MainThreadMsPerMove),
    MainThreadAnimations = g.Sum(r => r.MainThreadAnimations), CompositedAnimations = g.Sum(r => r.CompositedAnimations),
    MergeSources = g.Sum(r => r.MergeSources), MergeSourcesCut = g.Sum(r => r.MergeSourcesCut),
    Merges = g.Sum(r => r.Merges), EarlyPops = g.Sum(r => r.EarlyPops),
    ScaleSnaps = g.Sum(r => r.ScaleSnaps), Teleports = g.Sum(r => r.Teleports),
}).ToList();

var jsonOut = Path.Combine(opts.Out, $"{opts.Label}.json");
File.WriteAllText(jsonOut, JsonSerializer.Serialize(new { opts.Label, opts.Url, Date = DateTime.UtcNow, summary, results }, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"## {opts.Label} ({opts.Url})");
Console.WriteLine();
Console.WriteLine("| profile | pace | dropped frames (rAF) | cc frames dropped / presented | frame p95 / max ms | Blazor ms per move median / max | keydown dispatch median / max ms | input→DOM median ms | long tasks | main-thread ms/move | style / layout / paint ms per move | main-thread / composited anims | merge sources cut | pops before arrival | scale snaps | teleports |");
Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var s in summary)
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"| {s.Profile} | {s.Pace} | {s.Dropped} ({s.DroppedPct:0.0}%) | {s.CcDropped} / {s.CcPresented} | {s.FrameP95:0.0} / {s.FrameMax:0.0} | {s.BlazorMedian:0.00} / {s.BlazorMax:0.0} | {s.KeydownMedian:0.00} / {s.KeydownMax:0.0} | {s.InputToDomMedian:0.00} | {s.LongTasks} | {s.MainMsPerMove:0.0} | {s.StyleMsPerMove:0.00} / {s.LayoutMsPerMove:0.00} / {s.PaintMsPerMove:0.00} | {s.MainThreadAnimations} / {s.CompositedAnimations} | {s.MergeSourcesCut}/{s.MergeSources} | {s.EarlyPops}/{s.Merges} | {s.ScaleSnaps} | {s.Teleports} |"));
Console.WriteLine();
Console.WriteLine($"Raw data: {jsonOut}");
return 0;

async Task<RawRun> RunAsync(Profile profile, int paceMs, bool sampling, string? tracePath)
{
    await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = new ViewportSize { Width = profile.Width, Height = profile.Height },
        DeviceScaleFactor = profile.Dpr, IsMobile = profile.Mobile, HasTouch = profile.Mobile,
        ColorScheme = ColorScheme.Dark, ServiceWorkers = ServiceWorkerPolicy.Block,
    });
    var page = await context.NewPageAsync();
    await page.AddInitScriptAsync(instrumentation);
    await page.GotoAsync(opts.Url);
    await page.Locator(".board .cell").First.WaitForAsync(new() { Timeout = 60_000 });
    await page.Locator(".game").FocusAsync();
    await page.WaitForTimeoutAsync(1500); // let startup work (JIT, best score load) finish

    if (profile.Throttle > 1)
    {
        var cdp = await context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = profile.Throttle });
    }

    // Warm-up moves so first-call JIT/interpreter costs are not counted.
    foreach (var k in new[] { "ArrowLeft", "ArrowRight" }) { await page.Keyboard.PressAsync(k); await page.WaitForTimeoutAsync(300); }

    ChromeTrace? tracing = null;
    if (tracePath is not null) tracing = await ChromeTrace.StartAsync(await context.NewCDPSessionAsync(page));

    await page.EvaluateAsync($"() => {{ const P = window.__perf; P.frames.length = 0; P.keys.length = 0; P.muts.length = 0; P.longtasks.length = 0; P.samples.length = 0; {(sampling ? "P.startSampling();" : "P.sampling = false;")} }}");
    var start = await page.EvaluateAsync<double>("() => performance.now()");
    var keys = new[] { "ArrowLeft", "ArrowDown", "ArrowRight", "ArrowDown" };
    for (var i = 0; i < opts.Moves; i++)
    {
        await page.Keyboard.PressAsync(keys[i % keys.Length]);
        await page.WaitForTimeoutAsync(paceMs);
    }
    await page.WaitForTimeoutAsync(700);
    var end = await page.EvaluateAsync<double>("() => performance.now()");
    var perf = await page.EvaluateAsync<JsonElement>("() => { window.__perf.sampling = false; return window.__perf; }");
    var movesDone = int.Parse(await page.Locator(".board").GetAttributeAsync("data-moves") ?? "0", CultureInfo.InvariantCulture);

    byte[]? trace = null;
    if (tracing is not null)
    {
        trace = await tracing.StopAsync();
        await File.WriteAllBytesAsync(tracePath!, trace);
    }
    return new RawRun(start, end, perf, trace, movesDone);
}

RunResult Analyze(string profile, string pace, int run, RawRun timing, RawRun visual)
{
    var p = timing.Perf;
    double[] Doubles(JsonElement e) => e.EnumerateArray().Select(x => x.GetDouble()).ToArray();
    // rAF frame intervals come from the sampling pass (the timing pass runs no rAF; it uses cc's frame reporting).
    var frames = Doubles(visual.Perf.GetProperty("frames")).Where(t => t >= visual.Start && t <= visual.End).ToArray();
    var intervals = frames.Zip(frames.Skip(1), (a, b) => b - a).ToArray();
    const double vsync = 1000.0 / 60;
    var dropped = intervals.Sum(iv => Math.Max(0, (int)Math.Round(iv / vsync) - 1));
    var sorted = intervals.OrderBy(x => x).ToArray();
    double Pct(double[] s, double q) => s.Length == 0 ? 0 : s[Math.Min(s.Length - 1, (int)Math.Ceiling(q * s.Length) - 1)];

    var keys = Doubles(p.GetProperty("keys"));
    var muts = Doubles(p.GetProperty("muts"));
    var inputToDom = new List<double>();
    for (var i = 0; i < keys.Length; i++)
    {
        var next = i + 1 < keys.Length ? keys[i + 1] : double.MaxValue;
        var m = muts.FirstOrDefault(t => t >= keys[i] && t < next, double.NaN);
        if (!double.IsNaN(m)) inputToDom.Add(m - keys[i]);
    }
    var longTasks = p.GetProperty("longtasks").EnumerateArray().Count(e => e[0].GetDouble() >= timing.Start);

    var trace = TraceStats.Parse(timing.Trace!);
    var moves = Math.Max(1, keys.Length);
    var glitches = Glitches.Find(visual.Perf);

    return new RunResult(profile, pace, run, frames.Length, dropped,
        Pct(sorted, 0.95), sorted.LastOrDefault(),
        Median(trace.KeydownMs), trace.KeydownMs.DefaultIfEmpty(0).Max(),
        Median(inputToDom), longTasks,
        trace.StyleMs / moves, trace.LayoutMs / moves, trace.PaintMs / moves, trace.MainThreadMs / moves,
        trace.MainThreadAnimations, trace.CompositedAnimations, trace.AnimationFailures, trace.AnimationsByName,
        Median(trace.BlazorMs), trace.BlazorMs.DefaultIfEmpty(0).Max(), trace.ForcedStyleLayoutMs / moves, trace.FramesPresented, trace.FramesDropped,
        glitches.MergeSources, glitches.MergeSourcesCut, glitches.Merges, glitches.EarlyPops, glitches.ScaleSnaps, glitches.Teleports,
        keys.Length, timing.MovesDone);
}

static double Median(IEnumerable<double> xs)
{
    var s = xs.OrderBy(x => x).ToArray();
    return s.Length == 0 ? 0 : s.Length % 2 == 1 ? s[s.Length / 2] : (s[s.Length / 2 - 1] + s[s.Length / 2]) / 2;
}

record Profile(string Name, int Width, int Height, float Dpr, bool Mobile, int Throttle);
record RawRun(double Start, double End, JsonElement Perf, byte[]? Trace, int MovesDone);
record RunResult(string Profile, string Pace, int Run, int Frames, int DroppedFrames, double FrameP95Ms, double FrameMaxMs,
    double KeydownMedianMs, double KeydownMaxMs, double InputToDomMedianMs, int LongTasks,
    double StyleMsPerMove, double LayoutMsPerMove, double PaintMsPerMove, double MainThreadMsPerMove,
    int MainThreadAnimations, int CompositedAnimations, Dictionary<string, int> AnimationFailures, Dictionary<string, int> AnimationsByName,
    double BlazorMedianMs, double BlazorMaxMs, double ForcedStyleLayoutMsPerMove, int CcFramesPresented, int CcFramesDropped,
    int MergeSources, int MergeSourcesCut, int Merges, int EarlyPops, int ScaleSnaps, int Teleports,
    int KeysPressed, int MovesApplied);

sealed record Options(string Url, string Label, string Out, int Moves, int Runs, string[]? Profiles)
{
    public static Options Parse(string[] args)
    {
        string Get(string name, string fallback)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
        var profiles = Get("--profiles", "");
        return new Options(Get("--url", "http://127.0.0.1:8765/blazor-2048/"), Get("--label", "run"), Get("--out", "perf-results"),
            int.Parse(Get("--moves", "24"), CultureInfo.InvariantCulture), int.Parse(Get("--runs", "2"), CultureInfo.InvariantCulture),
            profiles.Length > 0 ? profiles.Split(',') : null);
    }
}
