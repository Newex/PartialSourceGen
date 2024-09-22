using System.Collections.Generic;

namespace PartialSourceGen.Constants;

/// <summary>
/// The full qualified names of external property attributes.
/// </summary>
public static class Names
{
    /// <summary>
    /// The collection of all attribute names.
    /// </summary>
    public static readonly List<string> AllAttributes = [
        IncludeInitializer,
        PartialReference,
        ExcludePartial,
        ForceNull,
        PartialType
    ];

    /// <summary>
    /// The include initializer attribute name.
    /// </summary>
    public const string IncludeInitializer = "PartialSourceGen.IncludeInitializerAttribute";

    /// <summary>
    /// The partial reference attribute name.
    /// </summary>
    public const string PartialReference = "PartialSourceGen.PartialReferenceAttribute";

    /// <summary>
    /// The exclude partial attribute name.
    /// </summary>
    public const string ExcludePartial = "PartialSourceGen.ExcludePartialAttribute";

    /// <summary>
    /// The force null attribute name.
    /// </summary>
    public const string ForceNull = "PartialSourceGen.ForceNullAttribute";

    /// <summary>
    /// The partial type attribute name.
    /// </summary>
    public const string PartialType = "PartialSourceGen.PartialTypeAttribute";
}