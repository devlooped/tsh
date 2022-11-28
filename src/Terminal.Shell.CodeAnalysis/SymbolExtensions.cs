using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis;

static class SymbolExtensions
{
    static readonly SymbolDisplayFormat fullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    static readonly SymbolDisplayFormat nonGenericFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public static string ToAssemblyNamespace(this INamespaceSymbol symbol)
        => symbol.ContainingAssembly.Name + "." + symbol.ToDisplayString(fullNameFormat);

    public static string ToFullName(this ISymbol symbol, Compilation compilation)
    {
        var fullName = symbol.ToDisplayString(nonGenericFormat);

        if (symbol is INamedTypeSymbol named)
        {
            if (named.IsSpecialType())
            {
                return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }
            else if (named.IsGenericType)
            {
                // Need to do ToFullName for each generic parameter.
                var genericArguments = named.TypeArguments.Select(t => t.ToFullName(compilation));
                fullName = GenericName(fullName).WithTypeArgumentList(
                        TypeArgumentList(SeparatedList<TypeSyntax>(genericArguments.Select(IdentifierName))))
                    .ToString();
            }
        }

        if (compilation.GetMetadataReference(symbol.ContainingAssembly) is MetadataReference reference &&
            !reference.Properties.Aliases.IsDefaultOrEmpty)
            return reference.Properties.Aliases.First() + "::" + fullName;

        return "global::" + fullName;
    }

    public static string ToFullName(this ISymbol symbol, NameSyntax name, CancellationToken cancellation = default)
    {
        var fullName = symbol.ToDisplayString(fullNameFormat);
        var root = name.SyntaxTree.GetRoot(cancellation);
        var aliases = root.ChildNodes().OfType<ExternAliasDirectiveSyntax>().Select(x => x.Identifier.Text).ToList();

        var candidate = name;
        while (candidate is QualifiedNameSyntax qualified)
            candidate = qualified.Left;

        if (candidate is IdentifierNameSyntax identifier &&
            aliases.FirstOrDefault(x => x == identifier.Identifier.Text) is string alias)
            return alias + ":" + fullName;

        return fullName;
    }

    public static bool IsNullable(this INamedTypeSymbol symbol)
        => symbol.IsGenericType &&
            symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    public static bool IsSpecialType(this ISymbol symbol)
        => symbol is INamedTypeSymbol named && named.IsSpecialType();

    public static bool IsSpecialType(this INamedTypeSymbol named)
        => named.SpecialType == SpecialType.System_Boolean ||
           named.SpecialType == SpecialType.System_Byte ||
           named.SpecialType == SpecialType.System_Char ||
           named.SpecialType == SpecialType.System_DateTime ||
           named.SpecialType == SpecialType.System_Decimal ||
           named.SpecialType == SpecialType.System_Double ||
           named.SpecialType == SpecialType.System_Int16 ||
           named.SpecialType == SpecialType.System_Int32 ||
           named.SpecialType == SpecialType.System_Int64 ||
           named.SpecialType == SpecialType.System_Object ||
           named.SpecialType == SpecialType.System_SByte ||
           named.SpecialType == SpecialType.System_Single ||
           named.SpecialType == SpecialType.System_String ||
           named.SpecialType == SpecialType.System_UInt16 ||
           named.SpecialType == SpecialType.System_UInt32 ||
           named.SpecialType == SpecialType.System_UInt64 ||
           (named.IsNullable() && named.TypeArguments[0].IsSpecialType());

    /// <summary>
    /// Checks whether the <paramref name="this"/> type inherits or implements the 
    /// <paramref name="baseTypeOrInterface"/> type, even if it's a generic type.
    /// </summary>
    public static bool Is(this ITypeSymbol? @this, ITypeSymbol? baseTypeOrInterface)
    {
        if (@this == null || baseTypeOrInterface == null)
            return false;

        if (@this.Equals(baseTypeOrInterface, SymbolEqualityComparer.Default) == true)
            return true;

        if (baseTypeOrInterface is INamedTypeSymbol namedExpected &&
            @this is INamedTypeSymbol namedActual &&
            namedActual.IsGenericType &&
            namedActual.ConstructedFrom.Equals(namedExpected, SymbolEqualityComparer.Default))
            return true;

        foreach (var iface in @this.AllInterfaces)
            if (iface.Is(baseTypeOrInterface))
                return true;

        if (@this.BaseType?.Name.Equals("object", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        return Is(@this.BaseType, baseTypeOrInterface);
    }
}
