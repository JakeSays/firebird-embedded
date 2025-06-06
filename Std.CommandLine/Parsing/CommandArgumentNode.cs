﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using Std.CommandLine.Arguments;


namespace Std.CommandLine.Parsing
{
    internal class CommandArgumentNode : SyntaxNode
    {
        public CommandArgumentNode(
            Token token, 
            IArgument argument,
            CommandNode parent) : base(token, parent)
        {
            if (token.Type != TokenType.Argument)
            {
                throw new ArgumentException($"Incorrect token type: {token}");
            }

            Argument = argument;
            ParentCommandNode = parent;
        }

        public IArgument Argument { get; }

        public CommandNode ParentCommandNode { get; }
    }
}
