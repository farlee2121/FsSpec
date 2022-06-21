namespace FsSpec

open System

module DefaultValidators = 
    let validateMax (max:IComparable<'a>) value= 
        max.CompareTo(value) >= 0 

    let validateMin (min:IComparable<'a>) value= 
        min.CompareTo(value) <= 0 

    let validateRegex (regex: System.Text.RegularExpressions.Regex) value=
        match box value with
        | null -> false
        | :? System.String as str -> regex.IsMatch(str) 
        | _ -> invalidArg (nameof value) $"Regex can only validate strings, not {value.GetType().FullName}"

    let private nonGenericLength (seq:System.Collections.IEnumerable) =
        [for x in seq -> x] |> List.length

    let validateMinLength minLen value =
        match box value with
        | null -> false 
        | :? System.Collections.IEnumerable as coll ->
            nonGenericLength coll >= minLen
        | _ -> invalidArg (nameof value) $"Min length can only be applied to IEnumerable and derivatives, not {value.GetType().FullName}"

    let validateMaxLength maxLen value =
        match box value with
        | null -> false 
        | :? System.Collections.IEnumerable as coll ->
            nonGenericLength coll <= maxLen
        | _ -> invalidArg (nameof value) $"Max length can only be applied to IEnumerable and derivatives, not {value.GetType().FullName}"
