using System.Text;
using CliWrap;

namespace Terminal.Shell.Extensions;

[Shared]
partial class UI
{
    readonly IMessageBus bus;

    [ImportingConstructor]
    public UI(IMessageBus bus)
    {
        this.bus = bus;

        bus.Observe<OnDidShellInitialize>()
           .Subscribe(_ => Task.Run(InitializeAsync));
    }

    [Export]
    [ExportMetadata("ExportedType", typeof(MenuBarItem))]
    public MenuBarItem Git { get; } = new MenuBarItem("GitHub", Array.Empty<MenuItem>());

    async Task InitializeAsync()
    {
        var output = new StringBuilder();
        var result = await Cli.Wrap("gh")
            .WithArguments("--version")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .ExecuteAsync();

        if (result.ExitCode == 0)
        {
            var version = output.ToString().Split('\r', '\n')[0].Trim();
            bus.Execute(new SetStatus(version));
        }
    }
}