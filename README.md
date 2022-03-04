# FsSpec

## What is Spec and why do I want it?

The specification pattern represents constraints in a way that can facilitate validation, classification, and generation.

Applying this constraint concept to the type system allows us to more clearly communicate, enforce, and leverage constraints inherent to our domain.

For example,
```fs
type Percent = int between 0.0 and 1.0
```

Enforcing these constraints in our type system reduces the amount of defensive programming required. This is often already accomplished with constructors.
Promoting these constraints to a more consistent member of our type system could reduce validation code, improve validation readability and consistency, and enable
new possibilities for automated correctness verification (i.e. generating values within constraints to see if any unexpected system states can be achieved).

## Inspiration
cite clojure.spec and Eric Evans spec document
https://clojure.org/about/spec
https://www.martinfowler.com/apsupp/spec.pdf
Design by Contract
Generative testing 
https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/
[Ada constrained numbers](https://en.wikipedia.org/wiki/Ada_(programming_language)#Data_types)

## Conclusion

Unfortunately, such an extension to the F# type system is not possible with type providers. Options without type providers provide no meaningful progress over existing techniques.

If you're interested in my investigations, they're all recorded in [doc](./doc/)