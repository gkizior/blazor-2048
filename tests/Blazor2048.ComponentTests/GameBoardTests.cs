using Blazor2048.Components;
using Bunit;
using Game2048.Core;
using Microsoft.AspNetCore.Components.Web;

namespace Blazor2048.ComponentTests;

public class GameBoardTests : AppTestContext
{
    private Game game => Game;

    /// <summary>The 16 board values (row-major) from the live tiles' data attributes.</summary>
    private static int[] Tiles(IRenderedComponent<GameBoard> cut)
    {
        var values = new int[16];
        foreach (var tile in cut.FindAll(".tile:not(.tile-retired)"))
            values[int.Parse(tile.GetAttribute("data-row")!) * 4 + int.Parse(tile.GetAttribute("data-col")!)] =
                int.Parse(tile.TextContent.Trim());
        return values;
    }

    private static string Score(IRenderedComponent<GameBoard> cut) => cut.FindAll(".score-box .value")[0].TextContent;
    private static string Best(IRenderedComponent<GameBoard> cut) => cut.FindAll(".score-box .value")[1].TextContent;

    [Fact]
    public void Renders_4x4_Board_With_Two_Starting_Tiles()
    {
        var cut = Render<GameBoard>();

        Assert.Equal(16, cut.FindAll(".cell").Count);
        Assert.Equal(2, cut.FindAll(".tile").Count);
        Assert.Equal("0", Score(cut));
        Assert.Contains("New Game", cut.Find(".sub .btn").TextContent);
        Assert.Equal("0", cut.Find(".game").GetAttribute("tabindex"));
    }

