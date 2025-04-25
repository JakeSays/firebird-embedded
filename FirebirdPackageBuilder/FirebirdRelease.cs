using Std.FirebirdEmbedded.Tools.Build;


namespace Std.FirebirdEmbedded.Tools;

internal class FirebirdRelease : IPackageDetails
{
    private readonly List<FirebirdAsset> _assets = [];

    public FirebirdRelease(ProductId product,
        string tag,
        string name,
        DateTimeOffset publishDate,
        string releaseNotes)
    {
        Product = product;
        Tag = tag;
        Name = name;
        PublishDate = publishDate;
        ReleaseNotes = releaseNotes;
        LinuxPackage = new ConsolidatedPackageDetails(this, Platform.Linux);
        WindowsPackage = new ConsolidatedPackageDetails(this, Platform.Windows);
        MacOsPackage = new ConsolidatedPackageDetails(this, Platform.Osx);
    }

    public void Initialize(BuildConfiguration config)
    {
        PackageId = $"{config.DefaultPackagePrefix}.Embedded.{Release.Product}.NativeAssets";
        if (config.PackagePrefix != null)
        {
            PackageId = $"{config.PackagePrefix}.{PackageId}";
        }

        PackageRootDirectory =
            Path.Combine(config.PackageWorkingDirectory, Release.Product.ToString(), $"Firebird-{Release.ReleaseVersion}-{Rid.ToStringFull()}");

        LinuxPackage.Initialize(config);
        WindowsPackage.Initialize(config);
        MacOsPackage.Initialize(config);
    }

    public Rid Rid => new (Platform.All);
    public string PackageId { get; private set; } = "";
    public string PackageRootDirectory { get; private set; } = "";
    public FirebirdRelease Release => this;
    public string LicensesFileName => $"LICENSES{Product}.zip";
    public Architecture[] Architectures => [Architecture.All];

    public ProductId Product { get; }
    public string Tag { get; }
    public string Name { get; }
    public DateTimeOffset PublishDate { get; }
    public string ReleaseNotes { get; }

    public ReleaseVersion ReleaseVersion { get; set; }

    public IReadOnlyList<FirebirdAsset> Assets => _assets;

    public List<NugetFile> NugetFiles { get; } = [];

    public ConsolidatedPackageDetails LinuxPackage { get; }
    public ConsolidatedPackageDetails WindowsPackage { get; }
    public ConsolidatedPackageDetails MacOsPackage { get; }
    public void AddAsset(FirebirdAsset asset)
    {
        _assets.Add(asset);
    }

    public string VersionLong =>
        Product switch
        {
            ProductId.V3 => "version 3",
            ProductId.V4 => "version 4",
            ProductId.V5 => "version 5",
            ProductId.AssetManager => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
}
