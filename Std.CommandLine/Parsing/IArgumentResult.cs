using System.Collections.Generic;
using Std.CommandLine.Arguments;


namespace Std.CommandLine.Parsing
{
    public interface IArgumentResult
    {
        IArgument Argument { get; }
        string? ErrorMessage { get; set; }
        ISymbol Symbol { get; }
        IReadOnlyList<Token> Tokens { get; }
    }
}
