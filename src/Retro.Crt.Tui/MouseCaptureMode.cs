namespace Retro.Crt.Tui;

/// <summary>
/// Controls whether <see cref="Application"/> enables terminal mouse
/// reporting while it runs. Set on the <see cref="Application"/> before
/// <see cref="Application.Run"/>; changes after start have no effect.
/// </summary>
/// <remarks>
/// <para>
/// The tradeoff: with mouse reporting on (<see cref="Full"/>), the
/// terminal forwards click / drag / wheel events to the app — required
/// for click-to-focus, scrollbar drag, and wheel scrolling. The cost is
/// that the terminal's own click-and-drag text selection no longer
/// works inside the alt-screen viewport, because the events are
/// captured before the terminal's selection layer sees them.
/// (<c>Shift</c>+drag still bypasses on most modern terminals, but
/// not all.)
/// </para>
/// <para>
/// Apps that only need keyboard input — or that prioritize letting
/// users copy log lines with the mouse — can set this to
/// <see cref="None"/> to keep the terminal's selection layer alive.
/// </para>
/// </remarks>
public enum MouseCaptureMode
{
    /// <summary>
    /// Enable mouse reporting (xterm mode 1003 + 1006). Click-to-focus,
    /// scrollbar drag, and wheel scrolling work. Terminal's native
    /// click-and-drag text selection is suppressed inside the
    /// alt-screen viewport on most terminals. This is the default.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Do not enable mouse reporting at all. Click-to-focus, scrollbar
    /// drag, and wheel scrolling do not work, but the terminal's native
    /// text selection works the same as in any other shell program.
    /// Use this when your app is keyboard-driven and users will want to
    /// copy content with the mouse.
    /// </summary>
    None = 1,
}
