namespace FsSpec

open System

module DefaultValidators = 
    let validateMax (max:IComparable<'a>) value= 
        max.CompareTo(value) >= 0 

    let validateMin (min:IComparable<'a>) value= 
        min.CompareTo(value) <= 0 

    let validateRegex (regex: System.Text.RegularExpressions.Regex) value=
        match value :> System.Object with
        | null -> false
        | :? System.String as str -> regex.IsMatch(str) 
        | _ -> invalidArg (nameof value) $"Regex can only validate strings not {value.GetType().FullName}"


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
