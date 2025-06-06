using System.Formats.Tar;
using System.IO.Compression;
using Std.FirebirdEmbedded.Tools.Build.Osx;
using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools.Build;

internal sealed class AssetUnPacker
{
    private readonly PkgUnpacker _pkgUnpacker = new ();

    public bool UnpackRelease(FirebirdRelease release)
    {
        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine($"Unpacking release {release.Name}");
        }

        foreach (var asset in release.Assets)
        {
            if (!UnpackAsset(asset))
            {
                return false;
            }
        }

        return true;
    }

    private bool UnpackAsset(FirebirdAsset asset)
    {
        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine($"Unpacking asset '{asset.Name}'");
        }

        try
        {
            IoHelpers.RecreateDirectory(asset.UnpackedDirectory);

            switch (asset.ContentType)
            {
                case ContentType.Tarball:
                    UnpackTarball();
                    break;
                case ContentType.Zip:
                    UnpackZipArchive(asset.UnpackedDirectory);
                    break;
                case ContentType.Xar:
                    UnpackPkg();
                    break;
                default:
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Unpacking asset '{asset.Name} failed: {ex.Message}");
            return false;
        }


        void UnpackTarball()
        {
            const string buildRootTarball = "buildroot.tar.gz";

            if (ConsoleConfig.IsLoud)
            {
                StdOut.DarkBlueLine($"Unpacking: '{asset.LocalPath}'.");
            }

            var unpackDirectory = asset.UnpackedBaseDirectory;

            var symbolicLinks = new List<(string Link, string Target)>();

            var outerTarballPath = DecompressTarball(asset.LocalPath, unpackDirectory);
            ExtractTarball(outerTarballPath, unpackDirectory, true, name => name.EndsWith(buildRootTarball));
            var buildRootFile = Path.Combine(unpackDirectory, buildRootTarball);
            var innerTarballPath = DecompressTarball(buildRootFile, unpackDirectory);
            ExtractTarball(innerTarballPath, unpackDirectory, symbolicLinks: symbolicLinks);

            MakeSymbolicLinks(symbolicLinks, unpackDirectory);
        }

        void MakeSymbolicLinks(List<(string Link, string Target)> symbolicLinks, string unpackDirectory)
        {
            foreach (var symbolicLink in symbolicLinks)
            {
                var linkPath = Path.Combine(unpackDirectory, symbolicLink.Link[2..]);
                var info = new FileInfo(linkPath);
                info.CreateAsSymbolicLink(symbolicLink.Target);
            }
        }

        void ExtractTarball(
            string tarballPath,
            string destDir,
            bool flatten = false,
            Func<string, bool>? fileFilter = null,
            List<(string Link, string Target)>? symbolicLinks = null)
        {
            if (ConsoleConfig.IsLoud)
            {
                StdOut.DarkBlueLine($"un-taring '{tarballPath}'");
            }

            using var source = File.OpenRead(tarballPath);
            using var reader = new TarReader(source, true);

            while (reader.GetNextEntry() is { } entry)
            {
                if (entry.EntryType != TarEntryType.RegularFile &&
                    entry.EntryType != TarEntryType.V7RegularFile)
                {
                    if (entry.EntryType == TarEntryType.HardLink)
                    {
                        if (ConsoleConfig.IsLoud)
                        {
                            StdOut.YellowLine($"Encountered a hard link: {entry.Name}, ignoring.");
                        }
                        //Console.WriteLine($"Hard link found: {entry.Name} -> {entry.LinkName}");
                    }
                    else if (entry.EntryType == TarEntryType.SymbolicLink)
                    {
                        if (entry.Name.StartsWith("./opt"))
                        {
                            if (ConsoleConfig.IsLoud)
                            {
                                StdOut.DarkBlueLine($"Symbolic link: '{entry.Name}'.");
                            }
                            symbolicLinks?.Add((entry.Name, entry.LinkName));
                        }

                        //Console.WriteLine($"Symbolic link found: {entry.Name} -> {entry.LinkName}");
                    }
                    continue;
                }

                if (fileFilter != null && !fileFilter(entry.Name))
                {
                    continue;
                }

                var destFile = flatten
                    ? Path.Combine(destDir, Path.GetFileName(entry.Name))
                    : Path.Combine(destDir, entry.Name);

                if (!flatten)
                {
                    var destFileDir = Path.GetDirectoryName(destFile);
                    Directory.CreateDirectory(destFileDir!);
                }

                entry.ExtractToFile(destFile, true);
            }
        }

        string DecompressTarball(string sourcePath, string unpackDirectory)
        {
            if (ConsoleConfig.IsLoud)
            {
                StdOut.DarkBlueLine($"decompressing '{sourcePath}'");
            }

            var destPath = Path.Combine(unpackDirectory, Path.GetFileNameWithoutExtension(sourcePath));
            using var sourceFile = File.OpenRead(sourcePath);
            using var gzip = new GZipStream(sourceFile, CompressionMode.Decompress);
            using var destFile = File.OpenWrite(destPath);

            gzip.CopyTo(destFile);

            destFile.Flush();
            return destPath;
        }

        void UnpackPkg()
        {
            if (ConsoleConfig.IsLoud)
            {
                StdOut.DarkBlueLine($"Unpacking: '{asset.LocalPath}'.");
            }

            _pkgUnpacker.Unpack(asset.LocalPath, asset.UnpackedBaseDirectory);
        }

        void UnpackZipArchive(string unpackDirectory)
        {
            if (ConsoleConfig.IsLoud)
            {
                StdOut.DarkBlueLine($"un-zipping '{asset.LocalPath}'");
            }
            ZipFile.ExtractToDirectory(asset.LocalPath, unpackDirectory);
        }
    }
}
