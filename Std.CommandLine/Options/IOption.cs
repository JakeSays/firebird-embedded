// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Std.CommandLine.Arguments;
using Std.CommandLine.Binding;


namespace Std.CommandLine.Options
{
    public interface IOption<TOpt>
    {
        IArgument<TOpt>? Argument { get; internal set; }

        bool Required { get; }
        IReadOnlyList<string> Aliases { get; }
        string? Description { get; }
        string Name { get; }
        bool IsHidden { get; }
        bool HasAlias(string alias);
    }

    public interface IOption : ISymbol, IValueDescriptor
    {
        IArgument? Argument { get; }

        public bool Required { get; }
    }
}
