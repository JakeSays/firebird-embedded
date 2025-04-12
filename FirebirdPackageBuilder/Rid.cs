using System.Diagnostics.CodeAnalysis;


namespace Std.FirebirdEmbedded.Tools;

internal record struct Rid(Platform Platform, Architecture Architecture = Architecture.All)
{
    public override string ToString() =>
        Architecture != Architecture.All
            ? ToStringFull()
            : Platform.RidPrefix();

    public string ToStringFull() => $"{Platform.RidPrefix()}-{Architecture.Name()}";

    [field: AllowNull, MaybeNull]
    public string PackageText => field ??= $"{Platform.RidPrefix()}-{Architecture.RidSuffix(Platform)}";

    [field: AllowNull, MaybeNull]
    public string DisplayName => field ??= $"{Platform}.{Architecture}";

    public static bool TryParse(string platformText, string? architectureText, out Rid rid)
    {
        ArgumentNullException.ThrowIfNull(platformText);

        if (!platformText.TryParsePlatform(out var platform))
        {
            rid = new Rid();
            return false;
        }

        if (architectureText == null)
        {
            rid = new Rid(platform);
            return true;
        }

        if (!architectureText.TryParseArchitecture(out var arch))
        {
            rid = new Rid();
            return false;
        }

        rid = new Rid(platform, arch);
        return true;
    }

    public static bool TryParse(string value, out Rid rid)
    {
        var parts = value.Split('-');
        switch (parts.Length)
        {
            case 2:
                return TryParse(parts[0], parts[1], out rid);
            case 1:
                return TryParse(value, null, out rid);
            default:
                rid = new Rid();
                return false;
        }
    }
}
