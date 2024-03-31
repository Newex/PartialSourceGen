using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PartialSourceGen.Tests.Configuration;

internal static class GeneratorDebugger
{
    internal static GeneratorDriverRunResult RunDebugging(IEnumerable<SyntaxTree> sourceCode, IIncrementalGenerator[] generators)
    {
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var inputCompilation = CSharpCompilation.Create("compilationAssembly", sourceCode, null, compilationOptions);
        var driver = (GeneratorDriver)CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out _, out _);
        return driver.GetRunResult();
    }
}