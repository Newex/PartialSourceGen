using System.Threading.Tasks;
using PartialSourceGen.Tests.Configuration;
using Xunit.Categories;
using static VerifyXunit.Verifier;

namespace PartialSourceGen.Tests;

/// <summary>
/// Create snapshots!
/// </summary>
[SnapshotTest]
public class RecordSnapshotTests
{
    [Fact]
    public Task Without_summary_or_required_properties()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public record Model
        {
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task With_an_inherited_summary()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public record Model
        {
            /// <summary>
            /// The name
            /// </summary>
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task With_required_property()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeRequiredProperties = true)]
        public record Model
        {
            /// <summary>
            /// The name
            /// </summary>
            public required string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
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
        public record Model
        {
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
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
        public record Model
        {
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
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
        public record Model<T>
        {
            public T Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task With_generic_type_parameters_and_type_constraint()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public record Model<T>
        where T : notnull
        {
            public T Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task With_property_default_value_assignments()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public record Model
        {
            public string Name { get; set; } = string.Empty;
            public bool Value { get; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task Struct_with_initializer_must_have_constructor()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public readonly record struct Model
        {
            public Model(string name)
            {
                Name = name;
            }

            public string Name { get; init; } = string.Empty;
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task Exclude_property_initializer()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public record Model
        {
            /// <summary>
            /// The name
            /// </summary>
            [WithoutInitializer]
            public string Name { get; set; } = "John Doe";
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task Struct_with_excluded_initializer_should_not_have_constructor()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public readonly record struct Model
        {
            public Model(string name)
            {
                Name = name;
            }

            [WithoutInitializer]
            public string Name { get; init; } = string.Empty;
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }
}