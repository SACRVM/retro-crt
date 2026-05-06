using System.Runtime.InteropServices;

namespace Retro.Crt.Internals;

/// <summary>
/// Cross-platform "window resized?" signal. On Linux/macOS we install a
/// <c>SIGWINCH</c> handler that flips an atomic flag; the application
/// loop drains the flag on every iteration and re-samples the terminal
/// size. On Windows the handler is a no-op — callers fall back to the
/// 16 ms polling path that already runs in the loop, which is fast
/// enough for an interactive resize and avoids the cost of a custom
/// <c>ReadConsoleInput</c> pipeline just for one event.
/// </summary>
internal static partial class WindowResize
{
    private static int _flag;
    private static bool _installed;

    /// <summary>
    /// Install the <c>SIGWINCH</c> handler if not already installed.
    /// Idempotent. AOT-clean (<c>LibraryImport</c> + <c>UnmanagedCallersOnly</c>).
    /// </summary>
    public static void Install()
    {
        if (_installed) return;
        _installed = true;

        if (OperatingSystem.IsWindows()) return;

        unsafe
        {
            // BSD/glibc signal() with SIGWINCH: handler stays installed
            // across deliveries on both Linux and macOS, no need for
            // sigaction's flags. Function-pointer cast routes the
            // [UnmanagedCallersOnly] managed entry point as a plain
            // C-callable.
            delegate* unmanaged<int, void> handler = &OnSigwinch;
            Signal(SIGWINCH, (IntPtr)handler);
        }
    }

    /// <summary>
    /// Atomically consume any pending resize signal. Returns <c>true</c>
    /// exactly once per delivered <c>SIGWINCH</c> burst — multiple
    /// signals between calls coalesce into one <c>true</c>.
    /// </summary>
    public static bool ConsumePending()
        => Interlocked.Exchange(ref _flag, 0) != 0;

    [UnmanagedCallersOnly]
    private static void OnSigwinch(int sig)
    {
        // Async-signal-safe: a single int store. No allocations, no
        // managed locks, no logging. Drainable from the main thread
        // via ConsumePending.
        Volatile.Write(ref _flag, 1);
    }

    private const int SIGWINCH = 28;

    [LibraryImport("libc", EntryPoint = "signal")]
    private static partial IntPtr Signal(int sig, IntPtr handler);
}
