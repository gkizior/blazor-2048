using Game2048.Core;

namespace Blazor2048.Tests;

/// <summary>
/// Tile identity drives the slide/merge/spawn animations: ids must survive slides,
/// merges must retire both sources onto the target cell, and spawns must be flagged.
/// </summary>
public class TileTrackingTests
{
    private static Game GameWith(int[,] board)
    {
        var game = new Game(4, new Random(42));
        game.SetBoard(board);
        return game;
    }

    private static Tile At(Game g, int r, int c) => g.Tiles.Single(t => t.Row == r && t.Col == c);

    [Fact]
    public void NewGame_FlagsBothStartingTilesAsNew()
    {
        var game = new Game(4, new Random(3));
        Assert.Equal(2, game.Tiles.Count);
        Assert.All(game.Tiles, t => Assert.True(t.IsNew));
        Assert.Empty(game.RetiredTiles);
    }

    [Fact]
    public void Slide_KeepsTileId()
    {
        var game = GameWith(new int[,]
        {
            { 2, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var id = At(game, 0, 0).Id;

        Assert.True(game.Move(Direction.Right));

        var moved = At(game, 0, 3);
        Assert.Equal(id, moved.Id);
        Assert.False(moved.IsNew);
        Assert.False(moved.IsMerged);
    }

    [Fact]
    public void Merge_RetiresBothSourcesOntoTarget_AndCreatesNewTile()
    {
        var game = GameWith(new int[,]
        {
            { 0, 0, 0, 0 },
            { 4, 0, 4, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var sourceIds = new[] { At(game, 1, 0).Id, At(game, 1, 2).Id };

        Assert.True(game.Move(Direction.Left));

        Assert.Equal(sourceIds.Order(), game.RetiredTiles.Select(t => t.Id).Order());
        Assert.All(game.RetiredTiles, t => Assert.Equal((1, 0, 4, true), (t.Row, t.Col, t.Value, t.IsRetired)));

        var merged = At(game, 1, 0);
        Assert.Equal(8, merged.Value);
        Assert.True(merged.IsMerged);
        Assert.DoesNotContain(merged.Id, sourceIds);
        Assert.True(merged.Id > sourceIds.Max());
    }

    [Fact]
    public void Move_FlagsExactlyOneSpawn_WithAFreshId()
    {
        var game = GameWith(new int[,]
        {
            { 2, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var before = game.Tiles.Select(t => t.Id).ToHashSet();

        game.Move(Direction.Down);

        var spawned = Assert.Single(game.Tiles, t => t.IsNew);
        Assert.DoesNotContain(spawned.Id, before);
        Assert.Equal(game.SpawnedCell, (spawned.Row, spawned.Col));
    }

    [Fact]
    public void RetiredTiles_AreClearedByTheNextMove()
    {
        var game = GameWith(new int[,]
        {
            { 2, 2, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        game.Move(Direction.Left);
        Assert.Equal(2, game.RetiredTiles.Count);

        Assert.True(game.Move(Direction.Down));
        Assert.DoesNotContain(game.RetiredTiles, t => t.Value == 2 && t.Row == 0 && t.Col == 0);
    }

    [Fact]
    public void NoOpMove_LeavesTilesUntouched()
    {
        var game = GameWith(new int[,]
        {
            { 2, 4, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var before = game.Tiles.ToArray();

        Assert.False(game.Move(Direction.Left));
        Assert.Equal(before, game.Tiles);
    }

    [Fact]
    public void RenderTiles_AreOrderedById_AndIdsAreUnique()
    {
        var game = new Game(4, new Random(11));
        foreach (var d in Enumerable.Repeat(new[] { Direction.Left, Direction.Up, Direction.Right, Direction.Down }, 25).SelectMany(x => x))
        {
            game.Move(d);
            var ids = game.RenderTiles.Select(t => t.Id).ToArray();
            Assert.Equal(ids.Order(), ids);
            Assert.Equal(ids.Length, ids.Distinct().Count());
            // Board and tiles always agree.
            Assert.Equal(game.Board.Cast<int>().Count(v => v != 0), game.Tiles.Count);
            Assert.All(game.Tiles, t => Assert.Equal(game.Board[t.Row, t.Col], t.Value));
        }
    }
}
