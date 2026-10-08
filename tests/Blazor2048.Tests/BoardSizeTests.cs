using Game2048.Core;

namespace Blazor2048.Tests;

/// <summary>Board size rules and custom-size validation (specs/011-board-sizes).</summary>
public class BoardSizeTests
{
    [Fact]
    public void Range_Is_2_To_Max_And_Defaults_To_4x4()
    {
        Assert.Equal(2, BoardSize.Min);
        Assert.Equal(4, BoardSize.Default);
        Assert.InRange(BoardSize.Max, 10, 23); // presets go to 10; 2^(Max+7) must fit in an int
        Assert.Equal([4, 5, 6, 7, 8, 9, 10], BoardSize.Presets);
        Assert.False(BoardSize.IsValid(1));
        Assert.False(BoardSize.IsValid(0));
        Assert.False(BoardSize.IsValid(-4));
        Assert.False(BoardSize.IsValid(BoardSize.Max + 1));
        Assert.True(BoardSize.IsValid(BoardSize.Max));
        Assert.Equal(4, new Game().Size);
    }

    [Theory]
    [InlineData(2, 512)]
    [InlineData(3, 1024)]
    [InlineData(4, 2048)]
    [InlineData(5, 4096)]
    [InlineData(6, 8192)]
    [InlineData(7, 16384)]
    [InlineData(8, 32768)]
    [InlineData(9, 65536)]
    [InlineData(10, 131072)]
    public void Target_Is_Two_To_The_N_Plus_7(int size, int target)
    {
        Assert.Equal(target, BoardSize.WinningTile(size));
        Assert.Equal(target, new Game(size).WinningTile);
    }

    [Fact]
    public void Target_At_The_Largest_Board_Does_Not_Overflow()
    {
        var target = BoardSize.WinningTile(BoardSize.Max);
        Assert.Equal(1L << (BoardSize.Max + 7), target);
        Assert.True(target > 0);
    }

    [Theory]
    [InlineData("2", 2)]
    [InlineData("4", 4)]
    [InlineData(" 12 ", 12)]
    [InlineData("007", 7)]
    [InlineData("+5", 5)]
    public void TryParse_Accepts_Whole_Numbers_In_Range(string text, int expected)
    {
        Assert.True(BoardSize.TryParse(text, out var size, out var error));
        Assert.Equal(expected, size);
        Assert.Equal("", error);
    }

    [Fact]
    public void TryParse_Accepts_The_Maximum() =>
        Assert.True(BoardSize.TryParse(BoardSize.Max.ToString(), out _, out _));

    [Theory]
    [InlineData("1", "Boards start at 2×2")]
    [InlineData("0", "Boards start at 2×2")]
    [InlineData("-3", "Boards start at 2×2")]
    [InlineData("-0", "Boards start at 2×2")]
    [InlineData("abc", "“abc” is not a number")]
    [InlineData("4x4", "is not a number")]
    [InlineData("7.5", "without decimals")]
    [InlineData("6.0", "without decimals")]
    [InlineData("6,5", "without decimals")]
    [InlineData("", "Enter a whole number from 2 to")]
    [InlineData("   ", "Enter a whole number from 2 to")]
    [InlineData(null, "Enter a whole number from 2 to")]
    [InlineData("99999999999999999999", "The largest board is")]
    public void TryParse_Rejects_With_A_Clear_Message(string? text, string message)
    {
        Assert.False(BoardSize.TryParse(text, out var size, out var error));
        Assert.Equal(0, size);
        Assert.Contains(message, error);
        Assert.Contains(BoardSize.RangeText, error); // always says what is allowed
    }

    [Fact]
    public void TryParse_Rejects_One_Above_The_Maximum()
    {
        Assert.False(BoardSize.TryParse((BoardSize.Max + 1).ToString(), out _, out var error));
        Assert.Contains($"The largest board is {BoardSize.Max}×{BoardSize.Max}", error);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Game_Rejects_Invalid_Sizes(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Game(size));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Game().NewGame(size));
    }
}
