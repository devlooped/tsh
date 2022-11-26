namespace Terminal.Shell;

[Shared]
partial class SetStatusHandler : ICommandHandler<SetStatus>
{
    readonly Lazy<ShellApp?> shell;
    readonly IThreadingContext threading;

    [ImportingConstructor]
    public SetStatusHandler(Lazy<ShellApp?> shell, IThreadingContext threading)
    {
        this.shell = shell;
        this.threading = threading;
    }

    public bool CanExecute(SetStatus command) => true;

    public void Execute(SetStatus command)
    {
        if (shell.Value?.StatusBar is StatusBar status)
        {
            status.RemoveItem(0);
            status.AddItemAt(0, new StatusItem(Key.Null, command.Status, () => { }));
        }
    }
}
