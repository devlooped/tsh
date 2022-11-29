using Microsoft.CodeAnalysis;

namespace Terminal.Shell.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public class MenuCommandTypeGenerator : IIncrementalGenerator
{
    record ResourceMetadata(string Name, string Namespace, string ResourceName);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attribute = context.CompilationProvider
            .Select((x, c) => x.GetTypeByMetadataName("Terminal.Shell.MenuAttribute"));

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

        var menuTypes = types
            .Combine(attribute)
            .Where(x => x.Right != null)
            .Where(x => x.Left.GetAttributes().Any(a => IsMenuAttribute(a, x.Right!)))
            .Select((x, _) => new
            {
                Type = x.Left,
                Menus = x.Left.GetAttributes().Where(a => IsMenuAttribute(a, x.Right!)).ToList()
            });

        context.RegisterImplementationSourceOutput(
            menuTypes.Combine(context.CompilationProvider),
            (ctx, data) =>
            {
                new MenuCommandTypeAction(
                    ctx, data.Left.Type, data.Left.Menus.Select(
                        a => a.ConstructorArguments[0].Value).OfType<string>().ToList()).Execute();

                // Emit partial class exporting the IMenuCommand and any extra interfaces.
                new ExportAction(ctx, data.Left.Type, data.Right, true).Execute();
            });
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
