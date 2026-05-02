# Retro.Crt

A Pascal CRT-Unit-style console library for modern .NET. Tiny, dependency-free,
trim- and AOT-clean. ANSI styling, classic `TextColor` / `GotoXY` / `ClrScr`
verbs, truecolor with graceful 16-color and `NO_COLOR` fallback.

```csharp
using Retro.Crt;

Crt.TextColor(Color.LightCyan);
Crt.WriteLine("system online.");

using (Crt.WithStyle(Color.Yellow, bold: true))
    Crt.WriteLine("> ready.");
```

## Why

Spectre.Console is great, but it does not trim or AOT cleanly, and a launcher
that ships as a 12 MB single binary cannot afford the runtime weight. Retro.Crt
is the small, opinionated alternative for tools that want curated colored
output, a banner, a progress bar, and nothing else.

## Status

Early. API will move until `1.0`.

## License

MIT
