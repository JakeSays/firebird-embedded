// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Utility;


namespace Std.CommandLine.Parsing
{
    public abstract class SymbolResult
    {
        private protected readonly List<Token> _tokens = [];
        private ValidationMessages? _validationMessages;

        private readonly Dictionary<IArgument, ArgumentResult> _defaultArgumentValues = [];

        private protected SymbolResult(ISymbol symbol,
            SymbolResult? parent)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));

            Parent = parent;
        }

        public string? ErrorMessage { get; set; }

        public SymbolResultSet Children { get; } = [];

        public SymbolResult? Parent { get; }

        public ISymbol Symbol { get; }

        public IReadOnlyList<Token> Tokens => _tokens;

        internal bool IsArgumentLimitReached => RemainingArgumentCapacity <= 0;

        private protected virtual int RemainingArgumentCapacity => MaximumArgumentCapacity() - Tokens.Count;

        internal int MaximumArgumentCapacity() =>
            Symbol.Arguments()
                .Sum(a => a.Arity.MaximumNumberOfValues);

        protected internal ValidationMessages ValidationMessages
        {
            get => _validationMessages ??= Parent is null
                ? ValidationMessages.Instance
                : Parent.ValidationMessages;
            set => _validationMessages = value;
        }

        internal void AddToken(Token token) => _tokens.Add(token);

        internal ArgumentResult GetOrCreateDefaultArgumentResult(Argument argument) =>
            _defaultArgumentValues.GetOrAdd(argument,
                arg => new ArgumentResult(argument,
                    this));

        internal bool UseDefaultValueFor(IArgument argument) =>
            this switch
            {
                OptionResult { IsImplicit: true } => true,
                CommandResult _ when Children.ResultFor(argument)?.Tokens.Count == 0 => true,
                _ => false
            };

        public override string ToString() => $"{GetType().Name}: {this.Token()}";

        internal ParseError? UnrecognizedArgumentError(Argument argument)
        {
            if (!(argument.AllowedValues?.Count > 0) ||
                Tokens.Count <= 0)
            {
                return null;
            }

            return (from token in Tokens
                where !argument.AllowedValues.Contains(token.Value)
                select new ParseError(ValidationMessages.UnrecognizedArgument(token.Value,
                        argument.AllowedValues),
                    this)).FirstOrDefault();
        }
    }
}
