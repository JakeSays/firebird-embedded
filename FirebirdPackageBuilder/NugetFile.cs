namespace Std.FirebirdEmbedded.Tools;

internal enum NugetDestination
{
    Runtime,
    Content
}

internal record NugetFile(
    NugetDestination Destination,
    string SourcePath
);
