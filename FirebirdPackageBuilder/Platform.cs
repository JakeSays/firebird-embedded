namespace Std.FirebirdEmbedded.Tools;

internal enum Platform
{
    Windows,
    Linux,
}

internal static class PlatformExtensions
{
    public static string LowerCase(this Platform platform) => platform.ToString().ToLower();

    public static string RidPrefix(this Platform platform) =>
        platform switch
        {
            Platform.Windows => "win",
            Platform.Linux => "linux",
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };

    public static bool TryParsePlatform(this string platformString, out Platform platform)
    {
        switch (platformString.ToLowerInvariant())
        {
            case "win":
            case "windows":
                platform = Platform.Windows;
                return true;
            case "linux":
                platform = Platform.Linux;
                return true;
        }
        platform = Platform.Linux;
        return false;
    }
}
