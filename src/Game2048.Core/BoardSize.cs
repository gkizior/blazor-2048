using System.Globalization;

namespace Game2048.Core;

/// <summary>
/// Board sizes the game supports. Boards are always square (N×N). See specs/011-board-sizes.
/// </summary>
public static class BoardSize
{
    /// <summary>Smallest board: 2×2 (1×1 cannot move at all).</summary>
    public const int Min = 2;

    /// <summary>Largest board. Chosen from measurements; see specs/011-board-sizes/plan.md.</summary>
    public const int Max = 16;

    /// <summary>The classic board, used when nothing is saved.</summary>
    public const int Default = 4;

    /// <summary>Sizes offered directly in the New Game menu.</summary>
    public static IReadOnlyList<int> Presets { get; } = [4, 5, 6, 7, 8, 9, 10];

    /// <summary>A playable size, <see cref="Min"/> to <see cref="Max"/> (what gets saved and offered).</summary>
    public static bool IsValid(int size) => size is >= Min and <= Max;

    /// <summary>
    /// The secret sizes, 0 and 1: Custom… accepts them and they win right away (a 1×1 board holds one
    /// tile that is already its target; a 0×0 board has nothing to play). They are never saved as the
    /// last size, never get a best score and are not in the menu. See spec 011, User Story 7.
    /// </summary>
    public static bool IsSecret(int size) => size is 0 or 1;

    /// <summary>Any size a <see cref="Game"/> can start: the playable range plus the secret sizes.</summary>
    public static bool IsSupported(int size) => IsValid(size) || IsSecret(size);

    /// <summary>The range as shown to players, for example "2 to 16".</summary>
    public static string RangeText => $"{Min} to {Max}";

    /// <summary>
    /// Parses a custom size typed by the player. Accepts a whole number from <see cref="Min"/> to
    /// <see cref="Max"/> (surrounding spaces allowed), and the secret sizes 0 and 1 (which the messages
    /// never mention). Otherwise returns false and a message that says what is wrong and what is allowed.
    /// </summary>
    public static bool TryParse(string? text, out int size, out string error)
    {
        size = 0;
        var s = text?.Trim() ?? "";
        if (s.Length == 0)
        {
            error = $"Enter a whole number from {RangeText}.";
            return false;
        }

        var digits = s.StartsWith('-') || s.StartsWith('+') ? s[1..] : s;
        if (digits.Length > 0 && digits.All(char.IsAsciiDigit))
        {
            var negative = s[0] == '-';
            // Very long digit strings are simply "too big"; no overflow.
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var n)) n = int.MaxValue;
            if (negative)
            {
                error = $"Boards can't have a negative size. Enter a whole number from {RangeText}.";
                return false;
            }
            if (n > Max)
            {
                error = $"The largest board is {Max}×{Max}. Enter a whole number from {RangeText}.";
                return false;
            }
            size = n;
            error = "";
            return true;
        }

        var parts = digits.Split('.', ',');
        if (parts.Length == 2 && parts.All(p => p.All(char.IsAsciiDigit)) && parts.Any(p => p.Length > 0))
        {
            error = $"Use a whole number, without decimals: {RangeText}.";
            return false;
        }

        error = $"“{(s.Length > 12 ? s[..12] + "…" : s)}” is not a number. Enter a whole number from {RangeText}.";
        return false;
    }

    /// <summary>
    /// The tile that wins, and the game's "name" at this size: 2^(N+7). 4×4 → 2048, 5×5 → 4096 …
    /// 10×10 → 131072; 2×2 → 512, 3×3 → 1024; the secret 1×1 → 256 and 0×0 → 128. Fits in an int up
    /// to N = 23 (Max is well below).
    /// </summary>
    public static int WinningTile(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 23);
        return 1 << (size + 7);
    }
}
