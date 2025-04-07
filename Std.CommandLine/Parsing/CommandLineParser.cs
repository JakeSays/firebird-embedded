// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;

namespace Std.CommandLine.Parsing
{
    internal class CommandLineParser
    {
        public CommandLineParser(CommandLineConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public CommandLineConfiguration Configuration { get; }

        public ParseResult Parse(
            IReadOnlyList<string> arguments,
            string? rawInput = null)
        {
            var lexer = new CommandLineLexer();
            var tokenizeResult = lexer.Tokenize(arguments, Configuration);

            var operation = new ParseOperation(
                tokenizeResult,
                Configuration);

            operation.Parse();

            var visitor = new ParseResultVisitor(
                this,
                tokenizeResult,
                operation.UnparsedTokens,
                operation.UnmatchedTokens,
                operation.Errors,
                rawInput);

            visitor.Visit(operation.RootCommandNode!);

            return visitor.Result;
        }
    }
}
