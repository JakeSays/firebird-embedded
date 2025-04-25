using System;
using Std.CommandLine.Arguments;
using JetBrains.Annotations;


namespace Std.CommandLine.Options
{
    [PublicAPI]
    public sealed class OptionBuilder<TOpt>
    {
        internal OptionBuilder(Option<TOpt> option)
        {
            Option = option;
            var arg = new Argument<TOpt>();
            Option.SetArgument(arg);
        }

        internal Option<TOpt> Option { get; }

        private Argument<TOpt> GetArg()
        {
            var arg = Option.TypedArgument;

            if (arg != null!)
            {
                return arg;
            }

            arg = new Argument<TOpt>();
            Option.SetArgument(arg);

            return arg;
        }

        public OptionBuilder<TOpt> Singleton()
        {
            GetArg().SetArity(0, 1);
            return this;
        }

        public OptionBuilder<TOpt> Count(int min, int max)
        {
            GetArg().SetArity(min, max);
            return this;
        }

        public OptionBuilder<TOpt> Argument(Action<ArgumentBuilder<TOpt>> config)
        {
            config?.Invoke(new ArgumentBuilder<TOpt>(GetArg()));
            return this;
        }

        public OptionBuilder<TOpt> Arg(Action<ArgumentBuilder<TOpt>> config)
        {
            config?.Invoke(new ArgumentBuilder<TOpt>(GetArg()));
            return this;
        }

        public OptionBuilder<TOpt> Parser(Func<string?, (TOpt Result, string? ParseError)> parser)
        {
            GetArg().SetParser(parser);
            return this;
        }

        public OptionBuilder<TOpt> DefaultValue(TOpt value)
        {
            GetArg().SetDefaultValue(value);
            return this;
        }

        public OptionBuilder<TOpt> DefaultValueProvider(Func<TOpt> valueProvider)
        {
            GetArg().SetDefaultValueFactory(() => valueProvider);
            return this;
        }

        public OptionBuilder<TOpt> Name(string name)
        {
            Option.Name = name;
            return this;
        }

        public OptionBuilder<TOpt> Alias(string alias)
        {
            Option.AddAlias(alias);
            return this;
        }

        public OptionBuilder<TOpt> Description(string description)
        {
            Option.Description = description;
            return this;
        }

        public OptionBuilder<TOpt> Hidden(bool hidden = true)
        {
            Option.IsHidden = hidden;
            return this;
        }

        public OptionBuilder<TOpt> Required(bool required = true)
        {
            Option.Required = required;
            return this;
        }
    }
}
