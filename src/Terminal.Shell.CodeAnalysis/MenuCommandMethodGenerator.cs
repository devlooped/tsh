using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Terminal.Shell.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public class MenuCommandMethodGenerator : IIncrementalGenerator
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

        var methods = context.CompilationProvider.SelectMany((x, c) =>
        {
            var visitor = new MethodsVisitor(s =>
                // Must be declared in the current assembly
                s.ContainingAssembly.Equals(x.Assembly, SymbolEqualityComparer.Default) &&
                // And be accessible within the current assembly (i.e. not a private nested type)
                x.IsSymbolAccessibleWithin(s, x.Assembly), c);

            x.GlobalNamespace.Accept(visitor);

            return visitor.MethodSymbols;
        });

        var methodMenus = methods
            .Combine(attribute)
            .Where(x => x.Right != null)
            .Where(x => x.Left.GetAttributes().Any(a => IsMenuAttribute(a, x.Right!)))
            .Select((x, _) => new
            {
                Method = x.Left,
                Menus = x.Left.GetAttributes().Where(a => IsMenuAttribute(a, x.Right!)).ToList()
            });

        context.RegisterImplementationSourceOutput(methodMenus,
            (ctx, data) =>
            {
                var ns = data.Method.ContainingNamespace.ToDisplayString(fullName);
                var nsdot = ns + ".";

                string ToTypeName(ITypeSymbol type)
                {
                    var display = type.ToDisplayString(fullName);
                    if (display.StartsWith(nsdot))
                        return display[nsdot!.Length..];

                    return display;
                }

                var type = $"{data.Method.ContainingType.Name}_{data.Method.Name}MenuCommand";
                var dependencies = data.Method.Parameters
                    .Where(p => p.Type.Name != "CancellationToken")
                    .Select(p => new { p.Name, p.Type })
                    .ToList();
                
                if (!data.Method.IsStatic)
                    dependencies.Insert(0, new { Name = "_instance", Type = (ITypeSymbol)data.Method.ContainingType });

                var parameters = data.Method.Parameters
                    .Select(p => p.Type.Name == "CancellationToken" ? "cancellation" : p.Name)
                    .ToList();

                var model = new
                {
                    Namespace = data.Method.ContainingNamespace.ToDisplayString(fullName),
                    Target = data.Method.IsStatic ? data.Method.ContainingType.Name : "_instance",
                    Parent = data.Method.ContainingType.Name,
                    Method = data.Method.Name,
                    Menus = data.Menus.Select(a => a.ConstructorArguments[0].Value).OfType<string>().ToList(),
                    IsAsync = data.Method.ReturnType.Name == "Task",
                    Parameters = parameters,
                    Dependencies = dependencies.Select(x => new { x.Name, Type = ToTypeName(x.Type) }).ToList(),
                };

                using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Terminal.Shell.MenuCommandMethod.sbntxt");
                using var reader = new StreamReader(resource!);
                var template = Template.Parse(reader.ReadToEnd());
                var output = template.Render(model, member => member.Name);

                ctx.AddSource($"{data.Method.ContainingType.ToDisplayString(fileName)}.{data.Method.Name}.g", output);
            });

        context.RegisterImplementationSourceOutput(
            methodMenus.Select((x, _) => x.Method.ContainingType).Collect(),
            (ctx, data) =>
            {
                // The declaring type must be exported too
                foreach (var type in data.Distinct(SymbolEqualityComparer.Default))
                {
                    if (type is not INamedTypeSymbol named)
                        continue;

                    new ExportAction(ctx, named, false).Execute();
                }
            });
    }

    static bool IsMenuAttribute(AttributeData data, INamedTypeSymbol attribute) =>
        data.AttributeClass?.Equals(attribute, SymbolEqualityComparer.Default) ?? false;

    class MethodsVisitor : SymbolVisitor
    {
        Func<IMethodSymbol, bool> shouldInclude;
        CancellationToken cancellation;
        HashSet<IMethodSymbol> methods = new(SymbolEqualityComparer.Default);

        public MethodsVisitor(Func<IMethodSymbol, bool> shouldInclude, CancellationToken cancellation)
        {
            this.shouldInclude = shouldInclude;
            this.cancellation = cancellation;
        }

        public HashSet<IMethodSymbol> MethodSymbols => methods;

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

            foreach (var member in type.GetMembers())
                member.Accept(this);

            foreach (var nestedType in type.GetTypeMembers())
                nestedType.Accept(this);
        }

        public override void VisitMethod(IMethodSymbol symbol)
        {
            if (shouldInclude(symbol))
                methods.Add(symbol);
        }
    }
}
