using Game2048.Core;

namespace Blazor2048.Tests;

/// <summary>The secret 1×1 board wins right away; there is no 0×0 board (spec 011, User Story 7).</summary>
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
    public void No_Move_Changes_Anything_And_Nothing_Throws()
    {
        var game = new Game(1, new Random(7));
        var before = game.Board;

        foreach (var d in AllDirections) Assert.False(game.Move(d));

        Assert.Equal(before, game.Board);
        Assert.Equal(0, game.Score);
        Assert.True(game.HasWon);
        Assert.False(game.KeepPlaying);
    }

    [Fact]
    public void Continue_Has_Nothing_To_Continue()
    {
        var game = new Game(1);
        game.Continue();
        Assert.False(game.KeepPlaying);
        Assert.True(game.HasWon);
    }

    [Fact]
    public void New_Games_Win_Again_And_Sizes_Switch_Both_Ways()
    {
        var game = new Game(4, new Random(3));
        game.Move(Direction.Left);

        game.NewGame(1);
        Assert.True(game.IsInstantWin);
        Assert.True(game.HasWon);
        Assert.Single(game.Tiles);

        game.NewGame();
        Assert.True(game.HasWon);
        Assert.Single(game.Tiles);

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
    public void SetBoard_Works_On_The_1x1_Board()
    {
        var one = new Game(1);
        one.SetBoard(new int[1, 1] { { 0 } });
        Assert.False(one.IsGameOver); // an empty cell can still take a spawn
        Assert.True(one.AddRandomTile());
        Assert.False(one.CanMove());
    }

    [Fact]
    public void There_Is_No_Zero_By_Zero_Board()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Game(0));
        var game = new Game(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => game.NewGame(0));
        Assert.Equal(1, game.Size); // unchanged
        Assert.False(BoardSize.TryParse("0", out _, out _));
    }
}
