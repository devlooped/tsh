using Devlooped.CodeAnalysis.Testing;
using AnalyzerTest = Microsoft.CodeAnalysis.Testing.AnalyzerTest<Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;

namespace Terminal.Shell.CodeAnalysis;

public class AnalyzerTests
{
    [Theory]
    [IncrementalGenerator<ExportGenerator>]
    [PackageReference("System.Composition.AttributedModel", "7.0.0")]
    [AssemblyReference("Terminal.Shell")]
    [AssemblyReference("Terminal.Shell.Sdk")]
    [AnalyzerDataAttribute<ExportAnalyzer>(
        """
        using System.Composition;
        
        public interface IFoo { }
        
        [Shared]
        public class Foo : IFoo { }
        """, 
        WithDiagnostic = "TSH0006",
        WithLocation = "6,14", 
        WithArguments = "Foo")]
    [AnalyzerDataAttribute<ExportAnalyzer>(
        """
        using System.Composition;
        
        public interface IFoo { }
        
        [Export]
        public class Foo : IFoo { }
        """,
        WithDiagnostic = "TSH0006",
        WithLocation = "6,14",
        WithArguments = "Foo")]
    [AnalyzerDataAttribute<MenuCommandAnalyzer>(
        """
        using System.Threading;
        using System.Threading.Tasks;
        using Terminal.Shell;
        
        public class MyMenu : IMenuCommand 
        { 
            public Task ExecuteAsync(CancellationToken cancellation = default) => Task.CompletedTask;
        }
        """,
        WithDiagnostic = "TSH0001",
        WithLocation = "5,14",
        WithArguments = "MyMenu")]
    public async Task TestAnalyzer(AnalyzerTest test)
    {
        await test.RunAsync();
    }

    class MyMenu : IMenuCommand
    {
        public Task ExecuteAsync(CancellationToken cancellation = default) => throw new NotImplementedException();
    }
}
