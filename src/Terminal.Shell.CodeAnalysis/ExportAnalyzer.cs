using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Shell;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class ExportAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.ExportedClassMustBePartial);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeTypeSymbol, SymbolKind.NamedType);
    }

    static void AnalyzeTypeSymbol(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        var exportAttribute = context.Compilation.GetTypeByMetadataName("System.Composition.ExportAttribute");
        var sharedAttribute = context.Compilation.GetTypeByMetadataName("System.Composition.SharedAttribute");
        if (exportAttribute == null || sharedAttribute == null)
            return;

        var exported = namedType.GetAttributes().Any(
            a => exportAttribute.Equals(a.AttributeClass, SymbolEqualityComparer.Default) ||
                 sharedAttribute.Equals(a.AttributeClass, SymbolEqualityComparer.Default));

        if (!exported)
            return;

        if (!namedType.DeclaringSyntaxReferences.All(
            r => r.GetSyntax() is ClassDeclarationSyntax c && c.Modifiers.Any(
                m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ExportedClassMustBePartial,
                namedType.Locations[0],
                namedType.Name));
        }
    }
}