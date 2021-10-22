module StaticlyTypedProto
open TreeModel

type Contraint<'a> = 
|
// an immediate problem I see is that, unlike elmish, the more detailed constraints depend on the more generic constraints.
// I need to flip the hierarchy, but still want specific types to respect 
// could I use an interface to operate on general constraints categories? Not really, operations need to be composed from component types just the 
// data structures. However, I can't have general combinators like AND/OR and composed types without some basic shared type. In the end, i'd have to violate 
// liskov substitution... unless... the inteface could include operations that output to some shared format, but that requisitely break the separation of data structure
// and operations

type Contraints = 
    | Max of int
    | Min of int
    | Regex of string

type Combinators = | And | Or

let max m = LeafNode (Max m)
let min m = LeafNode (Min m)
let matches expr = LeafNode (Regex expr)
let (&&&) left right = InternalNode (And, [left; right])
let (|||) left right = InternalNode (Or, [left; right])

let validate constraintTree value= 
    let fLeaf (op, res) leaf =
        let leafResult = 
            match leaf with // this case is a lot nicer feeling than piping a bunch of functions. Reads better
            | Max max -> DefaultValidations.validateMax value max
            | Min min -> DefaultValidations.validateMin value min
            | Regex expr -> DefaultValidations.validateRegex value expr
        match op with
        | And -> (op, DefaultValidations.validateAnd [res; leafResult])
        | Or -> (op, DefaultValidations.validateOr [res; leafResult])

    let fCombinator (_, res) newOp = (newOp, res)
    let (_, result) = Tree.fold fLeaf fCombinator (And, Ok value) constraintTree
    result