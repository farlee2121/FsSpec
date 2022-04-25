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
  - Thinking back on his blog posts, AutoFixture doesn't make an effort to vary information. It has simple defaults for given types. For example, i think strings are guids, lists are empty by default, etc. It can reflect to find basic types, it expects all necessary data of complex types to be required via the constructor, it always uses the simplest constructor.  
    - https://blog.ploeh.dk/2009/07/01/AutoFixtureAsTestDataBuilder/
  - AutoData attribute will fill in data for a function's parameters
  - Conclusion: There relevant pieces, but AutoFixture is does not meet the key constraint of automatically detecting constraints. A customization could probably be added to detect constraints and make it even simper to use.
- https://www.nuget.org/packages/CsCheck/
  - LINQ based generator composition is very interesting
  - More hands-on in guiding certain uses of property testing like comparing multiple routes, performance comparison, Model-based (randomize application of operations and check state, basically a way to test encapsulation on stateful constructs)
  - !!! Idea: would model-based testing make sense against a test API... I don't think so. I don't think it's worth the duplicated state tracking. It could be useful if I thought operations might temporally couple in weird ways.
  - Conclusion: property testing, but no automated constraint detection
- https://www.nuget.org/packages/AndreasDorfer.BehaviorTestGenerator/
  - lists some cool dependencies https://github.com/ionide/FsAst, https://github.com/fsprojects/Argu
  - Seems similar in motivation to TestApi. It defines a set of tests against some stateful service. It then runs them with a bunch of FsCheck generated data
    - requires myriad (a compiler plugin) and seems more involved than my Expecto.TestAPI
  - Does not appear to support any custom generators or arbitrary and does not appear to consider constraints
- https://www.nuget.org/packages/Fable.FastCheck/
  - FastCheck is a property test framework for typescript and javascript https://github.com/dubzzz/fast-check
  - This package is just bindings for fast check in Fable, which is f# compiles to javascript
- https://www.nuget.org/packages/AntaniXml/
  - generate date based on XML schema
  - xml schema's can have range constraints https://stackoverflow.com/questions/15486246/xsd-default-integer-value-range
  - Looks like strings can also be constrained with regular expressions using `pattern` constraint. Length can also be constrained
  - Restriction examples: https://www.w3schools.com/XML/schema_facets.asp
  - !!! Looks like xml can handle most constraints I'd care about, but I'd still need to translate from expressions to xml
    - this could possibly be useful for generating data, but I'd guess it'll be as easy to do `expression -> constraints as F# data structure -> generators`
- https://www.nuget.org/packages/FSecurity.FsCheck/2.1.0-beta
  - Very cool. Generates possible insecure strings like injection, XSS, XML references
  - Composes above generators into various fuzzing attacks
  - Works with FsCheck
  - Conclusion: Works at a more raw data level. Not concerned with domain constraints


## Experiment Goals

Doesn't look like a framework exists for what I want to do.

I should 
- [ ] check if a simple boolean filter is performant enough
  - [ ] idea: could probably wrap validators and constructors to create boolean predicates. Constructor would be false if exception thrown. Validator would be false if Result.Error or if Option.None
- [ ] decide what I look for in expressions 
  - [ ] use [scott wlaschin's samples](https://gist.github.com/swlaschin/54cfff886669ccab895a) as reference
  - [ ] probably need to let users register their own success/failure types, like if they use custom result unions
- [ ] Try to translate expressions into constraint data structures
  - [ ] IComparable limits (probably start with just int?)
    - [ ] should cover date rates, number ranges, even string ranges
  - [ ] string regular expressions
  - [ ] length (string, collection)
  - [ ] Finite allowed values
  - [ ] Disallowed values
  - [ ] Combinators: and, or, not
  - [ ] Other: predicates that don't fall into any other category (e.g. maybe calls database to check value)
  - [ ] Handle dynamic values (i.e. DateTime.Now)
  - [ ] handle named constants
  - [ ] Function calls 
  - [ ] complex types (i.e. tuples, records, objects)
  


Q: what kinds of challenges do I expect from creating generators out of expressions
- recognizing constraints like `<=`, regular expressions, finite sets
- different ordering of expressions? 
- expressions composed from multiple functions
- complex type validation (e.g. numerous properties)
- Dealing with non-boolean expression components (i.e. result types, class initialization, exceptions, assertions)

Q: what about dynamic value comparisons like date >= DateTime.Now?
- consider comparison against a factory-provided value `type ConstraintValue<T> | Static value: T | Factory factory:()->T`. This approach would also allow dynamic sets loaded from storage. The downside is that factories don't translate into transferrable data neatly. Different serializers could decide if they want to recognize and encode special values (like datetime now) or just invoke the factory and encode the point-in-time values


## Filter performance

source: https://fsharpforfunandprofit.com/posts/property-based-testing-1/
- good overview of how to use FsCheck different ways
- Side-note: idea: `QuickAll` might be a good way to exercise reusable specs like roundtrip

Int filtering is actually pretty fast 
gen 100k of int =19 or =20 -> consistently 6.6s over 4 runs
gen 100k of int to =19 -> 9.09s
forAll VerboseCheck =19 -> 165ms

Q: how fast is 100000 unfiltered integers?
- A: 

Q: How fast is the FsCheck regex generator?

Regex filter?
gen 100k of `@"\d{3}-\d{3}-\d{4}"` -> ran for several minutes and did not terminate
verbose check -> also never finished

Conclusion: Integer filtering is probably fine, but string filtering does not seem feasible

TODO: Test performance of https://github.com/moodmosaic/fscheck-regex
- not published as a nuget package
- Only about 7 lines of code to create the generator. Most of the work is done by Xeger
- Xeger appears to be part of Fare, which is a pretty popular package
- Times
  - Quick check `"^http\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$"` -> didn't time exactly, but test was pretty much instant
  - gen 100k urls -> gave up after a few minutes
  - gen 100k phone numbers `@"\d{3}-\d{3}-\d{4}"` -> 1m 53s
    - -> 2m 09s using Gen.constant, did not disrupt proper generation, but did cause generation of `string list list` instead of `string list`
- NOTE: The proper way to handle this would probably be to create a Gen for the target type that directly invoke Xeger.
  - https://fscheck.github.io/FsCheck/TestData.html
  - `gen { Phone (Xeger pattern).Generate()}` creates a `Gen<Phone>` this can then be registered without any type conflicts (i.e. overriding string gen)
- Conclusion: not super fast for large samples, but fast enough for property tests and much faster than filter

## Implementation scratch

What am I looking for in an expression?
- I think every validation will ultimately come down to a boolean expression.
- That boolean may be embedded in a pattern match or in a conditional that creates some Result or Option. The predicate may also be 
  - let's start with just a plain boolean expression




## Convention ideas 
Idea: make manual configuration of type -> expression in a way that other methods (like convention-based discovery) can be merged in a separate stage
- i.e. Some composite configuration strategy  that sets override order (e.g. manual if present, then try find validate function, then try find constructor)
  - Maybe call it `ConfigPriorityComposite`? or `ConfigBuilder`?
- by separating out composition, consumers can create their own configuration priorities and strategies



