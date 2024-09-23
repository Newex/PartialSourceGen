using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PartialSourceGen.Constants;

namespace PartialSourceGen.Helpers;

/// <summary>
/// Collection of utilities to help transformation, building source code.
/// </summary>
public static class Utilities
{
    /// <summary>
    /// Create a <see cref="TypeSyntax"/> from a type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>A type syntax.</returns>
    public static TypeSyntax CreateTypeSyntax(Type type)
    {
        var typeName = type.FullName;
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName.Split('`')[0];
            var genericArguments = type.GetGenericArguments();

            // Recursion!
            var genericArgsSyntax = SyntaxFactory.SeparatedList(genericArguments.Select(arg => CreateTypeSyntax(arg)));

            return SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(genericTypeName),
                    SyntaxFactory.TypeArgumentList(genericArgsSyntax));
        }

        return SyntaxFactory.ParseTypeName(typeName);
    }

    /// <summary>
    /// Retrieve field and method references
    /// </summary>
    /// <param name="node">The partial node</param>
    /// <param name="ident">The current identifier</param>
    /// <param name="result">The resulting found members</param>
    public static void GetMembers(SyntaxNode node, IdentifierNameSyntax ident, ref Dictionary<string, MemberDeclarationSyntax> result)
    {
        var current = node.DescendantNodes()
                          .OfType<FieldDeclarationSyntax>()
                          .SingleOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.ValueText.Equals(ident.Identifier.ValueText)));

        if (current is not null)
        {
            var name = ident.Identifier.ValueText;
            result.TryAdd(name, current);
            return;
        }

        var method = node.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .SingleOrDefault(m => m.Identifier.ValueText.Equals(ident.Identifier.ValueText));

        if (method is not null)
        {
            // Add method
            var name = method.Identifier.ValueText;
            result.TryAdd(name, method);

            // Check method locals for field refs
            var locals = method.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var local in locals)
            {
                GetMembers(node, local, ref result);
            }
        }
    }

    /// <summary>
    /// Checks whether a string is not a partial attribute.
    /// </summary>
    /// <param name="input">The string input.</param>
    /// <returns>True if not a partial attribute otherwise false.</returns>
    public static bool IsNotLocalAttribute(string input) =>
        !(input.StartsWith(Names.IncludeInitializer)
        || input.StartsWith(Names.PartialReference)
        || input.StartsWith(Names.ExcludePartial)
        || input.StartsWith(Names.PartialType));
}