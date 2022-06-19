---
date: 2022-06-17
---

## Motivation

Explaining why a value fails to meet expectations is a key capability I think the library should support.

## Requirements and Constraints

REQ: Given any specification, a programmer can get a list of sub-specifications that are violated, if any
REQ: Programmers should be able to format custom messages explaining the failed spec
REQ: Programmer should be able to respond to failed constraints in arbitrary ways, not just printing messages
- e.g. could decide to do one thing if below min, and another if above max

## Exploration

Q: do combinators (and/or) need to known non-failing member to properly format messages?
- CON: this would add complexity and require a separate data structure for errors versus specs
- PRO: This would allow more descriptive explanations (i.e. Expected min 5 and max but value is more than 10)

Q: Do I need a general spec to string formatter?
- this would be useful for various messages, like displaying what constraints were expected of the data. Printing field help tips, validation messages.


Q: Can I use the existing spec model for my error data model?
- Q: What does the caller need to know about the failure?
  - value being validated (which they should have access to)
  - the particular constraints that failed
  - potentially the grouping the constraint belongs to / where it sits in the spec hierarchy
    - also might want to normalize to only examine leafs, and not consider groupings
- a separate error format feels redundant, because it'll need much of the same structure as a spec
  - At least we have combinators and leafs separated from the tree structure, we could create a modified tree using the existing combinator and leaf unions.
    This would eliminate most duplication
- hmm even if I split the data model from the library I still end up unable to use the factories in the validators. It's not awful, but a bit verbose
- Separate model
  - PRO: more flexibility
  - CON: can't reuse formatters between spec and error trees
    - how likely is this?
    - I actually could have some reuse, you'd just invoke the spec formatter from within the error formatter
      - !!! could glue approaches together by passing a spec formatter to an error formatter. 

```fsharp
type SpecError = 
    | Combinator of { Combinator ; ValidBranches; InvalidBranches}
    | Leaf of LeafData
```

Q: Should the default validators return the shared data structure, or just some pass/fail?
- the validators don't really benefit from having their own result stucture. The parent would just need to map it into the shared format
  - it would allow me to keep the constraint data definitions with the constraint module though. This is not necessarily a quality to optimize against
- Using pass/fail 
  - CON: disallows possibility of multiple failure modes
    - are multiple failure modes even a concern? I think every leaf spec has to be binary by nature. I'm not 100% that's true
  - CON: disallows additional failure info if available
    - example, a pattern match might be able to indicate what part of the string failed the pattern
    - ALT: This could also be accomplished in the formatter.

I don't think the error is actually a list. Just like a spec always boils down to a single top-level structure,
the error will do the same so long as it's respecting the hierarchy and not just listing failed leafs
- I don't think we can just list failed leafs because or condition errors only make sense if you list all the options that failed

IDEA: What if I don't exclude any results from the validation structure? I return a hierarchy of results
- This would pose challenges for typing, the type of the combinator results would be different from the leafs
  - I could solve this by giving it it's own tree structure
- PRO: More information for printing messages
  - e.g. Could print "Validated 11 and got: min 5 (OK) and max 10 (FAIL)"
- CON: More cases for formatters to handle
  - relatively minimal. Expecially if I was going to include all children of the combinators
- Again, comes back to a separate data structure



!!! I probably want both success and failure cases to contain their spec value
- It seems the result structure is used for analysing what happened. If I want to support cases like showing what passed and what failed, then I need to know the spec in both failure and pass.
- Do I still want a function of shape `'a -> Result<'a, Errors>`?
  - I think explain is actually a more fundamental function than validate
  - Validate tests everything, validate then just passes back a value if the explanation is all OK and some error structure or the explanation if not
    - This way allows us to have the full explain, but have a different structure for validate's error if desired (e.g.)
    - most errors will probably just be passed to a explain formatter anyway. Trees are rather complex to handle another way
      - The formatter can always decide to ignore successes. It'd be pretty easy



## Key decisions thus far
- Explain is more fundamental than validate
- Explain preserves the structure of the spec, just indicates success or failure of each component
  - allows maximial information
- Validate could have a separate/derivative error structure than explain if needed
  - Probably should just be the explanation, since that's the first thing people will look to do with a failed result

## Data format

```fsharp
type Explanation<'a> =
    | Leaf of SpecResult<SpecLeaf<'a>>
    | Combinator of SpecResult<Combinator<'a> * (SpecResult<Explanation<'a>> list)>
```

Q: Reduction methods will be redundant with Spec. Can/should I refactor?
- I could have both of them base on tree, but that can make the type signatures weird. I think the bit of duplication is better than having the library's core type signatures cause any confusion

Q: How nicely does this handle for reduction?
- leaf will get a `Result<Leaf<>>`, that should provide all the info needed to format a message
- combinator would fit the current cata better if only the operator was wrapped with pass/fail and the children were separate

PROBLEM: the union cases of Spec and Explanation are clashing with each other

NOTE: I was expecting to nicely pattern match OK/Error against the explanation, but that doesn't really work...
- Is there anything I should do about this?

## Text format ideas

Q: Infix or prefix notation for combinators?
- Prefix
  - PRO: prefix notation seems more consistent, lets use that
- Infix
  - PRO: from my samples, infix seems much easier to read for low nesting and complexity
- I can always create multiple included formats

Decided: should parenthesize anything deeper than one combinator. 
- Probably even one deep if i'm using prefix notation

Q: Do empty combinators need a special case `"and ([no-criteria])"` or `"empty and"`
- prefix is feel more consistent in this case

I think leafs would be understandable with as just `name value`
- This won't be the most optimized 

Q: Should I consider normalizing ordering?
- i.e. if they put specify as max && min, should I put min first?
- A: I think mostly no, I should let them specify it how they like. It's easy enough for them to make their own formatter if they'd like. By default I should preserver their structure
  - this also applies for not normalizing to distributed and

Q: Are there special printing cases i'd want?
- ranges: e.g. `0 < fieldName < 10`
- exact vs one of list
  - e.g. `expected 0 < i < 10 or -5` (prefix `expected one of [(and [min 0, max 10]), -5]
  - could differentiate by if there's only one value?

Q: Potential alternative included formatters
- Distributed and -> I think displaying choices for input feedback would be easiest to understand as strict alternatives
  - this really just comes down to a special tree format, and maybe returning as a list rather than a single string


It feels like the text formatter is on the cusp of easy sub-ability. But there doesn't seem to be much left over if I pass in the changeable bits
 
It'd be nice to have the status printing all decided at combinator level, but it complicates the reduction, leaving us with a top-level result

PROBLEM: TODO: it's become apparent that the value should be an intrinsic part of the explanation. It doesn't make any sense to pass a value to the formatter. It allows us to pass a value that the explanation doesn't apply to