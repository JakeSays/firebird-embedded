using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Std.CommandLine.Collections;
using Std.CommandLine.Commands;
using Std.CommandLine.Options;


namespace Std.CommandLine.Parsing
{
    internal class CommandLineLexer
    {
        private CommandLineConfiguration? _configuration;

        public TokenizeResult Tokenize(IReadOnlyList<string> args,
            CommandLineConfiguration configuration)
        {
            _configuration = configuration;
            var tokenList = new List<Token>();
            var errorList = new List<TokenizeError>();

            ICommand? currentCommand = null;
            var foundEndOfArguments = false;
            var foundEndOfDirectives = !configuration.EnableDirectives;
            var argList = NormalizeRootCommand(args);

            var argumentDelimiters = configuration.ArgumentDelimitersInternal
                ?? throw new InvalidOperationException("Delimiters are required");

            var knownTokens = GetValidTokens(configuration.RootCommand);

            for (var i = 0; i < argList.Count; i++)
            {
                var arg = argList[i];

                if (foundEndOfArguments)
                {
                    tokenList.Add(Operand(arg));
                    continue;
                }

                if (arg == "--")
                {
                    tokenList.Add(EndOfArguments());
                    foundEndOfArguments = true;
                    continue;
                }

                if (HandleDirectives(arg))
                {
                    continue;
                }

                if (HandleResponseFile(arg, i))
                {
                    continue;
                }

                arg = HandleBundling(arg, i);

                if (HandleValueOption(arg))
                {
                    continue;
                }

                if (!knownTokens.ContainsKey(arg) ||
                    currentCommand?.HasRawAlias(arg) == true)
                {
                    // if token matches the current command name, consider it an argument

                    tokenList.Add(Argument(arg));
                    continue;
                }

                if (knownTokens.TryGetValue(arg, out var token) &&
                    token.Type == TokenType.Option)
                {
                    tokenList.Add(Option(arg));
                    continue;
                }

                // when a subcommand is encountered, re-scope which tokens are valid

                var symbolSet = currentCommand is { } subcommand
                    ? subcommand.Children
                    : configuration.Symbols;

                currentCommand = (ICommand) symbolSet.GetByAlias(arg)!;

                knownTokens = GetValidTokens(currentCommand);

                tokenList.Add(Command(arg));
            }

            return new TokenizeResult(tokenList, errorList);

            bool CanBeUnbundled(string arg, out IReadOnlyCollection<string>? replacement)
            {
                replacement = null;

                if (tokenList.Count == 0)
                {
                    replacement = null;
                    return false;
                }

                // don't un-bundle if the last token is an option expecting an argument
                if (tokenList[^1] is { Type: TokenType.Option } lastToken &&
                    currentCommand?.Children.GetByAlias(lastToken.Value) is IOption { Argument.Arity.MinimumNumberOfValues: > 0 })
                {
                    return false;
                }

                var (prefix, alias) = arg.SplitPrefix();

                return prefix == "-" &&
                    TryUnbundle(out replacement);

                Token? TokenForOptionAlias(char c) =>
                    argumentDelimiters.Contains(c)
                        ? null
                        : knownTokens.Values.FirstOrDefault(token => token.Type == TokenType.Option && token.UnprefixedValue == c.ToString());

                void AddRestValue(List<string> list, string rest)
                {
                    if (argumentDelimiters.Contains(rest[0]))
                    {
                        list[^1] += rest;
                    }
                    else
                    {
                        list.Add(rest);
                    }
                }

                bool TryUnbundle(out IReadOnlyCollection<string>? newReplacement)
                {
                    if (string.IsNullOrEmpty(alias))
                    {
                        newReplacement = null;
                        return false;
                    }

                    var lastTokenHasArgument = false;
                    var builder = new List<string>();

                    for (var i = 0; i < alias.Length; i++)
                    {
                        var token = TokenForOptionAlias(alias[i]);

                        if (token is null)
                        {
                            if (lastTokenHasArgument)
                            {
                                // The previous token had an optional argument while the current
                                // character does not match any known tokens. Interpret this as
                                // the current character is the first char in the argument.
                                AddRestValue(builder, alias[i..]);
                                break;
                            }

                            // The previous token did not expect an argument, and the current
                            // character does not match an option, so unbundeling cannot be
                            // done.
                            newReplacement = null;
                            return false;
                        }

                        var opt = currentCommand?.Children.GetByAlias(token.Value) as IOption;
                        builder.Add(token.Value);

                        // Here we're at an impass, because if we don't have the `IOption`
                        // because we haven't received the correct command yet for instance,
                        // we will take the wrong decision. This is the same logic as the earlier
                        // `CanBeUnbundled` check to take the decision.
                        // A better option is probably introducing a new token-type, and resolve
                        // this after we have the correct model available.
                        var requiresArgument = opt?.Argument?.Arity.MinimumNumberOfValues > 0;
                        lastTokenHasArgument = opt?.Argument?.Arity.MaximumNumberOfValues > 0;

                        // If i == arg.Length - 1, we're already at the end of the string
                        // so no need for the custom handling of argument.
                        if (!requiresArgument ||
                            i >= alias.Length - 1)
                        {
                            continue;
                        }

                        // The current option requires an argument, and we're still in
                        // the middle of unbundling a string. Example: `-lsomelib.so`
                        // should be interpreted as `-l somelib.so`.
                        AddRestValue(builder, alias[(i + 1)..]);
                        break;
                    }

                    newReplacement = builder;
                    return true;
                }
            }

            void ReadResponseFile(string filePath, int i)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    errorList.Add(new TokenizeError($"Invalid response file token: {filePath}"));
                    return;
                }

