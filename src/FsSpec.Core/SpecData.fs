namespace FsSpec

open System

[<AutoOpen>]
module Data =
    type SpecLeaf<'a> = 
        | None
        | Max of IComparable<'a> 
        | Min of IComparable<'a>
        | Regex of System.Text.RegularExpressions.Regex
        | MinLength of int
        | MaxLength of int
        | Values of 'a list
        | NotValues of 'a list
        | Custom of (string * ('a -> bool))

    module SpecLeaf = 
        let isMax = (function | Max _ -> true | _ -> false)
        let isMin = (function | Min _ -> true | _ -> false)
        let isRegex = (function | Regex _ -> true | _ -> false)
        let isMinLength = (function | MinLength _ -> true | _ -> false)
        let isMaxLength = (function | MaxLength _ -> true | _ -> false)
        let isValues = (function | Values _ -> true | _ -> false)
        let isNotValues = (function | NotValues _ -> true | _ -> false)
        let isNone = (function | None -> true | _ -> false)

    type Combinator<'a> = | And | Or

type Spec<'a> =
    | SpecLeaf of SpecLeaf<'a>
    | Combinator of Combinator<'a> * Spec<'a> list