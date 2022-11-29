using System.ComponentModel;
using System.Reflection;
using System.Text;
using CliWrap;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Composition;
using Scriban;
using SharpYaml.Serialization;
using Xunit.Abstractions;

namespace Terminal.Shell;

[Menu("File.My")]
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

public class NotifyingData : INotifyPropertyChanged
{
    string login;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NotifyingData(string login)
    {
        this.login = login;
    }

    public string Login
    {
        get => login;
        set
        {
            var old = login;
            login = value;
            if (old != value)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Login)));
        }
    }
}

public record Misc(ITestOutputHelper Output)
{
    [Fact]
    public void NotifyChanges()
    {
        var data = new NotifyingData("kzu");
        var dict = new NotifyingDictionary(data) { { "Login", data.Login } };
        string? changed = default;
        object? value = default;

        dict.PropertyChanged += (sender, args) =>
        {
            changed = args.PropertyName;
            value = dict[args.PropertyName!];
        };

        data.Login = "foo";

        Assert.Equal("Login", changed);
        Assert.Equal("foo", value);
        Assert.Equal("foo", dict["Login"]);

        dict.Dispose();

        data.Login = "bar";

        Assert.NotEqual("bar", value);
        Assert.NotEqual("bar", dict["Login"]);
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

