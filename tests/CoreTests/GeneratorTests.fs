module GeneratorTests

open Expecto
open FsCheck
open FsSpec
open FsSpec.FsCheck
open CustomGenerators
open FsSpec.Spec
open FsSpec.Normalization

let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultSpecArbs>] } name test

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

let canGenerateAny arb =
    let sampleSize = 1
    try arb |> Arb.toGen |> Gen.sample 0 sampleSize 
        |> List.length |> ((=) sampleSize) 
    with | _ -> false
            

let generationPassesValidation<'a> name =
    testProperty' name <| fun (spec: OnlyLeafsForType<'a>) ->
        let spec = spec.Spec
        let arb = (Arb.fromSpec spec)

        let isOnlyImpossiblePaths spec = spec |> Spec.toAlternativeLeafGroups |> List.forall Constraint.Internal.isKnownImpossibleSpec
        let canTest = canGenerateAny arb && (not (isOnlyImpossiblePaths spec))
        canTest ==> lazy (
                let prop = Prop.forAll arb <| fun (x:'a) ->
                    Spec.isValid spec x
                prop.QuickCheckThrowOnFailure()
            )
            


let genOrTimeout timeout (tree: Spec<'a>) = 
    Arb.generate
    |> Gen.tryFilter (Spec.isValid tree)
    |> Gen.map (Option.defaultWith (fun () ->
        // time penalty for failing to produce a value
        // also serves as a cap for expected performance of main function
        System.Threading.Thread.Sleep(timeout = timeout); Unchecked.defaultof<'a>))

[<Tests>]
let generatorTests = testList "Spec to Generator Tests" [
    testList "Detect invalid leaf groups" [
        test "Regex for non-string" {
            let leafGroup = [(Data.SpecLeaf.Regex (System.Text.RegularExpressions.Regex(@"\d")))]
            Expect.isTrue (Constraint.Internal.isKnownImpossibleSpec leafGroup) "Regex should not be a valid constraint for int"
        }
        test "Min > Max" {
            let leafGroup = all [min 10; max 5] |> Spec.toAlternativeLeafGroups |> List.head
            Expect.isTrue (Constraint.Internal.isKnownImpossibleSpec leafGroup) "Min should not be allowed to be greater than Max"
        }
    ]

    test "canGenerateAny returns false if no values can be generated" {
        let arb = Arb.generate<int> |> Gen.tryFilter (fun i-> false) |> Gen.map Option.get |> Arb.fromGen
        Expect.isFalse (canGenerateAny arb) ""
    }

    testProperty' "Spec with no possible routes throws exception when building generator" <| fun () ->
        let noValidRoutes = gen {
            let! invalidRoutes = SpecGen.impossibleLeafs |> Gen.nonEmptyListOf
            return Spec.any invalidRoutes
        } 
        Prop.forAll (noValidRoutes |> Arb.fromGen) <| fun spec -> 
            Expect.throws (fun () -> Gen.fromSpec spec |> ignore) "Specs impossible to generate should throw an exception when building generator"

    testProperty' "Spec with mixed possible/impossible alternatives reliably generates data" 
        <| fun (possibleTree:OnlyLeafsForType<int>, impossibleTrees:NonEmptyArray<CustomGenerators.ImpossibleIntSpec>) ->
            // still need to account for cases like min > max
            (possibleTree.Spec |> (not << Constraint.Internal.containsImpossibleGroup)) ==> lazy(
                let mixedTree = 
                    impossibleTrees.Get 
                    |> List.ofArray 
                    |> List.map unwrapSpec
                    |> List.append [possibleTree.Spec]
                    |> Spec.any
                Prop.forAll (Arb.fromSpec mixedTree) <| fun generated -> 
                    true
            )

    testList "Generated data passes validation for type" [
        generationPassesValidation<int> "Int"
    ]
            
    testList "Optimized case tests" [
        testCase "Small int range" <| fun () ->
            let spec = all [min 10; max 11]
            let sampleSize = 1
            let timeout = System.TimeSpan.FromMilliseconds(20)

            let baselineGen = genOrTimeout timeout spec
            let baseline () = 
                baselineGen
                |> Gen.sample 0 sampleSize
                |> List.length  
                
            let inferredGen = Gen.fromSpec spec
            let inferredGenerator () =
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isFasterThan inferredGenerator baseline "Case should support generation faster than basic filtering"

        testCase "Small int64 range" <| fun () ->
            let spec = all [min 10L; max 11L]
            let sampleSize = 1
            let timeout = System.TimeSpan.FromMilliseconds(20)

            let baselineGen = genOrTimeout timeout spec
            let baseline () = 
                baselineGen
                |> Gen.sample 0 sampleSize
                |> List.length  
                
            let inferredGen = Gen.fromSpec spec
            let inferredGenerator () =
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isFasterThan inferredGenerator baseline "Case should support generation faster than basic filtering"

        testCase "Regex similar to hand-coded gen" <| fun () ->
            let pattern = "xR32([a-z]){4}"
            let spec = regex pattern
            let sampleSize = 10

            let regexGen = gen { 
                return (Fare.Xeger pattern).Generate()
            }

            let baseline () = 
                regexGen
                |> Gen.sample 0 sampleSize
                |> List.length

            let inferredGen = Gen.fromSpec spec
            let fromSpec () = 
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isSimilarOrFaster 1.0 fromSpec baseline

        testCase "Regex" <| fun () ->
            let spec = regex "xR32([a-z]){4}"
            let sampleSize = 1
            let timeout = System.TimeSpan.FromMilliseconds(20)

            let baselineGen = genOrTimeout timeout spec
            let baseline () = 
                baselineGen
                |> Gen.sample 0 sampleSize
                |> List.length  
            
            let inferredGen = Gen.fromSpec spec
            let compare () =
                inferredGen
                |> Gen.sample 0 sampleSize
                |> List.length

            Expect.isFasterThan compare baseline "Regex should support generation faster than basic filtering"
    ]
]