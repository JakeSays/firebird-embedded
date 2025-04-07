
using System;

#if FB_PACKAGE_BUILDER
namespace Std.FirebirdEmbedded.Tools;
#else
namespace FirebirdSql.Embedded;
#endif


#if FB_PACKAGE_BUILDER
internal
#else
public
#endif
enum FirebirdVersion
{
    V3,
    V4,
    V5
}

internal static class FirebirdVersionExtensions
{
    public static string NativeBinarySuffix(this FirebirdVersion version) =>
        version switch
        {
            FirebirdVersion.V3 => "12",
            FirebirdVersion.V4 => "13",
            FirebirdVersion.V5 => "13",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
}
