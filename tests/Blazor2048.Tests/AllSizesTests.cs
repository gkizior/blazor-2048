using Game2048.Core;

namespace Blazor2048.Tests;

/// <summary>
/// The engine at every supported size, 2×2 to <see cref="BoardSize.Max"/>: moves, merges, spawns,
/// game over, the win target, property-style random play against a reference, and allocations.
/// </summary>
public class AllSizesTests
{
    public static TheoryData<int> Sizes()
    {
        var data = new TheoryData<int>();
        for (var n = BoardSize.Min; n <= BoardSize.Max; n++) data.Add(n);
        return data;
    }

    private static int[,] Empty(int n) => new int[n, n];

    private static int[,] Checkerboard(int n)
    {
        var board = Empty(n);
        for (var r = 0; r < n; r++)
        for (var c = 0; c < n; c++)
            board[r, c] = (r + c) % 2 == 0 ? 2 : 4;
        return board;
    }

    [Theory, MemberData(nameof(Sizes))]
    public void NewGame_Has_Two_Tiles_On_An_NxN_Board(int n)
    {
        var game = new Game(n, new Random(n));
        Assert.Equal(n, game.Size);
        Assert.Equal(n, game.Board.GetLength(0));
        Assert.Equal(n, game.Board.GetLength(1));
        Assert.Equal(2, game.Tiles.Count);
        Assert.All(game.Tiles, t => Assert.True(t.IsNew && t.Value is 2 or 4 && t.Row < n && t.Col < n));
        Assert.False(game.IsGameOver);
    }

    [Theory, MemberData(nameof(Sizes))]
    public void Moves_Slide_And_Merge_In_Every_Direction(int n)
    {
        // One full line of 2s along each direction: it merges pairwise toward the edge.
        foreach (var dir in Enum.GetValues<Direction>())
        {
            var game = new Game(n, new Random(1));
            var board = Empty(n);
            for (var i = 0; i < n; i++)
                if (dir is Direction.Left or Direction.Right) board[0, i] = 2; else board[i, 0] = 2;
            game.SetBoard(board);

            Assert.True(game.Move(dir));

            for (var i = 0; i < (n + 1) / 2; i++)
            {
                // i counts from the edge the tiles moved toward.
                var (r, c) = dir switch
                {
                    Direction.Left => (0, i),
                    Direction.Right => (0, n - 1 - i),
                    Direction.Up => (i, 0),
                    _ => (n - 1 - i, 0),
                };
                Assert.Equal(i < n / 2 ? 4 : 2, game[r, c]);
            }
            Assert.Equal(4 * (n / 2), game.Score);
            Assert.Equal(2 * (n / 2), game.RetiredTiles.Count);
            Assert.Equal(n / 2, game.Tiles.Count(t => t.IsMerged));
        }
    }

    [Theory, MemberData(nameof(Sizes))]
    public void A_Full_Board_With_No_Merges_Is_Game_Over(int n)
    {
        var game = new Game(n, new Random(1));
        game.SetBoard(Checkerboard(n));

        Assert.True(game.IsGameOver);
        Assert.False(game.CanMove());
        foreach (var dir in Enum.GetValues<Direction>()) Assert.False(game.Move(dir));
    }

    [Theory, MemberData(nameof(Sizes))]
    public void A_Full_Board_With_One_Pair_Can_Still_Move(int n)
    {
        var game = new Game(n, new Random(1));
        var board = Checkerboard(n);
        board[n - 1, n - 1] = board[n - 1, n - 2]; // a pair in the last corner
        game.SetBoard(board);

        Assert.False(game.IsGameOver);
        Assert.True(game.Move(Direction.Right));
    }

    [Theory, MemberData(nameof(Sizes))]
    public void Reaching_The_Target_Wins_And_Half_Of_It_Does_Not(int n)
    {
        var target = BoardSize.WinningTile(n);
        var game = new Game(n, new Random(1));
        var board = Empty(n);
        board[0, 0] = board[0, 1] = target / 4;
        game.SetBoard(board);
        game.Move(Direction.Left);
        Assert.False(game.HasWon);

        board = Empty(n);
        board[n - 1, 0] = board[n - 1, 1] = target / 2;
        game.SetBoard(board);
        game.Move(Direction.Left);
        Assert.True(game.HasWon);
        Assert.Equal(target, game[n - 1, 0]);
    }

