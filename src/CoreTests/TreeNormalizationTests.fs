module TreeNormalizationTests

open Expecto
open FsSpec.CustomTree
open GeneratorExperiment
open FsCheck
open System

type CustomArb =
    static member IComparableInt() = 
        Arb.generate<int>
        |> Gen.map (fun i -> i :> IComparable<int>)
        |> Arb.fromGen

    static member Regex() =
        Arb.generate<Guid>
        |> Gen.map (string >> System.Text.RegularExpressions.Regex)
        |> Arb.fromGen


let testProperty' = testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<CustomArb>] }

[<Tests>]
let tests = testList "Constraint Tree Normalization" [
    testProperty' "Top layer is always AND" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
         
        match normalized with
        | Combinator (And, _) -> true
        | _ -> false

    testProperty' "Second layer is always OR" <| fun (tree: Constraint<int>) ->
        let normalized = Constraint.normalizeToDistributedAnd tree
         
        match normalized with
        | Combinator (And, [(Combinator (OR,_))]) -> true
        | _ -> false
]