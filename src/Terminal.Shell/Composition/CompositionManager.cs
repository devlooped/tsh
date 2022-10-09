using System.Reflection;
using Microsoft.VisualStudio.Composition;

namespace Terminal.Shell;

class CompositionManager : ICompositionManager
{
    readonly IExtensionsManager extensions;

    public CompositionManager(IExtensionsManager extensions) => this.extensions = extensions;

    public IComposition CreateComposition()
    {
        var context = extensions.Load();
        var assemblies = context.GetAssemblies();

        // See https://github.com/microsoft/vs-mef/blob/master/doc/hosting.md
        var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, true);
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            // Add Shell
            .AddParts(discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()).Result)
            // Add Shell.Sdk
            .AddParts(discovery.CreatePartsAsync(typeof(ShellView).Assembly).Result)
            .WithCompositionService();

        // Add parts from plugins
        foreach (var assembly in assemblies)
        {
            catalog = catalog.AddParts(discovery.CreatePartsAsync(assembly).Result);
        }

        foreach (var assemblyFile in catalog.DiscoveredParts.DiscoveryErrors.GroupBy(x => x.AssemblyPath).Select(x => x.Key))
        {
            if (assemblyFile != null)
                extensions.Uninstall(assemblyFile);
        }

        var config = CompositionConfiguration.Create(catalog);

        foreach (var assembly in config.CompositionErrors
            .SelectMany(error => error
            .SelectMany(diagnostic => diagnostic.Parts
            .Select(part => part.Definition.Type.Assembly)))
            .Distinct())
        {
            extensions.Uninstall(assembly);
        }

        var provider = config.CreateExportProviderFactory().CreateExportProvider();

        return new Composition(provider, context);
    }

    class Composition : IComposition
    {
        readonly ExportProvider exports;
        readonly IDisposable context;

        public Composition(ExportProvider exports, IDisposable context)
            => (this.exports, this.context)
            = (exports, context);

        public void Dispose()
        {
            exports.Dispose();
            // TODO: is this needed?
            GC.Collect();
            context.Dispose();
        }

        public Lazy<T, IDictionary<string, object>> GetExport<T>(string? contractName = null) => exports.GetExport<T, IDictionary<string, object>>(contractName);
        public Lazy<T, TMetadataView> GetExport<T, TMetadataView>(string? contractName = null) => exports.GetExport<T, TMetadataView>(contractName);
        public T GetExportedValue<T>(string? contractName = null) => exports.GetExportedValue<T>(contractName);
        public IEnumerable<T> GetExportedValues<T>(string? contractName = null) => exports.GetExportedValues<T>(contractName);
        public IEnumerable<Lazy<T, IDictionary<string, object>>> GetExports<T>(string? contractName = null) => exports.GetExports<T, IDictionary<string, object>>(contractName);
        public IEnumerable<Lazy<T, TMetadataView>> GetExports<T, TMetadataView>(string? contractName = null) => exports.GetExports<T, TMetadataView>(contractName);
    }
}
