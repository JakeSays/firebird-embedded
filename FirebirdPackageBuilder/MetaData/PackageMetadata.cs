namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class PackageMetadata
{
    public const string MetadataFileName = "release-info.xml";

    public PackageReleaseHistory AssetManagerReleases { get; }
    public PackageReleaseHistory V3Releases { get; }
    public PackageReleaseHistory V4Releases { get; }
    public PackageReleaseHistory V5Releases { get; }

    public bool Changed { get; set; }

    public PackageMetadata()
    {
        AssetManagerReleases = new PackageReleaseHistory(this, ProductId.AssetManager);
        V3Releases = new PackageReleaseHistory(this, ProductId.V3);
        V4Releases = new PackageReleaseHistory(this, ProductId.V4);
        V5Releases = new PackageReleaseHistory(this, ProductId.V5);
    }

    public PackageRelease? GetLatestRelease(ProductId version, Rid? rid = null)
    {
        var productReleases = GetProductHistory(version);

        var latest = productReleases.History.Where(r => rid is null || r.Rid == rid)
            .OrderByDescending(r => r.FirebirdRelease)
            .ThenByDescending(r => r.PackageVersion)
            .ThenByDescending(r => r.BuildDate)
            .FirstOrDefault();
        return latest;
    }

    public PackageRelease? FindRelease(ProductId product, ReleaseVersion packageVersion, Rid rid)
    {
        return GetProductHistory(product).History.FirstOrDefault(r => r.PackageVersion == packageVersion && r.Rid == rid);
    }

    public PackageReleaseHistory GetProductHistory(ProductId product) =>
        product switch
        {
            ProductId.V3 => V3Releases,
            ProductId.V4 => V4Releases,
            ProductId.V5 => V5Releases,
            ProductId.AssetManager => AssetManagerReleases,
            _ => throw new ArgumentOutOfRangeException(nameof(product), product, null)
        };

    public void AddRelease(PackageRelease release, bool loading = false)
    {
        switch (release.Product)
        {
            case ProductId.V3:
                V3Releases.AddRelease(release, loading);
                break;
            case ProductId.V4:
                V4Releases.AddRelease(release, loading);
                break;
            case ProductId.V5:
                V5Releases.AddRelease(release, loading);
                break;
            case ProductId.AssetManager:
                AssetManagerReleases.AddRelease(release, loading);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
