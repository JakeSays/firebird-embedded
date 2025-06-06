namespace Std.FirebirdEmbedded.Tools;

internal interface IPackageDetails
{
    Rid Rid { get; }
    string PackageId { get; }
    string PackageRootDirectory { get; }
    FirebirdRelease Release { get; }
    public string LicensesFileName { get; }
    Architecture[] Architectures { get; }
}
