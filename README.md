![NuGet Version](https://img.shields.io/nuget/v/PartialSourceGen)

# What is it?

This is a package that generates a partial entity from a model that has the attribute `Partial`.

The package is inspired by the typescript generic type `Partial` which converts all the properties of an entity into optional properties.

Example:

Input model: `Person.cs`
```csharp
using System;
using PartialSourceGen;

namespace MySpace;

[Partial]
public record Person
{
    public int ID { get; init; }
    public string Name { get; init; }
}
```

The output: `PartialPerson.g.cs`
```csharp
#nullable enable
using System;
using PartialSourceGen;

namespace MySpace;

public partial record PartialPerson
{
    public int? ID { get; init; }
    public string? Name { get; init; }
}
```

# Installation

Add nuget package `dotnet add package PartialSourceGen` to your project and ensure that the csproj reference the package as an analyzer/source generator by having:

```xml
<ItemGroup>
    <PackageReference Include="PartialSourceGen" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

# Why
When you have an API that takes in some model, but you don't need to specify all the properties, you can just use this library.

OR you can just write the partial model yourself.

The advantage with source generated models is that this will be in-sync with your actual model without requiring you to update both the actual model and the partial model every time you make changes to the actual model.

Example:

```csharp
// web api endpoint
app.MapPost("/update/person", async (PartialPerson updates) =>
{
    // Only the values that are set, in updates have values, the rest are null
    // work: update person
});
```

# Conventions and settings
The generated model can be fine tuned by the `PartialAttribute` parameters.

### Custom summary
The partial entity can have a custom summary by specifying the `Summary` property like so:

```csharp
using PartialSourceGen;

namespace MyNameSpace;

[Partial(Summary = "My custom summary for the partial entity")]
public record Model
{
    public int ID { get; init; }
}
```

### Custom generated entity name
The partial entity can have a custom name by specifying the `PartialClassName` property like so:

```csharp
using PartialSourceGen;

namespace MyNameSpace;

[Partial(PartialClassName = "MyPartialModel")]
public record Model
{
    public int ID { get; init; }
}
```

Be carefull the generated model does not sanitize input, therefore be sure that the name you give is a valid C# object name.

The usage of the generated output will be:

```csharp
MyPartialModel model = new()
{
    ID = 123
};

// Prints: Model ID: 123
Console.WriteLine("Model ID: {0}", model.ID.GetValueOrDefault());
```

### Include required properties
If the model contains properties that are required, they will be made optional by default and nullable.  
If you want to keep the required modifier, you can specify `IncludeRequiredProperties`, like so:

```csharp
using PartialSourceGen;

namespace MyNameSpace;

[Partial(IncludeRequiredProperties = true)]
public record Model
{
    public required int ID { get; init; }
}
```

Then when constructing the partial entity, you must include the required properties!

```csharp
PartialModel model = new()
{
    // Must include ID
    // when initializing PartialModel
    ID = 123
};

// Prints: Model ID: 123
Console.WriteLine("Model ID: {0}", model.ID);
```

**Note:**  
That required properties can be set either via using the keyword `required` or an attribute `Required`. When including properties that are marked as required, the property will not be made nullable. They will retain their original property type, thus if the property was nullable the required property will also be nullable.

### Include attribute annotations
If the model contains properties with attributes you want to include in the partial entity, you can specify `IncludeExtraAttributes` like so:

```csharp
using System.Text.Json.Serialization;
using PartialSourceGen;

namespace MyNameSpace;

[Partial(IncludeExtraAttributes = true)]
public record Model
{
    [JsonPropertyName("id")]
    public int ID { get; init; }
}
```

This will output:

```csharp
public partial record PartialModel
{
    [JsonPropertyName("id")]
    public int? ID { get; init; }
}
```

### Add custom methods to the partial entity
The generated class/struct/record is a partial entity, thus it is possible to just add a method in a separate file.  
The normal constraints and rules apply for partial classes: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods

Example:

```csharp
// Person.cs
using PartialSourceGen;

namespace People;

[Partial]
record Person
{
    int Age { get; }
}
```

Add custom method:

```csharp
// PartialPerson.cs
using System;

namespace Person;

partial record PartialPerson
{
    int AgeInDogYears()
    {
        return Age * 7;
    }
}
```

## Property attributes

| Attribute | Short explanation |
|-----------|-------------------|
| `IncludeInitializer` | Includes property initializer |
| `PartialReference<TOriginal, TPartial>` | Replaces a type for a partial type |
| `ExcludePartial` | Excludes a property |
| `ForceNull` | Force a required property to be nullable |
| `PartialType<TReplacement>` | Replaces a property type with the specified type |


### IncludeInitializer
A property initializer in the partial entity can be included by annotating the property with `IncludeInitializer` attribute.

Example:

Input `Person.cs`
```csharp
[Partial]
public record Person
{
    [IncludeInitializer]
    public string Name { get; set; } = string.Empty;
}
```

Would produce `PartialPerson.g.cs`:

```csharp
public partial record PartialPerson
{
    public string Name { get; set; } = string.Empty;
}
```

The default behaviour is to exclude property initializers. When a property is included with its initializer, the type will be retained if it is non nullable.

### PartialReference
To reference another partial object, add the `PartialReference` attribute to the property.

If using c# 11.0 (dotnet 7.0 or newer) you can use the generic version, like so:

```csharp
// Person.cs
using PartialSourceGen;

namespace MySpace;

[Partial]
public record Person
{
    [PartialReference<Post, PartialPost>("CustomOptionalNameForPosts")]
    public List<Post> Posts { get; set; } = [];
}
```

Which will generate:

```csharp
// PartialPerson.g.cs
using PartialSourceGen;

namespace MySpace;

public partial record PartialPerson
{
    public List<PartialPost>? CustomOptionalNameForPosts { get; set; }
}
```

If no name is included the original name will be used.

If using an older version than C# 11.0, you can use the non-generic attribute version:

```csharp
// Person.cs
using PartialSourceGen;

namespace MySpace;

[Partial]
public record Person
{
    [PartialReference(typeof(Post), typeof(PartialPost))]
    public List<Post> Posts { get; set; } = [];
}
```

### ExcludePartial

To exclude a property from being included in the generated output, annotate the property with `ExcludePartial` attribute.

```csharp
// Input: Person.cs
[Partial]
public record Person
{
    [ExcludePartial]
    public string Name { get; set; } = string.Empty;
}
```
Produces:

```csharp
// Output: PartialPerson.g.cs
public partial record PartialPerson
{
}
```

By default all properties will be included unless specifically excluded.

### ForceNull

This will force a property to be nullable. When a property has the `required` keyword and the class has `IncludeRequiredProperties` set to true - this will override it, and force the property to be nullable.

Or if the property contains an initializer and is marked with `IncludeInitializer`, this will force the property to be nullable again.

```csharp
[Partial(IncludeRequiredProperties = true)]
public record Person
{
    [IncludeInitializer, ForceNull]
    public required string Name { get; set; } = string.Empty;
}
```

Will produce:

```csharp
public partial record PartialPerson
{
    public required string? Name { get; set; } = string.Empty;
}
```

### PartialType

This will replace the property type with the specified type.

If you are on newer C# you can use the generic attribute syntax:

```csharp
// Input: Person.cs
[Partial]
public record Person
{
    [PartialType<object>(name: "NewName")]
    public string Name { get; set; } = string.Empty;
}
```

Will output:

```csharp
// Output: PartialPerson.g.cs
public partial record PartialPerson
{
    public object? NewName { get; set; }
}
```

## Functionalities

This source generator will do the following:

- [x] If the input class has type constraints for a generic type, with `notnull`. This will be removed in the partial class.
- [x] If the property is marked with a required keyword, or a required attribute. The type will be unchanged.
- [x] The type will be retained when including initializer on a non-nullable property.
- [x] Any methods or fields that are referenced from a property will be included in the partial class
- [x] If the input is a struct, and contains property initializers then all the constructors and their references to fields and methods will be included.
- [x] If the model is a partial model, all properties from the partial model will be included.
- [x] If the model uses inheritance, all properties from ancestors will be included (except `System.Object`).

# Warning when using InternalsVisibleTo
If you use the `InternalsVisibleTo` to a project that contains the `PartialSourceGen` library you will get a warning that you have a conflict `CS0436`.

This is because in both projects the source code for the `PartialSourceGen` attributes will be injected, thus creating duplicate versions of the same files.

To fix this you can omit the auto-generation of the attributes by including the following constant in your csproj:

```xml
<!-- File: myproject.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <!-- Excluded other xml elements -->
    <PropertyGroup>
        <DefineConstants>PARTIALSOURCEGEN_EXCLUDE_ATTRIBUTES</DefineConstants>
    </PropertyGroup>
</Project>
```

To add the attributes you can either recreate the attributes yourself, or use the package `PartialSourceGen.Attributes` from nuget.

# References

* The typescript `Partial` utility type: https://www.typescriptlang.org/docs/handbook/utility-types.html#partialtype
* Learn about incremental source generators: https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
* The cookbook: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md

# Future improvements / ideas
- [ ] What about conflicting classes or files? Not currently handled
- [ ] Custom namespace for partial objects?