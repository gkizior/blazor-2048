using System.Text.Json;
using Microsoft.Playwright;

namespace Blazor2048.E2ETests;

/// <summary>
/// Exact accounting for rapid key presses. A press that cannot move anything (for example
/// ArrowLeft when every tile already sits on the left edge) is correctly ignored by the game, so
/// "one move per key" is not a valid expectation on a random board. Instead, the page records the
/// board after every move (a MutationObserver on <c>data-moves</c>, which changes in the same render as
/// the tiles), and <see cref="VerifyAsync"/> replays the keys in order against an independent slide
/// oracle: every key that can move the board it is pressed on must produce exactly the next
/// recorded board (that board slid in the key's direction plus one new 2 or 4), and keys that cannot
/// move must leave it alone. A dropped, duplicated or reordered key fails the replay.
/// </summary>
internal static class InputLedger
{
    public sealed record Snapshot(int Moves, int[] Cells);

    private const string ReadBoard = @"() => {
        const b = document.querySelector('.board'), n = +b.dataset.size, cells = Array(n * n).fill(0);
        b.querySelectorAll('.tile:not(.tile-retired)').forEach(t => cells[+t.dataset.row * n + +t.dataset.col] = +t.dataset.value);
        return { moves: +b.dataset.moves, size: n, cells };
    }";

    /// <summary>Start recording; returns the current board, which the first key is pressed on.</summary>
    public static async Task<(int Size, Snapshot Start)> StartAsync(IPage page)
    {
        var start = await page.EvaluateAsync<JsonElement>($@"() => {{
            const read = {ReadBoard};
            const L = window.__ledger = [];
            new MutationObserver(() => L.push(read()))
                .observe(document.querySelector('.board'), {{ attributes: true, attributeFilter: ['data-moves'] }});
            return read();
        }}");
        return (start.GetProperty("size").GetInt32(), ToSnapshot(start));
    }

    /// <summary>
    /// Replays <paramref name="keys"/> from <paramref name="start"/>, waiting (on state, not on a fixed sleep)
    /// for each move the replay expects. Returns how many keys moved the board.
    /// </summary>
    public static async Task<int> VerifyAsync(IPage page, int size, Snapshot start, IReadOnlyList<string> keys)
    {
        var cur = start.Cells;
        var expected = 0;
        foreach (var (key, i) in keys.Select((k, i) => (k, i)))
        {
            var slid = Slide(cur, size, key);
            if (slid.SequenceEqual(cur)) continue; // this key cannot move this board: the game ignores it

            expected++;
            try
            {
                await page.WaitForFunctionAsync("n => window.__ledger.length >= n", expected, new() { Timeout = 5_000 });
            }
            catch (TimeoutException)
            {
                Assert.Fail($"Key {i + 1} ({key}) could move the board but was dropped: {expected - 1} moves recorded. " +
                            $"Board before it: {string.Join(",", cur)}");
            }

            var snap = (await LedgerAsync(page))[expected - 1];
            Assert.True(start.Moves + expected == snap.Moves, $"Key {i + 1} ({key}): data-moves is {snap.Moves}, expected {start.Moves + expected}.");
            var spawned = Enumerable.Range(0, cur.Length).Where(c => slid[c] != snap.Cells[c]).ToArray();
            Assert.True(spawned.Length == 1 && slid[spawned[0]] == 0 && snap.Cells[spawned[0]] is 2 or 4,
                $"Key {i + 1} ({key}) was not applied in order: expected {string.Join(",", slid)} plus one new tile, got {string.Join(",", snap.Cells)}.");
            cur = snap.Cells;
        }

        // Nothing extra was applied either.
        var all = await LedgerAsync(page);
        Assert.True(expected == all.Count, $"{all.Count} moves recorded for {expected} movable keys.");
        Assert.Equal((start.Moves + expected).ToString(), await page.Locator(".board").GetAttributeAsync("data-moves"));
        return expected;
    }

    /// <summary>The first of the four directions that moves <paramref name="cells"/> (a fresh board always has one).</summary>
    public static string FirstMovable(int[] cells, int size) =>
        new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown" }.First(k => !Slide(cells, size, k).SequenceEqual(cells));

    public static string Opposite(string key) => key switch
    {
        "ArrowLeft" => "ArrowRight", "ArrowRight" => "ArrowLeft", "ArrowUp" => "ArrowDown", _ => "ArrowUp",
    };

    private static async Task<List<Snapshot>> LedgerAsync(IPage page) =>
        (await page.EvaluateAsync<JsonElement>("() => window.__ledger")).EnumerateArray().Select(ToSnapshot).ToList();

    private static Snapshot ToSnapshot(JsonElement e) =>
        new(e.GetProperty("moves").GetInt32(), e.GetProperty("cells").EnumerateArray().Select(c => c.GetInt32()).ToArray());

    /// <summary>The oracle: classic 2048 slide (each tile merges at most once), written independently of Game2048.Core.</summary>
    internal static int[] Slide(int[] cells, int n, string key)
    {
        var result = new int[cells.Length];
        for (var line = 0; line < n; line++)
        {
            // Cell index of the k-th cell of this line, counted from the edge the tiles slide toward.
            Func<int, int> at = key switch
            {
                "ArrowLeft" => k => line * n + k,
                "ArrowRight" => k => line * n + (n - 1 - k),
                "ArrowUp" => k => k * n + line,
                "ArrowDown" => k => (n - 1 - k) * n + line,
                _ => throw new ArgumentException(key),
            };
            var packed = new List<int>();
            var mergedLast = false;
            for (var k = 0; k < n; k++)
            {
                var v = cells[at(k)];
                if (v == 0) continue;
                if (!mergedLast && packed.Count > 0 && packed[^1] == v) { packed[^1] = v * 2; mergedLast = true; }
                else { packed.Add(v); mergedLast = false; }
            }
            for (var k = 0; k < packed.Count; k++) result[at(k)] = packed[k];
        }
        return result;
    }
}
