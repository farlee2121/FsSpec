module GeneratorTests

open Expecto
open FsCheck
open FsSpec.CustomTree
open GeneratorExperiment
open CustomGenerators

let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultCustomArb>] } name test


[<Tests>]
let generatorTests = testList "Constraint to Generator Tests" [
    testProperty' "Generated data passes validation" <| fun (tree: Constraint<int>) ->        
        Prop.forAll (Arb.fromConstraint tree) <| fun (x:int) ->
            Constraint.isValid tree x
]