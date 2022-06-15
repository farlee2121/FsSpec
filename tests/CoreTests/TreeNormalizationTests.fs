module TreeNormalizationTests

open Expecto
open FsCheck
open FsSpec
open Swensen.Unquote
open CustomGenerators
open Force.DeepCloner


let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultSpecArbs>] } name test

[<Tests>]
let depthTests = testList "Tree depths tests" [
    testProperty' "Depth always more than zero" <| fun (spec: Spec<int>) ->
        Spec.depth spec >! 0
    testProperty' "Leaf always depth 1" <| fun (leaf: SpecLeaf<int>) ->
        (Spec.depth (SpecLeaf leaf)) =! 1
    testProperty' "Any combinator without children has depth 1" <| fun (comb: Combinator<int>) ->
        Spec.depth (Combinator (comb, [])) =! 1
    testProperty' "Any combinator with children has depth at least 2" 
    <| fun (comb: Combinator<int>, children: NonEmptyArray<Spec<int>>) ->
        let children = List.ofArray children.Get
        Spec.depth (Combinator (comb, children)) >=! 2
    testProperty' "N nested combinators always N deep" <| fun (combinators: NonEmptyArray<Combinator<int>>) ->
        let head::tail = combinators.Get |> List.ofArray 
        let spec = tail
                   |> List.fold (fun agg c -> Spec.Combinator (c,[agg])) (Combinator (head, []))
        Spec.depth spec =! combinators.Get.Length
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
        | SpecLeaf lLeaf, SpecLeaf rLeaf ->
            compareLeafs lLeaf rLeaf
        | _ -> false
    recurse left right

[<Tests>]
let treeEquality = testList "Tree equal" [
    testProperty' "Any tree equals itself" <| fun (spec: Spec<int>) ->
        treeEqual spec spec

    testProperty' "Equality is structural, not referential" <| fun (spec: Spec<int>) ->
        treeEqual spec (spec.DeepClone())
        
]

[<Tests>]
let trimTests = testList "Trim Empty Branches" [
    testProperty' "Leaf trims to itself" <| fun (spec: CustomGenerators.LeafOnly<int>) ->
        let spec = spec.Spec
        test <@ treeEqual (Spec.trimEmptyBranches spec) spec @>

    testProperty' "Leafless trees return None" <| fun (tree: CustomGenerators.LeaflessSpecTree<int>) ->
        test <@ treeEqual (Spec.trimEmptyBranches tree.Spec) (SpecLeaf SpecLeaf.None) @>

    testProperty' "Combinator with only leafs is unchanged" <| fun (root:Combinator<int>, leafs:NonEmptyArray<SpecLeaf<int>>) ->
        let tree = (Combinator (root, leafs.Get |> List.ofArray |> List.map SpecLeaf))
        test <@ treeEqual (Spec.trimEmptyBranches tree) tree @>

    testProperty' "Combinator with mixed leaves and empty combinators returns only Leafs" 
    <| fun (root:Combinator<int>, leafs:NonEmptyArray<SpecLeaf<int>>, emptyBranches: NonEmptyArray<Combinator<int>>) ->
        let expectedChildren = leafs.Get |> List.ofArray |> List.map SpecLeaf
        let emptyBranch op = Combinator (op, [])
        let spec = 
            (Combinator (root, List.concat [
                expectedChildren
                emptyBranches.Get |> List.ofArray |> List.map emptyBranch
            ]))
        spec 
        |> Spec.trimEmptyBranches
        |> Spec.getChildren
        |> List.forall2 treeEqual expectedChildren

    testPropertyWithConfig 
        {FsCheckConfig.defaultConfig with arbitrary = [typeof<AllListsNonEmpty>; typeof<DefaultSpecArbs>]}
        "Combinator with non-empty combinators remains unchanged" <| fun (tree: Spec<int>)  ->
            test <@ treeEqual (Spec.trimEmptyBranches tree) tree @>
]


[<Tests>]
let tests = testList "Spec Tree Normalization" [
    testProperty' "Top layer is always OR" <| fun (tree: Spec<int>) ->
        let normalized = Spec.normalizeToDistributedAnd tree
         
        match normalized with
        | Combinator (Or, _) -> true
        | _ -> false

    testProperty' "Second layer is always AND" <| fun (tree: Spec<int>) ->
        let normalized = Spec.normalizeToDistributedAnd tree
         
        let isAnd = (function | (Combinator (And,_)) -> true | _ -> false)
        match normalized with
        | Combinator (Or, children) -> children |> List.forall isAnd
        | _ -> false

    testProperty' "AND groups contain no combinators (spec tree is 3 deep)" <| fun (spec: GuaranteedLeafs<int>) ->
        let normalized = Spec.normalizeToDistributedAnd spec.Spec
        test <@ Spec.depth normalized = 3 @>

    testProperty' "Any spec without leaves (combinators-only) normalizes to a single form" <| fun (spec: LeaflessSpecTree<int>) ->
        let normalized = Spec.normalizeToDistributedAnd spec.Spec
        match normalized with 
        | (Combinator (Or, [Combinator (And, [SpecLeaf SpecLeaf.None])])) -> ()
        | other -> failtest $"Expected default empty spec, got {other}"

    testProperty' "Original and normalized expressions are logically equivalent" <| fun (spec: Spec<int>) ->
        let normalized = Spec.normalizeToDistributedAnd spec
        Check.QuickThrowOnFailure <| fun (i:int) ->
            test <@ Spec.isValid normalized i = Spec.isValid spec i @>


    testProperty' "Normalization is idempotent" <| fun (spec: Spec<int>) ->
        let normalized = Spec.normalizeToDistributedAnd spec
        treeEqual normalized (Spec.normalizeToDistributedAnd normalized)

    testProperty' "Any normal-form spec remains unchanged" <| fun (leafGroups: NonEmptyArray<NonEmptyArray<SpecLeaf<int>>>) ->
        // Subtly different than idempotence. Idempotence can be achieved by always returning the same value from the normalizer
        let normalTree = 
            leafGroups.Get |> List.ofArray
            |> List.map (fun l -> l.Get |> List.ofArray)
            |> List.map (List.map SpecLeaf)
            |> List.map Spec.all
            |> Spec.any
        treeEqual normalTree (Spec.normalizeToDistributedAnd normalTree)

]