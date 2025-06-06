﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Std.CommandLine.Arguments;


namespace Std.CommandLine.Binding
{
    internal abstract class FailedArgumentConversionArityResult : FailedArgumentConversionResult
    {
        internal FailedArgumentConversionArityResult(IArgument argument, string errorMessage) : base(argument, errorMessage)
        {
        }
    }
}
