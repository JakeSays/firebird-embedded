﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Commands;
using Std.CommandLine.Options;


namespace Std.CommandLine.Binding
{
    internal class FailedArgumentTypeConversionResult : FailedArgumentConversionResult
    {
        internal FailedArgumentTypeConversionResult(
            IArgument argument,
            Type type,
            string value) :
            base(argument, FormatErrorMessage(argument, type, value))
        {
        }

        private static string FormatErrorMessage(
            IArgument argument,
            Type type,
            string value)
        {
            if (argument is Argument { Parents.Count: 1 } a)
            {
                // TODO: (FailedArgumentTypeConversionResult) localize

                var firstParent = a.Parents.First();

                var symbolType =
                    firstParent switch {
                        ICommand _ => "command",
                        IOption _ => "option",
                        _ => null
                        };

                var alias = firstParent.RawAliases[0];

                return $"Cannot parse argument '{value}' for {symbolType} '{alias}' as expected type {type}.";
            }

            return $"Cannot parse argument '{value}' as expected type {type}.";
        }
    }
}
