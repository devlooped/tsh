using System.Reflection;

namespace Terminal.Shell;

interface IExtensionsManager
{
    IEnumerable<ExtensionInfo> Extensions { get; set; }

    IExtensionsContext Load();

    void Install(string id, string version);

    void Uninstall(string id);

    void Uninstall(Assembly assembly);
}
