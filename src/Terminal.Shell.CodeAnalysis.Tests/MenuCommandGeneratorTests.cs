using Devlooped.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Terminal.Shell.CodeAnalysis.CodeAnalysisHelpers;

namespace Terminal.Shell.CodeAnalysis;

public record MenuCommandGeneratorTests(ITestOutputHelper Output)
{
    [Theory]
    [IncrementalGenerator<MenuCommandMethodGenerator>]
    [PackageReference("System.Composition.AttributedModel", "7.0.0")]
    [AssemblyReference("Terminal.Shell")]
    [AssemblyReference("Terminal.Shell.Sdk")]
    [CompilationData(
        """
        namespace Terminal.Shell;

        partial class Test
        {
            [Menu("Test")]
            public void Exit(IContext context) { }
        }
        """)]
    public void TestCompilation(Compilation compilation)
    {
        // Emit the file to disk so we can analyze the resulting MEF composition
        var assembly = compilation.Load();

        // Add Sdk and Shell which are always available to tsh app/extensions so code can use dependencies too.
        var exports = CompositionSetup.CreateProvider(assembly, typeof(IResourceManager).Assembly, typeof(ShellApp).Assembly);

        // Grab all exported commands, which might also contain built-in ones from tsh.
        var menus = exports.GetExports<IMenuCommand, IDictionary<string, object?>>();

        // There should be a newly exported menu with our identifier.
        var menu = menus.FirstOrDefault(x => "Test".Equals(x.Metadata["Name"]));

        Assert.NotNull(menu);
        Assert.NotNull(menu.Value);
    }

    [Theory]
    [InlineData(
        "Instance method",
        """
        namespace Terminal.Shell;

        partial class Test
        {
            [Menu("$identifier$")]
            public void Exit(IContext context) { }
        }
        """)]
    [InlineData(
        "Instance async method",
        """
        using System.Threading;
        using System.Threading.Tasks;
        
        namespace Terminal.Shell;

        partial class Test
        {
            [Menu("$identifier$")]
            public async Task ExitAsync(IContext context, CancellationToken cancellation) => await Task.Delay(0);
        }
        """)]
    [InlineData(
        "Instance task method no cancellation",
        """
        using System.Threading.Tasks;
        
        namespace Terminal.Shell;

        partial class Test
        {
            [Menu("$identifier$")]
            public Task ExitAsync(IContext context) => Task.CompletedTask;
        }
        """)]
    [InlineData(
        "Static method",
        """
        namespace Terminal.Shell;

        static partial class Test
        {
            [Menu("$identifier$")]
            public static void Exit(IContext context) { }
        }
        """)]
    [InlineData(
        "Static async method",
        """
        using System.Threading;
        using System.Threading.Tasks;
        
        namespace Terminal.Shell;

        static partial class Test
        {
            [Menu("$identifier$")]
            public static async Task ExitAsync(IContext context, CancellationToken cancellation) => await Task.Delay(0);
        }
        """)]
    public void GenerateMenuCommand(string scenario, string code)
    {
        var identifier = new string(scenario.Where(SyntaxFacts.IsIdentifierPartCharacter).ToArray());
        var source = code.Replace("$identifier$", identifier);
        var compilation = CreateCompilation(source);

        // Input source code should have no diagnostics already.
        Assert.Empty(compilation.GetDiagnostics());

        // Both menu method and type generator work in concert.
        var driver = CSharpGeneratorDriver.Create(new MenuCommandTypeGenerator(), new MenuCommandMethodGenerator());

        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostic).GetRunResult();

        // There should be no diagnostics either after generation.
        Assert.Empty(output.GetDiagnostics());

        // Emit the file to disk so we can analyze the resulting MEF composition
        var assembly = output.Load(identifier + ".dll");

        // Add Sdk and Shell which are always available to tsh app/extensions so code can use dependencies too.
        var exports = CompositionSetup.CreateProvider(assembly, typeof(IResourceManager).Assembly, typeof(ShellApp).Assembly);

        // Grab all exported commands, which might also contain built-in ones from tsh.
        var menus = exports.GetExports<IMenuCommand, IDictionary<string, object?>>();

        // There should be a newly exported menu with our identifier.
        var menu = menus.FirstOrDefault(x => identifier.Equals(x.Metadata["Name"]));

        Assert.NotNull(menu);
        // And we can actually instantiate it too.
        Assert.NotNull(menu.Value);
    }
}