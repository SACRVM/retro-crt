namespace Retro.Crt.Internals;

/// <summary>
/// Pure RGB → palette quantization. No I/O, no allocations on the hot
/// path. Used by <see cref="Color.For(ColorDepth)"/> to step a truecolor
/// value down to xterm-256 or BIOS-16 anchors.
/// </summary>
internal static class ColorQuantization
{
    /// <summary>
    /// RGB anchor values for the 16 BIOS palette slots. Same indices as
    /// <see cref="Color.Standard"/> — 0=Black, 14=Yellow, 15=White.
    /// Values match the canonical IBM PC / DOS 16-color palette so
    /// <see cref="Color.Yellow"/>.R == 255 reflects what the slot
    /// actually paints on a CRT, not zero.
    /// </summary>
    public static readonly (byte R, byte G, byte B)[] BiosPalette =
    [
        (  0,   0,   0),  // 0  Black
        (  0,   0, 170),  // 1  DarkBlue
        (  0, 170,   0),  // 2  DarkGreen
        (  0, 170, 170),  // 3  DarkCyan
        (170,   0,   0),  // 4  DarkRed
        (170,   0, 170),  // 5  DarkMagenta
        (170,  85,   0),  // 6  Brown
        (170, 170, 170),  // 7  LightGray
        ( 85,  85,  85),  // 8  DarkGray
        ( 85,  85, 255),  // 9  LightBlue
        ( 85, 255,  85),  // 10 LightGreen
        ( 85, 255, 255),  // 11 LightCyan
        (255,  85,  85),  // 12 LightRed
        (255,  85, 255),  // 13 LightMagenta
        (255, 255,  85),  // 14 Yellow
        (255, 255, 255),  // 15 White
    ];

    /// <summary>
    /// Six steps used by the xterm 6×6×6 RGB cube. Nearest of these is
    /// picked per channel.
    /// </summary>
    private static readonly byte[] CubeSteps = [0, 95, 135, 175, 215, 255];

    /// <summary>
    /// Find the BIOS palette index whose RGB anchor is closest to
    /// <paramref name="r"/>/<paramref name="g"/>/<paramref name="b"/>
    /// in straight Euclidean distance. Returns 0..15.
    /// </summary>
    public static byte ToStandard16(byte r, byte g, byte b)
    {
        var bestIndex = 0;
        var bestDist = int.MaxValue;
        for (var i = 0; i < BiosPalette.Length; i++)
        {
            var p = BiosPalette[i];
            var dr = r - p.R;
            var dg = g - p.G;
            var db = b - p.B;
            var d = dr * dr + dg * dg + db * db;
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }
        return (byte)bestIndex;
    }

    /// <summary>
    /// Snap an RGB triple to the closest xterm 256-palette entry — the
    /// 6×6×6 color cube (16..231) or the 24-step grayscale ramp
    /// (232..255), whichever is closer. Returns 16..255 (never 0..15;
    /// those are reserved for the user's terminal-themed Standard16
    /// slots).
    /// </summary>
    public static byte ToXterm256(byte r, byte g, byte b)
    {
        var ri = NearestCubeStep(r);
        var gi = NearestCubeStep(g);
        var bi = NearestCubeStep(b);

        var cubeR = CubeSteps[ri];
        var cubeG = CubeSteps[gi];
        var cubeB = CubeSteps[bi];
        var cubeDist = SqDist(r, g, b, cubeR, cubeG, cubeB);

        // Grayscale ramp at 232..255 has 24 evenly spaced steps from 8
        // to 238 in increments of 10 (xterm convention).
        var grayValue = (r + g + b) / 3;
        var grayStep = grayValue < 8 ? 0 : grayValue > 238 ? 23 : (grayValue - 8) / 10;
        var grayLevel = (byte)(8 + grayStep * 10);
        var grayDist = SqDist(r, g, b, grayLevel, grayLevel, grayLevel);

        if (grayDist < cubeDist)
            return (byte)(232 + grayStep);

        return (byte)(16 + 36 * ri + 6 * gi + bi);
    }

    private static int NearestCubeStep(byte v)
    {
        // Linear search — six entries, branch-free min.
        var best = 0;
        var bestDelta = Abs(v - CubeSteps[0]);
        for (var i = 1; i < CubeSteps.Length; i++)
        {
            var d = Abs(v - CubeSteps[i]);
            if (d < bestDelta) { bestDelta = d; best = i; }
        }
        return best;
    }

    private static int Abs(int x) => x < 0 ? -x : x;

    private static int SqDist(byte r, byte g, byte b, byte tr, byte tg, byte tb)
    {
        var dr = r - tr;
        var dg = g - tg;
        var db = b - tb;
        return dr * dr + dg * dg + db * db;
    }
}
