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
- Structs would be an efficient wrapper for primitive types. I'd need to consider how to keep the interface consistent across 

GOAL: Generate typescript validators to prevent code duplication in UIs

GOAL: Spec inheritance / Implicit spec mapping
- REQ: Offer an operator for "upcasting" a spec to a less restrictive spec (e.g. a number 1 to 5 is a natural number)
- GOAL: allow more restrictive spec with an explicit child relationship be passed as an instance of the less restrictive spec
- GOAL?: remove the need for explicit relationships to perform upcasting
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


## Language proposals of interest
https://github.com/fsharp/fslang-design/blob/main/RFCs/FS-1043-extension-members-for-operators-and-srtp-constraints.md
https://github.com/fsharp/fslang-design/blob/main/FSharp-5.0/FS-1071-witness-passing-quotations.md
Static abstract methods eventually coming from C# side
- Would allow extension methods on a class of types that may not be explicitly instantiated (basically like passing a module around)


I'm not sure it's the right tool. Since it would still require implementation of those methods for each concrete type, but it does bring a possible solution to mind
An interface with covariant extension methods (or module with covariant methods) could be a great tool for specs
- C# compatibility
- Consistent implementation by different wrapper types
  - structs, records, unions, classes, can all implement an interface and then be consumed the same way while leaving
  - could possibly even extend primitives, but that'd be a bad idea
- Interface can require the specific type to return it's constraints. All the general methods would be generic methods shared between specs
  - static members actually might be the way to go to avoid instance shenanigans 
- Relatively low-bar to implement.
- Easy and familiar to extend
- a Type provider (maybe quotations?) could be used to create a specialized syntax to simplify wrapping primitives and other types
  - ... I don't know that syntax will get any simpler than a single case union. The goal here would be more consistency and clarity of constraints






## Implementation Thoughts

Don't need type provider for terse spec syntax. A generic constructor backed with a struct or class would suffice. 

Hmm, that works for creating instances with constraints, then values could be constructed by the constraint/spec instance as described.
This would create a simple syntax, but probably makes static analysis weird. 

Inheritance of some generic doesn't improve declaration verbosity....

A downside of validation running on value instances is it dilutes typing guarantees. Not every instance of a class may have the same guarantees (e.g injection) messes with static checking

It seems constraints belongs to the spec type, but that makes generic operations across specs difficult.
Maybe generic operations require two args, the spec and the value...

Options for where validation can live
- on value instances
  - pro: simplest route to generic spec methods. Just invoke some interface
  - con: instances could have their constraint data structures messed with. I.e. not every instance of a spec type might behave the same, and that seems really bad
- on constraint class instances
  - Easy to share code between types, but it requires either passing the spec instance or accessing methods through each spec instance instead of
  directly leveraging shared definitions
  - I don't like accessing methods per spec instance. It opens up possibility for some shennaigans on overriding spec behavior.
  I'd rather the spec only return a data structure. Extension/modification is done by composing new functions that work on that constraint defintion structure
    - passing the spec instance, seems a bit redundant since each value should be typed according to its spec. This wouldn't be a problem if I could infer the spec type
	as a generic argument inferred from the value and somehow access the constraints from the static type
- statically as part of some type/module definition
  - con: difficult to share code between type definitions
  - I think this would work with abstract static members on interfaces
  - I know scott wlaschin linked to an explanation of type-class-like features using static members
    - this article https://fsharpforfunandprofit.com/posts/elevated-world-4//
  - I could also use sketchy reflection like seems to be the norm in C#
  - This thread mentioned some options like FsharpPlus https://github.com/fsharp/fslang-suggestions/issues/243
  - probably possible through type providers?
- DECIDED: The below exploration of what ideas belong where tells me that static definitions seem to most clearly fit the natural belonging of concepts
  - instances of specs would be acceptable, but require a conceptually redundant parameter, and a conceptually redundant declaration

  Some fundamental questions to grapple with
  - Q: Where does the constraint definition belong?
    - A: I think constraints belong to a named specification. Not to values
	- Q: so what type do values have?
	  - values will be intended as a specific named spec at a given time, but a given value could satisfy the conditions of multiple specs and thus be a valid value of multiple specs
	    - A: I think this means that values need to be typed by the expected spec
		- Q: The question then is how relationships between specs are handled
		  - I think an explicit conversion can be expected. Something like `specConvert<TargetSpec> value`. Of course this returns a result in case of conversion failure.
		  An implementation that throws an exception can also be provided. This gives us a clear symbol to search for when providing static analysis of conversion legitimacy
  - Q: Where do operations on the spec constraints live?
    - A: these seem like they are global to all specs



