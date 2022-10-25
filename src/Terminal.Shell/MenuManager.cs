using System.Composition;

namespace Terminal.Shell;

[Shared]
[Export]
class MenuManager
{
    readonly IEnumerable<Lazy<IMenuCommand, IDictionary<string, object>>> menus;
    readonly IResourceManager resources;
    readonly IThreadingContext threading;
    
    record MenuMetadata(string Title, string? Help);

    [ImportingConstructor]
    public MenuManager(
        [ImportMany] IEnumerable<Lazy<IMenuCommand, IDictionary<string, object>>> menus,
        IResourceManager resources, 
        IThreadingContext threading)
        => (this.menus, this.resources, this.threading)
        = (menus, resources, threading);

    public MenuBar CreateMenu()
    {
        var items = new Dictionary<string, object>();

        foreach (var menu in menus)
        {
            if (!menu.Metadata.TryGetValue(nameof(MenuCommandAttribute.Name), out var value))
                continue;

            if (value is string name)
            {
                ProcessMenu(items, name, menu);
            }
            else if (value is string[] names)
            {
                foreach (var single in names)
                    ProcessMenu(items, single, menu);
            }
        }
        
        return new MenuBar(ToMenus(items));
    }

    MenuBarItem[] ToMenus(Dictionary<string, object> items)
    {
        var menus = new List<MenuBarItem>();
        foreach (var pair in items)
        {
            if (pair.Value is Lazy<IMenuCommand, MenuMetadata> lazy)
            {
                menus.Add(new MenuBarItem(lazy.Metadata.Title, lazy.Metadata.Help, () =>
                {
                    // Monitor completion/report errors/etc.
                    _ = Task.Run(() => lazy.Value.ExecuteAsync());
                }));
            }
            else if (pair.Value is Dictionary<string, object> submenu)
            {
                menus.Add(new MenuBarItem(
                    resources.GetString(pair.Key + ":Title") ?? pair.Key,
                    ToMenuItems(submenu, pair.Key).ToArray()));
            }
        }
        return menus.ToArray();
    }

    IEnumerable<MenuItem> ToMenuItems(Dictionary<string, object> items, string path)
    {
        foreach (var pair in items)
        {
            if (pair.Value is Lazy<IMenuCommand, MenuMetadata> lazy)
            {
                yield return new MenuItem(lazy.Metadata.Title, lazy.Metadata.Help, () =>
                {
                    _ = Task.Run(() => lazy.Value.ExecuteAsync());
                });
            }
            else if (pair.Value is Dictionary<string, object> submenu)
            {
                var id = $"{path}.{pair.Key}";
                yield return new MenuBarItem(
                    resources.GetString($"{id}:Title") ?? pair.Key,
                    ToMenuItems(submenu, id).ToArray());
            }
        }
    }

    void ProcessMenu(Dictionary<string, object> items, string name, Lazy<IMenuCommand> command)
    {
        if (string.IsNullOrEmpty(name))
            return;
        
        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var normalized = parts.Select(x => x.Replace("_", "")).ToArray();
        var parent = items;
        var leaf = normalized[^1];

        var id = string.Join('.', normalized);
        // TODO: collect other metadata, such as shortcut?
        var title = resources.GetString($"{id}:Title") ?? parts[^1];
        var help = resources.GetString($"{id}:Help");
        var entry = new Lazy<IMenuCommand, MenuMetadata>(() => command.Value, new MenuMetadata(title, help));

        foreach (var part in normalized)
        {
            if (parent.TryGetValue(part, out var value))
            {
                // Log menu override?
                if (part == leaf)
                {
                    parent[part] = entry;
                    return;

                }

                if (value is Dictionary<string, object> submenu)
                {
                    parent = submenu;
                }
                else
                {
                    var values = new Dictionary<string, object>();
                    parent[part] = values;
                    parent = values;
                }
            }
            else
            {
                if (part == leaf)
                {
                    parent[part] = entry;
                    return;
                }

                var children = new Dictionary<string, object>();
                parent.Add(part, children);
                parent = children;
            }
        }
    }
}
