using Std.FirebirdEmbedded.Tools.Support;


namespace Std.FirebirdEmbedded.Tools;

internal enum Verbosity
{
    Silent,
    Normal,
    Loud,
    Nagging
}

internal static class ConsoleConfig
{
    public static Verbosity Verbosity { get; private set; }

    public static void Initialize(Verbosity verbosity)
    {
        Verbosity = verbosity;
        NugetLogger.InitializeDefault(verbosity);
    }

    public static bool IsSilent => Verbosity == Verbosity.Silent;
    public static bool IsNormal => Verbosity >= Verbosity.Normal;
    public static bool IsLoud => Verbosity == Verbosity.Loud;
    public static bool IsNaggy => Verbosity == Verbosity.Nagging;
}
