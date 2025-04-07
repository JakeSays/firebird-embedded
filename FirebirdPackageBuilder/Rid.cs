namespace Std.FirebirdEmbedded.Tools;

internal readonly record struct Rid(Platform Platform, Architecture Architecture = Architecture.All)
{
    public override string ToString() =>
        Architecture != Architecture.All
            ? ToStringFull()
            : Platform.RidPrefix();

    public string ToStringFull() => $"{Platform.RidPrefix()}-{Architecture.Name()}";

    public string PackageText => $"{Platform.RidPrefix()}-{Architecture.RidSuffix(Platform)}";

    public string DisplayName => $"{Platform}.{Architecture}";

    public static bool TryParse(string value, out Rid rid)
    {
        var parts = value.Split('-');
        if (parts.Length != 2)
        {
            if (parts.Length == 1 &&
                parts[0].TryParsePlatform(out var platformOnly))
            {
                rid = new Rid(platformOnly);
                return true;
            }
            rid = new Rid();
            return false;
        }

        if (!parts[0].TryParsePlatform(out var platform) ||
            !parts[1].TryParseArchitecture(out var architecture))
        {
            rid = new Rid();
            return false;
        }

        rid = new Rid(platform, architecture);
        return true;
    }
}
