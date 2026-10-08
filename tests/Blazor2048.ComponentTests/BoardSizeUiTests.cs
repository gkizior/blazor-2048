using AngleSharp.Dom;
using Blazor2048.Components;
using Bunit;
using Game2048.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazor2048.ComponentTests;

/// <summary>
/// The New Game split button, size menu, Custom… dialog and what changes with the size: cells,
/// best score per size, the saved size and the name easter egg (specs/011-board-sizes).
/// </summary>
public class BoardSizeUiTests : AppTestContext
{
    private IRenderedComponent<GameBoard> RenderReady()
    {
        var cut = Render<GameBoard>();
        cut.WaitForAssertion(() => Assert.DoesNotContain("board-pending", cut.Find(".board").ClassName));
        return cut;
    }

    private static IElement Caret(IRenderedComponent<GameBoard> cut) => cut.Find("#size-menu-button");

    private static void OpenMenu(IRenderedComponent<GameBoard> cut) => Caret(cut).Click();

    private static void Pick(IRenderedComponent<GameBoard> cut, string size)
    {
        OpenMenu(cut);
        cut.Find($".size-option[data-size='{size}']").Click();
    }

    private static string Best(IRenderedComponent<GameBoard> cut) => cut.FindAll(".score-box .value")[1].TextContent;

    private IEnumerable<string> Saved(string key) => JSInterop.Invocations
        .Where(i => i.Identifier == "localStorage.setItem" && (string?)i.Arguments[0] == key)
        .Select(i => (string)i.Arguments[1]!);

    /// <summary>The element that the last FocusAsync call targeted (bUnit renders @ref ids as an attribute).</summary>
    private string? LastFocusedRefId() => JSInterop.Invocations
        .Where(i => i.Identifier.EndsWith("focus", StringComparison.Ordinal))
        .Select(i => ((ElementReference)i.Arguments[0]!).Id).LastOrDefault();

    // bUnit only prints an element's @ref id in the render that first captured it, so tests read the
    // ids right after the element appears and compare later focus calls against them.
    private static string? RefId(IElement e) => e.GetAttribute("blazor:elementreference");

    [Fact]
    public void Split_Button_Has_A_Menu_Button_With_Aria_State()
    {
        var cut = RenderReady();

        var main = cut.Find(".split-btn .new-game");
        Assert.Equal("New Game, 4 by 4", main.GetAttribute("aria-label"));
        Assert.Contains("4×4", main.TextContent);
        var caret = Caret(cut);
        Assert.Equal("menu", caret.GetAttribute("aria-haspopup"));
        Assert.Equal("false", caret.GetAttribute("aria-expanded"));
        Assert.Equal("size-menu", caret.GetAttribute("aria-controls"));
        Assert.Empty(cut.FindAll("[role=menu]"));

        OpenMenu(cut);

        Assert.Equal("true", Caret(cut).GetAttribute("aria-expanded"));
        var items = cut.FindAll("#size-menu [role=menuitemradio]");
        Assert.Equal(["4", "5", "6", "7", "8", "9", "10", "custom"], items.Select(i => i.GetAttribute("data-size")));
        Assert.Equal(["4×4classic", "5×5", "6×6", "7×7", "8×8", "9×9", "10×10", "Custom…"],
            items.Select(i => i.TextContent.Replace(" ", "").Trim()));
        Assert.Equal("true", items[0].GetAttribute("aria-checked"));
        Assert.All(items.Skip(1), i => Assert.Equal("false", i.GetAttribute("aria-checked")));
        Assert.All(items, i => Assert.Equal("-1", i.GetAttribute("tabindex")));
        // Opening focuses the checked item.
        cut.WaitForAssertion(() => Assert.Equal(RefId(cut.FindAll("[role=menuitemradio]")[0]), LastFocusedRefId()));
    }

    [Fact]
    public void Choosing_A_Size_Starts_That_Board_And_Saves_It()
    {
        var cut = RenderReady();

        Pick(cut, "8");

        Assert.Equal(8, Game.Size);
        Assert.Equal(64, cut.FindAll(".cell").Count);
        Assert.Equal(2, cut.FindAll(".tile").Count);
        Assert.Equal("8", cut.Find(".board").GetAttribute("data-size"));
        Assert.Empty(cut.FindAll("[role=menu]"));
        Assert.Equal("false", Caret(cut).GetAttribute("aria-expanded"));
        Assert.Contains("8", Saved("blazor2048.size"));
        Assert.Equal("New Game, 8 by 8", cut.Find(".split-btn .new-game").GetAttribute("aria-label"));
        Assert.Contains("--n:8", cut.Find(".game").GetAttribute("style"));
    }

