// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Std.CommandLine.Binding;
using Std.CommandLine.Commands;
using Std.CommandLine.Help;
using Std.CommandLine.Invocation;
using Std.CommandLine.Options;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming


namespace Std.CommandLine
{
    [PublicAPI]
    public class StdApplication
        : IStdApplication
    {
        private readonly List<(InvocationMiddleware middleware, int order)> _middleware = [];

        private CommandLineParser? _parser;

        private Option? _versionOption;

        private protected ParseResult? ParseResult { get; private set; }

        internal RootCommand RootCommand { get; }

        internal List<string> Args { get; set; }
        public IReadOnlyList<string> UnmatchedArgs { get; private set; }

        public StdApplication(List<string>? args, string? applicationName = null)
        {
            Args = args ?? [];
            RootCommand = new RootCommand(applicationName ?? ExecutableName);
            CommandLine = new CommandLineBuilder(this, false);
            UnmatchedArgs = new List<string>();
        }

        public ICommandLineBuilder CommandLine { get; }

        internal bool EnableDirectives { get; set; } = true;

        internal bool EnablePosixBundling { get; set; } = true;

        internal bool TreatUnmatchedArgsAsErrors { get; set; }

        internal ResponseFileHandling ResponseFileHandling { get; set; }

        internal Func<BindingContext, IHelpBuilder>? HelpBuilderFactory { get; set; }

        internal HelpOption? HelpOption { get; set; }

        internal ValidationMessages? ValidationMessages { get; set; }
        public bool ExitOnParseError { get; set; }

        protected BuildStatus InternalBuild()
        {
            var rootCommand = RootCommand;
            rootCommand.TreatUnmatchedTokensAsErrors = TreatUnmatchedArgsAsErrors;

            _parser = new CommandLineParser(new CommandLineConfiguration(rootCommand,
                enablePosixBundling: EnablePosixBundling,
                enableDirectives: EnableDirectives,
                validationMessages: ValidationMessages,
                responseFileHandling: ResponseFileHandling,
                middlewarePipeline: _middleware.OrderBy(m => m.order)
                    .Select(m => m.middleware)
                    .ToArray(),
                helpBuilderFactory: HelpBuilderFactory));

            RootCommand.ImplicitParser = _parser;

            ParseResult = _parser.Parse(Args);
            ((List<string>) UnmatchedArgs).AddRange(ParseResult.UnmatchedTokens);

            if (ParseResult.Errors.Count == 0)
            {
                return TreatUnmatchedArgsAsErrors
                    ? BuildStatus.Failure
                    : BuildStatus.Success;
            }

            foreach (var error in ParseResult.Errors)
            {
                DefaultConsoles.StdErr.RedLine(error.Message);
            }

            return BuildStatus.Failure;
        }

        internal BuildStatus Build()
        {
            var status = InternalBuild();
            if (status != BuildStatus.Success &&
                ExitOnParseError)
            {
                Environment.Exit(IStdApplication.ExitCodeFailure);
            }

            return status;
        }

        public int Run()
        {
            var result = ParseResult!.Invoke();
            Environment.ExitCode = result;
            return result;
        }

        internal TResult? ParseCommandLine<TResult>()
            where TResult : class, new() =>
            ParseCommandLine<TResult>(out _);

        internal TResult? ParseCommandLine<TResult>(out IReadOnlyList<string> unmatchedArgs)
            where TResult : class, new()
        {
            var status = Build();

            unmatchedArgs = UnmatchedArgs;

            if (status != BuildStatus.Success)
            {
                return null;
            }

            var bindingContext = new BindingContext(ParseResult!);

            var descriptor = ModelDescriptor.FromType<TResult>();

            var binder = new ModelBinder(typeof(TResult), descriptor);

            var result = new TResult();
            binder.UpdateInstance(result, bindingContext);

            return result;
        }

        internal void AddMiddleware(
            InvocationMiddleware middleware,
            MiddlewareOrder order)
        {
            _middleware.Add((middleware, (int) order));
        }

        internal void AddMiddleware(
            InvocationMiddleware middleware,
            MiddlewareOrderInternal order)
        {
            _middleware.Add((middleware, (int) order));
        }


        private static readonly Lazy<string> _executablePath = new(() => GetAssembly().Location);

        private static readonly Lazy<string> _executableName = new(() =>
        {
            var location = _executablePath.Value;

            if (location.IsNullOrEmpty())
            {
                location = Environment.GetCommandLineArgs().FirstOrDefault();
            }

            location ??= "unknown";

            return Path.GetFileNameWithoutExtension(location).Replace(" ", "");
        });

        private static Assembly GetAssembly() =>
            Assembly.GetEntryAssembly() ??
            Assembly.GetExecutingAssembly();

        /// <summary>
        /// The name of the currently running executable.
        /// </summary>
        internal static string ExecutableName => _executableName.Value;

        /// <summary>
        /// The path to the currently running executable.
        /// </summary>
        internal static string ExecutablePath => _executablePath.Value;

        internal bool DisplayHelp(BindingContext context)
        {
            if (!CanDisplayHelp(context))
            {
                return false;
            }

            context.HelpBuilder
                .Write(context.ParseResult.CommandResult.Command);

            return true;
        }

        private bool CanDisplayHelp(BindingContext context) =>
            HelpOption != null && context.ParseResult.FindResultFor(HelpOption) != null;

        internal void AddHelpOption()
        {
            if (HelpOption != null)
            {
                return;
            }

            HelpOption = new HelpOption();
            RootCommand.TryAddGlobalOption(HelpOption);

            AddMiddleware((context, next) =>
            {
                if (!CanDisplayHelp(context.BindingContext))
                {
                    return next(context);
                }

                context.InvocationResult = new HelpResult();
                return 0;

            }, MiddlewareOrderInternal.HelpOption);
        }

        private void InitializeVersionOption()
        {
            if (_versionOption != null)
            {
                return;
            }

            _versionOption = new Option("--version", "Show version information and exit");

            RootCommand.AddOption(_versionOption);
        }

        internal void AddVersionOption(string version)
        {
            Guard.NotNullOrEmpty(version, nameof(version));

            InitializeVersionOption();

            AddMiddleware((context, next) =>
            {
                if (!context.ParseResult.HasOption(_versionOption!))
                {
                    return next(context);
                }

                DefaultConsoles.StdOut.NormalLine(version);
                Environment.Exit(0);
                return 0;

            }, MiddlewareOrderInternal.VersionOption);
        }

        internal void AddVersionOption(Assembly versionedAssembly)
        {
            Guard.NotNull(versionedAssembly, nameof(versionedAssembly));

            AddVersionOption(() => versionedAssembly.GetName().Version?.ToString() ?? "unknown");
        }

        internal void AddVersionOption(Func<string> versionProvider)
        {
            Guard.NotNull(versionProvider, nameof(versionProvider));

            InitializeVersionOption();

            AddMiddleware((context, next) =>
            {
                if (!context.ParseResult.HasOption(_versionOption!))
                {
                    return next(context);
                }

                DefaultConsoles.StdOut.NormalLine(versionProvider());
                Environment.Exit(0);
                return 0;

            }, MiddlewareOrderInternal.VersionOption);
        }

        internal void AddCancelOnProcessTermination()
        {
            AddMiddleware((context, next) =>
            {
                var cancellationHandlingAdded = false;
                ManualResetEventSlim? blockProcessExit = null;
                ConsoleCancelEventHandler? consoleHandler = null;
                EventHandler? processExitHandler = null;

                context.CancellationHandlingAdded += cts =>
                {
                    blockProcessExit = new ManualResetEventSlim(initialState: false);
                    cancellationHandlingAdded = true;
                    consoleHandler = (_, args) =>
                    {
                        cts.Cancel();
                        // Stop the process from terminating.
                        // Since the context was cancelled, the invocation should
                        // finish and Main will return.
                        args.Cancel = true;
                    };
                    processExitHandler = (_1, _2) =>
                    {
                        cts.Cancel();
                        // The process exits as soon as the event handler returns.
                        // We provide a return value using Environment.ExitCode
                        // because Main will not finish executing.
                        // Wait for the invocation to finish.
                        blockProcessExit.Wait();
                        Environment.ExitCode = context.ResultCode;
                    };
                    Console.CancelKeyPress += consoleHandler;
                    AppDomain.CurrentDomain.ProcessExit += processExitHandler;
                };

                try
                {
                    return next(context);
                }
                finally
                {
                    if (cancellationHandlingAdded)
                    {
                        Console.CancelKeyPress -= consoleHandler;
                        AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
                        blockProcessExit!.Set();
                    }
                }
            }, MiddlewareOrderInternal.Startup);
        }

        internal void AddExceptionHandler(Func<Exception, (ExceptionBehavior Behavior, int TerminateExitCode)>? exceptionHandler)
        {
            AddMiddleware((context, next) =>
            {
                try
                {
                    return next(context);
                }
                catch (Exception exception)
                {
                    if (exceptionHandler != null)
                    {
                        var (action, exitCode) = exceptionHandler(exception);
                        //does this actually continue execution?
                        if (action == ExceptionBehavior.Continue)
                        {
                            return 0;
                        }

                        //figure out the standard behavior and
                        //consider using it.
                        Environment.Exit(exitCode);
                    }

                    DefaultConsoles.StdErr.RedLine($"Unhandled exception: {exception}");

                    context.ResultCode = 1;

                    return context.ResultCode;
                }
            }, MiddlewareOrderInternal.ExceptionHandler);
        }
    }
}
