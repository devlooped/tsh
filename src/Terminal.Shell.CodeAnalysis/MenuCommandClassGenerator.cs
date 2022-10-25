using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Terminal.Shell.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public class MenuCommandClassGenerator : IIncrementalGenerator
{
    static readonly SymbolDisplayFormat fileName = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
    static readonly SymbolDisplayFormat fullName = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    record ResourceMetadata(string Name, string Namespace, string ResourceName);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attribute = context.CompilationProvider
            .Select((x, c) => x.GetTypeByMetadataName("Terminal.Shell.MenuCommandAttribute"));

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

        context.RegisterImplementationSourceOutput(menuTypes,
            (ctx, data) =>
            {
                // If type is not a partial class, report diagnostic
                if (!data.Type.DeclaringSyntaxReferences.All(
                    r => r.GetSyntax() is ClassDeclarationSyntax c && c.Modifiers.Any(
                        m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
                {
                    // The MenuCommandClassAnalyzer would have already reported this diagnostic
                    return;
                }

                var ns = data.Type.ContainingNamespace.ToDisplayString(fullName);
                var nsdot = ns + ".";

                string ToTypeName(ITypeSymbol type)
                {
                    var display = type.ToDisplayString(fullName);
                    if (display.StartsWith(nsdot))
                        return display[nsdot!.Length..];

                    return display;
                }

                var model = new
                {
                    Namespace = data.Type.ContainingNamespace.ToDisplayString(fullName),
                    Type = ToTypeName(data.Type),
                    Menus = data.Menus.Select(a => a.ConstructorArguments[0].Value).OfType<string>().ToList(),
                };

                using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Terminal.Shell.MenuCommandClass.sbntxt");
                using var reader = new StreamReader(resource!);
                var template = Template.Parse(reader.ReadToEnd());
                var output = template.Render(model, member => member.Name);

                ctx.AddSource($"{data.Type.ToDisplayString(fileName)}.{data.Type.Name}.g", output);
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