    [Fact]
    public void Main_Button_Keeps_The_Current_Size()
    {
        var cut = RenderReady();
        Pick(cut, "6");
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        cut.Find(".split-btn .new-game").Click();

        Assert.Equal(6, Game.Size);
        Assert.Equal(36, cut.FindAll(".cell").Count);
        Assert.Equal(2, cut.FindAll(".tile").Count);
        Assert.Equal(0, Game.Score);
    }

    [Fact]
    public void Open_Menu_Pauses_Board_Moves()
    {
        Game.SetBoard(new int[,] { { 0, 0, 0, 2 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } });
        var cut = RenderReady();
        OpenMenu(cut);

        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.Equal("0", cut.Find(".board").GetAttribute("data-moves"));
        Assert.Equal(2, Game[0, 3]);
    }

    [Fact]
    public void Menu_Keyboard_Moves_Focus_And_Wraps()
    {
        var cut = RenderReady();
        OpenMenu(cut);
        var ids = cut.FindAll("[role=menuitemradio]").Select(RefId).ToArray();
        Assert.All(ids, id => Assert.False(string.IsNullOrEmpty(id)));
        string? Item(int i) => ids[i];
        void Key(string key) => cut.Find("#size-menu").KeyDown(new KeyboardEventArgs { Key = key });

        Key("ArrowDown");
        cut.WaitForAssertion(() => Assert.Equal(Item(1), LastFocusedRefId()));
        Key("End");
        cut.WaitForAssertion(() => Assert.Equal(Item(7), LastFocusedRefId()));
        Key("ArrowDown"); // wraps to the first item
        cut.WaitForAssertion(() => Assert.Equal(Item(0), LastFocusedRefId()));
        Key("ArrowUp"); // wraps to the last item
        cut.WaitForAssertion(() => Assert.Equal(Item(7), LastFocusedRefId()));
        Key("Home");
        cut.WaitForAssertion(() => Assert.Equal(Item(0), LastFocusedRefId()));
    }

    [Fact]
    public void Caret_Arrow_Keys_Open_The_Menu()
    {
        var cut = RenderReady();
        Caret(cut).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal("true", Caret(cut).GetAttribute("aria-expanded"));
        cut.WaitForAssertion(() => Assert.Equal(RefId(cut.FindAll("[role=menuitemradio]")[7]), LastFocusedRefId()));
    }

