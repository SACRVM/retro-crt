# Retro.Crt

A Pascal CRT-Unit-style console library for modern .NET. Tiny, dependency-
free, trim- and AOT-clean. ANSI styling, classic `TextColor` / `GotoXY` /
`ClrScr` verbs, truecolor with graceful 16-color and `NO_COLOR` fallback,
plus a small set of Pascal-flavoured output building blocks: framed
banners, in-place progress bars, semantic logging, and a typewriter that
fades characters in.

```csharp
using Retro.Crt;

Crt.TextColor(Color.LightCyan);
Crt.WriteLine("system online.");

using (Crt.WithStyle(Color.Yellow, bold: true))
    Crt.WriteLine("> ready.");
```

## Install

```bash
dotnet add package Retro.Crt
```

Targets `net10.0`. No third-party dependencies.

## Why

Spectre.Console is great, but it does not trim or AOT cleanly, and a
launcher that ships as a 12 MB single binary cannot afford the runtime
weight. Retro.Crt is the small, opinionated alternative for tools that
want curated colored output, a banner, a progress bar, and nothing else.

## How to use

### Colors and styling

The 16 classic DOS palette names (`LightCyan`, `Brown`, …) map onto the
user's terminal theme via SGR codes — so themed terminals (Solarized,
Dracula, …) keep their identity. Use `Color.Rgb(r, g, b)` for truecolor.

```csharp
Crt.TextColor(Color.LightGreen);
Crt.Write("ok");
Crt.ResetColor();

using (Crt.WithStyle(fg: Color.Rgb(255, 140, 0), bold: true))
    Crt.WriteLine("warning, in orange");
```

`Crt.ColorEnabled` reflects whether escapes will actually reach the
terminal (false when output is redirected, `NO_COLOR` is set, or VT
enablement failed on Windows). `FORCE_COLOR=1` overrides redirection
detection.

### Pascal CRT verbs

```csharp
Crt.ClrScr();
Crt.GotoXY(10, 5);     // 1-based, like the original CRT unit
Crt.Write("hi");
Crt.ClrEol();
```

### Banner

```csharp
Banner.Box("Retro.Crt 0.2", fg: Color.LightCyan);

Banner.Box(
    ["Retro.Crt 0.2", "Banner / Bar / Log / Typewriter"],
    fg: Color.LightCyan);

Banner.Gradient(
    asciiArtLines,
    from: Color.Rgb(80, 220, 255),
    to:   Color.Rgb(255, 120, 175));
```

`Box` uses unicode box-drawing glyphs when the terminal can render them,
and falls back to `+--+` on legacy ASCII code pages. `Gradient`
interpolates per line; both endpoints must be truecolor or it falls back
to `from`.

### ProgressBar

```csharp
using var bar = ProgressBar.Start(
    total: 4_500_000,
    width: 30,
    label: " download",
    color: Color.LightCyan);

for (var i = 0; i <= 100; i++)
{
    bar.Set(i * 45_000);
    Thread.Sleep(40);
}
```

The bar redraws in place on every `Set` / `Tick`, hides the terminal
cursor for its lifetime, and prints a single final frame on `Dispose`.
When ANSI is unavailable (output redirected, `NO_COLOR`, dumb terminal)
intermediate updates are suppressed and only the final frame is written —
so log files do not end up with sixty progress lines in a row.

### Log

```csharp
Log.Debug("loading config from /etc/retro");
Log.Info("system online");
Log.Success("checksum verified");
Log.Warn("disk usage at 84%");
Log.Error("failed to bind port 8080");
```

Format: `HH:MM:SS  LEVEL  message` with a fixed five-char level tag so
columns line up. `Warn` and `Error` go to `stderr`; everything else to
`stdout`.

### Typewriter

Reveals text one character at a time. Optional fake cursor between
characters and optional alpha fade-in (the final glyph appears in its
target color but ramps from dim to full brightness). The terminal's
native cursor is hidden for the whole reveal.

```csharp
Typewriter.TypeLine(
    "system online.",
    msPerChar: 25,
    fg: Color.LightCyan);

Typewriter.TypeLine(
    "with a fake cursor...",
    msPerChar: 30,
    fg: Color.LightGreen,
    cursor: TypewriterCursor.Block);

Typewriter.TypeLine(
    "alpha fade-in (truecolor)...",
    msPerChar: 50,
    fg: Color.Rgb(255, 120, 200),
    fade: TypewriterFade.Alpha);

Typewriter.TypeLine(
    "gradient + cursor + alpha fade",
    msPerChar: 40,
    cursor: TypewriterCursor.Block,
    fade: TypewriterFade.Alpha,
    gradient: (Color.Rgb(80, 220, 255), Color.Rgb(255, 120, 175)));
```

`Alpha` fade requires truecolor — on Standard16 it silently degrades to
no fade (Standard16 has no brightness scaling). When ANSI is off both
cursor and fade are skipped and the string is dumped at full speed —
still typed, but without animation residue in logs.

## Building from source

```bash
git clone https://github.com/chloe-dream/retro-crt
cd retro-crt
dotnet build
dotnet test
dotnet run --project samples/Retro.Crt.Demo
```

## NuGet package

The library project is already packable. Build a `.nupkg` locally with:

```bash
dotnet pack src/Retro.Crt/Retro.Crt.csproj -c Release
```

Output lands in `src/Retro.Crt/bin/Release/Retro.Crt.<version>.nupkg`
(plus a `.snupkg` symbol package — `IncludeSymbols` is on).

To publish to nuget.org, set up an API key and:

```bash
dotnet nuget push src/Retro.Crt/bin/Release/Retro.Crt.0.2.0.nupkg \
    --api-key $NUGET_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

The package is not published yet — version 0.2.0 is the first one worth
shipping (Stage 1 was barely a library).

## Roadmap

See [ROADMAP.md](ROADMAP.md). Stage 3 sketches a cell-grid screen buffer
with diff renderer for Turbo-Vision-style shadows, save/restore-rect, and
flicker-free repaint.

## Status

Pre-1.0. Public API may move between minor versions until 1.0.

## License

MIT
