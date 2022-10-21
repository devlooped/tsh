using System.Diagnostics;
using System.Reflection;
using System.Text;
using CliWrap;
using Microsoft.VisualStudio.Composition;
using Xunit.Abstractions;

namespace Terminal.Shell;

public record Misc(ITestOutputHelper Output)
{
    [Fact]
    public async Task TestAsync()
    {
        var output = new StringBuilder();
        var result = await Cli.Wrap("dotnet")
            .WithArguments("--version")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
        
        Output.WriteLine(output.ToString());
    }

    [Fact]
    public void RunDgml()
    {
        var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, true);
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            .AddParts(discovery.CreatePartsAsync(typeof(ShellApp).Assembly).Result)
            .AddParts(discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()).Result)
            .WithCompositionService();

        var config = CompositionConfiguration.Create(catalog);
        var dgml = config.CreateDgml();
        dgml.Save(@"C:\Delete\composition.dgml");
    }
}

