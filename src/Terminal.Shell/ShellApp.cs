using System.Composition;

namespace Terminal.Shell;

/// <summary>
/// Exposes the current <see cref="ShellApp"/> to the composition, so that components 
/// can affect the main shell too.
/// </summary>
[Shared]
partial class ShellAppProvider
{
    [Export]
    public ShellApp? ShellApp => (ShellApp?)AppDomain.CurrentDomain.GetData(nameof(ShellApp));

    [MenuCommand("File._Reload")]
    public void Reload() => ShellApp?.Reload();
}

/// <summary>
/// Main shell app runner.
/// </summary>
public class ShellApp : Toplevel
{
    readonly Label spinner = new("Loading extensions...")
    {
        X = Pos.Center(),
        Y = Pos.Center(),
    };

    readonly ICompositionManager manager;

    /// <summary>
    /// Creates the shell app.
    /// </summary>
    public ShellApp() : this(new CompositionManager(new ExtensionsManager())) { }

    internal ShellApp(ICompositionManager manager)
    {
        this.manager = manager;
        ColorScheme = Colors.Base;

        AppDomain.CurrentDomain.SetData(nameof(SynchronizationContext), SynchronizationContext.Current);
        AppDomain.CurrentDomain.SetData(nameof(ShellApp), this);

        Reload();
    }

    internal IComposition? Composition { get; private set; }

    internal void Reload()
    {
        Composition?.Dispose();
        RemoveAll();
        Add(spinner);
        SetNeedsDisplay();

        var sync = SynchronizationContext.Current;

        _ = Task.Run(async () =>
        {
            Composition = await manager.CreateCompositionAsync();

            var threading = Composition.GetExportedValue<IThreadingContext>();

            await Task.Delay(2000).ConfigureAwait(false);
            await threading.SwitchToForeground();

            Add(Composition.GetExportedValue<MenuManager>().CreateMenu());

            Add(new StatusBar(new[] { new StatusItem(Key.Null, "Ready", () => { }) }));
            Remove(spinner);
            SetNeedsDisplay();

            await Task.Delay(1000).ConfigureAwait(false);
        });
    }
}
