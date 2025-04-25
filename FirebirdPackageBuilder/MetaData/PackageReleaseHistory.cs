namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class PackageReleaseHistory
{
    private readonly PackageMetadata _metadata;
    private readonly List<PackageRelease> _history = [];

    public ProductId Product { get; }
    public ReleaseVersion CurrentPackageVersion { get; set; }
    public ReleaseVersion CurrentAssetVersion { get; set; }

    public IReadOnlyList<PackageRelease> History => _history;

    public PackageRelease? HighestRelease => _history
        .OrderByDescending(r => r.PackageVersion)
        .ThenByDescending(r => r.FirebirdRelease)
        .ThenByDescending(r => r.BuildDate)
        .FirstOrDefault();

    public void AddRelease(PackageRelease release, bool loading = false)
    {
        if (_history.Contains(release))
        {
            return;
        }

        release.Metadata = _metadata;

        _history.Add(release);

        if (release.PackageVersion > CurrentPackageVersion)
        {
            CurrentPackageVersion = release.PackageVersion;
        }

        if (release.FirebirdRelease > CurrentAssetVersion)
        {
            CurrentAssetVersion = release.FirebirdRelease;
        }

        if (!loading)
        {
            _metadata.Changed = true;
        }
    }

    public PackageReleaseHistory(
        PackageMetadata metadata,
        ProductId product)
    {
        _metadata = metadata;
        Product = product;
    }
}
