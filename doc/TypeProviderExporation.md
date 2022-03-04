---
date: [2022-03-02]
---

## Motivation

FsSpec is trying to experiment with a constraint- and composition-enabled type system.
To do this I need to be able to create types associated with given constraints.

There is some exploration of what these types should look like in [Requirements Exploration.md](./Requirement%20Exploration.md).

My goal here is to feel out the limitations of type providers and if they can satisfy my needs for affiliating constraints to types.

Ideally, the type provider takes a constraint expression directly


## Explorations

The json type provider at least shows that it can accept a constant
- https://fsprojects.github.io/FSharp.Data/#Type-Providers

How do queries work?
- those use computation expressions, which won't work. They produce a value, and I need to produce a type. I need values to be created as this generated type.

tutorial: https://docs.microsoft.com/en-us/dotnet/fsharp/tutorials/type-providers/creating-a-type-provider
- says sql provider checks queries at compile-time
- looks like type providers can also produce anonymous type instances
  - https://fsprojects.github.io/FSharp.Data.SqlClient/
- "the mechanism isn't designed for intra-language meta-programming, even though that domain contains some valid uses"


READ: https://dev.to/7sharp9/applied-meta-programming-with-myriad-and-falanx-7l4
- "Type Providers can not create any F# constructs like records or Discriminated Unions"
- Skimmed it, but it looks like a great dive into Myriad's motivations, design, and the possibilities for metaprogramming in F#
- Haxe: pretty much a language language. It's meant for cross-compiling to many languages. For example, if you want to compile an api client for multiple languages.


Looks like type providers can have dependencies
- https://fsprojects.github.io/FSharp.TypeProviders.SDK/technical-notes.html#Dependencies

Q: How will the type provider consume custom constraint types?
- I think I can have the type provider accept any `IConstraint<T>`. 
- All the generated type really needs to do is 
  - be type safe for further composition of constraints
  - return the data structure representing the constraint
- Really, it might be a good idea for the type to implement `IConstraint<T>` itself. 
  - this allows the type to be immediately be used with all the same methods as used for primitives
  - This makes composition an endomorphism, which should facilitate the kind of infinite composability I desire

Q: Key: Can a type provider accept an expression as a static parameter?
- not looking promising https://stackoverflow.com/questions/9547225/can-i-provide-a-type-as-an-input-to-a-type-provider-in-f
- looks like only primitives are valid because they want to avoid possible compilation of the host file before the type provider :/
  - this would exclude typed expressions too, because they're not primitive and could lean on local types
- passing constraint strings would prohibit custom constraints
- IDEA: erased types could be used if I wanted only compile-time assistance without runtime performance hit.
  - this is unlikely though. It's probably better to just detect when the constrained type is a struct and wrap with a struct if the performance is really necessary


Since type providers are out, My next best route may be inheritance

```fs
type RoomNumber private () =
    inherit Constrained(min 2 &&& max 20)
```
- that's not to long. Users can always take the long route and implement the rather simple interface if they need a struct for performance
- The main downside is that the user has to remember to keep their constructor private
- NOTE: construction has to be on the type in order for us to create instances while guaranteeing no one can create an invalid instance
  - this is true even if we generate full custom types
  - !!! This kinda undermines having a separate validate function
- This doesn't work, or would get really verbose, for complex types


Q: Is there benefit in centralized methods (e.g. conform, satisfies/isValid, explain, validate)?
- validation has to happen on creation. There can be no instances that aren't valid
- !!!: this has me realizing that any operations of the data will require unwrapping then revalidating, or a bind
- all methods (explain, validate, satisfies) require some unvalidated type to be generated. It's simple for validating primitives, but would require type generation for any complex types
  - the implementations could still be generic, since it all boils down to `IConstraint` and is a chain of broken down `a -> bool`
  - I don't think it's performance friendly do dynamically break-down the expressions
  - If creation/instantiation has to live on the type, then it's probably most consistent to make all the methods available on the type.


I could look at manually composing constraints like
```fs
let complex = complexBuilder [
    fun x -> x.Dollars, dollarConstraint
    fun x -> x.Tax, min 0 &&& max 1 // percent constraint
]
```

This ends up being a decent validation experience, but isn't the type system I'd hope for.
- If i'm not going to have guaranteed constraint satisfaction, then there's no need for generated types or compile-time assistance. I could have constraints as values, build using builder functions or even a computation expression. I'm pretty sure there are already libraries for that kind of thing, though I'm not sure they support explain

## Summary of takeaways
- Type providers can't currently satisfy what I want to do with this library
- instantiation requires some input type that isn't constrained
- Without type providers, complex composition is not reasonable
  - requires manual map of constraints to properties. Even if I do that, I don't have a reasonable unconstrained type to instantiate from 
- General methods like `Spec.isValid` don't mesh well with guaranteed constraint compliance of instances
  - spec methods probably live on the individual types

Even with all the uncovered limitations (no general methods, needing unconstrained inputs), it would still be helpful if I could achieve this type generation.
It'd automate a lot of type creation that, now that I think about it, I was doing manually while following the type-driven development. Plus, those modifications
make the end result effectively the same as the manual outcome.