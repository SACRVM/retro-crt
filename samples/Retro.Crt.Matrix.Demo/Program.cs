using Retro.Crt;

// The opening sequence everyone remembers. Pure cinematic showcase of
// MatrixBlock cursor + Blink + GreenCrt theme + Typewriter. ~25 s.
//
// Run with `dotnet run --project samples/Retro.Crt.Matrix.Demo` in a
// terminal that ships the U+2588 full-block glyph (Cascadia Code,
// JetBrains Mono, Fira Code). Without it the chunky cursor shows as
// '#' — still readable, less iconic.

var t = Themes.GreenCrt;
const int blinkRate = 420; // half-period — slow, ominous

Crt.ResetColor();
Crt.WriteLine();

// Empty stage with a blinking cursor — sets the tone.
Crt.Write(" ");
Typewriter.Blink(1500, TypewriterCursor.MatrixBlock,
    fg: t.Foreground, blinkRateMs: blinkRate);

// Line 1 — Wake up, Neo.
Typewriter.Type(
    "wake up, neo...",
    msPerChar: 90,
    fg: t.Foreground,
    cursor: TypewriterCursor.MatrixBlock);
Typewriter.Blink(1500, TypewriterCursor.MatrixBlock,
    fg: t.Foreground, blinkRateMs: blinkRate);
Crt.WriteLine();
Crt.WriteLine();

// Line 2 — The Matrix has you.
Crt.Write(" ");
Typewriter.Type(
    "the matrix has you...",
    msPerChar: 90,
    fg: t.Foreground,
    cursor: TypewriterCursor.MatrixBlock);
Typewriter.Blink(1500, TypewriterCursor.MatrixBlock,
    fg: t.Foreground, blinkRateMs: blinkRate);
Crt.WriteLine();
Crt.WriteLine();

// Line 3 — Follow the white rabbit.
Crt.Write(" ");
Typewriter.Type(
    "follow the white rabbit.",
    msPerChar: 80,
    fg: t.Foreground,
    cursor: TypewriterCursor.MatrixBlock);
Typewriter.Blink(2000, TypewriterCursor.MatrixBlock,
    fg: t.Foreground, blinkRateMs: blinkRate);
Crt.WriteLine();
Crt.WriteLine();

// Closer — shorter pause, brighter accent. Knock, knock, Neo.
Crt.Write(" ");
using (Crt.WithStyle(fg: t.Accent, bold: true))
    Typewriter.Type(
        "knock, knock, neo.",
        msPerChar: 70,
        cursor: TypewriterCursor.MatrixBlock);
Typewriter.Blink(1200, TypewriterCursor.MatrixBlock,
    fg: t.Accent, blinkRateMs: blinkRate);
Crt.WriteLine();

Crt.WriteLine();
Crt.ResetColor();
return 0;
