using System.Composition;

namespace Terminal.Shell;

/// <summary>
/// Exports a theme to the shell, which must be of type <see cref="ColorScheme"/>.
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class ThemeAttribute : ExportAttribute
{
    /// <summary>
    /// Exports a theme with the given name.
    /// </summary>
    public ThemeAttribute(string Name) : base(typeof(ColorScheme)) => this.Name = Name;

    /// <summary>
    /// Name of the theme.
    /// </summary>
    public string Name { get; }
}