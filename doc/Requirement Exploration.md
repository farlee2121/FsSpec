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

Note: I realized another use of Specification: classification
- https://blog.ploeh.dk/2010/08/25/ChangingthebehaviorofAutoFixtureauto-mockingwithMoq/
- the "isValid" can be used to classify inputs. These can be composed to check if data fall into any combination of type constraints ("and", "or", "not")
without actually needing to conform the type

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

GOAL: Spec inheritance / Implicit spec mapping
- REQ: Offer an operator for "upcasting" a spec to a less restrictive spec (e.g. a number 1 to 5 is a natural number)
- GOAL: allow more restrictive spec with an explicit child relationship be passed as an instance of the less restrictive spec
- GOAL: remove the need for explicit relationships to perform upcasting
- GOAL: allow users to configure mapping behavior (none, explicit, implicit, custom policy?)


Possible: Explore more strict Design by Contract enforcement. Perhaps at the function level




## Ideas

Dynamic DTOs: Mark Seemann comments about using dynamic object at the boundaries, then mapping into domain objects by convention https://blog.ploeh.dk/2011/05/31/AttheBoundaries,ApplicationsareNotObject-Oriented/
- This would drastically cut down on DTO definitions, but then we don't get any type assistance trying to define those objects or for generating API schema definitions
  - We might be able to generate schemas based on specifications
  - Hmm. I think workflows should often take in unvalidated versions of data. Handling incorrect input is still usually a domain activity.
  - Alt: Idea: maybe we could use type providers to generate unvalidated equivalents of specs and still get well-defined contracts
	- Could have type providers for different conventions like allowing any field to be empty, or cohersion from primitives
	- It doesn't have to stop at input. We could also generate persistable/output DTOs based off of specs. It should mostly be the same process.
	We hint at stronger guarantees for the output data (i.e. no un-modeled optionals)
- Dictionaries or expando objects could be used...