    [Fact]
    public void Renders_Classic_Tile_Classes()
    {
        game.SetBoard(new int[,]
        {
            { 2, 4, 8, 2048 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();

        Assert.NotNull(cut.Find(".tile.tile-2"));
        Assert.NotNull(cut.Find(".tile.tile-2048"));
    }

    [Fact]
    public void ArrowKey_Moves_Tiles_And_Updates_Score()
    {
        game.SetBoard(new int[,]
        {
            { 2, 2, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 4, 0, 0, 4 },
        });
        var cut = Render<GameBoard>();

        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        var tiles = Tiles(cut);
        Assert.Equal(4, tiles[0]);    // 2+2 merged into the top-left corner
        Assert.Equal(8, tiles[12]);   // 4+4 merged into the bottom-left corner
        Assert.Equal("12", Score(cut));
        Assert.Equal("12", Best(cut));
        Assert.Equal(3, tiles.Count(v => v != 0)); // two merged tiles + one new spawn
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "localStorage.setItem");
    }

    [Fact]
    public void Swipe_Right_Moves_Tiles()
    {
        game.SetBoard(new int[,]
        {
            { 2, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();
        var root = cut.Find(".game");

        root.TouchStart(new TouchEventArgs { Touches = [new TouchPoint { ClientX = 50, ClientY = 300 }] });
        root.TouchEnd(new TouchEventArgs { ChangedTouches = [new TouchPoint { ClientX = 250, ClientY = 310 }] });

        Assert.Equal(2, Tiles(cut)[3]);
    }

    [Fact]
    public void Tap_Is_Not_A_Swipe()
    {
        game.SetBoard(new int[,]
        {
            { 2, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();
        var root = cut.Find(".game");

        root.TouchStart(new TouchEventArgs { Touches = [new TouchPoint { ClientX = 100, ClientY = 300 }] });
        root.TouchEnd(new TouchEventArgs { ChangedTouches = [new TouchPoint { ClientX = 105, ClientY = 302 }] });

        Assert.Equal(1, Tiles(cut).Count(v => v != 0));
    }

    [Fact]
    public void NewGame_Button_Resets_Board_And_Score()
    {
        game.SetBoard(new int[,]
        {
            { 2, 2, 8, 16 },
            { 32, 64, 4, 2 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        }, score: 500);
        var cut = Render<GameBoard>();
        Assert.Equal("500", Score(cut));

        cut.Find(".sub .btn").Click();

        Assert.Equal("0", Score(cut));
        Assert.Equal(2, cut.FindAll(".tile").Count);
    }

    [Fact]
    public void Shows_GameOver_Overlay_When_No_Moves_Left()
    {
        game.SetBoard(new int[,]
        {
            { 2, 4, 2, 4 },
            { 4, 2, 4, 2 },
            { 2, 4, 2, 4 },
            { 4, 2, 4, 2 },
        });
        var cut = Render<GameBoard>();

        Assert.Contains("Game over!", cut.Find(".overlay.lose").TextContent);
        cut.Find(".overlay.lose .btn").Click();
        Assert.Empty(cut.FindAll(".overlay"));
    }

    [Fact]
    public void Shows_Win_Overlay_And_Keep_Going()
    {
        game.SetBoard(new int[,]
        {
            { 1024, 1024, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();

        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Contains("You win!", cut.Find(".overlay.win").TextContent);

        cut.Find(".overlay.win .btn").Click(); // Keep going
        Assert.Empty(cut.FindAll(".overlay"));
    }

    [Fact]
    public void Loads_Best_Score_From_LocalStorage()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.best").SetResult("4096");

        var cut = Render<GameBoard>();

        cut.WaitForAssertion(() => Assert.Equal("4096", Best(cut)));
    }

    [Fact]
    public void Tiles_Are_Positioned_With_Custom_Properties()
    {
        game.SetBoard(new int[,]
        {
            { 0, 0, 0, 0 },
            { 0, 0, 8, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();

        var tile = cut.Find(".tile.tile-8");
        Assert.Contains("--r:1", tile.GetAttribute("style"));
        Assert.Contains("--c:2", tile.GetAttribute("style"));
        Assert.Equal("8", tile.QuerySelector(".tile-inner")!.TextContent);
    }

    [Fact]
    public void Slide_Keeps_The_Tile_Key_And_Updates_Its_Position()
    {
        game.SetBoard(new int[,]
        {
            { 8, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();
        var id = cut.Find(".tile.tile-8").GetAttribute("data-id");

        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var after = cut.Find(".tile.tile-8");
        Assert.Contains("--c:3", after.GetAttribute("style"));
        // Same tile id = same @key, so Blazor keeps the element and the browser transitions its transform.
        Assert.Equal(id, after.GetAttribute("data-id"));
        Assert.DoesNotContain("tile-new", after.ClassName);
    }

    [Fact]
    public void Merge_Renders_Pop_Tile_Over_Retired_Halves_And_A_Spawn()
    {
        game.SetBoard(new int[,]
        {
            { 4, 4, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();

        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        var merged = cut.Find(".tile.tile-merged");
        Assert.Equal("8", merged.TextContent.Trim());
        var retired = cut.FindAll(".tile.tile-retired");
        Assert.Equal(2, retired.Count);
        Assert.All(retired, t => Assert.Contains("--c:0", t.GetAttribute("style")));
        Assert.Single(cut.FindAll(".tile.tile-new"));

        // The next move drops the retired halves and the animation flags.
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Empty(cut.FindAll(".tile.tile-retired"));
        Assert.Empty(cut.FindAll(".tile.tile-merged"));
    }

    [Fact]
    public void Rapid_Moves_Are_All_Applied()
    {
        game.SetBoard(new int[,]
        {
            { 2, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();
        var root = cut.Find(".game");
        var applied = 0;
        foreach (var key in new[] { "ArrowRight", "ArrowDown", "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown" })
        {
            var before = string.Join(",", Tiles(cut));
            root.KeyDown(new KeyboardEventArgs { Key = key });
            if (string.Join(",", Tiles(cut)) != before) applied++;
        }

        Assert.True(applied >= 4, $"Only {applied} of 6 moves changed the board.");
    }

    [Fact]
    public void Header_Has_Docs_Link_And_Theme_Toggle()
    {
        var cut = Render<GameBoard>();

        var docs = cut.Find("a.docs-btn");
        Assert.Equal("docs", docs.GetAttribute("href"));
        Assert.Equal("Open the docs", docs.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("button.theme-toggle"));
    }
}
