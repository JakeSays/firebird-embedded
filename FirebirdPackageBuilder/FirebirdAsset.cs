namespace Std.FirebirdEmbedded.Tools;

internal sealed class FirebirdAsset : IPackageDetails
{
    public FirebirdRelease Release { get; }
    public Architecture[] Architectures { get; }

    public string PackageId { get; }
    public string NormalizedName { get; }

    public string LocalPath { get; }
    public string UnpackedDirectory { get; }
    public string PackageRootDirectory { get; }

    public List<NugetFile> NugetFiles { get; } = [];

    public string PropertySuffix => $"{Release.Version}{Platform}{Architecture}";
    //public string Rid => $"{Platform.RidPrefix()}-{Architecture.RidSuffix(Platform)}";
    public ReleaseVersion Version { get; }
    public string Name { get; }
    public string FileName { get; }
    public Rid Rid { get; }

    //TODO: replace with Rid
    public Platform Platform { get; }
    //TODO: replace with Rid
    public Architecture Architecture { get; }
    public ContentType ContentType { get; }
    public string DownloadUrl { get; }
    public uint DownloadSize { get; init; }
    public bool Uploaded { get; init; }

    public FirebirdAsset(ReleaseVersion version,
        string name,
        string fileName,
        Platform platform,
        Architecture architecture,
        ContentType contentType,
        string downloadUrl,
        uint downloadSize,
        bool uploaded,
        FirebirdRelease release,
        Configuration config)
    {
        Version = version;
        Name = name;
        FileName = fileName;
        Platform = platform;
        Architecture = architecture;
        ContentType = contentType;
        DownloadUrl = downloadUrl;
        DownloadSize = downloadSize;
        Uploaded = uploaded;
        Release = release;
        Rid = new Rid(platform, architecture);
        Architectures = [architecture];

        release.AddAsset(this);

        PackageId = $"{config.DefaultPackagePrefix}.Embedded.{Release.Version}.NativeAssets.{Platform}.{Architecture}";
        if (config.PackagePrefix != null)
        {
            PackageId = $"{config.PackagePrefix}.{PackageId}";
        }

        NormalizedName = $"Firebird-{version}-{platform.LowerCase()}-{architecture.Name()}";

        LocalPath = Path.Combine(config.DownloadDirectory, FileName);
        PackageRootDirectory =
            Path.Combine(config.PackageWorkingDirectory, release.Version.ToString(), NormalizedName);
        UnpackedDirectory =
            Path.Combine(config.UnpackWorkingDirectory, release.Version.ToString(), NormalizedName);
        if (Platform == Platform.Linux)
        {
            UnpackedDirectory += "/opt/firebird";
        }
    }
}
