﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Std.CommandLine.Arguments;
using Std.CommandLine.Binding;
using Std.CommandLine.Commands;
using Std.CommandLine.Options;
using Std.CommandLine.Utility;


namespace Std.CommandLine.Parsing
{
    internal class ParseOperation
    {
        private readonly TokenizeResult _tokenizeResult;
        private readonly CommandLineConfiguration _configuration;
        private int _index;
        private readonly Dictionary<IArgument, int> _argumentCounts = new Dictionary<IArgument, int>();

        public ParseOperation(
            TokenizeResult tokenizeResult,
            CommandLineConfiguration configuration)
        {
            _tokenizeResult = tokenizeResult;
            _configuration = configuration;
        }

        private Token CurrentToken => _tokenizeResult.Tokens[_index];

        public List<ParseError> Errors { get; } = [];

        public RootCommandNode? RootCommandNode { get; private set; }

        public List<Token> UnmatchedTokens { get; } = [];

        public List<Token> UnparsedTokens { get; } = [];

        private void Advance()
        {
            _index++;
        }

        private void IncrementCount(IArgument argument)
        {
            if (!_argumentCounts.TryGetValue(argument, out var count))
            {
                count = 0;
            }

            _argumentCounts[argument] = count + 1;
        }

        private bool IsFull(IArgument argument)
        {
            Guard.NotNull(argument, nameof(argument));

            var count = _argumentCounts.GetValueOrDefault(argument, 0);

            return count >= argument.Arity.MaximumNumberOfValues;
        }

        private bool More()
        {
            return _index < _tokenizeResult.Tokens.Count;
        }

        public void Parse()
        {
            RootCommandNode = ParseRootCommand();
        }

        private RootCommandNode ParseRootCommand()
        {
            var rootCommandNode = new RootCommandNode(
                CurrentToken,
                _configuration.RootCommand);

            Advance();

            ParseDirectives(rootCommandNode);

            ParseCommandChildren(rootCommandNode);

            ParseRemainingTokens();

            return rootCommandNode;
        }

        private CommandNode? ParseSubcommand(CommandNode parentNode)
        {
            if (CurrentToken.Type != TokenType.Command)
            {
                return null;
            }

            var command = parentNode.Command
                                    .Children
                                    .GetByAlias(CurrentToken.Value) as ICommand;

            if (command is null)
            {
                return null;
            }

            var commandNode = new CommandNode(CurrentToken, command, parentNode);

            Advance();

            ParseCommandChildren(commandNode);

            return commandNode;
        }

        private void ParseCommandChildren(CommandNode parent)
        {
            while (More())
            {
                if (CurrentToken.Type == TokenType.EndOfArguments)
                {
                    return;
                }

                var child = ParseSubcommand(parent) ??
                            (SyntaxNode?)ParseOption(parent) ??
                            ParseCommandArgument(parent);

                if (child is null)
                {
                    UnmatchedTokens.Add(CurrentToken);
                    Advance();
                }
                else
                {
                    parent.AddChildNode(child);
                }
            }
        }

        private CommandArgumentNode? ParseCommandArgument(CommandNode commandNode)
        {
            if (CurrentToken.Type != TokenType.Argument)
            {
                return null;
            }

            var argument = commandNode.Command
                                      .Arguments
                                      .FirstOrDefault(a => !IsFull(a));

            if (argument is null)
            {
                return null;
            }

            var argumentNode = new CommandArgumentNode(
                CurrentToken,
                argument,
                commandNode);

            IncrementCount(argument);

            Advance();

            return argumentNode;
        }

        private OptionNode? ParseOption(CommandNode parent)
        {
            if (CurrentToken.Type != TokenType.Option)
            {
                return null;
            }

            if (parent.Command.Children.GetByAlias(CurrentToken.Value) is not IOption option)
            {
                return null;
            }

            var optionNode = new OptionNode(
                CurrentToken,
                option,
                parent);

            Advance();

            ParseOptionArguments(optionNode);

            return optionNode;
        }

        private void ParseOptionArguments(OptionNode optionNode)
        {
            if (optionNode.Option is FlagOption)
            {
                return;
            }

            var argument = optionNode.Option.Argument;

            var contiguousTokens = 0;

            while (More() &&
                   CurrentToken.Type == TokenType.Argument)
            {
                if (IsFull(argument!))
                {
                    if (contiguousTokens > 0)
                    {
                        return;
                    }

                    if (argument!.Arity.MaximumNumberOfValues == 0)
                    {
                        return;
                    }
                }
                else if (argument!.ValueType == typeof(bool))
                {
                    if (ArgumentConverter.ConvertObject(
                            argument,
                            argument.ValueType,
                            CurrentToken.Value) is FailedArgumentTypeConversionResult)
                    {
                        return;
                    }
                }

                optionNode.AddChildNode(
                    new OptionArgumentNode(
                        CurrentToken,
                        argument,
                        optionNode));

                IncrementCount(argument);

                contiguousTokens++;

                Advance();
            }
        }

        private void ParseDirectives(RootCommandNode parent)
        {
            while (More())
            {
                var token = CurrentToken;

                if (token.Type != TokenType.Directive)
                {
                    return;
                }

                var withoutBrackets = token.Value.Substring(1, token.Value.Length - 2);
                var keyAndValue = withoutBrackets.Split([
                    ':'
                ], 2);

                var key = keyAndValue[0];
                var value = keyAndValue.Length == 2
                                ? keyAndValue[1]
                                : null;

                var directiveNode = new DirectiveNode(token, parent, key, value);

                parent.AddChildNode(directiveNode);

                Advance();
            }
        }

        private void ParseRemainingTokens()
        {
            var foundEndOfArguments = false;

            while (More())
            {
                if (CurrentToken.Type == TokenType.EndOfArguments)
                {
                    foundEndOfArguments = true;
                }
                else if (foundEndOfArguments)
                {
                    UnparsedTokens.Add(CurrentToken);
                }
                else
                {
                    UnmatchedTokens.Add(CurrentToken);
                }

                Advance();
            }
        }
    }
}
