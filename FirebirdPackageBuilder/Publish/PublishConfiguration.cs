using Std.FirebirdEmbedded.Tools.Common;


namespace Std.FirebirdEmbedded.Tools.Publish;

internal class PublishConfiguration : ToolConfiguration
{
    public string NugetApiKey { get; }
    public NugetSource Source { get; }
    public string? LocalPackageDir { get; }
    public int TimeoutInSeconds { get; }
    public ReleaseVersion? PackageVersion { get; }
    public bool ForcePublish { get;  }

    public PublishConfiguration(
        string nugetApiKey,
        NugetSource source,
        int timeoutInSeconds,
        ReleaseVersion? packageVersion,
        string? repositoryRoot,
        string? packageDirectory,
        string? metadataFilePath,
        bool forcePublish,
        string? localPackageDir)
        : base(repositoryRoot, packageDirectory, metadataFilePath)
    {
        NugetApiKey = nugetApiKey;
        Source = source;
        TimeoutInSeconds = timeoutInSeconds;
        PackageVersion = packageVersion;
        ForcePublish = forcePublish;
        LocalPackageDir = localPackageDir;
    }
}
