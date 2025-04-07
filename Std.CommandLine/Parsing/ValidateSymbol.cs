// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Std.CommandLine.Parsing
{
    internal delegate string? ValidateSymbol<in T>(T symbolResult)
        where T : SymbolResult;
}
