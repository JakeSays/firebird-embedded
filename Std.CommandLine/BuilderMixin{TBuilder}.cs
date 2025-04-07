// ReSharper disable InconsistentNaming
namespace Std.CommandLine
{
    public interface BuilderMixin<out TBuilder>
    {
        public TBuilder Name(string name);

        public TBuilder Alias(string alias);

        public TBuilder Description(string description);

        public TBuilder Hidden();
    }
}
