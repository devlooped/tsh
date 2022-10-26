//using System.ComponentModel.Composition;
using System.Composition;

namespace Terminal.Shell;

[Export(typeof(MenuBarItem))]
partial class ThemeMenu : MenuBarItem
{
    [ImportingConstructor]
    public ThemeMenu(
        [Import(AllowDefault = true)] ShellApp? shell,
        IResourceManager resources,
        [ImportMany] IEnumerable<Lazy<ColorScheme, ThemeMetadata>> themes)
        : base(resources.GetString("Theme:Title") ?? "_Theme", GetMenuItems(shell, themes))
    { }

    static MenuItem[] GetMenuItems(ShellApp? shell, IEnumerable<Lazy<ColorScheme, ThemeMetadata>> themes)
    {
        var items = new List<MenuItem>();
        foreach (var theme in themes
            .Where(x => x.Metadata.Name != null)
            .OrderBy(x => x.Metadata.Name))
        {
            var title = theme.Metadata.Name!;
            var scheme = theme.Value;
            var item = new MenuItem
            {
                Title = $"_{title}",
                //Shortcut = Key.AltMask | (Key)title[..1][0],
            };
            item.CheckType |= MenuItemCheckStyle.Radio;
            item.Checked = theme.Value == shell?.ColorScheme;

            if (shell != null)
            {
                item.Action += () =>
                {
                    shell.ColorScheme = scheme;
                    foreach (var item in items)
                    {
                        item.Checked = item.Title.Equals($"_{title}") && scheme == shell.ColorScheme;
                    }

                    shell.SetNeedsDisplay();
                };
            }

            items.Add(item);
        }

        return items.ToArray();
    }
}

// Cannot use records (for now?): https://github.com/microsoft/vs-mef/issues/343
//public record ThemeMetadata(string Name);
public class ThemeMetadata
{
    public string? Name { get; set; }
}

[Shared]
partial class Themes
{
    [Theme(nameof(Default))]
    public ColorScheme Default => Colors.TopLevel;

    [Theme(nameof(Base))]
    public ColorScheme Base => Colors.Base;
}