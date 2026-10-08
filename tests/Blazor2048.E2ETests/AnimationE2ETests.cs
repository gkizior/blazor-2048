using System.Text.Json;
using Microsoft.Playwright;

namespace Blazor2048.E2ETests;

/// <summary>
/// Frame-by-frame checks of the tile animations: a requestAnimationFrame sampler (test-side only)
/// records every tile's on-screen box and opacity each frame while the test plays.
/// </summary>
public class AnimationE2ETests(SiteServer site) : AppTest(site)
{
    [E2EFact]
    public async Task Merge_Sources_Slide_Into_The_Target_Before_The_Merged_Tile_Pops()
    {
        var page = await OpenAsync();
        await StartSamplingAsync(page);

        // Paced play (slower than the 110 ms slide) until a few merges happened.
        var keys = new[] { "ArrowLeft", "ArrowDown", "ArrowRight", "ArrowDown" };
        for (var i = 0; i < 40 && await page.EvaluateAsync<int>("window.__anim.merges.size") < 3; i++)
        {
            await page.Keyboard.PressAsync(keys[i % keys.Length]);
            await page.WaitForTimeoutAsync(300);
        }

        var r = await AnalyzeAsync(page);
        Assert.True(r.Merges >= 1, "No merge happened.");
        Assert.True(r.MergeSources >= 2 * r.Merges);
        Assert.Equal(0, r.SourcesRemovedBeforeArriving);
        Assert.Equal(0, r.PopsBeforeSourcesArrived);
        Assert.Equal(0, r.ScaleSnaps);
    }

    [E2EFact]
    public async Task Rapid_Input_Retargets_Slides_Without_Snapping()
    {
        var page = await OpenAsync();
        var board = page.Locator(".board");
        var start = int.Parse((await board.GetAttributeAsync("data-moves"))!);
        await StartSamplingAsync(page);

        // Every press lands mid-animation (well inside the 110 ms slide).
        var keys = new[] { "ArrowLeft", "ArrowRight", "ArrowLeft", "ArrowRight", "ArrowLeft", "ArrowRight" };
        foreach (var key in keys)
        {
            await page.Keyboard.PressAsync(key);
            await page.WaitForTimeoutAsync(40);
        }
        // A press that cannot move anything (random start position) doesn't count, so wait for
        // the counter to settle instead of expecting exactly one move per key.
        await Expect(board).Not.ToHaveAttributeAsync("data-moves", start.ToString(), new() { Timeout = 2_000 });
        await page.WaitForTimeoutAsync(500);
        var moved = int.Parse((await board.GetAttributeAsync("data-moves"))!) - start;
        Assert.InRange(moved, 1, keys.Length);

        var r = await AnalyzeAsync(page);
        Assert.True(r.Frames > 20, $"Only {r.Frames} frames sampled.");
        Assert.Equal(0, r.Teleports);   // retargeted slides continue from where the tile is
        Assert.Equal(0, r.ScaleSnaps);  // spawn/pop animations are not cut short by the next move
    }

    internal static Task StartSamplingAsync(IPage page) => page.EvaluateAsync(@"() => {
        const A = window.__anim = { frames: [], merges: new Set(), cells: null };
        const layer = document.querySelector('.tile-layer');
        const L = layer.getBoundingClientRect();
        A.cells = [...document.querySelectorAll('.board .cell')].map(c => {
            const r = c.getBoundingClientRect(); return [r.x + r.width / 2 - L.x, r.y + r.height / 2 - L.y, r.width];
        });
        const loop = t => {
            const tiles = [];
            for (const el of layer.children) {
                const inner = el.firstElementChild, r = inner.getBoundingClientRect(), c = el.className;
                const flag = c.includes('retired') ? 'R' : c.includes('merged') ? 'M' : c.includes('new') ? 'N' : '';
                if (flag === 'M') A.merges.add(el.dataset.id);
                tiles.push([+el.dataset.id, +el.dataset.row, +el.dataset.col, flag,
                    r.x + r.width / 2 - L.x, r.y + r.height / 2 - L.y, r.width, +getComputedStyle(inner).opacity]);
            }
            A.frames.push([t, tiles]);
            requestAnimationFrame(loop);
        };
        requestAnimationFrame(loop);
    }");

    internal sealed record Result(int Frames, int Merges, int MergeSources, int SourcesRemovedBeforeArriving,
        int PopsBeforeSourcesArrived, int ScaleSnaps, int Teleports);

    private sealed record Sample(int Frame, double T, int Row, int Col, string Flag, double X, double Y, double W, double Op);

    internal static async Task<Result> AnalyzeAsync(IPage page)
    {
        var data = await page.EvaluateAsync<JsonElement>("() => ({ cells: window.__anim.cells, frames: window.__anim.frames })");
        var cells = data.GetProperty("cells").EnumerateArray().Select(c => (X: c[0].GetDouble(), Y: c[1].GetDouble(), W: c[2].GetDouble())).ToArray();
        var step = cells[1].X - cells[0].X;
        var cellW = cells[0].W;
        var n = (int)Math.Round(Math.Sqrt(cells.Length)); // board size: cells are row-major

        var byId = new Dictionary<int, List<Sample>>();
        var frameCount = 0;
        foreach (var f in data.GetProperty("frames").EnumerateArray())
        {
            foreach (var x in f[1].EnumerateArray())
            {
                var id = x[0].GetInt32();
                if (!byId.TryGetValue(id, out var list)) byId[id] = list = [];
                list.Add(new Sample(frameCount, f[0].GetDouble(), x[1].GetInt32(), x[2].GetInt32(), x[3].GetString()!,
                    x[4].GetDouble(), x[5].GetDouble(), x[6].GetDouble(), x[7].GetDouble()));
            }
            frameCount++;
        }

        double Dist(Sample s)
        {
            var t = cells[s.Row * n + s.Col];
            return Math.Sqrt((s.X - t.X) * (s.X - t.X) + (s.Y - t.Y) * (s.Y - t.Y));
        }

        var retiredByFrame = byId.Values.SelectMany(l => l).Where(s => s.Flag == "R").ToLookup(s => s.Frame);
        int merges = 0, sources = 0, cut = 0, early = 0, snaps = 0, teleports = 0;
        foreach (var list in byId.Values)
        {
            if (list.Any(s => s.Flag == "R"))
            {
                sources++;
                if (Dist(list[^1]) > 0.25 * step) cut++;
            }
            if (list[0].Flag == "M")
            {
                merges++;
                var visible = list.FirstOrDefault(s => s.Op > 0.05 && s.W > 0.1 * cellW);
                if (visible is not null && retiredByFrame[visible.Frame].Any(r =>
                        r.Row == visible.Row && r.Col == visible.Col && Dist(r) > 0.25 * step))
                    early++;
            }
            for (var i = 1; i < list.Count; i++)
            {
                var (a, b) = (list[i - 1], list[i]);
                if (b.Frame != a.Frame + 1 || b.T - a.T > 25) continue; // judge consecutive, undropped frames only
                if (b.W - a.W > 0.2 * cellW && b.W >= 0.98 * cellW) snaps++;
                var moved = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                if (moved >= 0.9 * step && Dist(b) < 1) teleports++;
            }
        }
        return new Result(frameCount, merges, sources, cut, early, snaps, teleports);
    }
}
