
## Extension
I think the following cases cover most constraints

Other potentials
- a list of values to exclude
- subset/superset
- type constraint (`basedOn<string>`). probably covered by the type initializer

```fs
type Constraint<'a> = 
    | MaxLength of int
    | MinLength of int
    | Regex of string // regex expression
    | Max of 'a // should be any type of number
    | Min of 'a
    | Choice of 'a list
    | And of Constraint<'a> list
    | Or of Constraint<'a> list
    | Custom of string * ('a -> bool)
```

About everything else can be handled by aliasing the constructors. Examples,
```fs
let lessThan limit = (Max limit - 1)
let futureDate = (Custom ("future-date", (fun date -> DateTime.Now < date) )
let even = (Custom ("even", (fun x -> x % 2 = 0)))

let allOf constraintList = (And constraintList)
let anyOf constraintList = (Or constraintList)
```

I could also provide generator, serializer, and other mappings for the common custom cases.

ISSUE: `lessThan` may be equivalent to `Max - 1` validation wise, but will not be semantically the same for error messages.
I could address this with 
- Option: encode it as a predicate/custom
  - I suppose I can always map the tag to the generator/validator/other a more specific constraint. i.e. lessThan -> DefaultGen.maxGen (limit - 1)
  - !!!the problem here is that I loose semantic information. The predicate has the limit baked in, so I can't access it as an intrinsic quality of the constraint
- Option: Make a general Labeled constraint that takes any constraint
  - I might want to make BoolTest it's own, without a label. I hesitate to remove the label because the error messages aren't very useful without a label, but with a label it duplicated the labeled case
- Option: create a separate case
  - This option only makes sense if there only a few more special cases

!!! Just about every constraint could be modeled with labelled predicates, The issue then is getting the proper seman

## Creating generators
Could possibly flatten into an array of AND groups? then I can split them between source and filter types.
Compatible sources can be anded together finding range overlap, finding choice options in a range and regexes together, eliminate choices not compatible with regex

The interpretation of length depends on the type... I think we handle it by string and list cases. Those are the main groups
Then filter on custom,

If incompatible constraints are specified (max int + minLength) then return an error

Another issue is that range appears to only apply for ints. I'd need a separate string range concept, or more likely just error for min/max with strings
I still want to handle the DU, Guid, and other valid comparables though...

The experimental branch of hedgehog can combine generators https://github.com/hedgehogqa/fsharp-hedgehog-experimental
However, they seem like they might just be filters. I also don't know that I want to support hedgehog who forked and didn't even try to contribute to FsCheck

TOOL: not what i'm looking for, but cool https://github.com/fscheck/FsCheck/issues/177

## How should I handle errors? 
 the value will be available up front and isn't necessary in the return type
 I could just return the constraint, but what about nesting? I think i'd need to return a tree?
 how do I apply to aggregated types? I either need to map properties manually
 (and probably add some helper for naming properties in the errors) or use reflection. Maybe a separate union for representing a tree of (proptery * constraint)


## Complex types
Creating a structure that represents complex types is hard, mostly because the value constraint requires a generic type parameter.
That type has to be passed in, but we can have arbitrarily many types in records or tuples

I think we're just going to have to rely on reflection here. Otherwise, ... we might have to weaken the type guarantee on a constraint to `obj`.
The complex type constraints would be a collection of `('parent -> obj, Type, Constraint<obj>)` where Type is a `System.Type` for casting the child property



## Clojure Spec

The relevant module is `spec/gen`
"the first predicate will determine the generator and subsequent branches will act as filters by applying the predicate to the produced values"

It would be really cool if everything could be defined as a predicate, and the explainer was smart enough to categorize predicates (as it appears clojure does)

