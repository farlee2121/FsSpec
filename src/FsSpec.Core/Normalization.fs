namespace FsSpec.Normalization
open FsSpec

module Spec = 
    let getChildren = function
        | SpecLeaf _ -> []
        | Combinator (_, children) -> children

    let trimEmptyBranches spec = Spec.trimEmptyBranches spec

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

        Spec.any (fullyDistributedAllGroups |> List.map Spec.all)

    let doWhile (state: 'a) (test:('a -> bool)) (iter:('a -> 'a))  : 'a = 
        let mutable _state = state
        while test(_state) do _state <- iter _state 
        _state
                
    let normalizeToDistributedAnd (spec:Spec<'a>) = 
        let normalizeEmpty = function
            | Combinator (_, []) -> Spec.any [Spec.all [Spec.none]]
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
            | SpecLeaf _ as leaf -> Spec.any[Spec.all[leaf]]
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
                let wrappedLeafs = leafs |> List.map (fun c -> Spec.all [c])
                Spec.any (List.concat [mergedOrChildren; wrappedLeafs])

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

   

