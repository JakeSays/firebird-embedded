using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Std.CommandLine.Arguments;
using Std.CommandLine.Commands;
using Std.CommandLine.Help;
using Std.CommandLine.Invocation;
using Std.CommandLine.Options;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;
using JetBrains.Annotations;


// ReSharper disable ClassNeverInstantiated.Global


namespace Std.CommandLine
{
    [PublicAPI]
    internal sealed class CommandLineBuilder : ICommandLineBuilder
    {
        private static readonly Lazy<string> CsxRunVersion =
            new(() =>
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

                return assemblyVersionAttribute is null
                    ? assembly.GetName().Version?.ToString() ?? "unknown"
                    : assemblyVersionAttribute.InformationalVersion;
            });

        private readonly RootCommand _rootCommand;
        private readonly StdApplication _application;

        internal bool IsHostApplication { get; }

        internal CommandLineBuilder(StdApplication application, bool isHostApplication)
        {
            Guard.NotNull(application, nameof(application));
            IsHostApplication = isHostApplication;

            _rootCommand = application.RootCommand;
            _application = application;
        }

        public ICommandLineBuilder UnmatchedArgsAreErrors()
        {
            _application.TreatUnmatchedArgsAsErrors = true;
            return this;
        }

        public ICommandLineBuilder Command(Action<CommandBuilder> config)
        {
            Guard.NotNull(config, nameof(config));

            var cmd = new Command();
            _rootCommand.AddCommand(cmd);

            config(new CommandBuilder(cmd));

            return this;
        }

        public ICommandLineBuilder Argument<TArg>(Action<ArgumentBuilder<TArg>> config)
        {
            Guard.NotNull(config, nameof(config));

            var arg = new Argument<TArg>();
            _rootCommand.AddArgument(arg);

            config(new ArgumentBuilder<TArg>(arg));

            return this;
        }

        public ICommandLineBuilder Flag(Action<FlagOptionBuilder> config)
        {
            Guard.NotNull(config, nameof(config));

            var opt = new FlagOption();
            _rootCommand.AddOption(opt);

            config(new FlagOptionBuilder(opt));

            return this;
        }

        public ICommandLineBuilder GlobalFlag(Action<FlagOptionBuilder> config)
        {
            Guard.NotNull(config, nameof(config));

            var opt = new FlagOption();
            _rootCommand.AddOption(opt);

            config(new FlagOptionBuilder(opt));

            return this;
        }

        public ICommandLineBuilder FileArg(Action<FileSystemArgumentBuilder<FileInfo>> config)
        {
            Guard.NotNull(config, nameof(config));

            var arg = new Argument<FileInfo>();
            _rootCommand.AddArgument(arg);

            config(new FileSystemArgumentBuilder<FileInfo>(arg));

            return this;
        }

        public ICommandLineBuilder DirectoryArg(Action<FileSystemArgumentBuilder<DirectoryInfo>> config)
        {
            Guard.NotNull(config, nameof(config));

            var arg = new Argument<DirectoryInfo>();
            _rootCommand.AddArgument(arg);

            config(new FileSystemArgumentBuilder<DirectoryInfo>(arg));

            return this;
        }

        public ICommandLineBuilder GlobalOption<TOpt>(Action<OptionBuilder<TOpt>> config)
        {
            Guard.NotNull(config, nameof(config));

            var option = new Option<TOpt>();
            _rootCommand.AddGlobalOption(option);

            config(new OptionBuilder<TOpt>(option));

            return this;
        }

        public ICommandLineBuilder Option<TOpt>(Action<OptionBuilder<TOpt>> config)
        {
            Guard.NotNull(config, nameof(config));

            var option = new Option<TOpt>();
            _rootCommand.AddOption(option);

            config(new OptionBuilder<TOpt>(option));

            return this;
        }

        public void Build() => _application.Build();

        public TResult? ParseCommandLine<TResult>()
            where TResult : class, new() =>
            _application.ParseCommandLine<TResult>()!;

        public TResult? ParseCommandLine<TResult>(out IReadOnlyList<string> unmatchedArgs)
            where TResult : class, new() =>
            _application.ParseCommandLine<TResult>(out unmatchedArgs);

        public ICommandLineBuilder EnableDirectives()
        {
            _application.EnableDirectives = true;
            return this;
        }

        public ICommandLineBuilder EnablePosixBundling()
        {
            _application.EnablePosixBundling = true;
            return this;
        }

        public ICommandLineBuilder SpaceSeparatedResponseFile()
        {
            _application.ResponseFileHandling = ResponseFileHandling.ParseArgsAsSpaceSeparated;
            return this;
        }

        public ICommandLineBuilder LineSeparatedResponseFile()
        {
            _application.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
            return this;
        }

        public ICommandLineBuilder NoResponseFile()
        {
            _application.ResponseFileHandling = ResponseFileHandling.Disabled;
            return this;
        }

        public ICommandLineBuilder WithHelp()
        {
            _application.AddHelpOption();
            return this;
        }

        public ICommandLineBuilder WithVersion(string version)
        {
            _application.AddVersionOption(version);
            return this;
        }

        public ICommandLineBuilder WithVersion(Assembly versionedAssembly)
        {
            _application.AddVersionOption(versionedAssembly);
            return this;
        }

        public ICommandLineBuilder WithVersion(Func<string> versionProvider)
        {
            _application.AddVersionOption(versionProvider);
            return this;
        }

        internal ICommandLineBuilder Middleware(InvocationMiddleware middleware, MiddlewareOrder order)
        {
            Guard.NotNull(middleware, nameof(middleware));

            _application.AddMiddleware(middleware, order);
            return this;
        }

        public ICommandLineBuilder CancelOnProcessTermination()
        {
            _application.AddCancelOnProcessTermination();
            return this;
        }

        public ICommandLineBuilder WithExceptionHandler(Func<Exception,
            (ExceptionBehavior Behavior, int TerminateExitCode)>? exceptionHandler)
        {
            _application.AddExceptionHandler(exceptionHandler);
            return this;
        }

        HandlerTarget HandlerProvider<ICommandLineBuilder>.Target => _rootCommand;

        ICommandLineBuilder HandlerProvider<ICommandLineBuilder>.Builder => this;
    }
}
