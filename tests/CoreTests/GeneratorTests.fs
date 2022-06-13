module GeneratorTests

open Expecto
open FsCheck
open FsSpec
open FsSpec.FsCheck
open CustomGenerators
open FsSpec.Constraint

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

let canGenerateAny arb =
    let sampleSize = 10
    try arb |> Arb.toGen |> Gen.sample 0 sampleSize 
        |> List.length |> ((=) sampleSize) 
    with | _ -> false
            

let generationPassesValidation<'a> name =
    testPropertyWithConfig 
        { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultConstraintArbs>; typeof<PossiblePredicatesOnly>] }
        name <| fun (tree: OnlyLeafsForType<'a>) ->
        let tree = tree.Constraint
        let arb = (Arb.fromConstraint tree)
        let isOnlyImpossiblePaths cTree = cTree |> Constraint.toAlternativeLeafGroups |> List.exists (not << Gen.Internal.isKnownImpossibleConstraint)
        let canTest = canGenerateAny arb && (not (isOnlyImpossiblePaths tree))
        canTest ==> lazy (
            try
                let prop = Prop.forAll arb <| fun (x:'a) ->
                    Constraint.isValid tree x
                prop.QuickCheckThrowOnFailure()
            with | _ -> failtest $"Passed filter, but failed to generate data. Constraint: {tree}"
            )
            


let genOrTimeout timeout (tree: Constraint<'a>) = 
    Arb.generate
    |> Gen.tryFilter (Constraint.isValid tree)
    |> Gen.map (Option.defaultWith (fun () ->
        // time penalty for failing to produce a value
        // also serves as a cap for expected performance of main function
        System.Threading.Thread.Sleep(timeout = timeout); Unchecked.defaultof<'a>))

[<Tests>]
let generatorTests = testList "Constraint to Generator Tests" [
    testList "Detect invalid leaf groups" [
        test "Regex for non-string" {
            let leafGroup = [(Internal.ConstraintLeaf<int>.Regex (System.Text.RegularExpressions.Regex(@"\d")))]
            Expect.isTrue (Gen.Internal.isKnownImpossibleConstraint leafGroup) "Regex should not be a valid constraint for int"
        }
        test "Min > Max" {
            let leafGroup = all [min 10; max 5] |> Constraint.toAlternativeLeafGroups |> List.head
            Expect.isTrue (Gen.Internal.isKnownImpossibleConstraint leafGroup) "Min should not be allowed to be greater than Max"
        }
    ]

    test "canGenerateAny returns false if no values can be generated" {
        let arb = Arb.generate<int> |> Gen.tryFilter (fun i-> false) |> Gen.map Option.get |> Arb.fromGen
        Expect.isFalse (canGenerateAny arb) ""
    }

    testProperty' "Constraint with no possible routes throws exception when building generator" <| fun () ->
        let noValidRoutes = gen {
            let! invalidRoutes = ConstraintGen.impossibleLeafs |> Gen.nonEmptyListOf
            return Constraint.any invalidRoutes
        } 
        Prop.forAll (noValidRoutes |> Arb.fromGen) <| fun cTree -> 
            Expect.throws (fun () -> Gen.fromConstraint cTree |> ignore) "Trees impossible to generate should throw an exception when building generator"

    testPropertyWithConfig 
        { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultConstraintArbs>; typeof<PossiblePredicatesOnly>] }
        "Constraint with mixed possible/impossible alternatives reliably generates data" <| fun (possibleTree:OnlyLeafsForType<int>, impossibleTrees:NonEmptyArray<CustomGenerators.ImpossibleIntConstraint>) ->
            // still need to account for cases like min > max
            (possibleTree.Constraint |> (not << Gen.Internal.containsImpossibleGroup)) ==> lazy(
                let mixedTree = 
                    impossibleTrees.Get 
                    |> List.ofArray 
                    |> List.map (fun c -> c.Constraint)
                    |> List.append [possibleTree.Constraint]
                    |> Constraint.any
                Prop.forAll (Arb.fromConstraint mixedTree) <| fun generated -> 
                    true
            )

    testList "Generated data passes validation for type" [
        generationPassesValidation<int> "Int"
        generationPassesValidation<string> "String"
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