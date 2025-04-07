namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class PackageMetadata
{
    private readonly List<PackageRelease> _v3Releases = [];
    private readonly List<PackageRelease> _v4Releases = [];
    private readonly List<PackageRelease> _v5Releases = [];

    public IReadOnlyList<PackageRelease> V3Releases => _v3Releases;

    public IReadOnlyList<PackageRelease> V4Releases => _v4Releases;

    public IReadOnlyList<PackageRelease> V5Releases => _v5Releases;

    public bool Changed { get; private set; }

    public PackageRelease? GetLatestRelease(FirebirdVersion version, Rid? rid = null)
    {
        var productReleases =
            version switch
            {
                FirebirdVersion.V3 => _v3Releases,
                FirebirdVersion.V4 => _v4Releases,
                FirebirdVersion.V5 => _v5Releases,
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
            };

        var latest = productReleases.Where(r => rid is null || r.Rid == rid)
            .OrderByDescending(r => r.FirebirdRelease)
            .ThenByDescending(r => r.PackageVersion)
            .FirstOrDefault();
        return latest;
    }

    public void AddRelease(PackageRelease release, bool loading = false)
    {
        if (!loading)
        {
            Changed = true;
        }

        switch (release.FbVersion)
        {
            case FirebirdVersion.V3:
                _v3Releases.Add(release);
                break;
            case FirebirdVersion.V4:
                _v4Releases.Add(release);
                break;
            case FirebirdVersion.V5:
                _v5Releases.Add(release);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
