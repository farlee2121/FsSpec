module GeneratorTests

open Expecto
open FsCheck
open FsSpec.CustomTree
open GeneratorExperiment
open CustomGenerators
open FsSpec.CustomTree.Constraint.Factories

let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultCustomArb>] } name test

module Async = 
    let RunSyncWithTimeout (timeout:System.TimeSpan) computation = 
        Async.RunSynchronously(computation = computation, timeout = timeout.Milliseconds)


//module Expect = 
//    open Expecto.Logging.Message
//    open Expecto.Logging
//    let isSimilarOrFaster errorMargin f1 f2 =
//        let timer = 

[<Tests>]
let generatorTests = testList "Constraint to Generator Tests" [
    testProperty' "Generated data passes validation" <| fun (tree: Constraint<int>) ->        
        Prop.forAll (Arb.fromConstraint tree) <| fun (x:int) ->
            Constraint.isValid tree x

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
                //async {
                    //return 
                    Gen.fromConstraint constr
                        |> Gen.sample 0 sampleSize
                        |> List.length
                //} |> Async.RunSyncWithTimeout timeout

            Expect.isFasterThan inferredGenerator baseline "Case should support generation faster than basic filtering"

        //testProperty "Small int range similar to manual gen" <| fun () ->
        //    //let baseline () = Gen
        //    ()
    ]
]