---
date: 2022-06-21
---

## Motivation
Length is common constraint for strings and collections. It is essential for core expectable use cases of a validation or generation library.


REQ: Validate string lengths
REQ: Validate collection lengths
REQ: Generate collections in a size range (lists, enumerables, arrays)
- GOAL: implement sized generation in a way that handles any IEnumerable
REQ: String generation obeys length, even when otherwise constrained (i.e. with regex)
REQ: Formatters handle length constraints
REQ: spec factories do not allow invalid specs (must be sequence type, length must be positive)


## Exploration

Known issue: Generation now depends on a package for core. I'll have to generate a core package (internal build) to develop generators

K: a max length less than min length is impossible
K: max length 0 is not quite impossible for a generator, but meaningless

Problem: isLeafValidForType is part of my FsCheck package, so it doesn't know about the update data structure, but it's used for tests on the core library. This causes tests to fail, and I can't add the new cases without updating the gen package's references to core
- OPT: move isLeafValidForType to core
  - seems to make the most sense. It's not really a gen-specific concern.


## TODO
- [x] add leaf types
- [x] add factories
- [x] handle in formatter
- [ ] test validation on stings
- [ ] test validation on collections
- [ ] Handle impossible combinations (i.e. max < min)
- [ ] verify string generation
- [ ] verify collection generation