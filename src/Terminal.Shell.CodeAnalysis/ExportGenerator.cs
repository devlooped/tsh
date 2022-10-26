using System;
using Microsoft.CodeAnalysis;

namespace Terminal.Shell.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public class ExportGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var exportAttr = context.CompilationProvider
            .Select((x, c) => x.GetTypeByMetadataName("System.Composition.ExportAttribute"));

        var sharedAttr = context.CompilationProvider
            .Select((x, c) => x.GetTypeByMetadataName("System.Composition.SharedAttribute"));

        var types = context.CompilationProvider.SelectMany((x, c) =>
        {
            var visitor = new TypesVisitor(s =>
                // Must be declared in the current assembly
                s.ContainingAssembly.Equals(x.Assembly, SymbolEqualityComparer.Default) &&
                // And be accessible within the current assembly (i.e. not a private nested type)
                x.IsSymbolAccessibleWithin(s, x.Assembly), c);

            x.GlobalNamespace.Accept(visitor);

            return visitor.TypeSymbols;
        });

        var attributes = exportAttr.Combine(sharedAttr);
        var exportedTypes = types
            .Combine(attributes)
            .Where(x => x.Right.Left != null && x.Right.Right != null)
            .Where(x => x.Left.GetAttributes().Any(
                a => x.Right.Left!.Equals(a.AttributeClass, SymbolEqualityComparer.Default) ||
                     x.Right.Right!.Equals(a.AttributeClass, SymbolEqualityComparer.Default)))
            .Select((x, _) => x.Left);

        // Emit partial class exporting all interfaces
        context.RegisterImplementationSourceOutput(
            exportedTypes,
            (ctx, data) => new ExportAction(ctx, data, true).Execute());
    }

    static bool IsMenuAttribute(AttributeData data, INamedTypeSymbol attribute) =>
        data.AttributeClass?.Equals(attribute, SymbolEqualityComparer.Default) ?? false;

    class TypesVisitor : SymbolVisitor
    {
        Func<ISymbol, bool> shouldInclude;
        CancellationToken cancellation;
        HashSet<INamedTypeSymbol> types = new(SymbolEqualityComparer.Default);

        public TypesVisitor(Func<ISymbol, bool> shouldInclude, CancellationToken cancellation)
        {
            this.shouldInclude = shouldInclude;
            this.cancellation = cancellation;
        }

        public HashSet<INamedTypeSymbol> TypeSymbols => types;

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            cancellation.ThrowIfCancellationRequested();
            symbol.GlobalNamespace.Accept(this);
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var namespaceOrType in symbol.GetMembers())
            {
                cancellation.ThrowIfCancellationRequested();
                namespaceOrType.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol type)
        {
            cancellation.ThrowIfCancellationRequested();

            if (!shouldInclude(type) || !types.Add(type))
                return;

            var nestedTypes = type.GetTypeMembers();
            if (nestedTypes.IsDefaultOrEmpty)
                return;

            foreach (var nestedType in nestedTypes)
            {
                cancellation.ThrowIfCancellationRequested();
                nestedType.Accept(this);
            }
        }
    }
}
