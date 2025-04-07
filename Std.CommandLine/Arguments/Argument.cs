// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Binding;
using Std.CommandLine.Options;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Arguments
{
    internal class Argument : Symbol, IArgument
    {
        private Func<ArgumentResult, object?>? _defaultValueFactory;
        private IArgumentArity? _arity;
        private TryConvertArgument? _convertArguments;
        private Type _argumentType = typeof(void);

        static Argument()
        {
            None = new Argument {Arity = ArgumentArity.Zero};
        }

        public Argument()
        { }

        public Argument(string? name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Name = name;
            }
        }

        public void AddOption(Option option)
        {
            option.AddParent(this);

            base.AddSymbol(option);
        }

        public void SetArity(int min, int max)
        {
            _arity = new ArgumentArity(min, max);
        }

        internal HashSet<string>? AllowedValues { get; private set; }

        public IArgumentArity Arity
        {
            get
            {
                if (_arity is not null)
                {
                    return _arity;
                }

                return ArgumentType != typeof(void)
                    ? ArgumentArity.Default(ArgumentType, this, Parents?.FirstOrDefault())
                    : ArgumentArity.Zero;
            }
            set => _arity = value;
        }

        internal TryConvertArgument? ConvertArguments
        {
            get
            {
                if (_convertArguments != null ||
                    ArgumentType == typeof(void))
                {
                    return _convertArguments;
                }

                if (!ArgumentType.CanBeBoundFromScalarValue())
                {
                    return _convertArguments;
                }

                _convertArguments = Arity.MaximumNumberOfValues switch
                {
                    0 when ArgumentType == typeof(bool) => (ArgumentResult symbol, out object? value) =>
                    {
                        value = ArgumentConverter.ConvertObject(this, typeof(bool), bool.TrueString);

                        return value is SuccessfulArgumentConversionResult;
                    },
                    1 when ArgumentType == typeof(bool) => (ArgumentResult symbol, out object? value) =>
                    {
                        value = ArgumentConverter.ConvertObject(this, typeof(bool),
                            symbol.Tokens.SingleOrDefault()?.Value ?? bool.TrueString);

                        return value is SuccessfulArgumentConversionResult;
                    },
                    _ => DefaultConvert
                };

                return _convertArguments;

                bool DefaultConvert(SymbolResult symbol, out object value)
                {
                    value = Arity.MaximumNumberOfValues switch
                    {
                        1 => ArgumentConverter.ConvertObject(this, ArgumentType,
                            symbol.Tokens.Select(t => t.Value).SingleOrDefault()),
                        _ => ArgumentConverter.ConvertStrings(this, ArgumentType,
                            symbol.Tokens.Select(t => t.Value).ToArray())
                    };

                    return value is SuccessfulArgumentConversionResult;
                }
            }
            set => _convertArguments = value;
        }

        public Type ArgumentType
        {
            get => _argumentType;
            set => _argumentType = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal List<ValidateSymbol<ArgumentResult>> Validators { get; } = [];

        public void AddValidator(ValidateSymbol<ArgumentResult> validator) => Validators.Add(validator);

        public object? GetDefaultValue() => GetDefaultValue(new ArgumentResult(this, null));

        internal object? GetDefaultValue(ArgumentResult argumentResult)
        {
            if (_defaultValueFactory is null)
            {
                throw new InvalidOperationException($"Argument \"{Name}\" does not have a default value");
            }

            return _defaultValueFactory.Invoke(argumentResult);
        }

        public void SetDefaultValue(object? value) => SetDefaultValueFactory(() => value);

        public void SetDefaultValueFactory(Func<object?> getDefaultValue)
        {
            if (getDefaultValue is null)
            {
                throw new ArgumentNullException(nameof(getDefaultValue));
            }

            SetDefaultValueFactory(_ => getDefaultValue());
        }

        public void SetDefaultValueFactory(Func<ArgumentResult, object?> getDefaultValue) =>
            _defaultValueFactory = getDefaultValue ?? throw new ArgumentNullException(nameof(getDefaultValue));

        public bool HasDefaultValue => _defaultValueFactory != null;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal static Argument None { get; private protected set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        internal void AddAllowedValues(IEnumerable<string> values)
        {
            AllowedValues ??= [];

            AllowedValues.UnionWith(values);
        }

        public override string ToString() => $"{nameof(Argument)}: {Name}";

        IArgumentArity IArgument.Arity => Arity;

        string IValueDescriptor.ValueName => Name;

        Type IValueDescriptor.ValueType => ArgumentType;
    }
}
