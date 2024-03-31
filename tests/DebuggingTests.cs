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
            using PartialSourceGen;

            namespace Sample.Models;

            /// <summary>
            /// A person
            /// </summary>
            [Obsolete, Partial(IncludeRequiredProperties = false]
            public class Person<T>
            where T : notnull
            {
                Person() { }

                /// <summary>
                /// The Person ID
                /// </summary>
                public required int ID { get; init; }
                public string FirstName { get; init; } = string.Empty;
                public string? LastName { get; init; }

                public void SomeMethod()
                {
                }
            }
            """);

        var result = GeneratorDebugger.RunDebugging([code], [partialGenerator]);
        Debug.WriteLine(result.GeneratedTrees.Length);
    }
}