﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Std.CommandLine.Binding;
using Std.CommandLine.Utility;


namespace Std.CommandLine.Parsing
{
    public static class ArgumentResultExtensions
    {
        public static object? GetValueOrDefault(this ArgumentResult argumentResult) =>
            argumentResult.GetValueOrDefault<object?>();

        [return: MaybeNull]
        public static T GetValueOrDefault<T>(this ArgumentResult argumentResult) =>
            argumentResult.GetArgumentConversionResult()
                          .ConvertIfNeeded(argumentResult, typeof(T))
                          .GetValueOrDefault<T>();
    }
}
