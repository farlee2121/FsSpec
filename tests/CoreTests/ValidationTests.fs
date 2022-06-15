module ValidationTests

open FsCheck
open FsSpec
open Expecto
open System



let intRangeGen (min, max) = 
    let min = Option.defaultValue Int32.MinValue min
    let max = Option.defaultValue Int32.MaxValue max
    if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

    Gen.choose (min, max)

let int64RangeGen (min,max) = 
    let min = Option.defaultValue Int64.MinValue min
    let max = Option.defaultValue Int64.MaxValue max
    if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"
    
    gen {
        return System.Random.Shared.NextInt64(min,max)
    }    

let dateTimeRangeGen (min,max) =
    let min = Option.defaultValue DateTime.MinValue min
    let max = Option.defaultValue DateTime.MaxValue max
    if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

    gen { 
        let! dateTimeTicks = int64RangeGen(Some min.Ticks, Some max.Ticks)
        return new DateTime(ticks = dateTimeTicks)
    }

let doubleRangeGen (min,max) = 
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

let normalDoubleRangeGen (min, max) = 
    let tryGet (opt:NormalFloat option) = opt |> Option.map (fun x -> x.Get)
    doubleRangeGen (tryGet min, tryGet max) |> Gen.map NormalFloat

// Min or max of nan is impossible


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
        minTestsForType<int> intRangeGen
        minTestsForType<DateTime> dateTimeRangeGen
        minTestsForType<NormalFloat> normalDoubleRangeGen
    ] 
]
