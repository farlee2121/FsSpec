---
date: 2022-04-20
author: [Spencer Farley]
---

## Motivation

[Test-only approaches](./2022-04-20%20Test-only%20approaches.md) outlines a new possible way forward for FsSpec. The focus is on creating generators from detected 

key questions
- can I use any existing libraries to achieve this?
- What requirements must be met to create a meaningful testing tool (focused only on the generation for now)?
- Is a simple arb filter good enough (clojure spec works this way)?
  - this would make Arb creation as easy as finding the right boolean expression
- Is is feasible to reliably create generators from arbitrary factories? 
- If infeasible, can I add constraints that would make it feasible?
  - i.e. require a type have a public function called `validate` with certain return type 

## Library prospects

Searched for FsCheck on nuget
- https://www.nuget.org/packages/AutoFixture.Idioms.FsCheck/
  - seems similar, but not very well documented
  - appears to just check that no return values are null
  - I don't see any references on the ploeh blog, but there are some about creating arbs https://blog.ploeh.dk/2015/09/08/ad-hoc-arbitraries-with-fscheckxunit/
  - TODO: look at how autofixture generated constrained types https://github.com/AutoFixture/AutoFixture
- https://www.nuget.org/packages/CsCheck/
- https://www.nuget.org/packages/AndreasDorfer.BehaviorTestGenerator/
- https://www.nuget.org/packages/Fable.FastCheck/
- https://www.nuget.org/packages/AntaniXml/
- https://www.nuget.org/packages/FSecurity.FsCheck/2.1.0-beta





Idea: make manual configuration of type -> expression in a way that other methods (like convention-based discovery) can be merged in a separate stage
- i.e. Some composite configuration strategy  that sets override order (e.g. manual if present, then try find validate function, then try find constructor)
  - Maybe call it `ConfigPriorityComposite`? or `ConfigBuilder`?
- by separating out composition, consumers can create their own configuration priorities and strategies



Q: what kinds of challenges do I expect from creating generators out of expressions
- recognizing constraints like `<=`, regular expressions, finite sets
- different ordering of expressions? 
- expressions composed from multiple functions
- complex type validation (e.g. numerous properties)
- Dealing with non-boolean expression components (i.e. result types, class initialization, exceptions, assertions)