namespace FsSpec

open System

module DefaultValidators = 
    let validateMax (value) (max:IComparable<'a>) = 
        match max.CompareTo(value) >= 0 with
        | true -> Ok value
        | _ -> Error [$"{value} is greater than the max {max}"]

    let validateMin value (min:IComparable<'a>) = 
        match min.CompareTo(value) <= 0 with
        | true -> Ok value
        | _ -> Error [$"{value} is less than the min {min}"]

    let validateRegex value (regex: System.Text.RegularExpressions.Regex)=
        match value :> System.Object with
        | :? System.String as str ->
            match regex.IsMatch(str) with
            | true -> Ok value
            | false -> Error [$"{value} didn't match expression {regex}"]
        | _ -> invalidArg (nameof value) $"Regex can only validate strings not {value.GetType().FullName}"
        

    let validateCustom value predicate = 
        match predicate value with 
        | true -> Result.Ok value 
        | false -> Result.Error ["nya"]


    // these are really just the "and" and "or" operations for a result type. Would probably be better to create parameterized versions
    // then build up my specific case
    let validateAnd value childResults = 
        let combine left right =
            match (left, right) with 
            | Ok _, Ok _ -> left
            | Ok _, Error err -> Error err
            | Error err, Ok _ -> Error err
            | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
        match childResults with
        | [] -> Ok value
        | _ -> childResults |> List.reduce combine

    let validateOr value childResults = 
        let combine left right =
            match (left, right) with 
            | Ok _, Ok _ -> left
            | Ok ok, Error _ -> Ok ok
            | Error err, Ok ok -> Ok ok
            | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
        match childResults with
        | [] -> Ok value
        | _ -> childResults |> List.reduce combine
