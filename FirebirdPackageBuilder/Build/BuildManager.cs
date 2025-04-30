using Std.FirebirdEmbedded.Tools.Common;
using Std.FirebirdEmbedded.Tools.MetaData;


namespace Std.FirebirdEmbedded.Tools.Build;

internal sealed class BuildResults : ToolResult
{
    public override bool Success { get; }

    public BuildResults()
    {
        Success = false;
    }

    public BuildResults(bool success)
    {
        Success = success;
    }

    public static implicit operator BuildResults(bool success) => new (success);
}

internal class BuildManager : Tool<BuildConfiguration, bool, BuildResults>
{
    private readonly GithubReleaseManager _githubReleaseManager;
    private FirebirdReleases _firebirdReleases = null!;

    internal BuildManager(BuildConfiguration configuration)
        : base(configuration)
    {
        _githubReleaseManager = new GithubReleaseManager(configuration);
    }

    private protected override BuildResults Execute(bool generateTemplatesOnly)
    {
        Config.TemplatesOnly = generateTemplatesOnly;

        _firebirdReleases = _githubReleaseManager.GetLatestReleases();
        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine($"Current versions are: V3={_firebirdReleases.V3.ReleaseVersion}, V4={_firebirdReleases.V4.ReleaseVersion}, V5={_firebirdReleases.V5.ReleaseVersion}");
        }

        if (!CalculateAssetManagerVersion())
        {
            return false;
        }

        if (!generateTemplatesOnly && !BuildAssetManager())
        {
            return false;
        }

        if (!FilterReleases())
        {
            return true;
        }

        if (!DownloadAssets())
        {
            return false;
        }

        if (!generateTemplatesOnly && !UnPack())
        {
            return false;
        }

        if (!BuildStructures())
        {
            return false;
        }

