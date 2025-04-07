// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.IO;
using JetBrains.Annotations;
using Std.CommandLine.Arguments;
using Std.CommandLine.Options;
using Std.CommandLine.Utility;


namespace Std.CommandLine.Commands
{
    [PublicAPI]
    public sealed class CommandBuilder : ICommandBuilder
    {
        internal CommandBuilder(Command command)
        {
            TheCommand = command;
        }

        internal Command TheCommand { get; }

        public ICommandBuilder Command(Action<ICommandBuilder> config)
        {
            Guard.NotNull(config, nameof(config));

            var cmd = new Command();
            TheCommand.AddCommand(cmd);

            config(new CommandBuilder(cmd));

            return this;
        }

        public ICommandBuilder Flag(Action<FlagOptionBuilder> config)
        {
            Guard.NotNull(config, nameof(config));

            var opt = new FlagOption();
            TheCommand.AddOption(opt);

            config(new FlagOptionBuilder(opt));

            return this;
        }

        public ICommandBuilder Option<TOpt>(Action<OptionBuilder<TOpt>> config)
        {
            Guard.NotNull(config, nameof(config));

            var option = new Option<TOpt>();
            TheCommand.AddOption(option);

            var builder =
                new OptionBuilder<TOpt>(option);

            config(builder);

            return this;
        }

        public ICommandBuilder Argument<TArg>(Action<ArgumentBuilder<TArg>> config)
        {
            Guard.NotNull(config, nameof(config));

            var arg = new Argument<TArg>();
            TheCommand.AddArgument(arg);

            config(new ArgumentBuilder<TArg>(arg));

            return this;
        }

        public ICommandBuilder FileArg(Action<FileSystemArgumentBuilder<FileInfo>> config)
        {
            Guard.NotNull(config, nameof(config));

            var arg = new Argument<FileInfo>();
            TheCommand.AddArgument(arg);

            config(new FileSystemArgumentBuilder<FileInfo>(arg));

            return this;
        }

        public ICommandBuilder DirectoryArg(Action<FileSystemArgumentBuilder<DirectoryInfo>> config)
        {
            Guard.NotNull(config, nameof(config));

            var arg = new Argument<DirectoryInfo>();
            TheCommand.AddArgument(arg);

            config(new FileSystemArgumentBuilder<DirectoryInfo>(arg));

            return this;
        }

        public ICommandBuilder Name(string name)
        {
            TheCommand.Name = name;

            if (!TheCommand.HasAlias(name))
            {
                TheCommand.AddAlias(name);
            }

            return this;
        }

        public ICommandBuilder Alias(string alias)
        {
            TheCommand.AddAlias(alias);
            return this;
        }

        public ICommandBuilder Description(string description)
        {
            TheCommand.Description = description;
            return this;
        }

        public ICommandBuilder Hidden()
        {
            TheCommand.IsHidden = true;
            return this;
        }

        HandlerTarget HandlerProvider<ICommandBuilder>.Target => TheCommand;

        ICommandBuilder HandlerProvider<ICommandBuilder>.Builder => this;
    }
}
