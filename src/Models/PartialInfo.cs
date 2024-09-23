using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PartialSourceGen.Models;

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
    /// <param name="removeAbstract">True if abstract keyword should be removed.</param>
    /// <param name="model">The semantic model.</param>
    /// <param name="root">The syntax root node. The beginning of the source syntax node usually begins with using statements.</param>
    /// <param name="node">The syntax node. The class, struct or record under consideration.</param>
    /// <param name="properties">The enumerated properties in the node.</param>
    public PartialInfo(
        string name,
        string? summary,
        bool includeRequired,
        bool includeAttributes,
        bool removeAbstract,
        SemanticModel model,
        SyntaxNode root,
        SyntaxNode node,
        PropertyDeclarationSyntax[] properties)
    {
        Name = name;
        Summary = summary;
        IncludeRequired = includeRequired;
        IncludeExtraAttributes = includeAttributes;
        RemoveAbstractModifier = removeAbstract;
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

    /// <summary>
    /// Remove abstract keyword.
    /// </summary>
    public bool RemoveAbstractModifier { get; }

    /// <summary>
    /// The semantic model.
    /// </summary>
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
    /// <param name="removeAbstractModifier">The remove abstract keyword toggle.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="root">The root syntax.</param>
    /// <param name="node">The node syntax.</param>
    /// <param name="properties">The list of properties.</param>
    public void Deconstruct(
        out string name,
        out string? summary,
        out bool includeRequired,
        out bool includeExtraAttributes,
        out bool removeAbstractModifier,
        out SemanticModel semanticModel,
        out SyntaxNode root,
        out SyntaxNode node,
        out PropertyDeclarationSyntax[] properties)
    {
        name = Name;
        summary = Summary;
        includeRequired = IncludeRequired;
        includeExtraAttributes = IncludeExtraAttributes;
        removeAbstractModifier = RemoveAbstractModifier;
        semanticModel = SemanticModel;
        root = Root;
        node = Node;
        properties = Properties;
    }
}