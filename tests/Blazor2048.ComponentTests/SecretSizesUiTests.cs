using AngleSharp.Dom;
using Blazor2048.Components;
using Bunit;
using Game2048.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazor2048.ComponentTests;

/// <summary>
/// The secret 1×1 board from Custom…: the instant win screen, the name, no moves, and what is (not)
/// remembered. 0 is rejected (spec 011, User Story 7 and its fourth follow-up).
/// </summary>
public class SecretSizesUiTests : AppTestContext
{
    private IRenderedComponent<GameBoard> RenderReady()
    {
        var cut = Render<GameBoard>();
        cut.WaitForAssertion(() => Assert.DoesNotContain("board-pending", cut.Find(".board").ClassName));
        return cut;
    }

    private static void StartCustom(IRenderedComponent<GameBoard> cut, string text)
    {
        cut.Find("#size-menu-button").Click();
        cut.Find(".size-option[data-size='custom']").Click();
        cut.Find("#custom-size").Input(text);
        cut.Find(".size-dialog form").Submit();
    }

    private static string Best(IRenderedComponent<GameBoard> cut) => cut.FindAll(".score-box .value")[1].TextContent;

    private string PageTitleText(IRenderedComponent<GameBoard> cut) =>
        Render(cut.FindComponent<PageTitle>().Instance.ChildContent!).Markup.Trim();

    private IEnumerable<string> SavedKeys() => JSInterop.Invocations
        .Where(i => i.Identifier == "localStorage.setItem")
        .Select(i => (string)i.Arguments[0]!);

    private static IElement Overlay(IRenderedComponent<GameBoard> cut) => cut.Find(".overlay.instant-win");

    [Fact]
    public void One_By_One_Is_An_Instant_256_Win()
    {
        var cut = RenderReady();
        StartCustom(cut, "1");

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Equal(1, Game.Size);
        Assert.Single(cut.FindAll(".cell"));
        var tile = Assert.Single(cut.FindAll(".tile-layer .tile"));
        Assert.Equal("256", tile.GetAttribute("data-value"));
        Assert.Equal("256", tile.TextContent.Trim());
        Assert.Contains("tile-new", tile.ClassName);

        Assert.Equal("256", cut.Find("h1.title").TextContent.Trim());
        Assert.Equal("256", PageTitleText(cut));
        Assert.Equal("Join the tile, get to 256!", cut.Find(".hint").TextContent.Trim());
        Assert.Equal("256 board, 1 by 1", cut.Find(".board").GetAttribute("aria-label"));

        var overlay = Overlay(cut);
        Assert.Equal("1", overlay.GetAttribute("data-size"));
        Assert.Contains("You made 256!", overlay.TextContent);
        Assert.Contains("One tile, zero moves. Speedrun complete.", overlay.TextContent);
        Assert.Empty(cut.FindAll(".overlay.lose")); // won, not "Game over!", though no move is possible
        Assert.DoesNotContain("Keep going", overlay.TextContent);
        Assert.Equal(["Play again", "Back to 4×4"], overlay.QuerySelectorAll("button").Select(b => b.TextContent.Trim()));
        Assert.Equal("0", cut.FindAll(".score-box .value")[0].TextContent);
    }

    [Fact]
    public void Zero_Is_Rejected_In_The_Dialog()
    {
        var cut = RenderReady();
        StartCustom(cut, "0");

        Assert.Contains("A 0×0 board has nothing to play", cut.Find("#custom-size-error").TextContent);
        Assert.Equal("true", cut.Find("#custom-size").GetAttribute("aria-invalid"));
        Assert.Equal(4, Game.Size);
        Assert.Empty(cut.FindAll(".overlay"));
    }

