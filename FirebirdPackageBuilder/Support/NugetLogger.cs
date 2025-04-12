using System.Reflection;
using NuGet.Common;


namespace Std.FirebirdEmbedded.Tools.Support;

internal sealed class NugetLogger : ILogger
{
    public static readonly string LocalizedPushSuccessfulString = "Your package was pushed.";
    private static NugetLogger? _instance;

    private readonly Func<LogLevel, string, bool>? _messageFilter;

    static NugetLogger()
    {
        var assy = typeof(NuGet.Protocol.CachingSourceProvider).Assembly;
        if (assy == null!)
        {
            return;
        }
        var stringsType = assy.GetType("NuGet.Protocol.Strings");
        if (stringsType == null)
        {
            return;
        }

        var stringProp = stringsType.GetProperty("PushCommandPackagePushed", BindingFlags.Static | BindingFlags.NonPublic);
        if (stringProp == null)
        {
            return;
        }

        var value = stringProp.GetValue(null) as string;
        if (value == null)
        {
            return;
        }

        LocalizedPushSuccessfulString = value;
    }

    public Verbosity Verbosity { get; }

    public void LogDebug(string data) => DoLog(LogLevel.Debug, data);

    public void LogVerbose(string data) => DoLog(LogLevel.Verbose, data);

    public void LogInformation(string data) => DoLog(LogLevel.Information, data);

    public void LogMinimal(string data) => DoLog(LogLevel.Minimal, data);

    public void LogWarning(string data) => DoLog(LogLevel.Warning, data);

    public void LogError(string data) => DoLog(LogLevel.Error, data);

    public void LogInformationSummary(string data) => DoLog(LogLevel.Information, data);

    public static NugetLogger Instance => _instance ?? new NugetLogger(Verbosity.Normal, null);

    public static void InitializeDefault(Verbosity verbosity) =>
        _instance = Create(verbosity);

    public static NugetLogger Create(Verbosity verbosity, Func<LogLevel, string, bool>? filter = null)
    {
        return new NugetLogger(verbosity, filter);
    }

    private NugetLogger(Verbosity verbosity, Func<LogLevel, string, bool>? filter)
    {
        Verbosity = verbosity;
        _messageFilter = filter;
    }

    public void Log(LogLevel level, string data)
    {
        DoLog(level, data);
    }

    public Task LogAsync(LogLevel level, string data)
    {
        DoLog(level, data);
        return Task.CompletedTask;
    }

    public void Log(ILogMessage message)
    {
        DoLog(message.Level, message.Message, message);
    }

    public Task LogAsync(ILogMessage message)
    {
        DoLog(message.Level, message.Message, message);
        return Task.CompletedTask;
    }

    private void DoLog(LogLevel level, string message, ILogMessage? logMessage = null)
    {
        if (_messageFilter?.Invoke(level, message) == true)
        {
            return;
        }

        var shouldLog = level switch
        {
            LogLevel.Debug when Verbosity >= Verbosity.Loud => true,
            LogLevel.Verbose when Verbosity >= Verbosity.Nagging => true,
            LogLevel.Minimal when Verbosity > Verbosity.Silent => true,
            LogLevel.Information when Verbosity >= Verbosity.Normal => true,
            LogLevel.Warning when Verbosity > Verbosity.Silent => true,
            LogLevel.Error => true,
            _ => false
        };

        if (!shouldLog)
        {
            return;
        }

        var color = Color(level);
        var prefix = Prefix(level);

        var warnText = level == LogLevel.Warning && logMessage != null
            ? $" ({logMessage.WarningLevel})"
            : "";
        var codeText = logMessage != null && logMessage.Code != NuGetLogCode.Undefined
            ? $" {logMessage.Code}"
            : "";

        StdOut.WriteLine(color, $"{prefix}{codeText}{warnText}: {message}");
    }

    private static string Prefix(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => "[DEBUG] ",
            LogLevel.Verbose => "[VERB] ",
            LogLevel.Minimal => "[INFO] ",
            LogLevel.Information => "[INFO] ",
            LogLevel.Warning => "[WARN] ",
            LogLevel.Error => "[ERROR] ",
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
