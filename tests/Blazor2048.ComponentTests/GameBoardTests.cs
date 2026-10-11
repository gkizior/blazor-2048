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

    // A tile's position is an inline transform in multiples of --step (one cell plus a gap).
    private static string Transform(int row, int col) => $"transform:translate(calc(var(--step)*{col}),calc(var(--step)*{row}))";
    private static string Col(int col) => $"translate(calc(var(--step)*{col}),";
    private static string Row(int row) => $",calc(var(--step)*{row}))";

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
        Assert.Contains("You made 2048!", cut.Find(".overlay.win").TextContent);

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
        Assert.Equal(Transform(1, 2), tile.GetAttribute("style"));
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
        Assert.Contains(Col(3), after.GetAttribute("style"));
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
        Assert.All(retired, t => Assert.Contains(Col(0), t.GetAttribute("style")));
        Assert.Single(cut.FindAll(".tile.tile-new"));

        // The next move drops the retired halves. The merged tile keeps its class (same element,
        // same class), so its pop animation is never restarted or cut short by the move.
        var mergedId = merged.GetAttribute("data-id");
        var mergedClass = merged.ClassName;
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Empty(cut.FindAll(".tile.tile-retired"));
        var mergedAfter = cut.Find($".tile[data-id='{mergedId}']");
        Assert.Equal(mergedClass, mergedAfter.ClassName);
        Assert.EndsWith(Row(3), mergedAfter.GetAttribute("style"));
    }

    [Fact]
    public void Merge_Sources_Keep_Their_Elements_And_Slide_Into_The_Target()
    {
        game.SetBoard(new int[,]
        {
            { 0, 2, 0, 2 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();
        var sourceIds = cut.FindAll(".tile.tile-2").Select(t => t.GetAttribute("data-id")).Order().ToArray();

        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        // Both halves are still rendered under their original keys (so the browser slides the same
        // elements), now targeting the merge cell, beneath the new tile.
        var retired = cut.FindAll(".tile.tile-retired");
        Assert.Equal(sourceIds, retired.Select(t => t.GetAttribute("data-id")).Order().ToArray());
        Assert.All(retired, t => Assert.Equal(Transform(0, 0), t.GetAttribute("style")));
        var merged = cut.Find(".tile.tile-merged");
        Assert.Equal(Transform(0, 0), merged.GetAttribute("style"));
        Assert.DoesNotContain(merged.GetAttribute("data-id"), sourceIds);
    }

    [Fact]
    public void Animation_Classes_Are_Fixed_For_A_Tiles_Life()
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

        root.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        var spawned = cut.Find(".tile.tile-new");
        var (id, css) = (spawned.GetAttribute("data-id"), spawned.ClassName);

        // Whatever the next moves do, the spawned tile's class (and so its CSS animation) is unchanged.
        foreach (var key in new[] { "ArrowDown", "ArrowLeft", "ArrowUp" })
        {
            root.KeyDown(new KeyboardEventArgs { Key = key });
            if (cut.FindAll($".tile[data-id='{id}']:not(.tile-retired)") is [var tile])
                Assert.Equal(css, tile.ClassName);
        }
    }

    [Fact]
    public void Only_Moves_That_Change_The_Board_Render()
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
        var renders = cut.RenderCount;

        root.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" }); // already at the left edge: no-op
        root.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });   // no-op
        root.KeyDown(new KeyboardEventArgs { Key = "x" });         // not a game key
        Assert.Equal(renders, cut.RenderCount);

        root.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(renders + 1, cut.RenderCount);
    }

    [Fact]
    public void A_Scoring_Move_Renders_Once_And_Never_Rerenders_The_Static_Cells()
    {
        game.SetBoard(new int[,]
        {
            { 2, 2, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
        });
        var cut = Render<GameBoard>();
        var renders = cut.RenderCount;

        // Scoring also saves the best score (an awaited JS interop call); that must not cost a second render.
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.Equal("4", Score(cut));
        Assert.Equal(renders + 1, cut.RenderCount);
        Assert.Equal(1, cut.FindComponent<BoardCells>().RenderCount);
        Assert.Equal(16, cut.FindAll(".board .cell").Count);
    }

    // Same seed as the context's Game: both consume the random tiles in the same order, so the twin
    // predicts every spawn and the board can be compared cell for cell after each key.
    private static Game Twin(int[,] board)
    {
        var twin = new Game(4, new Random(7));
        twin.SetBoard(board);
        return twin;
    }

    private static readonly int[,] OneTile =
    {
        { 2, 0, 0, 0 },
        { 0, 0, 0, 0 },
        { 0, 0, 0, 0 },
        { 0, 0, 0, 0 },
    };

    private static Direction Dir(string key) => key switch
    {
        "ArrowUp" => Direction.Up, "ArrowDown" => Direction.Down, "ArrowLeft" => Direction.Left, _ => Direction.Right,
    };

    private static int[] Flat(Game g) => Enumerable.Range(0, 16).Select(i => g[i / 4, i % 4]).ToArray();

    // Starts with ArrowLeft on a board whose only tile is already top-left: a key that cannot move anything.
    private static readonly string[] Burst =
        ["ArrowLeft", "ArrowRight", "ArrowDown", "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft", "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowDown", "ArrowUp"];

    [Fact]
    public void Rapid_Moves_Are_All_Applied()
    {
        // Every key that can move the board is applied, in order, and counted; one that can't is ignored.
        game.SetBoard(OneTile);
        var twin = Twin(OneTile);
        var cut = Render<GameBoard>();
        var root = cut.Find(".game");
        var expected = 0;
        foreach (var key in Burst)
        {
            if (twin.Move(Dir(key))) expected++;
            root.KeyDown(new KeyboardEventArgs { Key = key });
            Assert.Equal(Flat(twin), Tiles(cut));
            Assert.Equal(expected.ToString(), cut.Find(".board").GetAttribute("data-moves"));
        }

        Assert.True(expected >= 8, $"Only {expected} of {Burst.Length} keys could move the board.");
        Assert.True(expected < Burst.Length); // the opening ArrowLeft had nothing to move: ignored, not counted
    }

    [Fact]
    public void Keys_Are_Applied_In_Order_While_Best_Score_Saves_Are_Still_Pending()
    {
        // Spec 004, T015: a move that beats the best score awaits the localStorage save. Keys that
        // arrive while that save (or several) is still in flight must not be lost or reordered.
        JSInterop.SetupVoid("localStorage.setItem", inv => (string?)inv.Arguments[0] == "blazor2048.best.4x4"); // never completes
        game.SetBoard(OneTile);
        var twin = Twin(OneTile);
        var cut = Render<GameBoard>();
        var root = cut.Find(".game");
        var expected = 0;
        foreach (var key in Burst)
        {
            if (twin.Move(Dir(key))) expected++;
            root.KeyDown(new KeyboardEventArgs { Key = key });
            Assert.Equal(Flat(twin), Tiles(cut));
            Assert.Equal(expected.ToString(), cut.Find(".board").GetAttribute("data-moves"));
            Assert.Equal(twin.Score.ToString(), Score(cut));
        }

        var saves = JSInterop.Invocations.Count(i => i.Identifier == "localStorage.setItem" && (string?)i.Arguments[0] == "blazor2048.best.4x4");
        Assert.True(saves >= 2, $"Only {saves} best-score saves were started, so none overlapped.");
        Assert.True(expected >= 8);
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
