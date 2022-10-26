namespace Terminal.Shell;

public interface IDynamicMenuCommand : IMenuCommand
{
    bool IsVisible { get; }

    bool IsEnabled { get; }
}
