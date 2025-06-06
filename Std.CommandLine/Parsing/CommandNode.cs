﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using Std.CommandLine.Commands;


namespace Std.CommandLine.Parsing
{
    internal class CommandNode : NonterminalSyntaxNode
    {
//        public CommandNode(){}

        public CommandNode(
            Token token,
            ICommand command,
            CommandNode? parent) : base(token, parent)
        {
            if (token.Type != TokenType.Command)
            {
                throw new ArgumentException($"Incorrect token type: {token}");
            }

            Command = command;
        }

        public ICommand Command { get; }
    }
}
