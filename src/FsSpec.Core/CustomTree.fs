namespace FsSpec.CustomTree

open System

// hmm. I think these being separate isn't great for discoverability. It also means 
type ConstraintLeaf<'a> = 
    | None
    | Max of IComparable<'a> 
    | Min of IComparable<'a>
    | Regex of System.Text.RegularExpressions.Regex
    // probably want to include some kind of "meta" field so that custom types can do things like make specific contraint-definition time values available to formatters
    // for example: customMax 20 would be ("customMax", {max: 20}, (fn value -> value <= 20)) with formatter | Custom ("customMax", meta, _) -> $"max {meta.max}" 
    | Custom of (string * ('a -> bool)) 
    // o

type Combinator<'a> = | And | Or

type Constraint<'a> =
    | ConstraintLeaf of ConstraintLeaf<'a>
    | Combinator of Combinator<'a> * Constraint<'a> list

module Constraint = 
    // Someone has to have made a version of this that is properly tail recursive...
    let rec private cata fLeaf fNode (tree:Constraint<'a>) :'r = 
        let recurse = cata fLeaf fNode  
        match tree with
        | Constraint.ConstraintLeaf leafInfo -> 
            fLeaf leafInfo 
        | Constraint.Combinator (nodeInfo,subtrees) -> 
            fNode nodeInfo (subtrees |> List.map recurse)

    let rec fold fLeaf fNode acc (tree:Constraint<'a>) :'r = 
        let recurse = fold fLeaf fNode  
        match tree with
        | Constraint.ConstraintLeaf leafInfo -> 
            fLeaf acc leafInfo 
        | Constraint.Combinator (nodeInfo,subtrees) -> 
            let localAccum = fNode acc nodeInfo
            let finalAccum = subtrees |> List.fold recurse localAccum 
            finalAccum 

    module DefaultValidators = 
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

        let validateCustom value predicate = 
            match predicate value with 
            | true -> Result.Ok value 
            | false -> Result.Error ["nya"]


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

    [<AutoOpen>]
    module Factories = 
        let max m = Constraint.ConstraintLeaf(Max m)
        let min m = Constraint.ConstraintLeaf (Min m)
        let regex pattern : Constraint<string> = Constraint.ConstraintLeaf (Regex (System.Text.RegularExpressions.Regex(pattern)))
        let matches expr = Constraint.ConstraintLeaf (Regex expr)
        // cand /cor?
        let (&&&) left right = Constraint.Combinator (And, [left; right])
        let (|||) left right = Constraint.Combinator (Or, [left; right])
        let all constraints = Constraint.Combinator (And, constraints)
        let any constraints = Constraint.Combinator (Or, constraints)

        let is<'a> : Constraint<'a> = Constraint.ConstraintLeaf (ConstraintLeaf.None)

    let validate constraintTree value = 
        let fLeaf (op, res) leaf =
            let leafResult = 
                match leaf with // this case is a lot nicer feeling than piping a bunch of functions. Reads better
                | None -> Ok value
                | Max max -> DefaultValidators.validateMax value max
                | Min min -> DefaultValidators.validateMin value min
                | Regex expr -> DefaultValidators.validateRegex value expr
                | Custom(_, pred) -> DefaultValidators.validateCustom value pred
            match op with
            | And -> (op, DefaultValidators.validateAnd [res; leafResult])
            | Or -> (op, DefaultValidators.validateOr [res; leafResult])

        let fCombinator (_, res) newOp = (newOp, res)
        let (_, result) = fold fLeaf fCombinator (And, Ok value) constraintTree
        result
