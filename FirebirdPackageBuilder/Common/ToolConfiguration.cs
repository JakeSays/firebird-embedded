using Std.FirebirdEmbedded.Tools.MetaData;


namespace Std.FirebirdEmbedded.Tools.Common;

internal class ToolConfiguration
{
    public string? RepositoryRoot { get; }
    public string PackageOutputDirectory { get; }
    public string MetadataFilePath { get; }

    public ToolConfiguration(
        string? repositoryRoot,
        string? packageDirectory,
        string? metadataFilePath)
    {
        RepositoryRoot = repositoryRoot;
        PackageOutputDirectory = MakePath(packageDirectory, "output");
        MetadataFilePath = MakePath(metadataFilePath, PackageMetadata.MetadataFileName);
    }

    private protected string MakePath(string? part, string defaultPart)
    {
        part ??= defaultPart;
        if (Path.IsPathRooted(part))
        {
            return part;
        }

        if (RepositoryRoot == null)
        {
            throw new InvalidOperationException($"RepositoryRoot is null, cannot make absolute path for '{part}'");
        }
        return Path.GetFullPath(part, RepositoryRoot);
    }
}
