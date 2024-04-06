using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PartialSourceGen.Tests.Configuration;
using VerifyTests;
using Xunit.Categories;
using static VerifyXunit.Verifier;

namespace PartialSourceGen.Tests;

/// <summary>
/// Create snapshots!
/// </summary>
[SnapshotTest]
public class StructSnapshotTests
{
    private static VerifySettings Settings([CallerMemberName] string method = "")
    {
        var settings = new VerifySettings();
        settings.UseFileName(method);
        return settings;
    }

    [Fact]
    public Task Custom_class_name()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial(PartialClassName = "ModelPartialCustom")]
        public struct Model
        {
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task Custom_summary()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial(Summary = "Custom summary")]
        public struct Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task Exclude_properties()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// input property:
        ///    [ExcludePartial]
        ///    public string Name { get; set; } = "John Doe";
        /// </summary>
        [Partial]
        public struct Model
        {
            /// <summary>
            /// DO NOT INCLUDE
            /// </summary>
            [ExcludePartial]
            public string Name { get; set; } = "John Doe";

            /// <summary>
            /// Only 1 property out of 2 originals
            /// </summary>
            public int ID { get; init; } = 0;
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task Include_property_initializer()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public struct Model
        {
            /// <summary>
            /// input:
            ///    [IncludeInitializer]
            ///    public string Name { get; set; } = "John Doe";
            /// </summary>
            [IncludeInitializer]
            public string Name { get; set; } = "John Doe";
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task With_an_inherited_summary()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// The inherited summary
        /// </summary>
        [Partial]
        public struct Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task With_generic_type_parameters()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public struct Model<T>
        {
            /// <summary>
            /// input:
            ///    public T Name { get; set; }
            /// </summary
            public T Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task With_generic_type_parameters_and_type_constraint()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// Class:
        ///    public struct Model<T>
        ///    where T : notnull
        /// </summary>
        [Partial]
        public struct Model<T>
        where T : notnull
        {
            /// <summary>
            /// input:
            ///    public T Name { get; set; }
            /// </summary
            public T Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task With_required_property()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeRequiredProperties = true)]
        public struct Model
        {
            /// <summary>
            /// IncludeRequiredProperties = true
            /// input:
            ///    public required string Name { get; set; }
            /// </summary
            public required string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }

    [Fact]
    public Task Without_summary_or_required_properties()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public struct Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Structs");
    }
}