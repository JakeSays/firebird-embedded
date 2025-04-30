namespace Std.FirebirdEmbedded.Tools.Templates;

internal partial class TargetFileTemplate
{
    public string Generate(string tfm, FirebirdAsset asset, bool transitive)
    {
        Release = asset.Release;
        Assets = [asset];
        TargetPlatform = asset.Platform;

        return DoGenerate(asset, tfm, transitive);
    }

    public string Generate(IPackageDetails packageDetails, string tfm, FirebirdAsset[] assets, bool transitive)
    {
        Release = packageDetails.Release;
        Assets = assets;
        TargetPlatform = assets[0].Platform;

        return DoGenerate(packageDetails, tfm, transitive);
    }

    private string DoGenerate(IPackageDetails packageDetails, string tfm, bool transitive)
    {
        Tfm = tfm;

        var targetFile = TransformText();

        var targetDir = transitive
            ? "buildTransitive"
            : "build";
        var outputDir = Path.Combine(packageDetails.PackageRootDirectory, targetDir, tfm);
        Directory.CreateDirectory(outputDir);
        var fileName = packageDetails.PackageId + ".targets";
        var outputPath = Path.Combine(outputDir, fileName);
        File.WriteAllText(outputPath, targetFile);

        return Path.Combine(targetDir, tfm, fileName);
    }
}
