namespace Retro.Crt.Tui.Layout;

/// <summary>
/// One track size in a <see cref="Split"/>. Either a fixed cell count
/// (<see cref="Cells"/>) or a star weight (<see cref="Star"/>) that
/// claims a proportional share of the leftover space.
/// </summary>
/// <remarks>
/// Borrows the WPF / CSS-grid model — fixed tracks are subtracted from
/// the available space first, then the remainder is divided among star
/// tracks weighted by their star value. <c>Star(1) Star(2)</c> = 1:2.
/// </remarks>
public readonly struct LayoutSize : IEquatable<LayoutSize>
{
    /// <summary>Cell count when this is a fixed track. <c>0</c> when this is a star track.</summary>
    public int FixedCells { get; }
    /// <summary>Weight when this is a star track. <c>0</c> when this is a fixed track.</summary>
    public int StarWeight { get; }

    private LayoutSize(int fixedCells, int starWeight)
    {
        FixedCells = fixedCells;
        StarWeight = starWeight;
    }

    /// <summary>
    /// A fixed-size track of <paramref name="cells"/> cells. Negative
    /// values are clamped to <c>0</c>.
    /// </summary>
    public static LayoutSize Cells(int cells) => new(Math.Max(0, cells), 0);

    /// <summary>
    /// A star track that claims a proportional share of the leftover
    /// space, weighted by <paramref name="weight"/>. Defaults to
    /// <c>1</c>; values below <c>1</c> are clamped up.
    /// </summary>
    public static LayoutSize Star(int weight = 1) => new(0, Math.Max(1, weight));

    /// <summary>True for fixed-size tracks (<see cref="FixedCells"/> &gt; 0).</summary>
    public bool IsFixed => FixedCells > 0;

    /// <summary>True for star tracks (<see cref="StarWeight"/> &gt; 0).</summary>
    public bool IsStar  => StarWeight > 0;

    public bool Equals(LayoutSize other) =>
        FixedCells == other.FixedCells && StarWeight == other.StarWeight;

    public override bool Equals(object? obj) => obj is LayoutSize other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(FixedCells, StarWeight);

    public static bool operator ==(LayoutSize left, LayoutSize right) => left.Equals(right);
    public static bool operator !=(LayoutSize left, LayoutSize right) => !left.Equals(right);
}
