namespace Terminal.Shell;

/// <summary>
/// Exposes the current <see cref="ShellApp"/> to the composition, so that components 
/// can affect the main shell too.
/// </summary>
[Shared]
partial class ShellAppProvider : IAsyncCommandHandler<ReloadAsync>
{
    [Export]
    public ShellApp? ShellApp => (ShellApp?)AppDomain.CurrentDomain.GetData(nameof(ShellApp));

    public bool CanExecute(ReloadAsync command) => ShellApp != null;

    public async Task ExecuteAsync(ReloadAsync command, CancellationToken cancellation) => await ShellApp!.ReloadAsync(false, cancellation);
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

        Add(spinner);

        _ = Task.Run(() => ReloadAsync(false));
    }

    internal IComposition? Composition { get; private set; }

    internal async Task ReloadAsync(bool cached, CancellationToken cancellation = default)
    {
        Composition?.Dispose();
        RemoveAll();

        var progress = new ProgressBar
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(50),
            Height = 1,
        };

        Add(new StatusBar(new[] { new StatusItem(Key.Null, "Ready", () => { }) }));

        Add(progress);
        SetNeedsDisplay();

        bool timer(MainLoop caller)
        {
            progress.Pulse();
            return true;
        }

        var token = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(300), timer);

        Composition = await manager.CreateCompositionAsync(cached, cancellation);

        var threading = Composition.GetExportedValue<IThreadingContext>();

        await Task.Delay(2000, cancellation).ConfigureAwait(false);
        await threading.SwitchToForeground();

        Add(Composition.GetExportedValue<MenuManager>().CreateMenu());

        await threading.SwitchToBackground(cancellation: cancellation);

        await Task.Delay(2000).ConfigureAwait(false);

        await threading.SwitchToForeground();
        Application.MainLoop.RemoveTimeout(token);
        Remove(progress);

        await threading.SwitchToBackground(cancellation: cancellation);
        Composition.GetExportedValue<IMessageBus>().Notify<OnDidShellInitialize>();
    }
}
