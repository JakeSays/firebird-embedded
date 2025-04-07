// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Collections;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;

namespace Std.CommandLine
{
    internal abstract class Symbol : ISymbol
    {
        private readonly List<string> _aliases = [];
        private readonly List<string> _rawAliases = [];
        private string _longestAlias = "";
        private string? _specifiedName;

        private readonly SymbolSet _parents = [];

        private protected Symbol()
        {
        }

        protected Symbol(
            IReadOnlyCollection<string>? aliases = null,
            string? description = null)
        {
            if (aliases is null)
            {
                throw new ArgumentNullException(nameof(aliases));
            }

            if (!aliases.Any())
            {
                throw new ArgumentException("An option must have at least one alias.");
            }

            foreach (var alias in aliases)
            {
                AddAlias(alias);
            }

            Description = description;
        }

        public IReadOnlyList<string> Aliases => _aliases;

        public IReadOnlyList<string> RawAliases => _rawAliases;

        public string? Description { get; set; }

        public virtual string Name
        {
            get => _specifiedName ?? _longestAlias;
            set
            {
                Guard.NotNullOrEmpty(value, nameof(value));

                _specifiedName = value;
            }
        }

        public SymbolSet? Parents => _parents;

        internal void AddParent(Symbol symbol)
        {
            _parents.AddWithoutAliasCollisionCheck(symbol);
        }

        private protected virtual void AddSymbol(Symbol symbol)
        {
            Children.Add(symbol);
        }

        private protected void AddArgumentInner(Argument argument)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            argument.AddParent(this);

            // if (argument.Name.NotUseful())
            // {
            //     argument.Name = _aliases.FirstOrDefault()?.ToLower();
            // }

            Children.Add(argument);
        }

        public SymbolSet Children { get; } = [];

        public void AddAlias(string value)
        {
            var aliases = value.Split('|');

            foreach (var alias in aliases)
            {
                Add(alias);
            }

            void Add(string? alias)
            {
                var unprefixedAlias = alias?.RemovePrefix();

                if (unprefixedAlias.IsNullOrEmpty())
                {
                    throw new ArgumentException($"{GetType().Name} alias cannot be null, empty, or contain whitespace: {(alias.IsNullOrEmpty() ? "<nothing>" : $"\"alias\"")}");
                }

                _rawAliases.Add(alias!);
                _aliases.Add(unprefixedAlias!);

                if (unprefixedAlias!.Length > Name?.Length)
                {
                    _longestAlias = unprefixedAlias;
                }
            }
        }

        public bool HasAlias(string alias)
        {
            if (alias.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(alias));
            }

            return _aliases.Contains(alias.RemovePrefix());
        }

        public bool HasRawAlias(string alias) => _rawAliases.Contains(alias);

        public bool IsHidden { get; set; }

        public override string ToString() => $"{GetType().Name}: {Name}";

        SymbolSet ISymbol.Children => Children;
    }
}
