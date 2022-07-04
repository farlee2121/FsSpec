
## Motivation

Users may commonly want to allow or disallow specific values.

For example
```fsharp
all [ is<string>; maxLength 100; notEqual [null; ""] ] 


// or a nullable string
or [
    all [ is<string>; minLength 1; maxLength 100;]
    equals null
]
```

## Exploration
Q: Should I require equality?
- all objects have .Equals unless explicitly annotated with NoEquality
- I may need to worry about null

STUCK: How do I compare equality without requiring the equality constraint
- equality doesn't have an interface, so I can't match on a type test
- could I use an active expression to fix my issue?


## Tasks
- [ ] add matching a value leaf type
  - [ ] test validation of value match
  - [ ] generate (choosing from the discreet set)
- [ ] add not equals leaf type
  - [ ] test validation
  - [ ] generate (use as a filter)
- [ ] publish new version
- [ ] empty values list as impossible constraint


Later
- [ ] consider allowing a custom equality comparer
  - this would be especially helpful for types like float, where precision is iffy and equality is relative
  - this also introduces some potential challenges for serialization, generation, and general programmatic interpretation