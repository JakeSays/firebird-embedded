namespace Std.FirebirdEmbedded.Tools.Templates;

internal partial class TargetFileTemplate
{
    public string Generate(string tfm, FirebirdAsset asset)
    {
        Release = asset.Release;
        Assets = [asset];

        return DoGenerate(asset, tfm);
    }

    public string Generate(IPackageDetails packageDetails, string tfm, FirebirdAsset[] assets)
    {
        Release = packageDetails.Release;
        Assets = assets;

        return DoGenerate(packageDetails, tfm);
    }

    private string DoGenerate(IPackageDetails packageDetails, string tfm)
    {
        var targetFile = TransformText();

        var outputDir = Path.Combine(packageDetails.PackageRootDirectory, "buildTransitive", tfm);
        Directory.CreateDirectory(outputDir);
        var fileName = packageDetails.PackageId + ".targets";
        var outputPath = Path.Combine(outputDir, fileName);
        File.WriteAllText(outputPath, targetFile);

        return Path.Combine("buildTransitive", tfm, fileName);
    }
}
