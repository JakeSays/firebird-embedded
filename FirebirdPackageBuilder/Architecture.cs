namespace Std.FirebirdEmbedded.Tools;

internal enum Architecture
{
    X32,
    X64,
    Arm32,
    Arm64,

    //used only for meta packages
    All
}

internal static class ArchitectureExtensions
{
    public static string Name(this Architecture architecture) =>
        architecture switch
        {
            Architecture.X32 => "x32",
            Architecture.X64 => "x64",
            Architecture.Arm32 => "arm32",
            Architecture.Arm64 => "arm64",
            Architecture.All => "all",
            _ => throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null)
        };

    public static string MsBuildName(this Architecture architecture) =>
        architecture switch
        {
            Architecture.X32 => "X86",
            Architecture.X64 => "X64",
            Architecture.Arm32 => "Arm32",
            Architecture.Arm64 => "Arm64",
            Architecture.All => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null)
        };

    public static string RidSuffix(this Architecture architecture, Platform platform) =>
        architecture switch
        {
            Architecture.X32 when platform == Platform.Windows => "x86",
            Architecture.X32 => "x32",
            Architecture.X64 => "x64",
            Architecture.Arm32 => "arm",
            Architecture.Arm64 => "arm64",
            Architecture.All => "all",
            _ => throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null)
        };

    public static bool TryParseArchitecture(this string value, out Architecture architecture)
    {
        switch (value.ToLowerInvariant())
        {
            case "x32":
            case "x86":
                architecture = Architecture.X32;
                return true;
            case "x64":
                architecture = Architecture.X64;
                return true;
            case "arm32":
            case "arm":
                architecture = Architecture.Arm32;
                return true;
            case "arm64":
                architecture = Architecture.Arm64;
                return true;
            case "all":
                architecture = Architecture.All;
                return true;
        }

        architecture = Architecture.X64;
        return false;
    }
}
