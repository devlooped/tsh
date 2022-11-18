using System.Composition;
using Merq;

namespace Terminal.Shell;

public partial record OnDidSpeak(string Message);

public partial record Echo(string Message) : ICommand<string>;

[Shared]
partial record class EchoHandler : ICommandHandler<Echo, string>
{
    public bool CanExecute(Echo command) => true;
    public string Execute(Echo command) => command.Message;
}