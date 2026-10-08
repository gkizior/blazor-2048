using Game2048.Core;

namespace Blazor2048.Tests;

public class SlideRowTests
{
    [Theory]
    [InlineData(new[] { 2, 2, 2, 2 }, new[] { 4, 4, 0, 0 }, 8)]
    [InlineData(new[] { 2, 2, 4, 0 }, new[] { 4, 4, 0, 0 }, 4)]
    [InlineData(new[] { 0, 0, 0, 2 }, new[] { 2, 0, 0, 0 }, 0)]
    [InlineData(new[] { 2, 0, 2, 0 }, new[] { 4, 0, 0, 0 }, 4)]
    [InlineData(new[] { 4, 4, 8, 8 }, new[] { 8, 16, 0, 0 }, 24)]
    [InlineData(new[] { 2, 4, 2, 4 }, new[] { 2, 4, 2, 4 }, 0)]
    [InlineData(new[] { 4, 2, 2, 0 }, new[] { 4, 4, 0, 0 }, 4)]
    [InlineData(new[] { 8, 0, 8, 8 }, new[] { 16, 8, 0, 0 }, 16)]
    [InlineData(new[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 }, 0)]
    public void SlideRow_MovesAndMergesCorrectly(int[] input, int[] expected, int expectedScore)
    {
        var (row, gained, _) = Game.SlideRow(input);
        Assert.Equal(expected, row);
        Assert.Equal(expectedScore, gained);
    }
}

public class GameTests
{
    private static Game GameWith(int[,] board) 
    {
        var game = new Game(4, new Random(42));
        game.SetBoard(board);
        return game;
    }

    private static int[] Row(Game g, int r) => Enumerable.Range(0, 4).Select(c => g.Board[r, c]).ToArray();
    private static int[] Col(Game g, int c) => Enumerable.Range(0, 4).Select(r => g.Board[r, c]).ToArray();
    private static int TileCount(Game g) => g.Board.Cast<int>().Count(v => v != 0);

    [Fact]
    public void NewGame_StartsWithTwoTiles()
    {
        var game = new Game(4, new Random(1));
        Assert.Equal(2, TileCount(game));
        Assert.All(game.Board.Cast<int>().Where(v => v != 0), v => Assert.Contains(v, new[] { 2, 4 }));
        Assert.Equal(0, game.Score);
    }

    [Fact]
    public void MoveRight_MergesTowardRightEdge()
    {
        var game = GameWith(new int[,]
        {
            { 2, 2, 2, 2 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        Assert.True(game.Move(Direction.Right));
        Assert.Equal(new[] { 0, 0, 4, 4 }, Row(game, 0));
        Assert.Equal(8, game.Score);
    }

    [Fact]
    public void MoveUp_MergesColumns()
    {
        var game = GameWith(new int[,]
        {
            { 2, 0, 0, 0 },
            { 2, 0, 0, 0 },
            { 4, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        Assert.True(game.Move(Direction.Up));
        Assert.Equal(new[] { 4, 4 }, Col(game, 0).Take(2));
    }

    [Fact]
    public void MoveDown_MergesColumns()
    {
        var game = GameWith(new int[,]
        {
            { 2, 0, 0, 0 },
            { 2, 0, 0, 0 },
            { 2, 0, 0, 0 },
            { 2, 0, 0, 0 },
        });
        Assert.True(game.Move(Direction.Down));
        Assert.Equal(new[] { 4, 4 }, Col(game, 0).Skip(2));
    }

    [Fact]
    public void MoveThatChangesBoard_SpawnsExactlyOneTile()
    {
        var game = GameWith(new int[,]
        {
            { 2, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        Assert.True(game.Move(Direction.Right));
        Assert.Equal(2, TileCount(game));
        Assert.NotNull(game.SpawnedCell);
    }

    [Fact]
    public void MoveThatDoesNotChangeBoard_DoesNotSpawn()
    {
        var game = GameWith(new int[,]
        {
            { 2, 4, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        Assert.False(game.Move(Direction.Left));
        Assert.Equal(2, TileCount(game));
        Assert.Null(game.SpawnedCell);
    }

    [Fact]
    public void SpawnedTiles_AreMostlyTwos()
    {
        var game = new Game(4, new Random(123));
        int twos = 0, fours = 0;
        for (var i = 0; i < 2000; i++)
        {
            game.SetBoard(new int[4, 4]);
            game.AddRandomTile();
            var v = game.Board.Cast<int>().Single(x => x != 0);
            if (v == 2) twos++; else if (v == 4) fours++; else Assert.Fail($"Unexpected tile {v}");
        }
        Assert.InRange(fours / 2000.0, 0.06, 0.14);
    }

    [Fact]
    public void ReachingWinningTile_SetsHasWon()
    {
        var game = GameWith(new int[,]
        {
            { 1024, 1024, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        game.Move(Direction.Left);
        Assert.True(game.HasWon);
        Assert.Equal(2048, game.Board[0, 0]);
    }

    [Fact]
    public void FullBoardWithNoMerges_IsGameOver()
    {
        var game = GameWith(new int[,]
        {
            { 2, 4, 2, 4 },
            { 4, 2, 4, 2 },
            { 2, 4, 2, 4 },
            { 4, 2, 4, 2 },
        });
        Assert.True(game.IsGameOver);
        Assert.False(game.Move(Direction.Left));
    }

    [Fact]
    public void FullBoardWithMergePossible_IsNotGameOver()
    {
        var game = GameWith(new int[,]
        {
            { 2, 2, 4, 8 },
            { 4, 8, 16, 32 },
            { 8, 16, 32, 64 },
            { 16, 32, 64, 128 },
        });
        Assert.False(game.IsGameOver);
    }
}
