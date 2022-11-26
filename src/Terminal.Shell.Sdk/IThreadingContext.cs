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

/// <summary>
/// Usability overloads for <see cref="IThreadingContext"/>.
/// </summary>
public static class ThreadingContextExtensions
{
    /// <summary>
    /// Gets an awaiter that allows scheduling continuations on the main thread via <c>await context.SwitchToForeground()</c>.
    /// </summary>
    public static ForegroundAwaitable SwitchToForeground(this IThreadingContext context, CancellationToken cancellation = default)
        => new(context, cancellation);

    /// <summary>
    /// Gets an awaiter that allows scheduling continuations on a task pool thread via <c>await context.SwitchToBackground()</c>.
    /// </summary>
    public static BackgroundAwaitable SwitchToBackground(this IThreadingContext context, CancellationToken cancellation = default)
        => new(TaskScheduler.Default, cancellation);

    /// <summary>
    /// Gets an awaiter that allows scheduling continuations on the given <see cref="TaskScheduler"/> via <c>await scheduler.SwitchTo()</c>.
    /// </summary>
    public static BackgroundAwaitable SwitchTo(this TaskScheduler scheduler, CancellationToken cancellation = default)
        => new(scheduler, cancellation);

    /// <summary>
    /// An awaitable struct executes continuations on the main thread, returned from 
    /// <see cref="SwitchToForeground(IThreadingContext, CancellationToken)"/>.
    /// </summary>
    public readonly struct ForegroundAwaitable
    {
        readonly IThreadingContext context;
        readonly CancellationToken cancellation;

        internal ForegroundAwaitable(IThreadingContext context, CancellationToken cancellation)
        {
            this.context = context;
            this.cancellation = cancellation;
        }

        /// <summary>
        /// Gets the awaiter that schedules continuations on the main/foreground thread.
        /// </summary>
        public ForegroundAwaiter GetAwaiter() => new(context, cancellation);
    }

    /// <summary>
    /// An awaiter returned from <see cref="ForegroundAwaitable.GetAwaiter"/>.
    /// </summary>
    public readonly struct ForegroundAwaiter : INotifyCompletion
    {
        readonly IThreadingContext context;
        readonly CancellationToken cancellation;

        internal ForegroundAwaiter(IThreadingContext context, CancellationToken cancellation)
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
    public readonly struct BackgroundAwaitable
    {
        readonly TaskScheduler scheduler;
        readonly CancellationToken cancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundAwaitable"/> struct.
        /// </summary>
        internal BackgroundAwaitable(TaskScheduler scheduler, CancellationToken cancellation)
            => (this.scheduler, this.cancellation)
            = (scheduler, cancellation);

        /// <summary>
        /// Gets an awaitable that schedules continuations on the specified scheduler.
        /// </summary>
        public BackgroundAwaiter GetAwaiter() => new(scheduler, cancellation);
    }

    /// <summary>
    /// An awaiter returned from <see cref="BackgroundAwaitable.GetAwaiter"/>.
    /// </summary>
    public readonly struct BackgroundAwaiter : INotifyCompletion
    {
        readonly TaskScheduler scheduler;
        readonly CancellationToken cancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundAwaiter"/> struct.
        /// </summary>
        internal BackgroundAwaiter(TaskScheduler scheduler, CancellationToken cancellation)
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
