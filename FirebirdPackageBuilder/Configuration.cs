using NuGet.Versioning;
using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools;

internal enum BuildType
{
    Normal,
    Rebuild,
    Force
}

internal static class LogConfig
{
    public static Verbosity Verbosity { get; private set; }

    public static void Initialize(Verbosity verbosity)
    {
        Verbosity = verbosity;
        NugetLogger.InitializeDefault(verbosity);
    }

    public static bool IsSilent => Verbosity == Verbosity.Silent;
    public static bool IsNormal => Verbosity >= Verbosity.Normal;
    public static bool IsLoud => Verbosity == Verbosity.Loud;
    public static bool IsNaggy => Verbosity == Verbosity.Nagging;
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

    public VersionRange AssetManagerVersion { get; }
    public ReleaseVersion InitialPackageVersion { get; }
    public string AssetManagerPackageName { get; }

    public BuildType BuildType { get; }
    public List<FirebirdVersion> VersionsToBuild { get; }
    public DateTimeOffset BuildDate { get; }
    public bool TemplatesOnly { get; set; }

    public static void Initialize(
        string? packagePrefix,
        Verbosity verbosity,
        string workspaceDirectory,
        string packageDirectory,
        string metadataFilePath,
        List<FirebirdVersion> versionsToBuild,
        bool forceDownload,
        BuildType buildType,
        VersionRange assetManagerVersion,
        ReleaseVersion initialPackageVersion)
    {
        Instance = new Configuration(
            packagePrefix,
            verbosity,
            workspaceDirectory,
            packageDirectory,
            metadataFilePath,
            versionsToBuild,
            forceDownload,
            buildType,
            assetManagerVersion,
            initialPackageVersion);
    }

    private Configuration(
        string? packagePrefix,
        Verbosity verbosity,
        string workspaceDirectory,
        string packageDirectory,
        string metadataFilePath,
        List<FirebirdVersion> versionsToBuild,
        bool forceDownload,
        BuildType buildType,
        VersionRange assetManagerVersion,
        ReleaseVersion initialPackageVersion)
    {
        LogConfig.Initialize(verbosity);

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
        AssetManagerVersion = assetManagerVersion; //"[1.0.2,2.0.0)";
        InitialPackageVersion = initialPackageVersion;

        AssetManagerPackageName = $"{DefaultPackagePrefix}.Embedded.NativeAssetManager";
        if (packagePrefix != null)
        {
            AssetManagerPackageName = $"{packagePrefix}.{AssetManagerPackageName}";
        }

        BuildDate = DateTimeOffset.Now;;
    }
}
