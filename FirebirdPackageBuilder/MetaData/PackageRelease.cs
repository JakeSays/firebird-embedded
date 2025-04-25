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

    public ProductId Product { get; }
    public ReleaseVersion PackageVersion { get; }
    public ReleaseVersion FirebirdRelease { get; }

    public PackageMetadata? Metadata { get; set; }

    public PackageRelease(
        Rid rid,
        DateTimeOffset buildDate,
        ProductId product,
        ReleaseVersion packageVersion,
        ReleaseVersion? firebirdRelease,
        DateTimeOffset? publishDate = null)
    {
        Rid = rid;
        Product = product;
        PackageVersion = packageVersion;
        FirebirdRelease = firebirdRelease ?? ReleaseVersion.Nil;
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
        ProductId fbVersion,
        ReleaseVersion? firebirdRelease,
        ReleaseVersion initialVersion) =>
    new (rid, releaseDate, fbVersion, initialVersion, firebirdRelease ?? ReleaseVersion.Nil);

    public PackageRelease WithBuildDate(DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, Product, PackageVersion, FirebirdRelease);

    public PackageRelease NextPatchVersion(DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, Product, PackageVersion.NextPatchVersion(), FirebirdRelease);

    public PackageRelease NextProductVersion(ReleaseVersion productVersion, DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, Product, PackageVersion.NextMajorVersion(), productVersion);

}
