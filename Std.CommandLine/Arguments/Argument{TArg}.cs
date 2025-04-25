// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Linq;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Arguments
{
    internal class Argument<TArg> : Argument, IArgument, IArgument<TArg>
    {
        public new static Argument<TArg> None { get; } = new() {Arity = ArgumentArity.Zero};

        static Argument()
        {
            Argument.None = None;
        }

        public Argument() : base(null)
        {
            ArgumentType = typeof(TArg);
        }

        public Argument(
            string name,
            string? description = null) : base(name)
        {
            ArgumentType = typeof(TArg);
            Description = description;
        }

        public Argument(
            string name,
            Func<TArg> getDefaultValue,
            string? description = null) : this(name)
        {
            if (getDefaultValue is null)
            {
                throw new ArgumentNullException(nameof(getDefaultValue));
            }

            SetDefaultValueFactory(() => getDefaultValue());

            Description = description;
        }

        public Argument(Func<TArg> getDefaultValue) : this()
        {
            if (getDefaultValue is null)
            {
                throw new ArgumentNullException(nameof(getDefaultValue));
            }

            SetDefaultValueFactory(() => getDefaultValue());
        }

        public void SetParser(Func<string?, (TArg Result, string? Error)> parser)
        {
            ConvertArguments = (ArgumentResult argumentResult, out object? value) =>
            {
                var rawValue = argumentResult.Tokens.FirstOrDefault(t => t.Type == TokenType.Argument);
                (var result, argumentResult.ErrorMessage) = parser(rawValue?.Value);

                if (string.IsNullOrEmpty(argumentResult.ErrorMessage))
                {
                    value = result;
                    return true;
                }

                value = default(TArg)!;
                return false;
            };
        }

        public void SetParser(Func<ArgumentResult, TArg> parser)
        {
            ConvertArguments = (ArgumentResult argumentResult, out object? value) =>
            {
                var result = parser(argumentResult);

                if (string.IsNullOrEmpty(argumentResult.ErrorMessage))
                {
                    value = result;
                    return true;
                }

                value = default(TArg)!;
                return false;
            };
        }

        public Argument(
            string? name,
            Func<ArgumentResult, TArg> parser,
            bool isDefault = false) : this()
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Name = name!;
            }

            if (parser is null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            if (isDefault)
            {
                SetDefaultValueFactory(argumentResult => parser(argumentResult));
            }

            SetParser(parser);
        }

        public Argument(Func<ArgumentResult, TArg> parser, bool isDefault = false)
            : this(null, parser, isDefault)
        {
        }
    }
}
