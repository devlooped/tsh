using System.Composition;

namespace Terminal.Shell;

[Shared]
[Export(typeof(IThreadingContext))]
class ThreadingContext : IThreadingContext
{
    readonly SynchronizationContext synchronization;
    int mainThreadId;

    public ThreadingContext() : this(
        SynchronizationContext.Current ?? 
        (SynchronizationContext?)AppDomain.CurrentDomain.GetData(nameof(SynchronizationContext)) ??
        throw new ArgumentNullException("SynchronizationContext.Current", "No SynchronizationContext is set."), -1) { }

    internal ThreadingContext(SynchronizationContext synchronization, int mainThreadId = -1)
    {
        this.mainThreadId = mainThreadId;
        this.synchronization = synchronization;

        if (mainThreadId == -1)
            synchronization.Post(_ => this.mainThreadId = Environment.CurrentManagedThreadId, null);
    }

    public bool IsOnMainThread => Environment.CurrentManagedThreadId == mainThreadId;

    public void Invoke(Action action, bool wait = false)
    {
        if (IsOnMainThread)
        {
            action();
            return;
        }

        if (wait)
            synchronization.Send(_ => action(), null);
        else
            synchronization.Post(_ => action(), null);
    }

    public T Invoke<T>(Func<T> function)
    {
        if (IsOnMainThread)
            return function();

        T? result = default;
        synchronization.Send(_ => result = function(), null);

#pragma warning disable CS8603 // Possible null reference return.
        return result;
#pragma warning restore CS8603 // Possible null reference return.
    }
}
