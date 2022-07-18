
## Motivation

Layout a roadmap of what needs to be done before the library is ready for early use and feedback

## TODO
- [x] Improve namespaces and assemblies of different tools
- [x] Consider rename from Constraint to Spec
  - would solve awkard conflict with the keyword
  - would also be more consistent with the library name
- [x] consider product types (tuple, records, etc): use reflection? use a computation expression? 
- [x] ~~Figure out how to generate `IComparable<'a>` by default from FsSpec.FsCheck~~
  - Don't actually need this. The type they're generating probably isn't directly an IComparable. I got tree generation mixed up with the constrained type generation
- [x] test validate
- [x] Setup a build
- [x] create an explainer
  - [x] better error paradigm (return list of failed constraints)
      - perhaps return special failure for constraints invalid for a given type?
- [x] Create new readme for value-based validation
  - [x] explain why another validation approach. constraints as data allow computation on those constraints: like creating generators, serializing, comparison
    - [x] probably also introduce type-driven approach similar to FSharp.Domain.Validation
    - [x] Usable without type-driven design. Can just use it for validation, or other applications of the specification pattern
  - [x] show type factory
  - [x] probably list available constraints
  - [x] How generation works, dangers and supported cases
  - [x] how to handle complex types (or via unions, and via records, tuples, etc)
- [x] Release nuget packages
- [x] Reduce required .NET version
- [ ] Test explain returned errors equal expected errors
- [ ] Test mapping from explanations to messages?
- [ ] Readme: Demonstrate customized explanation formatter (probably in form of a partial pattern match that can report it created a message before defaults run? Need to consider composition here)
- [ ] Consider custom equality on constraintLeaf -> really equality for custom. 
- [ ] Seem to be a lot of testing issues around constraints not valid for a given type. What, if anything, do I do about it?
- [ ] Add NaN min/max as known impossible constraint
- [ ] Consider additional generation and validation types needed to start getting feedback
  - [x] probably at least support generating double and date ranges
- [ ] Should type mismatches really throw exception? (complicates logical equivalence, but it's something that should probably fail fast and obviously)
- [ ] Explanation namespacing seems a bit awkward trying to balance conflict with the Spec tree union cases and availability

Later
- [ ] Consider a base library of standard constraints (PastDate, FutureDate, NonNegativeInt, NonNegativeDouble, NonEmpty, etc)
- [ ] Consider representing named specs in the data (e.g. this group means "sanitized markdown"). It would potentially improve message generation
- consider a builder for c#?
- [ ] Consider shrinkers for fromSpec generators
- [ ] consider new leaf types (divisibility/mod class, contains, length, min length, max length, allowed exact values, disallowed exact values)
  - [ ] is it worth sub-dividing leafs into groups that work on a certain type?
  - [ ] idea: could have an extension package using a math library to constraint (and generate) from equations
- [ ] Figure out floating point generation 
- [ ] Consider more sophisticated custom (i.e. allows case data separate from predicate)
- [ ] More special generation cases
  - [x] DateTime ranges
  - [x] DateTimeOffset (does offset need special gen considerations?)
  - [x] Int64
  - [x] Int16
  - [ ] unsigned integers
  - [ ] DateOnly
  - [ ] TimeOnly
  - [ ] TimeSpan
- [ ] Additional leaf types
  - [ ] not combinator
  - [ ] match value(s) (maybe with optional custom equality comparer)
  - [ ] min/max length
- [ ] consider simpler formatter customization
  - [ ] most common cases will probably be overriding leaf cases, especially custom.
  - [ ] another common case might be reductions (e.g. min + max -> `0 < i < 100`)
    - [ ] could maybe simplify this by making min and max a single Range constraint with optional bounds...

## Reasons this library could fail to be useful

- Reduced individuality of errors, difficulty mapping to custom domain errors
  - idea: `Error.like` a nice wrapper for seeing if certain expectations failed (rather than folding over the explanation directly)
    - could also create a `Spec.like` which checks if the spec enforces the given expectation. This is effectively checks if one spec is a subset of another
- Doesn't decrease complexity enough compared to bespoke implementations
- Insufficient constraint expressiveness
  - There is a tension between constraints that economically express intent, and minimal internal constraint cases. 
    - More specific cases can increase expressiveness of error messages, or reduce work for special formatting (e.g. require exact length and state it as such rather than a min/max pair). But this also increases library complexity
- Too many real constraints don't fit a shared representation
  - i.e. checks that require out-of process reconciliation, against a DB or dynamic data
    - this could be partially mitigated with type providers
  
## Product Types
I think the products should enforce their own invariants. Product types are inherently an AND and unions an OR. 

Enforce expectations at the member/field level
- If a record should have one member or another, then they should really be wrapped in a union and made one field that describes the view in which they are interchangeable
  - If it must have at least one of a set of fields, again, this should be refactored to a union with a one primary then a list of additional. Otherwise, it can be a union wrapping a lists with a non-empty constraint. Use a union to wrap any different types you want in the list
- If a member is optional, make it an option
- See Scott Wlaschin's Designing With Types

## Awareness and Feedback
- [ ] Consider posting FsSpec to F# slack