// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Binding;
using Std.CommandLine.Collections;
using Std.CommandLine.Commands;
using Std.CommandLine.Help;
using Std.CommandLine.Invocation;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;


namespace Std.CommandLine
{
    internal class CommandLineConfiguration
    {
        private readonly SymbolSet _symbols = [];

        public CommandLineConfiguration(
            RootCommand rootCommand,
            IReadOnlyCollection<char>? argumentDelimiters = null,
            bool enablePosixBundling = true,
            bool enableDirectives = true,
            ValidationMessages? validationMessages = null,
            ResponseFileHandling responseFileHandling = ResponseFileHandling.Disabled,
            IReadOnlyCollection<InvocationMiddleware>? middlewarePipeline = null,
            Func<BindingContext, IHelpBuilder>? helpBuilderFactory = null)
        {
            Guard.NotNull(rootCommand, nameof(rootCommand));

            RootCommand = rootCommand;

            if (argumentDelimiters is null)
            {
                ArgumentDelimitersInternal =
                [
                    ':',
                    '='
                ];
            }
            else
            {
                ArgumentDelimitersInternal = new HashSet<char>(argumentDelimiters);
            }

            foreach (var symbol in rootCommand)
            {
                foreach (var alias in symbol.RawAliases)
                {
                    foreach (var delimiter in ArgumentDelimiters)
                    {
                        if (alias.Contains(delimiter))
                        {
                            throw new ArgumentException($"{symbol.GetType().Name} \"{alias}\" is not allowed to contain a delimiter but it contains \"{delimiter}\"");
                        }
                    }
                }
            }

            // if (symbols.Count == 1 &&
            //     symbols.Single() is Command rootCommand)
            // {
            //     RootCommand = rootCommand;
            // }
            // else
            // {
            //     // reuse existing auto-generated root command, if one is present, to prevent repeated mutations
            //     var parentRootCommand =
            //         symbols.SelectMany(s => s.Parents)
            //                .OfType<RootCommand>()
            //                .FirstOrDefault();
            //
            //     if (parentRootCommand is null)
            //     {
            //         parentRootCommand = new RootCommand();
            //
            //         foreach (var symbol in symbols)
            //         {
            //             parentRootCommand.Add(symbol);
            //         }
            //     }
            //
            //     RootCommand = rootCommand = parentRootCommand;
            // }

            _symbols.Add(RootCommand);

            AddGlobalOptionsToChildren(rootCommand);

            EnablePosixBundling = enablePosixBundling;
            EnableDirectives = enableDirectives;
            ValidationMessages = validationMessages ?? ValidationMessages.Instance;
            ResponseFileHandling = responseFileHandling;
            Middleware = middlewarePipeline ?? new List<InvocationMiddleware>();
            HelpBuilderFactory = helpBuilderFactory ?? (context => new HelpBuilder());
        }

        private void AddGlobalOptionsToChildren(Command parentCommand)
        {
            foreach (var globalOption in parentCommand.GlobalOptions)
            {
                foreach (var childCommand in parentCommand.Children.FlattenBreadthFirst(c => c.Children).OfType<Command>())
                {
                    if (!childCommand.Children.IsAnyAliasInUse(globalOption, out _))
                    {
                        childCommand.AddOption(globalOption);
                    }
                }
            }
        }

        public SymbolSet Symbols => _symbols;

        public IReadOnlyCollection<char> ArgumentDelimiters => ArgumentDelimitersInternal;

        internal HashSet<char> ArgumentDelimitersInternal { get; }

        public bool EnableDirectives { get; }

        public bool EnablePosixBundling { get; }

        public ValidationMessages ValidationMessages { get; }

        internal Func<BindingContext, IHelpBuilder> HelpBuilderFactory { get; }

        internal IReadOnlyCollection<InvocationMiddleware> Middleware { get; }

        public ICommand RootCommand { get; }

        internal ResponseFileHandling ResponseFileHandling { get; }
    }
}
