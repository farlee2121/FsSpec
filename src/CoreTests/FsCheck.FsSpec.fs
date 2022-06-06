module GeneratorExperiment
open FsSpec.CustomTree
open Constraint.Factories

module Constraint = 
    let private tryGetChildren = function
        | ConstraintLeaf _ -> []
        | Combinator (_, children) -> children

    let private isOr = (function | Combinator (Or, _) -> true | _ -> false)
    let private distributeAnd (children:Constraint<'a> list) =
        let (childOrs, childAnds) = children |> List.partition isOr
        let listWrap x = [x]
        let mergedAndChildren = childAnds |> List.map tryGetChildren |> List.concat
        let orGroups = childOrs |> List.map tryGetChildren |> List.filter (not << List.isEmpty) |> List.map (List.map listWrap)
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
                let mergedOrChildren = childOrs |> List.map tryGetChildren |> List.concat
                any (List.concat [mergedOrChildren; childAnds])
            | And -> distributeAnd normalizedChildren

        let normalized = Constraint.cata fLeaf fInternal (any [all[constraints |> Constraint.trimEmptyBranches]] )
        normalized |> normalizeEmpty

//module Gen = 
//    open FsCheck

    //let fromConstraint (constraints:Constraint<'a>) : Gen<'a> =
    //   Gen.
    //   Arb.generate<'a> |> 
    //   Arb.


