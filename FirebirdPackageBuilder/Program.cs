using System.Reflection;
using Std.CommandLine;
using Std.FirebirdEmbedded.Tools.MetaData;


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
        public bool Push { get; set; }
        public string? NugetApiKey { get; set; }
        public bool ForceDownload { get; set; }
        public string MetadataFile { get; set; } = null!;
        public BuildType Type { get; set; }
    }

    private class PushArgs
    {
        public Verbosity Verbosity { get; set; }
        public string? PackageDir { get; set; }
        public string? NugetApiKey { get; set; }
    }

    public static void Main(string[] args)
    {
        var app = new StdApplication(args.ToList(), "Firebird Embedded Tools");
        app.CommandLine
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
                .Flag(f => f
                    .Alias("--forcedownload")
                    .Name("ForceDownload")
                    .Description("Force download of assets from github.")
                )
                .Flag(f => f
                    .Alias("--push")
                    .Name("Push")
                    .Description("Push packages on successful build.")
                )
                .Option<string?>(o => o
                    .Alias("--apikey")
                    .Name("NugetApiKey")
                    .Singleton()
                    .Description("Nuget API Key to use when pushing.")
                )
                .OnExecute((BuildArgs bargs) => BuildPackages(bargs))
            )
            .Command(c => c
                .Name("push")
                .Option<string>(o => o
                    .Alias("-p|--packagedir")
                    .Name("PackageDir")
                    .Required()
                    .Description("Directory for packages")
                )
                .Option<string?>(o => o
                    .Alias("--apikey")
                    .Name("NugetApiKey")
                    .Required()
                    .Description("Nuget API Key to use when pushing.")
                )
                .OnExecute((PushArgs pargs) => PushPackages(pargs))
            )
            .Build();
        app.Run();
    }

    private static int BuildPackages(BuildArgs args)
    {
        args.PackageDir ??= Path.Combine(args.WorkDir, "output");
        Configuration.Initialize(
            args.PackagePrefix,
            args.Verbosity,
            args.WorkDir,
            args.PackageDir,
            args.MetadataFile,
            args.Versions,
            args.ForceDownload,
            args.Type);

        var metadata = MetadataSerializer.Load(Configuration.Instance.MetadataFilePath);
        if (metadata == null)
        {
            return IStdApplication.ExitCodeFailure;
        }

        var result = ReleaseBuilder.DoIt(Configuration.Instance, metadata);
        if (result && metadata.Changed)
        {
            MetadataSerializer.Save(metadata, Configuration.Instance.MetadataFilePath);
        }

        return result
            ? IStdApplication.ExitCodeSuccess
            : IStdApplication.ExitCodeFailure;
    }

    private static int PushPackages(PushArgs pargs)
    {
        return 0;
    }
}

