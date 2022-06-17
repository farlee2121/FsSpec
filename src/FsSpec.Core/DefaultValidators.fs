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
