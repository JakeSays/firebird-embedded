﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Linq;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Binding
{
    internal static class Binder
    {
        internal static bool IsMatch(this string parameterName, string alias) =>
            string.Equals(alias?.RemovePrefix()
                              .Replace("-", ""),
                          parameterName,
                          StringComparison.OrdinalIgnoreCase);

        internal static bool IsMatch(this string parameterName, ISymbol symbol) =>
            parameterName.IsMatch(symbol.Name) || symbol.Aliases.Any(parameterName.IsMatch);

        internal static bool IsNullable(this Type t)
        {
            return t.IsGenericType &&
                   t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static object? GetDefaultValueForType(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