    [Theory, MemberData(nameof(Sizes))]
    public void Random_Play_Matches_The_Reference_Rule_And_Keeps_Invariants(int n)
    {
        var rng = new Random(1000 + n);
        var game = new Game(n, new Random(n * 31));
        var dirs = Enum.GetValues<Direction>();
        for (var step = 0; step < 400; step++)
        {
            if (game.IsGameOver) game.NewGame();
            var before = game.Board;
            var score = game.Score;
            var dir = dirs[rng.Next(4)];
            var (expected, gained) = Reference(before, dir);
            var changed = game.Move(dir);
            var after = game.Board;

            Assert.Equal(!SameBoard(before, expected), changed);
            if (!changed)
            {
                Assert.True(SameBoard(before, after));
                continue;
            }
            Assert.Equal(score + gained, game.Score);

            // The board equals the reference except for exactly one spawned 2 or 4 on an empty cell.
            var (sr, sc) = game.SpawnedCell!.Value;
            Assert.Equal(0, expected[sr, sc]);
            Assert.Contains(after[sr, sc], new[] { 2, 4 });
            expected[sr, sc] = after[sr, sc];
            Assert.True(SameBoard(expected, after));

            // Tiles: unique ids in id order, one live tile per non-empty cell, values powers of two.
            var render = game.RenderTiles;
            for (var i = 1; i < render.Count; i++) Assert.True(render[i - 1].Id < render[i].Id);
            var live = game.Tiles;
            Assert.Equal(after.Cast<int>().Count(v => v != 0), live.Count);
            Assert.All(live, t => Assert.Equal(after[t.Row, t.Col], t.Value));
            Assert.All(live, t => Assert.True(int.IsPow2(t.Value)));
            Assert.Equal(render.Count, live.Count + game.RetiredTiles.Count);
            Assert.Single(live, t => t.IsNew);
        }
    }

    /// <summary>The move rule written the obvious way (2D array, one line at a time with SlideRow).</summary>
    private static (int[,] Board, int Gained) Reference(int[,] board, Direction dir)
    {
        var n = board.GetLength(0);
        var result = new int[n, n];
        var gained = 0;
        for (var line = 0; line < n; line++)
        {
            (int R, int C) Cell(int i) => dir switch
            {
                Direction.Left => (line, i),
                Direction.Right => (line, n - 1 - i),
                Direction.Up => (i, line),
                _ => (n - 1 - i, line),
            };
            var values = Enumerable.Range(0, n).Select(i => board[Cell(i).R, Cell(i).C]).ToArray();
            var (slid, g, _) = Game.SlideRow(values);
            gained += g;
            for (var i = 0; i < n; i++) result[Cell(i).R, Cell(i).C] = slid[i];
        }
        return (result, gained);
    }

    private static bool SameBoard(int[,] a, int[,] b) => a.Cast<int>().SequenceEqual(b.Cast<int>());

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(BoardSize.Max)]
    public void Moves_And_The_Render_List_Do_Not_Allocate(int n)
    {
        var game = new Game(n, new Random(5));
        var dirs = new[] { Direction.Left, Direction.Down, Direction.Right, Direction.Down, Direction.Up };

        void Play(int moves)
        {
            for (var i = 0; i < moves; i++)
            {
                if (game.IsGameOver) game.NewGame();
                game.Move(dirs[i % dirs.Length]);
                _ = game.RenderTiles.Count; // what the board reads every render
            }
        }

        Play(3000); // warm up: JIT, and the render list growing to its largest size
        var before = GC.GetAllocatedBytesForCurrentThread();
        Play(2000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Less than one byte per move: no per-move arrays, lists, LINQ or boxing. (A few hundred bytes
        // can come from the runtime itself, e.g. tiered JIT, not from the game.)
        Assert.True(allocated < 2000, $"{allocated} bytes allocated over 2000 moves on {n}x{n}");
    }

    [Fact]
    public void Switching_Sizes_Leaves_Nothing_Stale()
    {
        var game = new Game(4, new Random(2));
        foreach (var n in new[] { 10, 2, BoardSize.Max, 5, 4 })
        {
            game.NewGame(n);
            Assert.Equal(n, game.Size);
            Assert.Equal(2, game.Tiles.Count);
            Assert.Empty(game.RetiredTiles);
            Assert.Equal(2, game.RenderTiles.Count);
            Assert.Equal(BoardSize.WinningTile(n), game.WinningTile);
            Assert.False(game.HasWon);
        }
    }
}
