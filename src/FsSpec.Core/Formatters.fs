namespace FsSpec.Formatters
module Formatters =
    open FsSpec.Explanation
    open FsSpec
    let private leafFormatter leaf =
        match leaf with
        | None -> "none"
        | Min min -> $"min {min}"
        | Max max -> $"max {max}"
        | Regex regex -> $"regex {regex.ToString()}"
        | Custom (label, _) -> label

    let prefix_allresults { Explanation = explanation; Value = value} : string= 
        
        let statusWrap result =
            match result with
            | Ok msg -> $"{msg} (OK)"
            | Error msg -> $"{msg} (FAIL)"

        let fComb (opResult:SpecResult<Combinator<'a>>) (children:string list) =
            let op =
                match opResult with
                | Ok op -> op
                | Error op -> op
            
            let opStr = 
                match op with 
                | And -> "and"
                | Or -> "or"

            let fmtChildren = 
                match children with 
                | [] -> "(empty)"
                | kids -> kids |> String.concat "; "
            $"{opStr} [{fmtChildren}]"

        let fleaf leafResult = 
            leafResult |> SpecResult.map leafFormatter |> statusWrap

        if Explanation.isOk explanation 
        then $"{value} is Ok"
        else $"{value} failed with: {Explanation.cata fleaf fComb explanation}" 