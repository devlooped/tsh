namespace Terminal.Shell;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class MenuCommandAttribute : System.Attribute
{
    public MenuCommandAttribute(string name) => Name = name;

    public string Name { get; }
}