# Design Scratch

Motivation: Many expectations on data are left implicit. For example, expecting strings to be valid emails. This leaves important program and business expectations up to developer knowledge of implicit expectations. It's ripe for error, but happens often because defensively enforcing contracts can be difficult and awkward.

Here are some sources that explore the issue
- https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/
- https://www.martinfowler.com/apsupp/spec.pdf
- https://clojure.org/about/spec

This library hopes to elevate type constraints as central concept of our domain definitions. This requires
- A standard approach to make constraints easy to find and comprehend
- Powerful reuse
  - Compose advanced constraints from other constraints
  - One definition enables composition, data validation, validation error messages, data generation, and program correctness verification
- Consistent enforcement of constraints


## MVP

REQ: Validate simple types based on a set of constraints
REQ: Validate composite types automatically based on their component types
REQ: Generate sample data based on value spec constraints
REQ: Spec definitions are decoupled from implementations 
REQ: Necessary type constraints
- strings: min length, max length, regex
- numbers: min value, max value
- collections: min length, max length, member constraints, uniqueness
- all types: allowed values, AND with other constraints, OR with other constraints

GOAL: Spec definitions are extensible with custom constraints

## V1
REQ: allow controlled instantiation of constrained types (prevent instances that do not satisfy expected constraints)
REQ: Provide an explanation for why a value is invalid
REQ: Allow customized error messages for any failed constraint
- Includes custom formatting of composed constraints (i.e. and/or messages) 
REQ: Validate multiple constraints either monadically or applicatively
REQ: FsCheck integration for sample generation
- REQ: Use FsCheck to generate sample data requested via FsSpec api
- GOAL: FsCheck property tests respect FsSpec constraints by default, or there is an easy path to configuring such

GOAL: Clean DSL for defining specs
GOAL: Single expression spec definitions (no separation of type, constraint, and operation definitions. All wrapped up in the spec DSL)
GOAL: Simple overloading of existing validation behaviors.
- E.g. modify official implementations without needing to rebuild from components
GOAL: Simple overloading of existing generation behaviors

## Future
GOAL: Explore custom spec definition extensions
- e.g. constraint expressions

GOAL: Instrument code: run constraint-based property tests against functions in a code base
- GOAL: config for instrumentation inclusion/exclusion rules
- GOAL: instrument from REPL
- REQ: instrument from command line
- REQ: output clear, readable results
- REQ: allow multiple levels of report verbosity
- REQ: optional output artifact that can be consumed in automated workflows / kept as record

GOAL: Explore performance improvements by supporting value types
- idea: maybe leverage aliases and a static analyzer to add compile errors that circumvent the spec

GOAL: Explore performance improvements via eliminating reflection / moving meta-programming to compile-time

GOAL: Generate typescript validators to prevent code duplication in UIs

Possible: Explore more strict Design by Contract enforcement. Perhaps at the function level