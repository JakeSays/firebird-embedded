using Std.CommandLine.Options;


namespace Std.CommandLine.Parsing
{
    internal interface IOptionResult
    {
        IOption Option { get; }
        bool IsImplicit { get; }
        string? ErrorMessage { get; set; }
    }
}