                try
                {
                    var next = i + 1;

                    foreach (var newArg in ExpandResponseFile(filePath,
                        configuration.ResponseFileHandling))
                    {
                        argList!.Insert(next, newArg);
                        next += 1;
                    }
                }
                catch (FileNotFoundException)
                {
                    var message = configuration.ValidationMessages
                        .ResponseFileNotFound(filePath);

                    errorList.Add(new TokenizeError(message));
                }
                catch (IOException e)
                {
                    var message = configuration.ValidationMessages
                        .ErrorReadingResponseFile(filePath, e);

                    errorList.Add(new TokenizeError(message));
                }
            }

            bool HandleDirectives(string arg)
            {
                if (foundEndOfDirectives)
                {
                    return false;
                }

                if (arg.StartsWith("[") &&
                    arg.EndsWith("]") &&
                    arg[1] != ']' &&
                    arg[1] != ':')
                {
                    tokenList.Add(Directive(arg));
                    return true;
                }

                if (!configuration.RootCommand.HasRawAlias(arg))
                {
                    foundEndOfDirectives = true;
                }

                return false;
            }

            bool HandleResponseFile(string arg, int i)
            {
                if (configuration.ResponseFileHandling == ResponseFileHandling.Disabled ||
                    !(GetResponseFileReference(arg) is { } filePath))
                {
                    return false;
                }

                ReadResponseFile(filePath, i);
                return true;
            }

            string HandleBundling(string arg, int i)
            {
                if (!configuration.EnablePosixBundling ||
                    !CanBeUnbundled(arg, out var replacement) ||
                    replacement == null)
                {
                    return arg;
                }

                argList.InsertRange(i + 1, replacement);
                argList.RemoveAt(i);
                arg = argList[i];

                return arg;
            }

