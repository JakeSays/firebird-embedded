namespace Std.FirebirdEmbedded.Tools.Build.Osx;

internal static class StreamExtensions
{
    public static int ReadBlock(this Stream stream, Memory<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var totalBytesRead = 0;
        while (buffer.Length > totalBytesRead)
        {
            var bytesJustRead = stream.Read(buffer[totalBytesRead..].Span);
            totalBytesRead += bytesJustRead;
            if (bytesJustRead == 0)
            {
                // We've reached the end of the stream.
                break;
            }
        }

        return totalBytesRead;
    }

    public static void ReadBlockOrThrow(this Stream stream, Memory<byte> buffer)
    {
        var bytesRead = ReadBlock(stream, buffer);
        if (bytesRead < buffer.Length)
        {
            throw new EndOfStreamException(
                $"Expected {buffer.Length} bytes but only received {bytesRead} before the stream ended.");
        }
    }
}
