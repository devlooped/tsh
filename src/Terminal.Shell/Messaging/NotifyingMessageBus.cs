using System.Runtime.ExceptionServices;
using Merq;

namespace Terminal.Shell;

/// <summary>
/// Decorates the default message bus by notifying <see cref="CommandExecuting{TCommand}"/> 
/// and <see cref="CommandExecuted{TCommand}"/> before and after command execution.
/// </summary>
partial class NotifyingMessageBus : IMessageBus
{
    readonly IMessageBus inner;

    public NotifyingMessageBus(IMessageBus inner) => this.inner = inner;

    public void Execute(ICommand command)
    {
        var commandType = command?.GetType() ?? throw new ArgumentNullException(nameof(command));
        Notify(Activator.CreateInstance(typeof(CommandExecuting<>).MakeGenericType(commandType), command));

        try
        {
            inner.Execute(command);
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, null));
        }
        catch (Exception e)
        {
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, e));
            // Rethrow original exception to preserve stacktrace.
            ExceptionDispatchInfo.Capture(e).Throw();
            throw;
        }
    }

    public TResult Execute<TResult>(ICommand<TResult> command)
    {
        var commandType = command?.GetType() ?? throw new ArgumentNullException(nameof(command));
        Notify(Activator.CreateInstance(typeof(CommandExecuting<>).MakeGenericType(commandType), command));

        try
        {
            var result = inner.Execute(command);
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, null));
            return result;
        }
        catch (Exception e)
        {
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, e));
            // Rethrow original exception to preserve stacktrace.
            ExceptionDispatchInfo.Capture(e).Throw();
            throw;
        }
    }

    public async Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation = default)
    {
        var commandType = command?.GetType() ?? throw new ArgumentNullException(nameof(command));
        Notify(Activator.CreateInstance(typeof(CommandExecuting<>).MakeGenericType(commandType), command));

        try
        {
            await inner.ExecuteAsync(command, cancellation);
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, null));
        }
        catch (Exception e)
        {
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, e));
            // Rethrow original exception to preserve stacktrace.
            ExceptionDispatchInfo.Capture(e).Throw();
            throw;
        }
    }

    public async Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation = default)
    {
        var commandType = command?.GetType() ?? throw new ArgumentNullException(nameof(command));
        Notify(Activator.CreateInstance(typeof(CommandExecuting<>).MakeGenericType(commandType), command));

        try
        {
            var result = await inner.ExecuteAsync(command, cancellation);
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, null));
            return result;
        }
        catch (Exception e)
        {
            Notify(Activator.CreateInstance(typeof(CommandExecuted<>).MakeGenericType(commandType), command, e));
            // Rethrow original exception to preserve stacktrace.
            ExceptionDispatchInfo.Capture(e).Throw();
            throw;
        }
    }

    public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable => inner.CanExecute(command);
    public bool CanHandle<TCommand>() where TCommand : IExecutable => inner.CanHandle<TCommand>();
    public bool CanHandle(IExecutable command) => inner.CanHandle(command);
    public void Notify<TEvent>(TEvent e) => inner.Notify(e);
    public IObservable<TEvent> Observe<TEvent>() => inner.Observe<TEvent>();
}
