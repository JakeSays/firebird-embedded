// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Std.CommandLine.Parsing;


namespace Std.CommandLine.Invocation
{
    internal class ParseDirectiveResult : IInvocationResult
    {
        public void Apply(InvocationContext context)
        {
            var parseResult = context.ParseResult;
            context.Console.NormalLine(parseResult.Diagram());
            context.ResultCode = parseResult.Errors.Count == 0
                                     ? 0
                                     : 1;
        }
    }
}
