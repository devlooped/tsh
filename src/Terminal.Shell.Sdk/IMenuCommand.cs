namespace Terminal.Shell;

/// <summary>
/// Represents a command that can be invoked from a menu.
/// </summary>
/// <remarks>
/// Menu commands can also be any regular method in a class, 
/// as long as it has the <see cref="MenuCommandAttribute"/>.
/// </remarks>
public interface IMenuCommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellation = default);
}