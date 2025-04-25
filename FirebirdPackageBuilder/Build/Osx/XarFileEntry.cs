//-----------------------------------------------------------------------
// <copyright file="XarFileEntry.cs" company="Quamotion bv">
//     Copyright (c) 2016 Quamotion. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Xml.Linq;


namespace Std.FirebirdEmbedded.Tools.Build.Osx;

internal class XarFileEntry
{
    private readonly XElement _tocEntry;

    public XarFileEntry(XElement tocEntry, XarFileEntry? parent)
    {
        _tocEntry = tocEntry ?? throw new ArgumentNullException(nameof(tocEntry));
        Parent = parent;

        FullName = parent == null
            ? Name
            : Path.Combine(parent.FullName, Name);
    }

    public string FullName { get; }

    public XarFileEntry? Parent { get; }

    /// <summary>
    /// Gets an Id which uniquely identifies the entry.
    /// </summary>
    public int Id => (int)_tocEntry.Attribute("id")!;

    /// <summary>
    /// Gets the date and time at which the file was created.
    /// </summary>
    public DateTimeOffset Created => (DateTimeOffset)_tocEntry.Element("ctime")!;

    /// <summary>
    /// Gets the date at which the file was modified.
    /// </summary>
    public DateTimeOffset Modified => (DateTimeOffset)_tocEntry.Element("mtime")!;

    /// <summary>
    /// Gets the date at which the file as archived.
    /// </summary>
    public DateTimeOffset Archived => (DateTimeOffset)_tocEntry.Element("atime")!;

    /// <summary>
    /// Gets the name of the group which owns the file.
    /// </summary>
    public string Group => (string)_tocEntry.Element("group")!;

    /// <summary>
    /// Gets the ID of the group which owns the file.
    /// </summary>
    public int GroupId => (int)_tocEntry.Element("gid")!;

    /// <summary>
    /// Gets the name of the user which owns the file.
    /// </summary>
    public string User => (string)_tocEntry.Element("user")!;

    /// <summary>
    /// Gets the ID of the user which owns the file.
    /// </summary>
    public int UserId => (int)_tocEntry.Element("uid")!;

    /// <summary>
    /// Gets the file mode flags of the file.
    /// </summary>
    public LinuxFileMode FileMode => (LinuxFileMode)Convert.ToInt32(_tocEntry.Element("mode")?.Value ?? "0", 8);

    /// <summary>
    /// Gets the device number of the file.
    /// </summary>
    public int DeviceNo => (int)_tocEntry.Element("deviceno")!;

    /// <summary>
    /// Gets the inode number of the file.
    /// </summary>
    public long Inode => (long)_tocEntry.Element("inode")!;

    /// <summary>
    /// Gets the name of the entry.
    /// </summary>
    public string Name => (string)_tocEntry.Element("name")!;

    /// <summary>
    /// Gets the type of the entry, such as <c>file</c> for a file entry or <c>directory</c> for a directory entry.
    /// </summary>
    public XarEntryType Type => Enum.Parse<XarEntryType>((string)_tocEntry.Element("type")!, ignoreCase: true);

    /// <summary>
    /// Gets the offset of the compressed data in the XAR archive, relative to the start of the
    /// heap.
    /// </summary>
    public long DataOffset => (long?)_tocEntry.Element("data")?.Element("offset") ?? 0;

    /// <summary>
    /// Gets the size of the uncompressed data.
    /// </summary>
    public long DataSize => (long?)_tocEntry.Element("data")?.Element("size") ?? 0;

    /// <summary>
    /// Gets the length of the compressed data in the XAR archive.
    /// </summary>
    public long DataLength => (long?)_tocEntry.Element("data")?.Element("length") ?? 0;

    /// <summary>
    /// Gets the encoding used to compress the data. Currently the only supported value is
    /// <c>application/x-gzip</c> for Deflate encoding.
    /// </summary>
    public string Encoding => (string)_tocEntry.Element("data")?.Element("encoding")?.Attribute("style")!;

    /// <summary>
    /// Gets the checksum of the uncompressed data.
    /// </summary>
    public string? ExtractedChecksum => (string?)_tocEntry.Element("data")?.Element("extracted-checksum");

    /// <summary>
    /// Gets the algorithm used to calculate the checksum of the uncompressed data.
    /// </summary>
    public string ExtractedChecksumStyle => (string)_tocEntry.Element("data")?.Element("extracted-checksum")?.Attribute("style")!;

    /// <summary>
    /// Gets the checksum of the compressed data.
    /// </summary>
    public string ArchivedChecksum => (string)_tocEntry.Element("data")?.Element("archived-checksum")!;

    /// <summary>
    /// Gets the algorithm used to calculate the checksum of the compressed data.
    /// </summary>
    public string ArchivedChecksumStyle => (string)_tocEntry.Element("data")?.Element("archived-checksum")?.Attribute("style")!;

    /// <summary>
    /// Gets a list of child entries.
    /// </summary>
    public List<XarFileEntry> Files { get; } = [];

    public override string ToString()
    {
        return $"{Type}: {FullName}";
    }
}
