// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Linq;

namespace Std.CommandLine.Parsing;

internal static class ParserExtensions
{
    public static int Invoke(
        this CommandLineParser parser,
        string commandLine)
    {
        return parser.Invoke(CommandLineStringSplitter.Instance.Split(commandLine).ToArray());
    }

    public static int Invoke(
        this CommandLineParser parser,
        string[] args)
    {
        return parser.Parse(args).Invoke();
    }

    // public static Task<int> InvokeAsync(
    //     this CommandLineParser parser,
    //     string commandLine) =>
    //     parser.InvokeAsync(CommandLineStringSplitter.Instance.Split(commandLine).ToArray());
    //
    // public static async Task<int> InvokeAsync(
    //     this CommandLineParser parser,
    //     string[] args) =>
    //     await parser.Parse(args).InvokeAsync();

    public static ParseResult Parse(
        this CommandLineParser parser,
        string commandLine)
    {
        var splitter = CommandLineStringSplitter.Instance;

        var readOnlyCollection = splitter.Split(commandLine).ToArray();

        return parser.Parse(readOnlyCollection, commandLine);
    }
}