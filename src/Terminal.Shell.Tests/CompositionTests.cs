extern alias First;
extern alias Second;
using Moq;
using Xunit.Abstractions;

namespace Terminal.Shell;

public record CompositionTests(ITestOutputHelper Output)
{
    [Fact]
    public async Task CanInvokeCompatibleCommand()
    {
        var manager = new CompositionManager(Mock.Of<IExtensionsManager>(
            x => x.Load() == Mock.Of<IExtensionsContext>(e =>
                e.GetAssemblies() == new[]
                {
                    typeof(First::Terminal.Shell.Echo).Assembly,
                    typeof(Second::Terminal.Shell.Echo).Assembly,
                })));
        var composition = await manager.CreateCompositionAsync(cached: false);

        var bus = composition.GetExportedValue<IMessageBus>();

        var result = bus.Execute(new Second::Terminal.Shell.Echo("Hello"));

        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task CanInvokeCompatibleCommandCached()
    {
        var manager = new CompositionManager(Mock.Of<IExtensionsManager>(
            x => x.Load() == Mock.Of<IExtensionsContext>(e =>
                e.GetAssemblies() == new[]
                {
                    typeof(First::Terminal.Shell.Echo).Assembly,
                    typeof(Second::Terminal.Shell.Echo).Assembly,
                })));

        var composition = await manager.CreateCompositionAsync(cached: false);
        composition.Dispose();
        composition = await manager.CreateCompositionAsync(cached: true);

        var bus = composition.GetExportedValue<IMessageBus>();

        var result = bus.Execute(new Second::Terminal.Shell.Echo("Hello"));

        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task CanSubscribeCompatibleEvents()
    {
        var manager = new CompositionManager(Mock.Of<IExtensionsManager>(
            x => x.Load() == Mock.Of<IExtensionsContext>(e =>
                e.GetAssemblies() == new[]
                {
                    typeof(First::Terminal.Shell.Echo).Assembly,
                    typeof(Second::Terminal.Shell.Echo).Assembly,
                })));
        var composition = await manager.CreateCompositionAsync(cached: false);

        var bus = composition.GetExportedValue<IMessageBus>();

        string? message = null;

        bus.Observe<First::Terminal.Shell.OnDidSpeak>()
            .Subscribe(d => message = d.Message);

        bus.Notify(new Second::Terminal.Shell.OnDidSpeak("Hello"));

        Assert.Equal("Hello", message);
    }

    [Fact]
    public async Task CanSubscribeCompatibleEventsCached()
    {
        var manager = new CompositionManager(Mock.Of<IExtensionsManager>(
            x => x.Load() == Mock.Of<IExtensionsContext>(e =>
                e.GetAssemblies() == new[]
                {
                    typeof(First::Terminal.Shell.Echo).Assembly,
                    typeof(Second::Terminal.Shell.Echo).Assembly,
                })));
        var composition = await manager.CreateCompositionAsync(cached: false);
        composition.Dispose();
        composition = await manager.CreateCompositionAsync(cached: true);

        var bus = composition.GetExportedValue<IMessageBus>();

        string? message = null;

        bus.Observe<First::Terminal.Shell.OnDidSpeak>()
            .Subscribe(d => message = d.Message);

        bus.Notify(new Second::Terminal.Shell.OnDidSpeak("Hello"));

        Assert.Equal("Hello", message);
    }
}