    [Fact]
    public void Escape_Closes_The_Menu_And_Returns_Focus_To_The_Caret()
    {
        var cut = RenderReady();
        var caretId = RefId(Caret(cut));
        OpenMenu(cut);

        cut.Find("#size-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll("[role=menu]"));
        cut.WaitForAssertion(() => Assert.Equal(caretId, LastFocusedRefId()));
        Assert.Equal(4, Game.Size);
    }

    [Fact]
    public void Clicking_Outside_Or_Tab_Closes_The_Menu()
    {
        var cut = RenderReady();
        OpenMenu(cut);
        cut.Find(".menu-backdrop").Click();
        Assert.Empty(cut.FindAll("[role=menu]"));

        OpenMenu(cut);
        cut.Find("#size-menu").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.Empty(cut.FindAll("[role=menu]"));
        Assert.Equal(4, Game.Size);
    }

    [Theory]
    [InlineData("1", "Boards start at 2×2")]
    [InlineData("0", "Boards start at 2×2")]
    [InlineData("-5", "Boards start at 2×2")]
    [InlineData("abc", "is not a number")]
    [InlineData("7.5", "without decimals")]
    [InlineData("", "Enter a whole number")]
    [InlineData("999", "The largest board is")]
    public void Custom_Dialog_Rejects_Invalid_Sizes_Inline(string text, string message)
    {
        var cut = RenderReady();
        Pick(cut, "custom");
        Assert.NotNull(cut.Find("[role=dialog][aria-modal=true]"));
        cut.WaitForAssertion(() => Assert.Equal(RefId(cut.Find("#custom-size")), LastFocusedRefId()));

        cut.Find("#custom-size").Input(text);
        cut.Find(".size-dialog form").Submit();

        var error = cut.Find("#custom-size-error");
        Assert.Equal("alert", error.GetAttribute("role"));
        Assert.Contains(message, error.TextContent);
        Assert.Equal("true", cut.Find("#custom-size").GetAttribute("aria-invalid"));
        Assert.Equal("custom-size-error", cut.Find("#custom-size").GetAttribute("aria-describedby"));
        Assert.Equal(4, Game.Size); // nothing started
        Assert.Empty(Saved("blazor2048.size"));
    }

    [Fact]
    public void Custom_Dialog_Starts_A_Valid_Square_Board()
    {
        var cut = RenderReady();
        Pick(cut, "custom");
        cut.Find("#custom-size").Input("12");
        Assert.Contains("× 12", cut.Find(".custom-preview").TextContent);

        cut.Find(".size-dialog form").Submit();

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(12, Game.Size);
        Assert.Equal(144, cut.FindAll(".cell").Count);
        Assert.Contains("12", Saved("blazor2048.size"));

        // Reopened, Custom… is the checked item and shows the size; the dialog starts with it.
        OpenMenu(cut);
        var custom = cut.Find(".size-option[data-size='custom']");
        Assert.Equal("true", custom.GetAttribute("aria-checked"));
        Assert.Contains("12×12", custom.TextContent);
        custom.Click();
        Assert.Equal("12", cut.Find("#custom-size").GetAttribute("value"));
    }

    [Fact]
    public void Custom_Dialog_Cancel_And_Escape_Change_Nothing()
    {
        var cut = RenderReady();
        var caretId = RefId(Caret(cut));
        Pick(cut, "custom");
        cut.Find("#custom-size").Input("9");
        cut.Find(".size-dialog .btn-secondary").Click();
        Assert.Empty(cut.FindAll("[role=dialog]"));
        cut.WaitForAssertion(() => Assert.Equal(caretId, LastFocusedRefId()));

        Pick(cut, "custom");
        cut.Find(".size-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(4, Game.Size);
    }

    [Fact]
    public void Typing_In_The_Dialog_Does_Not_Move_The_Board()
    {
        Game.SetBoard(new int[,] { { 0, 0, 0, 2 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } });
        var cut = RenderReady();
        Pick(cut, "custom");

        foreach (var key in new[] { "a", "s", "w", "d", "ArrowLeft" })
            cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal("0", cut.Find(".board").GetAttribute("data-moves"));
    }

    [Fact]
    public void Saved_Size_Is_Restored_On_Load()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.size").SetResult("6");
        var cut = RenderReady();

        Assert.Equal(6, Game.Size);
        Assert.Equal(36, cut.FindAll(".cell").Count);
        Assert.Equal("8192", cut.Find("h1.title").TextContent.Trim());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("999")]
    [InlineData("junk")]
    public void Invalid_Saved_Size_Falls_Back_To_4x4(string saved)
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.size").SetResult(saved);
        var cut = RenderReady();
        Assert.Equal(4, Game.Size);
        Assert.Equal(16, cut.FindAll(".cell").Count);
    }

    [Fact]
    public void Best_Score_Is_Per_Size()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.best.4x4").SetResult("1200");
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.best.6x6").SetResult("500");
        var cut = RenderReady();
        cut.WaitForAssertion(() => Assert.Equal("1200", Best(cut)));

        Pick(cut, "6");
        cut.WaitForAssertion(() => Assert.Equal("500", Best(cut)));

        Pick(cut, "9");
        cut.WaitForAssertion(() => Assert.Equal("0", Best(cut)));

