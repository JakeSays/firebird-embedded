using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.RuntimeModel;
using NuGet.Versioning;
using Std.FirebirdEmbedded.Tools.MetaData;
using Std.FirebirdEmbedded.Tools.Templates;


namespace Std.FirebirdEmbedded.Tools.Build;

internal sealed class NugetPackageBuilder
{
    private readonly ReadmeTemplate _readmeTemplate = new();
    private readonly TargetFileTemplate _targetFileTemplate = new();

    private readonly BuildConfiguration _config;

    private readonly PackageMetadata _metadata;

    public NugetPackageBuilder(BuildConfiguration config, PackageMetadata metadata)
    {
        _config = config;
        _metadata = metadata;
    }

    public bool BuildPackagesForRelease(FirebirdRelease release)
    {
        try
        {
            Directory.CreateDirectory(_config.PackageOutputDirectory);

//            BuildMetaPackage(release);

            BuildMetaAssetPackagesForRelease(release);

            foreach (var asset in release.Assets)
            {
                BuildAssetPackage(asset);
            }

            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Failed to build nuget packages for release '{release.Name}': {ex.Message}");
            return false;
        }
    }

    private void BuildMetaAssetPackagesForRelease(FirebirdRelease release)
    {
        BuildConsolidatedPackage(release.LinuxPackage);
        BuildConsolidatedPackage(release.WindowsPackage);

        if (release.Product == ProductId.V5)
        {
            BuildConsolidatedPackage(release.MacOsPackage);
        }

        void BuildConsolidatedPackage(ConsolidatedPackageDetails details)
        {
            if (_config.TemplatesOnly)
            {
                var tassets = release.Assets
                    .Where(a => a.Platform == details.Platform)
                    .ToArray();
                _targetFileTemplate.Generate(details, "netstandard2.0", tassets, true);
                _readmeTemplate.Generate(details);
                return;
            }

            var (currentRelease, releaseChanged) = CalculatePackageRelease(details);

            if (!releaseChanged &&
                _config.BuildType != BuildType.Force)
            {
                if (ConsoleConfig.IsNormal)
                {
                    StdOut.NormalLine(
                        $"Meta package '{details.PackageId}' version {currentRelease.PackageVersion.ToString(VersionStyle.Nuget)} is unchanged.");
                }

                return;
            }

            _metadata.AddRelease(currentRelease);

            var assets = release.Assets
                .Where(a => a.Platform == details.Platform)
                .ToArray();

            var builder = PreparePackageBuilder(
                details.PackageId,
                currentRelease.PackageVersion.ToString(VersionStyle.Nuget),
                $"Binary assets for embedded FirebirdSQL {release.VersionLong} ({details.Release.ReleaseVersion}) {details.Platform} {string.Join(", ", details.Architectures)}.");

            AddAssetManagerDependency(builder, ".NETStandard2.0");
            AddAssetManagerDependency(builder, ".NETFramework4.8");

            foreach (var asset in assets)
            {
                var contentRoot = MakeAssetPath(asset);
                var files = asset.NugetFiles
                    .Where(a => !a.SourcePath.Contains("LICENSES"))
                    .Where(a => !a.SourcePath.EndsWith(PackageStructureBuilder.IconFileName))
                    .ToList();
                AddNugetFiles(builder, contentRoot, files, asset.PackageRootDirectory);
            }

            _readmeTemplate.Generate(details);
            builder.AddFiles(details.PackageRootDirectory, "README.md", "");

            AddNugetFiles(builder, "", details.NugetFiles, details.PackageRootDirectory);

            AddTargetFile("netstandard2.0", true);
            AddTargetFile("netstandard2.0", false);
            AddTargetFile("net48", true);
            AddTargetFile("net48", false);

            MakeLibDir(details.PackageRootDirectory, "netstandard2.0");
            MakeLibDir(details.PackageRootDirectory, "net48");

            builder.AddFiles(details.PackageRootDirectory, "dummies/lib/**/*", "lib");

            var packageFileName =
                $"{details.PackageId}.{currentRelease.PackageVersion.ToString(VersionStyle.Nuget)}.nupkg";
            var packagePath = Path.Combine(_config.PackageOutputDirectory, packageFileName);

            if (ConsoleConfig.IsNormal)
            {
                StdOut.NormalLine($"Building package '{packageFileName}'");
            }

            using var outputStream = new FileStream(packagePath, FileMode.Create, FileAccess.Write);
            builder.Save(outputStream);

            void AddTargetFile(string tfm, bool transitive)
            {
                var targetFile = _targetFileTemplate.Generate(details, tfm, assets, transitive);
                builder.AddFiles(details.PackageRootDirectory, targetFile, Path.GetDirectoryName(targetFile));
            }
        }
    }

