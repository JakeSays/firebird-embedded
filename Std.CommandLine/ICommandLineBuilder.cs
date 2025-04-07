using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Std.CommandLine.Arguments;
using Std.CommandLine.Commands;
using Std.CommandLine.Options;


namespace Std.CommandLine
{
    public interface ICommandLineBuilder
        : HandlerProvider<ICommandLineBuilder>
    {
        void Build();

        TResult? ParseCommandLine<TResult>()
            where TResult : class, new();

        TResult? ParseCommandLine<TResult>(out IReadOnlyList<string> unmatchedArgs)
            where TResult : class, new();

        ICommandLineBuilder UnmatchedArgsAreErrors();
        ICommandLineBuilder Command(Action<CommandBuilder> config);
        ICommandLineBuilder Argument<TArg>(Action<ArgumentBuilder<TArg>> config);
        ICommandLineBuilder GlobalOption<TOpt>(Action<OptionBuilder<TOpt>> config);
        ICommandLineBuilder Option<TOpt>(Action<OptionBuilder<TOpt>> config);

        // ScriptApplicationBuilder FileArg(Action<FileSystemArgumentBuilder<FileInfo>> config);
        // ScriptApplicationBuilder? DirectoryArg(Action<FileSystemArgumentBuilder<DirectoryInfo>> config);

        ICommandLineBuilder EnableDirectives();
        ICommandLineBuilder EnablePosixBundling();
        ICommandLineBuilder SpaceSeparatedResponseFile();
        ICommandLineBuilder LineSeparatedResponseFile();
        ICommandLineBuilder NoResponseFile();
        ICommandLineBuilder WithHelp();

        ICommandLineBuilder WithVersion(string version);
        ICommandLineBuilder WithVersion(Assembly versionedAssembly);
        ICommandLineBuilder WithVersion(Func<string> versionProvider);
//        ScriptApplicationBuilder EnableWaitForDebugger();
        ICommandLineBuilder CancelOnProcessTermination();
        ICommandLineBuilder WithExceptionHandler(Func<Exception,
            (ExceptionBehavior Behavior, int TerminateExitCode)>? exceptionHandler);
        ICommandLineBuilder Flag(Action<FlagOptionBuilder> config);
        ICommandLineBuilder GlobalFlag(Action<FlagOptionBuilder> config);
    }
}
