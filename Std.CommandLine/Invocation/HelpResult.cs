﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Std.CommandLine.Invocation
{
    internal class HelpResult : IInvocationResult
    {
        public void Apply(InvocationContext context)
        {
            context.BindingContext
                   .HelpBuilder
                   .Write(context.ParseResult.CommandResult.Command);

            System.Environment.Exit(0);
        }
    }
}
