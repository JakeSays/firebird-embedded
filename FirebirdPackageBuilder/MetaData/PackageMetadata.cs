namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class PackageMetadata
{
    private readonly List<PackageRelease> _v3Releases = [];
    private readonly List<PackageRelease> _v4Releases = [];
    private readonly List<PackageRelease> _v5Releases = [];

    public IReadOnlyList<PackageRelease> V3Releases => _v3Releases;

    public IReadOnlyList<PackageRelease> V4Releases => _v4Releases;

    public IReadOnlyList<PackageRelease> V5Releases => _v5Releases;

    public bool Changed { get; set; }

    public PackageRelease? GetLatestRelease(FirebirdVersion version, Rid? rid = null)
    {
        var productReleases = PackageGroup(version);

        var latest = productReleases.Where(r => rid is null || r.Rid == rid)
            .OrderByDescending(r => r.FirebirdRelease)
            .ThenByDescending(r => r.PackageVersion)
            .FirstOrDefault();
        return latest;
    }

    public PackageRelease? FindRelease(FirebirdVersion version, ReleaseVersion packageVersion, Rid rid)
    {
        return PackageGroup(version).FirstOrDefault(r => r.PackageVersion == packageVersion && r.Rid == rid);
    }

    private List<PackageRelease> PackageGroup(FirebirdVersion version) =>
        version switch
        {
            FirebirdVersion.V3 => _v3Releases,
            FirebirdVersion.V4 => _v4Releases,
            FirebirdVersion.V5 => _v5Releases,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };

    private static void TryAdd(List<PackageRelease> group, PackageRelease release)
    {
        if (!group.Contains(release))
        {
            group.Add(release);
        }
    }

    public void AddRelease(PackageRelease release, bool loading = false)
    {
        release.Metadata = this;

        if (!loading)
        {
            Changed = true;
        }

        switch (release.FbVersion)
        {
            case FirebirdVersion.V3:
                TryAdd(_v3Releases, release);
                break;
            case FirebirdVersion.V4:
                TryAdd(_v4Releases, release);
                break;
            case FirebirdVersion.V5:
                TryAdd(_v5Releases, release);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
