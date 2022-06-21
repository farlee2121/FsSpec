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
        // probably want to include some kind of "meta" field so that custom types can do things like make specific contraint-definition time values available to formatters
        // for example: customMax 20 would be ("customMax", {max: 20}, (fn value -> value <= 20)) with formatter | Custom ("customMax", meta, _) -> $"max {meta.max}" 
        | Custom of (string * ('a -> bool))

    module SpecLeaf = 
        let isMax = (function | Max _ -> true | _ -> false)
        let isMin = (function | Min _ -> true | _ -> false)
        let isRegex = (function | Regex _ -> true | _ -> false)
        let isMinLength = (function | MinLength _ -> true | _ -> false)
        let isMaxLength = (function | MaxLength _ -> true | _ -> false)
        let isNone = (function | None -> true | _ -> false)

    type Combinator<'a> = | And | Or

type Spec<'a> =
    | SpecLeaf of SpecLeaf<'a>
    | Combinator of Combinator<'a> * Spec<'a> list