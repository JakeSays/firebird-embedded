namespace Std.FirebirdEmbedded.Tools;

internal class ConsolidatedPackageDetails : IPackageDetails
{
    public Rid Rid { get; }

    public FirebirdRelease Release { get; }
    public Architecture[] Architectures { get; }
    public Platform Platform { get; }
    public string PackageRootDirectory { get; private set; } = null!;
    public List<NugetFile> NugetFiles { get; } = [];

    public string PackageId { get; private set; } = null!;

    public ConsolidatedPackageDetails(FirebirdRelease release, Platform platform)
    {
        Rid = new Rid(platform);
        Release = release;
        Platform = platform;
        Architectures = release.Assets
            .Where(a => a.Rid.Platform == platform)
            .Select(a => a.Rid.Architecture)
            .ToArray();
    }

    public void Initialize(Configuration config)
    {
        PackageId = $"{config.DefaultPackagePrefix}.Embedded.{Release.Version}.NativeAssets.{Rid.DisplayName}";
        if (config.PackagePrefix != null)
        {
            PackageId = $"{config.PackagePrefix}.{PackageId}";
        }

        PackageRootDirectory =
            Path.Combine(config.PackageWorkingDirectory, Release.Version.ToString(), $"Firebird-{Release.ReleaseVersion}-{Rid.ToStringFull()}");
    }
}

internal class FirebirdRelease
{
    private readonly List<FirebirdAsset> _assets = [];

    public FirebirdRelease(FirebirdVersion version,
        string tag,
        string name,
        DateTimeOffset publishDate,
        string releaseNotes)
    {
        Version = version;
        Tag = tag;
        Name = name;
        PublishDate = publishDate;
        ReleaseNotes = releaseNotes;
        LinuxPackage = new ConsolidatedPackageDetails(this, Platform.Linux);
        WindowsPackage = new ConsolidatedPackageDetails(this, Platform.Windows);
    }

    public void Initialize(Configuration config)
    {
        LinuxPackage.Initialize(config);
        WindowsPackage.Initialize(config);
    }

    public FirebirdVersion Version { get; }
    public string Tag { get; }
    public string Name { get; }
    public DateTimeOffset PublishDate { get; }
    public string ReleaseNotes { get; }

    public ReleaseVersion ReleaseVersion { get; set; }

    public IReadOnlyList<FirebirdAsset> Assets => _assets;

    public ConsolidatedPackageDetails LinuxPackage { get; }
    public ConsolidatedPackageDetails WindowsPackage { get; }
    public void AddAsset(FirebirdAsset asset)
    {
        _assets.Add(asset);
    }

    public string VersionLong =>
        Version switch
        {
            FirebirdVersion.V3 => "version 3",
            FirebirdVersion.V4 => "version 4",
            FirebirdVersion.V5 => "version 5",
            _ => throw new ArgumentOutOfRangeException()
        };
}
