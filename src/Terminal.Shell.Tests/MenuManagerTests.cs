using Moq;

namespace Terminal.Shell;

public class MenuManagerTests
{
    [Fact]
    public void CanGetMenuManager() => CompositionSetup.CreateDefaultProvider().GetExportedValue<MenuManager>();

    [Fact]
    public void CreateMenu()
    {
        var manager = new MenuManager(new[]
        {
            new Lazy<IMenuCommand, IDictionary<string, object>>(() => new Mock<IMenuCommand>().Object, new Dictionary<string, object>
            {
                ["Name"] = "File._Exit",
            }),
            new Lazy<IMenuCommand, IDictionary<string, object>>(() => new Mock<IMenuCommand>().Object, new Dictionary<string, object>
            {
                ["Name"] = "File._Reload",
            }),
            new Lazy<IMenuCommand, IDictionary<string, object>>(() => new Mock<IMenuCommand>().Object, new Dictionary<string, object>
            {
                ["Name"] = "Edit.Format.Justified",
            }),
            new Lazy<IMenuCommand, IDictionary<string, object>>(() => new Mock<IMenuCommand>().Object, new Dictionary<string, object>
            {
                ["Name"] = "Edit.Format.Centered",
            }),
        }, Mock.Of<IResourceManager>(), Mock.Of<IThreadingContext>());

        var menu = manager.CreateMenu();

        Assert.NotNull(menu);
        Assert.Equal(2, menu.Menus.Length);
        
        Assert.Contains(menu.Menus, item => item.Title == "File");
        Assert.Contains(menu.Menus[0].Children, item => item.Title == "_Exit");
        Assert.Contains(menu.Menus[0].Children, item => item.Title == "_Reload");
        
        Assert.Contains(menu.Menus, item => item.Title == "Edit");
        Assert.Contains(menu.Menus[1].Children, item => item.Title == "Format");
        Assert.IsType<MenuBarItem>(menu.Menus[1].Children[0]);
        Assert.Contains(((MenuBarItem)menu.Menus[1].Children[0]).Children, item => item.Title == "Justified");
        Assert.Contains(((MenuBarItem)menu.Menus[1].Children[0]).Children, item => item.Title == "Centered");
    }
}
