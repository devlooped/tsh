//using System.ComponentModel.Composition;
using System.Composition;

namespace Terminal.Shell;

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