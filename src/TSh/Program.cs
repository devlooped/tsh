AppDomain.CurrentDomain.UnhandledException += (sender, args) => WriteError(args.ExceptionObject?.ToString());
TaskScheduler.UnobservedTaskException += (sender, args) => WriteError(args.Exception?.ToString());

try
{
    Application.Run<ShellApp>(e =>
    {
        WriteError(e.Message);
        return true;
    });
}
catch (Exception e)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;

    if (e.InnerException != null)
        Console.Error.WriteLine(e.InnerException.Message);
    else
        Console.Error.WriteLine(e.Message);

    Console.ForegroundColor = color;

    Console.ReadLine();
    return;
}

static void WriteError(string? message)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ForegroundColor = color;
}
