module ValidationTests

open FsCheck
open FsSpec
open Expecto
open System
open CustomGenerators
open FsSpec.FsCheck.OptimizedCases
open System.Collections.Immutable

module Gen =
    let normalDoubleRange (min, max) = 
        let tryGet (opt:NormalFloat option) = opt |> Option.map unwrapGet
        Gen.doubleRange (tryGet min, tryGet max) |> Gen.map NormalFloat

    let listInRange<'a> (minLen, maxLen) = gen {
        let! len = Gen.choose (minLen, maxLen)  
        return! Arb.generate<'a> |> Gen.listOfLength len
    }

    let stringOfLength len = 
        Arb.generate<char> 
        |> Gen.listOfLength len 
        |> Gen.map (Array.ofList >> String)

    let stringInRange (minLen, maxLen) = gen {
        let! len = Gen.choose (minLen, maxLen)  
        return! stringOfLength len
    }



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

let minLengthTestsForType (rangeGen: int * int -> Gen<'a>) = 
    testList $"MinLength for {typeof<'a>.Name}" [
        let collSizeCap = 10000
        
        testProperty "Min length is inclusive" <| fun (minLen:NonNegativeInt) ->
            let minLen = (min minLen.Get collSizeCap)
            let spec = Spec.minLength minLen
            Prop.forAll (rangeGen (minLen, minLen) |> Arb.fromGen) <| fun coll ->
                Spec.isValid spec coll

        testProperty "Length less than min fails validation" <| fun (minLen:PositiveInt) ->
            let minLen = (min minLen.Get collSizeCap)
            let spec = Spec.minLength minLen
            Prop.forAll (rangeGen (0, minLen - 1) |> Arb.fromGen) <| fun str ->
                not (Spec.isValid spec str)

        testProperty "Length of at least min pass validation" <| fun (minLen:NonNegativeInt) ->
            let minLen = (min minLen.Get collSizeCap)
            let spec = Spec.minLength minLen
            Prop.forAll (rangeGen (minLen, collSizeCap) |> Arb.fromGen) <| fun str ->
                Spec.isValid spec str
    ]

let maxLengthTestsForType (rangeGen: int * int -> Gen<'a>) = 
    testList $"MaxLength for {typeof<'a>.Name}" [
        let collSizeCap = 10000
        
        testProperty "Max length is inclusive" <| fun (maxLen:NonNegativeInt) ->
            let maxLen = (min maxLen.Get collSizeCap)
            let spec = Spec.maxLength maxLen
            Prop.forAll (rangeGen (maxLen, maxLen) |> Arb.fromGen) <| fun coll ->
                Spec.isValid spec coll

        testProperty "Lengths of at most max pass validation" <| fun (maxLen:PositiveInt) ->
            let maxLen = (min maxLen.Get collSizeCap)
            let spec = Spec.maxLength maxLen
            Prop.forAll (rangeGen (0, maxLen) |> Arb.fromGen) <| fun str ->
                Spec.isValid spec str

        testProperty "Lengths of more than max fail validation" <| fun (maxLen:NonNegativeInt) ->
            let maxLen = (min maxLen.Get collSizeCap)
            let spec = Spec.maxLength maxLen
            Prop.forAll (rangeGen (maxLen + 1, collSizeCap) |> Arb.fromGen) <| fun str ->
                not(Spec.isValid spec str)
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

    testList "Regex" [
        test "Regex throws exception for non-string" {
            let f () =
                let spec = (Spec.SpecLeaf (Regex (System.Text.RegularExpressions.Regex(@"\d"))))
                Spec.isValid spec 0 |> ignore

            Expect.throws f "Regex should throw exception for non-string values"
        }

        testProperty "Any string matching format passes validation" <| fun () ->
            let pattern = @"\d{4}-[a-z]{3}!"
            let regexGen = gen {
                return Fare.Xeger(pattern).Generate()
            }

            Prop.forAll (Arb.fromGen regexGen) <| fun (str) ->
                Spec.isValid (Spec.regex pattern) str

        testProperty "Non-matching strings fail validation" <| fun (str: string) ->
            let pattern = @"\d{4}-[a-z]{3}!"
            let regex = System.Text.RegularExpressions.Regex(pattern)
            
            not(str <> null && regex.IsMatch(str)) ==> 
                lazy (not(Spec.isValid (Spec.regex pattern) str))

        test "Validating null value does not thrown an exception" {
            Spec.isValid (Spec.regex @"\d") null |> ignore
        }
            
    ]

    testList "Min Length" [
        testCase "Min length less than zero throws exception" <| fun () ->
            Expect.throwsT<ArgumentException> (fun () -> Spec.minLength -1 |> ignore) "Min length less than zero throws exception"

        minLengthTestsForType Gen.stringInRange
        minLengthTestsForType Gen.listInRange<int>
        minLengthTestsForType (Gen.listInRange<int> >> Gen.map Array.ofList)
        minLengthTestsForType (Gen.listInRange<int> >> Gen.map ImmutableList.CreateRange)
    ]

    testList "Max Length" [
        testCase "Max length less than zero throws exception" <| fun () ->
            Expect.throwsT<ArgumentException> (fun () -> Spec.maxLength -1 |> ignore) "Max length less than zero throws exception"

        maxLengthTestsForType Gen.stringInRange
        maxLengthTestsForType Gen.listInRange<int>
        maxLengthTestsForType (Gen.listInRange<int> >> Gen.map Array.ofList)
        maxLengthTestsForType (Gen.listInRange<int> >> Gen.map ImmutableList.CreateRange)
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
            let children = children.Get |> List.ofArray |> List.map unwrapSpec
            let isAnyChildValid = children |> List.exists (fun s -> Spec.isValid s i)
            let orSpec = Spec.any children
            Spec.isValid orSpec i = isAnyChildValid

    ]

    testList "And" [
        let testProperty' name test = 
            testPropertyWithConfig { FsCheckConfig.defaultConfig with arbitrary = [typeof<DefaultSpecArbs>] } name test

        let nOfConstantValidity validity count = List.init count (fun index -> Spec.predicate "const" (fun x -> validity))
        testProperty "Empty AND is aways valid" <| fun (i: int) ->
            Spec.isValid (Spec.all []) i

        testProperty "AND is true if all children are true" <| fun (childCount:PositiveInt) ->
            let spec = Spec.all (nOfConstantValidity true childCount.Get)
            let value = childCount
            Spec.isValid spec value

        testProperty' "AND is false if any child constraint is false" <| fun (children: NonEmptyArray<OnlyLeafsForType<int>>, i:int) ->
            let children = children.Get |> List.ofArray |> List.map unwrapSpec
            let areAllChildrenValid = children |> List.forall (fun s -> Spec.isValid s i)
            let orSpec = Spec.all children
            Spec.isValid orSpec i = areAllChildrenValid

    ]
    
]