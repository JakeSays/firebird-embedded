//-----------------------------------------------------------------------
// <copyright file="CpioHeader.cs" company="Quamotion bv">
//     Copyright (c) 2016 Quamotion bv. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Text;


namespace Std.FirebirdEmbedded.Tools.Build.Osx;

/// <summary>
/// Represents the header of an individual entry in a CPIO file, in ASCII format.
/// </summary>
/// <seealso href="https://people.freebsd.org/~kientzle/libarchive/man/cpio.5.txt"/>
internal struct CpioHeader
{
    /// <summary>
    /// The integer value octal 070707.  This value can be used to determine
    /// whether this archive is written with little-endian or big-endian integers,
    /// or ASCII.
    /// </summary>
    public uint Signature;

    /// <summary>
    /// The device number from the disk.  These are used by
    /// programs that read cpio archives to determine when two entries
    /// refer to the same file.Programs that synthesize cpio archives
    /// should be careful to set these to distinct values for each entry.
    /// </summary>
    public uint Dev;

    /// <summary>
    /// The inode number from the disk.  These are used by
    /// programs that read cpio archives to determine when two entries
    /// refer to the same file.Programs that synthesize cpio archives
    /// should be careful to set these to distinct values for each entry.
    /// </summary>
    public uint Ino;

    /// <summary>
    /// The mode specifies both the regular permissions and the file type.
    /// </summary>
    public LinuxFileMode Mode;

    /// <summary>
    /// The numeric user id of the owner.
    /// </summary>
    public uint Uid;

    /// <summary>
    /// The numeric group id of the owner.
    /// </summary>
    public uint Gid;

    /// <summary>
    /// The number of links to this file. Directories always have a
    /// value of at least two here. Note that hardlinked files include
    /// file data with every copy in the archive.
    /// </summary>
    public uint Nlink;

    /// <summary>
    /// For block special and character special entries, this field contains
    /// the associated device number.For all other entry types,
    /// it should be set to zero by writers and ignored by readers.
    /// </summary>
    public uint Rdev;

    /// <summary>
    /// Modification time of the file, indicated as the number of seconds
    /// since the start of the epoch, 00:00:00 UTC January 1, 1970.  The
    /// four-byte integer is stored with the most-significant 16 bits
    /// first followed by the least-significant 16 bits.Each of the two
    /// 16 bit values are stored in machine-native byte order.
    /// </summary>
    public DateTimeOffset Mtime;

    /// <summary>
    /// The number of bytes in the pathname that follows the header.
    /// This count includes the trailing NUL byte.
    /// </summary>
    public uint Namesize;

    /// <summary>
    /// The size of the file.  Note that this archive format is limited
    /// to four gigabyte file sizes. See mtime above for a description
    /// of the storage of four-byte integers.
    /// </summary>
    public long Filesize;

    /// <summary>
    /// Gets the size of this struct.
    /// </summary>
    public static int Size => 76;

    /// <summary>
    /// Reads data from a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="buffer">
    /// A buffer which contains the header data.
    /// </param>
    /// <returns>
    /// The number of bytes read.
    /// </returns>
    public int ReadFrom(Span<byte> buffer)
    {
        Signature = ReadOctalString(buffer.Slice(0, 6));
        Dev = ReadOctalString(buffer.Slice(6, 6));
        Ino = ReadOctalString(buffer.Slice(12, 6));
        Mode = (LinuxFileMode)ReadOctalString(buffer.Slice(18, 6));
        Uid = ReadOctalString(buffer.Slice(24, 6));
        Gid = ReadOctalString(buffer.Slice(30, 6));
        Nlink = ReadOctalString(buffer.Slice(36, 6));
        Rdev = ReadOctalString(buffer.Slice(42, 6));
        Mtime = DateTimeOffset.FromUnixTimeSeconds(ReadOctalString(buffer.Slice(48, 11)));
        Namesize = ReadOctalString(buffer.Slice(59, 6));
        Filesize = ReadOctalString(buffer.Slice(65, 11));

        return Size;
    }

    private static uint ReadOctalString(Span<byte> data) => Convert.ToUInt32(Encoding.UTF8.GetString(data), 8);
}
