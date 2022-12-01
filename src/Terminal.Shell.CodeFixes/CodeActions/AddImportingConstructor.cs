using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Terminal.Shell.CodeAnalysis;

public class AddImportingConstructor : CodeAction
{
    readonly Document document;
    readonly SyntaxNode root;
    readonly ConstructorDeclarationSyntax declaration;

    public AddImportingConstructor(Document document, SyntaxNode root, ConstructorDeclarationSyntax declaration)
        => (this.document, this.root, this.declaration)
        = (document, root, declaration);

    public override string Title => "Add [ImportingConstructor]";
    public override string EquivalenceKey => Title;

    protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        => Task.FromResult(document.WithSyntaxRoot(
            root.ReplaceNode(declaration,
                declaration.AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("ImportingConstructor"))))))));
}