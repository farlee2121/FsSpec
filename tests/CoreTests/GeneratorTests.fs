module GeneratorTests

open Expecto
open FsCheck
open FsSpec
open FsSpec.FsCheck
open CustomGenerators
open FsSpec.Constraint.Factories

let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultConstraintArbs>] } name test

module Async = 
    let RunSyncWithTimeout (timeout:System.TimeSpan) computation = 
        Async.RunSynchronously(computation = computation, timeout = timeout.Milliseconds)


module Expect = 

    let time f =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        f () |> ignore
        sw.Stop()
        sw.ElapsedMilliseconds


    let isSimilarOrFaster margin f1 f2 =
        let sample = 10

        let timei f i = time f
        let meanSample f = 
            let totalDuration = (Seq.initInfinite (timei f) |> Seq.take sample |> Seq.sum) 
            (double totalDuration) / (double sample)
        let baselineMean = meanSample f2
        let mean = meanSample f1

        //let percentDiff compare base' = (base' - compare) / compare
        if mean <= (baselineMean * (1.0 + margin))
        then ()
        else failtest $"Expected f1 to have better or equal performance. Actual: {mean}ms to {baselineMean}ms"

let generationPassesValidation<'a> name = 
    testProperty' name <| fun (tree: Constraint<'a>) ->  
        let arb = (Arb.fromConstraint tree)
        let canGenerateAny arb =
            try arb |> Arb.toGen |> Gen.sample 0 1 |> (not << List.isEmpty) with | _ -> false
        canGenerateAny arb ==> lazy (
            Prop.forAll arb <| fun (x:'a) ->
                Constraint.isValid tree x)


let genOrTimeout timeout (tree: Constraint<'a>) = 
    Arb.generate
    |> Gen.tryFilter (Constraint.isValid tree)
    |> Gen.map (Option.defaultWith (fun () ->
        // time penalty for failing to produce a value
        // also serves as a cap for expected performance of main function
        System.Threading.Thread.Sleep(timeout = timeout); Unchecked.defaultof<'a>))

[<Tests>]
let generatorTests = testList "Constraint to Generator Tests" [
    
    testList "Generated data passes validation for type" [
        generationPassesValidation<int> "Int"
    ]
            
    testList "Optimized case tests" [
        testCase "Small int range" <| fun () ->
            let constr = all [min 10; max 11]
            let sampleSize = 1
            let timeout = System.TimeSpan.FromMilliseconds(20)

            let baselineGen = genOrTimeout timeout constr
            let baseline () = 
                baselineGen
                |> Gen.sample 0 sampleSize
                |> List.length  
                
            let inferredGen = Gen.fromConstraint constr
            let inferredGenerator () =
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isFasterThan inferredGenerator baseline "Case should support generation faster than basic filtering"

        testCase "Regex similar to hand-coded gen" <| fun () ->
            let pattern = "xR32([a-z]){4}"
            let constr = regex pattern
            let sampleSize = 10

            let regexGen = gen { 
                return (Fare.Xeger pattern).Generate()
            }

            let baseline () = 
                regexGen
                |> Gen.sample 0 sampleSize
                |> List.length

            let inferredGen = Gen.fromConstraint constr
            let fromConstraint () = 
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isSimilarOrFaster 1.0 fromConstraint baseline

        testCase "Regex" <| fun () ->
            let constr = regex "xR32([a-z]){4}"
            let sampleSize = 1
            let timeout = System.TimeSpan.FromMilliseconds(20)

            let baselineGen = genOrTimeout timeout constr
            let baseline () = 
                baselineGen
                |> Gen.sample 0 sampleSize
                |> List.length  
            
            let inferredGen = Gen.fromConstraint constr
            let compare () =
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isFasterThan compare baseline "Regex should support generation faster than basic filtering"
    ]
]