using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Retro.Crt.Internals;

/// <summary>
/// Byte source for the input loop. Lets <c>TerminalInput</c> read raw
/// bytes from stdin without baking the OS path into the consumer; tests
/// inject a stream-backed source instead.
/// </summary>
internal interface IInputSource
{
    /// <summary>Blocks until at least one byte is available; returns the count. 0 = EOF.</summary>
    int Read(Span<byte> buffer);

    /// <summary>Best-effort wait for incoming bytes. <c>0</c> = poll without blocking; negative = block forever.</summary>
    bool TryWait(int timeoutMs);
}

internal static class OsInputSource
{
    public static IInputSource Create()
    {
        if (OperatingSystem.IsLinux())   return new LinuxSource();
        if (OperatingSystem.IsMacOS())   return new DarwinSource();
        if (OperatingSystem.IsWindows()) return new WindowsSource();
        throw new PlatformNotSupportedException(
            "Terminal input is supported on Linux, macOS, and Windows only.");
    }

    [SupportedOSPlatform("linux")]
    private sealed class LinuxSource : IInputSource
    {
        public int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty) return 0;
            ref var first = ref MemoryMarshal.GetReference(buffer);
            var n = LibcLinux.Read(0, ref first, (nuint)buffer.Length);
            if (n < 0)
                throw new IOException(
                    $"read(stdin) failed: errno={Marshal.GetLastWin32Error()}",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            return (int)n;
        }

        public bool TryWait(int timeoutMs)
        {
            var pfd = new LibcLinux.PollFd { fd = 0, events = LibcLinux.POLLIN };
            var r = LibcLinux.Poll(ref pfd, 1, timeoutMs);
            if (r < 0)
                throw new IOException(
                    $"poll(stdin) failed: errno={Marshal.GetLastWin32Error()}",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            return r > 0 && (pfd.revents & LibcLinux.POLLIN) != 0;
        }
    }

    [SupportedOSPlatform("macos")]
    private sealed class DarwinSource : IInputSource
    {
        public int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty) return 0;
            ref var first = ref MemoryMarshal.GetReference(buffer);
            var n = LibcDarwin.Read(0, ref first, (nuint)buffer.Length);
            if (n < 0)
                throw new IOException(
                    $"read(stdin) failed: errno={Marshal.GetLastWin32Error()}",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            return (int)n;
        }

        public bool TryWait(int timeoutMs)
        {
            var pfd = new LibcDarwin.PollFd { fd = 0, events = LibcDarwin.POLLIN };
            var r = LibcDarwin.Poll(ref pfd, 1, timeoutMs);
            if (r < 0)
                throw new IOException(
                    $"poll(stdin) failed: errno={Marshal.GetLastWin32Error()}",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            return r > 0 && (pfd.revents & LibcDarwin.POLLIN) != 0;
        }
    }

    [SupportedOSPlatform("windows")]
    private sealed class WindowsSource : IInputSource
    {
        private readonly IntPtr _handle;

        public WindowsSource()
        {
            _handle = WindowsConsoleInput.GetStdHandle(WindowsConsoleInput.StdInputHandle);
            if (_handle == IntPtr.Zero || _handle == WindowsConsoleInput.InvalidHandle)
                throw new InvalidOperationException(
                    "GetStdHandle(STD_INPUT_HANDLE) returned an invalid handle; is stdin attached to a console?");
        }

        public int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty) return 0;
            ref var first = ref MemoryMarshal.GetReference(buffer);
            if (!WindowsConsoleInput.ReadFile(_handle, ref first, (uint)buffer.Length, out var read, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "ReadFile(stdin) failed.");
            return (int)read;
        }

        public bool TryWait(int timeoutMs)
        {
            // Best-effort: WaitForSingleObject on a console handle is
            // signaled by any input record (including key-up events that
            // produce no VT bytes). Callers that need strict non-blocking
            // semantics should run ReadEvent on a background thread.
            var ms = timeoutMs < 0 ? 0xFFFFFFFFu : (uint)timeoutMs;
            var r = WindowsConsoleInput.WaitForSingleObject(_handle, ms);
            return r == WindowsConsoleInput.WaitObject0;
        }
    }
}
