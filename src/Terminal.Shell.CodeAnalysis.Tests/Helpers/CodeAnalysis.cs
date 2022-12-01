using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.Extensions.DependencyModel;
using Xunit.Sdk;

namespace Devlooped.CodeAnalysis.Testing;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CompilationDataAttribute : TestDataAttribute<CompilationDataAttribute.NullAnalyzer>
{
    public CompilationDataAttribute(string code, params object[] args)
        => (Code, Arguments)
        = (code, args);

    public string Code { get; }

    public object[] Arguments { get; }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var test = (AnalyzerTest<NullAnalyzer>)base.GetData(testMethod).First()[0];
        var project = test.GetProject();
        var compilation = project!.GetCompilationAsync().Result!;

        foreach (var transform in test.CompilationTransforms)
        {
            compilation = transform(compilation, project);
        }

        if (Arguments.Length > 0)
            yield return new[] { compilation }.Concat(Arguments).ToArray();
        else
            yield return new[] { compilation };
    }

    protected override void ConfigureData(MethodInfo method, AnalyzerTest<NullAnalyzer> data)
        => data.TestCode = Code;

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
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AnalyzerDataAttribute<TAnalyzer> : TestDataAttribute<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    public AnalyzerDataAttribute(string code) => Code = code;

    public string Code { get; }

    public string? WithDiagnostic { get; set; }
    public string? WithLocation { get; set; }
    public string? WithArguments { get; set; }

    protected override void ConfigureData(MethodInfo method, AnalyzerTest<TAnalyzer> data)
    {
        data.TestCode = Code;

        if (WithDiagnostic != null)
        {
            var analyzer = new TAnalyzer();
            var descriptor = analyzer.SupportedDiagnostics.FirstOrDefault(x => x.Id == WithDiagnostic);
            if (descriptor == null)
                throw new NotSupportedException($"Analyzer {typeof(TAnalyzer).Name} does not support expected diagnostic {WithDiagnostic}. Supported diagnostics: {string.Join(", ", analyzer.SupportedDiagnostics.Select(x => x.Id))}.");

            var expected = new DiagnosticResult(descriptor);
            if (WithLocation != null)
            {
                var parsed = WithLocation.Split(',').Select(int.Parse).ToArray();
                // Line/columns are 1-based, but the diagnostic is 0-based
                expected = expected.WithLocation(parsed[0], parsed[1]);
            }

            if (WithArguments != null)
            {
                expected = expected.WithArguments(WithArguments.Split(','));
            }

            data.ExpectedDiagnostics.Add(expected);
        }
    }
}

public abstract class TestDataAttribute<TAnalyzer> : DataAttribute where TAnalyzer : DiagnosticAnalyzer, new()
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var test = new AnalyzerTest<TAnalyzer>
        {
            TestState =
            {
                ReferenceAssemblies = new ReferenceAssemblies(
                    "net6.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref", "6.0.0"),
                        Path.Combine("ref", "net6.0"))
                    .AddPackages(testMethod.GetCustomAttributes<PackageReferenceAttribute>()
                        .Select(attr => new PackageIdentity(attr.Id, attr.Version))
                        .ToImmutableArray()),
            },
            CompilationTransforms =
            {
                (compilation, _) =>
                {
                    var output = compilation.AddReferences(testMethod.GetCustomAttributes<AssemblyReferenceAttribute>()
                        .SelectMany(attr => DependencyContext.Default!.CompileLibraries
                        .Where(x => x.Name == attr.AssemblyName)
                        .SelectMany(x => x.ResolveReferencePaths())
                        .Select(x => MetadataReference.CreateFromFile(x))));

                    var sourcegen = testMethod.GetCustomAttributes<SourceGeneratorAttribute>()
                        .Select(attr => attr.Generator)
                        .ToArray();

                    if (sourcegen.Length > 0)
                        CSharpGeneratorDriver.Create(sourcegen).RunGeneratorsAndUpdateCompilation(output, out output, out var _);

                    var incremental = testMethod.GetCustomAttributes<IncrementalGeneratorAttribute>()
                        .Select(attr => attr.Generator)
                        .ToArray();

                    if (incremental.Length > 0)
                        CSharpGeneratorDriver.Create(incremental).RunGeneratorsAndUpdateCompilation(output, out output, out var _);

                    return output;
                }
            },
        };

        ConfigureData(testMethod, test);

        yield return new object[] { test };
    }

    protected virtual void ConfigureData(MethodInfo method, AnalyzerTest<TAnalyzer> data) { }
}

public class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier> where TAnalyzer : DiagnosticAnalyzer, new()
{
    public Project? GetProject()
    {
        var analyzers = GetDiagnosticAnalyzers().ToArray();
        var defaultDiagnostic = GetDefaultDiagnostic(analyzers);
        var supportedDiagnostics = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToImmutableArray();
        var fixableDiagnostics = ImmutableArray<string>.Empty;
        var testState = TestState
            .WithInheritedValuesApplied(null, fixableDiagnostics)
            .WithProcessedMarkup(MarkupOptions, defaultDiagnostic, supportedDiagnostics, fixableDiagnostics, DefaultFilePath);

        var primaryProject = new EvaluatedProjectState(testState, ReferenceAssemblies);
        var additionalProjects = testState.AdditionalProjects.Values
            .Select(additionalProject => new EvaluatedProjectState(additionalProject, ReferenceAssemblies))
            .ToImmutableArray();

        var project = CreateProjectAsync(primaryProject, additionalProjects, default).Result;
        project = ApplyCompilationOptions(project);

        return project;
    }

    public override string ToString() => string.Join(Environment.NewLine, ExpectedDiagnostics.Select(x => x.ToString()));
}

public abstract class IncrementalGeneratorAttribute : Attribute
{
    public abstract IIncrementalGenerator Generator { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class IncrementalGeneratorAttribute<TGenerator> : IncrementalGeneratorAttribute where TGenerator : IIncrementalGenerator, new()
{
    public override IIncrementalGenerator Generator => new TGenerator();
}

public abstract class SourceGeneratorAttribute : Attribute
{
    public abstract ISourceGenerator Generator { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SourceGeneratorAttribute<TGenerator> : SourceGeneratorAttribute where TGenerator : ISourceGenerator, new()
{
    public override ISourceGenerator Generator => new TGenerator();
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class PackageReferenceAttribute : Attribute
{
    public PackageReferenceAttribute(string id, string version)
        => (Id, Version)
        = (id, version);

    public string Id { get; }
    public string Version { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AssemblyReferenceAttribute : Attribute
{
    public AssemblyReferenceAttribute(string assemblyName) => AssemblyName = assemblyName;

    public string AssemblyName { get; }
}