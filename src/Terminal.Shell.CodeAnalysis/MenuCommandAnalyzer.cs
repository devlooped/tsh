using System;
using System.Collections.Immutable;
using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Shell.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MenuCommandAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.MenuCommandShouldBeAttributed,
            Diagnostics.MenuCommandTypeMustBePartial,
            Diagnostics.MenuCommandMethodMustBeVisible,
            Diagnostics.MenuCommandMethodClassMustBeVisible,
            Diagnostics.MenuCommandRecordRequiresDefaultConstructor);

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
        var menuAttribute = context.Compilation.GetTypeByMetadataName("Terminal.Shell.MenuAttribute");
        var menuCommand = context.Compilation.GetTypeByMetadataName("Terminal.Shell.IMenuCommand");
        if (menuAttribute == null || menuCommand == null)
            return;

        // If [MenuCommand]-annotated type is not a partial type, report diagnostic
        if (namedType.GetAttributes().Any(a => a.AttributeClass?.Equals(menuAttribute, SymbolEqualityComparer.Default) ?? false) &&
            !namedType.DeclaringSyntaxReferences.All(
                r => r.GetSyntax() is TypeDeclarationSyntax c && c.Modifiers.Any(
                    m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MenuCommandTypeMustBePartial,
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
            r => r.GetSyntax() is TypeDeclarationSyntax c && c.Modifiers.Any(
                m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MenuCommandTypeMustBePartial,
                namedType.Locations[0],
                namedType.Name));

            // Records cannot have a ctor annotated with [ImportingConstructor], so, even 
            // if the type is partial, we cannot allow a primary record ctor in this case.
            if (namedType.IsRecord && !namedType.InstanceConstructors.Any(m => m.Parameters.IsDefaultOrEmpty))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MenuCommandRecordRequiresDefaultConstructor,
                    namedType.Locations[0],
                    namedType.Name));
            }
        }
    }

    static void AnalyzeMethodSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        var menuAttribute = context.Compilation.GetTypeByMetadataName("Terminal.Shell.MenuAttribute");
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