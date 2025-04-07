// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Std.CommandLine.Commands;


namespace Std.CommandLine.Parsing
{
    public static class StringExtensions
    {
        private static readonly string[] OptionPrefixStrings = ["++", "+", "--", "-", "/"];

        internal static bool ContainsCaseInsensitive(this string source,
            string? value) =>
            source.IndexOfCaseInsensitive(value) >= 0;

        internal static int IndexOfCaseInsensitive(this string source,
            string? value) =>
            CultureInfo.InvariantCulture
                .CompareInfo
                .IndexOf(source,
                    value ?? "",
                    CompareOptions.OrdinalIgnoreCase);

        internal static string RemovePrefix(this string rawAlias)
        {
            foreach (var prefix in OptionPrefixStrings)
            {
                if (rawAlias.StartsWith(prefix))
                {
                    return rawAlias.Substring(prefix.Length);
                }
            }

            return rawAlias;
        }

        internal static (string? prefix, string alias) SplitPrefix(this string rawAlias)
        {
            foreach (var prefix in OptionPrefixStrings)
            {
                if (rawAlias.StartsWith(prefix))
                {
                    return (prefix, rawAlias.Substring(prefix.Length));
                }
            }

            return (null, rawAlias);
        }
    }
}
