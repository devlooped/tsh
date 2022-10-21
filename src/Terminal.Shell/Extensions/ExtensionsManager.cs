using System.Composition;
using System.Reflection;

namespace Terminal.Shell;

[Shared]
[Export(typeof(IExtensionsManager))]
class ExtensionsManager : IExtensionsManager
{
    public event EventHandler? ExtensionsChanged;

    public IEnumerable<ExtensionInfo> Extensions
    {
        get => Enumerable.Empty<ExtensionInfo>();
        set { }
    }

    public void Install(string id, string version) { }
    public IExtensionsContext Load() => new ExtensionsContext();
    public void Uninstall(string id) { }
    public void Uninstall(Assembly assembly) { }

    class ExtensionsContext : IExtensionsContext
    {
        public void Dispose() { }
        public IEnumerable<Assembly> GetAssemblies() => Enumerable.Empty<Assembly>();
    }
}
