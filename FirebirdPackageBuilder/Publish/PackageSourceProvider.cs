using NuGet.Configuration;


namespace Std.FirebirdEmbedded.Tools.Publish;

internal sealed class PackageSourceProvider : IPackageSourceProvider
{
    private readonly List<PackageSource> _sources = [];
    private PackageSource? _activeSource;
    private string? _defaultPushSource;

    public IEnumerable<PackageSource> LoadPackageSources() => _sources;

    public IReadOnlyList<PackageSource> LoadAuditSources() => [];

    public PackageSource? GetPackageSourceByName(string name) => _sources.FirstOrDefault(s => s.Name == name);

    public PackageSource? GetPackageSourceBySource(string source) => _sources.FirstOrDefault(s => s.Source == source);

    public void RemovePackageSource(string name)
    {
        var packageSource = GetPackageSourceByName(name);
        if (packageSource != null)
        {
            _sources.Remove(packageSource);
        }
    }

    public void EnablePackageSource(string name)
    {
        var packageSource = GetPackageSourceBySource(name);
        if (packageSource != null)
        {
            packageSource.IsEnabled = true;
        }
    }

    public void DisablePackageSource(string name)
    {
        var packageSource = GetPackageSourceBySource(name);
        if (packageSource != null)
        {
            packageSource.IsEnabled = false;
        }
    }

    public void UpdatePackageSource(PackageSource source, bool updateCredentials, bool updateEnabled)
    {
        //no-op
    }

    public void AddPackageSource(PackageSource source)
    {
        _sources.Add(source);
    }

    public void SavePackageSources(IEnumerable<PackageSource> sources)
    {
        //no-op
    }

    public bool IsPackageSourceEnabled(string name) => GetPackageSourceByName(name)?.IsEnabled ?? false;

    public void SaveActivePackageSource(PackageSource source)
    {
        _activeSource = source;
        _defaultPushSource = source.Source;
    }

    public string? ActivePackageSourceName => _activeSource?.Name;
    public string? DefaultPushSource => _defaultPushSource;

    //not used
    #pragma warning disable CS0067 // The event is never used
    public event EventHandler? PackageSourcesChanged;
    #pragma warning restore CS0067 // The event is never used
}
