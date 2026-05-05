using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Retro.Crt.Internals;

[InlineArray(32)]
internal struct CcArray32
{
    private byte _e0;
}

/// <summary>
/// Linux glibc <c>struct termios</c>: 4-byte <c>tcflag_t</c>, NCCS = 32,
/// includes the <c>c_line</c> field. Layout matches <c>bits/termios-struct.h</c>
/// for x86_64 and aarch64 glibc; not used on musl / uClibc but glibc is
/// the de-facto contract for distro Linux.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct TermiosLinux
{
    public uint c_iflag;
    public uint c_oflag;
    public uint c_cflag;
    public uint c_lflag;
    public byte c_line;
    public CcArray32 c_cc;
    public uint c_ispeed;
    public uint c_ospeed;

    public const uint ISIG   = 0x00000001;
    public const uint ICANON = 0x00000002;
    public const uint ECHO   = 0x00000008;
    public const uint IEXTEN = 0x00008000;

    public const int VTIME = 5;
    public const int VMIN  = 6;

    public const int TCSANOW = 0;
}

[SupportedOSPlatform("linux")]
internal static partial class LibcLinux
{
    [LibraryImport("libc", EntryPoint = "tcgetattr", SetLastError = true)]
    internal static partial int Tcgetattr(int fd, out TermiosLinux t);

    [LibraryImport("libc", EntryPoint = "tcsetattr", SetLastError = true)]
    internal static partial int Tcsetattr(int fd, int actions, in TermiosLinux t);

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

    // Linux nfds_t = unsigned long (8 B on LP64).
    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    internal static partial int Poll(ref PollFd fds, nuint nfds, int timeoutMs);
}
