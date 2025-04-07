using System;
using System.IO;
using Std.CommandLine.Parsing;
using JetBrains.Annotations;


namespace Std.CommandLine.Arguments
{
    [PublicAPI]
    public sealed class FileSystemArgumentBuilder<TArg>
        where TArg : FileSystemInfo
    {
        private readonly Argument<TArg> _argument;

        internal FileSystemArgumentBuilder(Argument<TArg> argument)
        {
            _argument = argument;
        }

        public FileSystemArgumentBuilder<TArg> Required()
        {
            _argument.Arity = new ArgumentArity(1, 1);

            return this;
        }

        public FileSystemArgumentBuilder<TArg> MustExist()
        {
            _argument.MustExist();
            return this;
        }

        public FileSystemArgumentBuilder<TArg> ValidPath()
        {
            _argument.LegalFilePathsOnly();
            return this;
        }

        public FileSystemArgumentBuilder<TArg> Validator(Action<IArgumentResult> validator)
        {
            _argument.AddValidator(a =>
            {
                validator(a);
                return a.ErrorMessage;
            });

            return this;
        }

        public FileSystemArgumentBuilder<TArg> Name(string name)
        {
            _argument.Name = name;
            return this;
        }

        public FileSystemArgumentBuilder<TArg> Alias(string alias)
        {
            _argument.AddAlias(alias);
            return this;
        }

        public FileSystemArgumentBuilder<TArg> Description(string description)
        {
            _argument.Description = description;
            return this;
        }

        public FileSystemArgumentBuilder<TArg> Hidden()
        {
            _argument.IsHidden = true;
            return this;
        }
    }
}
