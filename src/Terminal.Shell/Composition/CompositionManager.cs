using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.Composition;

namespace Terminal.Shell;

class CompositionManager : ICompositionManager
{
    readonly string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Terminal.Shell", "ComponentModelCache");
    bool refresh;
    readonly IExtensionsManager extensions;

    public CompositionManager(IExtensionsManager extensions)
    {
        this.extensions = extensions;
        extensions.ExtensionsChanged += (_,_) => refresh = true;
    }

    public async Task<IComposition> CreateCompositionAsync(CancellationToken cancellation)
    {
        var cachePath = Path.Combine(cacheDir, "Terminal.Shell.cache");
        var cached = new CachedComposition();

        if (File.Exists(cachePath) && !refresh)
        {
            var cachedTime = File.GetLastWriteTime(cachePath);
            refresh =
                File.GetLastWriteTime(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName) > cachedTime ||
                File.GetLastWriteTime(typeof(IThreadingContext).Assembly.ManifestModule.FullyQualifiedName) > cachedTime;
        }

        if (!refresh && File.Exists(cachePath))
        {
            try
            {
                using var stream = File.OpenRead(cachePath);
                var loader = new CachedAssemblyLoader();
                var factory = await cached.LoadExportProviderFactoryAsync(stream, new Resolver(loader), cancellation);
                return new Composition(factory.CreateExportProvider(), loader);
            }
            catch (Exception)
            {
                // TODO: report failed to load from cache
            }
        }

        var context = extensions.Load();
        var assemblies = context.GetAssemblies();

        // See https://github.com/microsoft/vs-mef/blob/master/doc/hosting.md
        var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, true);
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            // Add Shell
            .AddParts(await discovery.CreatePartsAsync(typeof(ICompositionManager).Assembly, cancellation))
            // Add Shell.Sdk
            .AddParts(await discovery.CreatePartsAsync(typeof(IThreadingContext).Assembly, cancellation))
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

        
        try
        {
            Directory.CreateDirectory(cacheDir);
            using var stream = File.Create(cachePath);
            await cached.SaveAsync(config, stream, cancellation);
        }
        catch (Exception)
        {
            // TODO: report failure to save cache
        }

        return new Composition(provider, context);
    }

    class CachedAssemblyLoader : IAssemblyLoader, IDisposable
    {
        AssemblyLoadContext context = new("ComponentModelCache", true);
        
        public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
        {
            if (File.Exists(codeBasePath))
                return context.LoadFromAssemblyPath(codeBasePath);

            return context.LoadFromAssemblyName(new AssemblyName(assemblyFullName));
        }

        public Assembly LoadAssembly(AssemblyName assemblyName) 
            => context.LoadFromAssemblyName(assemblyName);

        public void Dispose() => context.Unload();
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
