namespace Std.FirebirdEmbedded.Tools.Build.Osx;

internal class CpioEntry : IDisposable
{
    public string Name { get; }
    public long Size { get; }
    public LinuxFileMode Mode { get; }
    public Stream? Data { get; }

    public string? LinkTarget { get; }

    public CpioHeader Header { get; }

    public CpioEntry(string name, long size, LinuxFileMode mode, Stream? data, string? linkTarget, CpioHeader header)
    {
        Name = name;
        Data = data;
        LinkTarget = linkTarget;
        Header = header;
        Size = size;
        Mode = mode;
    }

    public bool IsDirectory => (Mode & LinuxFileMode.S_IFDIR) == LinuxFileMode.S_IFDIR;
    public bool IsFile => (Mode & LinuxFileMode.S_IFREG) == LinuxFileMode.S_IFREG;
    public bool IsSymLink => (Mode & LinuxFileMode.S_IFLNK) == LinuxFileMode.S_IFLNK;

    public void Dispose() => Data?.Dispose();

    public override string ToString()
    {
        return $"'{Name}': {Type()} [{Size}]";

        string Type()
        {
            if (IsDirectory)
            {
                return "dir";
            }

            if (IsFile)
            {
                return "file";
            }

            if (IsSymLink)
            {
                return "symlink";
            }

            return "unknown";
        }
    }
}
