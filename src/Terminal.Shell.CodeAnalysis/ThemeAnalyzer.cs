using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Shell.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
class ThemeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Diagnostics.ThemeMustBeColorScheme);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeTypeSymbol, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzePropertySymbol, SymbolKind.Property);
    }

    static void AnalyzeTypeSymbol(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;
        var themeAttribute = context.Compilation.GetTypeByMetadataName("Terminal.Shell.ThemeAttribute");
        var colorScheme = context.Compilation.GetTypeByMetadataName("Terminal.Gui.ColorScheme");

        if (themeAttribute == null || colorScheme == null)
            return;

        var theme = namedType.GetAttributes().FirstOrDefault(
            a => a.AttributeClass?.Equals(themeAttribute, SymbolEqualityComparer.Default) ?? false);

        // If type has theme attribute, it must inherit from ColorScheme
        if (theme != null)
        {
            // It must inherit directly or indirectly from ColorScheme
            var tested = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var baseType = namedType.BaseType;
            while ((baseType = namedType.BaseType) != null && !tested.Contains(baseType))
            {
                if (baseType.Equals(colorScheme, SymbolEqualityComparer.Default))
                    return;

                tested.Add(baseType);
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ThemeMustBeColorScheme,
                namedType.Locations[0],
                theme.ConstructorArguments.Select(arg => arg.Value?.ToString()).FirstOrDefault() ?? namedType.Name));
        }
    }

    static void AnalyzePropertySymbol(SymbolAnalysisContext context)
    {
        var property = (IPropertySymbol)context.Symbol;
        var themeAttribute = context.Compilation.GetTypeByMetadataName("Terminal.Shell.ThemeAttribute");
        var colorScheme = context.Compilation.GetTypeByMetadataName("Terminal.Gui.ColorScheme");

        if (themeAttribute == null || colorScheme == null)
            return;

        var theme = property.GetAttributes().FirstOrDefault(
            a => a.AttributeClass?.Equals(themeAttribute, SymbolEqualityComparer.Default) ?? false);

        // If property is annotated with theme attribute, its type must be ColorScheme
        if (theme != null &&
            !property.Type.Equals(colorScheme, SymbolEqualityComparer.Default))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ThemeMustBeColorScheme,
                property.Locations[0],
                theme.ConstructorArguments.Select(arg => arg.Value?.ToString()).FirstOrDefault() ?? property.Name));
        }
    }
}

