using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Retro.Crt.Internals;

[InlineArray(20)]
internal struct CcArray20
{
    private byte _e0;
}

/// <summary>
/// Darwin (macOS) <c>struct termios</c>: 8-byte <c>tcflag_t</c> on LP64,
/// NCCS = 20, no <c>c_line</c> field. Layout matches
/// <c>&lt;sys/termios.h&gt;</c> on x86_64 and arm64.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct TermiosDarwin
{
    public ulong c_iflag;
    public ulong c_oflag;
    public ulong c_cflag;
    public ulong c_lflag;
    public CcArray20 c_cc;
    public ulong c_ispeed;
    public ulong c_ospeed;

    public const ulong ECHO   = 0x00000008;
    public const ulong ISIG   = 0x00000080;
    public const ulong ICANON = 0x00000100;
    public const ulong IEXTEN = 0x00000400;

    public const int VMIN  = 16;
    public const int VTIME = 17;

    public const int TCSANOW = 0;
}

[SupportedOSPlatform("macos")]
internal static partial class LibcDarwin
{
    [LibraryImport("libc", EntryPoint = "tcgetattr", SetLastError = true)]
    internal static partial int Tcgetattr(int fd, out TermiosDarwin t);

    [LibraryImport("libc", EntryPoint = "tcsetattr", SetLastError = true)]
    internal static partial int Tcsetattr(int fd, int actions, in TermiosDarwin t);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    internal static partial nint Read(int fd, ref byte buf, nuint count);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    public const short POLLIN = 0x0001;

    // Darwin nfds_t = unsigned int (4 B).
    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    internal static partial int Poll(ref PollFd fds, uint nfds, int timeoutMs);
}
