using System.Text.RegularExpressions;
using Octokit;


namespace Std.FirebirdEmbedded.Tools;

internal sealed partial class GithubReleaseManager : IDisposable
{
    private HttpClient? _httpClient;

    private readonly Configuration _config;

    public GithubReleaseManager(Configuration config)
    {
        _config = config;
    }

    public FirebirdReleases GetLatestReleases()
    {
        if (LogConfig.IsNormal)
        {
            StdOut.NormalLine("Downloading firebird release information from github.");
        }

        var client = new GitHubClient(new ProductHeaderValue("FirebirdPackageBuilder"));
        var releases = client.Repository.Release.GetAll("FirebirdSQL", "firebird")
            .Result
            .OrderByDescending(r => r.PublishedAt).ToList();

        var v3Latest = releases.First(r => r.TagName.StartsWith("v3.") && !r.Prerelease);
        var v4Latest = releases.First(r => r.TagName.StartsWith("v4.") && !r.Prerelease);
        var v5Latest = releases.First(r => r.TagName.StartsWith("v5.") && !r.Prerelease);

        var v3Release = new FirebirdRelease(
            FirebirdVersion.V3,
            v3Latest.TagName,
            v3Latest.Name,
            v3Latest.PublishedAt ?? v3Latest.CreatedAt,
            v3Latest.Body);

        foreach (var v3Asset in v3Latest.Assets)
        {
            MakeV3V4Asset(v3Asset, v3Release);
        }

        v3Release.Initialize(_config);

        var v4Release = new FirebirdRelease(
            FirebirdVersion.V4,
            v4Latest.TagName,
            v4Latest.Name,
            v4Latest.PublishedAt ?? v4Latest.CreatedAt,
            v4Latest.Body);

        foreach (var v4Asset in v4Latest.Assets)
        {
            MakeV3V4Asset(v4Asset, v4Release);
        }

        v4Release.Initialize(_config);

        var v5Release = new FirebirdRelease(
            FirebirdVersion.V5,
            v5Latest.TagName,
            v5Latest.Name,
            v5Latest.PublishedAt ?? v5Latest.CreatedAt,
            v5Latest.Body);

        foreach (var v5Asset in v5Latest.Assets)
        {
            MakeV5Asset(v5Asset, v5Release);
        }

        v5Release.Initialize(_config);

        return new FirebirdReleases(v3Release, v4Release, v5Release);

        void MakeV3V4Asset(ReleaseAsset source, FirebirdRelease release)
        {
            var result = ParseFileName(release.Version, source);
            if (result == null)
            {
                return;
            }

            if (release.ReleaseVersion == ReleaseVersion.Nil)
            {
                release.ReleaseVersion = result.Version;
            }

            //the asset adds itself to the release in its ctor.
            _ = new FirebirdAsset(
                result.Version,
                RemoveExtension(source.Name),
                source.Name,
                result.Platform,
                result.Arch,
                result.Type,
                source.BrowserDownloadUrl,
                (uint)source.Size,
                source.State == "uploaded",
                release,
                _config);
        }

        void MakeV5Asset(ReleaseAsset source, FirebirdRelease release)
        {
            var result = ParseFileName(release.Version, source);
            if (result == null)
            {
                return;
            }

            if (release.ReleaseVersion == ReleaseVersion.Nil)
            {
                release.ReleaseVersion = result.Version;
            }

            //the asset adds itself to the release in its ctor.
            _ = new FirebirdAsset(
                result.Version,
                RemoveExtension(source.Name),
                source.Name,
                result.Platform,
                result.Arch,
                result.Type,
                source.BrowserDownloadUrl,
                (uint)source.Size,
                source.State == "uploaded",
                release,
                _config);
        }

        static string RemoveExtension(string fileName)
        {
            const string tgzExtension = ".tar.gz";

            return fileName.EndsWith(tgzExtension, StringComparison.OrdinalIgnoreCase)
                ? fileName[..^tgzExtension.Length]
                : Path.GetFileNameWithoutExtension(fileName);
        }
    }

    record ParseResult(ReleaseVersion Version, Architecture Arch, ContentType Type, Platform Platform);

