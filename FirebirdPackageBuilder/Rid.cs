using System.Diagnostics.CodeAnalysis;


namespace Std.FirebirdEmbedded.Tools;

internal struct Rid : IEquatable<Rid>
{
    public override string ToString() => ToStringFull();

    public static Rid All => new(Platform.All);

    public Rid(Platform platform, Architecture architecture = Architecture.All)
    {
        Platform = platform;
        Architecture = architecture;
    }

    public Platform Platform { get; }
    public Architecture Architecture { get; }
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

    public bool Equals(Rid other) => Platform == other.Platform && Architecture == other.Architecture;

    public override bool Equals(object? obj) => obj is Rid other && Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)Platform, (int)Architecture);

    public static bool operator ==(Rid left, Rid right) => left.Equals(right);

    public static bool operator !=(Rid left, Rid right) => !left.Equals(right);
}
