﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Std.CommandLine.Arguments;
using Std.CommandLine.Commands;
using Std.CommandLine.Options;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;
using JetBrains.Annotations;
using static Std.CommandLine.Help.DefaultHelpText;
// ReSharper disable UnusedMember.Global


namespace Std.CommandLine.Help
{
    [PublicAPI]
    public class HelpBuilder : IHelpBuilder
    {
        protected const int DefaultColumnGutter = 4;
        protected const int DefaultIndentationSize = 2;
        protected const int WindowMargin = 2;
        private int _indentationLevel;

        protected static SystemConsole Console => DefaultConsoles.StdOut;

        public int ColumnGutter { get; }

        public int IndentationSize { get; }

        public int MaxWidth { get; }

        /// <summary>
        /// Brokers the generation and output of help text of <see cref="Symbol"/>
        /// and the <see cref="SystemConsole"/>
        /// </summary>
        /// <param name="columnGutter">
        /// Number of characters to pad invocation information from their descriptions
        /// </param>
        /// <param name="indentationSize">Number of characters to indent new lines</param>
        /// <param name="maxWidth">
        /// Maximum number of characters available for each line to write to the console
        /// </param>
        public HelpBuilder(int? columnGutter = null,
            int? indentationSize = null,
            int? maxWidth = null)
        {
            ColumnGutter = columnGutter ?? DefaultColumnGutter;
            IndentationSize = indentationSize ?? DefaultIndentationSize;
            MaxWidth = maxWidth ?? GetConsoleWindowWidth();
        }

        /// <inheritdoc />
        public virtual void Write(ICommand command)
        {
            Guard.NotNull(command, nameof(command));

            AddSynopsis(command);
            AddUsage(command);
            AddArguments(command);
            AddOptions(command);
            AddSubcommands(command);
//            AddAdditionalArguments(command);
        }

        private void AddSubCommand(ICommand command)
        {
            Guard.NotNull(command, nameof(command));

            AddSynopsis(command);
//            AddUsage(command);
            AddArguments(command);
            AddOptions(command);
            AddSubcommands(command);
//            AddAdditionalArguments(command);
        }

