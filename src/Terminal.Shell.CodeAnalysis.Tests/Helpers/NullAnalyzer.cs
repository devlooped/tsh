using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Terminal.Shell.CodeAnalysis;

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