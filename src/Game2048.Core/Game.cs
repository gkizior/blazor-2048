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

/// <summary>
/// Pure 2048 game logic for a square N×N board (default 4x4, see <see cref="BoardSize"/>).
/// </summary>
/// <remarks>
/// Built for big boards: cells, tile ids and flags live in flat arrays (index = row * Size + col),
/// a move works through reusable line buffers into a second set of arrays that is then swapped in,
/// and the id-ordered list the UI renders is rebuilt into one reused list. Buffers are allocated
/// only when the board size changes, so a move allocates nothing (see the allocation tests).
/// </remarks>
public sealed class Game
{
    /// <summary>The classic (4×4) win tile; other sizes use <see cref="BoardSize.WinningTile"/>.</summary>
    public const int ClassicWinningTile = 2048;

    private const byte FlagNew = 1, FlagMerged = 2;

    private readonly Random _random;
    private int _nextId = 1;

    // Current state, flat (row-major) and double-buffered with the _next* arrays.
    private int[] _cells = [], _ids = [];
    private byte[] _flags = [];
    private int[] _nextCells = [], _nextIds = [];
    private byte[] _nextFlags = [];

    // Per-line scratch buffers (length Size), reused by every move.
    private int[] _lineIndex = [], _lineValues = [], _lineResult = [], _lineTargets = [];
    private bool[] _lineMerged = [];

    private readonly List<Tile> _retired = new();
    private readonly List<Tile> _render = new();
    private bool _renderStale = true;
    private int _winningTile;

    private static readonly Comparison<Tile> ById = static (a, b) => a.Id.CompareTo(b.Id);

    public int Size { get; private set; }

    /// <summary>The tile that wins on this board size, 2^(N+7) (also the game's "name").</summary>
    public int WinningTile => _winningTile;

    /// <summary>The value at a cell (0 = empty).</summary>
    public int this[int row, int col] => _cells[row * Size + col];

    /// <summary>A copy of the board as a 2D array (for tests and tools; the UI uses <see cref="RenderTiles"/>).</summary>
    public int[,] Board
    {
        get
        {
            var board = new int[Size, Size];
            for (var i = 0; i < _cells.Length; i++) board[i / Size, i % Size] = _cells[i];
            return board;
        }
    }

    public int Score { get; private set; }
    public bool HasWon { get; private set; }
    public bool KeepPlaying { get; private set; }
    public bool IsGameOver { get; private set; }

    /// <summary>The cell where the last random tile spawned.</summary>
    public (int Row, int Col)? SpawnedCell { get; private set; }

    /// <summary>
    /// Tiles that were merged away on the last move, positioned on the cell they merged into.
    /// The UI keeps rendering them (under the merged tile) so their slide can finish.
    /// </summary>
    public IReadOnlyList<Tile> RetiredTiles => _retired;

    public Game(int size = BoardSize.Default, Random? random = null)
    {
        if (!BoardSize.IsValid(size)) throw new ArgumentOutOfRangeException(nameof(size), size, $"Board size must be {BoardSize.RangeText}.");
        _random = random ?? new Random();
        Resize(size);
        NewGame();
    }

    /// <summary>
    /// Live tiles, ordered by id, as a new list (for tests; the UI uses <see cref="RenderTiles"/>).
    /// </summary>
    public IReadOnlyList<Tile> Tiles
    {
        get
        {
            var tiles = new List<Tile>(Size * Size);
            AddLiveTiles(tiles);
            tiles.Sort(ById);
            return tiles;
        }
    }

    /// <summary>
    /// Retired and live tiles together, ordered by id. Ordering by id keeps the relative order of
    /// surviving tiles stable between moves, so a keyed UI list only inserts and removes elements
    /// and never reorders them (moving DOM nodes would cancel their CSS transitions). This is what
    /// the board renders. The same list instance is reused and rebuilt after each change, so read
    /// it, do not keep it.
    /// </summary>
    public IReadOnlyList<Tile> RenderTiles
    {
        get
        {
            if (_renderStale)
            {
                _render.Clear();
                _render.AddRange(_retired);
                AddLiveTiles(_render);
                _render.Sort(ById);
                _renderStale = false;
            }
            return _render;
        }
    }

