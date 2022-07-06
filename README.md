# FsSpec

[![CI Build](https://github.com/farlee2121/FsSpec/actions/workflows/ci.yml/badge.svg)](https://github.com/farlee2121/FsSpec/actions/workflows/ci.yml)
[![Nuget (with prereleases)](https://img.shields.io/nuget/v/fsspec?label=NuGet%3A%20FsSpec)](https://www.nuget.org/packages/fsspec)


## What is FsSpec and why would you use it?

### Short Motivation
FsSpec represents value constraints as data to reuse one constraint declaration for validation, data generation, error explanation, and more.

It also makes for a concise and consistent Type-Driven approach
```fsharp
open FsSpec
type InventoryCount = private InventoryCount of int
module InventoryCount = 
    let spec = Spec.all [Spec.min 0; Spec.max 1000]
    let tryCreate n =
      Spec.validate spec n 
      |> Result.map InventoryCount

// Generate data
let inventoryAmounts = Gen.fromSpec InventoryCount.spec |> Gen.sample 0 10
```

### Longer Motivation
Type-Driven and/or Domain-Driven systems commonly model data types with constraints. For example, 
- an string that represents an email or phone number (must match format)
- an inventory amount between 0 and 1000
- Birthdates (can't be in the future)

We centralize these constraints by wrapping them in a type, such as

```fsharp
type PhoneNumber = private PhoneNumber of string
module PhoneNumber = 
    let tryCreate str =
      if (Regex(@"\d{3}-\d{4}-\d{4}").IsMatch(str))
      then Some (PhoneNumber str)
      else None 
```

This is great. It prevents defensive programming from leaking around the system and clearly encodes expectations on data. It avoids the downsides of [primitive obsession](https://grabbagoft.blogspot.com/2007/12/dealing-with-primitive-obsession.html).

However, we're missing out on some power. We're encoding constraints in a way that only gives us pass/fail validation. 
We have to duplicate constraint information if we want to explain a failed value, generate data, or similar actions.

FsSpec represents these constraints as data so that our programs can understand the constraints on a value. 

```fsharp
let inventorySpec = Spec.all [Spec.min 0; Spec.max 1000]

// Validation
Spec.isValid inventorySpec 20

// Explanation: understand what constraints failed (as a data structure)
Spec.explain inventorySpec -1

// Validation Messages
Spec.explain inventorySpec -1 |> Formatters.prefix_allresults // returns: "-1 failed with: and [min 0 (FAIL); max 1000 (OK)]"

// Data Generation (with FsCheck)
Gen.fromSpec inventorySpec |> Gen.sample 0 10  // returns 10 values between 0 and 1000
```

There are also other possibilities FsSpec doesn't have built-in. For example,
- Comparing specifications (i.e. is one a more constrained version of the other)
- Serialize and interpret constraints for use in different UI technologies
- Automatic generator registration with property testing libraries (e.g. FsCheck)

## Basic Value Type using FsSpec

It's still a good idea to create value types for constrained values. Here's how you might do it with FsSpec

```fsharp
open FsSpec
type InventoryCount = private InventoryCount of int
module InventoryCount = 
    let spec = Spec.all [Spec.min 0; Spec.max 1000]
    let tryCreate n =
      Spec.validate spec n 
      |> Result.map InventoryCount
```

## Supported Constraints

- `Spec.all spec-list`: Logical and. Requires all sub-specs to pass
- `Spec.any spec-list`: Logical or. Requires at least one sub-spec to pass
- `Spec.min min`: Minimum value, inclusive. Works for any `IComparable<'a>`
- `Spec.max max`: Maximum value, inclusive. Works for any `IComparable<'a>`
- `Spec.regex pattern`: String must match the given regex pattern. Only works for strings. 
- `Spec.predicate label pred`: Any predicate (`'a -> bool`) and a explanation/label
- `Spec.minLength min`: set a minimum length for a string or any IEnumerable derivative
- `Spec.maxLength max`: set a maximum length for a string or any IEnumerable derivative
- `Spec.values values`: an explicit list of allowed values
- `Spec.notValues values`: an explicit list of disallowed values

## Generation Limitations
[![Nuget (with prereleases)](https://img.shields.io/nuget/v/fsspec.fscheck?label=NuGet%3A%20FsSpec.FsCheck)](https://www.nuget.org/packages/FsSpec.FsCheck)

Data generation can't be done efficiently for all specifications.
The library recognizes [special cases](./src/FsSpec.FsCheck/OptimizedCases.fs) and filters a standard generator of the base type for everything else.

Supported cases
- Common ranges: most numeric ranges, date ranges
  - Custom scenarios for other IComparable types would be easy to add, if you encounter a type that isn't supported.
- Regular expressions
- Logical and/or scenarios
- String length
- Collection length: currently support `IEnumerable<T>`, lists, arrays, and readonly lists and collections.
  - Dictionaries, sets, and other collections are not yet supported but should not be difficult to add if users find they need them.
- `Spec.values`, an explicit list of allowed values 
  - `Spec.notValues` works by filtering. This will likely fail if the disallowed values are a significant portion of the total possible values

Predicates have limited generation support. For example, 
```fsharp
let spec = Spec.predicate "predicate min/max" (fun i -> 0 < i && i < 5)
```
The above case will probably not generate. It is filtering a list of randomly generated integers, and it is unlikely many of them will be in the narrow range of 0 to 5. FsSpec can't understand the intent of the predicate to create a smarter generator.

Impossible specs (like `all [min 10; max 5]`), also cannot produce generators. The library tries to catch impossible specs and thrown an error instead of returning a bad generator.

## Complex / Composed Types

FsSpec doesn't currently support composed types like tuples, records, unions, and objects.

The idea is that these types should enforce their expectations through the types they compose. Scott Wlaschin gives a [great example](https://fsharpforfunandprofit.com/posts/designing-with-types-representing-states/) as part of his designing with types series.

A short sample here.

Sum types (i.e. unions) represent "OR". Any valid value for any of their cases should be a valid union value. The cases themselves should be of types that enforces any necessary assumptions
```fsharp
type Contact = 
  | Phone of PhoneNumber
  | Email of Email
```

Product types (records, tuples, objects) should represent "AND". They expect their members to be filled. If a product type doesn't require all of it's members, the members that are not required should be made Options.

```fsharp
type Person = {
  // each field enforces it's own constraints
  Name: FullName 
  Phone: PhoneNumber option // use option for non-required fields
  Email: Email option
}
```

Rules involving multiple members should be refactored to a single member of a type that enforces the expectation. A common example is requiring a primary contact method, but allowing multiple contact methods.
```fsharp
type Contact = 
  | Phone of PhoneNumber
  | Email of Email

type Person = {
  Name: FullName 
  PrimaryContactInfo: Contact
  OtherContactInfo: Contact list
}
```

See [Designing with Types](https://fsharpforfunandprofit.com/series/designing-with-types/) (free blog series) or the fantastic [Domain Modeling Made Functional](https://fsharpforfunandprofit.com/books/#domain-modeling-made-functional) (book) for more detailed examples.

## Roadmap

This library is early in development. The goal is to get feedback and test the library in real applications before adding too many features. Please open an issue if you have feedback.

Lines of inquiry include

- Improve customization: Explore how users most often need to extend or modify existing functionality. 
  - add formatting for their custom constraint?
  - mapping custom errors? / interpreting error scenarios?
- Identifying base set of constraints that should be built into the library
- Predicate spec meta: Potentially allow meta separate from predicates so instances of a similar custom constraints can leverage case specific info (e.g. if max were implemented as custom, making the max value accessible to custom formatters, comparisons, generators, etc)
- Not spec: Negate any specification. 
  - This is easy to add for validation, but makes normalization for inferring generators more complex. It should be doable, but I have to consider negations of specs (i.e. max becomes min, regex becomes ???) and how that would impact other features like explanation
  - 
## Project Status
The most foundational features (validation, generation, explanation) are implemented and tested.
The library should be reliable, but the public API is subject to change based on feedback.

The main goal right now is to gather feedback, validate usefulness, and determine next steps, if any.

## Inspiration
This library borrows inspiriation from many sources 
- [Clojure.spec](https://clojure.org/about/spec)
- [Specification Pattern](https://www.martinfowler.com/apsupp/spec.pdf) by Eric Evans and Martin Fowler
- [Domain Driven Design](https://en.wikipedia.org/wiki/Domain-driven_design)
- Type-driven Development
  - [Designing with Types](https://fsharpforfunandprofit.com/series/designing-with-types/) by Scott Wlaschin
  - [Mark Seemann](https://blog.ploeh.dk/2015/05/07/functional-design-is-intrinsically-testable/#aee72ce959654d9388b448023f469cbc)

## Original Experiments

I previously looked into adding constraints as a more integrated part of the F# type system. 
Those experiments failed, but are [still available to explore](https://github.com/farlee2121/FsSpec-OriginalExperiment).

If you want such a type system, you might checkout [F*](https://www.fstar-lang.org/), [Idris](https://www.idris-lang.org/), or [Dafny](https://github.com/dafny-lang/dafny).
