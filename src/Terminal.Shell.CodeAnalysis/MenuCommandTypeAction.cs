using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Terminal.Shell;

class MenuCommandTypeAction : SourceAction
{
    readonly SourceProductionContext ctx;
    readonly INamedTypeSymbol type;
    readonly ICollection<string> menus;

    public MenuCommandTypeAction(SourceProductionContext ctx, INamedTypeSymbol type, ICollection<string> menus)
        => (this.ctx, this.type, this.menus)
        = (ctx, type, menus);

    public override void Execute()
    {
        if (!type.DeclaringSyntaxReferences.All(
            r => r.GetSyntax() is TypeDeclarationSyntax c && c.Modifiers.Any(
                m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            // The MenuCommandAnalyzer would have already reported this diagnostic
            return;
        }

        var ns = type.ContainingNamespace.ToDisplayString(FullNameFormat);
        var nsdot = ns + ".";

        string ToTypeName(ITypeSymbol type)
        {
            var display = type.ToDisplayString(FullNameFormat);
            if (display.StartsWith(nsdot))
                return display[nsdot!.Length..];

            return display;
        }

        var model = new
        {
            Namespace = type.ContainingNamespace.ToDisplayString(FullNameFormat),
            Type = ToTypeName(type),
            Kind = type.TypeKind == TypeKind.Struct ? "struct" : "class",
            Record = type.IsRecord ? "record " : "",
            Menus = menus,
        };

        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Terminal.Shell.MenuCommandType.sbntxt");
        using var reader = new StreamReader(resource!);
        var template = Template.Parse(reader.ReadToEnd());
        var output = template.Render(model, member => member.Name);

        ctx.AddSource($"{type.ToDisplayString(FileNameFormat)}.Menus.g", output);
    }
}
