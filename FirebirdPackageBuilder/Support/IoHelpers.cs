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

    public static bool SafeCreateDirectory(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        try
        {
            Directory.CreateDirectory(directory);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
