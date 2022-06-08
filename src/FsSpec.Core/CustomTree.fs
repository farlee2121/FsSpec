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

type Combinator<'a> = | And | Or

type Constraint<'a> =
    | ConstraintLeaf of ConstraintLeaf<'a>
    | Combinator of Combinator<'a> * Constraint<'a> list

module Constraint = 
    // Someone has to have made a version of this that is properly tail recursive...
    let rec cata fLeaf fNode (tree:Constraint<'a>) :'r = 
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
        let validateAnd value childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok _, Error err -> Error err
                | Error err, Ok _ -> Error err
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            match childResults with
            | [] -> Ok value
            | _ -> childResults |> List.reduce combine

        let validateOr value childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok ok, Error _ -> Ok ok
                | Error err, Ok ok -> Ok ok
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            match childResults with
            | [] -> Ok value
            | _ -> childResults |> List.reduce combine

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

    let trimEmptyBranches tree =
        let isEmptyCombinator = function
            | Combinator (_, []) -> true
            | _ -> false

        let fLeaf leaf = ConstraintLeaf leaf
        let fBranch comb children = 
            Combinator (comb, children |> List.filter (not << isEmptyCombinator))
        let trim = cata fLeaf fBranch

        let trimmed = trim tree 
        if isEmptyCombinator trimmed then (ConstraintLeaf None) else trimmed

    let validate constraintTree value = 
        let fLeaf leaf = 
            match leaf with
            | None -> Ok value
            | Max max -> DefaultValidators.validateMax value max
            | Min min -> DefaultValidators.validateMin value min
            | Regex expr -> DefaultValidators.validateRegex value expr
            | Custom(_, pred) -> DefaultValidators.validateCustom value pred
        let fComb comb childResults = 
            match comb with
            | And -> DefaultValidators.validateAnd value childResults
            | Or -> DefaultValidators.validateOr value childResults

        constraintTree |> trimEmptyBranches |> cata fLeaf fComb

    let isValid constraintTree value =
        match validate constraintTree value with
        | Ok _ -> true
        | Error _ -> false

    let depth (tree:Constraint<'a>) =
        let rec recurse subtree = 
            match subtree with
            | ConstraintLeaf _ ->  1
            | Combinator (_, children) as c -> 
                1 + (children |> List.map recurse 
                    |> (function | [] -> 0 | l -> List.max l))
        recurse tree

    let getChildren = function
        | ConstraintLeaf _ -> []
        | Combinator (_, children) -> children

    let private isOr = (function | Combinator (Or, _) -> true | _ -> false)
    let private distributeAnd (children:Constraint<'a> list) =
        let (childOrs, childAnds) = children |> List.partition isOr
        let listWrap x = [x]
        let mergedAndChildren = childAnds |> List.map getChildren |> List.concat
        let orGroups = childOrs |> List.map getChildren |> List.filter (not << List.isEmpty) |> List.map (List.map listWrap)
        let cross set1 set2 = [for x in set1 do for y in set2 do yield [x;y]]

        let crossedOrGroups = 
            match orGroups with
            | [] -> []
            | head::tail -> tail |> List.fold (fun agg set -> (cross agg set) |> List.concat) head
                
        let fullyDistributedAllGroups = 
            match crossedOrGroups, mergedAndChildren with
            | [], [] -> []
            | [], r -> [r]
            | l, [] -> l
            | orGroups, mergedAnds -> orGroups |> List.map (List.append mergedAnds) 

        any (fullyDistributedAllGroups |> List.map all)
                
    let normalizeToDistributedAnd (constraints:Constraint<'a>) = 
        let normalizeEmpty = function
            | Combinator (_, []) -> any [all [ConstraintLeaf ConstraintLeaf.None]]
            | c -> c

        let fLeaf leaf = all [ConstraintLeaf leaf] 
        let fInternal op normalizedChildren = 
            match op with
            | Or ->
                let (childOrs, childAnds) = normalizedChildren |> List.partition isOr
                let mergedOrChildren = childOrs |> List.map getChildren |> List.concat
                any (List.concat [mergedOrChildren; childAnds])
            | And -> distributeAnd normalizedChildren

        let normalized = cata fLeaf fInternal (any [all[constraints |> trimEmptyBranches]] )
        normalized |> normalizeEmpty

    let private notNormalized () = invalidOp "Constraint tree is not normalized to distributed and"

    let private toAlternativeAndConstraints (constraintTree:Constraint<'a>) = 
        let normalized = (normalizeToDistributedAnd constraintTree)
        match normalized with
        | Combinator (Or, andGroups) -> andGroups 
        | _ -> notNormalized ()

    let toAlternativeLeafGroups (constraintTree:Constraint<'a>) : ConstraintLeaf<'a> list list= 
        let tryGetAndChildren = (function | Combinator (And,leafs) -> leafs | _ -> notNormalized())
        let tryGetLeafs = (function |ConstraintLeaf leaf -> leaf | _ -> notNormalized())
        constraintTree 
        |> toAlternativeAndConstraints 
        |> List.map tryGetAndChildren
        |> List.map (List.map tryGetLeafs)

        