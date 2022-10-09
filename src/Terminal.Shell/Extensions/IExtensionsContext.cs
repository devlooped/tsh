using System.Reflection;

namespace Terminal.Shell;

interface IExtensionsContext : IDisposable
{
    IEnumerable<Assembly> GetAssemblies();
}