namespace Std.CommandLine.Invocation
{
    internal enum MiddlewareOrderInternal
    {
        Startup = -4000,
        ExceptionHandler = -3000,
        ConfigureConsole = -2500,
        RegisterWithDotnetSuggest = -2400,
        DebugDirective = -2300,
        ParseDirective = -2200,
        SuggestDirective = -2000,
        TypoCorrection = -1900,
        VersionOption = -1200,
        HelpOption = -1100,
        ParseErrorReporting = 1000,
    }
}
