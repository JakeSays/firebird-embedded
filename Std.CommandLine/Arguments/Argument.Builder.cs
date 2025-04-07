using System;
using Std.CommandLine.Options;
using Std.CommandLine.Parsing;
using Std.CommandLine.Utility;
using JetBrains.Annotations;


namespace Std.CommandLine.Arguments
{
    [PublicAPI]
    public sealed class ArgumentBuilder<TArg>
    {
        private readonly Argument<TArg> _argument;

        internal ArgumentBuilder(Argument<TArg> argument)
        {
            _argument = argument;
        }

        public ArgumentBuilder<TArg> Arity(int minValueCount, int maxValueCount = 0)
        {
            maxValueCount = maxValueCount == 0
                ? minValueCount
                : maxValueCount;

            _argument.Arity = new ArgumentArity(minValueCount, maxValueCount);

            return this;
        }

        public ArgumentBuilder<TArg> DefaultValue(TArg value)
        {
            _argument.SetDefaultValue(value);
            return this;
        }

        public ArgumentBuilder<TArg> DefaultValue(Func<TArg> valueProvider)
        {
            _argument.SetDefaultValueFactory(() => valueProvider);
            return this;
        }

        public ArgumentBuilder<TArg> DefaultValue(Func<ArgumentResult, TArg> valueProvider)
        {
            _argument.SetDefaultValueFactory(ar => valueProvider(ar));
            return this;
        }

        public ArgumentBuilder<TArg> Validator(Action<IArgumentResult> validator)
        {
            _argument.AddValidator(a =>
            {
                validator(a);
                return a.ErrorMessage;
            });

            return this;
        }

        public ArgumentBuilder<TArg> Name(string name)
        {
            _argument.Name = name;
            return this;
        }

        public ArgumentBuilder<TArg> Alias(string alias)
        {
            _argument.AddAlias(alias);
            return this;
        }

        public ArgumentBuilder<TArg> Description(string description)
        {
            _argument.Description = description;
            return this;
        }

        public ArgumentBuilder<TArg> Hidden()
        {
            _argument.IsHidden = true;
            return this;
        }
    }
}
