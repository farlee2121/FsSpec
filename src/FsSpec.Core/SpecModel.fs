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
    let predicate description pred : Spec<'a> = Spec.SpecLeaf (Custom (description, pred))
    let (&&&) left right = Spec.Combinator (And, [left; right])
    let (|||) left right = Spec.Combinator (Or, [left; right])
    let all specs = Spec.Combinator (And, specs)
    let any specs = Spec.Combinator (Or, specs)
    let internal none = Spec.SpecLeaf SpecLeaf.None
    let is<'a> : Spec<'a> = none

    let internal trimEmptyBranches spec =
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

     