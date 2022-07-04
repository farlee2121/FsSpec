module GeneratorTests

open Expecto
open FsCheck
open FsSpec
open FsSpec.FsCheck
open CustomGenerators
open FsSpec.Spec
open FsSpec.Normalization
open System

let testProperty' name test = 
    testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultSpecArbs>] } name test

module Async = 
    let RunSyncWithTimeout (timeout:System.TimeSpan) computation = 
        Async.RunSynchronously(computation = computation, timeout = timeout.Milliseconds)

module Spec = 
    let containsLeafLike leafTest spec =
        let fComb _ children = children |> List.exists id 
        Spec.cata leafTest fComb spec

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
            

let generationPassesValidation<'a> name (leafExclusions: Spec<'a> -> bool)=
    testProperty' name <| fun (spec: OnlyLeafsForType<'a>) ->
        let spec = spec.Spec

        let isOnlyImpossiblePaths spec = spec |> Spec.toAlternativeLeafGroups |> List.forall Spec.Internal.isKnownImpossibleSpec
        let canTest = (not (isOnlyImpossiblePaths spec)) && not (leafExclusions spec) && canGenerateAny (Arb.fromSpec spec)
        canTest ==> lazy (
            let arb = (Arb.fromSpec spec)
            let prop = Prop.forAll arb <| fun (x:'a) ->
                Spec.isValid spec x
            prop.QuickCheckThrowOnFailure()
        )
            


let genOrTimeout timeout (tree: Spec<'a>) = 
    // time penalty for failing to produce a value
    // also serves as a cap for expected performance of main function
    let fallback () = 
        System.Threading.Thread.Sleep(timeout = timeout)
        Unchecked.defaultof<'a>
    try
        Arb.generate
        |> Gen.tryFilter (Spec.isValid tree)
        |> Gen.map (Option.defaultWith fallback)
    with 
    | _ -> Gen.fresh fallback 


[<Tests>]
let generatorTests = testList "Spec to Generator Tests" [
    testList "Detect invalid leaf groups" [
        test "Regex for non-string" {
            let leafGroup = [(Data.SpecLeaf.Regex (System.Text.RegularExpressions.Regex(@"\d")))]
            Expect.isTrue (Spec.Internal.isKnownImpossibleSpec leafGroup) "Regex should not be a valid constraint for int"
        }
        test "Min > Max" {
            let leafGroup = all [min 10; max 5] |> Spec.toAlternativeLeafGroups |> List.head
            Expect.isTrue (Spec.Internal.isKnownImpossibleSpec leafGroup) "Min should not be allowed to be greater than Max"
        }
        test "MinLength > MaxLength" {
            let leafGroup = all [is<int list>; minLength 10; maxLength 5] |> Spec.toAlternativeLeafGroups |> List.head
            Expect.isTrue (Spec.Internal.isKnownImpossibleSpec leafGroup) "MinLength should not be allowed to be greater than MaxLength"
        }
        test "Empty value set" {
            let leafGroup = all [is<int>; (SpecLeaf (Values []))] |> Spec.toAlternativeLeafGroups |> List.head
            Expect.isTrue (Spec.Internal.isKnownImpossibleSpec leafGroup) "Value sets should not be empty"
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
            (not <| Spec.Internal.containsImpossibleGroup possibleTree.Spec) ==> lazy(
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
        let excludeMinMax = function | Max _| Min _ -> true | _ ->false
        let excludeValues = function | Values _ -> true | _ ->false
        let exclude tests leaf = tests |> List.exists (fun test -> test leaf)
        let noExculsions _ = false
        generationPassesValidation<int> "Int" noExculsions
        //generationPassesValidation<int list> "Collections" (Spec.containsLeafLike (exclude [excludeMinMax; excludeValues]))
    ]
            
    testList "Optimized case tests" [
        let isFasterThanBaselineWithConfig sampleSize timeout spec =
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

        let isFasterThanBaseline spec = isFasterThanBaselineWithConfig 1 (System.TimeSpan.FromMilliseconds(20)) spec

        testCase "Small int range" <| fun () ->
            let spec = all [min 10; max 11]
            isFasterThanBaseline spec

        testCase "Small int64 range" <| fun () ->
            let spec = all [min 10L; max 11L]
            isFasterThanBaseline spec

        testCase "Small int16 range" <| fun () ->
            let spec = all [min (int16 10); max (int16 11)]
            isFasterThanBaseline spec

        testCase "Small Single (float32) range" <| fun () ->
            let spec = all [min 10f; max 11f]
            isFasterThanBaseline spec

        testCase "Small Double (float64) range" <| fun () ->
            let spec = all [min 10.; max 11.]
            isFasterThanBaseline spec

        testCase "Small DateTime range" <| fun () ->
            let spec = all [min (DateTime(2022, 06, 16)); max (DateTime(2022,06,17))]
            isFasterThanBaseline spec

        testCase "Small DateTimeOffset range" <| fun () ->
            let spec = all [min (DateTimeOffset.UtcNow); max (DateTimeOffset.UtcNow.AddDays(1))]
            isFasterThanBaseline spec

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
            isFasterThanBaseline spec

        testCase "Small length range: fsharp list" <| fun () ->
            let spec = Spec.is<int list> &&& Spec.minLength 5 &&& Spec.maxLength 6
            isFasterThanBaseline spec

        testCase "Small length range: Array" <| fun () ->
            let spec = Spec.is<int[]> &&& Spec.minLength 5 &&& Spec.maxLength 6
            isFasterThanBaseline spec

        testCase "Small length range: BCL List" <| fun () ->
            let spec = Spec.is<Collections.Generic.List<int>> &&& Spec.minLength 5 &&& Spec.maxLength 6
            isFasterThanBaseline spec

        testCase "Small length range: IEnumerable" <| fun () ->
            let spec = Spec.is<int seq> &&& Spec.minLength 1000 &&& Spec.maxLength 1001
            isFasterThanBaseline spec

        testCase "Small length range: string" <| fun () ->
            let spec = Spec.is<string> &&& Spec.minLength 5 &&& Spec.maxLength 6
            isFasterThanBaseline spec

        testCase "Small value set" <| fun () ->
            let spec = Spec.values [5,100]
            isFasterThanBaseline spec

    ]
]