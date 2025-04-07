using System.Reflection;
using System.Text;


namespace Std.FirebirdEmbedded.Tools.Assets;

internal static class ManifestResourceManager
{
	private const string Prefix = $"{nameof(Std)}.{nameof(FirebirdEmbedded)}.{nameof(Tools)}.{nameof(Assets)}.";
	private static readonly Assembly ResourceAssembly = typeof(ManifestResourceManager).Assembly;

	public static string[] GetResourceNames()
	{
		var resourceNames = ResourceAssembly.GetManifestResourceNames();
		return resourceNames;
	}

	private static string ResourceName(string resourceName)
	{
		ArgumentException.ThrowIfNullOrEmpty(resourceName);

		if (!resourceName.StartsWith(Prefix))
		{
			resourceName = Prefix + resourceName;
		}

		return resourceName;
	}

	public static string? ReadStringResource(string resourceName)
	{
		using var rstream = ResourceAssembly.GetManifestResourceStream(ResourceName(resourceName));
		if (rstream == null)
		{
			return null;
		}

		using var reader = new StreamReader(rstream, Encoding.UTF8);
		var data = reader.ReadToEnd();
		return data;
	}

	public static Stream? GetResourceStream(string resourceName)
	{
		var rstream = ResourceAssembly.GetManifestResourceStream(ResourceName(resourceName));
		return rstream;
	}

	public static byte[]? ReadBinaryResource(string resourceName)
	{
		using var rstream = ResourceAssembly.GetManifestResourceStream(ResourceName(resourceName));
		if (rstream == null)
		{
			return null;
		}

		var resourceLength = rstream.Length;
		var data = new byte[resourceLength];
		_ = rstream.Read(data, 0, (int) resourceLength);

		return data;
	}
}
