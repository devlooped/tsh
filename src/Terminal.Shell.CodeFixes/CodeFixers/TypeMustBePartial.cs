using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminal.Shell.CodeFixers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class TypeMustBePartial : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.MenuCommandTypeMustBePartial.Id,
        Diagnostics.ExportedClassMustBePartial.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var declaration = root.FindNode(context.Span).FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (declaration == null)
            return;
        
        context.RegisterCodeFix(
            new CodeActions.AddPartialModifier(context.Document, root, declaration),
            context.Diagnostics);
    }
}