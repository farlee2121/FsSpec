
## Motivation


## TODO
- [x] Improve namespaces and assemblies of different tools
- [ ] Figure out how to generate `IComparable<'a>` by default from FsSpec.FsCheck
- [ ] Consider rename from Constraint to Spec
  - would solve awkard conflict with the keyword
  - would also be more consistent with the library name
- [ ] Create new readme for value-based validation
  - [ ] explain why another validation approach. constraints as data allow computation on those constraints: like creating generators, serializing, comparison
    - [ ] probably also introduce type-driven approach similar to FSharp.Domain.Validation
    - [ ] Usable without type-driven design. Can just use it for validation, or other applications of the specification pattern
  - [ ] show type factory
  - [ ] show customized explainer (probably in form of a partial pattern match that can report it created a message before defaults run? Need to consider composition here)
  - [ ] how to handle complex types (or via unions, and via records, tuples, etc)
  - [ ] probably list available constraints
  - [ ] How generation works, dangers and supported cases
- [ ] test validate
- [ ] consider product types (tuple, records, etc): use reflection? use a computation expression? 
- [ ] create an explainer
  - [ ] better error paradigm (return list of failed constraints)
      - perhaps return special failure for constraints invalid for a given type?
- [ ] Consider custom equality on constraintLeaf -> really equality for custom. 
- [ ] Seem to be a lot of testing issues around constraints not valid for a given type. What if anything do I do about it

Later
- consider a builder for c#?
- [ ] consider new leaf types (divisibility/mod class, contains, length, min length, max length, allowed exact values, disallowed exact values)
  - [ ] is it worth sub-dividing leafs into groups that work on a certain type?
  - [ ] idea: could have an extension package using a math library to constraint (and generate) from equations


## Awareness and Feedback
- [ ] Consider posting FsSpec to F# slack