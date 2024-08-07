﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PartialSourceGen.Generators;
using VerifyTests;

namespace PartialSourceGen.Tests.Configuration;

public static class TestHelper
{
    public static GeneratorDriver GeneratorDriver(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: [reference]);

        var generator = new PartialIncrementalSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        return driver.RunGenerators(compilation);
    }

    public static GeneratorDriver GeneratorDriver(IEnumerable<string> sources)
    {
        List<SyntaxTree> syntaxTrees = [];
        foreach (var source in sources)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            syntaxTrees.Add(syntaxTree);
        }

        var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: [reference]);

        var generator = new PartialIncrementalSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        return driver.RunGenerators(compilation);
    }

    public static Target GetSecondResult(this GeneratorDriverRunResult result)
    {
        // Usually the first result will be the PartialAttribute.g.cs code.
        // The 2nd result will be the tested generated code.
        var source = result.Results[0].GeneratedSources[1].SourceText.ToString();
        var name = result.Results[0].GeneratedSources[1].HintName;
        return new("cs", source, Path.GetFileNameWithoutExtension(name));
    }

    public static Target GetThirdResult(this GeneratorDriverRunResult result)
    {
        // Usually the first result will be the PartialAttribute.g.cs code.
        var source = result.Results[0].GeneratedSources[2].SourceText.ToString();
        var name = result.Results[0].GeneratedSources[2].HintName;
        return new("cs", source, Path.GetFileNameWithoutExtension(name));
    }
}