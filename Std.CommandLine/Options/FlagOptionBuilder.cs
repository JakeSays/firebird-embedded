using Std.CommandLine.Arguments;
using JetBrains.Annotations;



namespace Std.CommandLine.Options
{
    [PublicAPI]
    public sealed class FlagOptionBuilder
    {
        internal FlagOptionBuilder(FlagOption option)
        {
            Option = option;
            // var arg = new Argument<bool>
            // {
            //     Arity = new ArgumentArity(1, 1),
            // };
            //
            // Option.SetArgument(arg);

//            Option.TypedArgument.SetDefaultValue(true);
        }

        internal FlagOption Option { get; }

        // private Argument<bool> GetArg()
        // {
        //     var arg = Option.TypedArgument;
        //
        //     if (arg != null)
        //     {
        //         return arg;
        //     }
        //
        //     arg = new Argument<bool>();
        //     Option.SetArgument(arg);
        //     arg.Arity = new ArgumentArity(0, 0);
        //
        //     return arg;
        // }

        public FlagOptionBuilder Name(string name)
        {
            Option.Name = name;
            return this;
        }

        public FlagOptionBuilder Alias(string alias)
        {
            Option.AddAlias(alias);
            return this;
        }

        public FlagOptionBuilder Description(string description)
        {
            Option.Description = description;
            return this;
        }

        public FlagOptionBuilder Hidden()
        {
            Option.IsHidden = true;
            return this;
        }

        // public FlagOptionBuilder Required()
        // {
        //     Option.Required = true;
        //     return this;
        // }
    }
}
