// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Std.CommandLine.Collections;


//using System.CommandLine.Suggestions;

namespace Std.CommandLine
{
    public interface ISymbol// : ISuggestionSource
    {
        public string Name { get; }

        public string? Description { get; }

        public IReadOnlyList<string> Aliases { get; }

        public IReadOnlyList<string> RawAliases { get; }

        public bool HasAlias(string alias);

        bool HasRawAlias(string alias);

        public bool IsHidden { get; }

        SymbolSet Children { get; }

        SymbolSet? Parents { get; }
    }
}
