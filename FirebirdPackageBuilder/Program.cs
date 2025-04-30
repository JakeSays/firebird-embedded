using System.Reflection;
using NuGet.Configuration;
using NuGet.Versioning;
using Std.CommandLine;
using Std.FirebirdEmbedded.Tools.Build;
using Std.FirebirdEmbedded.Tools.Build.Osx;
using Std.FirebirdEmbedded.Tools.Publish;
using Std.FirebirdEmbedded.Tools.Support;

// ReSharper disable UnusedAutoPropertyAccessor.Local


namespace Std.FirebirdEmbedded.Tools;

internal static class Program
{
    static string Join<TEnum>(bool lowerCase = false, Func<TEnum, bool>? filter = null)
        where TEnum : struct, Enum
    {
        filter ??= _ => true;

        var tags = Enum.GetNames<TEnum>().Select(tag => (Tag: Enum.Parse<TEnum>(tag), Name: tag))
            .Where(t => t.Name != "Unspecified");

        var result = string.Join(", ", tags.Where(e => filter(e.Tag)).Select(e => lowerCase ? e.Name.ToLower() : e.Name));
        return result;
    }

    private class CommonArgs
    {
        public Verbosity Verbosity { get; set; }
        public string? RepositoryRoot { get; set; }
        public string MetadataFile { get; set; } = null!;
    }

    private class BuildArgs : CommonArgs
    {
        public List<ProductId> Versions { get; set; } = [];
        public string? WorkDir { get; set; } = null!;
        public string? PackageDir { get; set; }
        public string? PackagePrefix { get; set; }
        public bool ForceDownload { get; set; }
        public BuildType Type { get; set; }
        public bool ForceBuildAssetManager { get; set; }
        public ReleaseVersion AssetManagerVersion { get; set; } = ReleaseVersion.Nil;
        public ReleaseVersion TargetPackageVersion { get; set; }
        public bool TemplatesOnly { get; set; }
    }

    private class PublishArgs : CommonArgs
    {
        public string? PackageDir { get; set; }
        public string NugetApiKey { get; set; } = null!;
        public string? SourceType { get; set; }
        public string? LocalPackageDir { get; set; }
        public int TimeoutInSeconds { get; set; }
        public ReleaseVersion PackageVersion { get; set; }
        public bool ForcePublish { get; set; }
    }