            bool HandleValueOption(string arg)
            {
                if (!TrySplitIntoSubtokens(arg, argumentDelimiters,
                    out var first,
                    out var rest))
                {
                    return false;
                }

                if (knownTokens.ContainsKey(first!))
                {
                    tokenList.Add(Option(first!));

                    // trim outer quotes in case of, e.g., -x="why"
                    var secondPartWithOuterQuotesRemoved = rest!.Trim('"');
                    tokenList.Add(Argument(secondPartWithOuterQuotesRemoved));
                }
                else
                {
                    tokenList.Add(Argument(arg));
                }

                return true;
            }
        }

        private List<string> NormalizeRootCommand(
            IReadOnlyList<string>? args)
        {
            args ??= new List<string>();

            var newArgs = new List<string>();

            string? potentialRootCommand = null;

            if (args.Count > 0)
            {
                try
                {
                    potentialRootCommand = Path.GetFileName(args[0]);
                }
                catch (ArgumentException)
                {
                    // possible exception for illegal characters in path on .NET Framework
                }

                if (potentialRootCommand != null &&
                    _configuration!.RootCommand.HasRawAlias(potentialRootCommand))
                {
                    newArgs.Add(potentialRootCommand);
                    newArgs.AddRange(args.Skip(1));
                    return newArgs;
                }
            }

            var commandName = _configuration!.RootCommand.Name;

            newArgs.Add(commandName);

            var startAt = 0;

            if (FirstArgMatchesRootCommand())
            {
                startAt = 1;
            }

            for (var i = startAt; i < args.Count; i++)
            {
                newArgs.Add(args[i]);
            }

            return newArgs;

            bool FirstArgMatchesRootCommand()
            {
                if (potentialRootCommand is null)
                {
                    return false;
                }

                if (potentialRootCommand.Equals($"{commandName}.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return potentialRootCommand.Equals($"{commandName}.exe", StringComparison.OrdinalIgnoreCase) ||
                    potentialRootCommand.Equals(commandName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string? GetResponseFileReference(string arg) =>
            arg.StartsWith("@") && arg.Length > 1
                ? arg.Substring(1)
                : null;

        private static bool TrySplitIntoSubtokens(string arg,
            IReadOnlyCollection<char> delimiters,
            out string? first,
            out string? rest)
        {
            var delimitersArray = delimiters.ToArray();

            for (var j = 0; j < delimiters.Count; j++)
            {
                var i = arg.IndexOfAny(delimitersArray);

                if (i < 0)
                {
                    continue;
                }

                first = arg[..i];

                rest = arg.Length > i
                    ? arg[(i + 1)..]
                    : null;

                return true;
            }

            first = null;
            rest = null;
            return false;
        }

        private static Token Argument(string value) => new Token(value, TokenType.Argument);

        private static Token Command(string value) => new Token(value, TokenType.Command);

        private static Token Option(string value) => new Token(value, TokenType.Option);

        private static Token EndOfArguments() => new Token("--", TokenType.EndOfArguments);

        private static Token Operand(string value) => new Token(value, TokenType.Operand);

        private static Token Directive(string value) => new Token(value, TokenType.Directive);

        private static IEnumerable<string> ExpandResponseFile(string filePath,
            ResponseFileHandling responseFileHandling)
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                foreach (var p in SplitLine(line))
                {
                    if (GetResponseFileReference(p) is { } path)
                    {
                        foreach (var q in ExpandResponseFile(path,
                            responseFileHandling))
                        {
                            yield return q;
                        }
                    }
                    else
                    {
                        yield return p;
                    }
                }
            }

            IEnumerable<string> SplitLine(string line)
            {
                var arg = line.Trim();

                if (arg.Length == 0 ||
                    arg.StartsWith("#"))
                {
                    yield break;
                }

                switch (responseFileHandling)
                {
                    case ResponseFileHandling.ParseArgsAsLineSeparated:

                        yield return line;

                        break;
                    case ResponseFileHandling.ParseArgsAsSpaceSeparated:

                        foreach (var word in CommandLineStringSplitter.Instance.Split(arg))
                        {
                            yield return word;
                        }

                        break;
                }
            }
        }

        private static Dictionary<string, Token> GetValidTokens(ICommand command)
        {
            var tokens = new Dictionary<string, Token>();

            foreach (var commandAlias in command.RawAliases)
            {
                tokens.Add(commandAlias,
                    new Token(commandAlias,
                        TokenType.Command));

                foreach (var child in command.Children)
                {
                    foreach (var childAlias in child.RawAliases)
                    {
                        tokens[childAlias] =
                            new Token(childAlias,
                                child is ICommand
                                    ? TokenType.Command
                                    : TokenType.Option);
                    }
                }
            }

            return tokens;
        }
    }
}
