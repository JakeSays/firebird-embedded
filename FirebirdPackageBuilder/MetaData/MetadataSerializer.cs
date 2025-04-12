using System.Xml.Linq;


namespace Std.FirebirdEmbedded.Tools.MetaData;

internal static class MetadataSerializer
{
    public static PackageMetadata? Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            return new PackageMetadata();
        }

        try
        {
            var root = XElement.Load(path);
            if (root.Name != RootEl)
            {
                Expected(RootEl, root);
                return null;
            }

            var metadata = new PackageMetadata();

            HashSet<FirebirdVersion> seenVersions = [];
            foreach (var el in root.Elements())
            {
                if (!ParseReleaseHistory(el, metadata, out var version))
                {
                    return null;
                }

                if (!seenVersions.Add((FirebirdVersion) version!))
                {
                    StdErr.RedLine($"Duplicate release history elements for version {version}.");
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Error loading metadata from '{path}': {ex.Message}");
            return null;
        }
    }

    public static bool Save(PackageMetadata metadata, string path)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            var xml = new XElement(
                RootEl,
                SerializeReleaseHistory(metadata.V3Releases, FirebirdVersion.V3),
                SerializeReleaseHistory(metadata.V4Releases, FirebirdVersion.V4),
                SerializeReleaseHistory(metadata.V5Releases, FirebirdVersion.V5));

            xml.Save(path, SaveOptions.OmitDuplicateNamespaces);
            return true;
        }
        catch (Exception ex)
        {
            StdErr.RedLine($"Error saving metadata to '{path}': {ex.Message}");
            return false;
        }

        static XElement SerializeReleaseHistory(IReadOnlyList<PackageRelease> releases, FirebirdVersion version)
        {
            var element = new XElement(
                ReleaseHistoryEl,
                [new XAttribute(FirebirdVersionAttr, version), ..releases.Select(SerializeRelease)]);

            return element;

            static XElement SerializeRelease(PackageRelease release)
            {
                const string dateFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz";

                var el = new XElement(
                    ReleaseEl,
                    new XAttribute(RidAttr, release.Rid),
                    new XAttribute(PackageVersionAttr, release.PackageVersion.ToString(VersionStyle.Nuget)),
                    new XAttribute(NativeAssetsVersionAttr, release.FirebirdRelease),
                    new XAttribute(BuildDateAttr, release.BuildDate.ToString(dateFormat)));
                if (release.PublishDate != null)
                {
                    el.Add(new XAttribute(PublishDateAttr, release.PublishDate.Value.ToString(dateFormat)));
                }
                return el;
            }
        }
    }

    private static bool ParseReleaseHistory(XElement xml, PackageMetadata metadata, out FirebirdVersion? fbVersion)
    {
        fbVersion = null;
        if (xml.Name != ReleaseHistoryEl)
        {
            Expected(ReleaseHistoryEl, xml);
            return false;
        }

        fbVersion = xml.EnumAttr<FirebirdVersion>(FirebirdVersionAttr);
        if (fbVersion is null)
        {
            IlFormed(xml, "Invalid or missing firebird version");
            return false;
        }

        foreach (var release in ParseReleases((FirebirdVersion) fbVersion, xml))
        {
            if (release == null)
            {
                return false;
            }

            metadata.AddRelease(release, loading: true);
        }

        return true;
    }

    private static IEnumerable<PackageRelease?> ParseReleases(FirebirdVersion fbVersion, XElement xml)
    {
        foreach (var releaseXml in xml.Elements())
        {
            if (releaseXml.Name != ReleaseEl)
            {
                Expected(ReleaseEl, releaseXml);
                yield return null;
                break;
            }

            var buildDate = (DateTimeOffset?) releaseXml.Attribute(BuildDateAttr);
            if (buildDate is null)
            {
                //try old attribute
                buildDate = (DateTimeOffset?) releaseXml.Attribute(PackageReleaseDateAttr);
                if (buildDate is null)
                {
                    IlFormed(xml, "Invalid or missing build date");
                    yield return null;
                    break;
                }
            }

            var publishDate = (DateTimeOffset?) releaseXml.Attribute(PublishDateAttr);

            var rid = ParseRid(releaseXml);
            if (rid is null)
            {
                yield return null;
                break;
            }

            var packageVersion = ParseReleaseVersion(releaseXml, PackageVersionAttr);
            if (packageVersion is null)
            {
                yield return null;
                break;
            }

            var nativeAssetsVersion = ParseReleaseVersion(releaseXml, NativeAssetsVersionAttr);
            if (nativeAssetsVersion is null)
            {
                yield return null;
                break;
            }

            var releaseInfo = new PackageRelease(
                (Rid) rid,
                (DateTimeOffset) buildDate,
                fbVersion,
                (ReleaseVersion) packageVersion,
                (ReleaseVersion) nativeAssetsVersion,
                publishDate);

            yield return releaseInfo;
        }


        static Rid? ParseRid(XElement xml)
        {
            var value = (string?) xml.Attribute(RidAttr);
            if (value is null)
            {
                IlFormed(xml, "Invalid or missing rid");
                return null;
            }

            if (Rid.TryParse(value, out var rid))
            {
                return rid;
            }

            return null;
        }

        static ReleaseVersion? ParseReleaseVersion(XElement xml, XName name)
        {
            var attrValue = (string?) xml.Attribute(name);
            if (attrValue is null)
            {
                MissingAttr(name, xml);
                return null;
            }

            var style = name == PackageVersionAttr
                ? VersionStyle.Nuget
                : VersionStyle.Firebird;

            if (ReleaseVersion.TryParse(attrValue, out var version, style))
            {
                return version;
            }

            IlFormed(xml, $"Invalid version '{name.LocalName}': {attrValue}'");
            return null;
        }
    }

    private static TEnum? EnumAttr<TEnum>(this XElement el, XName name)
        where TEnum : struct, Enum
    {
        if (!typeof(TEnum).IsEnum)
        {
            throw new InvalidOperationException("Type parameter TEnum is not valid");
        }

        var value = el.Attribute(name);

        if (value != null &&
            Enum.TryParse(value.Value, out TEnum vv))
        {
            return vv;
        }

        return null;
    }

    private static void Expected(XName name, XObject actual)
    {
        IlFormed(actual, $"Expected '{name.LocalName}'");
    }

    private static void MissingAttr(XName name, XElement el) => IlFormed(el, $"Missing attribute '{name.LocalName}'");

    private static void IlFormed(XObject node, string message)
    {
        var elName = node switch
        {
            XElement el => el.Name.LocalName,
            XAttribute attr => attr.Name.LocalName,
            _ => "<unknown>"
        };

        var elType = node is XElement
            ? "Element"
            : "Attribute";
        StdErr.RedLine($"{elType} '{elName}': {message}.");
    }

    private static readonly XName RootEl = "VersionInfo";
    private static readonly XName ReleaseEl = "Release";
    private static readonly XName ReleaseHistoryEl = "ReleaseHistory";

    private static readonly XName FirebirdVersionAttr = "FirebirdVersion";
    private static readonly XName NativeAssetsVersionAttr = "NativeAssetsVersion";
    private static readonly XName BuildDateAttr = "BuildDate";
    private static readonly XName PublishDateAttr = "PublishDate";
    private static readonly XName PackageReleaseDateAttr = "PackageReleaseDate";
    private static readonly XName PackageVersionAttr = "PackageVersion";
    private static readonly XName RidAttr = "Rid";
}
