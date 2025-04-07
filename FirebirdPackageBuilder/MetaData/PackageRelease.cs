namespace Std.FirebirdEmbedded.Tools.MetaData;

internal sealed class PackageRelease
{
    public Rid Rid { get; }
    public DateTimeOffset ReleaseDate { get; }
    public FirebirdVersion FbVersion { get; }
    public ReleaseVersion PackageVersion { get; }
    public ReleaseVersion FirebirdRelease { get; }

    public PackageRelease(
        Rid rid,
        DateTimeOffset releaseDate,
        FirebirdVersion fbVersion,
        ReleaseVersion packageVersion,
        ReleaseVersion firebirdRelease)
    {
        Rid = rid;
        ReleaseDate = releaseDate;
        FbVersion = fbVersion;
        PackageVersion = packageVersion;
        FirebirdRelease = firebirdRelease;
    }

    public static PackageRelease Initial(
        Rid rid,
        DateTimeOffset releaseDate,
        FirebirdVersion fbVersion,
        ReleaseVersion firebirdRelease) =>
    new (rid, releaseDate, fbVersion, new ReleaseVersion(1,0,1), firebirdRelease);

    public PackageRelease WithReleaseDate(DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, FbVersion, PackageVersion, FirebirdRelease);

    public PackageRelease NextPatchVersion(DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, FbVersion, PackageVersion.NextPatchVersion(), FirebirdRelease);

    public PackageRelease NextProductVersion(ReleaseVersion productVersion, DateTimeOffset releaseDate) =>
        new (Rid, releaseDate, FbVersion, PackageVersion.NextPatchVersion(), productVersion);

}
