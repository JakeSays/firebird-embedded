using System;
using System.IO;
using Std.CommandLine.Arguments;
using Std.CommandLine.Options;
using JetBrains.Annotations;


namespace Std.CommandLine.Commands
{
    [PublicAPI]
    public interface ICommandBuilder : HandlerProvider<ICommandBuilder>
    {
        ICommandBuilder Command(Action<ICommandBuilder> config);
        ICommandBuilder Flag(Action<FlagOptionBuilder> config);
        ICommandBuilder Option<TOpt>(Action<OptionBuilder<TOpt>> config);
        ICommandBuilder Argument<TArg>(Action<ArgumentBuilder<TArg>> config);
        ICommandBuilder Name(string name);
        ICommandBuilder Alias(string alias);
        ICommandBuilder Description(string description);
        ICommandBuilder Hidden();
        // CommandBuilder FileArg(Action<FileSystemArgumentBuilder<FileInfo>> config);
        // CommandBuilder DirectoryArg(Action<FileSystemArgumentBuilder<DirectoryInfo>> config);
    }
}
