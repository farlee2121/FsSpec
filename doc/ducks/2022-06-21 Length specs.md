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


!!! today I learned about flexible type annotation `#IComparable` or `#System.Collections.IEnumerable`
- https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/flexible-types
- allows any type that derives from the given type, equivalent to `'a when 'a :> type-here`
- Q: Could this be used to make my comparable experience better?
  - doesn't look like it

Q: how do I generate arbitrary collection types?
- Q: would it be better to return mostly one collection type?
  - that would require me to have a separate generator endpoint. I'd prefer to keep the api uniform across all spec types
- Q: how does FsCheck do it?
  - mostly covered in `Internals.DefaultArbs.fs`, registers a bunch of different arbs for explicit types
  - I might not have to worry because FsCheck seems to set up collection arbs with (I think) implicit convertability
```fsharp
static member Set(elements: Arbitrary<'T>) = 
        Default.FsList(elements)
        |> Arb.convert Set.ofList Set.toList
```

FsCheck seems to do it via reflection
```fs
/// Returns a function that creates the given System.Collections.Immutable type,
/// with a single generic type parameter, from an array.
let getImmutableCollection1Constructor (t:Type) (elementType: Type) =
    let staticTypeName = t.GetGenericTypeDefinition().AssemblyQualifiedName.Replace("`1", "")
    let staticType = Type.GetType(staticTypeName, throwOnError=true)
    let createMethod = 
        staticType.GetRuntimeMethods()
        |> Seq.find(fun (mi:MethodInfo) -> 
                        let parameters = mi.GetParameters()
                        mi.IsPublic && mi.IsStatic && mi.Name = "Create" 
                        && parameters.Length = 1 && parameters.[0].ParameterType.IsArray)
    let genericCreateMethod = createMethod.MakeGenericMethod(elementType)
    fun arr -> genericCreateMethod.Invoke(null, [| arr |])

// in reflective gen
elif isImmutableCollectionType t then
    let genericArguments = t.GetTypeInfo().GenericTypeArguments
    if genericArguments.Length = 1 then
        let elementType = genericArguments.[0]
        let arrGen = elementType.MakeArrayType() |> getGenerator
        let make = getImmutableCollection1Constructor t elementType
        arrGen
        |> Gen.map make
        |> box
    elif genericArguments.Length = 2 then
        // Immutable(Sorted)Dictionary
        let dictGen = typedefof<Collections.Generic.Dictionary<_,_>>.MakeGenericType(genericArguments) |> getGenerator
        let make = getImmutableCollection2Constructor t genericArguments
        dictGen
        |> Gen.map make
        |> box
    else
        failwithf "Unexpected System.Collections.Immutable type: %s. This is a bug in FsCheck, please open an issue." t.AssemblyQualifiedName
```

The default arbs also explicitly handle many explicit collection types (List, IList, ICollection, Array, Dictionary, set, map)

PROBLEM: List generators should respect overrides for the element generators, but Optimized case generators don't currently have access to the arbmap

Stuck: why am I stuck? What are the key problems?
- I first need to be able to generate a collection, of any kind, from a type that I can currently only get via reflection
  - the public endpoints for creating arbs only support generic type parameters. 
    - This is not true of FsCheck3, which exposes an endpoint to get an arbitrary by a non-generic type argument
- I then need to be able to translate that collection to the spec'd collection type

limitations
- I can't handle unknown (i.e. custom) collection types with this cast approach
  - Could I reflectively find constructors accepting a list-like and invoke them (maybe using Activator? Maybe just invoke?). Then I could support any type that takes an IEnumerable in the constructor, or some supported subset of collection types in the constructor
  - A: DECIDED: I think I table this for now. the specs should generally be against the wrapped primitive system types. The custom type wraps the value and the spec, so it isn't (generally) subject to the spec itself

PICKUP: solve just strings, then move to just list, then figure out mapping to core collection types
- Ok. I can invoke the ranged list generator
- Now I can't cast back to a list of the given type since dynamic invoke returns obj
  - I could also dynamic invoke the converter
  - I could wrap the whole thing so it takes the element type as a generic param and then dynamic invoke

PROBLEM: my collection optimization is never matching because pattern matching does not support flexible typing beyond the top-level type
- Could I take some inspiration from TypeClass to solve this issue?
- Q: Rather than composing externally, should I keep a registry of collection type converters. Then the top method only cares about `'b` it doesn't need to dig deeper
- do I need to find a reduced problem?

There is a lot more in System.Collections that I realized: Generic,Immutable, Concurrent, readonly, specialized, sets, queues, map, dictionary, etc

A: Delegates, like Func or FsharpFunc, do not have a "free" type parameter. And cannot be passed for stronger typing over a method info

Q: Could I replace a type parameter to remake a generic method as a different type?
- A: it appears no. Once "MakeGenericMethod()" is applied and the method becomes a delegate, then there is no tracing back to the generic. It has become a new type


PROBLEM: The strategy either has to match on a more limited type, or has to handle all collection types under one strategy, else only the first assignable strategy will match


## TODO
- [x] add leaf types
- [x] add factories
- [x] handle in formatter
- [x] test validation on stings
- [x] test validation on collections
- [ ] Handle impossible combinations (i.e. max < min)
- [x] verify string generation
- [ ] verify collection generation
  - [ ] list
  - [x] FsharpList
  - [ ] Array
  - [ ] IList, ICollection
  - [ ] Set? Dictionary? Map? Queue?
  - [ ] NOTE: Some collections have multiple type parameters (i.e. dictionary, map)
- [ ] immutable collection generation (using generic approach used by FsCheck)
- [ ] make sure sized collections respect the element type generators


## Later?

- Handle collections that accept a supported collection type in their constructor
- Less common collections: Concurrent, readonly, specialized