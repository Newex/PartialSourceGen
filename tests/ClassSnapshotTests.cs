using System.Threading.Tasks;
using PartialSourceGen.Tests.Configuration;
using Xunit.Categories;
using static VerifyXunit.Verifier;

namespace PartialSourceGen.Tests;

/// <summary>
/// Create snapshots!
/// </summary>
[SnapshotTest]
public class ClassSnapshotTests
{
    [Fact]
    public Task Without_summary_or_required_properties()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public class Model
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
        public class Model
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
        public class Model
        {
            public required string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }

    [Fact]
    public Task With_required_nullable_property()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeRequiredProperties = true)]
        public class Model
        {
            public required string? Name { get; set; }
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
        public class Model
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
        public class Model
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
        public class Model<T>
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
        public class Model<T>
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
    public Task With_arrow_property()
    {
        // a.k.a. expression-bodied member
        var source = """
        using System;
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public class Model
        {
            public string Value => "Some value";
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        return Verify(runResult).UseDirectory("Results/Snapshots");
    }
}