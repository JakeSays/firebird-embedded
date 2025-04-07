namespace Std.FirebirdEmbedded.Tools.Templates;

internal partial class ReadmeTemplate
{
    public void Generate(FirebirdAsset asset)
    {
        FbVersion = asset.Version.ToString();
        Platform = asset.Platform.ToString();
        Architecture = [asset.Architecture.ToString()];
        Content = asset.Release.ReleaseNotes;

        var readme = TransformText();

        var readmePath = Path.Combine(asset.PackageRootDirectory, "README.md");
        File.WriteAllText(readmePath, (string?)readme);
    }

    public void Generate(IPackageDetails details)
    {
        FbVersion = details.Release.ReleaseVersion.ToString();
        Platform = details.Rid.Platform.ToString();
        Architecture = [..details.Architectures.Select(a => a.ToString())];
        Content = details.Release.ReleaseNotes;

        var readme = TransformText();

        var readmePath = Path.Combine(details.PackageRootDirectory, "README.md");
        File.WriteAllText(readmePath, (string?)readme);
    }
}
