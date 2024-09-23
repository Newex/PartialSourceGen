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
public class ClassSnapshotTests
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
        ///    public class Model
        /// </summary>
        [Partial(PartialClassName = "ModelPartialCustom")]
        public class Model
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
        ///    public class Model
        /// </summary>
        [Partial(Summary = "Custom summary")]
        public class Model
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
        public class Model
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Only_create_one_source_per_class()
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
            /// input:
            ///    public string Name { get; set; } = "John Doe";
            /// </summary>
            public string Name { get; set; } = "John Doe";
        }

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public class AnotherModel
        {
            /// <summary>
            /// Do not include
            /// </summary>
            public string AnotherName { get; set; } = "John Doe";
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialReference_attribute_in_a_nested_type_using_generic_attribute()
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
            /// input:
            ///    [PartialReference<Post, PartialPost>]
            ///    public List<Post> Name { get; set; }
            /// </summary>
            [PartialReference<Post, PartialPost>]
            public List<Post> Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialReference_attribute_using_generic_attribute()
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
            /// input:
            ///    [PartialReference<Post, PartialPost>]
            ///    public Post Name { get; set; }
            /// </summary>
            [PartialReference<Post, PartialPost>]
            public Post Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialReference_attribute_using_normal_attribute()
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
            /// input:
            ///    [PartialReference(typeof(Post), typeof(PartialPost))]
            ///    public Post Name { get; set; }
            /// </summary>
            [PartialReference(typeof(Post), typeof(PartialPost))]
            public Post Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialReference_attribute_using_normal_attribute_with_custom_name()
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
            /// input:
            ///    [PartialReference(typeof(Post), typeof(PartialPost), "PartialPost")]
            ///    public Post Name { get; set; }
            /// </summary>
            [PartialReference(typeof(Post), typeof(PartialPost), "PartialPost")]
            public Post Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
        public class Model
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
            private string? other;

            /// <summary>
            /// input:
            ///    public string? Value => other;
            /// </summary>
            public string? Value => other;

            /// <summary>
            /// input:
            ///    public string NonNullValue => "Value";
            /// </summary>
            public string NonNullValue => "Value";
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task With_generic_type_parameters_and_type_constraint()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// Class:
        ///    public class Model<T>
        ///    where T : notnull
        /// </summary>
        [Partial]
        public class Model<T>
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task With_nested_field_references()
    {
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
            private string? other;
            private string? firstLevel;
            private string secondLevel = "DefaultValue";

            public string Value
            {
                get
                {
                    if (other is not null)
                        return other;

                    return First();
                }
            }

            private string? First()
            {
                if (firstLevel is not null)
                    return firstLevel;

                return Second();
            }

            private string Second()
            {
                return secondLevel;
            }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
            /// <summary>
            /// IncludeRequiredProperties = true
            /// input:
            ///    public required string? Name { get; set; }
            /// </summary>
            public required string? Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Without_summary_or_required_properties()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public class Model
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Partial_class_should_have_source_generation()
    {
        var source = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public partial class Model
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
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Partial_class_should_have_all_members_generated()
    {
        var file1 = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public partial class Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            public string Name { get; set; }
        }
        """;

        var file2 = """
        namespace MySpace;

        public partial class Model
        {
            /// <summary>
            /// input:
            ///    public int Id { get; set; }
            /// </summary>
            public int Id { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver([file1, file2])
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Inherited_partial_should_include_members()
    {
        var file1 = """
        using PartialSourceGen;

        namespace MySpace;

        [Partial]
        public class Model : Entity
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            public string Name { get; set; }
        }
        """;

        var file2 = """
        namespace MySpace;

        public class Entity : Base
        {
            /// <summary>
            /// input:
            ///    public int Id { get; set; }
            /// </summary>
            public int Id { get; set; }
        }

        public class Base
        {
            /// <summary>
            /// From Base-class
            /// </summary>
            public Guid CommonId { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver([file1, file2])
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Should_include_extra_attributes_when_specified()
    {
        var source = """
        using System.Text.Json.Serialization;
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeExtraAttributes = true)]
        public class Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            [IncludeInitializer]
            [JsonPropertyName("myName")]
            public string Name { get; set; } = "John Doe";
        }
        """;

        var runResult = TestHelper.GeneratorDriver([source],
            extraAssemblies: typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Should_include_extra_attributes_from_inherited_classes()
    {
        var file1 = """
        using System.Text.Json.Serialization;
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeExtraAttributes = true)]
        public class Model : Entity
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            [IncludeInitializer]
            [JsonPropertyName("myName")]
            public string Name { get; set; } = "John Doe";
        }
        """;

        var file2 = """
        using System.Text.Json.Serialization;

        namespace MySpace;

        public class Entity : Base
        {
            /// <summary>
            /// input:
            ///    public int Id { get; set; }
            /// </summary>
            [JsonPropertyName("id")]
            public int Id { get; set; }
        }

        public class Base
        {
            /// <summary>
            /// From Base-class
            /// </summary>
            [JsonPropertyName("commonId")]
            public Guid CommonId { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver([file1, file2],
            extraAssemblies: typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Should_include_extra_attributes_from_partial_files()
    {
        var file1 = """
        using System.Text.Json.Serialization;
        using PartialSourceGen;

        namespace MySpace;

        [Partial(IncludeExtraAttributes = true)]
        public partial class Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            [IncludeInitializer]
            [JsonPropertyName("myName")]
            public string Name { get; set; } = "John Doe";
        }
        """;

        var file2 = """
        using System.Text.Json.Serialization;
        namespace MySpace;

        public partial class Model
        {
            /// <summary>
            /// input:
            ///    public int Id { get; set; }
            /// </summary>
            [JsonPropertyName("id")]
            public int Id { get; set; }
        }

        public partial class Model
        {
            /// <summary>
            /// From Base-class
            /// </summary>
            [JsonPropertyName("commonId")]
            public Guid CommonId { get; set; }
        }
        """;

        var jsonTextAssembly = typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly;
        var runResult = TestHelper.GeneratorDriver([file1, file2], jsonTextAssembly)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task Aliased_PartialSourceGen_attribute_should_not_be_included()
    {
        var source = """
        using System.Text.Json.Serialization;
        using PartialSourceGen;
        using TryBypass = PartialSourceGen.IncludeInitializerAttribute;

        namespace MySpace;

        [Partial(IncludeExtraAttributes = true)]
        public class Model
        {
            /// <summary>
            /// input:
            ///    public string Name { get; set; }
            /// </summary>
            [TryBypass]
            [JsonPropertyName("myName")]
            public string Name { get; set; } = "John Doe";
        }
        """;

        var runResult = TestHelper.GeneratorDriver([source],
            extraAssemblies: typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialType_attribute_using_generic_attribute()
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
            /// input:
            ///    [PartialType<string>]
            ///    public Post Name { get; set; }
            /// </summary>
            [PartialType<string>]
            public Post Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialType_attribute_using_non_generic_attribute()
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
            /// input:
            ///    [PartialType(typeof(string))]
            ///    public Post Name { get; set; }
            /// </summary>
            [PartialType(typeof(string))]
            public Post Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }

    [Fact]
    public Task PartialType_attribute_with_generic_replacement()
    {
        var source = """
        using System.Collections.Generic;
        using PartialSourceGen;

        namespace MySpace;

        /// <summary>
        /// An entity model
        /// </summary>
        [Partial]
        public class Model
        {
            /// <summary>
            /// input:
            ///    [PartialType<List<string>>]
            ///    public Post Name { get; set; }
            /// </summary>
            [PartialType<List<string>>]
            public Post Name { get; set; }
        }
        """;

        var runResult = TestHelper.GeneratorDriver(source)
                                  .GetRunResult()
                                  .GetSecondResult();
        var settings = Settings();
        return Verify(runResult, settings).UseDirectory("Results/Classes");
    }
}