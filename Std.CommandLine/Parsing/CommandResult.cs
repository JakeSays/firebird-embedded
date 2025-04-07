// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Binding;
using Std.CommandLine.Commands;
using Std.CommandLine.Utility;


namespace Std.CommandLine.Parsing
{
    public class CommandResult : SymbolResult
    {
        private ArgumentConversionResultSet? _results;

        internal CommandResult(
            ICommand command,
            Token token,
            CommandResult? parent = null) :
            base(command ?? throw new ArgumentNullException(nameof(command)),
                 parent)
        {
            Command = command;

            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public ICommand Command { get; }

        public OptionResult? this[string alias] => OptionResult(alias);

        public OptionResult? OptionResult(string alias)
        {
            return Children[alias] as OptionResult;
        }

        public Token Token { get; }


        internal virtual RootCommandResult? Root => (Parent as CommandResult)?.Root;

        internal bool TryGetValueForArgument(
            IValueDescriptor valueDescriptor,
            out object? value)
        {
            if (valueDescriptor.ValueName is { } valueName)
            {
                foreach (var argument in Command.Arguments)
                {
                    if (!valueName.IsMatch(argument.Name))
                    {
                        continue;
                    }

                    value = ArgumentConversionResults[argument.Name]?.GetValueOrDefault();
                    return true;
                }
            }

            value = null;
            return false;
        }

        internal bool TryGetValueForOption(IValueDescriptor valueDescriptor,
            out object? value)
        {
            var children = Children
                .Where(o => valueDescriptor.ValueName?.IsMatch(o.Symbol) == true)
                .ToArray();

            SymbolResult? symbolResult = null;

            if (children.Length > 1)
            {
                throw new ArgumentException(
                    $"Ambiguous match while trying to bind parameter {valueDescriptor.ValueName} among: {string.Join(",", children.Select(o => o.Symbol.Name))}");
            }

            if (children.Length == 1)
            {
                symbolResult = children[0];
            }

            if (symbolResult is OptionResult optionResult)
            {
                if (optionResult.ConvertIfNeeded(valueDescriptor.ValueType) is SuccessfulArgumentConversionResult
                    successful)
                {
                    value = successful.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public object? ValueForOption(string alias)
        {
            if (Children[alias] is OptionResult optionResult)
            {
                if (optionResult.Option.Argument?.Arity.MaximumNumberOfValues > 1)
                {
                    return optionResult.GetValueOrDefault<IEnumerable<string>>();
                }
            }

            return ValueForOption<object?>(alias);
        }

        [return: MaybeNull]
        public T ValueForOption<T>(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(alias));
            }

            if (Children[alias] is OptionResult optionResult)
            {
                return optionResult.GetValueOrDefault<T>();
            }
            else
            {
                return default!;
            }
        }

        internal ArgumentConversionResultSet ArgumentConversionResults
        {
            get
            {
                if (!(_results is null))
                {
                    return _results;
                }

                var results = Children
                    .OfType<ArgumentResult>()
                    .Select(r => r.Convert(r.Argument));

                _results = [];

                foreach (var result in results)
                {
                    _results.Add(result);
                }

                return _results;
            }
        }
    }
}
