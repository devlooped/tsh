using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Shell.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ImportingConstructorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.ExportedClassMustHaveImportingConstructor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeConstructorSymbol, SymbolKind.Method);
    }

    static void AnalyzeConstructorSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        var exportAttribute = context.Compilation.GetTypeByMetadataName("System.Composition.ExportAttribute");
        var sharedAttribute = context.Compilation.GetTypeByMetadataName("System.Composition.SharedAttribute");
        var importingAttribute = context.Compilation.GetTypeByMetadataName("System.Composition.ImportingConstructorAttribute");
        if (exportAttribute == null || sharedAttribute == null || importingAttribute == null || method.MethodKind != MethodKind.Constructor)
            return;

        bool IsExportAttribute(INamedTypeSymbol? attribute)
        {
            if (attribute == null || attribute.Name.Equals("object", StringComparison.OrdinalIgnoreCase))
                return false;

            return attribute.Equals(exportAttribute, SymbolEqualityComparer.Default) ||
                attribute.Equals(sharedAttribute, SymbolEqualityComparer.Default) ||
                IsExportAttribute(attribute.BaseType);
        }

        bool IsExported(INamedTypeSymbol? type) => type != null && type.GetAttributes().Any(attr => IsExportAttribute(attr.AttributeClass));

        if (!IsExported(method.ContainingType))
            return;

        if (method.ContainingType.GetMembers().OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Constructor)
            .Any(x =>
                // Must have either a parameterless ctor
                x.Parameters.Length == 0 ||
                // Or one annotated with [ImportingConstructor]
                x.GetAttributes().Any(a => a.AttributeClass?.Equals(importingAttribute, SymbolEqualityComparer.Default) == true)))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.ExportedClassMustHaveImportingConstructor,
            method.Locations[0],
            method.ContainingType.Name));
    }
}