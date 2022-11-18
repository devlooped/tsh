using System.Composition;
using System.Reflection;

namespace Terminal.Shell;

[Shared]
partial class ExtensionsManager : IExtensionsManager
{
    public event EventHandler? ExtensionsChanged;

    public IEnumerable<ExtensionInfo> Extensions
    {
        get => Enumerable.Empty<ExtensionInfo>();
        set { }
    }

    public void Install(string id, string version)
    {
        ExtensionsChanged?.Invoke(this, EventArgs.Empty);
    }

    public IExtensionsContext Load() => new ExtensionsContext();

    public void Uninstall(string id)
    {
        ExtensionsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Uninstall(Assembly assembly)
    {
        ExtensionsChanged?.Invoke(this, EventArgs.Empty);
    }

    class ExtensionsContext : IExtensionsContext
    {
        public void Dispose() { }
        public IEnumerable<Assembly> GetAssemblies() => Enumerable.Empty<Assembly>();
    }
}
