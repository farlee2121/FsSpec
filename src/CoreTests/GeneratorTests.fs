module GeneratorTests

open Expecto
open FsCheck
open FsSpec.CustomTree
open GeneratorExperiment
open CustomGenerators
open FsSpec.CustomTree.Constraint.Factories

let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultConstraintArbs>] } name test

module Async = 
    let RunSyncWithTimeout (timeout:System.TimeSpan) computation = 
        Async.RunSynchronously(computation = computation, timeout = timeout.Milliseconds)


module Expect = 
    open Expecto.Logging.Message
    open Expecto.Logging

    //type SamplingConfig = { SampleSize: int;  }
    
    //type Percent = private Percent of double
    //module Percent =
    //    let ofFrequency (n:double) =
    //        if 0. <= n && n <= 1.
    //        then Percent n
    //        else invalidArg "n" "Must be a double between 0 and 1"

    //    let ofInt i =
    //        if 0 <= i && i <= 100
    //        then Percent ((double i)/100.)
    //        else invalidArg "i" "i must be an integer between 0 and 100"

    //let isSimilarOrFaster sampleSize errorMargin f1 f2 =
    //    let timer = 

    //let isSimilarOrFaster f1 f2 =
    //    let timer = 

let validForType (leafTest:ConstraintLeaf<'a> -> bool) (constr:Constraint<'a>) = 
    let fInternal op children = not (List.contains false children)
    Constraint.cata leafTest fInternal constr

let generationPassesValidation<'a> name (leafTest: ConstraintLeaf<'a> -> bool) = 
    testProperty' name <| fun (tree: Constraint<'a>) ->        
        tree |> validForType leafTest ==> lazy (
            Prop.forAll (Arb.fromConstraint tree) <| fun (x:'a) ->
                Constraint.isValid tree x)

module LeafTests =
    let isIntFriendlyLeaf = function |Regex _| Custom _-> false | _ -> true


[<Tests>]
let generatorTests = testList "Constraint to Generator Tests" [
    
    testList "Generated data passes validation for type" [
        generationPassesValidation<int> "Int" LeafTests.isIntFriendlyLeaf  
    ]
            
    testList "Optimized case tests" [
        testProperty "Small int range" <| fun () ->
            let constr = all [min 10; max 5000]
            let sampleSize = 10
            let timeout = System.TimeSpan.FromMilliseconds(500)

            let baseline () = 
                try 
                    async {
                        return Gen.Internal.defaultGen constr
                            |> Gen.sample 0 sampleSize
                            |> List.length  
                    } |> Async.RunSyncWithTimeout timeout
                with
                | _ -> sampleSize
                
            let inferredGenerator () =
                async {
                    return Gen.fromConstraint constr
                        |> Gen.sample 0 sampleSize
                        |> List.length
                } |> Async.RunSyncWithTimeout timeout

            Expect.isFasterThan inferredGenerator baseline "Case should support generation faster than basic filtering"

        //testProperty "Small int range similar to manual gen" <| fun () ->
        //    //let baseline () = Gen
        //    ()
    ]
]