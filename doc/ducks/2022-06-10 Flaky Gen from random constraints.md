---
date: 2022-06-10
---

## Motivation 

I have a simple property to verify that any data generated for a constraint also passes validation for the constraint.

There is no problem with passing validation, but the generator occassionally fails to produce a value. 
This even happens when I add condition that the property should only run if the generator is proven to produce values.

I want to understand why this is failing and stablize the test.


## Ideas

non-deterministic predicates
  - looks like it could be once cause, but not the only

!!! Constraints can include dead-end/impossible and groups but still pass validation
- OR combinators allow a constraint to have branches that are impossible to satisfy, but the constraint as a whole can still be satisfied
- This is fine during validation, but not during generation. During generation OR chooses with equal probability between the AND groups. 
If it picks a valid group it will appear to generate values, but if it picks an impossible group the generation will fail.

This means I need to filter out impossible groups before constructing the final `oneof` generator

Q: What are common impossible constraints?
- constraints invalid for a type (i.e. regex for anything but string)
- Improper paired constraints (i.e. a max that is less than the min)
- a predicate that always returns false (hard to detect/filter, but probably not a practical worry, I can just adjust the arb generation)


## TODO
- [x] create a test/registry to check if a leaf type is valid for the constrained type
- [x] create a registry/test for known impossible constraints
- [x] decide what to do if there are no possible paths after removing impossible leaf groups (probably throw exception)
- [x] Add a property to ensure known impossible leaf groups are filtered
- [ ] IComparable Generator needs to be part of the generator package, not my test suite
- [ ] String is gen -> validate test fails because string generator doesn't know how to handle min/max

Refactorings
- consider renaming ConstraintLeaf to something like Leaf or Single
- consider renamign Constraint to Spec


