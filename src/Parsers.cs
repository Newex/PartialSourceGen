using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
}