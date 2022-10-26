using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Terminal.Shell;

class ExportAction : SourceAction
{
    readonly SourceProductionContext ctx;
    readonly INamedTypeSymbol type;
    readonly bool exportInterfaces;

    public ExportAction(SourceProductionContext ctx, INamedTypeSymbol type, bool exportInterfaces = true)
        => (this.ctx, this.type, this.exportInterfaces)
        = (ctx, type, exportInterfaces);

    public override void Execute()
    {
        // If type is not a partial class, report diagnostic
        if (!type.DeclaringSyntaxReferences.All(
            r => r.GetSyntax() is ClassDeclarationSyntax c && c.Modifiers.Any(
                m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            // A separate analyzer should have already reported this scenario as a 
            // diagnostics
            return;
        }

        var selfExported = type.GetAttributes().FirstOrDefault(
            attr => attr.AttributeClass?.Name == "ExportAttribute" && attr.ConstructorArguments.Length == 0);

        var model = new
        {
            Namespace = type.ContainingNamespace.ToDisplayString(FullNameFormat),
            Type = type.Name,
            ExportSelf = selfExported == null,
            Interfaces = exportInterfaces ?
                type.AllInterfaces.Select(x => x.ToDisplayString(FullNameFormat)).ToArray() :
                Array.Empty<string>(),
        };

        if (model.ExportSelf || model.Interfaces.Length > 0)
        {
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Terminal.Shell.Export.sbntxt");
            using var reader = new StreamReader(resource!);
            var template = Template.Parse(reader.ReadToEnd());
            var output = template.Render(model, member => member.Name);

            ctx.AddSource($"{type.ToDisplayString(FileNameFormat)}.Exports.g", output);
        }
    }
}
