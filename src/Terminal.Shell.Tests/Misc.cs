using System.Dynamic;
using System.Reflection;
using System.Text;
using CliWrap;
using Microsoft.VisualStudio.Composition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scriban;
using SharpYaml.Serialization;
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

    [InlineData(@"..\..\..\Terminal.Shell.CodeAnalysis\MenuCommandMethod.sbntxt",
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
    [InlineData(@"..\..\..\Terminal.Shell.CodeAnalysis\MenuCommandMethod.sbntxt",
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
    [InlineData(@"..\..\..\Terminal.Shell.CodeAnalysis\MenuCommandMethod.sbntxt",
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

    static object? ToDynamic(object? model)
    {
        if (model is IDictionary<object, object?> dictionary)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object?>)expando;
            foreach (var kvp in dictionary)
            {
                if (kvp.Key is string key)
                    expandoDict.Add(key, ToDynamic(kvp.Value));
            }
            return expando;
        }
        else if (model is IEnumerable<object> list)
        {
            var expandoList = new List<object>();
            foreach (var item in list)
            {
                if (ToDynamic(item) is object value)
                    expandoList.Add(value);
            }
            return expandoList.ToArray();
        }
        else
        {
            return model;
        }
    }
}

