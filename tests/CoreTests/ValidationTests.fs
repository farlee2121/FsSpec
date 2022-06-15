module ValidationTests

open FsCheck
open FsSpec
open Expecto
open System

let minTestsForType<'a when 'a :> IComparable<'a>> rangeGen = 
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
                not (Spec.isValid (Spec.min min) i)
    ] 

let intRangeGen (min, max) = 
    let min = Option.defaultValue Int32.MinValue min
    let max = Option.defaultValue Int32.MaxValue max
    Gen.choose (min, max)

let int64RangeGen (min,max) = 
    let min64 = Option.defaultValue Int64.MinValue min
    let max64 = Option.defaultValue Int64.MaxValue max
    
    gen {
        return System.Random.Shared.NextInt64(min64,max64)
    }    

let dateTimeRangeGen (min,max) = 
    let min = Option.defaultValue DateTime.MinValue min
    let max = Option.defaultValue DateTime.MaxValue max
    gen { 
        let! dateTimeTicks = int64RangeGen(Some min.Ticks, Some max.Ticks)
        return new DateTime(ticks = dateTimeTicks)
    }

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
    ] 

]