    private (PackageRelease Release, bool Changed) CalculatePackageRelease(IPackageDetails details)
    {
        var currentReleaseOrg = _metadata.GetLatestRelease(details.Release.Product, details.Rid);
        var currentRelease = currentReleaseOrg;
        if (currentRelease == null)
        {
            currentRelease = PackageRelease.Initial(
                details.Rid,
                _config.BuildDate,
                details.Release.Product,
                details.Release.ReleaseVersion,
                _config.TargetPackageVersion);
        }
        else if (currentRelease.FirebirdRelease != details.Release.ReleaseVersion)
        {
            //this is a firebird version bump so ignore the build type
            currentRelease = currentRelease.NextProductVersion(details.Release.ReleaseVersion, _config.BuildDate);
        }
        else if (_config.BuildType == BuildType.Rebuild)
        {
            //a rebuild increments the package patch number
            currentRelease = currentRelease.NextPatchVersion(_config.BuildDate);
        }
        else if (_config.BuildType == BuildType.Force)
        {
            //we are rebuilding the exact same package version.. not sure why we would want to do this beyond testing.
            currentRelease.BuildDate = _config.BuildDate;
        }
        // if nothing changed then this is a 'normal' build, no firebird version bump, no package version bump.

        return (currentRelease,
            !ReferenceEquals(currentRelease, currentReleaseOrg) || _config.BuildType == BuildType.Force);
    }

    private void BuildAssetPackage(FirebirdAsset asset)
    {
        if (_config.TemplatesOnly)
        {
            _targetFileTemplate.Generate("netstandard2.0", asset, true);
            _readmeTemplate.Generate(asset);
            return;
        }

        var (currentRelease, releaseChanged) = CalculatePackageRelease(asset);

        if (!releaseChanged)
        {
            if (ConsoleConfig.IsNormal)
            {
                StdOut.NormalLine(
                    $"Asset package '{asset.PackageId}' version {currentRelease.PackageVersion.ToString(VersionStyle.Nuget)} is unchanged, build skipped.");
            }

            return;
        }

        _metadata.AddRelease(currentRelease);

        _readmeTemplate.Generate(asset);

        var builder = PreparePackageBuilder(
            asset.PackageId,
            currentRelease.PackageVersion.ToString(VersionStyle.Nuget),
            $"Binary assets for embedded FirebirdSQL {asset.Release.VersionLong} ({asset.Release.ReleaseVersion}) {asset.Platform} {asset.Architecture}.");

        AddAssetManagerDependency(builder, ".NETFramework4.8");
        AddAssetManagerDependency(builder, ".NETStandard2.0");

        var contentRoot = MakeAssetPath(asset);

        AddNugetFiles(builder, contentRoot, asset.NugetFiles, asset.PackageRootDirectory);

        builder.AddFiles(asset.PackageRootDirectory, "README.md", "");

        AddTargetFile("net48", true);
        AddTargetFile("net48", false);
        AddTargetFile("netstandard2.0", true);
        AddTargetFile("netstandard2.0", false);

        MakeLibDir(asset.PackageRootDirectory, "net48");
        MakeLibDir(asset.PackageRootDirectory, "netstandard2.0");

        builder.AddFiles(asset.PackageRootDirectory, "dummies/lib/**/*", "lib");

        var packageFileName = $"{asset.PackageId}.{currentRelease.PackageVersion.ToString(VersionStyle.Nuget)}.nupkg";
        var packagePath = Path.Combine(_config.PackageOutputDirectory, packageFileName);

        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine($"Building package '{packageFileName}'");
        }

        using var outputStream = new FileStream(packagePath, FileMode.Create, FileAccess.Write);
        builder.Save(outputStream);

