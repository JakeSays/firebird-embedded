using Std.FirebirdEmbedded.Tools.Assets;
using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools;

internal sealed class PackageStructureBuilder
{
    public const string LicenseFileName = "LICENSES.zip";
    public const string IconFileName = "icon.png";
    private readonly byte[] _licenseFileBytes = ManifestResourceManager.ReadBinaryResource(LicenseFileName)
        ?? throw new InvalidOperationException("License file resource not found.");
    private readonly byte[] _iconBytes = ManifestResourceManager.ReadBinaryResource(IconFileName)
        ?? throw new InvalidOperationException("Icon file resource not found.");

    private readonly Configuration _config;

    public PackageStructureBuilder(Configuration config)
    {
        _config = config;
        Directory.CreateDirectory(config.PackageWorkingDirectory);
    }

    public bool BuildStructures(FirebirdRelease release)
    {
        if (LogConfig.IsNormal)
        {
            StdOut.NormalLine($"Building structures for release '{release.Name}'");
        }

        try
        {
            foreach (var asset in release.Assets)
            {
                CreatePackageStructure(release.Version, asset);
            }

            CreateConsolidatedPackageStructure(release.LinuxPackage);
            CreateConsolidatedPackageStructure(release.WindowsPackage);

            if (!_config.TemplatesOnly)
            {
                CopyLicenses(release.LinuxPackage, release.LinuxPackage.NugetFiles, release.LinuxPackage.PackageRootDirectory);
                CopyLicenses(release.WindowsPackage, release.WindowsPackage.NugetFiles, release.WindowsPackage.PackageRootDirectory);
                CopyIcon(release.LinuxPackage.NugetFiles, release.LinuxPackage.PackageRootDirectory);
                CopyIcon(release.WindowsPackage.NugetFiles, release.WindowsPackage.PackageRootDirectory);
            }

            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Building structures for release '{release.Name}' failed: {ex.Message}");
            return false;
        }
    }

    private void CreateConsolidatedPackageStructure(ConsolidatedPackageDetails details)
    {
        if (LogConfig.IsLoud)
        {
            StdOut.DarkBlueLine($"Building structure for consolidated package '{details.PackageId}'");
        }

        IoHelpers.RecreateDirectory(details.PackageRootDirectory);
    }

    private void CreatePackageStructure(FirebirdVersion version, FirebirdAsset asset)
    {
        if (LogConfig.IsLoud)
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
            case FirebirdVersion.V3 when asset.Platform == Platform.Linux:
                CreateV3LinuxStructure(asset, asset.PackageRootDirectory);
                break;
            case FirebirdVersion.V3 when asset.Platform == Platform.Windows:
                CreateV3WindowsStructure(asset, asset.PackageRootDirectory);
                break;
            case FirebirdVersion.V4 when asset.Platform == Platform.Linux:
                CreateV4LinuxStructure(asset, asset.PackageRootDirectory);
                break;
            case FirebirdVersion.V4 when asset.Platform == Platform.Windows:
                CreateV4WindowsStructure(asset, asset.PackageRootDirectory);
                break;
            case FirebirdVersion.V5 when asset.Platform == Platform.Linux:
                CreateV5LinuxStructure(asset, asset.PackageRootDirectory);
                break;
            case FirebirdVersion.V5 when asset.Platform == Platform.Windows:
                CreateV5WindowsStructure(asset, asset.PackageRootDirectory);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(version), version, null);
        }
    }

    private void CopyFile(FirebirdAsset asset, string fileName, string destRootDirectory)
    {
        if (LogConfig.IsNaggy)
        {
            StdOut.DarkGreenLine($"Copying file '{fileName}' to '{destRootDirectory}'");
        }

        var sourcePath = Path.Combine(asset.UnpackedDirectory, fileName);
        var destPath = Path.Combine(destRootDirectory, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(sourcePath, destPath, true);

        asset.NugetFiles.Add(new NugetFile(NugetDestination.Runtime, fileName));
    }

    private void CopyLicenses(IPackageDetails details, List<NugetFile> nugetFiles, string destRootDirectory)
    {
        var destPath = Path.Combine(destRootDirectory, details.LicensesFileName);

        if (LogConfig.IsNaggy)
        {
            StdOut.DarkGreenLine($"Writing Licenses file to '{destPath}'");
        }

        File.WriteAllBytes(destPath, _licenseFileBytes);
        nugetFiles.Add(new NugetFile(NugetDestination.Content, details.LicensesFileName));
    }

    private void CopyIcon(List<NugetFile> nugetFiles, string destRootDirectory)
    {
        var destPath = Path.Combine(destRootDirectory, IconFileName);

        if (LogConfig.IsNaggy)
        {
            StdOut.DarkGreenLine($"Writing icon file to '{destPath}'");
        }

        File.WriteAllBytes(destPath, _iconBytes);
        nugetFiles.Add(new NugetFile(NugetDestination.Content, IconFileName));
    }

    private void CopyTzData(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "tzdata/metaZones.res", destRootDirectory);
        CopyFile(asset, "tzdata/timezoneTypes.res", destRootDirectory);
        CopyFile(asset, "tzdata/windowsZones.res", destRootDirectory);
        CopyFile(asset, "tzdata/zoneinfo64.res", destRootDirectory);
    }

    private void CreateV3LinuxStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libib_util.so", destRootDirectory);
        CopyFile(asset, "lib/libfbclient.so.2", destRootDirectory);
        CopyFile(asset, "plugins/libEngine12.so", destRootDirectory);
    }

    private void CreateV4LinuxStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libib_util.so", destRootDirectory);
        CopyFile(asset, "lib/libfbclient.so.2", destRootDirectory);
        CopyFile(asset, "lib/libtomcrypt.so.1", destRootDirectory);
        CopyFile(asset, "plugins/libEngine13.so", destRootDirectory);

        CopyTzData(asset, destRootDirectory);
    }

    private void CreateV5LinuxStructure(FirebirdAsset asset, string destRootDirectory)
    {
        CopyFile(asset, "lib/libib_util.so", destRootDirectory);
        CopyFile(asset, "lib/libfbclient.so.2", destRootDirectory);
        CopyFile(asset, "lib/libtomcrypt.so.1", destRootDirectory);
        CopyFile(asset, "plugins/libEngine13.so", destRootDirectory);

        CopyTzData(asset, destRootDirectory);
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
        CopyFile(asset, "plugins/engine13.dll", destRootDirectory);

        CopyFile(asset, "tzdata/ids.dat", destRootDirectory);

        CopyTzData(asset, destRootDirectory);
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

        if (asset.Architecture == Architecture.X64)
        {
            CopyFile(asset, "vcruntime140_1.dll", destRootDirectory);
        }

        CopyFile(asset, "zlib1.dll", destRootDirectory);
        CopyFile(asset, "plugins/engine13.dll", destRootDirectory);

        CopyFile(asset, "tzdata/ids.dat", destRootDirectory);

        CopyTzData(asset, destRootDirectory);
    }
}
