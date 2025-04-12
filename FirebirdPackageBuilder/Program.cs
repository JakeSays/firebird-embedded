using System.Reflection;
using NuGet.Versioning;
using Std.CommandLine;
using Std.FirebirdEmbedded.Tools.MetaData;
using Std.FirebirdEmbedded.Tools.Publish;
using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools;

internal enum Verbosity
{
    Silent,
    Normal,
    Loud,
    Nagging
}

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

    private class BuildArgs
    {
        public List<FirebirdVersion> Versions { get; set; } = [];
        public Verbosity Verbosity { get; set; }
        public string WorkDir { get; set; } = null!;
        public string? PackageDir { get; set; }
        public string? PackagePrefix { get; set; }
        public bool ForceDownload { get; set; }
        public string MetadataFile { get; set; } = null!;
        public BuildType Type { get; set; }
        public string? AssetManagerVersion { get; set; }
        public string InitialPackageVersion { get; set; } = null!;
    }

    private class PublishArgs
    {
        public Verbosity Verbosity { get; set; }
        public string? PackageDir { get; set; }
        public string NugetApiKey { get; set; } = null!;
        public string? SourceType { get; set; }
        public string? LocalPackageDir { get; set; }
        public int TimeoutInSeconds { get; set; }
        public string MetadataFile { get; set; } = null!;
        public string? PackageVersion { get; set; }
        public bool ForcePublish { get; set; }
    }

    public static void Main(string[] args)
    {
        var app = new StdApplication(args.ToList(), "Firebird Embedded Tools");

        app.CommandLine
            .WithExitOnParseError()
            .WithHelp()
            .WithVersion(Assembly.GetExecutingAssembly())
            .Option<Verbosity>(o => o
                .Name("Verbosity")
                .Alias("-v|--verbose")
                .Description($"Verbosity Level. Options are [{Join<Verbosity>()}]")
                .DefaultValue(Verbosity.Normal)
            )
            .Command(c => c
                .Name("build")
                .Description("Build native asset packages")
                .Option<string>(o => o
                    .Alias("-w|--workdir")
                    .Name("WorkDir")
                    .Required()
                    .Singleton()
                    .Description("Root directory for temporary work")
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
                    .Required()
                    .Singleton()
                    .Description("Path to the metadata file")
                )
                .Option<BuildType>(o => o
                    .Alias("-t|--type")
                    .Name("Type")
                    .Singleton()
                    .DefaultValue(BuildType.Normal)
                    .Description($"Build type. Default is Normal. Options are [{Join<BuildType>()}]")
                )
                .Option<List<FirebirdVersion>>(o => o
                    .Alias("--fbver")
                    .Name("Versions")
                    .Description("Select a firebird version to build. Can be specified multiple times. Default is to build all version.")
                )
                .Option<string>(o => o
                    .Alias("--amver")
                    .Name("AssetManagerVersion")
                    .Singleton()
                    .Required()
                    .Description("Minimum asset manager version.")
                )
                .Flag(f => f
                    .Alias("--forcedownload")
                    .Name("ForceDownload")
                    .Description("Force download of assets from github.")
                )
                .Option<string>(o => o
                    .Alias("--initver")
                    .Name("InitialPackageVersion")
                    .DefaultValue("1.0.1")
                    .Singleton()
                    .Description("Initial package version. Defaults to 1.0.1")
                )
                .OnExecute((BuildArgs bargs) => BuildPackages(bargs))
            )
            .Command(c => c
                .Name("publish")
                .Description("publish native asset packages")
                .Option<string>(o => o
                    .Alias("-p|--packagedir")
                    .Name("PackageDir")
                    .Required()
                    .Singleton()
                    .Description("Directory for packages")
                )
                .Option<string?>(o => o
                    .Alias("--pkgver")
                    .Name("PackageVersion")
                    .Singleton()
                    .Description("Specific package version to publish")
                )
                .Option<string>(o => o
                    .Alias("-m|--metadata")
                    .Name("MetadataFile")
                    .Required()
                    .Singleton()
                    .Description("Path to the metadata file")
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
                    .Description("Package source. Options are 'prod, staging, local, test' Defaults to 'test'")
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
    }

    private static int BuildPackages(BuildArgs args)
    {
        if (!NuGetVersion.TryParse(args.AssetManagerVersion, out var minAssetManagerVersion))
        {
            StdErr.RedLine($"Invalid asset manager version: {args.AssetManagerVersion}");
            return IStdApplication.ExitCodeFailure;
        }

        var maxAssetManagerVersion = new NuGetVersion(minAssetManagerVersion.Major + 1, 0, 0);

        var assetManagerVersion = new VersionRange(
            minAssetManagerVersion,
            includeMinVersion: true,
            maxVersion: maxAssetManagerVersion,
            includeMaxVersion: false);

        if (!ReleaseVersion.TryParse(args.InitialPackageVersion, out var initialPackageVersion, VersionStyle.Nuget))
        {
            StdErr.RedLine($"Invalid package version: {args.InitialPackageVersion}");
            return IStdApplication.ExitCodeFailure;
        }

        args.PackageDir ??= Path.Combine(args.WorkDir, "output");
        Configuration.Initialize(
            args.PackagePrefix,
            args.Verbosity,
            args.WorkDir,
            args.PackageDir,
            args.MetadataFile,
            args.Versions,
            args.ForceDownload,
            args.Type,
            assetManagerVersion,
            (ReleaseVersion) initialPackageVersion!);

        var metadata = MetadataSerializer.Load(Configuration.Instance.MetadataFilePath);
        if (metadata == null)
        {
            return IStdApplication.ExitCodeFailure;
        }

        var result = ReleaseBuilder.Build(Configuration.Instance, metadata);
        if (result && metadata.Changed)
        {
            MetadataSerializer.Save(metadata, Configuration.Instance.MetadataFilePath);
        }

        return result
            ? IStdApplication.ExitCodeSuccess
            : IStdApplication.ExitCodeFailure;
    }

    private static int PublishPackages(PublishArgs pargs)
    {
        LogConfig.Initialize(pargs.Verbosity);
        var metadata = MetadataSerializer.Load(pargs.MetadataFile);
        if (metadata == null)
        {
            return IStdApplication.ExitCodeFailure;
        }

        var result = PublishCommon(pargs, metadata);
        if (result != PublishStatus.Failed &&
            metadata.Changed)
        {
            MetadataSerializer.Save(metadata, pargs.MetadataFile);
        }

        return result switch
        {
            PublishStatus.Success => IStdApplication.ExitCodeSuccess,
            PublishStatus.Failed => IStdApplication.ExitCodeFailure,
            PublishStatus.CompletedWithErrors => 2,
            _ => IStdApplication.ExitCodeFailure
        };
    }

    private static PublishStatus PublishCommon(PublishArgs args, PackageMetadata metadata)
    {
        if (!Directory.Exists(args.PackageDir))
        {
            StdErr.RedLine($"Package directory '{args.PackageDir}' does not exist.");
            return PublishStatus.Failed;
        }

        var filter = args.PackageVersion != null
            ? $"*.{args.PackageVersion}.nupkg"
            : "*.nupkg";
        var packagePaths = Directory.GetFiles(args.PackageDir, filter, SearchOption.TopDirectoryOnly)
            .ToList();

        if (packagePaths.Count == 0)
        {
            if (LogConfig.IsNormal)
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
            var pusher = new PackagePublisher((NugetSource) source, args.LocalPackageDir);
            var status = pusher.Publish(
                metadata,
                packagePaths,
                args.NugetApiKey,
                args.TimeoutInSeconds,
                true,
                args.ForcePublish);

            return status;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Error publishing: {ex.Message}");
            return PublishStatus.Failed;
        }
    }
}

