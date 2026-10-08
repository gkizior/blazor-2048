using Game2048.Core;

namespace Blazor2048.Tests;

/// <summary>The secret 1×1 and 0×0 boards win right away (spec 011, User Story 7).</summary>
public class SecretSizesTests
{
    private static readonly Direction[] AllDirections = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    [Fact]
    public void One_By_One_Holds_A_Single_Tile_That_Is_Already_The_Target()
    {
        var game = new Game(1, new Random(1));

        Assert.True(game.IsInstantWin);
        Assert.Equal(256, game.WinningTile);
        var tile = Assert.Single(game.Tiles);
        Assert.Equal(256, tile.Value);
        Assert.Equal((0, 0), (tile.Row, tile.Col));
        Assert.True(tile.IsNew); // it pops in like any new tile
        Assert.Equal(256, game[0, 0]);
        Assert.Equal(new int[1, 1] { { 256 } }, game.Board);
        Assert.Equal(0, game.Score);
        Assert.True(game.HasWon);
        Assert.True(game.IsGameOver);
        Assert.False(game.CanMove());
        Assert.False(game.AddRandomTile());
        Assert.Single(game.RenderTiles);
        Assert.Empty(game.RetiredTiles);
    }

    [Fact]
    public void Zero_By_Zero_Is_An_Empty_Board_Already_Won()
    {
        var game = new Game(0, new Random(1));

        Assert.True(game.IsInstantWin);
        Assert.Equal(128, game.WinningTile);
        Assert.Equal(0, game.Size);
        Assert.Empty(game.Tiles);
        Assert.Empty(game.RenderTiles);
        Assert.Empty(game.RetiredTiles);
        Assert.Equal(0, game.Board.Length);
        Assert.Null(game.SpawnedCell);
        Assert.Equal(0, game.Score);
        Assert.True(game.HasWon);
        Assert.True(game.IsGameOver);
        Assert.False(game.CanMove());
        Assert.False(game.AddRandomTile());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void No_Move_Changes_Anything_And_Nothing_Throws(int size)
    {
        var game = new Game(size, new Random(7));
        var before = game.Board;

        foreach (var d in AllDirections) Assert.False(game.Move(d));

        Assert.Equal(before, game.Board);
        Assert.Equal(0, game.Score);
        Assert.True(game.HasWon);
        Assert.False(game.KeepPlaying);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Continue_Has_Nothing_To_Continue(int size)
    {
        var game = new Game(size);
        game.Continue();
        Assert.False(game.KeepPlaying);
        Assert.True(game.HasWon);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void New_Games_Win_Again_And_Sizes_Switch_Both_Ways(int size)
    {
        var game = new Game(4, new Random(3));
        game.Move(Direction.Left);

        game.NewGame(size);
        Assert.True(game.IsInstantWin);
        Assert.True(game.HasWon);
        Assert.Equal(size, game.Tiles.Count); // 1×1: one tile, 0×0: none

        game.NewGame();
        Assert.True(game.HasWon);
        Assert.Equal(size, game.Tiles.Count);

        game.NewGame(6);
        Assert.False(game.IsInstantWin);
        Assert.False(game.HasWon);
        Assert.False(game.IsGameOver);
        Assert.Equal(2, game.Tiles.Count);
        Assert.Equal(8192, game.WinningTile);
        Assert.True(game.Move(Direction.Left) || game.Move(Direction.Right) || game.Move(Direction.Up) || game.Move(Direction.Down));
    }

    [Fact]
    public void Replaying_One_By_One_Gives_The_Tile_A_New_Id_So_It_Pops_Again()
    {
        var game = new Game(1);
        var first = Assert.Single(game.Tiles).Id;
        game.NewGame();
        Assert.NotEqual(first, Assert.Single(game.Tiles).Id);
    }

    [Fact]
    public void SetBoard_Works_On_Tiny_Boards()
    {
        var zero = new Game(0);
        zero.SetBoard(new int[0, 0]);
        Assert.True(zero.IsGameOver);

        var one = new Game(1);
        one.SetBoard(new int[1, 1] { { 0 } });
        Assert.False(one.IsGameOver); // an empty cell can still take a spawn
        Assert.True(one.AddRandomTile());
        Assert.False(one.CanMove());
    }
}
