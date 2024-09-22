using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PartialSourceGen.Builders;

/// <summary>
/// Construct partial entity helper methods
/// </summary>
public static class PartialEntityBuilder
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

                Parsers.GetMembers(original, member, ref foundMembers);
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
            var attributeResults = FilterAttributeByName(attributeList, semanticModel, (n) => n == fullyQualifiedTypeName);
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
}
