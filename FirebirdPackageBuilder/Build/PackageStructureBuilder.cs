using Std.FirebirdEmbedded.Tools.Assets;
using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools.Build;

internal sealed class PackageStructureBuilder
{
    public const string LicenseFileName = "LICENSES.zip";
    public const string IconFileName = "icon.png";
    private readonly byte[] _licenseFileBytes = ManifestResourceManager.ReadBinaryResource(LicenseFileName)
        ?? throw new InvalidOperationException("License file resource not found.");
    private readonly byte[] _iconBytes = ManifestResourceManager.ReadBinaryResource(IconFileName)
        ?? throw new InvalidOperationException("Icon file resource not found.");

    private readonly BuildConfiguration _config;

    public PackageStructureBuilder(BuildConfiguration config)
    {
        _config = config;
        Directory.CreateDirectory(config.PackageWorkingDirectory);
    }

    public bool BuildStructures(FirebirdRelease release)
    {
        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine($"Building structures for release '{release.Name}'");
        }

        try
        {
            CreateMetaPackageStructure(release);

            foreach (var asset in release.Assets)
            {
                CreatePackageStructure(release.Product, asset);
            }

            CreateConsolidatedPackageStructure(release.LinuxPackage);
            CreateConsolidatedPackageStructure(release.WindowsPackage);
            if (release.Product == ProductId.V5)
            {
                CreateConsolidatedPackageStructure(release.MacOsPackage);
            }

            if (!_config.TemplatesOnly)
            {
                CopyLicenses(release.LinuxPackage, release.LinuxPackage.NugetFiles, release.LinuxPackage.PackageRootDirectory);
                CopyLicenses(release.WindowsPackage, release.WindowsPackage.NugetFiles, release.WindowsPackage.PackageRootDirectory);
                CopyIcon(release.LinuxPackage.NugetFiles, release.LinuxPackage.PackageRootDirectory);
                CopyIcon(release.WindowsPackage.NugetFiles, release.WindowsPackage.PackageRootDirectory);

                if (release.Product == ProductId.V5)
                {
                    CopyLicenses(
                        release.MacOsPackage, release.MacOsPackage.NugetFiles,
                        release.MacOsPackage.PackageRootDirectory);
                    CopyIcon(release.MacOsPackage.NugetFiles, release.MacOsPackage.PackageRootDirectory);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Building structures for release '{release.Name}' failed: {ex.Message}");
            return false;
        }
    }

    private void CreateMetaPackageStructure(FirebirdRelease release)
    {
        if (ConsoleConfig.IsLoud)
        {
            StdOut.DarkBlueLine($"Building structure for meta package '{release.PackageId}'");
        }

        IoHelpers.RecreateDirectory(release.PackageRootDirectory);

        if (_config.TemplatesOnly)
        {
            return;
        }

        CopyLicenses(release, release.NugetFiles, release.PackageRootDirectory);
        CopyIcon(release.NugetFiles, release.PackageRootDirectory);
    }

    private void CreateConsolidatedPackageStructure(ConsolidatedPackageDetails details)
    {
        if (ConsoleConfig.IsLoud)
        {
            StdOut.DarkBlueLine($"Building structure for consolidated package '{details.PackageId}'");
        }

        IoHelpers.RecreateDirectory(details.PackageRootDirectory);
    }

    private void CreatePackageStructure(ProductId version, FirebirdAsset asset)
    {
        if (ConsoleConfig.IsLoud)
        {
            StdOut.DarkBlueLine($"Building structure for package '{asset.PackageId}'");
        }

        IoHelpers.RecreateDirectory(asset.PackageRootDirectory);

        if (_config.TemplatesOnly)
        {
            return;
        }

        CopyLicenses(asset, asset.NugetFiles, asset.PackageRootDirectory);
        CopyIcon(asset.NugetFiles, asset.PackageRootDirectory);

        switch (version)
        {
            case ProductId.V3 when asset.Platform == Platform.Linux:
                CreateV3LinuxStructure(asset, asset.PackageRootDirectory);
                break;
            case ProductId.V3 when asset.Platform == Platform.Windows:
                CreateV3WindowsStructure(asset, asset.PackageRootDirectory);
                break;
            case ProductId.V4 when asset.Platform == Platform.Linux:
                CreateV4LinuxStructure(asset, asset.PackageRootDirectory);
                break;
            case ProductId.V4 when asset.Platform == Platform.Windows:
                CreateV4WindowsStructure(asset, asset.PackageRootDirectory);
                break;
            case ProductId.V5 when asset.Platform == Platform.Linux:
                CreateV5LinuxStructure(asset, asset.PackageRootDirectory);
                break;
            case ProductId.V5 when asset.Platform == Platform.Windows:
                CreateV5WindowsStructure(asset, asset.PackageRootDirectory);
                break;
            case ProductId.V5 when asset.Platform == Platform.Osx:
                CreateV5MacOsStructure(asset, asset.PackageRootDirectory);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(version), version, null);
        }
    }

