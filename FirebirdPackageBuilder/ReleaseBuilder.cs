using Std.FirebirdEmbedded.Tools.MetaData;


namespace Std.FirebirdEmbedded.Tools;

internal static class ReleaseBuilder
{
    public static bool DoIt(Configuration config, PackageMetadata metadata)
    {
        var releases = DownloadAssets(config);
        if (releases == null)
        {
            return false;
        }

        if (!UnPack(config, releases))
        {
            return false;
        }

        if (!BuildStructures(config, releases))
        {
            return false;
        }

        return BuildPackages(config, releases, metadata);
    }

    private static bool BuildPackages(Configuration config, FirebirdReleases releases, PackageMetadata metadata)
    {
        if (Configuration.IsNormal)
        {
            StdOut.NormalLine("Building nuget packages.");
        }

        if (config.BuildType == BuildType.Normal)
        {
            //for a normal build filter out product versions
            //which have current package releases.
            FilterBuild(releases.V3, metadata.V3Releases);
            FilterBuild(releases.V4, metadata.V4Releases);
            FilterBuild(releases.V5, metadata.V5Releases);
        }

        if (config.VersionsToBuild.Count == 0)
        {
            if (Configuration.IsNormal)
            {
                StdOut.NormalLine("All versions current, nothing to build.");
            }
            return true;
        }

        var builder = new NugetPackageBuilder(config, metadata);
        var result = ExecuteForReleases(config, releases, release => builder.BuildPackagesForRelease(release));

        return result;

        void FilterBuild(FirebirdRelease release, IReadOnlyList<PackageRelease> packages)
        {
            if (packages.Count == 0)
            {
                //if there have been no releases for a particular product version
                //then don't filter it out.
                return;
            }

            var highestRelease = packages
                .OrderByDescending(r => r.FirebirdRelease)
                .ThenByDescending(r => r.PackageVersion)
                .First();

            if (release.ReleaseVersion != highestRelease.FirebirdRelease)
            {
                //there has not been a package released for the current product version
                //so don't filter it out
                return;
            }

            //a package has been released for this product version so remove it from the build
            config.VersionsToBuild.Remove(release.Version);

            if (Configuration.IsNormal)
            {
                StdOut.NormalLine($"Firebird {release.VersionLong} was released in package version {highestRelease.PackageVersion.ToString(VersionStyle.Nuget)} on {highestRelease.ReleaseDate:yyyy-MM-dd hh:mm:ss}, excluding from build.");
            }
        }
    }

    private static bool BuildStructures(Configuration config, FirebirdReleases releases)
    {
        if (Configuration.IsNormal)
        {
            StdOut.NormalLine("Building package structures.");
        }

        var structurizer = new PackageStructureBuilder(config);
        var result = ExecuteForReleases(config, releases, release => structurizer.BuildStructures(release));

        return result;
    }

    private static bool UnPack(Configuration config, FirebirdReleases releases)
    {
        if (Configuration.IsNormal)
        {
            StdOut.NormalLine("Unpacking assets.");
        }

        var unPacker = new AssetUnPacker();
        var result = ExecuteForReleases(config, releases, release => unPacker.UnpackRelease(release));

        return result;
    }

    private static FirebirdReleases? DownloadAssets(Configuration config)
    {
        using var rm = new GithubReleaseManager(config);
        var releases = rm.GetLatestReleases();

        if (Configuration.IsNormal)
        {
            StdOut.NormalLine($"Current versions are: V3={releases.V3.ReleaseVersion}, V4={releases.V4.ReleaseVersion}, V5={releases.V5.ReleaseVersion}");
        }

        var result = ExecuteForReleases(config, releases, Download);

        return result
            ? releases
            : null;

        bool Download(FirebirdRelease release)
        {
            // ReSharper disable once AccessToDisposedClosure
            var downloadResult = rm.DownloadAssets(release)
                .Result.All(r => r);
            return downloadResult;
        }
    }

    private static bool ExecuteForReleases(Configuration config, FirebirdReleases releases, Func<FirebirdRelease, bool> func)
    {
        foreach (var ver in config.VersionsToBuild)
        {
            var success = ver switch
            {
                FirebirdVersion.V3 => func(releases.V3),
                FirebirdVersion.V4 => func(releases.V4),
                FirebirdVersion.V5 => func(releases.V5),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!success)
            {
                return false;
            }
        }
        return true;
    }
}
