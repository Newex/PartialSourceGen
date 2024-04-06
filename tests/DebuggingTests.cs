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
            public class Person
            {
                Person() { }

                [Required]
                [Other, ExcludePartial]
                public string NotMe { get; set; }
            }
            """);

        var result = GeneratorDebugger.RunDebugging([code], [partialGenerator]);
        var generated = result.GeneratedTrees[1].GetText().ToString();
        Debug.WriteLine(generated);
    }
}