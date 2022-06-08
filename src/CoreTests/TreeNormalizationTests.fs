module TreeNormalizationTests

open Expecto
open FsSpec.CustomTree
open FsCheck
open Swensen.Unquote
open CustomGenerators



let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultCustomArb>] } name test

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

    testProperty' "AND groups contain no combinators (tree is 3 deep)" <| fun () ->
        Prop.forAll ConstraintArb.guaranteedLeafs<int> <| fun tree ->
            let normalized = Constraint.normalizeToDistributedAnd tree
            test <@ Constraint.depth normalized = 3 @>

    testProperty' "Any tree without leaves (combinators-only) normalizes to a single form" <| fun () ->
        Prop.forAll ConstraintArb.noLeafs<int> <| fun tree ->
            let normalized = Constraint.normalizeToDistributedAnd tree
            match normalized with 
            | (Combinator (Or, [Combinator (And, [ConstraintLeaf ConstraintLeaf.None])])) -> ()
            | other -> failtest $"Expected default empty tree, got {other}"

    testProperty' "Original and normalized expressions are logically equivalent" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
        Check.QuickThrowOnFailure <| fun (i:int) ->
            test <@ Constraint.validate normalized i = Constraint.validate tree i @>


    //testProperty' "Normalization is idempotent" <| fun (tree: Constraint<int>) ->
    //    let normalized = Constraint.normalizeToDistributedAnd tree
    //    normalized = (Constraint.normalizeToDistributedAnd normalized)

]




//let treeEqual left right : bool =
//    // the other option would be to make the custom branch it's own type so I can give it a custom equality override
//    // the problem is that two predicates of the same tag needn't be equal. They probably should be if I also split out meta
//    let leafToComparable leaf : obj =  
//        match leaf with
//        | Custom (name, _) -> name
//        | other -> other
//    let internalToComparable op children = children
//    let treeToComparable = Constraint.cata leafToComparable internalToComparable
//    (treeToComparable left) = (treeToComparable right)


//    let rec recurse tree =