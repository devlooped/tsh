using Merq;

namespace Terminal.Shell;

/// <summary>
/// Event raised when a command is about to be executed.
/// </summary>
/// <param name="Command">The command that will be executed.</param>
public partial record CommandExecuting<TCommand>(TCommand Command) where TCommand : IExecutable;

/// <summary>
/// Notifies that the given command has finished executing.
/// </summary>
/// <param name="Command">The command that has finished executing.</param>
/// <param name="Exception">Optional exception of if command execution was not successful.</param>
public partial record CommandExecuted<TCommand>(TCommand Command, Exception? Exception = default) where TCommand : IExecutable;

/// <summary>
/// Notifies that the given command has finished executing.
/// </summary>
/// <param name="Command">The command that has finished executing.</param>
/// <param name="Exception">Optional exception if command execution was not successful.</param>
/// <param name="Result">Optional result if command execution was successful.</param>
public partial record CommandExecuted<TCommand, TResult>(TCommand Command, Exception? Exception = default, TResult? Result = default) where TCommand : IExecutable<TResult>;