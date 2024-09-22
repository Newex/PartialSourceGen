using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PartialSourceGen.Builders;
using PartialSourceGen.Constants;
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
    private static readonly List<string> PartialAttributeNamesArray = [
        "PartialSourceGen.IncludeInitializerAttribute",
        "PartialSourceGen.PartialReferenceAttribute",
        "PartialSourceGen.ExcludePartialAttribute",
        "PartialSourceGen.ForceNullAttribute",
        "PartialSourceGen.PartialTypeAttribute"
    ];

    private static bool IsNotLocalAttribute(string input) =>
        !(string.Equals(input, PartialAttributeNamesArray[0])
        || string.Equals(input, PartialAttributeNamesArray[1])
        || string.Equals(input, PartialAttributeNamesArray[2])
        || string.Equals(input, PartialAttributeNamesArray[3]));

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
        var includeRequired = context.GetIncludeRequiredProperties();
        var includeExtra = context.GetIncludeExtraAttributesProperties();
        return new(
            givenName ?? ("Partial" + name),
            summary,
            includeRequired,
            includeExtra,
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
             semanticModel,
             root,
             node,
             originalProps) = source.GetValueOrDefault();
        List<PropertyDeclarationSyntax> optionalProps = [];
        Dictionary<string, MemberDeclarationSyntax> propMembers = [];
        var hasPropertyInitializer = false;

        foreach (var prop in originalProps)
        {
            var hasExcludeAttribute = prop.PropertyHasAttributeWithTypeName(semanticModel, PartialAttributeNamesArray[2]);
            if (hasExcludeAttribute)
            {
                continue;
            }

            var hasIncludeInitializer = prop.PropertyHasAttributeWithTypeName(semanticModel, PartialAttributeNamesArray[0]);
            var isExpression = prop.ExpressionBody is not null;
            TypeSyntax propertyType;
            if (prop.Type is NullableTypeSyntax nts)
            {
                propertyType = nts;
            }
            else
            {
                var hasRequiredAttribute = prop.PropertyHasAttributeWithTypeName(semanticModel, "System.ComponentModel.DataAnnotations.RequiredAttribute");
                var hasRequiredModifier = prop.Modifiers.Any(m => m.IsKind(SyntaxKind.RequiredKeyword));
                var keepType = hasRequiredModifier || hasRequiredAttribute || hasIncludeInitializer;
                var forceNull = prop.PropertyHasAttributeWithTypeName(semanticModel, PartialAttributeNamesArray[3]);

                if (keepType && !forceNull)
                {
                    // Retain original type when
                    // 1. has Required attribute
                    // 2. has required keyword
                    // 3. has IncludeInitializer with initializer
                    propertyType = prop.Type;
                }
                else
                {
                    propertyType = SyntaxFactory.NullableType(prop.Type);
                }
            }

            var propName = prop.Identifier.ValueText.Trim();
            IEnumerable<SyntaxToken> modifiers = prop.Modifiers;
            List<AttributeListSyntax> keepAttributes = [];

            if (!includeRequired)
            {
                // Remove the required keyword
                modifiers = prop.Modifiers.Where(m => !m.IsKind(SyntaxKind.RequiredKeyword));
            }
            if (includeExtra)
            {
                foreach (var attrList in prop.AttributeLists)
                {
                    var newAttributes = new List<AttributeSyntax>();

                    var externalAttributes = attrList.FilterAttributeByName(semanticModel, IsNotLocalAttribute);
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
            var hasPartialReference = prop.GetPartialReferenceInfo(out var originalSource, out var partialSource, out var partialRefName);
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

        SyntaxNode? partialType = node switch
        {
            RecordDeclarationSyntax record => SyntaxFactory
                .RecordDeclaration(record.Kind(), record.Keyword, name)
                .WithClassOrStructKeyword(record.ClassOrStructKeyword)
                .WithModifiers(record.AddPartialKeyword())
                .WithConstraintClauses(SyntaxFactory.List(excludeNotNullConstraint))
                .WithTypeParameterList(record.TypeParameterList)
                .WithOpenBraceToken(record.OpenBraceToken)
                .IncludeConstructorIfStruct(record, hasPropertyInitializer, propMembers)
                .AddMembers([.. members])
                .WithCloseBraceToken(record.CloseBraceToken)
                .WithSummary(record, summaryTxt),
            StructDeclarationSyntax val => SyntaxFactory
                .StructDeclaration(name)
                .WithModifiers(val.AddPartialKeyword())
                .WithTypeParameterList(val.TypeParameterList)
                .WithConstraintClauses(SyntaxFactory.List(excludeNotNullConstraint))
                .WithOpenBraceToken(val.OpenBraceToken)
                .IncludeConstructorOnInitializer(val, hasPropertyInitializer, propMembers)
                .AddMembers([.. members])
                .WithCloseBraceToken(val.CloseBraceToken)
                .WithSummary(val, summaryTxt),
            ClassDeclarationSyntax val => SyntaxFactory
                .ClassDeclaration(name)
                .WithModifiers(val.AddPartialKeyword())
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

/// <summary>
/// Model for the execution pipeline.
/// </summary>
internal readonly record struct PartialInfo
{
    /// <summary>
    /// Initialize a <see cref="PartialInfo"/>.
    /// </summary>
    /// <param name="name">The type name for the partial entity.</param>
    /// <param name="summary">The custom summary for the partial entity.</param>
    /// <param name="includeRequired">True if required should be included.</param>
    /// <param name="includeAttributes">True if extra attributes should be copied.</param>
    /// <param name="model">The semantic model.</param>
    /// <param name="root">The syntax root node. The beginning of the source syntax node usually begins with using statements.</param>
    /// <param name="node">The syntax node. The class, struct or record under consideration.</param>
    /// <param name="properties">The enumerated properties in the node.</param>
    public PartialInfo(
        string name,
        string? summary,
        bool includeRequired,
        bool includeAttributes,
        SemanticModel model,
        SyntaxNode root,
        SyntaxNode node,
        PropertyDeclarationSyntax[] properties)
    {
        Name = name;
        Summary = summary;
        IncludeRequired = includeRequired;
        IncludeExtraAttributes = includeAttributes;
        SemanticModel = model;
        Root = root;
        Node = node;
        Properties = properties;
    }

    /// <summary>
    /// The partial entity type name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The partial entity summary.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// True if required properties should be included as required instead of
    /// being made optional.
    /// </summary>
    public bool IncludeRequired { get; }

    /// <summary>
    /// Include extra attributes in the source that have been annotated.
    /// By copying the attributes.
    /// </summary>
    public bool IncludeExtraAttributes { get; }

    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// The root of the syntax, usually begins with using statements.
    /// </summary>
    public SyntaxNode Root { get; }

    /// <summary>
    /// The node under consideration. Can be either class, struct or record.
    /// </summary>
    public SyntaxNode Node { get; }

    /// <summary>
    /// The list of original properties found, including from inherited parents.
    /// </summary>
    public PropertyDeclarationSyntax[] Properties { get; }

    /// <summary>
    /// Deconstructor.
    /// </summary>
    /// <param name="name">The partial entity name.</param>
    /// <param name="summary">The partial entity summary.</param>
    /// <param name="includeRequired">The required toggle.</param>
    /// <param name="includeExtraAttributes">The extra attributes toggle.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="root">The root syntax.</param>
    /// <param name="node">The node syntax.</param>
    /// <param name="properties">The list of properties.</param>
    public void Deconstruct(
        out string name,
        out string? summary,
        out bool includeRequired,
        out bool includeExtraAttributes,
        out SemanticModel semanticModel,
        out SyntaxNode root,
        out SyntaxNode node,
        out PropertyDeclarationSyntax[] properties)
    {
        name = Name;
        summary = Summary;
        includeRequired = IncludeRequired;
        includeExtraAttributes = IncludeExtraAttributes;
        semanticModel = SemanticModel;
        root = Root;
        node = Node;
        properties = Properties;
    }
}