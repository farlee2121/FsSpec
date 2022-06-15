namespace FsSpec

open System

[<AutoOpen>]
module Data =
    type SpecLeaf<'a> = 
        | None
        | Max of IComparable<'a> 
        | Min of IComparable<'a>
        | Regex of System.Text.RegularExpressions.Regex
        // probably want to include some kind of "meta" field so that custom types can do things like make specific contraint-definition time values available to formatters
        // for example: customMax 20 would be ("customMax", {max: 20}, (fn value -> value <= 20)) with formatter | Custom ("customMax", meta, _) -> $"max {meta.max}" 
        | Custom of (string * ('a -> bool))

    module SpecLeaf = 
        let isMax = (function | Max _ -> true | _ -> false)
        let isMin = (function | Min _ -> true | _ -> false)
        let isRegex = (function | Regex _ -> true | _ -> false)
        let isNone = (function | None -> true | _ -> false)

    type Combinator<'a> = | And | Or

type Spec<'a> =
    | SpecLeaf of SpecLeaf<'a>
    | Combinator of Combinator<'a> * Spec<'a> list

module Spec = 
    // Someone has to have made a version of this that is properly tail recursive...
    let rec cata fLeaf fNode (spec:Spec<'a>) :'r = 
        let recurse = cata fLeaf fNode  
        match spec with
        | Spec.SpecLeaf leafInfo -> 
            fLeaf leafInfo 
        | Spec.Combinator (nodeInfo,subtrees) -> 
            fNode nodeInfo (subtrees |> List.map recurse)

    let rec fold fLeaf fNode acc (spec:Spec<'a>) :'r = 
        let recurse = fold fLeaf fNode  
        match spec with
        | Spec.SpecLeaf leafInfo -> 
            fLeaf acc leafInfo 
        | Spec.Combinator (nodeInfo,subtrees) -> 
            let localAccum = fNode acc nodeInfo
            let finalAccum = subtrees |> List.fold recurse localAccum 
            finalAccum 

    
    let max m = Spec.SpecLeaf(Max m)
    let min m = Spec.SpecLeaf (Min m)
    let regex pattern : Spec<string> = Spec.SpecLeaf (Regex (System.Text.RegularExpressions.Regex(pattern)))
    let matches expr = Spec.SpecLeaf (Regex expr)
    // cand /cor?
    let (&&&) left right = Spec.Combinator (And, [left; right])
    let (|||) left right = Spec.Combinator (Or, [left; right])
    let all specs = Spec.Combinator (And, specs)
    let any specs = Spec.Combinator (Or, specs)
    let internal none = Spec.SpecLeaf SpecLeaf.None
    let is<'a> : Spec<'a> = none

    let trimEmptyBranches spec =
        let isEmptyCombinator = function
            | Combinator (_, []) -> true
            | _ -> false

        let fLeaf leaf = SpecLeaf leaf
        let fBranch comb children = 
            Combinator (comb, children |> List.filter (not << isEmptyCombinator))
        let trim = cata fLeaf fBranch

        let trimmed = trim spec 
        if isEmptyCombinator trimmed then none else trimmed

    let validate spec value = 
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

        spec |> trimEmptyBranches |> cata fLeaf fComb

    let isValid spec value =
        match validate spec value with
        | Ok _ -> true
        | Error _ -> false

    let depth (specTree:Spec<'a>) =
        let rec recurse subtree = 
            match subtree with
            | SpecLeaf _ ->  1
            | Combinator (_, children) as c -> 
                1 + (children |> List.map recurse 
                    |> (function | [] -> 0 | l -> List.max l))
        recurse specTree

    let getChildren = function
        | SpecLeaf _ -> []
        | Combinator (_, children) -> children

    let private isLeaf = (function | SpecLeaf _ -> true | _ -> false)
    let private isOr = (function | Combinator (Or, _) -> true | _ -> false)
    let private distributeAnd (children:Spec<'a> list) =
        let (leafs, branches) = children |> List.partition isLeaf
        let (childOrs, childAnds) = branches |> List.partition isOr
        let listWrap x = [x]
        let mergedAndChildren = childAnds |> List.map getChildren |> List.concat |> List.append leafs
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

    let doWhile (state: 'a) (test:('a -> bool)) (iter:('a -> 'a))  : 'a = 
        let mutable _state = state
        while test(_state) do _state <- iter _state 
        _state
                
    let normalizeToDistributedAnd (spec:Spec<'a>) = 
        let normalizeEmpty = function
            | Combinator (_, []) -> any [all [none]]
            | c -> c

        let isNormal tree =
            match tree with
            | (Combinator (Or, children)) ->
                children |> List.forall (function 
                    | Combinator (And, children) -> children |> List.forall isLeaf 
                    | _ -> false)
            | _ -> false

        let distributeTop tree = 
            match tree with
            | SpecLeaf _ as leaf -> any[all[leaf]]
            | Combinator (And, children) -> distributeAnd children
            | Combinator (Or, children) -> 
                let andsDistributed =  
                    children
                    |> List.map (function 
                        | Combinator (And, andChildren) -> distributeAnd andChildren 
                        | c -> c) 
                // all child combinators are OR after distribution
                let (leafs, orBranches) = andsDistributed |> List.partition isLeaf
                let mergedOrChildren = orBranches |> List.map getChildren |> List.concat
                let wrappedLeafs = leafs |> List.map (fun c -> all [c])
                any (List.concat [mergedOrChildren; wrappedLeafs])

        doWhile (spec |> trimEmptyBranches |> normalizeEmpty) (not << isNormal) distributeTop
            

    let private notNormalized () = invalidOp "Spec tree is not normalized to distributed and"

    let private toAlternativeAndSpecs (spec:Spec<'a>) = 
        let normalized = (normalizeToDistributedAnd spec)
        match normalized with
        | Combinator (Or, andGroups) -> andGroups 
        | _ -> notNormalized ()

    let toAlternativeLeafGroups (spec:Spec<'a>) : SpecLeaf<'a> list list= 
        let tryGetAndChildren = (function | Combinator (And,leafs) -> leafs | _ -> notNormalized())
        let tryGetLeafs = (function |SpecLeaf leaf -> leaf | _ -> notNormalized())

        spec 
        |> toAlternativeAndSpecs 
        |> List.map tryGetAndChildren
        |> List.map (List.map tryGetLeafs)

        