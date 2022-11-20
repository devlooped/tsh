using System.ComponentModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CliWrap;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.VisualStudio.Composition;
using Scriban;
using SharpYaml.Serialization;
using Xunit.Abstractions;

namespace Terminal.Shell;

[MenuCommand("File.My")]
public partial class MyCommand : IMenuCommand
{
    public Task ExecuteAsync(CancellationToken cancellation = default) => throw new NotImplementedException();
}

public interface IFoo { }
public interface IBar { }

[Shared]
public partial class Foo : IFoo, IBar
{

}

public interface IExpressionContext
{
    public HashSet<string>? Data { get; set; }
}

public static partial class ExpressionContext
{
    public static partial class Generated
    {
        [Export(typeof(IExpressionContext))]
        [ExportMetadata("Expression", "Initialized && GitHub && !GitHubUser")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public partial class _e98d5c36e753a133520419cd954dde3e : IExpressionContext
        {
            public bool Initialized => Data?.Contains(nameof(Initialized)) == true;
            public bool GitHub => Data?.Contains(nameof(GitHub)) == true;
            public bool GitHubUser => Data?.Contains(nameof(GitHubUser)) == true;

            public HashSet<string>? Data { get; set; }
        }
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContextExpression
{
    public static object CreateContext(string expression, HashSet<string> context) => expression switch  
    {
        "Initialized && GitHub && !GitHubUser" => new GeneratedContexts._e98d5c36e753a133520419cd954dde3e(context),
        _ => throw new NotImplementedException(),
    };

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GeneratedContexts
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class _e98d5c36e753a133520419cd954dde3e
        {
            readonly HashSet<string> context;
            public _e98d5c36e753a133520419cd954dde3e(HashSet<string> context) => this.context = context;

            public bool Initialized => context.Contains(nameof(Initialized));
            public bool GitHub => context.Contains(nameof(GitHub));
            public bool GitHubUser => context.Contains(nameof(GitHubUser));
        }
    }
}

//public class FooCommandHandler : ICommandHandler<Foo>
//{
//    public bool CanExecute(Foo command) => Validator.TryValidateObject(command, new ValidationContext(command), null, true);
    
//    public void Execute(Foo command) => throw new NotImplementedException();
//}

public record Misc(ITestOutputHelper Output)
{
    [Fact]
    public async Task GetContextFromExport()
    {
        var expression = "Initialized && GitHub && !GitHubUser";
        var provider = CompositionSetup.CreateDefaultProvider();
        var contexts = provider.GetExports<IExpressionContext, IDictionary<string, object?>>();
        var context = contexts.First(x => 
            x.Metadata.TryGetValue("Expression", out var value) && 
            value is string metadata && 
            metadata == "Initialized && GitHub && !GitHubUser").Value;

        Assert.NotNull(context);

        var script = CSharpScript.Create<bool>(expression, globalsType: context.GetType());        
        var runner = script.CreateDelegate();

        var data = new HashSet<string>(new[] { "Initialized", "GitHub", "GitHubUser" });
        context.Data = data;

        Assert.False(await runner(context));
        data.Remove("GitHubUser");
        Assert.True(await runner(context));
    }

    [Fact]
    public async Task EvaluateExpression()
    {
        var expression = "Initialized && GitHub && !GitHubUser";
        
        // Gets a hash for the expression, to use as the map between expression > generated code.
        var hash = string.Concat(MD5.HashData(Encoding.UTF8.GetBytes(expression)).Select(b => b.ToString("x2")));
        Output.WriteLine(hash);

        var context = new HashSet<string>(new[] { "Initialized", "GitHub", "GitHubUser" });
        var evaluation = Terminal.Shell.ContextExpression.CreateContext(expression, context);
        
        var typed = CSharpScript.Create<bool>(expression, globalsType: evaluation.GetType());
        var name = typed.GetCompilation().AssemblyName;
        var runner = typed.CreateDelegate();

        Assert.False(await runner(evaluation));
        context.Remove("GitHubUser");
        Assert.True(await runner(evaluation));
    }

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

    [InlineData("../../../Terminal.Shell.CodeAnalysis/MenuCommandMethod.sbntxt",
        """
        Namespace: Terminal.Shell
        Target: _instance
        Parent: TestMenu
        Method: ReloadAsync
        Menus: [ "File.Reload", "Tools.Reload" ]
        Dependencies: 
        - Name: _instance
          Type: TestMenu
        - Name: threading
          Type: IThreadingContext
        Parameters: 
        - threading
        - cancellation
        """)]
    [InlineData("../../../Terminal.Shell.CodeAnalysis/MenuCommandMethod.sbntxt",
        """
        Namespace: MyExtension
        Target: _instance
        Parent: TestMenu
        Method: ReloadAsync
        Menus: [ "File.Reload" ]
        Dependencies: []
        Parameters: 
        - cancellation
        """)]
    [InlineData("../../../Terminal.Shell.CodeAnalysis/MenuCommandMethod.sbntxt",
        """
        Namespace: Terminal.Shell
        Target: _instance
        Parent: TestMenu
        Method: Exit
        Menus: [ "File.Exit" ]
        Dependencies: []
        Parameters: []
        """)]
    [Theory]
    public void RenderTemplate(string templateFile, string modelYaml)
    {
        var serializer = new Serializer(new SerializerSettings
        {
            SerializeDictionaryItemsAsMembers = true,
        });

        var model = serializer.Deserialize(modelYaml);
        Assert.NotNull(model);

        Assert.True(File.Exists(templateFile));
        var template = Template.Parse(File.ReadAllText(templateFile), templateFile);

        var output = template.Render(model, member => member.Name);

        Output.WriteLine(output);
    }
}

