// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Std.CommandLine.Binding;
using Std.CommandLine.Commands;
using Std.CommandLine.Parsing;


namespace Std.CommandLine.Invocation
{
    internal static class InvocationPipeline
    {
        public static int Invoke(ParseResult parseResult)
        {
            var context = new InvocationContext(parseResult);

            InvocationMiddleware invocationChain = BuildInvocationChain(context);

            invocationChain.Invoke(context, invocationContext => invocationContext.ResultCode);

            return GetResultCode(context);
        }

        public static int Invoke(BindingContext bindingContext)
        {
            var context = new InvocationContext(bindingContext);

            InvocationMiddleware invocationChain = BuildInvocationChain(context);

            invocationChain.Invoke(context, invocationContext => invocationContext.ResultCode);

            return GetResultCode(context);
        }

        private static InvocationMiddleware BuildInvocationChain(InvocationContext context)
        {
            var invocations = new List<InvocationMiddleware>(context.Parser.Configuration.Middleware)
            {
                (invocationContext, next) =>
                {
                    if (!(invocationContext
                        .ParseResult
                        .CommandResult
                        .Command is Command command))
                    {
                        return context.ResultCode;
                    }

                    var handler = command.Handler;

                    if (handler != null)
                    {
                        context.ResultCode = handler.Invoke(invocationContext);
                    }

                    return context.ResultCode;
                }
            };


            return invocations.Aggregate(
                (first, second) =>
                    (ctx, next) =>
                        first(ctx,
                            c => second(c, next)));
        }

        private static int GetResultCode(InvocationContext context)
        {
            context.InvocationResult?.Apply(context);

            return context.ResultCode;
        }
    }
}
