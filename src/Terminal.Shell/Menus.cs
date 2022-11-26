using System.Composition;

namespace Terminal.Shell;

[Shared]
partial class Menus
{
    readonly IMessageBus bus;

    [ImportingConstructor]
    public Menus(IMessageBus bus) => this.bus = bus;

    [Menu("File._Exit")]
    internal static void Exit() => Application.ExitRunLoopAfterFirstIteration = true;

    [Menu("File._Reload")]
    public Task ReloadAsync(CancellationToken cancellation = default) => bus.ExecuteAsync(new ReloadAsync());
}
