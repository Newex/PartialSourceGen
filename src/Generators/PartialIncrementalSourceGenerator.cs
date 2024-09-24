using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PartialSourceGen.Constants;
using PartialSourceGen.Helpers;
using PartialSourceGen.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PartialSourceGen.Generators;

/// <summary>
/// An incremental generator for constructing partial entities
/// </summary>
[Generator]
public class PartialIncrementalSourceGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("PartialSourceGenAttributes.g.cs", SourceText.From(SourceCodeText.Disclaimer + SourceCodeText.SourceAttribute, Encoding.UTF8)));

        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "PartialSourceGen.PartialAttribute",
            predicate: static (n, _) => n.IsKind(SyntaxKind.ClassDeclaration)
                                 || n.IsKind(SyntaxKind.StructDeclaration)
                                 || n.IsKind(SyntaxKind.RecordDeclaration)
                                 || n.IsKind(SyntaxKind.RecordStructDeclaration),
            transform: SemanticTransform)
            .Where(static n => n is not null);

        context.RegisterSourceOutput(candidates, static (spc, source) => Execute(in source, spc));
    }

    private PartialInfo? SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        context.Attributes.SingleOrDefault(a => a.NamedArguments.Any(n => n.Key == ""));
        if (context.TargetSymbol is not INamedTypeSymbol nameSymbol)
        {
            return null;
        }

#if DEBUG && INTERCEPT
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            System.Diagnostics.Debugger.Launch();
        }
