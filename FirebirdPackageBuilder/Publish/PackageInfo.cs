using System.Text.RegularExpressions;
using Std.FirebirdEmbedded.Tools.MetaData;


namespace Std.FirebirdEmbedded.Tools.Publish;

internal sealed partial class PackageInfo
{
    public string Name { get; }
    public string Path { get; }
    public ReleaseVersion PackageVersion { get; }
    public ProductId Product { get; }
    public Rid Rid { get; }
    public PackageRelease? Release { get; set; }

    private PackageInfo(string path, ReleaseVersion packageVersion, ProductId product, Rid rid)
    {
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Path = path;
        PackageVersion = packageVersion;
        Product = product;
        Rid = rid;
    }

    public static PackageInfo Parse(string packagePath)
    {
        ArgumentNullException.ThrowIfNull(packagePath);

        var match = PathParser.Match(packagePath);
        if (!match.Success)
        {
            return VersionOnlyParse(packagePath);
        }

        var product = match.Groups["ver"].Value
            switch
            {
                "3" => ProductId.V3,
                "4" => ProductId.V4,
                "5" => ProductId.V5,
                _ => throw new ArgumentOutOfRangeException(nameof(packagePath), "Invalid version")
            };

        var pkgVersion = new ReleaseVersion(
            uint.Parse(match.Groups["major"].Value),
            uint.Parse(match.Groups["minor"].Value),
            uint.Parse(match.Groups["patch"].Value));

        if (!Rid.TryParse(
            match.Groups["platform"].Value,
            match.Groups["arch"].Value, out var rid))
        {
            throw new InvalidOperationException($"Invalid package path '{packagePath}'");
        }

        return new PackageInfo(packagePath, pkgVersion, product, rid);
    }

    private static PackageInfo VersionOnlyParse(string packagePath)
    {
        var match = VersionOnlyPathParser.Match(packagePath);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Invalid package path '{packagePath}'");
        }
        var pkgVersion = new ReleaseVersion(
            uint.Parse(match.Groups["major"].Value),
            uint.Parse(match.Groups["minor"].Value),
            uint.Parse(match.Groups["patch"].Value));

        return new PackageInfo(packagePath, pkgVersion, ProductId.AssetManager, new Rid(Platform.All));
    }

    [GeneratedRegex(@"V(?<ver>\d)\.(?:[^\.]+)\.(?<platform>[^\.]+)\.(?<arch>[^\.]+)\.(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)",
        RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex PathParser { get; }

    [GeneratedRegex(@"\.(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)",
        RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex VersionOnlyPathParser { get; }
}
