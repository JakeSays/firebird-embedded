using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Std.FirebirdEmbedded.Tools.MetaData;
using Std.FirebirdEmbedded.Tools.Support;


// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable


namespace Std.FirebirdEmbedded.Tools.Publish;

internal enum NugetSource
{
    Staging,
    Production,
    LocalDirectory,
    TestServer
}

internal enum PublishStatus
{
    Failed,
    Success,
    CompletedWithErrors
}

internal sealed class PackagePublisher
{
    private readonly CachingSourceProvider _sourceProviderCache;
    private readonly PackageSourceProvider _packageSourceProvider = new ();
    private readonly PackageUpdateResource _updateResource;
    private readonly PackageSource _theSource;
    private bool _pushSucceeded = false;

    public NugetSource Source { get; }

    public PackagePublisher(NugetSource source, string? localDirectory = null)
    {
        Source = source;

        var url = source switch
        {
            NugetSource.Production => "https://api.nuget.org/v3/index.json",
            NugetSource.Staging => "https://apiint.nugettest.org/v3/index.json",
            NugetSource.TestServer => "http://localhost:5000/v3/index.json",
            NugetSource.LocalDirectory => localDirectory ?? throw new ArgumentNullException(nameof(localDirectory)),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };

        _theSource = new PackageSource(url, "TheSource", true);
        if (source == NugetSource.TestServer)
        {
            _theSource.AllowInsecureConnections = true;
        }
        _packageSourceProvider.AddPackageSource(_theSource);
        _sourceProviderCache = new CachingSourceProvider(_packageSourceProvider);

        var repository = _sourceProviderCache.CreateRepository(_theSource);
        _updateResource = repository.GetResource<PackageUpdateResource>();
        _updateResource.Settings = new NullSettings();
    }

    public PublishStatus Publish(
        PackageMetadata metadata,
        List<string> packagePaths,
        string apiKey,
        int timeoutInSeconds,
        bool skipDuplicates,
        bool forcePublish)
    {
        var logger = NugetLogger.Create(Verbosity.Loud, LogMessageFilter);

        if (LogConfig.IsNormal && forcePublish)
        {
            StdOut.YellowLine("Force publish enabled");
        }

        try
        {
            var packages = ParsePackages(metadata, packagePaths)
                .Where(package => forcePublish || package.Release!.PublishDate == null)
                .ToArray();

            if (packages.Length == 0)
            {
                if (LogConfig.IsNormal)
                {
                    StdOut.YellowLine("No unpublished packages found, nothing to publish.");
                }
                return PublishStatus.Success;
            }

            var successCount = 0;

            foreach (var package in packages)
            {
                var startTime = DateTimeOffset.Now;
                if (LogConfig.IsNormal)
                {
                    StdOut.Normal($"Publishing package '{package.Name}'..");
                }

                _pushSucceeded = false;
                _updateResource.Push(
                    packagePaths: [package.Path],
                    symbolSource: null,
                    timeoutInSecond: timeoutInSeconds,
                    disableBuffering: false,
                    getApiKey: _ => apiKey,
                    getSymbolApiKey: null,
                    noServiceEndpoint: false,
                    skipDuplicate: skipDuplicates,
                    symbolPackageUpdateResource: null,
                    log: logger)
                    .Wait();

                var endTime = DateTimeOffset.Now;
                var duration = endTime - startTime;

                if (!_pushSucceeded)
                {
                    StdOut.RedLine(
                        !LogConfig.IsNormal
                            ? $"Publishing package '{package.Name}' FAILED."
                            : $" FAILED in {(int) duration.TotalSeconds} seconds.");
                    continue;
                }

                package.Release!.PublishDate = endTime;

                successCount++;
                if (!LogConfig.IsNormal)
                {
                    continue;
                }

                StdOut.GreenLine($" completed in {(int) duration.TotalSeconds} seconds.");
            }

            if (LogConfig.IsNormal)
            {
                StdOut.BlankLine();
                var failedCount = packages.Length - successCount;
                if (failedCount > 0)
                {
                    StdOut.YellowLine($"Publish completed with {successCount} successful and {failedCount} failed packages.");
                    return PublishStatus.CompletedWithErrors;
                }

                StdOut.GreenLine($"Published {successCount} packages successfully.");
            }

            return successCount == packagePaths.Count
                ? PublishStatus.Success
                : PublishStatus.Failed;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Error pushing packages: {ex.Message}");
            return PublishStatus.Failed;
        }
    }

    private bool LogMessageFilter(LogLevel level, string message)
    {
        //this is a really hacky way of detecting a successful push
        //but it's all we have to work with.

        if (level == LogLevel.Information &&
            message == NugetLogger.LocalizedPushSuccessfulString)
        {
            _pushSucceeded = true;
        }

        //filter all messages
        return true;
    }

    private static IEnumerable<PackageInfo> ParsePackages(PackageMetadata metadata, List<string> packagePaths)
    {
        foreach (var packagePath in packagePaths)
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException("Package not found", packagePath);
            }

            var package = PackageInfo.Parse(packagePath);

            package.Release = metadata.FindRelease(package.Version, package.PackageVersion, package.Rid);
            if (package.Release == null)
            {
                throw new InvalidOperationException($"No release found for package '{package.Name}'");
            }

            yield return package;
        }
    }
}
