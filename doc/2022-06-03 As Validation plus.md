---
date: 2022-06-03
author: [Spencer Farley]
---

## Context

I had a lengthy exchange on experimental totality checking with Mark Seemann https://blog.ploeh.dk/2015/05/07/functional-design-is-intrinsically-testable/#9209b510035645399e2e720228af9271

He made the excellent point that using property tests effectively covers anything this kind of test would cover.

It also led me to realize that migrating away from exceptions for defensive programming is probably easier done simply by counting `throw` keywords, or even writing an analyzer to consider them a warning/error

I also realized that C# has no standard Option or Result. This would make analyzing factories much harder, because we'd need some convention or configurable way to understand what consitutes a success versus failure path.


All this led me to think what value could still be had?
- standardizing type factories
- automated generators
- leverage constraint data in making decisions (e.g. ordering restrictiveness)
- ??? I'm sure people could find many uses for constraints as data

These goals could be accomplished with a validation framework that keeps its expectations as data.
Users could expose the constraints as a property that some FsSpec plugin could pick up on to create generators.
The same constraints are used as the implementation of the type factory.

This doesn't ensure the type is only created in satisfaction of constraints. It also doesn't simplify constrainted types declarations 

In this way it's mostly a validation framework that we leverage behind the scenes of our factory, but then we can also use the constraint data programattically

Using static member constraints, we could even have centralized spec methods. They would be more of a convenience wrapper over type-specific factories and constraint use.

I don't think we'd really need to bother with complex types. Those can be validated with existing tools like the FsToolkit's validation computation expressions.


## Auto testing

Such a system could easily leverage constraint-based automated random testing, since we could create all types without futher developer input

However, the benefit is reduced since it could not be a transition measure.

## Side note

I don't know how I didn't find this framework in my previous explorations
https://github.com/lfr/FSharp.Domain.Validation

Seems to be made with similar intent.
They're based on a validation predicate. Wouldn't support explainers or automatic generators


FsToolkit has some nice validation computation expressions https://www.compositional-it.com/news-blog/validation-with-f-5-and-fstoolkit/
- result-based

This library has an interesting composition scheme https://github.com/JamesRandall/AccidentalFish.FSharp.Validation

## Generators

Hmm. This is harder than I thought.
- Ranged generation is only straightforward for integers
- I'm feel going to end up with a lot of filtering, in which case I'm probably better off just writing custom
- Special cases are complex to detect
  - consider, `min 0 &&& (max 10 ||| value 500)`. How do detect bounds from this that don't end up throwing away tons of values?
    - Idea: I could distribute the and. Or could be decided between with `Gen.oneOf` to randomly pick a branch
      - This would allow me to reduce the list to a predictable form where I have the most information I can about any branch of value generation
      - `(min 0 &&& max 10) ||| (min 0 &&& value 500)` is much easier to create a generator for

Ok think of common types can be done performantly and how?
- Int -> can always get full range info (or constant value list) using distribution 
- limited constant values -> gen always becomes a choose + filter
- DateTimes -> can use tick representation to create an integer range, then convert back to datetimes
- strings -> always start with regex if present (or constant values), then truncate and/or filter

Values that probably doomed to filter
- Floats?
- bytes 


## TODO
- better error paradigm (return list of failed constraints)
- demonstrate generation
- library-specific tree to improve (and simplify) type signatures (`combinators` and `leafconstraint`?)
- move, but preserve, old readme and experiements
- Create new readme for value-based validation
  - explain why another validation approach. constraints as data allow computation on those constraints: like creating generators, serializing, comparison
    - probably also introduce type-driven approach similar to FSharp.Domain.Validation
    - Usable without type-driven design. Can just use it for validation, or other applications of the specification pattern
  - show type factory
  - show customized explainer (probably in form of a partial pattern match that can report it created a message before defaults run? Need to consider composition here)
  - how to handle complex types (or via unions, and via records, tuples, etc)
  - probably list available constraints
- consider product types (tuple, records, etc): use reflection? use a computation expression? 

Later
- consider a builder for c#?
 