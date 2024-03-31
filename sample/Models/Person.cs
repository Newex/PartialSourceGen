using PartialSourceGen;

namespace Sample.Models;

/// <summary>
/// A person
/// </summary>
[Partial(IncludeRequiredProperties = false, Summary = "A partial person")]
public readonly record struct Person
{
    public Person()
    {
    }

    public required int ID { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string? LastName { get; init; }
}