#endif
        var root = context.SemanticModel.SyntaxTree.GetRoot(token);

        // Code copied from:
        // https://github.com/Newex/PartialSourceGen/issues/14#issue-2307071834
        List<PropertyDeclarationSyntax> props = [];
        for (var currentType = nameSymbol; currentType != null; currentType = currentType.BaseType)
        {
            var newProps = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .SelectMany(s => s.DeclaringSyntaxReferences)
                .Select(s => s.GetSyntax())
                .OfType<PropertyDeclarationSyntax>();
            props.AddRange(newProps);
        }

        var name = nameSymbol.Name;
        var givenName = context.GetPartialClassName();
        var node = context.TargetNode;
        var summary = context.GetSummaryText();
        var includeRequired = context.ConstructorPropertyIsTrue(Names.BooleanProperties.IncludeRequiredProperties);
        var includeExtra = context.ConstructorPropertyIsTrue(Names.BooleanProperties.IncludeExtraAttributes);
        var removeAbstract = context.ConstructorPropertyIsTrue(Names.BooleanProperties.RemoveAbstractModifier);
        return new(
            givenName ?? ("Partial" + name),
            summary,
            includeRequired,
            includeExtra,
            removeAbstract,
            context.SemanticModel,
            root,
            node,
            [.. props]
        );
    }

    private static void Execute(in PartialInfo? source, SourceProductionContext spc)
    {
        if (!source.HasValue)
        {
            return;
        }

        var (name,
             summaryTxt,
             includeRequired,
             includeExtra,
             removeAbstract,
             semanticModel,
             root,
             node,
             originalProps) = source.GetValueOrDefault();
        List<PropertyDeclarationSyntax> optionalProps = [];
        Dictionary<string, MemberDeclarationSyntax> propMembers = [];
        var hasPropertyInitializer = false;

        foreach (var prop in originalProps)
        {
            var hasExcludeAttribute = prop.PropertyHasAttributeWithTypeName(semanticModel, Names.ExcludePartial);
            if (hasExcludeAttribute)
            {
                continue;
            }

            var propName = prop.Identifier.ValueText.Trim();
            var hasIncludeInitializer = prop.PropertyHasAttributeWithTypeName(semanticModel, Names.IncludeInitializer);
            var isExpression = prop.ExpressionBody is not null;
            TypeSyntax propertyType;
            IEnumerable<SyntaxToken> modifiers = prop.Modifiers;
            if (prop.Type is NullableTypeSyntax nts)
            {
                propertyType = nts;
            }
            else
            {
                var keepType = false;
                var hasRequiredAttribute = false;
                var hasRequiredModifier = false;
                if (includeRequired)
                {
                    hasRequiredAttribute = prop.PropertyHasAttributeWithTypeName(semanticModel, "System.ComponentModel.DataAnnotations.RequiredAttribute");
                    hasRequiredModifier = modifiers.Any(m => m.IsKind(SyntaxKind.RequiredKeyword));
                }

                keepType = hasIncludeInitializer || (includeRequired && (hasRequiredModifier || hasRequiredAttribute));
                var forceNull = prop.PropertyHasAttributeWithTypeName(semanticModel, Names.ForceNull);
                var hasNewType = prop.PropertyHasAttributeWithTypeName(semanticModel, Names.PartialType);

                if (hasNewType && hasIncludeInitializer)
                {
                    // TODO: Throw diagnostic error! or warning!
                    // Unless we check that the initializer does not conflict
                    // with the new type.
                    // Since we are lazy, we do not want to do that now :\
                }

                if (hasNewType && (hasRequiredAttribute || hasRequiredModifier))
                {
                    propertyType = prop.ExtractTypeForPartialType(semanticModel, out var customName);
                    propName = customName ?? propName;
                }
                else if (hasNewType)
                {
                    propertyType = SyntaxFactory.NullableType(prop.ExtractTypeForPartialType(semanticModel, out var customName));
                    propName = customName ?? propName;
                }
                else if (!forceNull && keepType)
                {
                    // Retain original type when
                    // 1. User has specified that IncludeRequired is true
                    // 2. has IncludeInitializer with initializer
                    // 3. has Required attribute
                    // 4. has required keyword
                    propertyType = prop.Type;
                }
                else
                {
                    propertyType = SyntaxFactory.NullableType(prop.Type);
                }
            }

            List<AttributeListSyntax> keepAttributes = [];

            if (!includeRequired)
            {
                // Remove the required keyword
                modifiers = modifiers.Where(m => !m.IsKind(SyntaxKind.RequiredKeyword));
            }
            if (removeAbstract)
            {
                modifiers = modifiers.Where(m => !m.IsKind(SyntaxKind.AbstractKeyword));
            }
            if (includeExtra)
            {
                foreach (var attrList in prop.AttributeLists)
                {
                    var newAttributes = new List<AttributeSyntax>();

                    var externalAttributes = attrList.FilterAttributeByName(semanticModel, Utilities.IsNotLocalAttribute);
                    foreach (var attr in externalAttributes)
                    {
                        // Should be kept
                        newAttributes.Add(attr);
                    }

                    // If there are any attributes left, add the new attribute list to keepAttributes
                    if (newAttributes.Any())
                    {
                        var newAttrList = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(newAttributes));
                        keepAttributes.Add(newAttrList);
                    }
                }
            }

            // A candidate for the optional property
            PropertyDeclarationSyntax candidateProp;

            if (!isExpression)
            {
                candidateProp = SyntaxFactory
                        .PropertyDeclaration(propertyType, propName)
                        .WithAttributeLists(SyntaxFactory.List(keepAttributes))
                        .WithModifiers(SyntaxFactory.TokenList(modifiers))
                        .WithAccessorList(prop.AccessorList)
                        .WithLeadingTrivia(prop.GetLeadingTrivia());
            }
            else
            {
                candidateProp = SyntaxFactory
                        .PropertyDeclaration(propertyType, propName)
                        .WithAttributeLists(SyntaxFactory.List(keepAttributes))
                        .WithModifiers(SyntaxFactory.TokenList(modifiers))
                        .WithAccessorList(prop.AccessorList)
                        .WithExpressionBody(prop.ExpressionBody)
                        .WithSemicolonToken(prop.SemicolonToken)
                        .WithLeadingTrivia(prop.GetLeadingTrivia());
            }

            // Get partial reference types
            var hasPartialReference = prop.GetPartialReferenceInfo(semanticModel, out var originalSource, out var partialSource, out var partialRefName);
            if (hasPartialReference)
            {
                var partialRefProp = SyntaxFactory.ParseTypeName(partialSource!);
                candidateProp = candidateProp
                    .ReplaceNodes(candidateProp.DescendantNodes().OfType<IdentifierNameSyntax>(), (n, _) =>
                        n.IsEquivalentTo(originalSource!, topLevel: true)
                            ? partialRefProp
                            : n);

                if (!string.IsNullOrWhiteSpace(partialRefName))
                {
                    candidateProp = candidateProp.WithIdentifier(SyntaxFactory.Identifier(partialRefName!));
                }
            }

            if (hasIncludeInitializer)
            {
                candidateProp = candidateProp
                    .WithInitializer(prop.Initializer)
                    .WithSemicolonToken(prop.SemicolonToken);
            }

            // Get all field and method references
            var hasPropertyMembers = prop.PropertyMemberReferences(node, out var constructPropMembers);
            if (hasPropertyMembers)
            {
                foreach (var propertyMember in constructPropMembers!)
                {
                    propMembers.TryAdd(propertyMember.Key, propertyMember.Value);
                }
            }

            hasPropertyInitializer = hasPropertyInitializer || (prop.Initializer is not null && hasIncludeInitializer);
            optionalProps.Add(candidateProp);
        }

        List<MemberDeclarationSyntax> members = [.. optionalProps];

        if (propMembers.Any())
        {
            members.AddRange(propMembers.Values);
        }

        // Sort members
        members = [.. members.OrderBy(declaration =>
        {
            if (declaration is FieldDeclarationSyntax)
                return 0; // Field comes first
            else if (declaration is PropertyDeclarationSyntax)
                return 1; // Property comes second
            else if (declaration is MethodDeclarationSyntax)
                return 2; // Method comes third
            else
                return 3; // Other member types can be handled accordingly
        })];

        var excludeNotNullConstraint = node.DescendantNodes().OfType<TypeParameterConstraintClauseSyntax>().Where(cs => cs.Constraints.Any(c => c.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => !n.Identifier.ValueText.Equals("notnull"))));

        var derivedTypeSyntax = node.GetDerivedFrom();

        SyntaxNode? partialType = node switch
        {
            RecordDeclarationSyntax record => SyntaxFactory
                .RecordDeclaration(record.Kind(), record.Keyword, name)
                .WithDerived(derivedTypeSyntax)
                .WithClassOrStructKeyword(record.ClassOrStructKeyword)
                .WithModifiers(record.AddPartialKeyword().ToggleAbstractModifier(removeAbstract))
                .WithConstraintClauses(SyntaxFactory.List(excludeNotNullConstraint))
                .WithTypeParameterList(record.TypeParameterList)
                .WithOpenBraceToken(record.OpenBraceToken)
                .IncludeConstructorIfStruct(record, hasPropertyInitializer, propMembers)
                .AddMembers([.. members])
                .WithCloseBraceToken(record.CloseBraceToken)
                .WithSummary(record, summaryTxt),
            StructDeclarationSyntax val => SyntaxFactory
                .StructDeclaration(name)
                .WithDerived(derivedTypeSyntax)
                .WithModifiers(val.AddPartialKeyword().ToggleAbstractModifier(removeAbstract))
                .WithTypeParameterList(val.TypeParameterList)
                .WithConstraintClauses(SyntaxFactory.List(excludeNotNullConstraint))
                .WithOpenBraceToken(val.OpenBraceToken)
                .IncludeConstructorOnInitializer(val, hasPropertyInitializer, propMembers)
                .AddMembers([.. members])
                .WithCloseBraceToken(val.CloseBraceToken)
                .WithSummary(val, summaryTxt),
            ClassDeclarationSyntax val => SyntaxFactory
                .ClassDeclaration(name)
                .WithDerived(derivedTypeSyntax)
                .WithModifiers(val.AddPartialKeyword().ToggleAbstractModifier(removeAbstract))
                .WithTypeParameterList(val.TypeParameterList)
                .WithConstraintClauses(SyntaxFactory.List(excludeNotNullConstraint))
                .WithOpenBraceToken(val.OpenBraceToken)
                .AddMembers([.. members])
                .WithCloseBraceToken(val.CloseBraceToken)
                .WithSummary(val, summaryTxt),
            _ => null
        };

        if (partialType is null)
        {
            return;
        }

        var newRoot = root
            .ReplaceNode(node, partialType)
            .WithNullableEnableDirective()
            .FilterOutEntitiesExcept(partialType)
            .NormalizeWhitespace();

        var newTree = SyntaxFactory.SyntaxTree(newRoot, root.SyntaxTree.Options);
        var sourceText = newTree.GetText().ToString();

        spc.AddSource(name + ".g.cs", SourceCodeText.Disclaimer + sourceText);
    }
}