using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Composition;

namespace Terminal.Shell.CodeFixers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ImportingConstructorRequired : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.ExportedClassMustHaveImportingConstructor.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var declaration = root.FindNode(context.Span).FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
        if (declaration == null)
            return;

        context.RegisterCodeFix(
            new CodeActions.AddImportingConstructor(context.Document, root, declaration),
            context.Diagnostics);
    }
}