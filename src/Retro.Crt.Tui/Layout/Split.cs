namespace Retro.Crt.Tui.Layout;

/// <summary>
/// Slice a parent <see cref="Rect"/> into N child rects along one axis,
/// using a mix of fixed and star <see cref="LayoutSize"/>s. Pure
/// geometry, zero-alloc — output goes into a caller-supplied span.
/// </summary>
public static class Split
{
    /// <summary>
    /// Slice <paramref name="bounds"/> into vertical strips left-to-right.
    /// Each strip keeps the parent's full height. <paramref name="output"/>
    /// must have the same length as <paramref name="sizes"/>.
    /// </summary>
    public static void Horizontal(Rect bounds, ReadOnlySpan<LayoutSize> sizes, Span<Rect> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(output.Length, sizes.Length, nameof(output));
        SliceAxis(bounds, sizes, output, horizontal: true);
    }

    /// <summary>
    /// Slice <paramref name="bounds"/> into horizontal strips top-to-bottom.
    /// Each strip keeps the parent's full width. <paramref name="output"/>
    /// must have the same length as <paramref name="sizes"/>.
    /// </summary>
    public static void Vertical(Rect bounds, ReadOnlySpan<LayoutSize> sizes, Span<Rect> output)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(output.Length, sizes.Length, nameof(output));
        SliceAxis(bounds, sizes, output, horizontal: false);
    }

    private static void SliceAxis(
        Rect bounds, ReadOnlySpan<LayoutSize> sizes, Span<Rect> output, bool horizontal)
    {
        if (sizes.IsEmpty) return;

        var available = horizontal ? bounds.Width : bounds.Height;
        if (available <= 0)
        {
            for (var i = 0; i < sizes.Length; i++) output[i] = Rect.Empty;
            return;
        }

        var fixedTotal = 0;
        var starTotal  = 0;
        for (var i = 0; i < sizes.Length; i++)
        {
            fixedTotal += sizes[i].FixedCells;
            starTotal  += sizes[i].StarWeight;
        }

        // Fixed tracks are subtracted first; if they overflow they get
        // shrunk pro-rata so every track still gets a non-negative slot.
        var starSpace = available - fixedTotal;
        if (starSpace < 0)
        {
            // Fixed tracks alone don't fit — scale them down to fit
            // exactly. Star tracks collapse to zero.
            DistributeFixedOverflow(bounds, sizes, output, available, fixedTotal, horizontal);
            return;
        }

        // Distribute star space using largest-remainder rounding so the
        // sum of allocated star cells equals `starSpace` exactly even
        // when (starSpace * weight) doesn't divide evenly.
        Span<int> allocated = stackalloc int[sizes.Length];
        Span<int> remainders = stackalloc int[sizes.Length];
        var givenStar = 0;
        if (starTotal > 0)
        {
            for (var i = 0; i < sizes.Length; i++)
            {
                if (sizes[i].StarWeight == 0) continue;
                var num = starSpace * sizes[i].StarWeight;
                allocated[i] = num / starTotal;
                remainders[i] = num % starTotal;
                givenStar += allocated[i];
            }

            var leftover = starSpace - givenStar;
            // Hand the leftover cells to the tracks with the largest
            // fractional remainder, ties broken by index.
            while (leftover > 0)
            {
                var bestIdx = -1;
                var bestRem = -1;
                for (var i = 0; i < sizes.Length; i++)
                {
                    if (sizes[i].StarWeight == 0) continue;
                    if (remainders[i] > bestRem)
                    {
                        bestRem = remainders[i];
                        bestIdx = i;
                    }
                }
                if (bestIdx < 0) break;
                allocated[bestIdx]++;
                remainders[bestIdx] = -1;
                leftover--;
            }
        }

        // Walk the axis, materializing rects.
        var cursor = horizontal ? bounds.X : bounds.Y;
        for (var i = 0; i < sizes.Length; i++)
        {
            var slot = sizes[i].IsFixed ? sizes[i].FixedCells : allocated[i];
            output[i] = horizontal
                ? new Rect(cursor, bounds.Y, slot, bounds.Height)
                : new Rect(bounds.X, cursor, bounds.Width, slot);
            cursor += slot;
        }
    }

    private static void DistributeFixedOverflow(
        Rect bounds, ReadOnlySpan<LayoutSize> sizes, Span<Rect> output,
        int available, int fixedTotal, bool horizontal)
    {
        // Scale fixed tracks proportionally to fit. Star tracks → zero.
        Span<int> allocated = stackalloc int[sizes.Length];
        var assigned = 0;
        for (var i = 0; i < sizes.Length; i++)
        {
            if (!sizes[i].IsFixed) continue;
            allocated[i] = (int)((long)sizes[i].FixedCells * available / fixedTotal);
            assigned += allocated[i];
        }
        // Hand leftover cells to the largest fixed tracks.
        var leftover = available - assigned;
        while (leftover > 0)
        {
            var bestIdx = -1;
            var bestSize = -1;
            for (var i = 0; i < sizes.Length; i++)
            {
                if (!sizes[i].IsFixed) continue;
                if (sizes[i].FixedCells > bestSize)
                {
                    bestSize = sizes[i].FixedCells;
                    bestIdx = i;
                }
            }
            if (bestIdx < 0) break;
            allocated[bestIdx]++;
            leftover--;
        }

        var cursor = horizontal ? bounds.X : bounds.Y;
        for (var i = 0; i < sizes.Length; i++)
        {
            var slot = allocated[i]; // 0 for star tracks
            output[i] = horizontal
                ? new Rect(cursor, bounds.Y, slot, bounds.Height)
                : new Rect(bounds.X, cursor, bounds.Width, slot);
            cursor += slot;
        }
    }
}
