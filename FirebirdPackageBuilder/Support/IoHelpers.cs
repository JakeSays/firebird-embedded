namespace Std.FirebirdEmbedded.Tools.Support;

internal static class IoHelpers
{
    public static void RecreateDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        Directory.CreateDirectory(directory);
    }
}
