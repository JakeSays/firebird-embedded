namespace Std.FirebirdEmbedded.Tools.Build;

internal enum BuildType
{
    /// <summary>
    /// Only builds packages if there is a new release
    /// from firebird. Causes a package major version bump.
    /// </summary>
    Normal,
    /// <summary>
    /// Rebuilds packages using existing firebird releases.
    /// Causes all packages to be rebuilt and a patch
    /// version bump.
    /// </summary>
    Rebuild,
    /// <summary>
    /// Forces rebuild of all packages at the existing version numbers.
    /// Mainly used for test/development.
    /// </summary>
    Force
}