    private void CopyFile(FirebirdAsset asset, string fileName, string destRootDirectory, string? fileNameOverride = null)
    {
        if (ConsoleConfig.IsNaggy)
        {
            StdOut.DarkGreenLine($"Copying file '{fileName}' to '{destRootDirectory}'{(fileNameOverride != null ? $"/{fileNameOverride}" : "")}");
        }

        var sourcePath = Path.Combine(asset.UnpackedDirectory, fileName);
        var destPath = Path.Combine(destRootDirectory, fileNameOverride ?? fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(sourcePath, destPath, true);

        asset.NugetFiles.Add(new NugetFile(NugetDestination.Runtime, fileNameOverride ?? fileName));
    }

    private void CopyLicenses(IPackageDetails details, List<NugetFile> nugetFiles, string destRootDirectory)
    {
        var destPath = Path.Combine(destRootDirectory, details.LicensesFileName);

        if (ConsoleConfig.IsNaggy)
        {
            StdOut.DarkGreenLine($"Writing Licenses file to '{destPath}'");
        }

        File.WriteAllBytes(destPath, _licenseFileBytes);
        nugetFiles.Add(new NugetFile(NugetDestination.Content, details.LicensesFileName));
    }

    private void CopyIcon(List<NugetFile> nugetFiles, string destRootDirectory)
    {
        var destPath = Path.Combine(destRootDirectory, IconFileName);

        if (ConsoleConfig.IsNaggy)
        {
            StdOut.DarkGreenLine($"Writing icon file to '{destPath}'");
        }

        File.WriteAllBytes(destPath, _iconBytes);
        nugetFiles.Add(new NugetFile(NugetDestination.Content, IconFileName));
    }

    private void CopyTzData(FirebirdAsset asset, string destRootDirectory, bool includeIds = false)
    {
        CopyFile(asset, "tzdata/metaZones.res", destRootDirectory);
        CopyFile(asset, "tzdata/timezoneTypes.res", destRootDirectory);
        CopyFile(asset, "tzdata/windowsZones.res", destRootDirectory);
        CopyFile(asset, "tzdata/zoneinfo64.res", destRootDirectory);
        if (includeIds)
        {
            CopyFile(asset, "tzdata/ids.dat", destRootDirectory);
        }
    }

    private void CreateV3LinuxStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libib_util.so", destRootDirectory);
        CopyFile(asset, "lib/libfbclient.so.2", destRootDirectory);
        CopyFile(asset, "plugins/libEngine12.so", destRootDirectory);
        CopyFile(asset, "intl/fbintl", destRootDirectory);
        CopyFile(asset, "intl/fbintl.conf", destRootDirectory);
    }

    private void CreateV4LinuxStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libib_util.so", destRootDirectory);
        CopyFile(asset, "lib/libfbclient.so.2", destRootDirectory);
        CopyFile(asset, "lib/libtomcrypt.so.1", destRootDirectory);
        CopyFile(asset, "plugins/libEngine13.so", destRootDirectory);
        CopyFile(asset, "intl/fbintl", destRootDirectory);
        CopyFile(asset, "intl/fbintl.conf", destRootDirectory);

