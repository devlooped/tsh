using Microsoft.CodeAnalysis.CSharp;
using Terminal.Shell.CodeAnalysis;
using static Terminal.Shell.CodeAnalysis.CodeAnalysisHelpers;

namespace Terminal.Shell.Roslyn.Tests;

public record ContextGeneratorTests(ITestOutputHelper Output)
{
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
        var generator = new ContextGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostic).GetRunResult();

        Assert.Empty(output.GetDiagnostics());
    }
}