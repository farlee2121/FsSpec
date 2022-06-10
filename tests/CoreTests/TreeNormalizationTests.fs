module TreeNormalizationTests

open Expecto
open FsSpec.CustomTree
open FsCheck
open Swensen.Unquote
open CustomGenerators
open Force.DeepCloner


let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultConstraintArbs>] } name test

[<Tests>]
let depthTests = testList "Tree depths tests" [
    testProperty' "Depth always more than zero" <| fun (tree: Constraint<int>) ->
        Constraint.depth tree >! 0
    testProperty' "Leaf always depth 1" <| fun (leaf: ConstraintLeaf<int>) ->
        (Constraint.depth (ConstraintLeaf leaf)) =! 1
    testProperty' "Any combinator without children has depth 1" <| fun (comb: Combinator<int>) ->
        Constraint.depth (Combinator (comb, [])) =! 1
    testProperty' "Any combinator with children has depth at least 2" 
    <| fun (comb: Combinator<int>, children: NonEmptyArray<Constraint<int>>) ->
        let children = List.ofArray children.Get
        Constraint.depth (Combinator (comb, children)) >=! 2
    testProperty' "N nested combinators always N deep" <| fun (combinators: NonEmptyArray<Combinator<int>>) ->
        let head::tail = combinators.Get |> List.ofArray 
        let tree = tail
                   |> List.fold (fun agg c -> Constraint<int>.Combinator (c,[agg])) (Combinator (head, []))
        Constraint.depth tree =! combinators.Get.Length
]

let treeEqual left right : bool =

    let compareLeafs left right = 
        match left, right with
        | Custom (llabel, _), Custom (rlabel, _) -> llabel = rlabel
        | Max l, Max r -> l = r
        | Min l, Min r -> l = r
        | Regex l, Regex r -> l.ToString() = r.ToString()
        | None, None -> true
        | _ -> false
        
    let rec recurse left right = 
        match left, right with
        | Combinator (lOp, lChildren), Combinator (rOp, rChildren) -> 
            (lOp = rOp) 
            && (lChildren.Length = rChildren.Length)
            && (List.forall2 recurse lChildren rChildren)
        | ConstraintLeaf lLeaf, ConstraintLeaf rLeaf ->
            compareLeafs lLeaf rLeaf
        | _ -> false
    recurse left right

[<Tests>]
let treeEquality = testList "Tree equal" [
    testProperty' "Any tree equals itself" <| fun (tree: Constraint<int>) ->
        treeEqual tree tree

    testProperty' "Equality is structural, not referential" <| fun (tree: Constraint<int>) ->
        treeEqual tree (tree.DeepClone())
        
]

[<Tests>]
let trimTests = testList "Trim Empty Branches" [
    testProperty' "Leaf trims to itself" <| fun (tree: CustomGenerators.LeafOnly<int>) ->
        let tree = tree.Constraint
        test <@ treeEqual (Constraint.trimEmptyBranches tree) tree @>

    testProperty' "Leafless trees return None" <| fun (tree: CustomGenerators.LeaflessConstraintTree<int>) ->
        test <@ treeEqual (Constraint.trimEmptyBranches tree.Constraint) (ConstraintLeaf ConstraintLeaf.None) @>

    testProperty' "Combinator with only leafs is unchanged" <| fun (root:Combinator<int>, leafs:NonEmptyArray<ConstraintLeaf<int>>) ->
        let tree = (Combinator (root, leafs.Get |> List.ofArray |> List.map ConstraintLeaf))
        test <@ treeEqual (Constraint.trimEmptyBranches tree) tree @>

    testProperty' "Combinator with mixed leaves and empty combinators returns only Leafs" 
    <| fun (root:Combinator<int>, leafs:NonEmptyArray<ConstraintLeaf<int>>, emptyBranches: NonEmptyArray<Combinator<int>>) ->
        let expectedChildren = leafs.Get |> List.ofArray |> List.map ConstraintLeaf
        let emptyBranch op = Combinator (op, [])
        let tree = 
            (Combinator (root, List.concat [
                expectedChildren
                emptyBranches.Get |> List.ofArray |> List.map emptyBranch
            ]))
        tree 
        |> Constraint.trimEmptyBranches
        |> Constraint.getChildren
        |> List.forall2 treeEqual expectedChildren

    testPropertyWithConfig 
        {FsCheckConfig.defaultConfig with arbitrary = [typeof<AllListsNonEmpty>; typeof<DefaultConstraintArbs>]}
        "Combinator with non-empty combinators remains unchanged" <| fun (tree: Constraint<int>)  ->
            test <@ treeEqual (Constraint.trimEmptyBranches tree) tree @>
]


[<Tests>]
let tests = testList "Constraint Tree Normalization" [
    testProperty' "Top layer is always OR" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
         
        match normalized with
        | Combinator (Or, _) -> true
        | _ -> false

    testProperty' "Second layer is always AND" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
         
        let isAnd = (function | (Combinator (And,_)) -> true | _ -> false)
        match normalized with
        | Combinator (Or, children) -> children |> List.forall isAnd
        | _ -> false

    testProperty' "AND groups contain no combinators (tree is 3 deep)" <| fun (tree: GuaranteedLeafs<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree.Constraint
        test <@ Constraint.depth normalized = 3 @>

    testProperty' "Any tree without leaves (combinators-only) normalizes to a single form" <| fun (tree: LeaflessConstraintTree<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree.Constraint
        match normalized with 
        | (Combinator (Or, [Combinator (And, [ConstraintLeaf ConstraintLeaf.None])])) -> ()
        | other -> failtest $"Expected default empty tree, got {other}"

    testProperty' "Original and normalized expressions are logically equivalent" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
        Check.QuickThrowOnFailure <| fun (i:int) ->
            test <@ Constraint.isValid normalized i = Constraint.isValid tree i @>


    testProperty' "Normalization is idempotent" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
        treeEqual normalized (Constraint.normalizeToDistributedAnd normalized)

    testProperty' "Any normal-form tree remains unchanged" <| fun (leafGroups: NonEmptyArray<NonEmptyArray<ConstraintLeaf<int>>>) ->
        // Subtly different than idempotence. Idempotence can be achieved by always returning the same value from the normalizer
        let normalTree = 
            leafGroups.Get |> List.ofArray
            |> List.map (fun l -> l.Get |> List.ofArray)
            |> List.map (List.map ConstraintLeaf)
            |> List.map Constraint.Factories.all
            |> Constraint.Factories.any
        treeEqual normalTree (Constraint.normalizeToDistributedAnd normalTree)

]