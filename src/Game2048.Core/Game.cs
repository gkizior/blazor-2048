namespace Game2048.Core;

public enum Direction { Up, Down, Left, Right }

/// <summary>
/// A tile as the UI sees it. <see cref="Id"/> is stable while a tile slides, so the UI can key
/// elements by it and let CSS transitions animate the move. A merge retires both source tiles
/// and creates a new tile (new id) with the doubled value.
/// </summary>
/// <param name="Id">Unique, increasing id. Never reused within a game instance.</param>
/// <param name="Value">Tile value (2, 4, 8, ...).</param>
/// <param name="Row">Current row (for a retired tile: the cell it merged into).</param>
/// <param name="Col">Current column (for a retired tile: the cell it merged into).</param>
/// <param name="IsNew">Spawned by the last move (or by a new game).</param>
/// <param name="IsMerged">Created by a merge on the last move.</param>
/// <param name="IsRetired">Merged away on the last move; only kept so the UI can finish its slide.</param>
public readonly record struct Tile(int Id, int Value, int Row, int Col,
    bool IsNew = false, bool IsMerged = false, bool IsRetired = false);

/// <summary>Pure 2048 game logic for a square board (default 4x4).</summary>
public sealed class Game
{
    public const int WinningTile = 2048;

    private readonly Random _random;
    private int[,] _ids;
    private int _nextId = 1;
    private readonly List<Tile> _retired = new();

    public int Size { get; }
    public int[,] Board { get; private set; }
    public int Score { get; private set; }
    public bool HasWon { get; private set; }
    public bool KeepPlaying { get; private set; }
    public bool IsGameOver { get; private set; }

    /// <summary>Cells that were created by a merge on the last move.</summary>
    public HashSet<(int Row, int Col)> MergedCells { get; } = new();

    /// <summary>The cell where the last random tile spawned.</summary>
    public (int Row, int Col)? SpawnedCell { get; private set; }

    /// <summary>Every cell that received a spawned tile since the last move (two after a new game).</summary>
    public HashSet<(int Row, int Col)> SpawnedCells { get; } = new();

    /// <summary>
    /// Tiles that were merged away on the last move, positioned on the cell they merged into.
    /// The UI keeps rendering them (under the merged tile) so their slide can finish.
    /// </summary>
    public IReadOnlyList<Tile> RetiredTiles => _retired;

    public Game(int size = 4, Random? random = null)
    {
        Size = size;
        _random = random ?? new Random();
        Board = new int[size, size];
        _ids = new int[size, size];
        NewGame();
    }

    /// <summary>
    /// Live tiles, ordered by id. Ordering by id keeps the relative order of surviving tiles
    /// stable between moves, so a keyed UI list only inserts and removes elements and never
    /// reorders them (moving DOM nodes would cancel their CSS transitions).
    /// </summary>
    public IReadOnlyList<Tile> Tiles
    {
        get
        {
            var tiles = new List<Tile>(Size * Size);
            for (var r = 0; r < Size; r++)
            for (var c = 0; c < Size; c++)
            {
                if (Board[r, c] == 0) continue;
                tiles.Add(new Tile(_ids[r, c], Board[r, c], r, c,
                    IsNew: SpawnedCells.Contains((r, c)),
                    IsMerged: MergedCells.Contains((r, c))));
            }
            tiles.Sort((a, b) => a.Id.CompareTo(b.Id));
            return tiles;
        }
    }

    /// <summary>Retired and live tiles together, ordered by id (see <see cref="Tiles"/>). This is what the board renders.</summary>
    public IReadOnlyList<Tile> RenderTiles =>
        _retired.Concat(Tiles).OrderBy(t => t.Id).ToList();

    public void NewGame()
    {
        Board = new int[Size, Size];
        _ids = new int[Size, Size];
        _retired.Clear();
        SpawnedCells.Clear();
        Score = 0;
        HasWon = false;
        KeepPlaying = false;
        IsGameOver = false;
        MergedCells.Clear();
        SpawnedCell = null;
        AddRandomTile();
        AddRandomTile();
    }

    /// <summary>Lets the player continue after reaching 2048.</summary>
    public void Continue() => KeepPlaying = true;

    /// <summary>Loads a specific board (used by tests).</summary>
    public void SetBoard(int[,] board, int score = 0)
    {
        if (board.GetLength(0) != Size || board.GetLength(1) != Size)
            throw new ArgumentException("Board size mismatch.", nameof(board));
        Board = (int[,])board.Clone();
        _ids = new int[Size, Size];
        for (var r = 0; r < Size; r++)
        for (var c = 0; c < Size; c++)
            if (Board[r, c] != 0) _ids[r, c] = _nextId++;
        _retired.Clear();
        MergedCells.Clear();
        SpawnedCells.Clear();
        SpawnedCell = null;
        Score = score;
        IsGameOver = !CanMove();
    }

