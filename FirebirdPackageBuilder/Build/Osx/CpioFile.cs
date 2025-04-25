//-----------------------------------------------------------------------
// <copyright file="CpioFile.cs" company="Quamotion">
//     Copyright (c) 2016 Quamotion bv. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Nerdbank.Streams;


namespace Std.FirebirdEmbedded.Tools.Build.Osx;

internal sealed class CpioFile : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private long _nextHeaderOffset;

    public CpioFile(Stream stream, bool leaveOpen)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        if (!_stream.CanSeek)
        {
            throw new NotSupportedException("Non-seekable streams are not supported.");
        }

        _leaveOpen = leaveOpen;
    }

    public static CpioFile Open(string path)
    {
        var input = File.OpenRead(path);
        return new CpioFile(input, false);
    }

    public IEnumerable<CpioEntry> Entries()
    {
        while (true)
        {
            CpioHeader header = default;
            string? name;

            using (var headerBuffer = MemoryPool<byte>.Shared.Rent(CpioHeader.Size))
            {
                _stream.Seek(_nextHeaderOffset, SeekOrigin.Begin);
                _stream.ReadBlockOrThrow(headerBuffer.Memory[..CpioHeader.Size]);
                header.ReadFrom(headerBuffer.Memory.Span);

                if (header.Signature != 0x71C7 /* 070707 in octal */)
                {
                    throw new InvalidDataException("The magic for the file entry is invalid");
                }

                using (var nameBuffer = MemoryPool<byte>.Shared.Rent((int)header.Namesize))
                {
                    var nameMemory = nameBuffer.Memory[..(int)header.Namesize];
                    _stream.ReadBlock(nameMemory);
                    name = Encoding.UTF8.GetString(nameMemory[..^1].Span);
                    if (name.StartsWith("./"))
                    {
                        name = name[2..];
                    }
                }

                _nextHeaderOffset = _stream.Position + header.Filesize;
            }

            if (name == "TRAILER!!!")
            {
                yield break;
            }

            if (header.Filesize > 0)
            {
                var data = _stream.ReadSlice(header.Filesize);
                string? symLink = null;
                if ((header.Mode & LinuxFileMode.S_IFLNK) == LinuxFileMode.S_IFLNK)
                {
                    using var sr = new StreamReader(data);
                    symLink = sr.ReadToEnd();
                }

                yield return new CpioEntry(name, symLink == null
                    ? header.Filesize
                    : 0, header.Mode, data, symLink, header);
                continue;
            }

            yield return new CpioEntry(name, 0, header.Mode, null, null, header);
        }
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
