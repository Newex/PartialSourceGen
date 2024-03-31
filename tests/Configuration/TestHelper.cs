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

    public static Target GetSecondResult(this GeneratorDriverRunResult result)
    {
        var source = result.Results[0].GeneratedSources[1].SourceText.ToString();
        var name = result.Results[0].GeneratedSources[1].HintName;
        return new("cs", source, Path.GetFileNameWithoutExtension(name));
    }
}