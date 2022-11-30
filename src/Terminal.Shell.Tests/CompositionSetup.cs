using System.Reflection;
using Microsoft.VisualStudio.Composition;

namespace Terminal.Shell;

public static class CompositionSetup
{
    public static ExportProvider CreateDefaultProvider() => CreateProvider(
        Assembly.GetExecutingAssembly(),
        typeof(IResourceManager).Assembly,
        typeof(ShellApp).Assembly);

    public static ExportProvider CreateProvider(params Assembly[] assemblies)
    {
        var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, true);
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            // Add Shell
            .AddParts(discovery.CreatePartsAsync(assemblies).Result)
            .WithCompositionService();

        var config = CompositionConfiguration.Create(catalog);
        var provider = config.CreateExportProviderFactory().CreateExportProvider();
        var setter = provider.GetExportedValue<Action<ExportProvider>>();
        setter.Invoke(provider);

        return provider;
    }

    public static ExportProvider CreateProvider(params Type[] types)
    {
        var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, true);
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            // Add Shell
            .AddParts(discovery.CreatePartsAsync(types).Result)
            .WithCompositionService();

        var config = CompositionConfiguration.Create(catalog);

        return config.CreateExportProviderFactory().CreateExportProvider();
    }
}
