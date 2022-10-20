namespace Terminal.Shell;

/// <summary>
/// Manages localizable resources for the shell.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Gets a localized string by its resource name.
    /// </summary>
    string? GetString(string name);
}