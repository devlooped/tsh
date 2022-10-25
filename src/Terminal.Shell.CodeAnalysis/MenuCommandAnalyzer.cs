using System;
using System.Collections.Immutable;
using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Shell;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class MenuCommandAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.MenuCommandShouldBeAttributed, 
            Diagnostics.MenuCommandClassMustBePartial, 
            Diagnostics.MenuCommandMethodMustBeVisible, 
            Diagnostics.MenuCommandMethodClassMustBeVisible);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeTypeSymbol, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeMethodSymbol, SymbolKind.Method);
    }

    static void AnalyzeTypeSymbol(SymbolAnalysisContext context)
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

        // If type contains any methods annotated with [MenuCommand], but type is not partial, report diagnostic
        if (namedType.GetMembers().OfType<IMethodSymbol>().Any(
            m => m.GetAttributes().Any(a => a.AttributeClass?.Equals(menuAttribute, SymbolEqualityComparer.Default) ?? false)) &&
            !namedType.DeclaringSyntaxReferences.All(
            r => r.GetSyntax() is ClassDeclarationSyntax c && c.Modifiers.Any(
                m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MenuCommandClassMustBePartial,
                namedType.Locations[0],
                namedType.Name));
        }
    }

    static void AnalyzeMethodSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        var menuAttribute = context.Compilation.GetTypeByMetadataName("Terminal.Shell.MenuCommandAttribute");
        if (menuAttribute == null)
            return;

        // If [MenuCommand]-annotated method is not visible within assembly, report diagnostic
        if (method.GetAttributes().Any(a => a.AttributeClass?.Equals(menuAttribute, SymbolEqualityComparer.Default) ?? false))
        {
            if (!context.Compilation.IsSymbolAccessibleWithin(method, context.Compilation.Assembly))
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MenuCommandMethodMustBeVisible,
                    method.Locations[0],
                    method.Name));

            if (!context.Compilation.IsSymbolAccessibleWithin(method.ContainingType, context.Compilation.Assembly))
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MenuCommandMethodClassMustBeVisible,
                    method.Locations[0],
                    method.ContainingType.Name, method.Name));
        }
    }
}