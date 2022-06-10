namespace FsSpec

module Constraint =

    let trimEmptyBranches tree =
        let isEmptyCombinator = function
            | Combinator (_, []) -> true
            | _ -> false

        let fLeaf leaf = ConstraintLeaf leaf
        let fBranch comb children = 
            Combinator (comb, children |> List.filter (not << isEmptyCombinator))
        let trim = Constraint.cata fLeaf fBranch

        let trimmed = trim tree 
        if isEmptyCombinator trimmed then (ConstraintLeaf None) else trimmed

    let private isLeaf = (function | ConstraintLeaf _ -> true | _ -> false)
    let private isOr = (function | Combinator (Or, _) -> true | _ -> false)
    let private distributeAnd (children:Constraint<'a> list) =
        let (leafs, branches) = children |> List.partition isLeaf
        let (childOrs, childAnds) = branches |> List.partition isOr
        let listWrap x = [x]
        let mergedAndChildren = childAnds |> List.map Constraint.getChildren |> List.concat |> List.append leafs
        let orGroups = childOrs |> List.map Constraint.getChildren |> List.filter (not << List.isEmpty) |> List.map (List.map listWrap)
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

        Constraint.any (fullyDistributedAllGroups |> List.map Constraint.Factories.all)

    let private doWhile (state: 'a) (test:('a -> bool)) (iter:('a -> 'a))  : 'a = 
        let mutable _state = state
        while test(_state) do _state <- iter _state 
        _state
                
    let normalizeToDistributedAnd (constraints:Constraint<'a>) = 
        let normalizeEmpty = function
            | Combinator (_, []) -> Constraint.Factories.any [Constraint.Factories.all [ConstraintLeaf ConstraintLeaf.None]]
            | c -> c

        let isNormal tree =
            match tree with
            | (Combinator (Or, children)) ->
                children |> List.forall (function 
                    | Combinator (And, children) -> children |> List.forall isLeaf 
                    | _ -> false)
            | _ -> false

        doWhile (constraints |> trimEmptyBranches |> normalizeEmpty) (not << isNormal) <| fun state ->
            match state with
            | ConstraintLeaf _ -> Constraint.Factories.any[Constraint.Factories.all[state]]
            | Combinator (And, children) -> distributeAnd children
            | Combinator (Or, children) -> 
                let andsDistributed =  
                    children
                    |> List.map (function 
                        | Combinator (And, andChildren) -> distributeAnd andChildren 
                        | c -> c) 
                // all child combinators are OR after distribution
                let (leafs, orBranches) = andsDistributed |> List.partition isLeaf
                let mergedOrChildren = orBranches |> List.map Constraint.getChildren |> List.concat
                let wrappedLeafs = leafs |> List.map (fun c ->Constraint.Factories.all [c])
                Constraint.Factories.any (List.concat [mergedOrChildren; wrappedLeafs])

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
