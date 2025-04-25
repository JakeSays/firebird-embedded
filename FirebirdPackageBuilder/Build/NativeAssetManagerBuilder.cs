using System.Diagnostics;


namespace Std.FirebirdEmbedded.Tools.Build;

internal static class NativeAssetManagerBuilder
{
    public static bool BuildPackage(string repositoryRoot, ReleaseVersion packageVersion)
    {
        const string assetManagerBaseName = "FirebirdDb.Embedded.NativeAssetManager";

        var projectPath = Path.Combine(repositoryRoot, assetManagerBaseName, $"{assetManagerBaseName}.csproj");

        var (result, output) = ExecuteBuild(projectPath, packageVersion);

        return result == 0;
    }

    private static (int ExitCode, IReadOnlyList<string> Output) ExecuteBuild(string path, ReleaseVersion packageVersion)
    {
        using var process = StartBuild(path, packageVersion, out var output);
        process.WaitForExit();

        return (process.ExitCode, output);
    }

    private static Process StartBuild(string path, ReleaseVersion packageVersion, out List<string> output)
    {
        output = [];
        var dotnet = FindDotNet();
        if (dotnet == null)
        {
            throw new FileNotFoundException("Cannot find the dotnet binary");
        }
        var process = new Process
        {
            StartInfo =
            {
                FileName = dotnet,
                ArgumentList =
                {
                    "msbuild",
                    "-target:Rebuild",
                    "-property:Configuration=Release",
                    $"-property:AmCurrentVersion={packageVersion.ToString(VersionStyle.Nuget)}",
                    path
                },
                RedirectStandardOutput = true,

            }
        };

        var outputForClosure = output;
        process.OutputDataReceived += (_, data) =>
        {
            if (data.Data is null)
            {
                return;
            }

            outputForClosure.Add(data.Data);

            if (!ConsoleConfig.IsNaggy)
            {
                return;
            }
            StdOut.NormalLine(data.Data);
        };

        StdOut.GreenLine("Building " + path);
        process.Start();
        process.BeginOutputReadLine();

        return process;
    }

    private static string? FindDotNet()
    {
        var exeName = OperatingSystem.IsWindows()
            ? "dotnet.exe"
            : "dotnet";

        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (dotnetRoot != null)
        {
            var path = Path.Combine(dotnetRoot, exeName);
            if (File.Exists(path) &&
                (File.GetAttributes(path) & FileAttributes.Normal) != 0)
            {
                return path;
            }
        }

        var paths = Environment.GetEnvironmentVariable("PATH")
            !.Split(":");

        foreach (var root in paths)
        {
            var path = Path.Combine(root, exeName);
            if (File.Exists(path) &&
                (File.GetAttributes(path) & FileAttributes.Normal) != 0)
            {
                return path;
            }
        }

        return null;
    }
}
