
## Motivation


## TODO
- [x] Improve namespaces and assemblies of different tools
- [x] Consider rename from Constraint to Spec
  - would solve awkard conflict with the keyword
  - would also be more consistent with the library name
- [x] consider product types (tuple, records, etc): use reflection? use a computation expression? 
- [x] ~~Figure out how to generate `IComparable<'a>` by default from FsSpec.FsCheck~~
  - Don't actually need this. The type they're generating probably isn't directly an IComparable. I got tree generation mixed up with the constrained type generation
- [ ] test validate
- [ ] Create new readme for value-based validation
  - [ ] explain why another validation approach. constraints as data allow computation on those constraints: like creating generators, serializing, comparison
    - [ ] probably also introduce type-driven approach similar to FSharp.Domain.Validation
    - [ ] Usable without type-driven design. Can just use it for validation, or other applications of the specification pattern
  - [ ] show type factory
  - [ ] show customized explainer (probably in form of a partial pattern match that can report it created a message before defaults run? Need to consider composition here)
  - [ ] how to handle complex types (or via unions, and via records, tuples, etc)
  - [ ] probably list available constraints
  - [ ] How generation works, dangers and supported cases
- [ ] create an explainer
  - [ ] better error paradigm (return list of failed constraints)
      - perhaps return special failure for constraints invalid for a given type?
- [ ] Consider custom equality on constraintLeaf -> really equality for custom. 
- [ ] Seem to be a lot of testing issues around constraints not valid for a given type. What if anything do I do about it
- [ ] Release nuget packages
- [ ] Setup a build

Later
- consider a builder for c#?
- [ ] consider new leaf types (divisibility/mod class, contains, length, min length, max length, allowed exact values, disallowed exact values)
  - [ ] is it worth sub-dividing leafs into groups that work on a certain type?
  - [ ] idea: could have an extension package using a math library to constraint (and generate) from equations
- [ ] Figure out floating point generation 

## Product Types
I think the products should enforce their own invariants. Product types are inherently an AND and unions an OR. 

Enforce expectations at the member/field level
- If a record should have one member or another, then they should really be wrapped in a union and made one field that describes the view in which they are interchangeable
  - If it must have at least one of a set of fields, again, this should be refactored to a union with a one primary then a list of additional. Otherwise, it can be a union wrapping a lists with a non-empty constraint. Use a union to wrap any different types you want in the list
- If a member is optional, make it an option
- See Scott Wlaschin's Designing With Types

## Awareness and Feedback
- [ ] Consider posting FsSpec to F# slack