    [Fact]
    public void Keys_And_Swipes_Do_Nothing()
    {
        var cut = RenderReady();
        StartCustom(cut, "1");
        var markup = cut.Find(".board").OuterHtml;

        foreach (var key in new[] { "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "w", "a", "s", "d" })
            cut.Find(".game").KeyDown(new KeyboardEventArgs { Key = key });
        cut.Find(".game").TouchStart(new TouchEventArgs { Touches = [new TouchPoint { ClientX = 100, ClientY = 100 }] });
        cut.Find(".game").TouchEnd(new TouchEventArgs { ChangedTouches = [new TouchPoint { ClientX = 10, ClientY = 100 }] });

        Assert.Equal("0", cut.Find(".board").GetAttribute("data-moves"));
        Assert.Equal(markup, cut.Find(".board").OuterHtml);
        Assert.True(Game.HasWon);
    }

    [Fact]
    public void Not_Saved_As_The_Last_Size_And_No_Best()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.best.4x4").SetResult("1200");
        var cut = RenderReady();
        cut.WaitForAssertion(() => Assert.Equal("1200", Best(cut)));

        StartCustom(cut, "1");

        Assert.Equal("–", Best(cut));
        Assert.DoesNotContain("blazor2048.size", SavedKeys());
        Assert.DoesNotContain(SavedKeys(), k => k.StartsWith("blazor2048.best", StringComparison.Ordinal));
    }

    [Fact]
    public void Play_Again_Wins_Again_And_Back_Returns_To_The_Last_Real_Size()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.best.6x6").SetResult("500");
        var cut = RenderReady();
        cut.Find("#size-menu-button").Click();
        cut.Find(".size-option[data-size='6']").Click();
        StartCustom(cut, "1");
        var firstId = cut.Find(".tile-layer .tile").GetAttribute("data-id");

        cut.Find(".overlay.instant-win button").Click(); // Play again
        Assert.Equal(1, Game.Size);
        Assert.NotEqual(firstId, cut.Find(".tile-layer .tile").GetAttribute("data-id")); // pops in again
        Assert.NotNull(Overlay(cut));

        Assert.Equal("Back to 6×6", cut.Find(".back-to-real").TextContent.Trim());
        cut.Find(".back-to-real").Click();

        Assert.Equal(6, Game.Size);
        Assert.Empty(cut.FindAll(".overlay"));
        Assert.Equal("8192", cut.Find("h1.title").TextContent.Trim());
        cut.WaitForAssertion(() => Assert.Equal("500", Best(cut)));
    }

    [Fact]
    public void Main_Button_Restarts_The_1x1_Board_And_Menu_Keeps_The_Secret()
    {
        var cut = RenderReady();
        StartCustom(cut, "1");
        var firstId = cut.Find(".tile-layer .tile").GetAttribute("data-id");

        cut.Find(".split-btn .new-game").Click();
        Assert.Equal(1, Game.Size);
        Assert.NotEqual(firstId, cut.Find(".tile-layer .tile").GetAttribute("data-id"));
        Assert.Contains("New Game, 1 by 1", cut.Find(".split-btn .new-game").GetAttribute("aria-label"));
        Assert.NotNull(Overlay(cut));

        cut.Find("#size-menu-button").Click();
        var items = cut.FindAll("#size-menu [role=menuitemradio]");
        Assert.Equal(["4", "5", "6", "7", "8", "9", "10", "custom"], items.Select(i => i.GetAttribute("data-size")));
        var custom = cut.Find(".size-option[data-size='custom']");
        Assert.Equal("true", custom.GetAttribute("aria-checked"));
        Assert.Contains("1×1", custom.TextContent);
        custom.Click();
        Assert.Contains("(2 to 16)", cut.Find("label[for=custom-size]").TextContent); // never mentions 1
        Assert.Equal("1", cut.Find("#custom-size").GetAttribute("value"));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    public void A_Saved_1_Or_0_Is_Never_Restored(string saved)
    {
        JSInterop.Setup<string?>("localStorage.getItem", "blazor2048.size").SetResult(saved);
        var cut = RenderReady();
        Assert.Equal(4, Game.Size);
        Assert.Empty(cut.FindAll(".overlay"));
    }
}
