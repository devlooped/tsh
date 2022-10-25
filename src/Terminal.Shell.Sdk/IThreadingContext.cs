using System.Runtime.CompilerServices;

namespace Terminal.Shell;

/// <summary>
/// Provides access to the threading context of the shell.
/// </summary>
public interface IThreadingContext
{
    /// <summary>
    /// Gets whether the current thread is the UI thread.
    /// </summary>
    bool IsOnMainThread { get; }

    /// <summary>
    /// Invokes the given action on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="wait">Whether to block execution until the action is run to completion.</param>
    void Invoke(Action action, bool wait = false);

    /// <summary>
    /// Invokes the given function on the UI thread.
    /// </summary>
    /// <param name="function">The function to execute.</param>
    T Invoke<T>(Func<T> function);
}

public static class ThreadingContextExtensions
{
    /// <summary>
    /// Gets an awaiter that allows scheduling continuations on the main thread via <c>await context.SwitchToForeground()</c>.
    /// </summary>
    public static MainThreadAwaitable SwitchToForeground(this IThreadingContext context, CancellationToken cancellation = default)
        => new(context, cancellation);

    /// <summary>
    /// Gets an awaiter that allows scheduling continuations on a task pool thread via <c>await context.SwitchToBackground()</c>.
    /// </summary>
    public static TaskSchedulerAwaitable SwitchToBackground(this IThreadingContext context, CancellationToken cancellation = default)
        => new(TaskScheduler.Default, cancellation);

    /// <summary>
    /// Gets an awaiter that allows scheduling continuations on the given <see cref="TaskScheduler"/> via <c>await scheduler.SwitchTo()</c>.
    /// </summary>
    public static TaskSchedulerAwaitable SwitchTo(this TaskScheduler scheduler, CancellationToken cancellation = default)
        => new(scheduler, cancellation);

    /// <summary>
    /// An awaitable struct executes continuations on the main thread, returned from 
    /// <see cref="SwitchToForeground(IThreadingContext, CancellationToken)"/>.
    /// </summary>
    public readonly struct MainThreadAwaitable
    {
        readonly IThreadingContext context;
        readonly CancellationToken cancellation;

        internal MainThreadAwaitable(IThreadingContext context, CancellationToken cancellation)
        {
            this.context = context;
            this.cancellation = cancellation;
        }

        public MainThreadAwaiter GetAwaiter() => new(context, cancellation);
    }

    /// <summary>
    /// An awaiter returned from <see cref="MainThreadAwaitable.GetAwaiter"/>.
    /// </summary>
    public readonly struct MainThreadAwaiter : INotifyCompletion
    {
        readonly IThreadingContext context;
        readonly CancellationToken cancellation;

        internal MainThreadAwaiter(IThreadingContext context, CancellationToken cancellation)
        {
            this.context = context;
            this.cancellation = cancellation;
        }

        /// <summary>
        /// Gets a value indicating whether the caller is already on the Main thread.
        /// </summary>
        public bool IsCompleted => context.IsOnMainThread;

        /// <summary>
        /// Called on the Main thread to prepare it to execute the continuation.
        /// </summary>
        public void GetResult()
        {
            cancellation.ThrowIfCancellationRequested();
            if (!IsCompleted)
                throw new InvalidOperationException("Wrong thread");
        }

        /// <summary>
        /// Schedules a continuation on the main thread.
        /// </summary>
        public void OnCompleted(Action continuation) => context.Invoke(continuation);
    }

    /// <summary>
    /// An awaitable that executes continuations on a task scheduler.
    /// </summary>
    public readonly struct TaskSchedulerAwaitable
    {
        readonly TaskScheduler scheduler;
        readonly CancellationToken cancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskSchedulerAwaitable"/> struct.
        /// </summary>
        internal TaskSchedulerAwaitable(TaskScheduler scheduler, CancellationToken cancellation) 
            => (this.scheduler, this.cancellation) 
            = (scheduler, cancellation);

        /// <summary>
        /// Gets an awaitable that schedules continuations on the specified scheduler.
        /// </summary>
        public TaskSchedulerAwaiter GetAwaiter() => new(scheduler, cancellation);
    }

    /// <summary>
    /// An awaiter returned from <see cref="TaskSchedulerAwaitable.GetAwaiter"/>.
    /// </summary>
    public readonly struct TaskSchedulerAwaiter : INotifyCompletion
    {
        readonly TaskScheduler scheduler;
        readonly CancellationToken cancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskSchedulerAwaiter"/> struct.
        /// </summary>
        internal TaskSchedulerAwaiter(TaskScheduler scheduler, CancellationToken cancellation)
            => (this.scheduler, this.cancellation)
            = (scheduler, cancellation);

        /// <summary>
        /// Gets a value indicating whether no yield is necessary.
        /// </summary>
        /// <value><c>true</c> if the caller is already running on that TaskScheduler.</value>
        public bool IsCompleted => 
            (scheduler == TaskScheduler.Default && Thread.CurrentThread.IsThreadPoolThread) || 
            (scheduler == TaskScheduler.Current && TaskScheduler.Current != TaskScheduler.Default);

        /// <summary>
        /// Schedules a continuation to execute using the specified task scheduler.
        /// </summary>
        public void OnCompleted(Action continuation)
        {
            if (scheduler == TaskScheduler.Default)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    (Action action, CancellationToken cancellation) = (ValueTuple<Action, CancellationToken>)state!;
                    cancellation.ThrowIfCancellationRequested();
                    action();
                }, (continuation, cancellation));
            }
            else
            {
                Task.Factory.StartNew(continuation, cancellation, TaskCreationOptions.None, scheduler);
            }
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void GetResult() { }
    }
}