        return BuildPackages();
    }

    private bool CalculateAssetManagerVersion()
    {
        if (Config.TargetPackageVersion &&
            Config.BuildType == BuildType.Force)
        {
            //a target version overrides if build is forced
            Config.EffectiveAssetManagerVersion = Config.TargetPackageVersion;
            return true;
        }

        if (Config.AssetManagerVersion)
        {
            //use explicit version set on command line
            Config.EffectiveAssetManagerVersion = Config.AssetManagerVersion;
            return true;
        }

        //use metadata current version if available
        if (Metadata.AssetManagerReleases.CurrentPackageVersion)
        {
            Config.EffectiveAssetManagerVersion = Config.BuildType switch
            {
                BuildType.Normal => Metadata.AssetManagerReleases.CurrentPackageVersion,
                BuildType.Rebuild => Metadata.AssetManagerReleases.CurrentPackageVersion.NextPatchVersion(),
                BuildType.Force => Metadata.AssetManagerReleases.CurrentPackageVersion,
                _ => throw new ArgumentOutOfRangeException()
            };
            return true;
        }

        //use the highest version adjusted for build type
        var highestRelease = Metadata.AssetManagerReleases.History
            .OrderByDescending(r => r.PackageVersion)
            .FirstOrDefault();
        if (highestRelease == null)
        {
            Config.EffectiveAssetManagerVersion = Config.TargetPackageVersion;
            if (Config.EffectiveAssetManagerVersion)
            {
                return true;
            }

            StdErr.RedLine("Cannot determine asset manager version");
            return false;
        }

        Config.EffectiveAssetManagerVersion = Config.BuildType switch
        {
            BuildType.Normal => highestRelease.PackageVersion,
            BuildType.Rebuild => highestRelease.PackageVersion.NextPatchVersion(),
            BuildType.Force => highestRelease.PackageVersion,
            _ => throw new ArgumentOutOfRangeException()
        };

        return true;
    }

    private bool BuildAssetManager()
    {
        Directory.CreateDirectory(Config.PackageOutputDirectory);

        var result = NativeAssetManagerBuilder.BuildPackage(Config.RepositoryRoot!, Config.EffectiveAssetManagerVersion);

        if (!result)
        {
            return false;
        }

        var highestBuild = Metadata.AssetManagerReleases.HighestRelease;

        if (Metadata.AssetManagerReleases.CurrentPackageVersion < Config.EffectiveAssetManagerVersion || highestBuild == null)
        {
            Metadata.AssetManagerReleases.CurrentPackageVersion = Config.EffectiveAssetManagerVersion;
            var release = new PackageRelease(Rid.All, Config.BuildDate, ProductId.AssetManager, Config.EffectiveAssetManagerVersion, null);
            Metadata.AssetManagerReleases.AddRelease(release);
        }
        else if (Config.BuildType == BuildType.Force)
        {
            var release = highestBuild.WithBuildDate(Config.BuildDate);
            Metadata.AssetManagerReleases.AddRelease(release);
        }

        return true;
    }

    private bool FilterReleases()
    {
        if (Config.BuildType == BuildType.Normal)
        {
            //for a normal build filter out product versions
            //which have current package releases.
            FilterBuild(_firebirdReleases.V3, Metadata.V3Releases);
            FilterBuild(_firebirdReleases.V4, Metadata.V4Releases);
            FilterBuild(_firebirdReleases.V5, Metadata.V5Releases);
        }

        if (Config.VersionsToBuild.Count == 0)
        {
            if (ConsoleConfig.IsNormal)
            {
                StdOut.NormalLine("All versions current, nothing to build.");
            }
            return false;
        }

        return true;

        void FilterBuild(FirebirdRelease release, PackageReleaseHistory product)
        {
            if (product.History.Count == 0)
            {
                //if there have been no releases for a particular product version
                //then don't filter it out.
                return;
            }

            if (product.CurrentAssetVersion &&
                release.ReleaseVersion != product.CurrentAssetVersion)
            {
                //there has not been a package released for the current product version
                //so don't filter it out
                return;
            }

            var highestRelease = product.History
                .OrderByDescending(r => r.FirebirdRelease)
                .ThenByDescending(r => r.PackageVersion)
                .ThenByDescending(r => r.BuildDate)
                .First();

            if (release.ReleaseVersion != highestRelease.FirebirdRelease)
            {
                //there has not been a package released for the current product version
                //so don't filter it out
                return;
            }

            //a package has been released for this product version so remove it from the build
            Config.VersionsToBuild.Remove(release.Product);

            if (ConsoleConfig.IsNormal)
            {
                StdOut.NormalLine($"Firebird {release.VersionLong} was released in package version {highestRelease.PackageVersion.ToString(VersionStyle.Nuget)} on {highestRelease.BuildDate:yyyy-MM-dd hh:mm:ss}, excluding from build.");
            }
        }
    }

    private bool BuildPackages()
    {
        if (Config.VersionsToBuild.Count == 0)
        {
            if (ConsoleConfig.IsNormal)
            {
                StdOut.NormalLine("All versions current, nothing to build.");
            }
            return true;
        }

        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine("Building nuget packages.");
        }

        var builder = new NugetPackageBuilder(Config, Metadata);
        var result = ExecuteForReleases(release => builder.BuildPackagesForRelease(release));

        return result;
    }

    private bool BuildStructures()
    {
        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine("Building package structures.");
        }

        var structurizer = new PackageStructureBuilder(Config);
        var result = ExecuteForReleases(release => structurizer.BuildStructures(release));

        return result;
    }

    private bool UnPack()
    {
        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine("Unpacking assets.");
        }

        var unPacker = new AssetUnPacker();
        var result = ExecuteForReleases(release => unPacker.UnpackRelease(release));

        return result;
    }

    private bool DownloadAssets()
    {
        var result = ExecuteForReleases(Download);

        return result;

        bool Download(FirebirdRelease release)
        {
            // ReSharper disable once AccessToDisposedClosure
            var downloadResult = _githubReleaseManager.DownloadAssets(release)
                .Result.All(r => r);
            return downloadResult;
        }
    }

    private bool ExecuteForReleases(Func<FirebirdRelease, bool> func)
    {
        foreach (var ver in Config.VersionsToBuild)
        {
            var success = ver switch
            {
                ProductId.V3 => func(_firebirdReleases.V3),
                ProductId.V4 => func(_firebirdReleases.V4),
                ProductId.V5 => func(_firebirdReleases.V5),
                ProductId.AssetManager => throw new NotSupportedException(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!success)
            {
                return false;
            }
        }
        return true;
    }

    public override void Dispose()
    {
        _githubReleaseManager.Dispose();
    }
}