        void AddTargetFile(string tfm, bool transitive)
        {
            var targetFile = _targetFileTemplate.Generate(tfm, asset, transitive);
            builder.AddFiles(asset.PackageRootDirectory, targetFile, Path.GetDirectoryName(targetFile));
        }
    }

    private void BuildMetaPackage(FirebirdRelease release)
    {
        var (currentRelease, releaseChanged) = CalculatePackageRelease(release);

        if (!releaseChanged &&
            _config.BuildType != BuildType.Force)
        {
            if (ConsoleConfig.IsNormal)
            {
                StdOut.NormalLine(
                    $"Meta package '{release.PackageId}' version {currentRelease.PackageVersion.ToString(VersionStyle.Nuget)} is unchanged.");
            }

            return;
        }

        _metadata.AddRelease(currentRelease);

        var builder = PreparePackageBuilder(
            release.PackageId,
            currentRelease.PackageVersion.ToString(VersionStyle.Nuget),
            $"Binary assets for embedded FirebirdSQL {release.VersionLong} ({release.ReleaseVersion}), meta package for all platforms and architectures.");

        AddAssetManagerDependency(builder, ".NETStandard2.0");
        AddAssetManagerDependency(builder, ".NETFramework4.8");

        var nugetVersion = VersionRange.Parse($"[{currentRelease.PackageVersion.ToString(VersionStyle.Nuget)}]");

        var runtimes = new List<RuntimeDescription>();
        foreach (var asset in release.Assets)
        {
            var depSet = new RuntimeDependencySet(
                release.PackageId, [new RuntimePackageDependency(asset.PackageId, nugetVersion)]);
            var runtimeDesc = new RuntimeDescription(asset.Rid.PackageText, [depSet]);
            runtimes.Add(runtimeDesc);
        }

        var dependencyGraph = new RuntimeGraph(runtimes);
        var jsonPath = Path.Combine(release.PackageRootDirectory, RuntimeGraph.RuntimeGraphFileName);
        JsonRuntimeFormat.WriteRuntimeGraph(jsonPath, dependencyGraph);
        builder.AddFiles(release.PackageRootDirectory,  RuntimeGraph.RuntimeGraphFileName, "");

        _readmeTemplate.Generate(release);
        builder.AddFiles(release.PackageRootDirectory, "README.md", "");

        AddNugetFiles(builder, "", release.NugetFiles,release.PackageRootDirectory);

        MakeLibDir(release.PackageRootDirectory, "netstandard2.0");
        MakeLibDir(release.PackageRootDirectory, "net48");

        builder.AddFiles(release.PackageRootDirectory, "dummies/lib/**/*", "lib");

        var packageFileName = $"{release.PackageId}.{currentRelease.PackageVersion.ToString(VersionStyle.Nuget)}.nupkg";
        var packagePath = Path.Combine(_config.PackageOutputDirectory, packageFileName);

        if (ConsoleConfig.IsNormal)
        {
            StdOut.NormalLine($"Building package '{packageFileName}'");
        }

        using var outputStream = new FileStream(packagePath, FileMode.Create, FileAccess.Write);
        builder.Save(outputStream);
    }

    private void AddAssetManagerDependency(PackageBuilder builder, string tfm)
    {
        var fixedVersion = _config.EffectiveAssetManagerVersion.AsNuGetVersion();
        var maxVersion = _config.EffectiveAssetManagerVersion.NextMajorVersion(true).AsNuGetVersion();

        var assetManagerVersion = new VersionRange(
            fixedVersion,
            includeMinVersion: true,
            maxVersion: maxVersion,
            includeMaxVersion: false);

        builder.DependencyGroups.Add(
            new PackageDependencyGroup(NuGetFramework.Parse(tfm),
            [
                new PackageDependency(_config.AssetManagerPackageName, assetManagerVersion)
            ]));
    }

    private static void MakeLibDir(string rootDirectory, string tfm)
    {
        var dir = Path.Combine(rootDirectory, "dummies", "lib", tfm);
        Directory.CreateDirectory(dir);
        using var _ = File.Create(Path.Combine(dir, "_._"));
    }

    private static string MakeAssetPath(FirebirdAsset asset)
    {
        return MakePath(asset, ".firebird", asset.Rid.PackageText, asset.Release.Product.ToString());
    }

    private static string MakePath(FirebirdAsset asset, params string[] parts)
    {
        return string.Join(
            asset.Platform == Platform.Linux
                ? '/'
                : '\\',
            parts);
    }

    private static void AddNugetFiles(NuGet.Packaging.PackageBuilder builder, string contentRoot, List<NugetFile> files, string sourceDirectory)
    {
        foreach (var file in files)
        {
            if (file.Destination == NugetDestination.Runtime)
            {
                var destPath = Path.Combine(contentRoot, file.SourcePath);
                builder.AddFiles(sourceDirectory, file.SourcePath, destPath);
                continue;
            }
            builder.AddFiles(sourceDirectory, file.SourcePath, "");
        }
    }

    public static NuGet.Packaging.PackageBuilder PreparePackageBuilder(string packageId, string packageVersion, string description)
    {
        var builder = new NuGet.Packaging.PackageBuilder(false, NullLogger.Instance)
        {
            Id = packageId,
            Version = NuGetVersion.Parse(packageVersion),
            Description = description,
            Authors = { "JakeSays" },
            Tags = { "firebird", "firebirdsql", "native", "sql", "embedded", "standalone", "firebirdsql.data.client" },
            LicenseUrl = new Uri("https://www.firebirdsql.org/en/licensing"),
            Language = "en-US",
            ProjectUrl = new Uri("https://github.com/jakesays/firebird-embedded"),
            Copyright = $"\u00a9 {DateTime.Now.Year} JakeSays",
            Readme = "README.md",
            Icon = "icon.png"
        };

        return builder;
    }
}