    private void AddLiveTiles(List<Tile> tiles)
    {
        var n = Size;
        for (var i = 0; i < _cells.Length; i++)
        {
            if (_cells[i] == 0) continue;
            var f = _flags[i];
            tiles.Add(new Tile(_ids[i], _cells[i], i / n, i % n, IsNew: (f & FlagNew) != 0, IsMerged: (f & FlagMerged) != 0));
        }
    }

    /// <summary>Starts a new game on a board of <paramref name="size"/>×<paramref name="size"/>.</summary>
    public void NewGame(int size)
    {
        if (!BoardSize.IsValid(size)) throw new ArgumentOutOfRangeException(nameof(size), size, $"Board size must be {BoardSize.RangeText}.");
        if (size != Size) Resize(size);
        NewGame();
    }

    public void NewGame()
    {
        ClearState();
        Score = 0;
        HasWon = false;
        KeepPlaying = false;
        IsGameOver = false;
        AddRandomTile();
        AddRandomTile();
    }

    /// <summary>Lets the player continue after reaching the target tile.</summary>
    public void Continue() => KeepPlaying = true;

    /// <summary>Loads a specific board (used by tests).</summary>
    public void SetBoard(int[,] board, int score = 0)
    {
        if (board.GetLength(0) != Size || board.GetLength(1) != Size)
            throw new ArgumentException("Board size mismatch.", nameof(board));
        ClearState();
        for (var i = 0; i < _cells.Length; i++)
        {
            var v = board[i / Size, i % Size];
            _cells[i] = v;
            if (v != 0) _ids[i] = _nextId++;
        }
        Score = score;
        IsGameOver = !CanMove();
    }

    // Buffers are sized here and only here: changing the board size is the one place a game allocates.
    private void Resize(int size)
    {
        Size = size;
        _winningTile = BoardSize.WinningTile(size);
        var cells = size * size;
        _cells = new int[cells]; _ids = new int[cells]; _flags = new byte[cells];
        _nextCells = new int[cells]; _nextIds = new int[cells]; _nextFlags = new byte[cells];
        _lineIndex = new int[size]; _lineValues = new int[size]; _lineResult = new int[size]; _lineTargets = new int[size];
        _lineMerged = new bool[size];
        _retired.Clear();
        _render.Clear();
        // Drop capacity grown for a bigger board, so switching back down really frees it.
        if (_render.Capacity > 2 * cells) _render.Capacity = 2 * cells;
        if (_retired.Capacity > cells) _retired.Capacity = cells;
    }

    private void ClearState()
    {
        Array.Clear(_cells); Array.Clear(_ids); Array.Clear(_flags);
        _retired.Clear();
        SpawnedCell = null;
        _renderStale = true;
    }

    /// <summary>
    /// Slides one row toward index 0 and merges equal neighbours.
    /// Each tile merges at most once per move. (Allocates its results; moves use <see cref="SlideLine"/>.)
    /// </summary>
    public static (int[] Row, int Gained, bool[] Merged) SlideRow(IReadOnlyList<int> row)
    {
        var values = row.ToArray();
        var result = new int[values.Length];
        var merged = new bool[values.Length];
        var gained = SlideLine(values, result, merged, new int[values.Length]);
        return (result, gained, merged);
    }

    /// <summary>
    /// The move rule on one line, without allocating: slides <paramref name="values"/> toward index 0
    /// into <paramref name="result"/>, sets <paramref name="merged"/> where a merge happened, and
    /// records in <paramref name="targets"/> the index each source tile ended up in (-1 for empty
    /// cells; two sources sharing a target merged). Returns the points gained.
    /// </summary>
    public static int SlideLine(ReadOnlySpan<int> values, Span<int> result, Span<bool> merged, Span<int> targets)
    {
        result.Clear();
        merged.Clear();
        var gained = 0;
        var target = 0;
        var canMergeWithPrevious = false;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
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
        return gained;
    }