    public static void Main(string[] args)
    {
        var app = new StdApplication(args.ToList(), "package-builder");

        app.CommandLine
            .WithExitOnParseError()
            .WithHelp()
            .WithVersion(Assembly.GetExecutingAssembly())
            .GlobalOption<Verbosity>(o => o
                .Name("Verbosity")
                .Alias("-v|--verbose")
                .Description($"Verbosity Level. Options are [{Join<Verbosity>()}]")
                .DefaultValue(Verbosity.Normal)
            )
            .GlobalOption<string>(o => o
                .Alias("-r|--repo-root")
                .Name("RepositoryRoot")
                .Singleton()
                .Description("Repository root directory. Used to resolve relative paths.")
            )
            .Command(c => c
                .Name("build")
                .Description("Build native asset packages")
                .Option<string>(o => o
                    .Alias("-w|--workdir")
                    .Name("WorkDir")
                    .Singleton()
                    .Description("Root directory for temporary work. Defaults to <repo root>/workspace")
                )
                .Option<string?>(o => o
                    .Alias("-p|--packagedir")
                    .Name("PackageDir")
                    .Singleton()
                    .Description("Output directory for packages. Defaults to <workdir>/output")
                )
                .Option<string?>(o => o
                    .Alias("--prefix")
                    .Name("PackagePrefix")
                    .Singleton()
                    .Description("Prefix to append to package names and ids")
                )
                .Option<string>(o => o
                    .Alias("-m|--metadata")
                    .Name("MetadataFile")
                    .Singleton()
                    .Description("Path to the metadata file. Defaults to <repo root>/release-info.xml")
                )
                .Option<BuildType>(o => o
                    .Alias("-t|--type")
                    .Name("Type")
                    .Singleton()
                    .DefaultValue(BuildType.Normal)
                    .Description($"Build type. Default is Normal. Options are [{Join<BuildType>()}]")
                )
                .Option<List<ProductId>>(o => o
                    .Alias("--fbver")
                    .Name("Versions")
                    .Description("Select a firebird product version to build. Can be specified multiple times. Default is to build all versions.")
                )
                .Option<ReleaseVersion>(o => o
                    .Alias("--amver")
                    .Name("AssetManagerVersion")
                    .Singleton()
                    .Parser(rawValue => ParseReleaseVersion(rawValue, "--amver"))
                    .Description("Asset manager version. Required on first build unless --pkgver is used.")
                )
                .Flag(f => f
                    .Alias("--force-build-am")
                    .Name("ForceBuildAssetManager")
                    .Description("Force build asset manager if no packages are built.")
                )
                .Flag(f => f
                    .Alias("--force-download")
                    .Name("ForceDownload")
                    .Description("Force download of assets from github.")
                )
                .Option<ReleaseVersion>(o => o
                    .Alias("--pkgver")
                    .Name("TargetPackageVersion")
                    .Singleton()
                    .Parser(rawValue => ParseReleaseVersion(rawValue, "--pkgver"))
                    .Description("Target package package version. Can be used to force everything to build at a specific version.")
                )
                .Flag(f => f
                    .Alias("--to")
                    .Name("TemplatesOnly")
                    .Hidden()
                    .Description("Generate templates only. Defaults to false.")
                )
                .OnExecute((BuildArgs bargs) => BuildPackages(bargs))
            )
            .Command(c => c
                .Name("publish")
                .Description("publish native asset packages")
                .Option<string?>(o => o
                    .Alias("-p|--packagedir")
                    .Name("PackageDir")
                    .Singleton()
                    .Description("Directory for packages. Defaults to <repo root>/workspace/output")
                )
                .Option<ReleaseVersion?>(o => o
                    .Alias("--pkgver")
                    .Name("PackageVersion")
                    .Singleton()
                    .Parser(rawValue => ParseReleaseVersion(rawValue, "Package version"))
                    .Description("Specific package version to publish")
                )
                .Option<string?>(o => o
                    .Alias("-m|--metadata")
                    .Name("MetadataFile")
                    .Singleton()
                    .Description("Path to the metadata file. Defaults to <repo root>/release-info.xml")
                )
                .Option<string>(o => o
                    .Alias("-k|--apikey")
                    .Name("NugetApiKey")
                    .Required()
                    .Singleton()
                    .Description("Nuget API Key to use when pushing")
                )
                .Option<string>(o => o
                    .Alias("-s|--source")
                    .Name("SourceType")
                    .Singleton()
                    .DefaultValue("test")
                    .Description("Package source. Options are 'prod, staging, local, test'")
                )
                .Flag(f => f
                    .Alias("--force")
                    .Name("ForcePublish")
                    .Description("Force publish the latest version")
                )
                .Option<int>(o => o
                    .Alias("--timeout")
                    .Name("TimeoutInSeconds")
                    .Singleton()
                    .DefaultValue(5 * 60)
                    .Description("Timeout in seconds. Default is 5 minutes")
                )
                .Option<string?>(o => o
                    .Alias("--localpath")
                    .Name("LocalPackageDir")
                    .Singleton()
                    .Description("Local package path. Required when --source is 'local'")
                )
                .OnExecute((PublishArgs pargs) => PublishPackages(pargs))
            )
            .Build();
        app.Run();

        static (ReleaseVersion Result, string? Error) ParseReleaseVersion(string? input, string valueName)
        {
            if (!ReleaseVersion.TryParse(input, out var result, VersionStyle.Nuget))
            {
                return (ReleaseVersion.Nil, $"Could not parse {valueName}");
            }

            return (result, null);
        }
    }

    private static int BuildPackages(BuildArgs args)
    {
        ConsoleConfig.Initialize(args.Verbosity);

        if (args.WorkDir != null)
        {
            args.PackageDir ??= Path.Combine(args.WorkDir, "output");
        }

        var config = new BuildConfiguration(
            args.RepositoryRoot,
            args.PackagePrefix,
            args.WorkDir,
            args.PackageDir,
            args.MetadataFile,
            args.Versions,
            args.ForceDownload,
            args.Type,
            args.AssetManagerVersion,
            args.TargetPackageVersion,
            args.ForceBuildAssetManager);

        var builder = new BuildManager(config);
        var result = builder.Run(args.TemplatesOnly);

        return result.Success
            ? IStdApplication.ExitCodeSuccess
            : IStdApplication.ExitCodeFailure;
    }

