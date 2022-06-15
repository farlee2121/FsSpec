module ValidationTests

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
    

]
