namespace Game2048.Core;

public enum Direction { Up, Down, Left, Right }

/// <summary>Pure 2048 game logic for a square board (default 4x4).</summary>
public sealed class Game
{
    public const int WinningTile = 2048;

    private readonly Random _random;

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

    public Game(int size = 4, Random? random = null)
    {
        Size = size;
        _random = random ?? new Random();
        Board = new int[size, size];
        NewGame();
    }

    public void NewGame()
    {
        Board = new int[Size, Size];
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
        Score = score;
        IsGameOver = !CanMove();
    }

    /// <summary>
    /// Slides one row toward index 0 and merges equal neighbours.
    /// Each tile merges at most once per move.
    /// </summary>
    public static (int[] Row, int Gained, bool[] Merged) SlideRow(IReadOnlyList<int> row)
    {
        var n = row.Count;
        var result = new int[n];
        var merged = new bool[n];
        var gained = 0;
        var target = 0;
        var canMergeWithPrevious = false;

        foreach (var value in row)
        {
            if (value == 0) continue;

            if (canMergeWithPrevious && result[target - 1] == value)
            {
                result[target - 1] = value * 2;
                merged[target - 1] = true;
                gained += value * 2;
                canMergeWithPrevious = false; // no double merge
            }
            else
            {
                result[target++] = value;
                canMergeWithPrevious = true;
            }
        }

        return (result, gained, merged);
    }

    /// <summary>Performs a move. Returns true if the board changed.</summary>
    public bool Move(Direction direction)
    {
        if (IsGameOver) return false;

        var changed = false;
        MergedCells.Clear();
        SpawnedCell = null;

        for (var line = 0; line < Size; line++)
        {
            var cells = LineCells(direction, line);
            var values = cells.Select(c => Board[c.Row, c.Col]).ToArray();
            var (slid, gained, merged) = SlideRow(values);

            for (var i = 0; i < Size; i++)
            {
                var (r, c) = cells[i];
                if (Board[r, c] != slid[i]) changed = true;
                Board[r, c] = slid[i];
                if (merged[i]) MergedCells.Add((r, c));
                if (slid[i] >= WinningTile) HasWon = true;
            }
            Score += gained;
        }

        if (changed)
        {
            AddRandomTile();
            IsGameOver = !CanMove();
        }

        return changed;
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
        SpawnedCell = (row, col);
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
