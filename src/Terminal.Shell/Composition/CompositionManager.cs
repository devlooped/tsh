using System.Collections;
using System.Composition;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;
using ContractNameServices = System.ComponentModel.Composition.AttributedModelServices;

namespace Terminal.Shell;

class CompositionManager : ICompositionManager
{
    readonly string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Terminal.Shell", "ComponentModelCache");
    bool refresh;
    readonly IExtensionsManager extensions;

    public CompositionManager(IExtensionsManager extensions)
    {
        this.extensions = extensions;
        extensions.ExtensionsChanged += (_, _) => refresh = true;
    }

    public async Task<IComposition> CreateCompositionAsync(bool cached = true, CancellationToken cancellation = default)
    {
        var cachePath = Path.Combine(cacheDir, "Terminal.Shell.cache");
        var cache = new CachedComposition();

        if (File.Exists(cachePath) && !refresh)
        {
            var cachedTime = File.GetLastWriteTime(cachePath);
            refresh =
                File.GetLastWriteTime(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName) > cachedTime ||
                File.GetLastWriteTime(typeof(IThreadingContext).Assembly.ManifestModule.FullyQualifiedName) > cachedTime;
        }

        if (!refresh && cached && File.Exists(cachePath))
        {
            try
            {
                using var stream = File.OpenRead(cachePath);
                var loader = new CachedAssemblyLoader();
                var factory = await cache.LoadExportProviderFactoryAsync(stream, new Resolver(loader), cancellation);
                stream.Position = 0;
                var runtime = await cache.LoadRuntimeCompositionAsync(stream, new Resolver(loader), cancellation);

                var cachedProvider = factory.CreateExportProvider();
                var cachedComposition = new Composition(cachedProvider, loader);

                var cachedCompositionProvider = cachedProvider.GetExportedValue<CompositionProvider>();
                cachedCompositionProvider.Composition = cachedComposition;
                cachedCompositionProvider.RuntimeParts = runtime.Parts;

                return cachedComposition;
            }
            catch (Exception)
            {
                // TODO: report failed to load from cache
            }
        }

        var context = extensions.Load();
        var assemblies = context.GetAssemblies() ?? Array.Empty<Assembly>();

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
            catalog = catalog.AddParts(discovery.CreatePartsAsync(assembly, cancellation).Result);
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
        var composition = new Composition(provider, context);
        var compositionProvider = provider.GetExportedValue<CompositionProvider>();
        compositionProvider.Composition = composition;
        compositionProvider.Parts = config.Parts;

        try
        {
            Directory.CreateDirectory(cacheDir);
            using var stream = File.Create(cachePath);
            await cache.SaveAsync(config, stream, cancellation);
            // We successfully saved a new cache, no need to refresh anymore from this point
            // unless extensions change again.
            refresh = false;
        }
        catch (Exception)
        {
            // TODO: report failure to save cache
        }

        return composition;
    }

    [Shared]
    [Export(typeof(CompositionProvider))]
    partial class CompositionProvider
    {
        IServiceCollection? services;

        [Export]
        public IComposition? Composition { get; set; }

        public ISet<ComposedPart> Parts
        {
            set => services = new ComposedPartServiceCollection(value);
        }

        public IReadOnlyCollection<RuntimeComposition.RuntimePart> RuntimeParts
        {
            set => services = new RuntimePartServiceCollection(value);
        }

        [Export]
        public IServiceCollection? Services => services;

        static Type? GetExportedType(Type? implementation, string typeIdentity)
        {
            if (implementation == null)
                return null;

            if (ContractNameServices.GetTypeIdentity(implementation) == typeIdentity)
                return implementation;

            if (implementation.GetInterfaces().FirstOrDefault(x => ContractNameServices.GetTypeIdentity(x) == typeIdentity) is Type iface)
                return iface;

            var baseType = implementation.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (ContractNameServices.GetTypeIdentity(baseType) == typeIdentity)
                    return baseType;

                baseType = baseType.BaseType;
            }

            return null;
        }

