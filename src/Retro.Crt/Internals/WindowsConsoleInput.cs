using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Retro.Crt.Internals;

/// <summary>
/// Windows console input mode + ReadFile / WaitForSingleObject bindings,
/// used by <c>RawMode</c> and the input loop. AOT-clean: every PInvoke
/// goes through <see cref="LibraryImportAttribute"/>.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsConsoleInput
{
    public const int StdInputHandle = -10;

    public const uint EnableProcessedInput        = 0x0001;
    public const uint EnableLineInput             = 0x0002;
    public const uint EnableEchoInput             = 0x0004;
    public const uint EnableWindowInput           = 0x0008;
    public const uint EnableVirtualTerminalInput  = 0x0200;

    public const uint WaitObject0 = 0x00000000;
    public const uint WaitTimeout = 0x00000102;

    public static readonly IntPtr InvalidHandle = new(-1);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadFile(
        IntPtr hFile,
        ref byte lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
}
