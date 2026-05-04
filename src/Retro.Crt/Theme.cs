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
    /// Modern dark theme — deep blue-charcoal background with soft
    /// pastel accents. Periwinkle highlights, mint success, gold warn,
    /// coral error. The "comfortable late-night IDE" look.
    /// </summary>
    public static readonly Theme Midnight = new()
    {
        Name       = "Midnight",
        Background = Color.Rgb( 20,  22,  32),
        Foreground = Color.Rgb(214, 218, 232),
        Accent     = Color.Rgb(180, 195, 255),
        Muted      = Color.Rgb( 95, 105, 130),
        Success    = Color.Rgb(155, 220, 180),
        Warn       = Color.Rgb(245, 205, 140),
        Error      = Color.Rgb(245, 145, 165),
    };

    /// <summary>
    /// Modern dark theme — neutral charcoal background with cool
    /// cyan-leaning pastels. Less blue than Midnight, more silver-grey;
    /// the calm, professional dark mode.
    /// </summary>
    public static readonly Theme Slate = new()
    {
        Name       = "Slate",
        Background = Color.Rgb( 36,  38,  44),
        Foreground = Color.Rgb(220, 222, 230),
        Accent     = Color.Rgb(140, 215, 230),
        Muted      = Color.Rgb(115, 122, 138),
        Success    = Color.Rgb(170, 220, 175),
        Warn       = Color.Rgb(235, 205, 145),
        Error      = Color.Rgb(235, 150, 160),
    };

    /// <summary>
    /// Modern dark theme — deep aubergine background with magenta and
    /// orchid pastels. Slightly warmer and more theatrical than
    /// Midnight; good for creative tooling.
    /// </summary>
    public static readonly Theme Twilight = new()
    {
        Name       = "Twilight",
        Background = Color.Rgb( 28,  22,  38),
        Foreground = Color.Rgb(225, 218, 235),
        Accent     = Color.Rgb(225, 175, 235),
        Muted      = Color.Rgb(125, 100, 140),
        Success    = Color.Rgb(180, 215, 200),
        Warn       = Color.Rgb(240, 200, 155),
        Error      = Color.Rgb(240, 145, 180),
    };

    /// <summary>
    /// All built-in themes, in stable display order — the retro era
    /// presets first, then the modern dark themes. Useful for demos
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
        Midnight,
        Slate,
        Twilight,
    ];
}
