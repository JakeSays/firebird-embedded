namespace Std.FirebirdEmbedded.Tools.Build;

internal enum NugetDestination
{
    Runtime,
    Content
}

internal record NugetFile(
    NugetDestination Destination,
    string SourcePath
);
