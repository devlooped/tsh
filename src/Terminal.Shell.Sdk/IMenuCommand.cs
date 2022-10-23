namespace Terminal.Shell;

public interface IMenuCommand
{
    Task ExecuteAsync(CancellationToken cancellation = default);
}