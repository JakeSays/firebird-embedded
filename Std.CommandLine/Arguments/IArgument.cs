// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Std.CommandLine.Binding;


namespace Std.CommandLine.Arguments
{
    public interface IArgument<TArg>
    {
        IArgumentArity Arity { get; set; }
        IReadOnlyList<string> Aliases { get; }
        string? Description { get; set; }
        string Name { get; set; }
        bool IsHidden { get; set; }
        bool HasAlias(string alias);
    }

    public interface IArgument
        : ISymbol,
            IValueDescriptor
    {
        IArgumentArity Arity { get; }
    }
}
