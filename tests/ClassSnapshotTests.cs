﻿using System.Runtime.CompilerServices;
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
}