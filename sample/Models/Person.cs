﻿using PartialSourceGen;
// using Important = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace Sample.Models;

/// <summary>
/// A person
/// </summary>
/// <typeparam name="T">The type</typeparam>
[Partial(Summary = "A partial person")]
public readonly partial record struct Person<T>
{
    public Person()
    {
    }

    // [Important]
    public required int ID { get; init; } = 123;

    /// <summary>
    /// The first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// The last name
    /// </summary>
    public string? LastName { get; init; }

    [PartialType<string>]
    public Post Post { get; init; }
}

[Partial]
public record struct Post
{
    public int MyProperty { get; set; }
    public int AnotherOne { get; set; }
}
