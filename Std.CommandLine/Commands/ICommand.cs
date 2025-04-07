// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Std.CommandLine.Arguments;
using Std.CommandLine.Options;


namespace Std.CommandLine.Commands
{
    public interface ICommand : ISymbol
    {
        bool TreatUnmatchedTokensAsErrors { get; }

        IReadOnlyList<IArgument> Arguments { get; }

        IReadOnlyList<IOption> Options { get; }
    }
}