Q: how should I handle constraints that only apply to certain types?
- clojure spec, in practice, basically always starts with a type constraint
- I think we can require the constraint tree to be typed (generically)
  - some of the constraint methods might only work on certain types like `Constraint<string>`
  - Q: Can I add union cases for specific type parameters?
    - A: no, union cases are not open
  - IDEA: I could use elmish-style composition
    - what does that gain me? A constraint is never going to apply to more than one underlying type at a time
- high-level options
  - OPT: all constraints fall in one global bucket
    - pro: can reuse constraints between types
	- con: not all constraints make sense for all types 
	  - I may be able to mitigate that by constraining the functions, but an invalid structure would be possible
  - OPT: define constraints for every primitive family
    - definitions could multiply very quickly
	- might be able to mitigate by composing elmish-style, e.g. the int is really just a wraper around numeric,
	which in turn maps out IComparable, set, and similar constraint families
  - OPT: use an open structure, and not unions so that constraints can be more easily composed without
    - example: use inheritance to define named `Constraint<T>` types where T is the most generic type I can use
	  - ALT: constraints could just be predicates on some type, but that
	- con: this still allows invalid structures to be created (i.e. AND[int?; string?])
	- con: an open type makes extension more opaque
	- tyler: static interfaces?
  	- generic structures / operations would invoke the static methods?
  	- what is that static method? 
    	- It can't be a data structure, that gets us back to where we started.
      	- Unless it's a more loosely typed data structure, in which case we end up with somewhat duplicate parallel structures...
    	- boolean: doesn't preserve input
- idea: maybe I should try making it several ways. One that allows invalid combinations and is open, one that is closed and disallows invalid combinations
- Q: what is needed for type-limited constraints?
  - what 
- tyler idea: can we use type providers to ensure type-constraint compatibility at compile time?
  - I think probably
  - TODO: look for type provider that operates on F# as expressions/ast
  - TODO: make sure type provider-defined constraints can be composed (output of type provider can be used by later type provider)
  - Pro: compile-time checking
  - Pro: no split type hierarchies
  - Con: doesn't prevent us from writing bad specs in write assistance, but it will yell at us early (compile-time)
  - Q: I hadn't thought, how do we type the generic functions that consume the constraint? (e.g `validate spec value` what is the inferred type of value)
    - We could maybe infer valid generic overloads, but that get's messy... the spec could allow multiple primitives
    - Maybe: what if TypeProvider was a wrapper type with generic type for internal spec. still would need internal  spec to inherit from some higher level type.
    - One solution would be for the type-provider to output a kind that includes the valid overloads of validate, but then the implementations are again tied to a single spec and not generic. We might have to accept that. It seems the alternative is having very permissive top-level api functions
  ```fs 

    type AnimalAction = Run | Jump | Climb
    type Animal = {
      Name : String
      Actions : AnimalActions list
    }

    type AnimalSpecProvider = FsSpecProvider(AnimalSpec, Animal);
    let spec = AnimalSpecProvider()
    // holy smokes not sure what key combo I hit there-  sorry.
    // I need to probably get back to work here - but lets keep this going
    // I should get to work too. I'll be sure to commit it. Thanks a bunch! this was fun
    // AGREED :)  have a good day
    // you too
    // Spencer:  Commit this code when you get a chance and I'll see if I can dig into how we 
    // can do this via TypeProvider.  Still kicking it around in my brain.
    Assert.equals(true, Provider.check());

  ```
  meet.google.com/hyf-emop-tgv

Q: Philosophical question. Should I allow constraints that can never be satisfied?
- for example, (AND[int?; string?]) is never possible
  - hmm, but this kind of genericism is necessary for OR, and OR[int?; string?] is a very plausible scenario
  - I could try to type AND. Would F# be smart enought to pick the stronger type?


Q: How to handle error explanations?
- I think error explations should be separate from the data structure. Something we construct with a catamorphism based on the data structure
  - ISSUE: this means my prototypes are wrong. The return type should be a structure of violated constraints that gets passed into an explainer
	- the error should include the value that violated the constraint
- Q: what should the validation return look like?
  - OPT: just return a boolean, have explain re-evaluate the tree
  - 


IDEA: I could use quotations / expressions to construct expressive errors for custom functions
- something like unquote would be very nice. I could probably just use unquote
- this would be a default message formatter, not part of the validation flow. That gives a bit a bit more leeway for performance concerns


Constraints as a data structure
- I realize I implicitly have preferred a data structure for constraints separate from implementations. I haven't explained why
- pro: data structures are easy to extend
  - users can implement their own interpretations of the structures
  - users can build on the data structure to accommodate new cases
  - Easier to define a base set that can be stored and transmitted safely
  - Data structures can be analyzed and operated on


!!! I could use member constraints to create generalized methods on bespoke types
- https://theburningmonk.com/2011/12/f-inline-functions-and-member-constraints
- doesn't solve strong vs weak constraint types, but simplifies centralization of methods
- without resorting to higher-kinded types