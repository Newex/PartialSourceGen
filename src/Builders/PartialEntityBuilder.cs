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
            // Find the constructors
            var ctors = original.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
            var originalMembers = original.DescendantNodes().OfType<MemberDeclarationSyntax>();
            Dictionary<string, MemberDeclarationSyntax> foundMembers = [];

            foreach (var ctor in ctors)
            {
                var members = ctor.DescendantNodes().OfType<IdentifierNameSyntax>();

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

            MemberDeclarationSyntax[] all = [.. difference,.. ctors];
            return node.AddMembers(all);
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
            // Find the constructors
            var ctors = original.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
            var originalMembers = original.DescendantNodes().OfType<MemberDeclarationSyntax>();
            Dictionary<string, MemberDeclarationSyntax> foundMembers = [];

            foreach (var ctor in ctors)
            {
                var members = ctor.DescendantNodes().OfType<IdentifierNameSyntax>();

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

            MemberDeclarationSyntax[] all = [.. difference,.. ctors];
            return node.AddMembers(all);
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
}
