using System.Composition;

namespace Terminal.Shell;

[MetadataAttribute]
public class ThemeAttribute : ExportAttribute
{
    public ThemeAttribute(string Name) : base(typeof(ColorScheme)) => this.Name = Name;

    public string Name { get; }
}