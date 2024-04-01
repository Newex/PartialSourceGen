using Microsoft.CodeAnalysis.CSharp;
using PartialSourceGen.Generators;
using PartialSourceGen.Tests.Configuration;
using System.Diagnostics;
using Xunit.Categories;

namespace PartialSourceGen.Tests;

[Exploratory]
public class DebuggingTests
{
    [Fact]
    public void DebugMarkerGenerator()
    {
        var markerGenerator = new PartialIncrementalSourceGenerator();
        var result = GeneratorDebugger.RunDebugging([], [markerGenerator]);
        Debug.WriteLine(result.GeneratedTrees.Length);
    }

    [Fact]
    public void DebugPartialGenerator()
    {
        var partialGenerator = new PartialIncrementalSourceGenerator();
        var code = CSharpSyntaxTree.ParseText("""
            using System;
            using System.ComponentModel.DataAnnotations;
            using PartialSourceGen;

            namespace Sample.Models;

            /// <summary>
            /// A person
            /// </summary>
            [Obsolete, Partial(IncludeRequiredProperties = true]
            public class Person<T>
            where T : notnull
            {
                Person() { }

                /// <summary>
                /// The Person ID
                /// Newline xml comment
                /// </summary>
                [Required]
                public int ID { get; init; }

                /// <summary>
                /// The first name
                /// </summary>
                public string FirstName { get; init; } = string.Empty;
                public string? LastName { get; init; }

                public void SomeMethod()
                {
                }
            }
            """);

        var result = GeneratorDebugger.RunDebugging([code], [partialGenerator]);
        var generated = result.GeneratedTrees[1].GetText().ToString();
        Debug.WriteLine(generated);
    }
}