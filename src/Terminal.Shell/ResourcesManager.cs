using System.Collections.Concurrent;
using System.Composition;
using System.Globalization;
using System.Resources;

namespace Terminal.Shell;

[Shared]
partial class ResourcesManager : IResourceManager
{
    readonly ConcurrentDictionary<string, string?> strings = new(StringComparer.OrdinalIgnoreCase);
    readonly ConcurrentDictionary<string, List<Lazy<ResourceManager>>> resources = new(StringComparer.OrdinalIgnoreCase);

    [ImportingConstructor]
    internal ResourcesManager(
        [ImportMany] IEnumerable<Lazy<ResourceManager, IDictionary<string, object>>> resources)
    {
        foreach (var resource in resources)
        {
            // Invariant culture
            if (!resource.Metadata.TryGetValue("Culture", out var culture))
                this.resources.GetOrAdd("", _ => new())
                    .Add(resource);
            // Single culture shows in metadata as a single string value
            else if (culture is string name)
                this.resources.GetOrAdd(name, _ => new())
                    .Add(resource);
            // Multiple cultures become string arrays in metadata
            else if (culture is string[] names)
            {
                foreach (var n in names)
                    this.resources.GetOrAdd(n, _ => new())
                        .Add(resource);
            }
        }
    }

    public string? GetString(string name) => strings.GetOrAdd(name, key =>
    {
        CultureInfo? culture = null;
        do
        {
            culture = culture == null ? Thread.CurrentThread.CurrentUICulture : culture.Parent;
            if (resources.TryGetValue(culture.Name, out var managers))
            {
                foreach (var manager in managers)
                {
                    // This avoids performing resource fallback which is the default behavior 
                    // when doing ResourceManager.GetString. This way we can retrieve the most 
                    // optimally localized value from across multiple resource managers.
                    if (manager.Value.GetResourceSet(culture, true, false) is ResourceSet set)
                    {
                        var value = set.GetString(name, true);
                        if (!string.IsNullOrEmpty(value))
                            return value;
                    }
                }
            }
        } while (culture.Parent != culture); // Parent of invariant culture is itself \o/

        return null;
    });
}