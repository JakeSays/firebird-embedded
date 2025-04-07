// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Collections;
using Std.CommandLine.Invocation;
using Std.CommandLine.Options;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Commands
{
    internal class Command : Symbol, ICommand, IEnumerable<Symbol>, HandlerTarget
    {
        private readonly SymbolSet _globalOptions = [];
        private Invokable? _handler;

        public Command()
        {
        }

        public Command(string name, string? description = null) : base([name], description)
        {
        }

        [field: AllowNull, MaybeNull]
        public IReadOnlyList<Argument> Arguments => field ??= Children.OfType<Argument>().ToList();

        [field: AllowNull, MaybeNull]
        public IReadOnlyList<Option> Options => field ??= Children.OfType<Option>().ToList();

        [field: AllowNull, MaybeNull]
        public IReadOnlyList<Option> GlobalOptions => field ??= _globalOptions.OfType<Option>().ToList();

        public void AddArgument(Argument argument) => AddArgumentInner(argument);

        public void AddCommand(Command command) => AddSymbol(command);

        public void AddOption(Option option) => AddSymbol(option);

        public void AddGlobalOption(Option option)
        {
            _globalOptions.Add(option);
            Children.AddWithoutAliasCollisionCheck(option);
        }

        public bool TryAddGlobalOption(Option option)
        {
            if (_globalOptions.IsAnyAliasInUse(option, out _))
            {
                return false;
            }

            _globalOptions.Add(option);
            Children.AddWithoutAliasCollisionCheck(option);
            return true;

        }

        public void Add(Symbol symbol) => AddSymbol(symbol);

        public void Add(Argument argument) => AddArgument(argument);

        private protected override void AddSymbol(Symbol symbol)
        {
            if (symbol is IOption option)
            {
                _globalOptions.ThrowIfAnyAliasIsInUse(option);
            }

            symbol.AddParent(this);

            base.AddSymbol(symbol);
        }

        internal List<ValidateSymbol<CommandResult>> Validators { get; } = [];

        public void AddValidator(ValidateSymbol<CommandResult> validate) => Validators.Add(validate);

        public bool TreatUnmatchedTokensAsErrors { get; set; } = true;

        public Invokable? Handler
        {
            get => _handler;
            set => _handler = value ?? throw new ArgumentNullException(nameof(value), "HEY! stop that!");
        }

        public IEnumerator<Symbol> GetEnumerator() => Children.OfType<Symbol>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IReadOnlyList<IArgument> ICommand.Arguments => Arguments;

        IReadOnlyList<IOption> ICommand.Options => Options;

        internal CommandLineParser? ImplicitParser { get; set; }

        public void SetHandler(Invokable handler) => Handler = handler;
    }
}
