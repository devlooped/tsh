using Devlooped.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Terminal.Shell.CodeAnalysis.CodeAnalysisHelpers;

namespace Terminal.Shell.CodeAnalysis;

public record ContextGeneratorTests(ITestOutputHelper Output)
{
    [Theory(Skip = "Pending")]
    [IncrementalGenerator<ContextExpressionGenerator>]
    [PackageReference("System.Composition.AttributedModel", "7.0.0")]
    [AssemblyReference("Terminal.Shell")]
    [AssemblyReference("Terminal.Shell.Sdk")]
    [CompilationData(
        """
        namespace Terminal.Shell;
        
        public class Dummy 
        {
            [Foo(expression: "IsGreat && IsTest")]
            public void Run() { }
        }
        
        [System.AttributeUsage(System.AttributeTargets.All)]
        public class FooAttribute : System.Attribute
        {
            public FooAttribute([ContextExpression] string expression) { }
        }
        """,
        "IsGreat && IsTest")]
    public void TestAnalyzer(Compilation compilation, string expression)
    {
        // Emit the file to disk so we can analyze the resulting MEF composition
        var assembly = compilation.Load();

        // Add Sdk and Shell which are always available to tsh app/extensions so code can use dependencies too.
        var exports = CompositionSetup.CreateProvider(assembly, typeof(IResourceManager).Assembly, typeof(ShellApp).Assembly);

        var context = exports.GetExportedValue<IContext>();

        Assert.False(context.Evaluate(expression));
    }

    [Fact]
    public void GeneratesPushExtensionsPartial()
    {
        var code =
            """
            #nullable enable

            namespace Terminal.Shell;

            public record GitHub(string Login)
            {
                public string? Organization { get; init; }
                public int? Id { get; init; }
            };

            public class Test
            {
                public Test(IContext context)
                {
                    var user = new GitHub("kzu");
                    using var disposable = context.Push("User", user);
                }
            }
            """;

        var compilation = CreateCompilation(code);
        var generator = new ContextExtensionsGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostic).GetRunResult();

        Assert.Empty(output.GetDiagnostics());
    }
}