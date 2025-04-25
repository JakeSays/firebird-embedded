//-----------------------------------------------------------------------
// <copyright file="XarFile.cs" company="Quamotion bv">
//     Copyright (c) 2016 Quamotion. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using DiscUtils.Compression;
using DiscUtils.Streams;
using Nerdbank.Streams;


namespace Std.FirebirdEmbedded.Tools.Build.Osx;

/// <summary>
/// Xar (Extensible ARchives) is a compression file format used by Apple.
/// The file consists of three sections, the header, the table of contents and the heap.
/// The header is a normal C struct, the table of contents is a zlib-compressed XML
/// document and the heap contains the compressed data.
/// </summary>
/// <seealso href="https://github.com/mackyle/xar/wiki/xarformat"/>
internal class XarFile : IDisposable
{
    /// <summary>
    /// The <see cref="Stream"/> around which this <see cref="XarFile"/> wraps.
    /// </summary>
    private readonly Stream _stream;

    /// <summary>
    /// Indicates whether to close <see cref="_stream"/> when we are disposed of.
    /// </summary>
    private readonly bool _leaveOpen;

    /// <summary>
    /// The table of contents, listing the entries compressed in this archive.
    /// </summary>
    private readonly XDocument _toc;

    /// <summary>
    /// The entries contained in the table of contents.
    /// </summary>
    private readonly List<XarFileEntry> _files;

    /// <summary>
    /// The start of the heap.
    /// </summary>
    private readonly ulong _heapStart;

    /// <summary>
    /// Initializes a new instance of the <see cref="XarFile"/> class.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> which represents the XAR archive.
    /// </param>
    /// <param name="leaveOpen">
    /// Indicates whether to close <paramref name="stream"/> when the <see cref="XarFile"/>
    /// is disposed of.
    /// </param>
    public XarFile(Stream stream, bool leaveOpen = true)
    {
        _leaveOpen = leaveOpen;
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        XarHeader header = default;
        var buffer = new byte[((IByteArraySerializable)header).Size];
        _stream.ReadExactly(buffer);
        header.ReadFrom(buffer.AsSpan());

        // Basic validation
        if (header.Signature != XarHeader.Magic)
        {
            throw new InvalidDataException("The XAR header signature is incorrect");
        }

        if (header.Version != XarHeader.CurrentVersion)
        {
            throw new InvalidDataException("The XAR header version is incorrect");
        }

        // Read the digest name, if available.
        var messageDigestNameLength = header.Size - 28;
        _stream.Seek(messageDigestNameLength, SeekOrigin.Current);

        // Read the table of contents
        using (var compressedTocStream = _stream.ReadSlice((long)header.TocLengthCompressed))
        using (var decompressedTocStream = new ZlibStream(compressedTocStream, CompressionMode.Decompress, leaveOpen: true))
        {
            // Read the TOC
            _toc = XDocument.Load(decompressedTocStream);

            var entryStack = new Stack<XarFileEntry>();

            _files = [];
            XarFileEntry? parent = null;

            foreach(var file in _toc
                        .Element("xar") !
                        .Element("toc") !
                        .Elements("file"))
            {
                var entry = MakeEntry(file);
                _files.Add(entry);
            }

            XarFileEntry MakeEntry(XElement xml)
            {
                var entry = new XarFileEntry(xml, parent);
                if (entry.Type != XarEntryType.Directory)
                {
                    return entry;
                }

                entryStack.Push(parent!);
                parent = entry;

                foreach (var child in xml.Elements("file"))
                {
                    var childEntry = MakeEntry(child);
                    parent.Files.Add(childEntry);
                }

                parent = entryStack.Pop();
                return entry;
            }
        }

        _heapStart = header.Size + header.TocLengthCompressed;
    }

    /// <summary>
    /// Gets a list of all files embedded in this <see cref="XarFile"/>.
    /// </summary>
    public List<XarFileEntry> Files => _files;

    /// <summary>
    /// Gets the names of all entries in this <see cref="XarFile"/>.
    /// </summary>
    public List<string> EntryNames => GetEntryNames();

    public void ExtractEntryTo(XarFileEntry entry, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrEmpty(outputPath);

        using var inputStream = Open(entry.FullName);
        using var outputStream = File.Open(outputPath, FileMode.Create);
        inputStream.CopyTo(outputStream);
    }

    public Stream Open(string entryName)
    {
        if (entryName == null)
        {
            throw new ArgumentNullException(nameof(entryName));
        }

        // Fetch the entry detail
        IEnumerable<XarFileEntry> files = _files;
        XarFileEntry? entry = null;

        foreach (var part in entryName.Split('/'))
        {
            entry = files.SingleOrDefault(f => f.Name == part);

            if (entry == null)
            {
                throw new ArgumentOutOfRangeException(nameof(entryName));
            }

            files = entry.Files;
        }

        Debug.Assert(entry != null, "The requested entry was not found");

        if (entry.Type != XarEntryType.File)
        {
            throw new ArgumentOutOfRangeException(nameof(entryName));
        }

        // Construct a substream which maps to the compressed data
        var start = (long)_heapStart + entry.DataOffset;

        _stream.Seek(start, SeekOrigin.Begin);
        var substream = _stream.ReadSlice(entry.DataLength);

        // Special case: uncompressed data can be returned 'as is'
        if (entry.Encoding == "application/octet-stream")
        {
            return substream;
        }

        if (entry.Encoding == "application/x-gzip")
        {
            // Create a new deflate stream, and return it.
            return new ZlibStream(substream, CompressionMode.Decompress, leaveOpen: false);
        }

        if (entry.Encoding == "application/x-bzip2")
        {
            return new BZip2DecoderStream(substream, Ownership.Dispose);
        }

        throw new InvalidDataException($"Unknown compression format '{entry.Encoding}'");
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    private List<string> GetEntryNames()
    {
        var values = new List<string>();

        foreach (var file in _files)
        {
            GetEntryNames(null, file, values);
        }

        return values;
    }

    private void GetEntryNames(string? path, XarFileEntry file, List<string> target)
    {
        var fullName = path == null ? file.Name : $"{path}/{file.Name}";

        target.Add(fullName);

        foreach (var child in file.Files)
        {
            GetEntryNames(fullName, child, target);
        }
    }
}
