using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PartialSourceGen.Constants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PartialSourceGen.Helpers;

/// <summary>
/// Construct partial entity helper methods
/// </summary>
public static class ExtensionHelpers
{
    /// <summary>
    /// Add the provided summary OR keep the original summary for the entity
    /// </summary>
    /// <param name="node">The partial entity class, record or struct</param>
    /// <param name="original">The original entity class, record or struct</param>
    /// <param name="summary">The optional summary text</param>
    /// <returns>A constructed node containing the summary blurb</returns>
    public static SyntaxNode WithSummary(this SyntaxNode node, SyntaxNode original, string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            var originalSummary = original.GetLeadingTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
            return node.WithLeadingTrivia(originalSummary);
        }

        var txt = "/// <summary>\n" + @"/// " + summary + "\n" + "/// </summary>\n";
        var summaryNode = SyntaxFactory.TriviaList(SyntaxFactory.ParseLeadingTrivia(txt)).Insert(0, SyntaxFactory.CarriageReturnLineFeed);
        return node.WithLeadingTrivia(summaryNode);
    }

    /// <summary>
    /// Adds constructors if it is a struct containing field initializers
    /// </summary>
    /// <param name="node">The partial entity</param>
    /// <param name="original">The original entity</param>
    /// <param name="hasPropertyInitializers">True if the original contains field initializers</param>
    /// <param name="propMembers">The members referenced by properties</param>
    /// <returns>A constructed node containing all the original constructors</returns>
    public static RecordDeclarationSyntax IncludeConstructorIfStruct(this RecordDeclarationSyntax node, SyntaxNode original, bool hasPropertyInitializers, Dictionary<string, MemberDeclarationSyntax> propMembers)
    {
        // Error CS8983 A 'struct' with field initializers must include an explicitly declared constructor.
        var isStruct = node.ClassOrStructKeyword.ValueText.Equals("struct", StringComparison.OrdinalIgnoreCase);
        if (isStruct && hasPropertyInitializers)
        {
            var members = AddConstructorsAndMembers(node, original, propMembers);
            return node.AddMembers(members);
        }
        return node;
    }

    /// <summary>
    /// Include constructors and all the members referenced if it has property initializers
    /// </summary>
    /// <param name="node">The partial node</param>
    /// <param name="original">The original node</param>
    /// <param name="hasPropertyInitializers">True if original contains property initializers</param>
    /// <param name="propMembers">The found property referenced members</param>
    /// <returns>A node with constructors and the referenced members</returns>
    public static StructDeclarationSyntax IncludeConstructorOnInitializer(this StructDeclarationSyntax node, SyntaxNode original, bool hasPropertyInitializers, Dictionary<string, MemberDeclarationSyntax> propMembers)
    {
        if (hasPropertyInitializers)
        {
            var members = AddConstructorsAndMembers(node, original, propMembers);
            return node.AddMembers(members);
        }

        return node;
    }

    /// <summary>
    /// Add the nullable enable directive trivia
    /// </summary>
    /// <param name="node">The partial node</param>
    /// <returns>A partial node with nullable enable directive</returns>
    public static SyntaxNode WithNullableEnableDirective(this SyntaxNode node)
    {
        var nullableDirective = SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword), true);
        var nullableTrivia = SyntaxFactory.Trivia(nullableDirective);
        return node.WithLeadingTrivia(nullableTrivia);
    }

    private static MemberDeclarationSyntax[] AddConstructorsAndMembers(TypeDeclarationSyntax node, SyntaxNode original, Dictionary<string, MemberDeclarationSyntax> propMembers)
    {
        // Find the constructors
        var ctors = original.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        var originalMembers = original.DescendantNodes().OfType<MemberDeclarationSyntax>();
        Dictionary<string, MemberDeclarationSyntax> foundMembers = [];
        List<ConstructorDeclarationSyntax> partialCtors = [];

        foreach (var ctor in ctors)
        {
            var members = ctor.DescendantNodes().OfType<IdentifierNameSyntax>();
            var partialCtor = SyntaxFactory.ConstructorDeclaration(ctor.AttributeLists, ctor.Modifiers, node.Identifier, ctor.ParameterList, ctor.Initializer, ctor.Body, ctor.ExpressionBody, ctor.SemicolonToken);
            partialCtors.Add(partialCtor);
            foreach (var member in members)
            {

                Utilities.GetMembers(original, member, ref foundMembers);
            }
        }

        List<MemberDeclarationSyntax> difference = [];
        foreach (var kv in propMembers)
        {
            if (!foundMembers.ContainsKey(kv.Key))
            {
                difference.Add(kv.Value);
            }
        }

        MemberDeclarationSyntax[] all = [.. difference, .. partialCtors];
        return all;
    }

    /// <summary>
    /// Add partial keyword to the entity
    /// </summary>
    /// <typeparam name="T">The struct, record or class type</typeparam>
    /// <param name="original">The original entity</param>
    /// <returns>A syntaxtoken list with the partial keyword</returns>
    public static SyntaxTokenList AddPartialKeyword<T>(this T original)
        where T : TypeDeclarationSyntax
    {
        List<SyntaxToken> withPartial = [.. original.Modifiers.Where(m => !m.IsKind(SyntaxKind.PartialKeyword)), SyntaxFactory.Token(SyntaxKind.PartialKeyword)];
        return SyntaxFactory.TokenList(withPartial);
    }

    /// <summary>
    /// Determine if the property contains an attribute with the given
    /// fully qualified type.
    /// </summary>
    /// <param name="propertyDeclaration">The property declaration syntax.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="fullyQualifiedTypeName">The fully qualified type name.</param>
    /// <returns>True if an attribute exists with that type name otherwise false.</returns>
    public static bool PropertyHasAttributeWithTypeName(this PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel, string fullyQualifiedTypeName)
    {
        foreach (var attributeList in propertyDeclaration.AttributeLists)
        {
            var attributeResults = attributeList.FilterAttributeByName(semanticModel, (n) => n == fullyQualifiedTypeName);
            return attributeResults.Any();
        }

        return false;
    }

    /// <summary>
    /// Retrieve the actual fully qualified type name for the attribute.
    /// </summary>
    /// <param name="attributes">The attribute list.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="predicate">The predicate filter, takes the fully qualified type name and returns a bool.</param>
    /// <returns>A collection of names.</returns>
    public static IEnumerable<AttributeSyntax> FilterAttributeByName(
        this AttributeListSyntax attributes,
        SemanticModel semanticModel,
        Func<string, bool> predicate)
    {
        foreach (var attribute in attributes.Attributes)
        {
            // Can throw ArgumentException
            // If the "Syntax node is not within the syntax tree".
            // How to properly get the semantic model: https://github.com/dotnet/roslyn/issues/18730#issuecomment-294314178
            var actualSemanticModel = semanticModel.Compilation.GetSemanticModel(attribute.SyntaxTree);
            var info = actualSemanticModel.GetTypeInfo(attribute);
            var type = info.Type;
            if (type is not null)
            {
                var name = type.ToString();
                if (predicate(name))
                {
                    yield return attribute;
                }
            }
        }
    }

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

    /// <summary>
    /// Extract if extra attributes should be included from partial attribute. Default false.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>True if extra attribute annotations should be included otherwise false.</returns>
    public static bool GetIncludeExtraAttributesProperties(this GeneratorAttributeSyntaxContext context)
    {
        var args = context
            .TargetNode
            .DescendantNodes()
            .OfType<AttributeArgumentSyntax>();

        var include = args
            .Where(n => n.Expression.IsKind(SyntaxKind.TrueLiteralExpression))
            .Where(n => string.Equals("IncludeExtraAttributes", n.NameEquals?.Name.Identifier.ValueText, StringComparison.Ordinal))
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
            Utilities.GetMembers(node, member, ref result);
        }

        return result.Any();
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
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="originalSource">The original type</param>
    /// <param name="partialSource">The partial type</param>
    /// <param name="name">The property name to use</param>
    /// <returns>True if has partial reference otherwise false</returns>
    public static bool GetPartialReferenceInfo(this PropertyDeclarationSyntax property, SemanticModel semanticModel, out IdentifierNameSyntax? originalSource, out string? partialSource, out string? name)
    {
        if (!property.AttributeLists.Any())
        {
            // No attributes
            originalSource = null;
            partialSource = null;
            name = null;
            return false;
        }

        var attributeLists = property.AttributeLists;
        AttributeSyntax? attribute = null;

        foreach (var attributeList in attributeLists)
        {
            var attributes = attributeList.FilterAttributeByName(semanticModel, (n) => string.Equals(n, Names.PartialReference));
            if (attributes.Any())
            {
                attribute = attributes.FirstOrDefault();
                break;
            }
        }

        if (attribute is null)
        {
            // No attributes
            originalSource = null;
            partialSource = null;
            name = null;
            return false;
        }

        // Only supported from NET7.0 and onwards with language version C# 11
        var generic = attribute.DescendantNodes().OfType<GenericNameSyntax>().FirstOrDefault();
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
            // Extract type info
            var attrArgs = attribute.DescendantNodes().OfType<TypeOfExpressionSyntax>();
            originalSource = attrArgs.FirstOrDefault()?.Type as IdentifierNameSyntax;
            var second = attrArgs.Skip(1).FirstOrDefault().Type as IdentifierNameSyntax;
            partialSource = second?.Identifier.ValueText;
            name = attribute.DescendantNodes().OfType<LiteralExpressionSyntax>().SingleOrDefault()?.Token.ValueText;
            return originalSource is not null && !string.IsNullOrWhiteSpace(partialSource);
        }
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
