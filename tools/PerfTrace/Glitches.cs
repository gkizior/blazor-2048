using System.Text.Json;

/// <summary>Visual glitches found by sampling every tile's on-screen box once per frame.</summary>
sealed record Glitches(int MergeSources, int MergeSourcesCut, int Merges, int EarlyPops, int ScaleSnaps, int Teleports)
{
    sealed record S(int Frame, double T, int Value, int Row, int Col, string Flag, double X, double Y, double W, double Op);

    public static Glitches Find(JsonElement perf)
    {
        var cells = perf.GetProperty("cells");
        if (cells.ValueKind != JsonValueKind.Array) return new(0, 0, 0, 0, 0, 0);
        var centers = cells.EnumerateArray().Select(c => (X: c[0].GetDouble(), Y: c[1].GetDouble())).ToArray();
        var cellW = cells[0][2].GetDouble();
        var step = centers[1].X - centers[0].X;
        (double X, double Y) Target(int r, int c) => centers[r * 4 + c];

        var byId = new Dictionary<int, List<S>>();
        var frameTimes = new List<double>();
        var frame = 0;
        foreach (var sample in perf.GetProperty("samples").EnumerateArray())
        {
            var t = sample[0].GetDouble();
            frameTimes.Add(t);
            foreach (var x in sample[1].EnumerateArray())
            {
                var s = new S(frame, t, x[1].GetInt32(), x[2].GetInt32(), x[3].GetInt32(), x[4].GetString() ?? "",
                    x[5].GetDouble(), x[6].GetDouble(), x[7].GetDouble(), x[8].GetDouble());
                var id = x[0].GetInt32();
                if (!byId.TryGetValue(id, out var list)) byId[id] = list = [];
                list.Add(s);
            }
            frame++;
        }

        double Dist(S s) { var (tx, ty) = Target(s.Row, s.Col); return Math.Sqrt((s.X - tx) * (s.X - tx) + (s.Y - ty) * (s.Y - ty)); }

        int sources = 0, cut = 0, merges = 0, early = 0, snaps = 0, teleports = 0;
        var retiredByFrame = byId.Values.SelectMany(l => l).Where(s => s.Flag == "R").ToLookup(s => s.Frame);

        foreach (var (id, list) in byId)
        {
            // Merge sources: did they reach the target before being removed?
            if (list.Any(s => s.Flag == "R"))
            {
                sources++;
                if (Dist(list[^1]) > 0.25 * step) cut++;
            }

            // Merged tiles: did they become visible before their sources arrived?
            if (list[0].Flag == "M")
            {
                merges++;
                var firstVisible = list.FirstOrDefault(s => s.Op > 0.05 && s.W > 0.1 * cellW);
                if (firstVisible is not null &&
                    retiredByFrame[firstVisible.Frame].Any(r => r.Row == firstVisible.Row && r.Col == firstVisible.Col && Dist(r) > 0.25 * step))
                    early++;
            }

            for (var i = 1; i < list.Count; i++)
            {
                var (a, b) = (list[i - 1], list[i]);
                if (b.Frame != a.Frame + 1 || b.T - a.T > 25) continue; // only judge consecutive, undropped frames
                if (b.W - a.W > 0.2 * cellW && b.W >= 0.98 * cellW) snaps++; // a scale animation cut short
                var moved = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                if (moved >= 0.9 * step && Dist(b) < 1) teleports++; // arrived in one frame: no slide
            }
        }
        return new(sources, cut, merges, early, snaps, teleports);
    }
}
