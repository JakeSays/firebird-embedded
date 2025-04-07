namespace Std.FirebirdEmbedded.Tools;

internal record PackageVersions(string V3Version, string V4Version, string V5Version);

internal enum BuildType
{
    Normal,
    Rebuild,
    Force
}

internal sealed class Configuration
{
    public static Configuration Instance { get; private set; } = null!;

    public Verbosity Verbosity { get; }
    public string WorkspaceDirectoryRoot { get; }
    public string UnpackWorkingDirectory { get; }
    public string PackageWorkingDirectory { get; }
    public string DownloadDirectory { get; }
    public string PackageOutputDirectory { get; }
    public string MetadataFilePath { get; }
    public bool ForceDownload { get; }
    public string? PackagePrefix { get; }
    public string DefaultPackagePrefix => "FirebirdDb";

    public string AssetManagerVersion { get; }
    public string AssetManagerPackageName { get; }

    public BuildType BuildType { get; }
    public List<FirebirdVersion> VersionsToBuild { get; }
    public DateTimeOffset BuildDate { get; }

    public static bool IsSilent => Instance.Verbosity == Verbosity.Silent;
    public static bool IsNormal => Instance.Verbosity >= Verbosity.Normal;
    public static bool IsLoud => Instance.Verbosity == Verbosity.Loud;
    public static bool IsNaggy => Instance.Verbosity == Verbosity.Nagging;

    public static void Initialize(
        string? packagePrefix,
        Verbosity verbosity,
        string workspaceDirectory,
        string packageDirectory,
        string metadataFilePath,
        List<FirebirdVersion> versionsToBuild,
        bool forceDownload,
        BuildType buildType)
    {
        Instance = new Configuration(
            packagePrefix,
            verbosity,
            workspaceDirectory,
            packageDirectory,
            metadataFilePath,
            versionsToBuild,
            forceDownload,
            buildType);
    }

    private Configuration(
        string? packagePrefix,
        Verbosity verbosity,
        string workspaceDirectory,
        string packageDirectory,
        string metadataFilePath,
        List<FirebirdVersion> versionsToBuild,
        bool forceDownload,
        BuildType buildType)
    {
        PackagePrefix = packagePrefix;
        Verbosity = verbosity;
        WorkspaceDirectoryRoot = workspaceDirectory;
        PackageOutputDirectory = packageDirectory;
        MetadataFilePath = metadataFilePath;
        ForceDownload = forceDownload;
        BuildType = buildType;
        VersionsToBuild = versionsToBuild.Count > 0
            ? versionsToBuild.Distinct().OrderBy(v => v).ToList()
            : [FirebirdVersion.V3, FirebirdVersion.V4, FirebirdVersion.V5];

        UnpackWorkingDirectory = Path.Combine(WorkspaceDirectoryRoot, "unpacked");
        PackageWorkingDirectory = Path.Combine(WorkspaceDirectoryRoot, "structure");
        DownloadDirectory = Path.Combine(WorkspaceDirectoryRoot, "downloads");
        AssetManagerVersion = "1.0.1";

        AssetManagerPackageName = $"{DefaultPackagePrefix}.Embedded.NativeAssetManager";
        if (packagePrefix != null)
        {
            AssetManagerPackageName = $"{packagePrefix}.{AssetManagerPackageName}";
        }

        BuildDate = DateTimeOffset.Now;;
    }
}
