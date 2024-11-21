using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PartialSourceGen.Generators;
using VerifyTests;

namespace PartialSourceGen.Tests.Configuration;

public static class TestHelper
{
    public static GeneratorDriver GeneratorDriver(string source, params Assembly[] extraAssemblies)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var componentModelReference = MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location);
        var extraReferences = extraAssemblies.Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: [reference, componentModelReference, .. extraReferences]);

        var generator = new PartialIncrementalSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        return driver.RunGenerators(compilation);
    }

    public static GeneratorDriver GeneratorDriver(IEnumerable<string> sources, params MetadataReference[] references)
    {
        List<SyntaxTree> syntaxTrees = [];
        foreach (var source in sources)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            syntaxTrees.Add(syntaxTree);
        }

        var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var componentModelReference = MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: [reference, componentModelReference, .. references]);

        var generator = new PartialIncrementalSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        return driver.RunGenerators(compilation);
    }

    public static MetadataReference ToReferenceFromAssembly<T>()
    {
        var assembly = typeof(T).Assembly;
        return MetadataReference.CreateFromFile(assembly.Location);
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