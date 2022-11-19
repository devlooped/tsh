using System.Collections.Concurrent;
using System.Composition;
using System.Reflection;
using Merq;

namespace Terminal.Shell;

[Shared]
partial class MessageBusProvider
{
    readonly IMessageBus bus;

    [ImportingConstructor]
    public MessageBusProvider(Lazy<IComposition> composition)
        => bus = new NotifyingMessageBus(
            new AutoMapperMessageBus(
                new CompositionServiceProvider(composition)));

    [Export]
    public IMessageBus MessageBus => bus;

    /// <summary>
    /// An <see cref="IServiceProvider"/> that uses 
    /// <see cref="IComponentModel.GetService{T}"/> and <see cref="IComponentModel.GetExtensions{T}"/> 
    /// to retrieve <see cref="IMessageBus"/> components.
    /// </summary>
    class CompositionServiceProvider : IServiceProvider
    {
        readonly Lazy<IComposition> composition;

        static readonly MethodInfo getValues = typeof(CompositionServiceProvider).GetMethod(nameof(GetValues), BindingFlags.NonPublic | BindingFlags.Static)!;
        static readonly MethodInfo getExports = typeof(IComposition).GetMethods().First(m => m.Name == nameof(IComposition.GetExports) && m.IsGenericMethod && m.GetGenericArguments().Length == 1)!;
        static readonly ConcurrentDictionary<Type, Func<IComposition, object?>> getServiceCache = new();

        public CompositionServiceProvider(Lazy<IComposition> composition) => this.composition = composition;

        public object? GetService(Type serviceType)
        {
            var getService = getServiceCache.GetOrAdd(serviceType, type =>
            {
                var many = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                var method = getExports.MakeGenericMethod(
                    many ? type.GetGenericArguments()[0] : type);

                var targetType = many ? type.GetGenericArguments()[0] : type;
                var filter = getValues.MakeGenericMethod(targetType);

                var assemblyName = !targetType.IsGenericType ?
                    targetType.Assembly.GetName().Name :
                    // In the case of ICommandHandler<T> or IExecutable<T>, we want to know the type
                    // of the command itself.
                    targetType.GetGenericArguments()[0].Assembly.GetName().Name;

                if (many)
                    return components => filter.Invoke(null, new[] { method.Invoke(components, new[] { (object?)null }), assemblyName });

                // NOTE: the behavior of IServiceProvider.GetService is to *not* fail when requesting 
                // a service, and instead return null. This is the opposite of what the export provider 
                // does, which throws instead. But the equivalent behavior can be had by requesting many 
                // and picking first if any. The ServiceProviderExtensions in Merq will take care of 
                // throwing when using GetRequiredService instead of GetService.
                return components => ((IEnumerable<object?>?)
                        filter.Invoke(null, new[] {
                            method.Invoke(components, new[] { (object?)null }),
                            assemblyName }))?
                    .FirstOrDefault();
            });

            return getService(composition.Value);
        }

        static IEnumerable<T> GetValues<T>(IEnumerable<Lazy<T, IDictionary<string, object>>> exports, string? assemblyName)
            => exports.Where(
                x => !x.Metadata.TryGetValue("AssemblyName", out var metadata) ||
                     (metadata is string name && name == assemblyName))
               .Select(x => x.Value);
    }

}
