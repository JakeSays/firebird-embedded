// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Std.CommandLine.Arguments;


namespace Std.CommandLine.Binding
{
    internal class MissingArgumentConversionResult : FailedArgumentConversionArityResult
    {
        internal MissingArgumentConversionResult(IArgument argument, string errorMessage) : base(argument, errorMessage)
        {
        }
    }
}
