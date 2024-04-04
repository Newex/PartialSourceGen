using PartialSourceGen;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Models;

/// <summary>
/// A person
/// </summary>
[Partial(IncludeRequiredProperties = true, Summary = "A partial person")]
public readonly partial record struct Person
{
    public Person()
    {
    }

    [Required]
    public int ID { get; init; } = 123;

    /// <summary>
    /// The first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// The last name
    /// </summary>
    public string? LastName { get; init; }

    public static string Email
    {
        get
        {
            return "MyEmail";
        }
    }
}