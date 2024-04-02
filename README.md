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

public record PartialPerson
{
    public int? ID { get; init; }
    public string? Name { get; init; }
}
```

# Installation

Add nuget package `dotnet add package PartialSourceGen`.

# Why
When you have an API that takes in some model, but you don't need to specify all the properties, you can just use this library.  
OR you can just write the partial model yourself.

The advantage with source generated models is that this will be in-sync with your actual model without requiring you to  
update both the actual model and the partial model every time you make changes to the actual model.


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

## Functionalities

This source generator will do the following:

- [x] If the input class has type constraints for a generic type, with `notnull`. This will be removed in the partial class.
- [x] If the property is marked with a required keyword, or a required attribute. The type will be unchanged.
- [x] Any methods or fields that are referenced from a property will be included in the partial class
- [x] If the input is a struct, and contains property initializers then all the constructors and their references to fields and methods will be included.

# References

* The typescript `Partial` utility type: https://www.typescriptlang.org/docs/handbook/utility-types.html#partialtype
* Learn about incremental source generators: https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
* The cookbook: https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md

# Future improvements / ideas
- [ ] Does this work in a large project? Using `IIncrementalSourceGenerator` should be faster for the IDE? I don't know.
- [ ] Somehow add a custom method to the generated partial entity that can create the actual model with default values for the missing properties.
- [ ] What about conflicting classes or files? Not currently handled
- [ ] Currently does not check if `Required` attribute comes from any particular namespace.