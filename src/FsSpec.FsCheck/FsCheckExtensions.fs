﻿namespace FsSpec.FsCheck
open FsCheck
open FsSpec
open FsSpec.Normalization

module Spec =
    module Internal = 
    
        let isKnownImpossibleSpec (leafGroup: SpecLeaf<'a> list) = 
            let isMaxLessThanMin leafGroup =
                if typeof<System.IComparable<'a>>.IsAssignableFrom(typeof<'a>)
                then 
                    match (List.tryFind SpecLeaf.isMin leafGroup), (List.tryFind SpecLeaf.isMax leafGroup) with
                    | Some (Min (min)), Some (Max max) -> 
                        match max :> obj with 
                        | :? 'a as max -> min.CompareTo(max) > 0
                        | _ -> false
                    | _ -> false
                else false

            isMaxLessThanMin leafGroup 
            || leafGroup |> List.exists (not << Spec.Internal.isLeafValidForType)

        let containsImpossibleGroup spec = 
            spec 
            |> Spec.toAlternativeLeafGroups 
            |> List.exists isKnownImpossibleSpec

module Gen = 
            
    module Internal =
        let defaultGen spec =
            Arb.generate<'a>
            |> Gen.tryFilter (Spec.isValid spec)
            |> Gen.map Option.get

    let internal leafGroupToGen (andGroup:SpecLeaf<'a> list) : Gen<'a> =
        let leafGroupToAnd leafs =
            leafs |> List.map SpecLeaf |> Spec.all
        
        OptimizedCases.strategiesInPriorityOrder ()
        |> List.tryPick (fun f -> f andGroup) 
        |> Option.defaultWith (fun () -> Arb.generate<'a>)
        |> Gen.tryFilter (Spec.isValid (andGroup |> leafGroupToAnd))
        |> Gen.map Option.get

    let fromSpec (spec:Spec<'a>) : Gen<'a> =
        let andGroupGens = 
            spec 
            |> Spec.toAlternativeLeafGroups 
            |> List.filter (not << Spec.Internal.isKnownImpossibleSpec)
            |> List.map leafGroupToGen

        match andGroupGens with
        | [] -> invalidArg (nameof spec) "Spec is impossible to satisfy and cannot generate data"
        | validAndGroupGens -> Gen.oneof validAndGroupGens
        

module Arb =
    let fromSpec (spec:Spec<'a>) : Arbitrary<'a> =
        Gen.fromSpec spec |> Arb.fromGen


