namespace Terminal.Shell;

public class ShellApp : Toplevel
{
    readonly Label spinner = new("Loading extensions...")
    {
        X = Pos.Center(),
        Y = Pos.Center(),
    };

    readonly ICompositionManager manager;

    public ShellApp() : this(new CompositionManager(new ExtensionsManager())) { }

    internal ShellApp(ICompositionManager manager)
    {
        this.manager = manager;
        ColorScheme = Colors.Base;

        AppDomain.CurrentDomain.SetData(nameof(SynchronizationContext), SynchronizationContext.Current);

        Reload();
    }

    internal IComposition? Composition { get; private set; }

    void Reload()
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
            
            Add(new MenuBar(new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Reload", null, () => threading.Invoke(Reload)),
                    new MenuItem("E_xit", null, () => Application.ExitRunLoopAfterFirstIteration = true),
                }),
                new MenuBarItem("_Theme", GetColorSchemes())
            }));

            Add(new StatusBar(new[] { new StatusItem(Key.Null, "Ready", () => { }) }));
            Remove(spinner);
            SetNeedsDisplay();

            await Task.Delay(1000).ConfigureAwait(false);
        });
    }

    MenuItem[] GetColorSchemes()
    {
        if (Composition == null)
            return Array.Empty<MenuItem>();

        var items = new List<MenuItem>();
        var themes = Composition.GetExports<ColorScheme>();

        foreach (var theme in themes
            .Where(x => x.Metadata.TryGetValue("Name", out var value) && value is string)
            .OrderBy(x => x.Metadata["Name"]))
        {
            var title = (string)theme.Metadata["Name"];
            var scheme = theme.Value;
            var item = new MenuItem
            {
                Title = $"_{title}",
                Shortcut = Key.AltMask | (Key)title[..1][0]
            };
            item.CheckType |= MenuItemCheckStyle.Radio;
            item.Checked = theme.Value == ColorScheme;
            item.Action += () =>
            {
                ColorScheme = scheme;
                foreach (var item in items)
                {
                    item.Checked = item.Title.Equals($"_{title}") && scheme == ColorScheme;
                }

                SetNeedsDisplay();
            };

            items.Add(item);
        }

        return items.ToArray();
    }
}
