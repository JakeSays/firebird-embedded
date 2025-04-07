// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Commands;
using Std.CommandLine.Options;


namespace Std.CommandLine
{
    internal static class SymbolExtensions
    {
        internal static IReadOnlyList<string> ChildSymbolAliases(this ISymbol symbol) =>
            symbol.Children
                  .Where(s => !s.IsHidden)
                  .SelectMany(s => s.RawAliases).ToList();

        internal static IReadOnlyList<IArgument> Arguments(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IOption option:
                    return option.Argument == null
                        ? []
                        : [option.Argument];
                case ICommand command:
                    return command.Arguments;
                case IArgument argument:
                    return
                    [
                        argument
                    ];
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
