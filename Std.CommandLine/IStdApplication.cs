using System.Collections.Generic;


namespace Std.CommandLine
{
    public interface IStdApplication
    {
        ICommandLineBuilder CommandLine { get; }
        int Run();

        public const int ExitCodeSuccess = 0;
        public const int ExitCodeFailure = 1;
    }
}
