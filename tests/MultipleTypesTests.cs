using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit.Categories;
using VerifyTests;
using static VerifyXunit.Verifier;
using PartialSourceGen.Tests.Configuration;

namespace PartialSourceGen.Tests;

[SnapshotTest]
public class MultipleTypesTests
{
    private static VerifySettings Settings([CallerMemberName] string method = "")
    {
        var settings = new VerifySettings();
        settings.UseFileName(method);
        return settings;
    }

    [Fact]
    public Task Single_source_with_enum_first_should_parse_all_partial()
    {
        /*
         All partial attributes in the same source, should
         be parsed.
         Earlier bug, only checked the first type, in the source
         then skipped the rest.
        */
        var source = """
        using PartialSourceGen;

        public enum First
        {
            Zero
        }

        [Partial]
        public record Second
        {
            public object Property { get; set; } = null!;
        }

        [Partial]
        public record Third
        {
            public object Property { get; set; } = null!;
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult();
        var second = runResult.GetSecondResult();
        var third = runResult.GetThirdResult();
        var settings = Settings();
        return Verify([second, third], settings).UseDirectory("Results/Mixed");
    }
}
