using Std.FirebirdEmbedded.Tools.MetaData;


namespace Std.FirebirdEmbedded.Tools.Common;

internal abstract class ToolResult
{
    public abstract bool Success { get; }
}

internal abstract class Tool<TConfiguration, TArgs, TResult> : IDisposable
    where TConfiguration : ToolConfiguration
    where TResult : ToolResult, new()
{
    public TConfiguration Config { get; }

    public PackageMetadata Metadata { get; private set; } = null!;

    public TResult Run(TArgs args)
    {
        var metadata = MetadataSerializer.Load(Config.MetadataFilePath);
        if (metadata == null)
        {
            return new TResult();
        }
        Metadata = metadata;

        var result = Execute(args);

        if (result.Success &&
            Metadata.Changed)
        {
            MetadataSerializer.Save(Metadata, Config.MetadataFilePath);
        }

        return result;
    }

    private protected abstract TResult Execute(TArgs args);

    private protected Tool(TConfiguration configuration)
    {
        Config = configuration;
    }

    public virtual void Dispose()
    {
    }
}
