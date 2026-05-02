using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Retro.Crt.Internals;

/// <summary>
/// Enables ANSI escape processing on the legacy Windows console host. On
/// Windows Terminal / ConPTY this is already on, but the call is harmless and
/// idempotent. AOT-clean: uses <see cref="LibraryImportAttribute"/>.
/// </summary>
internal static partial class WindowsVt
{
    private const int  StdOutputHandle                  = -11;
    private const uint EnableVirtualTerminalProcessing  = 0x0004;

    private static readonly IntPtr InvalidHandle = new(-1);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [SupportedOSPlatform("windows")]
    public static bool TryEnable()
    {
        try
        {
            var handle = GetStdHandle(StdOutputHandle);
            if (handle == IntPtr.Zero || handle == InvalidHandle) return false;
            if (!GetConsoleMode(handle, out var mode)) return false;
            mode |= EnableVirtualTerminalProcessing;
            return SetConsoleMode(handle, mode);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