    private static int PublishPackages(PublishArgs pargs)
    {
        ConsoleConfig.Initialize(pargs.Verbosity);

        var result = PublishCommon(pargs);

        return result switch
        {
            PublishStatus.Success => IStdApplication.ExitCodeSuccess,
            PublishStatus.Failed => IStdApplication.ExitCodeFailure,
            PublishStatus.CompletedWithErrors => 2,
            _ => IStdApplication.ExitCodeFailure
        };
    }

    private static PublishStatus PublishCommon(PublishArgs args)
    {
        if (args.PackageDir == null)
        {
            if (args.RepositoryRoot == null)
            {
                StdErr.RedLine("Repository root required.");
                return PublishStatus.Failed;
            }
            args.PackageDir = Path.Combine(args.RepositoryRoot!, "workspace/output");
        }
        if (!Directory.Exists(args.PackageDir))
        {
            StdErr.RedLine($"Package directory '{args.PackageDir}' does not exist.");
            return PublishStatus.Failed;
        }

        var filter = $"*.{args.PackageVersion.ToString(VersionStyle.Nuget)}.nupkg";
        var packagePaths = Directory.GetFiles(args.PackageDir, filter, SearchOption.TopDirectoryOnly)
            .ToList();

        if (packagePaths.Count == 0)
        {
            if (ConsoleConfig.IsNormal)
            {
                StdErr.YellowLine("No packages found, nothing to publish.");
            }
            return PublishStatus.Success;
        }

        var source = args.SourceType?.ToLowerInvariant()
            switch
            {
                null => NugetSource.Staging,
                "staging" => NugetSource.Staging,
                "prod" => NugetSource.Production,
                "local" => NugetSource.LocalDirectory,
                "test" => NugetSource.TestServer,
                _ => (NugetSource?) null,
            };

        if (source == null)
        {
            StdErr.RedLine($"Invalid source type '{args.SourceType}'.");
            return PublishStatus.Failed;
        }

        if (source == NugetSource.LocalDirectory)
        {
            if (args.LocalPackageDir == null)
            {
                StdErr.RedLine("Local publish directory is required with source type 'local'.");
                return PublishStatus.Failed;
            }

            if (!IoHelpers.SafeCreateDirectory(args.LocalPackageDir))
            {
                StdErr.RedLine($"Could not create local publish directory '{args.LocalPackageDir}'.");
                return PublishStatus.Failed;
            }
        }

        try
        {
            var config = new PublishConfiguration(
                args.NugetApiKey,
                (NugetSource) source,
                args.TimeoutInSeconds,
                args.PackageVersion,
                args.RepositoryRoot,
                args.PackageDir,
                args.MetadataFile,
                args.ForcePublish,
                args.LocalPackageDir);

            var pusher = new PackagePublisher(config);
            var result = pusher.Run(packagePaths);

            return result.Status;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Error publishing: {ex.Message}");
            return PublishStatus.Failed;
        }
    }

    private static void TestXar()
    {
        var pkgUnpacker = new PkgUnpacker();
        pkgUnpacker.Unpack("/p/firebird/Firebird-5.0.2.1613-0-macos-arm64.pkg", "/p/firebird/test-output");

        //
        // if (!XarApiInitializer.Initialize("/p/firebird/xar/out/debug"))
        // {
        //     StdErr.RedLine("Cannot initialize xar");
        // }
        //
        // using var reader = new XarReader();
        // if (!reader.Open("/p/firebird/Firebird-5.0.2.1613-0-macos-arm64.pkg"))
        // {
        //     StdErr.RedLine("Open failed");
        // }
        //
        // foreach (var entry in reader.GetEntries())
        // {
        //     StdErr.NormalLine($"{entry.Type}: {entry.Path} ({entry.Size})");
        // }
        //
        // var result = reader.Extract("/p/firebird/xartest");
    }
}

