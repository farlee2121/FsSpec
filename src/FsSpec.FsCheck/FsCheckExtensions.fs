namespace FsSpec.FsCheck
open FsCheck
open FsSpec

module Gen = 
            
    module Internal =
        let defaultGen constraintTree =
            Arb.generate<'a>
            |> Gen.tryFilter (Constraint.isValid constraintTree)
            |> Gen.map Option.get


    let internal leafGroupToGen (andGroup:ConstraintLeaf<'a> list) : Gen<'a> =
        let leafGroupToAnd leafs =
            leafs |> List.map ConstraintLeaf |> Constraint.all
        
        let defaultGen = andGroup |> leafGroupToAnd |> Internal.defaultGen
        OptimizedCases.strategiesInPriorityOrder ()
        |> List.tryPick (fun f -> f andGroup) 
        |> Option.defaultValue Arb.generate<'a>
        |> Gen.tryFilter (Constraint.isValid (andGroup |> leafGroupToAnd))
        |> Gen.map Option.get

    let fromConstraint (constraintTree:Constraint<'a>) : Gen<'a> =
        let andGroupGens = 
            constraintTree 
            |> Constraint.toAlternativeLeafGroups 
            |> List.map leafGroupToGen
        
        Gen.oneof andGroupGens
        

module Arb =
    let fromConstraint (constraints:Constraint<'a>) : Arbitrary<'a> =
        Gen.fromConstraint constraints |> Arb.fromGen


