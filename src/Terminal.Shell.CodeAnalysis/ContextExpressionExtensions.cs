using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminal.Shell;

static class ContextExpressionExtensions
{
    public static bool IsNamedArgContextExpression(INamedTypeSymbol expr, IMethodSymbol ctor, AttributeArgumentSyntax arg, AttributeSyntax attr)
        => arg.NameColon?.Name.ToString() is string name &&
           ctor.Parameters.FirstOrDefault(x => x.Name == name) is IParameterSymbol param &&
           param.GetAttributes().Any(x => expr.Equals(x.AttributeClass, SymbolEqualityComparer.Default));

    public static bool IsNamedPropContextExpression(INamedTypeSymbol expr, IMethodSymbol ctor, AttributeArgumentSyntax arg, AttributeSyntax attr)
        => arg.NameEquals?.Name.ToString() is string name &&
           ctor.ContainingType.GetMembers(name).FirstOrDefault() is IPropertySymbol prop &&
           prop.GetAttributes().Any(x => expr.Equals(x.AttributeClass, SymbolEqualityComparer.Default));

    public static bool IsIndexArgContextExpresion(INamedTypeSymbol expr, IMethodSymbol ctor, AttributeArgumentSyntax arg, AttributeSyntax attr)
        => attr.ArgumentList!.Arguments.IndexOf(arg) is int index &&
           ctor.Parameters.Length > index &&
           ctor.Parameters[index].GetAttributes().Any(x => expr.Equals(x.AttributeClass, SymbolEqualityComparer.Default));
}
