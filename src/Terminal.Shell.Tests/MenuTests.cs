namespace Terminal.Shell;

partial class MenuTests
{
    [Menu("File.Exit2")]
    public static void StaticExit() => Application.ExitRunLoopAfterFirstIteration = true;

    [Menu("File.Exit")]
    public void Exit() => Application.ExitRunLoopAfterFirstIteration = true;

    [Menu("File.Sleep")]
    public Task<bool> SleepAsync() => Task.FromResult(true);

    //[MenuCommand("Tools.Reload")]
    [Menu("File.Reload", "ShellInitialized")]
    public async Task ReloadAsync(IThreadingContext threading, CancellationToken cancellation)
    {
        await threading.SwitchToForeground(cancellation);
        var result = threading.Invoke(() => MessageBox.Query("Hello", "World", "Ok", "Cancel"));
        await threading.SwitchToBackground(cancellation);
        await Task.Delay(1000, cancellation);
    }
}
