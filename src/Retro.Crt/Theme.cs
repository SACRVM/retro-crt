namespace Retro.Crt;

/// <summary>
/// A curated palette evoking a specific era of computing: DOS, the
/// Norton Commander blue, an amber CRT, etc. Themes are pure data —
/// pick the colors you want and pass them to <see cref="Banner"/>,
/// <see cref="ProgressBar"/>, <see cref="Crt.WithStyle"/>, or anywhere
/// else a <see cref="Color"/> is accepted.
/// </summary>
/// <remarks>
/// Theme colors are truecolor. On terminals without truecolor support,
/// the standard Retro.Crt fallback applies: the closest SGR Standard16
/// slot is used, which means the user's terminal theme will tint the
/// preset. For pixel-faithful retro looks, viewers need a truecolor
/// terminal (Windows Terminal, iTerm2, modern xterm, etc.).
/// </remarks>
public readonly record struct Theme
{
    /// <summary>Human-readable theme name, e.g. <c>"AmberCrt"</c>.</summary>
    public string Name { get; init; }

    /// <summary>Suggested background fill.</summary>
    public Color Background { get; init; }

    /// <summary>Default foreground / body text.</summary>
    public Color Foreground { get; init; }

    /// <summary>Highlight / heading / banner color.</summary>
    public Color Accent { get; init; }

    /// <summary>De-emphasized text — borders, hints, secondary info.</summary>
    public Color Muted { get; init; }

    /// <summary>OK / passed / completed signals.</summary>
    public Color Success { get; init; }

    /// <summary>Caution / non-fatal issue.</summary>
    public Color Warn { get; init; }

    /// <summary>Failure / fatal error.</summary>
    public Color Error { get; init; }
}

/// <summary>
/// Built-in Retro.Crt themes. Each is a snapshot of the most
/// recognizable color combination from its era — close enough to be
/// instantly familiar without trying to be a museum-grade emulation.
/// </summary>
public static class Themes
{
    /// <summary>
    /// Classic IBM PC / DOS prompt. Black background, light-gray text,
    /// bright primaries for emphasis. The default look most people
    /// remember from early-90s text-mode software.
    /// </summary>
    public static readonly Theme Dos = new()
    {
        Name       = "Dos",
        Background = Color.Rgb(  0,   0,   0),
        Foreground = Color.Rgb(170, 170, 170),
        Accent     = Color.Rgb(255, 255, 255),
        Muted      = Color.Rgb( 85,  85,  85),
        Success    = Color.Rgb( 85, 255,  85),
        Warn       = Color.Rgb(255, 255,  85),
        Error      = Color.Rgb(255,  85,  85),
    };

    /// <summary>
    /// Amber phosphor CRT — monochrome warm orange on near-black, like
    /// a 1980s terminal. Everything is a shade of amber; "errors" just
    /// run hotter.
    /// </summary>
    public static readonly Theme AmberCrt = new()
    {
        Name       = "AmberCrt",
        Background = Color.Rgb( 20,  10,   0),
        Foreground = Color.Rgb(255, 176,   0),
        Accent     = Color.Rgb(255, 220,  80),
        Muted      = Color.Rgb(140,  90,   0),
        Success    = Color.Rgb(255, 220,  80),
        Warn       = Color.Rgb(255, 140,   0),
        Error      = Color.Rgb(255,  60,   0),
    };

    /// <summary>
    /// Green phosphor CRT — the other classic monochrome look, late-70s
    /// computer terminals and early Apple ][ monitors.
    /// </summary>
    public static readonly Theme GreenCrt = new()
    {
        Name       = "GreenCrt",
        Background = Color.Rgb(  0,  16,   0),
        Foreground = Color.Rgb( 51, 255,  51),
        Accent     = Color.Rgb(160, 255, 160),
        Muted      = Color.Rgb( 30, 130,  30),
        Success    = Color.Rgb(160, 255, 160),
        Warn       = Color.Rgb(220, 255,   0),
        Error      = Color.Rgb(255,  80,  40),
    };

    /// <summary>
    /// Commodore Amiga Workbench 1.x — the unmistakable orange-on-blue
    /// look. Bright, friendly, slightly garish.
    /// </summary>
    public static readonly Theme Amiga = new()
    {
        Name       = "Amiga",
        Background = Color.Rgb(  0,  85, 170),
        Foreground = Color.Rgb(255, 255, 255),
        Accent     = Color.Rgb(255, 136,   0),
        Muted      = Color.Rgb(170, 170, 170),
        Success    = Color.Rgb(  0, 204,  68),
        Warn       = Color.Rgb(255, 204,   0),
        Error      = Color.Rgb(255,   0,  68),
    };

    /// <summary>
    /// Commodore 64 — light-blue text on a darker C64-blue background,
    /// the boot screen everyone with a 64 remembers.
    /// </summary>
    public static readonly Theme C64 = new()
    {
        Name       = "C64",
        Background = Color.Rgb( 64,  49, 141),
        Foreground = Color.Rgb(120, 105, 196),
        Accent     = Color.Rgb(255, 255, 255),
        Muted      = Color.Rgb( 88,  76, 162),
        Success    = Color.Rgb(154, 220, 113),
        Warn       = Color.Rgb(213, 223, 124),
        Error      = Color.Rgb(213, 100, 121),
    };

    /// <summary>
    /// Norton Commander on DOS — deep blue background, white menus,
    /// yellow highlights. The look that defined orthodox file managers.
    /// </summary>
    public static readonly Theme NortonCommander = new()
    {
        Name       = "NortonCommander",
        Background = Color.Rgb(  0,   0, 170),
        Foreground = Color.Rgb(170, 170, 170),
        Accent     = Color.Rgb(255, 255,  85),
        Muted      = Color.Rgb( 85,  85, 255),
        Success    = Color.Rgb( 85, 255,  85),
        Warn       = Color.Rgb(255, 255,  85),
        Error      = Color.Rgb(255,  85,  85),
    };

    /// <summary>
    /// All built-in themes, in stable display order. Useful for demos
    /// and theme pickers.
    /// </summary>
    public static readonly Theme[] All =
    [
        Dos,
        AmberCrt,
        GreenCrt,
        Amiga,
        C64,
        NortonCommander,
    ];
}
