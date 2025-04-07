namespace Std.FirebirdEmbedded.Tools;

internal enum VersionStyle
{
    Firebird,
    Nuget
}

internal readonly record struct ReleaseVersion(
    uint Major,
    uint Minor,
    uint Patch,
    uint Build = 0,
    uint Revision = 0
) : IComparable<ReleaseVersion>, IComparable
{
    public static readonly ReleaseVersion Nil = new ();

    public override string ToString() => ToString(VersionStyle.Firebird);

    public string ToString(VersionStyle style) =>
        style switch
        {
            VersionStyle.Nuget => $"{Major}.{Minor}.{Patch}",
            VersionStyle.Firebird => $"{Major}.{Minor}.{Patch}.{Build}-{Revision}",
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };

//    public string AsNugetVersion(bool prerelease) => $"{Major}.{Minor}.{Patch}-{(prerelease ? $"pre{Revision}" : Revision)}";

    public ReleaseVersion NextMajorVersion() =>
        this with
        {
            Major = Major + 1
        };

    public ReleaseVersion NextPatchVersion() =>
        this with
        {
            Patch = Patch + 1
        };

    public static bool TryParse(string value, out ReleaseVersion? releaseVersion, VersionStyle style = VersionStyle.Firebird)
    {
        releaseVersion = null;
        var parts = value.Split('.');

        if (parts.Length < 3)
        {
            return false;
        }

        if (!uint.TryParse(parts[0], out var major) ||
            !uint.TryParse(parts[1], out var minor) ||
            !uint.TryParse(parts[2], out var patch))
        {
            return false;
        }

        if (parts.Length == 3)
        {
            if (style == VersionStyle.Firebird)
            {
                return false;
            }
            releaseVersion = new ReleaseVersion(major, minor, patch, 0, 0);
            return true;
        }

        var buildParts = parts[3].Split('-');
        if (buildParts.Length != 2)
        {
            return false;
        }

        if (!uint.TryParse(buildParts[0], out var build) ||
            !uint.TryParse(buildParts[1], out var revision))
        {
            return false;
        }

        releaseVersion = new ReleaseVersion(major, minor, patch, build, revision);
        return true;
    }

    public int CompareTo(ReleaseVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
        {
            return patchComparison;
        }

        var buildComparison = Build.CompareTo(other.Build);
        if (buildComparison != 0)
        {
            return buildComparison;
        }

        return Revision.CompareTo(other.Revision);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        return obj is ReleaseVersion other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(ReleaseVersion)}");
    }

    public static bool operator <(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(ReleaseVersion left, ReleaseVersion right) => left.CompareTo(right) >= 0;
}
