namespace Terminal.Shell;

public class ShellApp : Toplevel
{
    readonly Label spinner = new("Loading extensions...")
    {
        X = Pos.Center(),
        Y = Pos.Center(),
    };
   
    public ShellApp() : this(new CompositionManager(new ExtensionsManager())) { }

    internal ShellApp(ICompositionManager manager)
    {
        Composition = manager.CreateComposition();
        ColorScheme = Colors.Base;

        Add(
            new MenuBar(new[]
            {
                new MenuBarItem("File", new[]
                {
                    new MenuItem("E_xit", null, () => Application.ExitRunLoopAfterFirstIteration = true)
                }),
                new MenuBarItem("_Theme", GetColorSchemes())
            }),
            new StatusBar(new[] { new StatusItem(Key.Null, "Ready", () => { }) })
        );

        Add(spinner);
    }

    internal IComposition Composition { get; }

    MenuItem[] GetColorSchemes()
    {
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
