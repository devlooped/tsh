using System;
using System.Collections.Immutable;
using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Shell;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MenuCommandClassAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.MenuCommandShouldBeAttributed, Diagnostics.MenuCommandClassMustBePartial);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        var menuAttribute = context.Compilation.GetTypeByMetadataName("Terminal.Shell.MenuCommandAttribute");
        var menuCommand = context.Compilation.GetTypeByMetadataName("Terminal.Shell.IMenuCommand");
        if (menuAttribute == null || menuCommand == null)
            return;

        // If [MenuCommand]-annotated type is not a partial class, report diagnostic
        if (namedType.GetAttributes().Any(a => a.AttributeClass?.Equals(menuAttribute, SymbolEqualityComparer.Default) ?? false) && 
            !namedType.DeclaringSyntaxReferences.All(
                r => r.GetSyntax() is ClassDeclarationSyntax c && c.Modifiers.Any(
                    m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MenuCommandClassMustBePartial,
                namedType.Locations[0],
                namedType.Name));
        }

        // If type implements IMenuCommand, but is not annotated with [MenuCommand], report diagnostic
        if (namedType.AllInterfaces.Any(i => i.Equals(menuCommand, SymbolEqualityComparer.Default)) &&
            !namedType.GetAttributes().Any(a => a.AttributeClass?.Equals(menuAttribute, SymbolEqualityComparer.Default) ?? false))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MenuCommandShouldBeAttributed,
                namedType.Locations[0],
                namedType.Name));
        }
    }
}