using System.Runtime.CompilerServices;
using VerifyTests;

namespace PartialSourceGen.Tests.Configuration;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}