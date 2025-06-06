﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Binding;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Options
{
    internal class Option : Symbol, IOption
    {
        public Option(){}

        public Option(string alias, string? description = null)
            : base([
                alias
            ], description)
        {
        }

        protected Option(string[] aliases, string? description = null) : base(aliases, description)
        {
        }

        public virtual Argument Argument
        {
            get => Arguments.FirstOrDefault() ?? Argument.None;
            set
            {
                foreach (var argument in Arguments.ToArray())
                {
                    Children.Remove(argument);
                }

                AddArgumentInner(value);
            }
        }

        private IEnumerable<Argument> Arguments => Children.OfType<Argument>();

        internal List<ValidateSymbol<OptionResult>> Validators { get; } = [];

        public void AddValidator(ValidateSymbol<OptionResult> validate) => Validators.Add(validate);

        IArgument IOption.Argument => Argument;

        public bool Required { get; set; }

        string IValueDescriptor.ValueName => Name;

        Type IValueDescriptor.ValueType => Argument.ArgumentType;

        bool IValueDescriptor.HasDefaultValue => Argument.HasDefaultValue;

        object? IValueDescriptor.GetDefaultValue() => Argument.GetDefaultValue();
    }
}
