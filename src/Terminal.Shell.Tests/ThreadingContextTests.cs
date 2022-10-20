using System.Collections.Concurrent;

namespace Terminal.Shell;

public class ThreadingContextTests
{
    [Fact]
    public async Task CanSwitchThreadsWithAwait()
    {
        var cts = new CancellationTokenSource();
        var sync = new TestSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(sync);

        var main = new Thread(() => sync.Run(cts.Token));
        main.Start();

        var mainThreadId = main.ManagedThreadId;
        var context = new ThreadingContext(sync, mainThreadId);

        Assert.NotEqual(mainThreadId, Environment.CurrentManagedThreadId);
        await context.SwitchToForeground();
        Assert.Equal(mainThreadId, Environment.CurrentManagedThreadId);
        await context.SwitchToBackground();
        Assert.NotEqual(mainThreadId, Environment.CurrentManagedThreadId);

        // Works too with Task.Run on a thread pool thread
        await Task.Run(async () =>
        {
            Assert.NotEqual(mainThreadId, Environment.CurrentManagedThreadId);
            await context.SwitchToForeground();
            Assert.Equal(mainThreadId, Environment.CurrentManagedThreadId);
            await context.SwitchToBackground();
            Assert.NotEqual(mainThreadId, Environment.CurrentManagedThreadId);
        });

        cts.Cancel();
    }

    class TestSynchronizationContext : SynchronizationContext
    {
        readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new();

        public TestSynchronizationContext() { }

        TestSynchronizationContext(BlockingCollection<(SendOrPostCallback Callback, object? State)> queue)
            => this.queue = queue;

        public void Run(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                if (queue.TryTake(out var item))
                    item.Callback(item.State);
            }
        }

        public override SynchronizationContext CreateCopy() => new TestSynchronizationContext(queue);

        public override void Post(SendOrPostCallback d, object? state) => queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            var ev = new ManualResetEventSlim();

            queue.Add((new SendOrPostCallback(state =>
            {
                d(state);
                ev.Set();
            }), state));

            ev.Wait();
        }
    }
}
