namespace Terminal.Shell;

public class ThemeAttribute : ComponentAttribute
{
    public ThemeAttribute(string Name) : base(typeof(ColorScheme)) => this.Name = Name;
    
    public string Name { get; }
}
