module TreeNormalizationTests

open Expecto
open FsSpec.CustomTree
open GeneratorExperiment
open FsCheck
open System
open Swensen.Unquote
open Constraint.Factories
open TreeModel

module SpecialGenerators =
    let leafOnly<'a> = Arb.generate<ConstraintLeaf<'a>> 
                        |> Gen.map ConstraintLeaf
                        |> Arb.fromGen
    let noLeafs<'a> = 
        Arb.generate<Tree<Combinator<'a>, Combinator<'a>>> 
        |> Gen.map (fun opTree ->
            let reduceLeaf leaf = Combinator (leaf, []) 
            let reduceInternal op children = (Combinator (op, List.ofSeq children))
            let tree = Tree.cata reduceLeaf reduceInternal opTree
            tree)
        |> Arb.fromGen
    let guaranteedLeafs<'a> = 
        let leafGen = leafOnly<'a> |> Arb.toGen

        let internalGen = gen {
            let! op = Arb.generate<Combinator<'a>>
            let! guaranteedLeaves = leafGen.NonEmptyListOf()
            let! otherBranches = Arb.generate<Constraint<'a> list>
            let allChildren = [List.ofSeq guaranteedLeaves; otherBranches] |> List.concat
            return Combinator (op, allChildren)
        }

        Gen.oneof [leafGen; internalGen] |> Arb.fromGen

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

    testProperty' "AND groups contain no combinators (tree is 3 deep)" <| fun () ->
        Prop.forAll SpecialGenerators.guaranteedLeafs<int> <| fun tree ->
            let normalized = Constraint.normalizeToDistributedAnd tree
            test <@ Constraint.depth normalized = 3 @>

    testProperty' "Any tree without leaves (combinators-only) normalizes to a single form" <| fun () ->
        Prop.forAll SpecialGenerators.noLeafs<int> <| fun tree ->
            let normalized = Constraint.normalizeToDistributedAnd tree
            match normalized with 
            | (Combinator (Or, [Combinator (And, [])])) -> ()
            | other -> failtest $"Expected default empty tree, got {other}"

    testProperty' "Original and normalized expressions are logically equivalent" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
        Check.QuickThrowOnFailure <| fun (i:int) ->
            test <@ Constraint.validate normalized i = Constraint.validate tree i @>

]