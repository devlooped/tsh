namespace Terminal.Shell;

public partial record OnDidSpeak(string Message);

public partial record Echo(string Message) : ICommand<string>;

public partial record SayHello(string Name) : ICommand<string>;

// NOTE: consumes Echo command from another extension, but has its own version of the Echo command contract.
[Shared]
partial class SayHelloHandler : ICommandHandler<SayHello, string>
{
    readonly IMessageBus messageBus;

    [ImportingConstructor]
    public SayHelloHandler(IMessageBus messageBus) => this.messageBus = messageBus;

    public bool CanExecute(SayHello command) => messageBus.CanHandle<Echo>();
    public string Execute(SayHello command)
    {
        var result = messageBus.Execute(new Echo("Hello" + command.Name))!;
        messageBus.Notify(new OnDidSpeak(result));
        return result;
    }
}
