module ValidationTests

open FsCheck
open FsSpec
open Expecto
open System
open CustomGenerators


module Gen =
    let intRange (min, max) = 
        let min = Option.defaultValue Int32.MinValue min
        let max = Option.defaultValue Int32.MaxValue max
        if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

        Gen.choose (min, max)

    let int64Range (min,max) = 
        let min = Option.defaultValue Int64.MinValue min
        let max = Option.defaultValue Int64.MaxValue max
        if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"
    
        gen {
            return System.Random.Shared.NextInt64(min,max)
        }    

    let dateTimeRange (min,max) =
        let min = Option.defaultValue DateTime.MinValue min
        let max = Option.defaultValue DateTime.MaxValue max
        if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

        gen { 
            let! dateTimeTicks = int64Range(Some min.Ticks, Some max.Ticks)
            return new DateTime(ticks = dateTimeTicks)
        }

    let doubleRange (min,max) = 
        let toFinite = function
            | Double.PositiveInfinity -> Double.MaxValue
            | Double.NegativeInfinity -> Double.MinValue
            | f -> f

        let min = Option.defaultValue Double.MinValue min 
        let max = Option.defaultValue Double.MaxValue max
        if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

        match min,max with 
        | Double.PositiveInfinity, _ -> 
            Gen.constant Double.PositiveInfinity
        | _, Double.NegativeInfinity -> 
            Gen.constant Double.NegativeInfinity
        | _ -> 
            gen {
                let! n = Arb.generate<int>
                let proportion = float n / float Int32.MaxValue 
                let rangeSize = toFinite((min |> toFinite) - (max |> toFinite)) 
                return (max |> toFinite) - Math.Abs(proportion * rangeSize) 
            }

    let normalDoubleRange (min, max) = 
        let tryGet (opt:NormalFloat option) = opt |> Option.map (fun x -> x.Get)
        doubleRange (tryGet min, tryGet max) |> Gen.map NormalFloat


let minTestsForType<'a when 'a :> IComparable<'a> and 'a : equality> rangeGen = 
    testList $"Min {typeof<'a>.Name}" [
        testProperty "Min inclusive" <| fun (i:'a) ->
            Spec.isValid (Spec.min i) i

        testProperty "Any value greater than or equal to min is valid" <| fun (min: 'a) ->
            let arb = rangeGen (Some min, Option.None) |> Arb.fromGen
            Prop.forAll arb <| fun (i:'a) ->
                Spec.isValid (Spec.min min) i

        testProperty "Any value less than min is invalid" <| fun (min: 'a) ->
            let arb = rangeGen (Option.None, Some min) |> Arb.fromGen
            Prop.forAll arb <| fun (i:'a) ->
                min <> i ==> lazy(
                    not (Spec.isValid (Spec.min min) i)
                )
    ] 

let maxTestsForType<'a when 'a :> IComparable<'a> and 'a : equality> rangeGen = 
    testList $"Max {typeof<'a>.Name}" [
        testProperty "Max inclusive" <| fun (i:'a) ->
            Spec.isValid (Spec.max i) i

        testProperty "Any value less than or equal to max is valid" <| fun (max: 'a) ->
            let arb = rangeGen (Option.None, Some max) |> Arb.fromGen
            Prop.forAll arb <| fun (i:'a) ->
                Spec.isValid (Spec.max max) i

        testProperty "Any value more than max is invalid" <| fun (max: 'a) ->
            let arb = rangeGen (Some max, Option.None) |> Arb.fromGen
            Prop.forAll arb <| fun (i:'a) ->
                max <> i ==> lazy(
                    not (Spec.isValid (Spec.max max) i)
                )
    ] 

[<Tests>]
let validateTests = testList "Spec Validation" [
   
    testList "None is valid for any value" [
        testProperty "int" <| fun (i:int) ->
            Spec.isValid (Spec.SpecLeaf None) i

        testProperty "string" <| fun (str:string) ->
            Spec.isValid (Spec.SpecLeaf None) str

        testProperty "double" <| fun (f:double) ->
            Spec.isValid (Spec.SpecLeaf None) f

        testProperty "DateTime" <| fun (dt:DateTime) ->
            Spec.isValid (Spec.SpecLeaf None) dt
    ]
    
    testList "Min" [
        minTestsForType<int> Gen.intRange
        minTestsForType<DateTime> Gen.dateTimeRange
        minTestsForType<NormalFloat> Gen.normalDoubleRange
    ] 

    testList "Max" [
        maxTestsForType<int> Gen.intRange
        maxTestsForType<DateTime> Gen.dateTimeRange
        maxTestsForType<NormalFloat> Gen.normalDoubleRange
    ] 

    testList "Custom" [
        testProperty "Custom spec validity always matches predicate output" <| fun () ->
            let cases = [true; false]

            let test outcome =
                Prop.forAll Arb.from<int> <| fun (i) ->
                    outcome = Spec.isValid (Spec.predicate "const" (fun x -> outcome)) i
            cases |> List.map test
    ]
           
    testList "Or" [
        let testProperty' name test = 
            testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultSpecArbs>] } name test

        let nOfConstantValidity validity count = List.init count (fun index -> Spec.predicate "const" (fun x -> validity))
        testProperty "Empty OR is aways valid" <| fun (i: int) ->
            Spec.isValid (Spec.any []) i

        testProperty "OR is false if no children are true" <| fun (childCount:PositiveInt) ->
            let spec = Spec.any (nOfConstantValidity false childCount.Get)
            let value = childCount
            not (Spec.isValid spec value) 

        testProperty' "OR is true if any child constraint is true" <| fun (children: NonEmptyArray<OnlyLeafsForType<int>>, i:int) ->
            let children = children.Get |> List.ofArray |> List.map (fun s -> s.Spec)
            let anyChildValid = children |> List.exists (fun s -> Spec.isValid s i)
            let orSpec = Spec.any children
            Spec.isValid orSpec i = anyChildValid

    ]
    
]