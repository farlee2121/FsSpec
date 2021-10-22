module ConstraintProto

type Constraint<'a> = 
    | MaxLength of int
    | MinLength of int
    | Regex of string // regex expression
    | Max of 'a // should be any type of number
    | Min of 'a
    | Choice of 'a list
    | And of Constraint<'a> list
    | Or of Constraint<'a> list
    | Custom of string * ('a -> bool) // the tag needn't be a string

    

    // do I need to segment some of these by types they can apply to?

module Constraint = 
    let rec catamorph maxLengthf minLengthf maxf minf regexf choicef andf orf customf inst : 'r =
        let recurse = catamorph maxLengthf minLengthf maxf minf regexf choicef andf orf customf
        match inst with
        | MaxLength len -> maxLengthf len
        | MinLength len -> minLengthf len
        | Regex expr -> regexf expr
        | Choice list -> choicef list
        | Max limit -> maxf limit
        | Min limit -> minf limit
        | And children -> children |> List.map recurse |> andf 
        | Or children -> children |> List.map recurse |> orf
        | Custom (name, predicate)  -> customf (name, predicate)
        //....


    let rec fold maxLengthf minLengthf maxf minf regexf choicef andf orf customf state constraint' : 'r= 
        //WARNING: this is not actually optimized to tail recursion. 
        // https://github.com/dotnet/fsharp/issues/6984 and I think list fold isn't inline, also preventing the optimization
        let recurse = fold maxLengthf minLengthf maxf minf regexf choicef andf orf customf 
        match constraint' with
        | MaxLength len -> maxLengthf state len
        | MinLength len -> minLengthf state len
        | Regex expr -> regexf state expr
        | Choice list -> choicef state list
        | Max limit -> maxf state limit
        | Min limit -> minf state limit
        | And children -> 
            let newAcc = andf state
            children |> List.fold recurse newAcc 
        | Or children -> 
            let newAcc = orf state
            children |> List.fold recurse newAcc 
        | Custom (name, predicate)  -> customf state (name, predicate)

    //let foldBack maxLengthf minLengthf maxf minf regexf choicef andf orf customf constraint' state : 'r = 
    //    let acc = 
    //    fold maxLengthf minLengthf maxf minf regexf choicef andf orf customf state constraint' 

    let (&&&) left right = And [left; right]
    let and' left right = And [left; right]
    let andAll list = And list
    let or' left right = Or [left; right]
    let orAny list = Or list
    let matchRegex expr = Regex expr
    let max limit = Max limit
    let min limit = Min limit
    let maxLength maxLen = MaxLength maxLen
    let minLength minLen = MinLength minLen
    let custom tag pred = Custom (tag, pred)
    let oneOf set = Choice set


    let (|IsComparable|) (obj : obj) = 
        match obj with
        | :? System.IComparable as comparable -> Some(comparable)
        | _ ->  None

    module DefaultValidations = 

        let validateMaxLength<'a> (value: 'a) maxLen =
            let enumToList (enum:System.Collections.IEnumerable) = [for item in enum do yield item]
            match value :> System.Object with
            | :? System.Collections.IEnumerable as enum -> 
                match enum |> enumToList |> List.length with
                | len when len < maxLen -> Ok value
                | _ -> Error [$"Max expected length is {maxLen}"]
            | _ -> Error [$"Invalid Constraint: MinLength cannot be applied to {typeof<'a>.Name}"]

        let validateMinLength<'a> (value: 'a) minLen =
            let enumToList (enum:System.Collections.IEnumerable) = [for item in enum do yield item]
            match value :> System.Object with
            | :? System.Collections.IEnumerable as enum -> 
                match enum |> enumToList |> List.length with
                | len when minLen < len -> Ok value
                | _ -> Error [$"Min expected length is {minLen}"]
            | _ -> Error [$"Invalid Constraint: MinLength cannot be applied to {typeof<'a>.Name}"]

        let validateMax value max = 
            match value with
            | v when v <= max -> Ok v
            | _ -> Error [$"{value} is greater than the max {max}"]

        let validateMin value min = 
            match value with
            | v when min <= v -> Ok v
            | _ -> Error [$"{value} is less than the min {min}"]
            //match value with
            //| :? System.IComparable as valid -> 
            //    match valid with 
            //    | Some v when v < max -> Ok v
            //    | _ -> Error "" // Should have a result type that maintains info and can be formatted later | InvalidConstraint | OverMax (val, max) | UnderMin..., that also makes explain easy
            //| _ -> Error "Invalid constraint for the given type" // can't do anything with this

        let validateRegex value regex = 
            let regexTest = System.Text.RegularExpressions.Regex(regex);
            try
                match value :> System.Object with
                | :? System.String as str ->
                    match regexTest.IsMatch(str) with
                    | true -> Ok value
                    | false -> Error [$"{value} didn't match expression {regex}"]
                | _ -> Error ["Invalid "]
            with
            | e -> Error [$"Cast to Object failed with exception: {e.Message}"]

        let validateChoice value options =
            if options |> List.contains value 
            then Ok value
            else Error [$"{value} not in allowed values %A{options}"]
        

        let validateAnd childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok _, Error err -> Error err
                | Error err, Ok _ -> Error err
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            childResults |> List.reduce combine

        let validateOr childResults = 
            let combine left right =
                match (left, right) with 
                | Ok _, Ok _ -> left
                | Ok ok, Error _ -> Ok ok
                | Error err, Ok ok -> Ok ok
                | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
            childResults |> List.reduce combine

        let validatePredicate value (name, predicate) =
            match predicate value with
            | true -> Ok value
            | false -> Error [$"Failed custom predicate: {name}"]

    let validate constraint' value = 
        let avoiding a = Ok value 

        
        // NOTE: Implemented with values baked in to the validators because it was easier to reason about applicative evaluation
        //       and I wanted it to return all possible errors
        let reduceToResultForValue = catamorph 
                                    <| DefaultValidations.validateMaxLength value
                                    <| DefaultValidations.validateMinLength value
                                    <| (DefaultValidations.validateMax value)
                                    <| DefaultValidations.validateMin value
                                    <| DefaultValidations.validateRegex value
                                    <| DefaultValidations.validateChoice value
                                    <| DefaultValidations.validateAnd
                                    <| DefaultValidations.validateOr
                                    <| DefaultValidations.validatePredicate value
                                    //avoiding avoiding DefaultValidations.validateMax avoiding avoiding avoiding (DefaultValidations.validateAnd) avoiding avoiding
        reduceToResultForValue constraint'

    //IMPORTANT:NOTE: validation cannot be implemented with a proper fold. think about it from the iteration standpoint, I'd have to have
    //                some kind of extra condition that tells me 
    type ResultCombinationOperation = | And | Or
    let validateFold constraint' value = 
        let combineByState (operation, accResult) nextResult = 
            match operation with
            | ResultCombinationOperation.And -> (operation, DefaultValidations.validateAnd [accResult; nextResult])
            | ResultCombinationOperation.Or -> (operation, DefaultValidations.validateOr [accResult; nextResult])
            
        let reduceToResultForValue = fold 
                                    <| (fun state maxLen -> combineByState state (DefaultValidations.validateMaxLength value maxLen)) // I don't think this is really better than the direct lambdas
                                    <| (fun state minLen -> combineByState state (DefaultValidations.validateMinLength value minLen))
                                    <| (fun state max -> combineByState state (DefaultValidations.validateMax value max))
                                    <| (fun state min -> combineByState state (DefaultValidations.validateMin value min))
                                    <| (fun state regex -> combineByState state (DefaultValidations.validateRegex value regex))
                                    <| (fun state choices -> combineByState state (DefaultValidations.validateChoice value choices))
                                    <| (fun (_, accResult) -> (ResultCombinationOperation.And, accResult))
                                    <| (fun (_, accResult) -> (ResultCombinationOperation.Or, accResult))
                                    <| (fun state custom -> combineByState state (DefaultValidations.validatePredicate value custom))
                                    <| (ResultCombinationOperation.And, (Ok value))
        let (op, result) = reduceToResultForValue constraint'
        result

    //TODO: Explain should allow them to pass a config with overrides. At least a map between custom tests and message formatters 


    //module DefaultGenerators =
    //    open FsCheck
    //    open Fare
    //    let minGen = () 

    //    let regexGen pattern = 
    //        Gen.sized (fun size ->
    //            let xeger = Xeger pattern
    //            let count = if size < 1 then 1 else size
    //            [ for i in 1..count -> xeger.Generate() ]
    //            |> Gen.elements
    //            |> Gen.resize count)


    //let toGen constraint' =
    //    let ignore = id
    //    let reduceToGen = catamorph 
    //                                <| DefaultValidations.validateMaxLength value
    //                                <| DefaultValidations.validateMinLength value
    //                                <| (DefaultValidations.validateMax value)
    //                                <| DefaultValidations.validateMin value
    //                                <| DefaultGenerators.regexGen
    //                                <| DefaultValidations.validateChoice value
    //                                <| DefaultValidations.validateAnd
    //                                <| DefaultValidations.validateOr
    //                                <| DefaultValidations.validatePredicate value
    //    reduceToGen constraint'


//type ComplexTypeConstraint = 
//| PropertyConstraint of System.Reflection.PropertyInfo * Constraint
//| List of Constraint
//| Complex of ComplexTypeConstraint list