        public virtual void Write(IOption option)
        {
            if (option is null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            var item = GetOptionHelpItems(option).ToList()[0];

            Console.Normal($"{item.Invocation}    {item.Description}");
        }

        protected int CurrentIndentation => _indentationLevel * IndentationSize;

        /// <summary>
        /// Increases the current indentation level
        /// </summary>
        protected void Indent(int levels = 1) => _indentationLevel += levels;

        /// <summary>
        /// Decreases the current indentation level
        /// </summary>
        protected void Outdent(int levels = 1)
        {
            if (_indentationLevel == 0)
            {
                throw new InvalidOperationException("Cannot outdent any further");
            }

            _indentationLevel -= levels;
        }

        /// <summary>
        /// Gets the currently available space based on the <see cref="MaxWidth"/>
        /// of the window and the current indentation level.
        /// </summary>
        /// <returns>
        /// The number of characters available on the current line. If no space is
        /// available then <see cref="int.MaxValue"/> is returned.
        /// </returns>
        protected int GetAvailableWidth()
        {
            var width = MaxWidth - CurrentIndentation - WindowMargin;

            return width > 0
                ? width
                : int.MaxValue;
        }

        /// <summary>
        /// Create a string of whitespace for the supplied number of characters
        /// </summary>
        /// <param name="width">The length of whitespace required</param>
        /// <returns>A string of <see cref="width"/> whitespace characters</returns>
        protected static string GetPadding(int width) => new(' ', width);

        /// <summary>
        /// Writes a blank line to the console
        /// </summary>
        private void AppendBlankLine() => Console.NormalLine();

        /// <summary>
        /// Writes whitespace to the console based on the provided offset,
        /// defaulting to the <see cref="CurrentIndentation"/>
        /// </summary>
        /// <param name="offset">Number of characters to pad</param>
        private void AppendPadding(int? offset = null)
        {
            var padding = GetPadding(offset ?? CurrentIndentation);
            Console.Normal(padding);
        }

        /// <summary>
        /// Writes a new line of text to the console, padded with a supplied offset
        /// defaulting to the <see cref="CurrentIndentation"/>
        /// </summary>
        /// <param name="text">The text content to write to the console</param>
        /// <param name="offset">Number of characters to pad the text</param>
        private void AppendLine(string text, int? offset = null)
        {
            AppendPadding(offset);
            Console.NormalLine(text ?? "");
        }

        /// <summary>
        /// Writes text to the console, padded with a supplied offset
        /// </summary>
        /// <param name="text">Text content to write to the console</param>
        /// <param name="offset">Number of characters to pad the text</param>
        private void AppendText(string text, int? offset = null)
        {
            AppendPadding(offset);
            Console.Normal(text ?? "");
        }

        /// <summary>
        /// Writes heading text to the console.
        /// </summary>
        /// <param name="heading">Heading text content to write to the console</param>
        /// <exception cref="ArgumentNullException"></exception>
        private void AppendHeading(string? heading) => AppendLine(heading ?? throw new ArgumentNullException(nameof(heading)));

        /// <summary>
        /// Writes a description block to the console
        /// </summary>
        /// <param name="description">Description text to write to the console</param>
        /// <exception cref="ArgumentNullException"></exception>
        private void AppendDescription(string description)
        {
            Guard.NotNull(description, nameof(description));

            var availableWidth = GetAvailableWidth();
            var descriptionLines = SplitText(description, availableWidth);

            foreach (var descriptionLine in descriptionLines)
            {
                AppendLine(descriptionLine, CurrentIndentation);
            }
        }

        /// <summary>
        /// Writes a collection of <see cref="HelpItem"/> to the console.
        /// </summary>
        /// <param name="helpItems">
        /// Collection of <see cref="HelpItem"/> to write to the console.
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual void AppendHelpItems(IReadOnlyList<HelpItem> helpItems)
        {
            Guard.NotNull(helpItems, nameof(helpItems));

            var table = CreateTable(helpItems,
                item => [item.Invocation, JoinNonEmpty(" ", item.Description, item.DefaultValueHint)]);

            var rows = table;
            var columnWidths = ColumnWidths(rows);
            AppendTable(rows, columnWidths);
        }

        /// <summary>
        /// Create a table of strings using the projection of a collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection of values to create the table from.</param>
        /// <param name="selector">A transformation function to apply to each element of <paramref name="collection"/>.</param>
        /// <returns>
        /// A table of strings whose elements are the projection of the collection with whitespace formatting removed.
        /// </returns>
        protected virtual IReadOnlyList<IReadOnlyList<string>> CreateTable<T>(IEnumerable<T> collection,
            Func<T, IEnumerable<string>> selector) =>
            collection.Select(selector)
                .Select(row => row
                    .Select(ShortenWhitespace)
                    .ToArray())
                .ToList();

        /// <summary>
        /// Allocate space for columns favoring minimal rows. Wider columns are allocated more space if it is available.
        /// </summary>
        /// <param name="table">The table of values to determine column widths for.</param>
        /// <returns>A collection of column widths.</returns>
        private IReadOnlyList<int> ColumnWidths(IReadOnlyList<IReadOnlyList<string>> table)
        {
            var rows = table;

            if (!rows.Any())
            {
                return [];
            }

            var columns = rows[0].Count;
            const int unsetWidth = -1;
            var widths = new int[columns];

            for (var i = 0; i < columns; ++i)
            {
                widths[i] = unsetWidth;
            }

            var maxWidths = new int[columns];

            for (var i = 0; i < columns; ++i)
            {
                maxWidths[i] = rows.Max(row => row[i].Length);
            }

            var nonEmptyColumns = maxWidths.Count(width => width > 0);

            // Usable width is the total available width minus space between columns.
            var available = GetAvailableWidth() - ColumnGutter * (nonEmptyColumns - 1);

            // If available space is not sufficent then do not wrap.
            // If all columns are empty then return array of zeros.
            if (available - nonEmptyColumns < 0 ||
                nonEmptyColumns == 0)
            {
                return maxWidths;
            }

            // Loop variables.
            var unset = nonEmptyColumns;
            var previousUnset = 0;
            // Limit looping to avoid O(columns^2) runtime.
            var loopLimit = 5;

            while (unset > 0)
            {
                var equal = (available - widths.Where(width => width > 0).Sum()) / unset;
                // Allocate remaining space equally if no other columns fit on a single line. Or if loop limit has been reached.
                var allocateRemaining = unset == previousUnset || loopLimit <= 1;

                for (var i = 0; i < columns; ++i)
                {
                    // If width has not been set.
                    if (widths[i] != unsetWidth)
                    {
                        continue;
                    }

                    // Attempt to fit column to single line.
                    var width = maxWidths[i];

                    if (allocateRemaining)
                    {
                        width = Math.Min(width, equal);
                    }

                    if (width <= equal)
                    {
                        widths[i] = width;
                    }
                }

                previousUnset = unset;
                unset = widths.Count(width => width < 0);
                --loopLimit;
            }

            return widths;
        }

        /// <summary>
        /// Writes a table of strings to the console.
        /// </summary>
        /// <param name="table">The table of values to write.</param>
        /// <param name="columnWidths">The width of each column of the table.</param>
        private void AppendTable(IEnumerable<IEnumerable<string>> table, IReadOnlyList<int> columnWidths)
        {
            foreach (var row in table)
            {
                AppendRow(row, columnWidths);
            }
        }

        /// <summary>
        /// Writes a row of strings to the console with columns of the given width.
        /// </summary>
        /// <param name="row">The row of elements to write.</param>
        /// <param name="columnWidths">The width of each column of the table.</param>
        private void AppendRow(IEnumerable<string> row, IReadOnlyList<int> columnWidths)
        {
            var split = row.Select((element, index) => SplitText(element, columnWidths[index])).ToArray();
            var longest = split.Max(lines => lines.Count);

            for (var line = 0; line < longest; ++line)
            {
                var columnStart = 0;
                var appended = 0;
                AppendPadding(CurrentIndentation);

                for (var column = 0; column < split.Length; ++column)
                {
                    var lines = split[column];

                    if (line < lines.Count)
                    {
                        var text = lines[line];

                        if (!string.IsNullOrEmpty(text))
                        {
                            var offset = columnStart - appended;
                            AppendText(text, offset);
                            appended += offset + text.Length;
                        }
                    }

                    columnStart += columnWidths[column] + ColumnGutter;
                }

                AppendBlankLine();
            }
        }

        /// <summary>
        /// Takes a string of text and breaks it into lines of <paramref name="width"/>
        /// characters. Whitespace formatting of the incoming text is removed.
        /// </summary>
        /// <param name="text">Text content to split into lines.</param>
        /// <param name="width">Maximum number of characters allowed per line.</param>
        /// <returns>
        /// Collection of lines of at most <paramref name="width"/> characters
        /// generated from the supplied <paramref name="text"/>.
        /// </returns>
        protected virtual IReadOnlyList<string> SplitText(string text, int width)
        {
            Guard.NotNull(text, nameof(text));
            Guard.GreaterEqual(width, 0, nameof(width));

            if (width == 0)
            {
                return [];
            }

            const char separator = ' ';

            var start = 0;
            var lines = new List<string>();
            text = ShortenWhitespace(text);

            while (start < text.Length - width)
            {
                var end = text.LastIndexOf(separator, start + width);

                // If last word starts before width / 2 include entire width.
                if (end - start <= width / 2)
                {
                    lines.Add(text.Substring(start, width));
                    // Start next line directly after current line. "abcdef" => abc|def
                    start += width;
                    continue;
                }

                lines.Add(text.Substring(start, end - start));
                // Move past separator for start of next line. "abc def" => abc| |def
                start = end + 1;
            }

            lines.Add(text.Substring(start, text.Length - start));

            return lines;
        }

        /// <summary>
        /// Formats the help rows for a given argument
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>A new <see cref="HelpItem"/></returns>
        private IEnumerable<HelpItem> GetArgumentHelpItems(ISymbol symbol)
        {
            foreach (var argument in symbol.Arguments())
            {
                if (!ShouldShowHelp(argument))
                {
                    continue;
                }

                var argumentDescriptor = ArgumentDescriptor(argument);

                var invocation = string.IsNullOrWhiteSpace(argumentDescriptor)
                    ? ""
                    : $"<{argumentDescriptor}>";

                var argumentDescription = argument?.Description ?? "";

                var defaultValueHint = argument != null
                    ? BuildDefaultValueHint(argument)
                    : null;

                yield return new HelpItem(invocation, argumentDescription, defaultValueHint);
            }

            string? BuildDefaultValueHint(IArgument argument)
            {
                var hint = DefaultValueHint(argument);

                return !string.IsNullOrWhiteSpace(hint)
                    ? $"[{hint}]"
                    : null;
            }
        }

        protected virtual string ArgumentDescriptor(IArgument argument)
        {
            if (argument.ValueType == typeof(bool) ||
                argument.ValueType == typeof(bool?))
            {
                return "";
            }

            return argument.Name;
        }

        protected virtual string DefaultValueHint(IArgument argument, bool isSingleArgument = true) =>
            (argument.HasDefaultValue, isSingleArgument, ShouldShowDefaultValueHint(argument)) switch
            {
                (true, true, true) => $"default: {argument.GetDefaultValue()}",
                (true, false, true) => $"{argument.Name}: {argument.GetDefaultValue()}",
                _ => ""
            };

        private IEnumerable<HelpItem> GetOptionHelpItems(ISymbol symbol)
        {
            var rawAliases = symbol
                .RawAliases
                .Select(r => r.SplitPrefix())
                .OrderBy(r => r.prefix, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.alias, StringComparer.OrdinalIgnoreCase)
                .GroupBy(t => t.alias)
                .Select(t => t.First())
                .Select(t => $"{t.prefix}{t.alias}");

            var invocation = string.Join(", ", rawAliases);

            if (ShouldShowHelp(symbol))
            {
                invocation = symbol.Arguments()
                    .Where(arg => arg != null!)
                    .Where(argument => ShouldShowHelp(argument) && !string.IsNullOrWhiteSpace(argument.Name))
                    .Select(ArgumentDescriptor)
                    .Where(argumentDescriptor => argumentDescriptor.NotNullOrWhiteSpace())
                    .Aggregate(invocation,
                        (current,
                            argumentDescriptor) => $"{current} <{argumentDescriptor}>");
            }

            if (symbol is IOption { Required: true })
            {
                invocation += " (REQUIRED)";
            }

            yield return new HelpItem(invocation,
                symbol.Description,
                BuildDefaultValueHint(symbol.Arguments()));

            string? BuildDefaultValueHint(IReadOnlyList<IArgument> arguments)
            {
                var defaultableArgumentCount = arguments
                    .Count(ShouldShowDefaultValueHint);

                var isSingleDefault = defaultableArgumentCount == 1;

                var argumentDefaultValues = arguments
                    .Where(ShouldShowDefaultValueHint)
                    .Select(argument => DefaultValueHint(argument, isSingleDefault));

                return defaultableArgumentCount > 0
                    ? $"[{string.Join(", ", argumentDefaultValues)}]"
                    : null;
            }
        }

        /// <summary>
        /// Writes a summary, if configured, for the supplied <see cref="command"/>
        /// </summary>
        /// <param name="command"></param>
        protected virtual void AddSynopsis(ICommand command)
        {
            if (!ShouldShowHelp(command))
            {
                return;
            }

            var title = $"{command.Name}:";
            HelpSection.WriteHeading(this, title, command.Description);
        }

        /// <summary>
        /// Writes the usage summary for the supplied <see cref="command"/>
        /// </summary>
        /// <param name="command"></param>
        protected virtual void AddUsage(ICommand command)
        {
            var usage = new List<string>();

            var subcommands = command is Command cmd
                ? cmd
                    .RecurseWhileNotNull(c => c.Parents
                        ?.OfType<Command>()
                        .FirstOrDefault())
                    .Reverse()
                : Enumerable.Empty<ICommand>();

            foreach (var subcommand in subcommands)
            {
                usage.Add(subcommand.Name);

                if (subcommand != command)
                {
                    usage.Add(FormatArgumentUsage(subcommand.Arguments.ToArray()));
                }
            }

            var hasOptionHelp = command.Children
                .OfType<IOption>()
                .Any(ShouldShowHelp);

            if (hasOptionHelp)
            {
                usage.Add(Usage.Options);
            }

            usage.Add(FormatArgumentUsage(command.Arguments.ToArray()));

            var hasCommandHelp = command.Children
                .OfType<ICommand>()
                .Any(ShouldShowHelp);

            if (hasCommandHelp)
            {
                usage.Add(Usage.Command);
            }

            // if (!command.TreatUnmatchedTokensAsErrors)
            // {
            //     usage.Add(Usage.AdditionalArguments);
            // }

            HelpSection.WriteHeading(this, Usage.Title,
                string.Join(" ", usage.Where(u => !string.IsNullOrWhiteSpace(u))));
        }

        private static string FormatArgumentUsage(IReadOnlyCollection<IArgument> arguments)
        {
            var sb = new StringBuilder();
            var args = new List<IArgument>(arguments.Where(ShouldShowHelp));
            var end = new Stack<string>();

            for (var i = 0; i < args.Count; i++)
            {
                var argument = args.ElementAt(i);

                var arityIndicator =
                    argument.Arity.MaximumNumberOfValues > 1
                        ? "..."
                        : "";

                var isOptional = IsOptional(argument);

                sb.Append(isOptional
                    ? $"[<{argument.Name}>{arityIndicator}"
                    : $"<{argument.Name}>{arityIndicator}");

                if (i < args.Count - 1)
                {
                    sb.Append(" ");
                }

                if (isOptional)
                {
                    end.Push("]");
                }
            }

            while (end.Count > 0)
            {
                sb.Append(end.Pop());
            }

            return sb.ToString();

            static bool IsMultiParented(IArgument argument) =>
                argument is Argument { Parents.Count: > 1 };

            bool IsOptional(IArgument argument) =>
                IsMultiParented(argument) ||
                argument.Arity.MinimumNumberOfValues == 0;
        }

        /// <summary>
        /// Writes the arguments, if any, for the supplied <see cref="command"/>
        /// </summary>
        /// <param name="command"></param>
        protected virtual void AddArguments(ICommand command)
        {
            var commands = new List<ICommand>();

            if (command is Command cmd &&
                cmd.Parents?.FirstOrDefault() is ICommand parent &&
                ShouldDisplayArgumentHelp(parent))
            {
                commands.Add(parent);
            }

            if (ShouldDisplayArgumentHelp(command))
            {
                commands.Add(command);
            }

            HelpSection.WriteItems(this,
                DefaultHelpText.Arguments.Title,
                commands.SelectMany(GetArgumentHelpItems).Distinct().ToArray());
        }

        /// <summary>
        /// Writes the <see cref="Option"/> help content, if any,
        /// for the supplied <see cref="command"/>
        /// </summary>
        /// <param name="command"></param>
        protected virtual void AddOptions(ICommand command)
        {
            var options = command
                .Children
                .OfType<IOption>()
                .Where(ShouldShowHelp)
                .ToArray();

            HelpSection.WriteItems(this,
                DefaultHelpText.Options.Title,
                options.SelectMany(GetOptionHelpItems).Distinct().ToArray());
        }

        /// <summary>
        /// Writes the help content of the <see cref="Command"/> subcommands, if any,
        /// for the supplied <see cref="command"/>
        /// </summary>
        /// <param name="command"></param>
        protected virtual void AddSubcommands(ICommand command)
        {
            var subcommands = command
                .Children
                .OfType<ICommand>()
                .Where(ShouldShowHelp)
                .ToArray();

            HelpSection.WriteItems(this,
                DefaultHelpText.Commands.Title,
                subcommands.SelectMany(GetOptionHelpItems).ToArray());

            // foreach (var subcommand in subcommands)
            // {
            //     AddSubCommand(subcommand);
            // }
        }

        protected virtual void AddAdditionalArguments(ICommand command)
        {
            if (command.TreatUnmatchedTokensAsErrors)
            {
                return;
            }

//            HelpSection.WriteHeading(this, AdditionalArguments.Title, AdditionalArguments.Description);
        }

        private static bool ShouldDisplayArgumentHelp(ICommand? command)
        {
            return command is not null && command.Arguments.Any(ShouldShowHelp);
        }

        private static int GetConsoleWindowWidth() => System.Console.WindowWidth;
            // console is Console systemConsole
            //     ? systemConsole.GetConsoleWindowWidth()
            //     : int.MaxValue;

        private static string ShortenWhitespace(string input) => Regex.Replace(input, @"\s+", " ");

        private static string JoinNonEmpty(string separator, params string?[] values) =>
            string.Join(separator, values.Where(str => !string.IsNullOrEmpty(str)));

        protected class HelpItem
        {
            public HelpItem(string invocation,
                string? description = null,
                string? defaultValueHint = null)
            {
                Invocation = invocation;
                Description = description ?? "";
                DefaultValueHint = defaultValueHint ?? "";
            }

            public string Invocation { get; }

            public string Description { get; }

            public string DefaultValueHint { get; }

            protected bool Equals(HelpItem? other) => (Invocation, Description) == (other?.Invocation, other?.Description);

            public override bool Equals(object? obj) => Equals((HelpItem?) obj);

            public override int GetHashCode() => (Invocation, Description).GetHashCode();

            public bool HasDefaultValueHint => !string.IsNullOrWhiteSpace(DefaultValueHint);
        }

        private static class HelpSection
        {
            public static void WriteHeading(HelpBuilder builder,
                string title,
                string? description = null)
            {
                if (!ShouldWrite(description, Array.Empty<ISymbol>()))
                {
                    return;
                }

                AppendHeading(builder, title);
                builder.Indent();
                AddDescription(builder, description);
                builder.Outdent();
                builder.AppendBlankLine();
            }

            public static void WriteItems(HelpBuilder builder,
                string title,
                IReadOnlyList<HelpItem> usageItems,
                string? description = null)
            {
                if (usageItems.Count == 0)
                {
                    return;
                }

                AppendHeading(builder, title);
                builder.Indent();

                AddDescription(builder, description);
                AddInvocation(builder, usageItems);

                builder.Outdent();
                builder.AppendBlankLine();
            }

            private static bool ShouldWrite(string? description, IReadOnlyCollection<ISymbol> usageItems)
            {
                if (description.NotNullOrWhiteSpace())
                {
                    return true;
                }

                return usageItems.Count > 0;
            }

            private static void AppendHeading(HelpBuilder builder, string? title = null)
            {
                if (title.IsNullOrWhiteSpace())
                {
                    return;
                }

                builder.AppendHeading(title);
            }

            private static void AddDescription(HelpBuilder builder, string? description = null)
            {
                if (description.IsNullOrWhiteSpace())
                {
                    return;
                }

                builder.AppendDescription(description!);
            }

            private static void AddInvocation(HelpBuilder builder, IReadOnlyList<HelpItem> helpItems) =>
                builder.AppendHelpItems(helpItems);
        }

        internal static bool ShouldShowHelp(ISymbol symbol) => !symbol.IsHidden;

        internal static bool ShouldShowDefaultValueHint(IArgument argument) =>
            argument.HasDefaultValue && ShouldShowHelp(argument);
    }
}
