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
public class RecordSnapshotTests
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
        /// Class:
        ///    [Partial(PartialClassName = "ModelPartialCustom")]
        ///    public record Model
        /// </summary>
        [Partial(PartialClassName = "ModelPartialCustom")]
        public record Model
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
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task Custom_summary()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// Class:
        ///    [Partial(Summary = "Custom summary")]
        ///    public record Model
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
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task Force_a_required_property_to_be_nullable()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeRequiredProperties = true)]
        public record Model
        {
            /// <summary>
            /// input:
            ///    [ForceNull]
            ///    public required string Name { get; set; }
            /// </summary>
            [ForceNull]
            public required string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
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
        public record Model
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
        return Verify(runResult, settings).UseDirectory("Results/Records");
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
            /// <summary>
            /// Must include ctor
            /// </summary>
            public Model(string name)
            {
                Name = name;
            }

            /// <summary>
            /// input:
            ///    [IncludeInitializer]
            ///    public string Name { get; init; } = string.Empty;
            /// </summary>
            [IncludeInitializer]
            public string Name { get; init; } = string.Empty;
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task Struct_without_any_initializers_should_not_have_constructor()
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
            /// <summary>
            /// Do not include ctor
            /// </summary>
            public Model(string name)
            {
                Name = name;
            }

            /// <summary>
            /// input:
            ///    public string Name { get; init; } = string.Empty;
            /// </summary>
            public string Name { get; init; } = string.Empty;
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
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
        public record Model
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
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task With_generic_type_parameters()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// Class:
        ///    public record Model<T>
        /// </summary>
        [Partial]
        public record Model<T>
        {
            /// <summary>
            /// input:
            ///    public T Name { get; set; }
            /// </summary>
            public T Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task With_generic_type_parameters_and_type_constraint()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// Class:
        ///    public record Model<T>
        ///    where T : notnull
        /// </summary>
        [Partial]
        public record Model<T>
        where T : notnull
        {
            /// <summary>
            /// input:
            ///    public T Name { get; set; }
            /// </summary>
            public T Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
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
            /// IncludeRequiredProperties = true
            /// input:
            ///    public required string Name { get; set; }
            /// </summary>
            public required string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task Without_summary_or_required_properties()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public record Model
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
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task With_required_attribute()
    {
        var source = """
        using PartialSourceGen;
        using System.ComponentModel.DataAnnotations;

        namespace MySpace;

        [Partial(IncludeRequiredProperties = true)]
        public record Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            ///
            /// output:
            ///    public string Name { get; set; }
            /// </summary>
            [Required]
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }

    [Fact]
    public Task With_aliased_required_attribute()
    {
        var source = """
        using PartialSourceGen;
        using Important = System.ComponentModel.DataAnnotations.RequiredAttribute;

        namespace MySpace;

        [Partial(IncludeRequiredProperties = true)]
        public record Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            ///
            /// output:
            ///    public string Name { get; set; }
            /// </summary>
            [Important]
            public string Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Records");
    }
}