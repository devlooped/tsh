namespace Terminal.Shell;

public class ThemeTests
{
    [Theme("Misc")]
    public ColorScheme Misc { get; } = Colors.Dialog;

    [Fact]
    public void CanExportMiscTheme()
    {
        using var provider = CompositionSetup.CreateDefaultProvider();
        
        var themes = provider.GetExports<ColorScheme, IDictionary<string, object>>();

        Assert.Contains(themes, t => t.Metadata["Name"] as string == "Misc");
        Assert.NotNull(themes.First(x => (string)x.Metadata["Name"] == "Misc").Value);
    }
}