namespace Terminal.Shell;

partial class TestMenu
{
    [MenuCommand("File.Exit2")]
    public static void StaticExit() => Application.ExitRunLoopAfterFirstIteration = true;

    [MenuCommand("File.Exit")]
    public void Exit() => Application.ExitRunLoopAfterFirstIteration = true;

    [MenuCommand("File.Sleep")]
    public Task<bool> SleepAsync() => Task.FromResult(true);

    //[MenuCommand("Tools.Reload")]
    [MenuCommand("File.Reload")]
    public async Task ReloadAsync(IThreadingContext threading, CancellationToken cancellation)
    {
        await threading.SwitchToForeground(cancellation);
        var result = threading.Invoke(() => MessageBox.Query("Hello", "World", "Ok", "Cancel"));
        await threading.SwitchToBackground(cancellation);
        await Task.Delay(1000, cancellation);
    }
}
