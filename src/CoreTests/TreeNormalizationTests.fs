module TreeNormalizationTests

open Expecto
open FsSpec.CustomTree
open GeneratorExperiment
open FsCheck
open System
open Swensen.Unquote

type CustomArb =
    static member IComparableInt() = 
        Arb.generate<int>
        |> Gen.map (fun i -> i :> IComparable<int>)
        |> Arb.fromGen

    static member Regex() =
        Arb.generate<Guid>
        |> Gen.map (string >> System.Text.RegularExpressions.Regex)
        |> Arb.fromGen


let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<CustomArb>] } name test

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

    testProperty' "AND groups contain no combinators (tree is 3 deep)" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
        Constraint.depth normalized =! 3
]