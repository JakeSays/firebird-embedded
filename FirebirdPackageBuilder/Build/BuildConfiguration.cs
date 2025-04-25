using NuGet.Versioning;
using Std.FirebirdEmbedded.Tools.Common;


namespace Std.FirebirdEmbedded.Tools.Build;

internal sealed class BuildConfiguration : ToolConfiguration
{
    public string WorkspaceDirectoryRoot { get; }
    public string UnpackWorkingDirectory { get; }
    public string PackageWorkingDirectory { get; }
    public string DownloadDirectory { get; }
    public bool ForceDownload { get; }
    public string? PackagePrefix { get; }
    public string DefaultPackagePrefix => "FirebirdDb";

    public ReleaseVersion AssetManagerVersion { get; }
    public ReleaseVersion EffectiveAssetManagerVersion { get; set; }
    public ReleaseVersion TargetPackageVersion { get; }
    public bool ForceBuildAssetManager { get; }
    public string AssetManagerPackageName { get; }

    public BuildType BuildType { get; }
    public List<ProductId> VersionsToBuild { get; }
    public DateTimeOffset BuildDate { get; }
    public bool TemplatesOnly { get; set; }

    public BuildConfiguration(
        string? repositoryRoot,
        string? packagePrefix,
        string? workspaceDirectory,
        string? packageDirectory,
        string? metadataFilePath,
        List<ProductId> versionsToBuild,
        bool forceDownload,
        BuildType buildType,
        ReleaseVersion assetManagerVersion,
        ReleaseVersion targetPackageVersion,
        bool forceBuildAssetManager)
        : base(repositoryRoot, MakePackageDirectory(packageDirectory, workspaceDirectory), metadataFilePath)
    {
        PackagePrefix = packagePrefix;
        WorkspaceDirectoryRoot = MakePath(workspaceDirectory, "workspace");
        ForceDownload = forceDownload;
        BuildType = buildType;
        VersionsToBuild = versionsToBuild.Count > 0
            ? versionsToBuild.Distinct().OrderBy(v => v).ToList()
            : [ProductId.V3, ProductId.V4, ProductId.V5];

        UnpackWorkingDirectory = Path.Combine(WorkspaceDirectoryRoot, "unpacked");
        PackageWorkingDirectory = Path.Combine(WorkspaceDirectoryRoot, "structure");
        DownloadDirectory = Path.Combine(WorkspaceDirectoryRoot, "downloads");
        AssetManagerVersion = assetManagerVersion;
        TargetPackageVersion = targetPackageVersion;
        ForceBuildAssetManager = forceBuildAssetManager;

        AssetManagerPackageName = $"{DefaultPackagePrefix}.Embedded.NativeAssetManager";
        if (packagePrefix != null)
        {
            AssetManagerPackageName = $"{packagePrefix}.{AssetManagerPackageName}";
        }

        BuildDate = DateTimeOffset.Now;
    }

    private static string MakePackageDirectory(string? packageDirectory, string? workspaceDirectory)
    {
        workspaceDirectory ??= "workspace";
        if (packageDirectory == null)
        {
            return Path.Combine(workspaceDirectory, "output");
        }

        if (Path.IsPathRooted(packageDirectory))
        {
            return packageDirectory;
        }

        return packageDirectory.StartsWith(workspaceDirectory)
            ? packageDirectory
            : Path.Combine(workspaceDirectory, packageDirectory);
    }
}
