namespace Retro.Crt.Internals;

/// <summary>
/// Defensive read of <see cref="Console.CursorLeft"/>. Returns 0 when
/// the host can't report the cursor position (output redirected, no
/// console attached, IOException). Tests can pin a value via
/// <see cref="OverrideForTests"/>.
/// </summary>
internal static class CursorState
{
    private static int? _testOverride;

    public static int GetLeft()
    {
        if (_testOverride is { } v) return v;
        try
        {
            var col = Console.CursorLeft;
            return col < 0 ? 0 : col;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Pin the value <see cref="GetLeft"/> returns until cleared. Pass
    /// <c>null</c> to resume real-host detection.
    /// </summary>
    internal static void OverrideForTests(int? value) => _testOverride = value;
}