    static ParseResult? ParseFileName(FirebirdVersion fbVersion, ReleaseAsset asset)
    {
        var match = fbVersion == FirebirdVersion.V5
            ? V5NameParser.Match(asset.Name.ToLower())
            : BelowV5NameParser.Match(asset.Name.ToLower());
        if (!match.Success)
        {
            return null;
        }

        var version = new ReleaseVersion(
            uint.Parse(match.Groups["major"].Value),
            uint.Parse(match.Groups["minor"].Value),
            uint.Parse(match.Groups["patch"].Value),
            uint.Parse(match.Groups["build"].Value),
            uint.Parse(match.Groups["rev"].Value));

        Platform platform;
        Architecture arch;
        if (fbVersion == FirebirdVersion.V5)
        {
            platform = match.Groups["platform"].Value
                switch
                {
                    "windows" => Platform.Windows,
                    "linux" => Platform.Linux,
                    _ => throw new ArgumentOutOfRangeException()
                };
            arch = match.Groups["arch"].Value
                switch
                {
                    "x86" => Architecture.X32,
                    "x64" => Architecture.X64,
                    "arm32" => Architecture.Arm32,
                    "arm64" => Architecture.Arm64,
                    _ => throw new ArgumentOutOfRangeException()
                };
        }
        else
        {
            (platform, arch) = match.Groups["arch"].Value
                switch
                {
                    "win32" => (Platform.Windows, Architecture.X32),
                    "x64" => (Platform.Windows, Architecture.X64),
                    "i686" => (Platform.Linux, Architecture.X32),
                    "amd64" => (Platform.Linux, Architecture.X64),
                    _ => throw new ArgumentOutOfRangeException()
                };
        }

        var contentType = match.Groups["ext"].Value
            switch
            {
                "zip" => ContentType.Zip,
                "tar.gz" => ContentType.Tarball,
                _ => throw new ArgumentOutOfRangeException()
            };

        return new ParseResult(version, arch, contentType, platform);
    }

    [GeneratedRegex(@"^Firebird\-(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.(?<build>\d+)\-(?<rev>\d+)(\-|\.)(?<arch>Win32|x64|i686|amd64)\.(?<ext>zip|tar\.gz)$",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BelowV5NameParser { get; }

    [GeneratedRegex(@"^Firebird\-(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)\.(?<build>\d+)\-(?<rev>\d+)\-(?<platform>linux|windows)\-(?<arch>x86|x64|arm32|arm64)\.(?<ext>zip|tar\.gz)$",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex V5NameParser { get; }

    internal record DownloadResult(
        FirebirdAsset Asset,
        Exception? Exception = null)
    {
        public static implicit operator bool(DownloadResult r) => r.Exception == null;
    }

    internal Task<DownloadResult[]> DownloadAssets(FirebirdRelease release)
    {
        if (LogConfig.IsNormal)
        {
            StdOut.NormalLine("Downloading firebird assets.");
        }

        var httpClient = _httpClient ??= new HttpClient();

        Directory.CreateDirectory(_config.DownloadDirectory);

        var downloadTasks = release.Assets.Select(a => Task.Run(() => DownloadAsset(a)));
        return Task.WhenAll(downloadTasks);

        async Task<DownloadResult> DownloadAsset(FirebirdAsset asset)
        {
            try
            {
                if (File.Exists(asset.LocalPath))
                {
                    if (!_config.ForceDownload)
                    {
                        if (LogConfig.IsNaggy)
                        {
                            StdOut.DarkGreenLine($"{asset.FileName} already exists, skipping download.");
                        }
                        return new DownloadResult(asset);
                    }
                    File.Delete(asset.LocalPath);
                }

                if (LogConfig.IsLoud)
                {
                    StdOut.NormalLine($"Downloading asset {asset.Name}");
                }

                await using var outputFile = File.OpenWrite(asset.LocalPath);
                await using var sourceStream = await httpClient.GetStreamAsync(asset.DownloadUrl);
                await sourceStream.CopyToAsync(outputFile);

                return new DownloadResult(asset);
            }
            catch (Exception ex)
            {
                if (!LogConfig.IsSilent)
                {
                    StdErr.RedLine($"Error downloading asset {asset.Name}, reason: {ex.Message}");
                }
                return new DownloadResult(asset, ex);
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
