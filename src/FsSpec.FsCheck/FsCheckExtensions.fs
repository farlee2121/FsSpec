namespace FsSpec.FsCheck
open FsCheck
open FsSpec

module Gen = 
            
    module Internal =
        let defaultGen constraintTree =
            Arb.generate<'a>
            |> Gen.tryFilter (Constraint.isValid constraintTree)
            |> Gen.map Option.get

        let isLeafValidForType (leaf:ConstraintLeaf<'a>) = 
            match leaf with
            | Regex _ as leaf -> 
                typeof<'a>.IsAssignableTo(typeof<string>)
            | Min _ | Max _ -> 
                typeof<'a>.IsAssignableTo(typeof<System.IComparable<'a>>)
            | Custom _ | None -> true
        


        let isKnownImpossibleConstraint (leafGroup: ConstraintLeaf<'a> list) = 
            let isMaxLessThanMin leafGroup =
                if typeof<'a>.IsAssignableTo(typeof<System.IComparable<'a>>)
                then 
                    match (List.tryFind ConstraintLeaf.isMin leafGroup), (List.tryFind ConstraintLeaf.isMax leafGroup) with
                    | Some (Min (min)), Some (Max max) -> 
                        match max :> obj with 
                        | :? 'a as max -> min.CompareTo(max) > 0
                        | _ -> false
                    | _ -> false
                else false

            isMaxLessThanMin leafGroup 
            || leafGroup |> List.exists (not << isLeafValidForType)

        let containsImpossibleGroup cTree = 
            cTree 
            |> Constraint.toAlternativeLeafGroups 
            |> List.exists isKnownImpossibleConstraint


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
            |> List.filter (not << Internal.isKnownImpossibleConstraint)
            |> List.map leafGroupToGen

        match andGroupGens with
        | [] -> invalidArg (nameof constraintTree) "Constraint is impossible to satisfy and cannot generate data"
        | validAndGroupGens -> Gen.oneof validAndGroupGens
        

module Arb =
    let fromConstraint (constraints:Constraint<'a>) : Arbitrary<'a> =
        Gen.fromConstraint constraints |> Arb.fromGen


