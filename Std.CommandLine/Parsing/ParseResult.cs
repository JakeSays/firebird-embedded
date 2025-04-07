// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Std.CommandLine.Arguments;
using Std.CommandLine.Binding;
using Std.CommandLine.Commands;
using Std.CommandLine.Invocation;
using Std.CommandLine.Options;
using Std.CommandLine.Utility;


namespace Std.CommandLine.Parsing
{
    internal class ParseResult
    {
        private readonly List<ParseError> _errors;
        private readonly RootCommandResult _rootCommandResult;

        internal ParseResult(
            CommandLineParser parser,
            RootCommandResult rootCommandResult,
            CommandResult commandResult,
            IDirectiveCollection directives,
            TokenizeResult tokenizeResult,
            IReadOnlyCollection<string> unparsedTokens,
            IReadOnlyCollection<string> unmatchedTokens,
            List<ParseError>? errors = null,
            string? rawInput = null)
        {
            Parser = parser;
            _rootCommandResult = rootCommandResult;
            CommandResult = commandResult;
            Directives = directives;

            // skip the root command
            Tokens = tokenizeResult.Tokens.Skip(1).ToArray();

            UnparsedTokens = unparsedTokens;
            UnmatchedTokens = unmatchedTokens;

            RawInput = rawInput;

            _errors = errors ?? [];

            if (parser.Configuration.RootCommand.TreatUnmatchedTokensAsErrors)
            {
                _errors.AddRange(unmatchedTokens.Select(token =>
                    new ParseError(parser.Configuration.ValidationMessages.UnrecognizedCommandOrArgument(token))));
            }
        }

        public CommandResult CommandResult { get; }

        internal CommandLineParser Parser { get; }

        public CommandResult RootCommandResult => _rootCommandResult;

        public IReadOnlyCollection<ParseError> Errors => _errors;

        public IDirectiveCollection Directives { get; }

        public IReadOnlyList<Token> Tokens { get; }

        public IReadOnlyCollection<string> UnmatchedTokens { get; }

        internal string? RawInput { get; }

        public IReadOnlyCollection<string> UnparsedTokens { get; }

        public object? ValueForOption(string alias) =>
            ValueForOption<object?>(alias);

        [return: MaybeNull]
        public T ValueForOption<T>(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(alias));
            }

            if (this[alias] is OptionResult optionResult)
            {
                return optionResult.GetValueOrDefault<T>();
            }
            else
            {
                return default!;
            }
        }

        public SymbolResult? this[string alias] => CommandResult.Children[alias];

        public override string ToString() => $"{nameof(ParseResult)}: {this.Diagram()}";

        public ArgumentResult? FindResultFor(IArgument argument) =>
            _rootCommandResult.FindResultFor(argument);

        public CommandResult? FindResultFor(ICommand command) =>
            _rootCommandResult.FindResultFor(command);

        public OptionResult? FindResultFor(IOption option) =>
            _rootCommandResult.FindResultFor(option);

        public int Invoke() => InvocationPipeline.Invoke(this);

        public string TextToMatch(int? position = null)
        {
            var lastToken = Tokens.LastOrDefault(t => t.Type != TokenType.Directive);

            string? textToMatch = null;
            var rawInput = RawInput;

            if (rawInput != null)
            {
                if (position != null)
                {
                    if (position > rawInput.Length)
                    {
                        rawInput += ' ';
                        position = Math.Min(rawInput.Length, position.Value);
                    }
                }
                else
                {
                    position = rawInput.Length;
                }
            }
            else if (lastToken?.Value != null)
            {
                position = null;
                textToMatch = lastToken.Value;
            }

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                if (UnmatchedTokens.Any() ||
                    lastToken?.Type == TokenType.Argument)
                {
                    return textToMatch ?? "";
                }
            }
            else
            {
                var textBeforeCursor = rawInput!.Substring(0, position!.Value);

                var textAfterCursor = rawInput.Substring(position.Value);

                return textBeforeCursor.Split(' ').LastOrDefault() +
                    textAfterCursor.Split(' ').FirstOrDefault();
            }

            return "";
        }

        public string Diagram()
        {
            var builder = new StringBuilder();

            Diagram(builder, RootCommandResult);

            if (!UnmatchedTokens.Any())
            {
                return builder.ToString();
            }

            builder.Append("   ???-->");

            foreach (var error in UnmatchedTokens)
            {
                builder.Append(" ");
                builder.Append(error);
            }

            return builder.ToString();
        }

        private void Diagram(StringBuilder builder,
            SymbolResult symbolResult)
        {
            if (Errors.Any(e => e.SymbolResult == symbolResult))
            {
                builder.Append("!");
            }

            if (symbolResult is OptionResult optionResult &&
                optionResult.IsImplicit)
            {
                builder.Append("*");
            }

            if (!(symbolResult is ArgumentResult argumentResult))
            {
                builder.Append("[ ");
                builder.Append(symbolResult.Token().Value);

                foreach (var child in symbolResult.Children)
                {
                    builder.Append(" ");
                    Diagram(builder, child);
                }

                builder.Append(" ]");
                return;
            }

            var includeArgumentName =
                argumentResult.Argument is Argument argument &&
                argument.Parents?.First() is ICommand command &&
                command.Name != argument.Name;

            if (includeArgumentName)
            {
                builder.Append("[ ");
                builder.Append(argumentResult.Argument.Name);
                builder.Append(' ');
            }

            switch (argumentResult.GetArgumentConversionResult())
            {
                case SuccessfulArgumentConversionResult successful:

                    switch (successful.Value)
                    {
                        case string s:
                            builder.Append($"<{s}>");
                            break;

                        case IEnumerable items:
                            builder.Append("<");

                            builder.Append(string.Join("> <",
                                items.Cast<object>().ToArray()));

                            builder.Append(">");
                            break;

                        default:
                            builder.Append("<");
                            builder.Append(successful.Value);
                            builder.Append(">");
                            break;
                    }

                    break;

                case FailedArgumentConversionResult _:

                    builder.Append("<");
                    builder.Append(string.Join("> <", symbolResult.Tokens.Select(t => t.Value)));
                    builder.Append(">");

                    break;
            }

            if (includeArgumentName)
            {
                builder.Append(" ]");
            }
        }

        public bool HasOption(IOption option) => CommandResult.Children.Any(s => s.Symbol == option);

        public bool HasOption(string alias) => CommandResult.Children.Contains(alias);
    }
}
