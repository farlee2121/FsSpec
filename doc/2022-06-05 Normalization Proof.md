Theorem: Any boolean expression can be normalized as a series of AND expressions (or single variables) separated by ORs

The most basic case would be a single variable. This trivially satisfies the theorem

```
A
``` 

Similarly, any combination of only AND expressions trivially satisfies the theorem. By distributivity, no grouping of AND statements changes the logical semantic.

```
A1 AND A2 AND A3 ...
```

Similarly, any combination of OR expressions satisfies the theorem. By distributivity, any grouping of the or clauses is logically equivalent.
```
A1 OR A2 OR A3 ...
```


Let us suppose we have an expression of mixed operators. By commutativity, order of terms doesn't matter. There are only two distinct cases.

CASE: `A1 OR (A2 AND A3)`

This case trivially satisfies the theorem.

CASE: `A1 AND (A2 OR A3)`

By distributivity, this case can be re-written as `(A1 AND A2) OR (A1 AND A3)`. This for satisfies the theorem.

Thus any expression of two operators satisfies the theorem.

Now consider the inductive step

<!-- Could reduce cases by pointing out the above expressions Are equivalent to A1 && A2 and A1 || A2, only need to show 4 cases, but we've already shown all four of those cases work -->

CASE: `A1 OR (A2 AND A3)` where A3 is `(B1 AND B2)`.
```
A1 OR (A2 AND (B1 AND B2)) = A1 OR (A2 AND B1 AND B2)
``` 

CASE: `A1 OR (A2 AND A3)` where A3 is `(B1 OR B2)`.
```
A1 OR (A2 AND (B1 OR B2)) 
= A1 OR ((A2 AND B1) OR (A2 AND B2)) 
= A1 OR (A2 AND B1) OR (A2 AND B2)`
```

CASE: `A1 OR (A2 OR A3)` where A3 is `(B1 OR B2)`.  
This is an expression of all OR operations, thus satisfies the theorem.

CASE: `A1 OR (A2 OR A3)` where A3 is `(B1 AND B2)`.
```
A1 OR (A2 OR (B1 AND B2)) = A1 OR A2 OR (B1 AND B2)
``` 

CASE: `A1 AND (A2 AND A3)` where A3 is `(B1 AND B2)`
```
A1 AND (A2 AND (B1 AND B2)) = A1 AND A2 AND B1 AND B2
```

CASE: `A1 AND (A2 AND A3)` where A3 is `(B1 OR B2)`
```
A1 AND (A2 AND (B1 OR B2)) 
= A1 AND ((A2 AND B1) OR (A2 AND B2)) 
= (A1 AND A2 AND B1) OR (A1 AND A2 AND B2)
```

CASE: `A1 AND (A2 OR A3)` has already been shown to satisfy `(A1 AND A2) OR (A1 AND A3) = C1 OR C2`. 
We've already shown that all substitution in an OR statement satisfy the theorem.

Due to commutativity, this is an exhaustive set of distinct cases.

By induction any expression can substitute a variable for a two parameter sub-expression and still satisfy the theorem.
Any boolean expression can be created by different combinations of these substitutions. Therefore any boolean expression can be reduced to a series of AND groups separated by OR operators.
Therefore any 

QED

LEMMA: Any boolean expression can be created by expanding a term into a sub expression of two terms.

This feels tedious to prove, but I'm confident it's true because we can express any set of conditions as a binary tree (or as a series of 2-input logic gates)
It also makes intuitive sense since every expression must reduce to a true or false.
We can always produce a logically equivalent series of statements on two terms for any statement on more than two terms.
