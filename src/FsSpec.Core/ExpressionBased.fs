namespace FsSpec

module ExpressionBased =
    open Microsoft.FSharp.Quotations
    type TypeConstraint<'a> = Expr<'a -> bool>
    type ConstraintCombinator<'a> = TypeConstraint<'a> -> TypeConstraint<'a> -> TypeConstraint<'a>

    module Constraint =
        open Microsoft.FSharp.Linq.RuntimeHelpers
        let inline min minValue = <@ fun value -> minValue <= value @>
        let inline max maxValue = <@ fun value -> value <= maxValue @>
        let inline maxExclusive maxValue = <@ fun value -> value < maxValue @> // maybe call this lessThan?
        let inline oneOf values = <@ fun value -> List.contains value values  @>
        let inline length length = <@ fun value -> (Seq.length value) = length @>
        let inline regex pattern = <@ fun value -> System.Text.RegularExpressions.Regex(pattern).IsMatch(value) @>
        let custom (predicate:Expr<'a->bool>) : TypeConstraint<'a> = predicate
        
        let inline not constraint' = <@ fun value -> not ((%constraint') value) @>
        let inline or' constraintLeft constraintRight = <@ fun value -> ((%constraintLeft) value) || ((%constraintRight) value) @>
        let inline (|||) constraintLeft constraintRight = or' constraintLeft constraintRight
        let inline and' constraintLeft constraintRight = <@ fun value -> ((%constraintLeft) value) && ((%constraintRight) value) @>
        let inline (&&&) constraintLeft constraintRight = and' constraintLeft constraintRight

        let explain = 5 // todo
        let isValid (constraint':TypeConstraint<'a>) (value: 'a) = ((LeafExpressionConverter.EvaluateQuotation constraint') :?> ('a -> bool)) value
        

    module ConstraintPatterns =
        let placeHolder = "waiting to figure out what expression matching looks like"


    