using System.IO.Compression;
using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools.Build.Osx;

internal class PkgUnpacker
{
    public bool Unpack(string pkgPath, string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(pkgPath);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        if (!File.Exists(pkgPath))
        {
            StdErr.RedLine($"File {pkgPath} does not exist.");
            return false;
        }

        IoHelpers.RecreateDirectory(outputDirectory);

        var payloadFile = Path.Combine(outputDirectory, "Payload");
        if (!ExtractPayload(pkgPath, payloadFile))
        {
            return false;
        }

        return ExtractContent(payloadFile, outputDirectory);
    }

    private bool ExtractContent(string inputPath, string outputDirectory)
    {
        try
        {
            using var cpio = CpioFile.Open(inputPath);

            foreach (var file in cpio.Entries())
            {
                if (file.IsDirectory)
                {
                    if (file.Name == ".")
                    {
                        continue;
                    }

                    var dir = Path.Combine(outputDirectory, file.Name);
                    Directory.CreateDirectory(dir);
                    continue;
                }

                if (file.IsSymLink)
                {
                    StdOut.YellowLine($"Symbolic link: {file.Name} -> {file.LinkTarget}");
                    continue;
                }

                if (file.Data == null)
                {
                    //wut?
                    continue;
                }
                using var source = file.Data!;

                var outputFile = Path.Combine(outputDirectory, file.Name);
                using var output = File.Open(outputFile, FileMode.Create);
                source.CopyTo(output);

                if (ConsoleConfig.IsNaggy)
                {
                    StdOut.NormalLine($"Extracted {file.Name}");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Cpio failed on '{inputPath}': {ex.Message}");
            return false;
        }
    }

    private bool ExtractPayload(string pkgPath, string outputPath)
    {
        var compressedPath = outputPath + ".gz";
        try
        {
            using (var input = File.OpenRead(pkgPath))
            {
                using var xar = new XarFile(input);

                var pkgEntry =
                    xar.Files.FirstOrDefault(e => e is { Type: XarEntryType.Directory, Name: "Firebird.pkg" });
                if (pkgEntry is null)
                {
                    StdErr.RedLine($"Cannot extract Firebird.pkg from '{pkgPath}'.");
                    return false;
                }

                var payloadEntry =
                    pkgEntry.Files.FirstOrDefault(e => e is { Type: XarEntryType.File, Name: "Payload" });
                if (payloadEntry is null)
                {
                    StdErr.RedLine($"Cannot extract payload from '{pkgPath}'.");
                    return false;
                }

                xar.ExtractEntryTo(payloadEntry, compressedPath);
            }

            using var compressedStream = File.Open(compressedPath, FileMode.Open);
            using var gzStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var outputStream = File.Open(outputPath, FileMode.Create);
            gzStream.CopyTo(outputStream);

            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Extract failed: {ex.Message}");
            return false;
        }
    }
}
