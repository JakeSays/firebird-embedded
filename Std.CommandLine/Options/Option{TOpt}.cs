// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using Std.CommandLine.Arguments;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Options
{
    internal class Option<TOpt> : Option, IOption<TOpt>
    {
        private Argument<TOpt>? _argument;

        public Option() {}

        public Option(
            string alias,
            string? description = null) : base(alias, description)
        {
        }

        public Option(
            string[] aliases,
            string? description = null) : base(aliases, description)
        {
        }

        public Option(
            string alias,
            ParseArgument<TOpt> parseArgument,
            bool isDefault = false,
            string? description = null) : base(alias, description)
        {
            if (parseArgument is null)
            {
                throw new ArgumentNullException(nameof(parseArgument));
            }
        }

        public Option(
            string[] aliases,
            ParseArgument<TOpt> parseArgument,
            bool isDefault = false,
            string? description = null) : base(aliases, description)
        {
            if (parseArgument is null)
            {
                throw new ArgumentNullException(nameof(parseArgument));
            }
        }

        public Option(
            string alias,
            Func<TOpt> getDefaultValue,
            string? description = null) : base(alias, description)
        {
            if (getDefaultValue is null)
            {
                throw new ArgumentNullException(nameof(getDefaultValue));
            }
        }

        public Option(
            string[] aliases,
            Func<TOpt> getDefaultValue,
            string? description = null) : base(aliases, description)
        {
            if (getDefaultValue is null)
            {
                throw new ArgumentNullException(nameof(getDefaultValue));
            }
        }

        internal Argument<TOpt>? TypedArgument { get; private set; }

        internal void SetArgument(Argument<TOpt> arg)
        {
            _argument = arg;
            Argument = arg;
            TypedArgument = arg;
        }

        IArgument<TOpt>? IOption<TOpt>.Argument
        {
            get => _argument ??= Argument<TOpt>.None;
            set => _argument = (Argument<TOpt>?) value;
        }

        public override Argument Argument
        {
            set
            {
                if (value is not Argument<TOpt>)
                {
                    throw new ArgumentException($"{nameof(Argument)} must be of type {typeof(Argument<TOpt>)} but was {value?.GetType().ToString() ?? "null"}");
                }

                base.Argument = value;
            }
        }
    }
}
