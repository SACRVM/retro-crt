using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Retro.Crt.Internals;

namespace Retro.Crt.Input;

/// <summary>
/// Put the terminal into raw mode for the duration of the returned scope:
/// no line buffering, no echo, no <c>IEXTEN</c> editing. Pairs with
/// <c>TerminalInput</c> to actually consume the bytes.
/// </summary>
/// <remarks>
/// <para>
/// On Linux and macOS this calls <c>tcgetattr</c> / <c>tcsetattr</c> on
/// stdin (fd 0). On Windows it flips the console-input mode bits via
/// <c>SetConsoleMode</c> with <c>ENABLE_VIRTUAL_TERMINAL_INPUT</c> so
/// stdin produces ANSI escape sequences exactly like Unix —
/// <c>ReadConsoleInputW</c> is intentionally not used.
/// </para>
/// <para>
/// The default strips signal generation (<c>ISIG</c> on Unix,
/// <c>ENABLE_PROCESSED_INPUT</c> on Windows): Ctrl-C arrives as a
/// regular <see cref="KeyEvent"/> for the app to handle. Pass
/// <c>keepSignals: true</c> to keep SIGINT live — the safer choice when
/// you don't want a hung app to swallow the user's Ctrl-C.
/// </para>
/// <para>
/// Disposing restores the previous terminal state. A
/// <c>ProcessExit</c> handler is registered lazily on first use so the
/// user's shell isn't left in raw mode if the process exits without
/// disposing the scope.
/// </para>
/// </remarks>
public static class RawMode
{
    private static readonly Lock Gate = new();
    private static IDisposable? _activeScope;
    private static bool _exitHandlerRegistered;

    /// <summary>
    /// Enter raw mode. Returns a scope that restores the previous
    /// terminal state on disposal. Throws
    /// <see cref="InvalidOperationException"/> if raw mode is already
    /// active — nesting is not supported, since terminal state is a
    /// single global resource.
    /// </summary>
    public static IDisposable Enter(bool keepSignals = false)
    {
        lock (Gate)
        {
            if (_activeScope is not null)
                throw new InvalidOperationException(
                    "Raw mode is already active. Dispose the previous scope before entering again.");

            EnsureExitHandler();

            IDisposable scope;
            if (OperatingSystem.IsWindows())
                scope = WindowsBackend.Enter(keepSignals);
            else if (OperatingSystem.IsLinux())
                scope = LinuxBackend.Enter(keepSignals);
            else if (OperatingSystem.IsMacOS())
                scope = DarwinBackend.Enter(keepSignals);
            else
                throw new PlatformNotSupportedException(
                    "Raw mode is supported on Linux, macOS, and Windows only.");

            _activeScope = scope;
            return new Scope(scope);
        }
    }

    private static void EnsureExitHandler()
    {
        if (_exitHandlerRegistered) return;
        _exitHandlerRegistered = true;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreOnExit();
    }

    private static void RestoreOnExit()
    {
        lock (Gate)
        {
            if (_activeScope is null) return;
            try { _activeScope.Dispose(); }
            catch { /* shutdown — better to swallow than crash on top */ }
            _activeScope = null;
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly IDisposable _inner;
        private bool _disposed;

        public Scope(IDisposable inner) { _inner = inner; }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (Gate)
            {
                if (ReferenceEquals(_activeScope, this) || ReferenceEquals(_activeScope, _inner))
                    _activeScope = null;
            }
            _inner.Dispose();
        }
    }

    [SupportedOSPlatform("linux")]
    private static class LinuxBackend
    {
        public static IDisposable Enter(bool keepSignals)
        {
            const int fd = 0;
            if (LibcLinux.Tcgetattr(fd, out var saved) != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "tcgetattr(stdin) failed; is stdin attached to a terminal?");

            var raw = saved;
            var lflagClear = TermiosLinux.ICANON | TermiosLinux.ECHO | TermiosLinux.IEXTEN;
            if (!keepSignals) lflagClear |= TermiosLinux.ISIG;
            raw.c_lflag &= ~lflagClear;

            raw.c_cc[TermiosLinux.VMIN]  = 1;
            raw.c_cc[TermiosLinux.VTIME] = 0;

            if (LibcLinux.Tcsetattr(fd, TermiosLinux.TCSANOW, in raw) != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "tcsetattr(stdin) failed while entering raw mode.");

            return new LinuxScope(saved);
        }

        private sealed class LinuxScope : IDisposable
        {
            private readonly TermiosLinux _saved;
            private bool _disposed;

            public LinuxScope(TermiosLinux saved) { _saved = saved; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                LibcLinux.Tcsetattr(0, TermiosLinux.TCSANOW, in _saved);
            }
        }
    }

    [SupportedOSPlatform("macos")]
    private static class DarwinBackend
    {
        public static IDisposable Enter(bool keepSignals)
        {
            const int fd = 0;
            if (LibcDarwin.Tcgetattr(fd, out var saved) != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "tcgetattr(stdin) failed; is stdin attached to a terminal?");

            var raw = saved;
            var lflagClear = TermiosDarwin.ICANON | TermiosDarwin.ECHO | TermiosDarwin.IEXTEN;
            if (!keepSignals) lflagClear |= TermiosDarwin.ISIG;
            raw.c_lflag &= ~lflagClear;

            raw.c_cc[TermiosDarwin.VMIN]  = 1;
            raw.c_cc[TermiosDarwin.VTIME] = 0;

            if (LibcDarwin.Tcsetattr(fd, TermiosDarwin.TCSANOW, in raw) != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "tcsetattr(stdin) failed while entering raw mode.");

            return new DarwinScope(saved);
        }

        private sealed class DarwinScope : IDisposable
        {
            private readonly TermiosDarwin _saved;
            private bool _disposed;

            public DarwinScope(TermiosDarwin saved) { _saved = saved; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                LibcDarwin.Tcsetattr(0, TermiosDarwin.TCSANOW, in _saved);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static class WindowsBackend
    {
        public static IDisposable Enter(bool keepSignals)
        {
            var handle = WindowsConsoleInput.GetStdHandle(WindowsConsoleInput.StdInputHandle);
            if (handle == IntPtr.Zero || handle == WindowsConsoleInput.InvalidHandle)
                throw new InvalidOperationException(
                    "GetStdHandle(STD_INPUT_HANDLE) returned an invalid handle; is stdin attached to a console?");

            if (!WindowsConsoleInput.GetConsoleMode(handle, out var saved))
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "GetConsoleMode(stdin) failed; is stdin a console handle?");

            var raw = saved;
            raw &= ~(WindowsConsoleInput.EnableLineInput | WindowsConsoleInput.EnableEchoInput);
            raw |= WindowsConsoleInput.EnableVirtualTerminalInput;
            if (!keepSignals) raw &= ~WindowsConsoleInput.EnableProcessedInput;
            else              raw |= WindowsConsoleInput.EnableProcessedInput;

            if (!WindowsConsoleInput.SetConsoleMode(handle, raw))
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "SetConsoleMode(stdin) failed while entering raw mode.");

            return new WindowsScope(handle, saved);
        }

        private sealed class WindowsScope : IDisposable
        {
            private readonly IntPtr _handle;
            private readonly uint _saved;
            private bool _disposed;

            public WindowsScope(IntPtr handle, uint saved)
            {
                _handle = handle;
                _saved = saved;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                WindowsConsoleInput.SetConsoleMode(_handle, _saved);
            }
        }
    }
}
