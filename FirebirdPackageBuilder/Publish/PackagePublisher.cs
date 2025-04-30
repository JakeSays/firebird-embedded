using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Std.FirebirdEmbedded.Tools.Common;
using Std.FirebirdEmbedded.Tools.Support;


// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable


namespace Std.FirebirdEmbedded.Tools.Publish;

internal sealed class PublishResults : ToolResult
{
    public override bool Success { get; }

    public PublishStatus Status { get; }

    public PublishResults(PublishStatus status)
    {
        Status = status;
        Success = status == PublishStatus.Success;
    }

    public PublishResults()
    {
        Success = false;
        Status = PublishStatus.Failed;
    }

    public static implicit operator PublishResults(PublishStatus status) => new(status);
}

internal sealed class PackagePublisher : Tool<PublishConfiguration, List<string>, PublishResults>
{
    private readonly CachingSourceProvider _sourceProviderCache;
    private readonly PackageSourceProvider _packageSourceProvider = new ();
    private readonly PackageUpdateResource _updateResource;
    private readonly PackageSource _theSource;
    private readonly PublishConfiguration _config;
    private bool _pushSucceeded = false;

    public NugetSource Source => _config.Source;

    public PackagePublisher(PublishConfiguration config)
        : base(config)
    {
        _config = config;

        var url = Source switch
        {
            NugetSource.Production => "https://api.nuget.org/v3/index.json",
            NugetSource.Staging => "https://apiint.nugettest.org/v3/index.json",
            NugetSource.TestServer => "http://localhost:5000/v3/index.json",
            NugetSource.LocalDirectory => config.LocalPackageDir ?? throw new ArgumentNullException(nameof(config.LocalPackageDir)),
            _ => throw new InvalidOperationException()
        };

        _theSource = new PackageSource(url, "TheSource", true);
        if (Source == NugetSource.TestServer)
        {
            _theSource.AllowInsecureConnections = true;
        }
        _packageSourceProvider.AddPackageSource(_theSource);
        _sourceProviderCache = new CachingSourceProvider(_packageSourceProvider);

        var repository = _sourceProviderCache.CreateRepository(_theSource);
        _updateResource = repository.GetResource<PackageUpdateResource>();
        _updateResource.Settings = new NullSettings();
    }

    private protected override PublishResults Execute(List<string> packagePaths)
    {
        var logger = NugetLogger.Create(Verbosity.Loud, LogMessageFilter);

        if (ConsoleConfig.IsNormal && _config.ForcePublish)
        {
            StdOut.YellowLine("Force publish enabled");
        }

        try
        {
            var packages = ParsePackages(packagePaths)
                .Where(package => _config.ForcePublish || package.Release!.PublishDate == null)
                .ToArray();

            if (packages.Length == 0)
            {
                if (ConsoleConfig.IsNormal)
                {
                    StdOut.YellowLine("No unpublished packages found, nothing to publish.");
                }
                return PublishStatus.Success;
            }

            var successCount = 0;

            foreach (var package in packages)
            {
                if (package.Rid.Platform == Platform.Osx)
                {
                    StdOut.YellowLine($"Skipping OSX package {package.Name}");
                    continue;
                }
                var startTime = DateTimeOffset.Now;
                if (ConsoleConfig.IsNormal)
                {
                    StdOut.Normal($"Publishing package '{package.Name}'..");
                }

                _pushSucceeded = false;
                _updateResource.Push(
                    packagePaths: [package.Path],
                    symbolSource: null,
                    timeoutInSecond: _config.TimeoutInSeconds,
                    disableBuffering: false,
                    getApiKey: _ => _config.NugetApiKey,
                    getSymbolApiKey: null,
                    noServiceEndpoint: false,
                    skipDuplicate: true,
                    symbolPackageUpdateResource: null,
                    log: logger)
                    .Wait();

                var endTime = DateTimeOffset.Now;
                var duration = endTime - startTime;

                if (!_pushSucceeded)
                {
                    StdOut.RedLine(
                        !ConsoleConfig.IsNormal
                            ? $"Publishing package '{package.Name}' FAILED."
                            : $" FAILED in {(int) duration.TotalSeconds} seconds.");
                    continue;
                }

                package.Release!.PublishDate = endTime;

                successCount++;
                if (!ConsoleConfig.IsNormal)
                {
                    continue;
                }

                StdOut.GreenLine($" completed in {(int) duration.TotalSeconds} seconds.");
            }

            if (ConsoleConfig.IsNormal)
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

    private IEnumerable<PackageInfo> ParsePackages(List<string> packagePaths)
    {
        foreach (var packagePath in packagePaths)
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException("Package not found", packagePath);
            }

            var package = PackageInfo.Parse(packagePath);

            package.Release = Metadata.FindRelease(package.Product, package.PackageVersion, package.Rid);
            if (package.Release == null)
            {
                throw new InvalidOperationException($"No release found for package '{package.Name}'");
            }

            yield return package;
        }
    }
}
