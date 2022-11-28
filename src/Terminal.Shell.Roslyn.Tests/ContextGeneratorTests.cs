using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Terminal.Shell.CodeAnalysis;
using Test = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Terminal.Shell.Roslyn.Tests.NullAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
//using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Terminal.Shell.CodeAnalysis.ContextGenerator, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace Terminal.Shell.Roslyn.Tests;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create<DiagnosticDescriptor>();

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    }
}

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

    static Compilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic)
                .Select(x => MetadataReference.CreateFromFile(x.Location))
                .ToArray(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
            throw new ArgumentException($"Resource '{name}' not found on assembly '{assembly.GetName().Name}'.", nameof(name));

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}