        CopyTzData(asset, destRootDirectory);
    }

    private void CreateV5LinuxStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libib_util.so", destRootDirectory);
        CopyFile(asset, "lib/libfbclient.so.2", destRootDirectory);
        CopyFile(asset, "lib/libtomcrypt.so.1", destRootDirectory);
        CopyFile(asset, "plugins/libEngine13.so", destRootDirectory);
        CopyFile(asset, "intl/fbintl", destRootDirectory);
        CopyFile(asset, "intl/fbintl.conf", destRootDirectory);
        CopyTzData(asset, destRootDirectory);
    }

    private void CreateV5MacOsStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libfbclient.dylib", destRootDirectory);
        CopyFile(asset, "lib/libib_util.dylib", destRootDirectory);
        CopyFile(asset, "lib/libicudata.71.1.dylib", destRootDirectory, "lib/libicudata.71.dylib");
        CopyFile(asset, "lib/libicui18n.71.1.dylib", destRootDirectory, "lib/libicui18n.71.dylib");
        CopyFile(asset, "lib/libicuuc.71.1.dylib", destRootDirectory, "lib/libicuuc.71.dylib");
        CopyFile(asset, "lib/libtomcrypt.dylib", destRootDirectory);
        CopyFile(asset, "lib/libtommath.dylib", destRootDirectory);
        CopyFile(asset, "plugins/libEngine13.dylib", destRootDirectory);

        CopyTzData(asset, destRootDirectory, includeIds: true);
    }

    private void CreateV3WindowsStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "fbclient.dll", destRootDirectory);
        CopyFile(asset, "ib_util.dll", destRootDirectory);
        CopyFile(asset, "icudt52.dll", destRootDirectory);
        CopyFile(asset, "icudt52l.dat", destRootDirectory);
        CopyFile(asset, "icuin52.dll", destRootDirectory);
        CopyFile(asset, "icuuc52.dll", destRootDirectory);
        CopyFile(asset, "msvcp100.dll", destRootDirectory);
        CopyFile(asset, "msvcr100.dll", destRootDirectory);
        CopyFile(asset, "zlib1.dll", destRootDirectory);
        CopyFile(asset, "intl/fbintl.dll", destRootDirectory);
        CopyFile(asset, "intl/fbintl.conf", destRootDirectory);
        CopyFile(asset, "plugins/engine12.dll", destRootDirectory);
    }

    private void CreateV4WindowsStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "fbclient.dll", destRootDirectory);
        CopyFile(asset, "ib_util.dll", destRootDirectory);
        CopyFile(asset, "icudt63.dll", destRootDirectory);
        CopyFile(asset, "icudt63l.dat", destRootDirectory);
        CopyFile(asset, "icuin63.dll", destRootDirectory);
        CopyFile(asset, "icuuc63.dll", destRootDirectory);
        CopyFile(asset, "msvcp140.dll", destRootDirectory);
        CopyFile(asset, "vcruntime140.dll", destRootDirectory);
        CopyFile(asset, "zlib1.dll", destRootDirectory);
        CopyFile(asset, "intl/fbintl.dll", destRootDirectory);
        CopyFile(asset, "intl/fbintl.conf", destRootDirectory);
        CopyFile(asset, "plugins/engine13.dll", destRootDirectory);

        CopyTzData(asset, destRootDirectory, true);
    }

    private void CreateV5WindowsStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "fbclient.dll", destRootDirectory);
        CopyFile(asset, "ib_util.dll", destRootDirectory);
        CopyFile(asset, "icudt63.dll", destRootDirectory);
        CopyFile(asset, "icudt63l.dat", destRootDirectory);
        CopyFile(asset, "icuin63.dll", destRootDirectory);
        CopyFile(asset, "icuuc63.dll", destRootDirectory);
        CopyFile(asset, "msvcp140.dll", destRootDirectory);
        CopyFile(asset, "vcruntime140.dll", destRootDirectory);
        CopyFile(asset, "intl/fbintl.dll", destRootDirectory);
        CopyFile(asset, "intl/fbintl.conf", destRootDirectory);

        if (asset.Architecture == Architecture.X64)
        {
            CopyFile(asset, "vcruntime140_1.dll", destRootDirectory);
        }

        CopyFile(asset, "zlib1.dll", destRootDirectory);
        CopyFile(asset, "plugins/engine13.dll", destRootDirectory);

        CopyTzData(asset, destRootDirectory, true);
    }
}
