using Retro.Crt;

// A self-hosted-app status panel — the canonical "what's running / where"
// first screen. Shows the table features added for the Fishbowl migration:
//   • Rule.Print          — a titled section header
//   • Table cellColors    — a per-row colored status column
//   • Crt.Link            — clickable endpoint URLs inside table cells
// EnableUtf8 first so the box-drawing glyphs render on a fresh Windows
// console instead of falling back to ASCII.

Crt.EnableUtf8();
Crt.ResetColor();
Crt.WriteLine();

Rule.Print("THE FISHBOWL", Color.LightCyan);
Crt.WriteLine();

// Each service: name, human status, status color, and an endpoint URL.
var services = new[]
{
    ("web",   "Running",        Color.LightGreen, "http://localhost:8080"),
    ("db",    "No listener",    Color.LightRed,   "http://localhost:5432"),
    ("cache", "Awaiting setup", Color.DarkGray,   "http://localhost:6379"),
};

var rows = new string[services.Length][];
var colors = new Color?[services.Length][];
for (var i = 0; i < services.Length; i++)
{
    var (name, status, color, url) = services[i];
    // The endpoint cell is a clickable hyperlink; the link label is the
    // bare host:port, the target is the full URL.
    var label = url.Replace("http://", "");
    rows[i]   = [name, status, Crt.Link(label, url)];
    colors[i] = [null, color, null];
}

Table.Print(
    headers: ["Service", "Status", "Endpoint"],
    rows: rows,
    cellColors: colors,
    headerColor: Color.LightCyan,
    borderColor: Color.DarkGray);

Crt.WriteLine();
Rule.Print(color: Color.DarkGray);
using (Crt.WithStyle(fg: Color.LightGray))
    Crt.WriteLine(" 3 services · 1 up · ctrl-c to quit");
Crt.WriteLine();

Crt.ResetColor();
return 0;
