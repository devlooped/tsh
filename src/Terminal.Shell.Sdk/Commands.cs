namespace Terminal.Shell.Commands;

/// <summary>
/// Sets the status bar text.
/// </summary>
public partial record SetStatus(string Status) : ICommand;

/// <summary>
/// Reloads the entire shell.
/// </summary>
public partial record ReloadAsync : IAsyncCommand;