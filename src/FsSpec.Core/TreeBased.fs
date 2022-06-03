module TreeBased
open TreeModel
open System

module DefaultValidations = 
    let validateMax (value) (max:IComparable<'a>) = 
        match max.CompareTo(value) >= 0 with
        | true -> Ok value
        | _ -> Error [$"{value} is greater than the max {max}"]

    let validateMin value (min:IComparable<'a>) = 
        match min.CompareTo(value) <= 0 with
        | true -> Ok value
        | _ -> Error [$"{value} is less than the min {min}"]

    let validateRegex value (regex: System.Text.RegularExpressions.Regex)=
        try
            match value :> System.Object with
            | :? System.String as str ->
                match regex.IsMatch(str) with
                | true -> Ok value
                | false -> Error [$"{value} didn't match expression {regex}"]
            | _ -> Error ["Invalid "]
        with
        | e -> Error [$"Cast to Object failed with exception: {e.Message}"]


    // these are really just the "and" and "or" operations for a result type. Would probably be better to create parameterized versions
    // then build up my specific case
    let validateAnd childResults = 
        let combine left right =
            match (left, right) with 
            | Ok _, Ok _ -> left
            | Ok _, Error err -> Error err
            | Error err, Ok _ -> Error err
            | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
        childResults |> List.reduce combine

    let validateOr childResults = 
        let combine left right =
            match (left, right) with 
            | Ok _, Ok _ -> left
            | Ok ok, Error _ -> Ok ok
            | Error err, Ok ok -> Ok ok
            | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
        childResults |> List.reduce combine


type Constraints<'a> = 
    | Max of IComparable<'a> 
    | Min of IComparable<'a>
    | Regex of System.Text.RegularExpressions.Regex
    // probably want to include some kind of "meta" field so that custom types can do things like make specific contraint-definition time values available to formatters
    // for example: customMax 20 would be ("customMax", {max: 20}, (fn value -> value <= 20)) with formatter | Custom ("customMax", meta, _) -> $"max {meta.max}" 
    | Custom of (string * ('a -> bool)) 
    // o

type Combinators<'a> = | And | Or

//never likely to or a two types, static languages don't play nice with that. The right way would to use a union for the or behavior
type Constraint<'a> = Tree<Constraints<'a>,Combinators<'a>>

let max m = Constraint.LeafNode(Max m)
let min m = Constraint.LeafNode (Min m)
let regex pattern = Constraint.LeafNode (Regex (System.Text.RegularExpressions.Regex(pattern)))
let matches expr = Constraint.LeafNode (Regex expr)
// cand /cor?
let (&&&) left right = Constraint.InternalNode (And, [left; right])
let (|||) left right = Constraint.InternalNode (Or, [left; right])

let validate constraintTree value = 
    let fLeaf (op, res) leaf =
        let leafResult = 
            match leaf with // this case is a lot nicer feeling than piping a bunch of functions. Reads better
            | Max max -> DefaultValidations.validateMax value max
            | Min min -> DefaultValidations.validateMin value min
            | Regex expr -> DefaultValidations.validateRegex value expr
            | Custom(_, pred) -> match pred value with | true -> Result.Ok value | false -> Result.Error ["nya"]
        match op with
        | And -> (op, DefaultValidations.validateAnd [res; leafResult])
        | Or -> (op, DefaultValidations.validateOr [res; leafResult])

    let fCombinator (_, res) newOp = (newOp, res)
    let (_, result) = Tree.fold fLeaf fCombinator (And, Ok value) constraintTree
    result

let test = min 1 &&& max 10

let r = validate test 7

let test2 = regex "\d{4}"
let r2 = validate test2 "4445"