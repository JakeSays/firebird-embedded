namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class PackageRelease
{
    private readonly NullableChangeTrackingProperty<DateTimeOffset> _publishDate;
    private readonly ChangeTrackingProperty<DateTimeOffset> _buildDate;

    public Rid Rid { get; }

    public DateTimeOffset BuildDate
    {
        get => _buildDate.Value;
        set => _buildDate.Value = value;
    }

    public DateTimeOffset? PublishDate
    {
        get => _publishDate.Value;
        set => _publishDate.Value = value;
    }

    public FirebirdVersion FbVersion { get; }
    public ReleaseVersion PackageVersion { get; }
    public ReleaseVersion FirebirdRelease { get; }

    public PackageMetadata? Metadata { get; set; }

    public PackageRelease(
        Rid rid,
        DateTimeOffset buildDate,
        FirebirdVersion fbVersion,
        ReleaseVersion packageVersion,
        ReleaseVersion firebirdRelease,
        DateTimeOffset? publishDate = null)
    {
        Rid = rid;
        FbVersion = fbVersion;
        PackageVersion = packageVersion;
        FirebirdRelease = firebirdRelease;
        _buildDate = new ChangeTrackingProperty<DateTimeOffset>(NotifyChange, buildDate);
        _publishDate = new NullableChangeTrackingProperty<DateTimeOffset>(NotifyChange, publishDate);

        void NotifyChange()
        {
            if (Metadata == null)
            {
                return;
            }

            Metadata.Changed = true;
        }
    }

    public static PackageRelease Initial(
        Rid rid,
        DateTimeOffset releaseDate,
        FirebirdVersion fbVersion,
        ReleaseVersion firebirdRelease,
        ReleaseVersion initialVersion) =>
    new (rid, releaseDate, fbVersion, initialVersion, firebirdRelease);

    public PackageRelease WithBuildDate(DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, FbVersion, PackageVersion, FirebirdRelease);

    public PackageRelease NextPatchVersion(DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, FbVersion, PackageVersion.NextPatchVersion(), FirebirdRelease);

    public PackageRelease NextProductVersion(ReleaseVersion productVersion, DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, FbVersion, PackageVersion.NextPatchVersion(), productVersion);

}