        class RuntimePartServiceCollection : IServiceCollection
        {
            static readonly MethodInfo getResolvedType = typeof(TypeRef).GetProperty("ResolvedType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetMethod!;

            IEnumerable<ServiceDescriptor> descriptors;

            public RuntimePartServiceCollection(IEnumerable<RuntimeComposition.RuntimePart> parts)
                => descriptors = GetRuntimeDescriptors(parts).Cached();

            static IEnumerable<ServiceDescriptor> GetRuntimeDescriptors(IEnumerable<RuntimeComposition.RuntimePart> parts)
            {
                foreach (var part in parts)
                {
                    if (getResolvedType.Invoke(part.TypeRef, null) is not Type partType)
                        continue;

                    foreach (var export in part.Exports)
                    {
                        if (export.Metadata.TryGetValue("ExportTypeIdentity", out var value) &&
                            value is string typeIdentity &&
                            GetExportedType(partType, typeIdentity) is Type exportedType)
                        {
                            var descriptor = new ServiceDescriptor(exportedType, partType, ServiceLifetime.Singleton);
                            yield return descriptor;
                        }
                    }
                }
            }

            public IEnumerator<ServiceDescriptor> GetEnumerator() => descriptors.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public ServiceDescriptor this[int index]
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public void Add(ServiceDescriptor item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(ServiceDescriptor item) => throw new NotImplementedException();
            public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => throw new NotImplementedException();
            public int IndexOf(ServiceDescriptor item) => throw new NotImplementedException();
            public void Insert(int index, ServiceDescriptor item) => throw new NotImplementedException();
            public bool Remove(ServiceDescriptor item) => throw new NotImplementedException();
            public void RemoveAt(int index) => throw new NotImplementedException();
        }

        class ComposedPartServiceCollection : IServiceCollection
        {
            IEnumerable<ServiceDescriptor> descriptors;

            public ComposedPartServiceCollection(IEnumerable<ComposedPart> parts)
                => descriptors = GetComposedDescriptors(parts).Cached();

            static IEnumerable<ServiceDescriptor> GetComposedDescriptors(IEnumerable<ComposedPart> parts)
            {
                foreach (var part in parts.Select(x => x.Definition))
                {
                    var typeName = part.Type.FullName;
                    foreach (var export in part.ExportDefinitions.Where(x => x.Key == null))
                    {
                        if (export.Value.Metadata.TryGetValue("ExportTypeIdentity", out var value) &&
                            value is string typeIdentity &&
                            GetExportedType(part.Type, typeIdentity) is Type exportedType)
                        {
                            var descriptor = new ServiceDescriptor(exportedType, part.Type, ServiceLifetime.Singleton);
                            yield return descriptor;
                        }
                    }

                    foreach (var export in part.ExportDefinitions.Where(x => x.Key != null))
                    {
                        var implementation = export switch
                        {
                            { Key.MemberInfo: MethodInfo method } => method.ReturnType,
                            { Key.MemberInfo: PropertyInfo property } => property.PropertyType,
                            { Key.MemberInfo: FieldInfo field } => field.FieldType,
                            _ => null
                        };

                        if (implementation != null &&
                            export.Value.Metadata.TryGetValue("ExportTypeIdentity", out var value) &&
                            value is string typeIdentity &&
                            GetExportedType(implementation, typeIdentity) is Type exportedType)
                        {
                            var descriptor = new ServiceDescriptor(exportedType, implementation, ServiceLifetime.Singleton);
                            yield return descriptor;
                        }
                    }
                }
            }

            public IEnumerator<ServiceDescriptor> GetEnumerator() => descriptors.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public ServiceDescriptor this[int index]
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public void Add(ServiceDescriptor item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(ServiceDescriptor item) => throw new NotImplementedException();
            public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => throw new NotImplementedException();
            public int IndexOf(ServiceDescriptor item) => throw new NotImplementedException();
            public void Insert(int index, ServiceDescriptor item) => throw new NotImplementedException();
            public bool Remove(ServiceDescriptor item) => throw new NotImplementedException();
            public void RemoveAt(int index) => throw new NotImplementedException();
        }
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

static class CachedEnumerable
{
    public static IEnumerable<T> Cached<T>(this IEnumerable<T> enumerable) => new CachedEnumerableImpl<T>(enumerable);

    class CachedEnumerableImpl<T> : IEnumerable<T>
    {
        readonly IEnumerable<T> enumerable;
        List<T>? cache;

        public CachedEnumerableImpl(IEnumerable<T> enumerable) => this.enumerable = enumerable;

        public IEnumerator<T> GetEnumerator() => (cache ??= new List<T>(enumerable)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
