using System.Reflection;
using Microsoft.CodeAnalysis;
using Scriban;

namespace Terminal.Shell.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public class MenuCommandMethodGenerator : IIncrementalGenerator
{
    record ResourceMetadata(string Name, string Namespace, string ResourceName);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attribute = context.CompilationProvider
            .Select((x, c) => x.GetTypeByMetadataName("Terminal.Shell.MenuAttribute"));

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

        context.RegisterImplementationSourceOutput(
            methodMenus.Combine(context.CompilationProvider),
            (ctx, data) =>
            {
                var method = data.Left.Method;
                var ns = method.ContainingNamespace.ToDisplayString(SourceAction.FullNameFormat);
                var nsdot = ns + ".";

                string ToTypeName(ITypeSymbol type)
                {
                    var display = type.ToDisplayString(SourceAction.FullNameFormat);
                    if (display.StartsWith(nsdot))
                        return display[nsdot!.Length..];

                    return display;
                }

                var type = $"{method.ContainingType.Name}_{method.Name}MenuCommand";
                var dependencies = method.Parameters
                    .Where(p => p.Type.Name != "CancellationToken")
                    .Select(p => new { p.Name, p.Type })
                    .ToList();

                if (!method.IsStatic)
                    dependencies.Insert(0, new { Name = "_instance", Type = (ITypeSymbol)method.ContainingType });

                // Always insert the threading context so we can run the menu command in the main thread
                dependencies.Insert(0, new
                {
                    Name = "_threading",
                    Type = (ITypeSymbol)data.Right.GetTypeByMetadataName("Terminal.Shell.IThreadingContext")!
                });

                var parameters = method.Parameters
                    .Select(p => p.Type.Name == "CancellationToken" ? "cancellation" : p.Name)
                    .ToList();

                var model = new
                {
                    Namespace = method.ContainingNamespace.ToDisplayString(SourceAction.FullNameFormat),
                    Target = method.IsStatic ? method.ContainingType.Name : "_instance",
                    Parent = method.ContainingType.Name,
                    Method = method.Name,
                    Menus = data.Left.Menus.Select(a => a.ConstructorArguments[0].Value).OfType<string>().ToList(),
                    IsAsync = method.ReturnType.Name == "Task",
                    Parameters = parameters,
                    Dependencies = dependencies.Select(x => new { x.Name, Type = ToTypeName(x.Type) }).ToList(),
                };

                using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Terminal.Shell.MenuCommandMethod.sbntxt");
                using var reader = new StreamReader(resource!);
                var template = Template.Parse(reader.ReadToEnd());
                var output = template.Render(model, member => member.Name);

                ctx.AddSource($"{method.ContainingType.ToDisplayString(SourceAction.FileNameFormat)}.{method.Name}.g", output);
            });

        context.RegisterImplementationSourceOutput(
            methodMenus
                .Select((x, _) => x.Method.ContainingType)
                // Only export if they aren't static clases
                .Where(x => !x.IsStatic)
                .Collect()
                .Combine(context.CompilationProvider),
            (ctx, data) =>
            {
                // The declaring type must be exported too
                foreach (var type in data.Left.Distinct(SymbolEqualityComparer.Default))
                {
                    if (type is not INamedTypeSymbol named)
                        continue;

                    // Only force-export if not already annotated with [System.Composition.Export] or [System.Composition.Shared]
                    if (named.GetAttributes().Any(
                        a => a.AttributeClass?.ToDisplayString(SourceAction.FullNameFormat) == "System.Composition.ExportAttribute" ||
                             a.AttributeClass?.ToDisplayString(SourceAction.FullNameFormat) == "System.Composition.SharedAttribute"))
                        continue;

                    new ExportAction(ctx, named, data.Right, false).Execute();
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
