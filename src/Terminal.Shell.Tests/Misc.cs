using System.Runtime.CompilerServices;
using System.Text;
using CliWrap;
using Microsoft;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Threading;
using Terminal.Gui;
using Xunit.Abstractions;

namespace Terminal.Shell;

public record Misc(ITestOutputHelper Output)
{
    [Fact]
    public async Task TestAsync()
    {
        var output = new StringBuilder();
        var result = await Cli.Wrap("ghs")
            .WithArguments("--version")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();



        Output.WriteLine(output.ToString());
    }

}

