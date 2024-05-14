using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PartialSourceGen;

/// <summary>
/// Parsing helpers
/// </summary>
public static class Parsers
{
    /// <summary>
    /// Extract the summary text from the partial attribute
    /// </summary>
    /// <param name="context">The context</param>
    /// <returns>A summary or null</returns>
    public static string? GetSummaryText(this GeneratorAttributeSyntaxContext context)
    {
        var args = context
            .TargetNode
            .DescendantNodes()
            .OfType<AttributeArgumentSyntax>();

        var summary = args
            .Where(n => n.Expression.IsKind(SyntaxKind.StringLiteralExpression))
            .Where(n => string.Equals("Summary", n.NameEquals?.Name.Identifier.ValueText, StringComparison.Ordinal))
            .Select(n => n.Expression)
            .OfType<LiteralExpressionSyntax>()
            .Select(n => n.Token.ValueText)
            .SingleOrDefault();

        return summary;
    }

    /// <summary>
    /// Extract the partial class name from the partial attribute
    /// </summary>
    /// <param name="context">The context</param>
    /// <returns>The partial class name or null</returns>
    public static string? GetPartialClassName(this GeneratorAttributeSyntaxContext context)
    {
        var args = context
            .TargetNode
            .DescendantNodes()
            .OfType<AttributeArgumentSyntax>();

        var partialClassName = args
            .Where(n => n.Expression.IsKind(SyntaxKind.StringLiteralExpression))
            .Where(n => string.Equals("PartialClassName", n.NameEquals?.Name.Identifier.ValueText, StringComparison.Ordinal))
            .Select(n => n.Expression)
            .OfType<LiteralExpressionSyntax>()
            .Select(n => n.Token.ValueText)
            .SingleOrDefault();

        return partialClassName?.Replace(" ", "");
    }

    /// <summary>
    /// Extract if required properties should be included from the partial attribute. Default false.
    /// </summary>
    /// <param name="context">The context</param>
    /// <returns>True if required properties should be included otherwise false</returns>
    public static bool GetIncludeRequiredProperties(this GeneratorAttributeSyntaxContext context)
    {
        var args = context
            .TargetNode
            .DescendantNodes()
            .OfType<AttributeArgumentSyntax>();

        var include = args
            .Where(n => n.Expression.IsKind(SyntaxKind.TrueLiteralExpression))
            .Where(n => string.Equals("IncludeRequiredProperties", n.NameEquals?.Name.Identifier.ValueText, StringComparison.Ordinal))
            .SingleOrDefault();

        return include is not null;
    }

    internal static bool PropertyMemberReferences(this PropertyDeclarationSyntax propertyDeclaration, SyntaxNode node, out Dictionary<string, MemberDeclarationSyntax>? result)
    {
        var members = propertyDeclaration.DescendantNodes().OfType<IdentifierNameSyntax>();

        if (members is null)
        {
            result = null;
            return false;
        }

        // The members are:
        result = [];
        foreach (var member in members)
        {
            GetMembers(node, member, ref result);
        }

        return result.Any();
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

    internal static void TryAdd(this Dictionary<string, MemberDeclarationSyntax> source, string key, MemberDeclarationSyntax value)
    {
        if (source is not null && !source.ContainsKey(key))
        {
            source.Add(key, value);
        }
    }

    /// <summary>
    /// Get any partial reference attribute info
    /// </summary>
    /// <param name="property">The current property under inspection</param>
    /// <param name="originalSource">The original type</param>
    /// <param name="partialSource">The partial type</param>
    /// <param name="name">The property name to use</param>
    /// <returns>True if has partial reference otherwise false</returns>
    public static bool GetPartialReferenceInfo(this PropertyDeclarationSyntax property, out IdentifierNameSyntax? originalSource, out string? partialSource, out string? name)
    {
        if (!property.AttributeLists.Any())
        {
            // No attributes
            originalSource = null;
            partialSource = null;
            name = null;
            return false;
        }

        // Only supported from NET7.0 and onwards with language version C# 11
        var generic = property.AttributeLists.SelectMany(ats => ats.Attributes.SelectMany(a => a.DescendantNodes())).OfType<GenericNameSyntax>().
            FirstOrDefault(a => a.Identifier.ValueText.StartsWith("PartialReference"));

        if (generic is not null)
        {
            // Extract type info
            originalSource = generic?.TypeArgumentList.Arguments.OfType<IdentifierNameSyntax>().FirstOrDefault();
            partialSource = generic?.TypeArgumentList.Arguments.OfType<IdentifierNameSyntax>().Skip(1).FirstOrDefault()?.Identifier.ValueText;
            name = generic?.Parent?.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.Parent.IsKind(SyntaxKind.AttributeArgument))
                .Select(f => f.Token.ValueText)
                .FirstOrDefault();
            return originalSource is not null && !string.IsNullOrWhiteSpace(partialSource);
        }
        else
        {
            // Non generic attributes "PartialReferenceAttribute"
            var attrs = property.DescendantNodes().OfType<AttributeSyntax>().SelectMany(a => a.DescendantNodes().OfType<IdentifierNameSyntax>())
                .Where(n => n.Identifier.ValueText.StartsWith("PartialReference"));
            if (attrs is null || !attrs.Any())
            {
                // Does not exist
                originalSource = null;
                partialSource = null;
                name = null;
                return false;
            }

            var attr = attrs.FirstOrDefault().Parent;
            if (attr is AttributeSyntax partialAttr)
            {
                // Extract type info
                var attrArgs = partialAttr.DescendantNodes().OfType<TypeOfExpressionSyntax>();
                originalSource = attrArgs.FirstOrDefault()?.Type as IdentifierNameSyntax;
                var second = attrArgs.Skip(1).FirstOrDefault().Type as IdentifierNameSyntax;
                partialSource = second?.Identifier.ValueText;
                name = partialAttr.DescendantNodes().OfType<LiteralExpressionSyntax>().SingleOrDefault()?.Token.ValueText;
                return originalSource is not null && !string.IsNullOrWhiteSpace(partialSource);
            }
        }

        originalSource = null;
        partialSource = null;
        name = null;
        return false;
    }

    /// <summary>
    /// Remove other classes/structs/records except the one currently in focus
    /// </summary>
    /// <param name="root">The root source code</param>
    /// <param name="entityToKeep">The entity to keep</param>
    /// <returns>The root node with only the entity to keep as a member node</returns>
    public static SyntaxNode FilterOutEntitiesExcept(this SyntaxNode root, SyntaxNode entityToKeep)
    {
        var typeDefinitions = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>();

        foreach (var t in typeDefinitions)
        {
            if (!t.IsEquivalentTo(entityToKeep))
            {
                var newNode = root.RemoveNode(t, SyntaxRemoveOptions.KeepNoTrivia);
                if (newNode is null)
                {
                    // We removed the whole thing D:
                    return root;
                }

                return FilterOutEntitiesExcept(newNode, entityToKeep);
            }
        }

        return root;
    }
}