        // A new best on 9x9 is saved under the 9x9 key.
        Game.SetBoard(new int[9, 9].Also(b => { b[0, 0] = 2; b[0, 1] = 2; }));
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Contains("4", Saved("blazor2048.best.9x9"));
    }

    [Fact]
    public void Legacy_4x4_Best_Carries_Over()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.best").SetResult("4096");
        var cut = RenderReady();

        cut.WaitForAssertion(() => Assert.Equal("4096", Best(cut)));
        Assert.Contains("4096", Saved("blazor2048.best.4x4"));
    }

    [Theory]
    [InlineData("4", "2048")]
    [InlineData("5", "4096")]
    [InlineData("6", "8192")]
    [InlineData("7", "16384")]
    [InlineData("8", "32768")]
    [InlineData("9", "65536")]
    [InlineData("10", "131072")]
    public void The_Name_Follows_The_Size(string size, string name)
    {
        var cut = RenderReady();
        if (size != "4") Pick(cut, size);

        Assert.Equal(name, cut.Find("h1.title").TextContent.Trim());
        Assert.Equal($"--digits:{name.Length}", cut.Find("h1.title").GetAttribute("style"));
        Assert.Contains($"get to {name}!", cut.Find(".hint").TextContent);
        Assert.Equal($"{name} board, {size} by {size}", cut.Find(".board").GetAttribute("aria-label"));
    }

    [Fact]
    public void Page_Title_Follows_The_Size()
    {
        var cut = RenderReady();
        var title = cut.FindComponent<PageTitle>();
        Assert.Equal("2048", RenderedText(title.Instance.ChildContent!));

        Pick(cut, "7");

        Assert.Equal("16384", RenderedText(cut.FindComponent<PageTitle>().Instance.ChildContent!));
    }

    private string RenderedText(RenderFragment fragment) => Render(fragment).Markup.Trim();

    [Fact]
    public void Title_Flips_Only_When_The_Size_Changes()
    {
        var cut = RenderReady();
        Assert.Empty(cut.FindAll(".title-flip")); // no animation on load

        Pick(cut, "5");
        Assert.NotNull(cut.Find(".title-num.title-flip"));
    }

    [Fact]
    public void Win_Message_Names_The_Target_For_The_Size()
    {
        var cut = RenderReady();
        Pick(cut, "5");

        Game.SetBoard(new int[5, 5].Also(b => { b[0, 0] = 1024; b[0, 1] = 1024; }));
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Empty(cut.FindAll(".overlay.win")); // 2048 does not win on 5x5

        Game.SetBoard(new int[5, 5].Also(b => { b[2, 0] = 2048; b[2, 1] = 2048; }));
        cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Contains("You made 4096!", cut.Find(".overlay.win").TextContent);
    }

    [Fact]
    public void Moves_Do_Not_Re_Render_The_Button_Or_The_Cells()
    {
        var cut = RenderReady();
        Pick(cut, "10");
        var button = cut.FindComponent<NewGameButton>();
        var cells = cut.FindComponent<BoardCells>();
        var (buttonRenders, cellRenders, boardRenders) = (button.RenderCount, cells.RenderCount, cut.RenderCount);

        foreach (var key in new[] { "ArrowLeft", "ArrowDown", "ArrowRight", "ArrowUp", "ArrowLeft", "ArrowDown" })
            cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.True(cut.RenderCount > boardRenders);
        Assert.Equal(buttonRenders, button.RenderCount);
        Assert.Equal(cellRenders, cells.RenderCount);

        // A size change does re-render both.
        Pick(cut, "5");
        Assert.True(button.RenderCount > buttonRenders);
        Assert.True(cells.RenderCount > cellRenders);
        Assert.Equal(25, cut.FindAll(".cell").Count);
    }

    [Theory]
    [InlineData(131072, 10, "128K")]
    [InlineData(16384, 8, "16K")]
    [InlineData(8192, 10, "8192")]
    [InlineData(1048576, 9, "1M")]
    [InlineData(131072, 7, "131072")]
    [InlineData(2, 16, "2")]
    [InlineData(512, 16, "512")]
    [InlineData(1024, 12, "1K")]
    [InlineData(1024, 11, "1024")]
    [InlineData(8192, 16, "8K")]
    [InlineData(131072, 16, "128K")]
    public void Tile_Labels_Are_Compact_On_Small_Cells(int value, int size, string label) =>
        Assert.Equal(label, GameBoard.TileLabel(value, size));

    [Fact]
    public void Compact_Tiles_Keep_The_Full_Value()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.size").SetResult("10");
        Game.NewGame(10);
        Game.SetBoard(new int[10, 10].Also(b => b[3, 4] = 131072));
        var cut = RenderReady();

        var tile = cut.Find(".tile[data-value='131072']");
        Assert.Equal("128K", tile.TextContent.Trim());
        Assert.Equal("131072", tile.GetAttribute("title"));
        Assert.Contains("tl-4", tile.ClassName);
        Assert.Contains("tile-super", tile.ClassName);
    }
}

internal static class TestArrayExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
