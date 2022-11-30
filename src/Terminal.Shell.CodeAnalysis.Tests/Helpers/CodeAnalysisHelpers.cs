using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
//using Test = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Terminal.Shell.Roslyn.Tests.NullAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
//using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Terminal.Shell.CodeAnalysis.ContextGenerator, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace Terminal.Shell.CodeAnalysis;

static class CodeAnalysisHelpers
{
    /// <summary>
    /// Creates a compilation with the same set of references as the current 
    /// project, and the given sources as additional source code.
    /// </summary>
    /// <seealso cref="https://github.com/dotnet/core/issues/2082#issuecomment-442713181"/>
    public static Compilation CreateCompilation(params string[] sources)
        => CSharpCompilation.Create("compilation",
            sources
                .Select(s => CSharpSyntaxTree.ParseText(s, new CSharpParseOptions(LanguageVersion.Latest)))
                .ToArray(),
            DependencyContext.Default?.CompileLibraries
                .SelectMany(cl => cl.ResolveReferencePaths())
                .Select(asm => MetadataReference.CreateFromFile(asm))
                .ToArray(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    /// <summary>
    /// Emits to an output assembly named after the current test method.
    /// </summary>
    public static Assembly Load(this Compilation compilation, [CallerMemberName] string? caller = default, [CallerFilePath] string? file = default)
        => Load(compilation, Path.GetFileNameWithoutExtension(file!) + "_" + caller);

    public static Assembly Load(this Compilation compilation, string name)
    {
        var path = Guid.NewGuid().ToString("n") + ".dll";
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        
        var result = compilation.WithAssemblyName(name).Emit(path);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.GetMessage())));
        
        var context = new AssemblyLoadContext(name, true);
        return context.LoadFromAssemblyPath(new FileInfo(path).FullName);
    }

    /// <summary>
    /// Reads a resource as a string, from the assembly containing the given <paramref name="type"/>.
    /// </summary>
    public static string ReadResource(Type type, string name) => ReadResource(type.Assembly, name);

    /// <summary>
    /// Reads a resource as a string from the given <paramref name="assembly"/>.
    /// </summary>
    public static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
            throw new ArgumentException($"Resource '{name}' not found on assembly '{assembly.GetName().Name}'.", nameof(name));

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

}
