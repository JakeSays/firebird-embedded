namespace Std.FirebirdEmbedded.Tools;

internal enum ProductId
{
    V3,
    V4,
    V5,
    AssetManager
}

internal static class FirebirdVersionExtensions
{
    public static string NativeBinarySuffix(this ProductId version) =>
        version switch
        {
            ProductId.V3 => "12",
            ProductId.V4 => "13",
            ProductId.V5 => "13",
            ProductId.AssetManager => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
}
