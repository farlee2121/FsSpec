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


## Side note

I don't know how I didn't find this framework in my previous explorations
https://github.com/lfr/FSharp.Domain.Validation

Seems to be made with similar intent.
They're based on a validation predicate. Wouldn't support explainers or automatic generators


FsToolkit has some nice validation computation expressions https://www.compositional-it.com/news-blog/validation-with-f-5-and-fstoolkit/
- result-based

This library has an interesting composition scheme https://github.com/JamesRandall/AccidentalFish.FSharp.Validation