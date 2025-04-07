// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Std.CommandLine.Parsing
{
    /// <summary>
    /// Performs custom parsing of an argument.
    /// </summary>
    /// <typeparam name="TArg">The type which the argument is to be parsed as.</typeparam>
    /// <param name="result">The argument result.</param>
    /// <returns>The parsed value.</returns>
    /// <remarks>Validation errors can be returned by setting <see cref="SymbolResult.ErrorMessage"/>.</remarks>
    public delegate TArg ParseArgument<out TArg>(ArgumentResult result);
}
