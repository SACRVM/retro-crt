using Retro.Crt;
using Retro.Crt.Input;

// Live event probe: enter raw mode, enable mouse tracking, and dump
// every decoded InputEvent to the screen until 'q' is pressed. Try
// arrows, Ctrl-letters, F-keys, mouse clicks, drags, and the wheel.

Crt.WriteLine(" Retro.Crt — input probe");
Crt.WriteLine(" Press any key, click, drag — 'q' quits.");
Crt.WriteLine();

using var raw   = RawMode.Enter();
using var mouse = Crt.UseMouse();

while (true)
{
    var ev = TerminalInput.ReadEvent();
    if (ev.Kind == InputEventKind.Key
        && ev.Key.Key == Key.Glyph
        && (ev.Key.Glyph == 'q' || ev.Key.Glyph == 'Q'))
    {
        break;
    }

    Crt.WriteLine(" " + Format(ev));
}

Crt.WriteLine();
Crt.WriteLine(" bye.");
return 0;

static string Format(InputEvent ev) => ev.Kind switch
{
    InputEventKind.Key   => FormatKey(ev.Key),
    InputEventKind.Mouse => FormatMouse(ev.Mouse),
    _                    => "(none)",
};

static string FormatKey(KeyEvent k)
{
    var mods = FormatMods(k.Modifiers);
    var name = k.Key == Key.Glyph ? $"'{k.Glyph}'" : k.Key.ToString();
    return $"key   {mods}{name}";
}

static string FormatMouse(MouseEvent m)
{
    var mods = FormatMods(m.Modifiers);
    return $"mouse {mods}{m.Kind} {m.Button} @ ({m.X},{m.Y})";
}

static string FormatMods(KeyModifiers m)
{
    if (m == KeyModifiers.None) return "";
    var parts = new List<string>(4);
    if ((m & KeyModifiers.Ctrl)  != 0) parts.Add("Ctrl");
    if ((m & KeyModifiers.Alt)   != 0) parts.Add("Alt");
    if ((m & KeyModifiers.Shift) != 0) parts.Add("Shift");
    if ((m & KeyModifiers.Meta)  != 0) parts.Add("Meta");
    return string.Join("+", parts) + " ";
}