    /// <summary>Performs a move. Returns true if the board changed.</summary>
    public bool Move(Direction direction)
    {
        if (IsGameOver) return false;

        var n = Size;
        var cells = _cells;
        var ids = _ids;
        var newCells = _nextCells;
        var newIds = _nextIds;
        var newFlags = _nextFlags;
        Array.Clear(newIds);
        Array.Clear(newFlags);

        // Retired tiles are only written once we know the move changed something.
        var retiredCleared = false;
        var gainedTotal = 0;
        var changed = false;

        for (var line = 0; line < n; line++)
        {
            FillLineIndex(direction, line);
            for (var i = 0; i < n; i++) _lineValues[i] = cells[_lineIndex[i]];
            gainedTotal += SlideLine(_lineValues, _lineResult, _lineMerged, _lineTargets);

            for (var i = 0; i < n; i++)
            {
                var t = _lineTargets[i];
                if (t < 0) continue;
                var source = _lineIndex[i];
                var dest = _lineIndex[t];
                if (_lineMerged[t])
                {
                    // Both halves of a merge slide into the target cell and are retired;
                    // the merged tile gets a fresh id below so its "pop" animation plays.
                    if (!retiredCleared) { retiredCleared = true; _retired.Clear(); }
                    _retired.Add(new Tile(ids[source], _lineValues[i], dest / n, dest % n, IsRetired: true));
                }
                else
                {
                    newIds[dest] = ids[source];
                }
            }

            for (var i = 0; i < n; i++)
            {
                var idx = _lineIndex[i];
                var v = _lineResult[i];
                if (cells[idx] != v) changed = true;
                newCells[idx] = v;
                if (_lineMerged[i])
                {
                    newFlags[idx] = FlagMerged;
                    if (v >= _winningTile) HasWon = true;
                }
            }
        }

        // Nothing moved, so nothing merged either: the last move's retired tiles stay as they were.
        if (!changed) return false;
        if (!retiredCleared) _retired.Clear();

        // Merged tiles get fresh ids in row-major order (stable, and newer than every survivor).
        for (var i = 0; i < newCells.Length; i++)
            if (newFlags[i] == FlagMerged) newIds[i] = _nextId++;

        // Swap the buffers in.
        (_cells, _nextCells) = (newCells, cells);
        (_ids, _nextIds) = (newIds, ids);
        (_flags, _nextFlags) = (newFlags, _flags);
        Score += gainedTotal;
        SpawnedCell = null;
        _renderStale = true;

        AddRandomTile();
        IsGameOver = !CanMove();
        return true;
    }

    public bool CanMove()
    {
        var n = Size;
        var cells = _cells;
        for (var i = 0; i < cells.Length; i++)
        {
            var v = cells[i];
            if (v == 0) return true;
            if (i % n + 1 < n && cells[i + 1] == v) return true;
            if (i + n < cells.Length && cells[i + n] == v) return true;
        }
        return false;
    }

    /// <summary>Adds a 2 (90%) or 4 (10%) to a random empty cell.</summary>
    public bool AddRandomTile()
    {
        var empty = 0;
        foreach (var v in _cells) if (v == 0) empty++;
        if (empty == 0) return false;

        // The k-th empty cell in row-major order (same choice as a list of empty cells would give).
        var k = _random.Next(empty);
        var index = 0;
        for (; index < _cells.Length; index++)
            if (_cells[index] == 0 && k-- == 0) break;

        _cells[index] = _random.NextDouble() < 0.9 ? 2 : 4;
        _ids[index] = _nextId++;
        _flags[index] = FlagNew;
        SpawnedCell = (index / Size, index % Size);
        _renderStale = true;
        return true;
    }

    /// <summary>Flat indexes of a line's cells, ordered from the edge tiles move toward.</summary>
    private void FillLineIndex(Direction direction, int line)
    {
        var n = Size;
        var buf = _lineIndex;
        switch (direction)
        {
            case Direction.Left: for (var i = 0; i < n; i++) buf[i] = line * n + i; break;
            case Direction.Right: for (var i = 0; i < n; i++) buf[i] = line * n + (n - 1 - i); break;
            case Direction.Up: for (var i = 0; i < n; i++) buf[i] = i * n + line; break;
            case Direction.Down: for (var i = 0; i < n; i++) buf[i] = (n - 1 - i) * n + line; break;
            default: throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }
}
