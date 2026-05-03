namespace Retro.Crt;

/// <summary>
/// Horizontal alignment of content lines inside a <c>Banner.Box</c>.
/// Affects the spaces injected around each line; the frame and padding
/// stay the same.
/// </summary>
public enum BoxAlign : byte
{
    /// <summary>
    /// Lines hug the left edge — trailing space fills the right side.
    /// Default; matches every Pascal-CRT muscle-memory banner.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Lines centred inside the inner content area. Odd leftover space
    /// goes on the right.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Lines hug the right edge — leading space fills the left side.
    /// </summary>
    Right = 2,
}
