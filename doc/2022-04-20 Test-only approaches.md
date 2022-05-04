
## Summary

A major benefit originally desired from spec is automated correctness testing. 
Correctness meaning inputs in the valid domain produces outputs in the expected range.
This requires a system that values total signatures and does not rely on side-effects like exceptions to represent expectable errors. It also generally expects arguments are not mutable, but I may be able to test mutated arguments too.

We also cannot completely test correctness, particularly
- that outputs values are the expected output values for given input

We can test for
- valid input creates valid output / our domain can handle any input it allows
- that the system handles expectable errors gracefully 

In a way, we're testing "least surprise". What's advertised is the actual allows domain/range.

I realized this can be done without language modification. We can
- expression to constraint translator
  - this would allow all kinds of different tools based on constraints found in code. For example, I could create xml schemas or translate into various validation frameworks
- Generate FsCheck Arb/generators from an expression (like a constructor or factory) via reflection 
  - update: probably want to parse expressions to a data structure first and then `constraint data structure -> FsCheck Arb`
- Reflect over functions to enumerate Arbs that need configured
- Run simple property tests that just ensure the right generators and run a predicate test on the output of a function
- Define a strategy(ies) for discovering functions to run the property tests on

This route has several independently useful and self-contained tools to experiment with. 
- We can start simple by manually passing expressions to create generators. That would be enough for us to fairly easily set up property tests manually.
- Then we could move to a test factory that accepts a function and reflects to produce arbs. `testFullFunctionRange domainFunc`
  - this would require some conventions for discovering factories/constructors/constraints on types
  - This would cover most cases nicely.
- Any convention- or configuration-based test discovery would just be convenience and a relatively known problem 


TODO
- [x] check if any reflection or expression-based arb generators exist
- [ ] use https://gist.github.com/swlaschin/54cfff886669ccab895a as a guide for supported conventions


Stretch goals
- test mutated arguments?
  - hmm, assigning a whole new object wouldn't work. Members of the argument would have to be mutable. If that's the case then we'd really be checking if setters have appropriate guards. We could probably check that statically just by making sure setters have the same constraints as any constructor. Public fields would be an automatic error.
- detect all means of instantiation and test the least restrictive
- 

## Package name ideas

FsSpec may not be the best overarching name, since i'm now focused on the random testing over the specification

Package that infers constraints
- FsSpec.FactoryToConstraint
- ExpressionToConstraint
- ConstraintParser
- DomainCheck.Infer.Factory?
- DomainCheck.Constraints.Core (for the data schema)
- DomainCheck.Constraints.Infer?

Package that automatically creates FsCheck properties from methods and type factory conventions
- FsCheck.AutoProperty?
- FsCheck.DomainCheck (.Generators? ConstrainedGenerators?)

Package that random tests whole code bases
- FsSpec.Instrument 
- FsRandom.Instrument
- DomainCheck.Instrument


## Raw late night programming thoughts
Would an fsspec built on some dynamic type be useful in f#?

It wouldn't provide the desired testing benefit.

Maybe in parsing or situations that interact with unvalidated data. there is always some kind of shape. I feel like unions might be a better solution to composed polymorphism than structural (set semantic) data.

Set semantics is nice in readable data formats, but it tells an incomplete story, which is dangerous in code


----


I could still do some automated correctness testing by analyzing types and inferring bounds based on constructors, factories, or another convention.

This requires the codebase uses total signatures and not exceptions. We cant know if the return was expected, only that the system handled it gracefully (expected values is for unit tests or similar, it requires author intent).

I don't think such a tool should allow configuration of type constraints. It should expect the user to encode it in their system 

----

Handled gracefully - > output in expected range

This basically makes fsspec a reflection-based fscheck Arb creator. The other bit would be specifying/discovering a list of functions to run with a very basic property test against (no exceptions and possible boolean constraint expression)

The first part might exist 

----

A good experiment would just be passing some function or expression to reflect on to an Arb generator. That could be a package of it's own.

The next step is trying to generate for all types on a function. Probably start w/ explicit, then some convention-based Arb config

Then there might be some FsCheck.Instrument package or cli tool that tries to auto build constrainted arbs for all functions based on a config or search pattern

----

```yml
date: 2022-04-24
```
It's more the factories I'm interested in than the validators. I need the type factory to create the generator. Part of the point here is that the type has constrained construction.

This could be a good compliment for mutation testing for completeness. It focuses on data over logic. Mutation testing covers the logic. Mutation testing can't guess requirements, but can tell us if semantic edits slip through 

I should search for the term auto property, seems like a legit title for such a framework 


## New VS Code test runner

I notice there's an new test explorer section that handles Expecto fantastically. It handles 
- go-to-test
- run tests in file
- inline test 
- run test from inline status
- reveal in test explorer

It isn't part of Test UI or the .Net Core Test Explorer extensions. I disabled both.
- ionide? -> !!! this appears to be it. The test view goes away when I disable ionide

The only features missing
- debug test
- recognizing xunit tests