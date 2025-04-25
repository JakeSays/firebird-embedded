namespace Std.FirebirdEmbedded.Tools.Build;

internal class ConsolidatedPackageDetails : IPackageDetails
{
    public Rid Rid { get; }

    public FirebirdRelease Release { get; }
    public Architecture[] Architectures { get; private set; } = null!;
    public Platform Platform { get; }
    public string PackageRootDirectory { get; private set; } = null!;
    public List<NugetFile> NugetFiles { get; } = [];

    public string PackageId { get; private set; } = null!;
    public string LicensesFileName { get; }

    public ConsolidatedPackageDetails(FirebirdRelease release, Platform platform)
    {
        Rid = new Rid(platform);
        Release = release;
        Platform = platform;
        LicensesFileName = $"LICENSES{release.Product}.zip";
    }

    public void Initialize(BuildConfiguration config)
    {
        PackageId = $"{config.DefaultPackagePrefix}.Embedded.{Release.Product}.NativeAssets.{Rid.DisplayName}";
        if (config.PackagePrefix != null)
        {
            PackageId = $"{config.PackagePrefix}.{PackageId}";
        }

        PackageRootDirectory =
            Path.Combine(config.PackageWorkingDirectory, Release.Product.ToString(), $"Firebird-{Release.ReleaseVersion}-{Rid.ToStringFull()}");

        Architectures = Release.Assets
            .Where(a => a.Rid.Platform == Platform)
            .Select(a => a.Rid.Architecture)
            .Order()
            .ToArray();
    }
}
