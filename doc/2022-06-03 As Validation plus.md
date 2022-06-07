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

## Normalization

Ok, What are my goals and what cases can I expect.

My ultimate goal is to have a logic expression that is nothing but an OR with a bunch of AND groups; the ANDs only contain leaf constraints.

Cases to worry about
- single constraint -> wrap it up in OR(AND )
- OR contains leafs -> wrap each child constraint with AND
- AND contains leafs -> do nothing
 - AND contains AND -> merge to single AND (could I get away with an OR (AND ) nest? I think so, since all ORs will eventually merge), recurse
 - AND contains OR -> return a single OR with distributed AND, recurse
 - OR containing OR -> merge to single AND, recurse
 - OR containing AND -> recurse to make sure AND children are normalized

TODO: Supporting `not` would require some more complicated transformation, but is still doable
- need to handle demorgans for negated combinators
- need to understand constraint negations
 - not (max 20) -> min 21
 - not regex -> ??? regex should have negation that I could leverage. Just wrap the whole expression with a not, minus any line start/end 
 - not value/set -> a filter is probably good most of the time
 - not custom -> just an inverted filter

Properties
- tree should always be 3 deep (or, and, leaf constraints)
  - redundant with other properties
- tree should always have a top-level OR
- The OR should only contain ANDs
- ANDs should never contain any combinators (this includes NOT once we support it)
- The trees should be logically equivalent (same answers for any input)
- What do I test to ensure NOT flips constraints correctly?
  - probably construct special trees to prove de morgans 
  - any leaf wrapped in not returns the expected opposite

STUCK: Ok, i'm suck with a major jump in complexity no matter how I order the known properties, and I can't think of another good property
- Should I use an example-based test to handle the next jump?
  - this would make it much easier to target a single layer without handling recursion yet
- Can I target single-layer distribution without just duplicating logic in the test?
- The leaf-less tree property is passing, that means the leaf behavior has to be the problem

TODO: Empty combinators are a constant pain and make no sense. I should probably just find a way to forbid them

What should be the behavior of an empty combinator? 
- It should have no effect
- I could solve this by trimming, or with identity laws. 
- The problem with identity laws is that I need to know the parent of the empty combinator to know what it should return to be neutral

## TODO

- [ ] better error paradigm (return list of failed constraints)
- [ ] demonstrate generation
- [ ] library-specific tree to improve (and simplify) type signatures (`combinators` and `leafconstraint`?)
- [ ] move, but preserve, old readme and experiements
- [ ] Create new readme for value-based validation
  - [ ] explain why another validation approach. constraints as data allow computation on those constraints: like creating generators, serializing, comparison
    - [ ] probably also introduce type-driven approach similar to FSharp.Domain.Validation
    - [ ] Usable without type-driven design. Can just use it for validation, or other applications of the specification pattern
  - [ ] show type factory
  - [ ] show customized explainer (probably in form of a partial pattern match that can report it created a message before defaults run? Need to consider composition here)
  - [ ] how to handle complex types (or via unions, and via records, tuples, etc)
  - [ ] probably list available constraints
- [ ] test validate
- [ ] consider product types (tuple, records, etc): use reflection? use a computation expression? 
- [ ] consider replacing sequence with list for more idiomatic F# (and eliminate infinite sequences)
- [ ] create an explainer
- [ ] Consider custom equality on constraintLeaf
- [ ] Improve namespaces and assemblies of different tools

Later
- consider a builder for c#?

!!! Rember `List.pick` is like `FirstOrDefault`