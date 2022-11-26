namespace Terminal.Shell;

/// <summary>
/// Exposes the type or method as a menu command.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class MenuAttribute : System.Attribute
{
    /// <summary>
    /// Initializes the menu with the given <paramref name="name"/> and optional 
    /// <paramref name="context"/> where it should be available.
    /// </summary>
    /// <param name="name">Required dot-separated menu name/path, such as <c>File.Reload</c>.</param>
    /// <param name="context">Optional context where menu should be made available. 
    /// See also <seealso cref="IContext.Evaluate(string)"/>.</param>
    public MenuAttribute(string name, [ContextExpression]string? context = default) => Name = name;

    /// <summary>
    /// Required dot-separated menu name/path, such as <c>File.Reload</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional context where menu should be made available.
    /// </summary>
    /// <remarks>
    /// Dynamic context that should make the menu available/unavailable.
    /// </remarks>
    /// <seealso cref="IContext.Evaluate(string)"/>
    [ContextExpression]
    public string? Context { get; }
}