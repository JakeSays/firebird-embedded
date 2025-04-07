namespace PackageTester;

public class Junk
{
    void CopyFiles(string sourceDirectory, string destDirectory, string rid)
    {
        // Log.LogMessage(MessageImportance.High, $"Source directory: {sourceDirectory}, Dest directory: {destDirectory}");
        // sourceDirectory = Path.Combine(sourceDirectory, rid);
        // destDirectory = Path.Combine(destDirectory, rid);
        // Log.LogMessage(MessageImportance.High, $"Source directory2: {sourceDirectory}, Dest directory2: {destDirectory}");

        var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

        var sourceDirLength = sourceDirectory.Length;
        foreach(var file in files)
        {
            var relativeDest = file.Substring(sourceDirLength);
            var destPath = Path.Combine(destDirectory, relativeDest);
            //Log.LogMessage(MessageImportance.High, $"relativeDest: {relativeDest}, destPath: {destPath}");
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, true);

            // Log.LogMessage(MessageImportance.High,
            //     $"Copied {file} to {destPath}");
        }
    }

}
