//-----------------------------------------------------------------------
// <copyright file="XarHeader.cs" company="Quamotion bv">
//     Copyright (c) 2016 Quamotion. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Buffers.Binary;
using DiscUtils.Streams;


namespace Std.FirebirdEmbedded.Tools.Build.Osx;

/// <summary>
/// Represents the header of a <see cref="XarFile"/>.
/// </summary>
internal struct XarHeader : IByteArraySerializable
{
    /// <summary>
    /// The magic for a XAR file. Represents <c>xar!</c> in ASCII.
    /// </summary>
    public const uint Magic = 0x78617221;

    /// <summary>
    /// The current version of the Xar header.
    /// </summary>
    public const uint CurrentVersion = 1;

    /// <summary>
    /// The signature of the header. Should equal <see cref="Magic"/>.
    /// </summary>
    public uint Signature;

    /// <summary>
    /// The size of the header.
    /// </summary>
    public ushort Size;

    /// <summary>
    /// The version of the header format. Should equal <see cref="CurrentVersion"/>.
    /// </summary>
    public ushort Version;

    /// <summary>
    /// The compressed length of the table of contents.
    /// </summary>
    public ulong TocLengthCompressed;

    /// <summary>
    /// The uncompressed length of the table of contents.
    /// </summary>
    public ulong TocLengthUncompressed;

    /// <summary>
    /// The algorithm used to calculate checksums.
    /// </summary>
    public XarChecksum ChecksumAlgorithm;


    public void WriteTo(Span<byte> buffer) => throw new NotSupportedException();

    /// <inheritdoc/>
    int IByteArraySerializable.Size => 28;

    public int ReadFrom(ReadOnlySpan<byte> buffer)
    {
        var offset = 0;
        Signature = BinaryPrimitives.ReadUInt32BigEndian(buffer[offset..]);
        offset += sizeof(uint);
        Size = BinaryPrimitives.ReadUInt16BigEndian(buffer[offset..]);
        offset += sizeof(ushort);
        Version = BinaryPrimitives.ReadUInt16BigEndian(buffer[offset..]);
        offset += sizeof(ushort);
        TocLengthCompressed = BinaryPrimitives.ReadUInt64BigEndian(buffer[offset..]);
        offset += sizeof(ulong);
        TocLengthUncompressed = BinaryPrimitives.ReadUInt64BigEndian(buffer[offset..]);
        offset += sizeof(ulong);
        ChecksumAlgorithm = (XarChecksum)BinaryPrimitives.ReadUInt32BigEndian(buffer[offset..]);

        return Size;
    }
}