    /// <summary>
    /// Slides one row toward index 0 and merges equal neighbours.
    /// Each tile merges at most once per move.
    /// </summary>
    public static (int[] Row, int Gained, bool[] Merged) SlideRow(IReadOnlyList<int> row)
    {
        var (result, gained, merged, _) = SlideCore(row);
        return (result, gained, merged);
    }

    /// <summary>
    /// The move rule, plus where each source tile went: <c>Targets[i]</c> is the index tile
    /// <c>i</c> ended up in (-1 for empty cells). Two sources sharing a target merged.
    /// </summary>
    private static (int[] Row, int Gained, bool[] Merged, int[] Targets) SlideCore(IReadOnlyList<int> row)
    {
        var n = row.Count;
        var result = new int[n];
        var merged = new bool[n];
        var targets = new int[n];
        var gained = 0;
        var target = 0;
        var canMergeWithPrevious = false;

        for (var i = 0; i < n; i++)
        {
            var value = row[i];
            targets[i] = -1;
            if (value == 0) continue;

            if (canMergeWithPrevious && result[target - 1] == value)
            {
                result[target - 1] = value * 2;
                merged[target - 1] = true;
                gained += value * 2;
                targets[i] = target - 1;
                canMergeWithPrevious = false; // no double merge
            }
            else
            {
                targets[i] = target;
                result[target++] = value;
                canMergeWithPrevious = true;
            }
        }

        return (result, gained, merged, targets);
    }

    /// <summary>Performs a move. Returns true if the board changed.</summary>
    public bool Move(Direction direction)
    {
        if (IsGameOver) return false;

        var newBoard = new int[Size, Size];
        var newIds = new int[Size, Size];
        var merged = new List<(int Row, int Col)>();
        var retired = new List<Tile>();
        var gainedTotal = 0;
        var changed = false;

        for (var line = 0; line < Size; line++)
        {
            var cells = LineCells(direction, line);
            var values = cells.Select(c => Board[c.Row, c.Col]).ToArray();
            var (slid, gained, mergedFlags, targets) = SlideCore(values);

            for (var i = 0; i < Size; i++)
            {
                if (targets[i] < 0) continue;
                var (sr, sc) = cells[i];
                var (tr, tc) = cells[targets[i]];
                if (mergedFlags[targets[i]])
                {
                    // Both halves of a merge slide into the target cell and are retired;
                    // the merged tile gets a fresh id below so its "pop" animation plays.
                    retired.Add(new Tile(_ids[sr, sc], values[i], tr, tc, IsRetired: true));
                }
                else
                {
                    newIds[tr, tc] = _ids[sr, sc];
                }
            }

            for (var i = 0; i < Size; i++)
            {
                var (r, c) = cells[i];
                if (Board[r, c] != slid[i]) changed = true;
                newBoard[r, c] = slid[i];
                if (mergedFlags[i]) merged.Add((r, c));
            }
            gainedTotal += gained;
        }

        if (!changed) return false;

        Board = newBoard;
        Score += gainedTotal;
        MergedCells.Clear();
        SpawnedCells.Clear();
        SpawnedCell = null;
        _retired.Clear();
        _retired.AddRange(retired);
        foreach (var (r, c) in merged)
        {
            newIds[r, c] = _nextId++;
            MergedCells.Add((r, c));
        }
        if (Board.Cast<int>().Any(v => v >= WinningTile)) HasWon = true;
        _ids = newIds;

        AddRandomTile();
        IsGameOver = !CanMove();
        return true;
    }

    public bool CanMove()
    {
        for (var r = 0; r < Size; r++)
        for (var c = 0; c < Size; c++)
        {
            var v = Board[r, c];
            if (v == 0) return true;
            if (c + 1 < Size && Board[r, c + 1] == v) return true;
            if (r + 1 < Size && Board[r + 1, c] == v) return true;
        }
        return false;
    }

    /// <summary>Adds a 2 (90%) or 4 (10%) to a random empty cell.</summary>
    public bool AddRandomTile()
    {
        var empty = new List<(int, int)>();
        for (var r = 0; r < Size; r++)
        for (var c = 0; c < Size; c++)
            if (Board[r, c] == 0) empty.Add((r, c));

        if (empty.Count == 0) return false;

        var (row, col) = empty[_random.Next(empty.Count)];
        Board[row, col] = _random.NextDouble() < 0.9 ? 2 : 4;
        _ids[row, col] = _nextId++;
        SpawnedCell = (row, col);
        SpawnedCells.Add((row, col));
        return true;
    }

    /// <summary>Cells of a line, ordered from the edge tiles move toward.</summary>
    private (int Row, int Col)[] LineCells(Direction direction, int line)
    {
        var cells = new (int, int)[Size];
        for (var i = 0; i < Size; i++)
        {
            cells[i] = direction switch
            {
                Direction.Left => (line, i),
                Direction.Right => (line, Size - 1 - i),
                Direction.Up => (i, line),
                Direction.Down => (Size - 1 - i, line),
                _ => throw new ArgumentOutOfRangeException(nameof(direction)),
            };
        }
        return cells;
    }
}
