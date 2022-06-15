module ValidationTests

open FsCheck
open FsSpec
open Expecto
open System

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
        testProperty "Min inclusive" <| fun (i:int) ->
            Spec.isValid (Spec.min i) i

        testProperty "Any int greater than or equal to min is valid" <| fun (min: int) ->
            let arb = Gen.choose (min, Int32.MaxValue) |> Arb.fromGen
            Prop.forAll arb <| fun (i:int) ->
                Spec.isValid (Spec.min min) i

        testProperty "Any int less than min is invalid" <| fun (min: int) ->
            let arb = Gen.choose (Int32.MinValue, min) |> Arb.fromGen
            Prop.forAll arb <| fun (i:int) ->
                not (Spec.isValid (Spec.min min) i)
    ] 

]
