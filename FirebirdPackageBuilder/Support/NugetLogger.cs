using NuGet.Common;


namespace Std.FirebirdEmbedded.Tools.Support;

internal sealed class NugetLogger : ILogger
{
    public void LogDebug(string data) => Log(LogLevel.Debug, data);

    public void LogVerbose(string data) => Log(LogLevel.Verbose, data);

    public void LogInformation(string data) => Log(LogLevel.Information, data);

    public void LogMinimal(string data) => Log(LogLevel.Minimal, data);

    public void LogWarning(string data) => Log(LogLevel.Warning, data);

    public void LogError(string data) => Log(LogLevel.Error, data);

    public void LogInformationSummary(string data) => Log(LogLevel.Information, data);

    public void Log(LogLevel level, string data)
    {
        var color = Color(level);
        var prefix = Prefix(level);

        StdOut.WriteLine(color, $"{prefix} {data}");
    }

    public Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
        return Task.CompletedTask;
    }

    public void Log(ILogMessage message)
    {
        var color = Color(message.Level);
        var prefix = Prefix(message.Level);

        var warnText = message.Level == LogLevel.Warning
            ? $" ({message.WarningLevel})"
            : "";
        var codeText = message.Code != NuGetLogCode.Undefined
            ? $" {message.Code}"
            : "";

        StdOut.WriteLine(color, $"{prefix}{codeText}{warnText}: {message.Message}");
    }

    public Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }

    private static string Prefix(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Verbose => "[VERB] ",
            LogLevel.Minimal => "[INFO] ",
            LogLevel.Information => "[INFO] ",
            LogLevel.Warning => "[WARN] ",
            LogLevel.Error => "[ERROR]",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };

    private static ConsoleColor Color(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => ConsoleColor.Green,
            LogLevel.Verbose => ConsoleColor.DarkCyan,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Minimal => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
}
