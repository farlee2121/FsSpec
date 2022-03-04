namespace FsSpec

module ObjectBased =
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Linq.RuntimeHelpers

    module Constraint =
        type ITypeConstraint<'a> = 
            abstract member Expression : Expr<'a->bool>
        
        module Types =
            type MinConstraint<'a when 'a : comparison> (minValue) =
            
                member self.MinValue = minValue
                interface ITypeConstraint<'a> with
                    member self.Expression = <@ fun value -> minValue <= value @>

            type MaxConstraint<'a when 'a : comparison> (maxValue) =
                member _.MaxValue = maxValue
                interface ITypeConstraint<'a> with
                    member _.Expression = <@ fun value -> value <= maxValue @>

            type PredicateConstraint<'a> (label, predicate) =
                member _.Label : string = label
                interface ITypeConstraint<'a> with
                    member _.Expression : Expr<'a -> bool> = predicate




            type ITypeConstraintCombinator<'a> =
                inherit ITypeConstraint<'a>

            type OrConstraintCombinator<'a> (left, right) =
                interface ITypeConstraintCombinator<'a> with
                    member _.Expression = <@ fun (value: 'a) -> ((%left.Expression) value) || ((%right.Expression) value) @>
                member _.Left : ITypeConstraint<'a> = left
                member _.Right : ITypeConstraint<'a> = right

            type AndConstraintCombinator<'a> (left, right) =
                interface ITypeConstraintCombinator<'a> with
                    member _.Expression = <@ fun (value: 'a) -> ((%left.Expression) value) && ((%right.Expression) value) @>
                member _.Right : ITypeConstraint<'a> = right
                member _.Left : ITypeConstraint<'a> = left

            type NotConstraint<'a> (typeConstraint: ITypeConstraint<'a>) =
                member _.NegatedConstraint = typeConstraint
                interface ITypeConstraint<'a> with 
                    member _.Expression = <@fun value -> not ((%typeConstraint.Expression) value)@>  

        // type Constraint<'a> = {
        //     Label : string
        //     Expression : Expr<'a -> bool>
        // }
        //     interface ITypeConstraint

        open Types
        let custom label (predicate:Expr<'a->bool>) = PredicateConstraint(label, predicate)
        let inline min minValue = MinConstraint(minValue) 
        let inline max maxValue = MaxConstraint(maxValue)
        

        let inline not constraint' = NotConstraint(constraint')
        let inline or' left right = OrConstraintCombinator(left, right)
        let inline (|||) left right = or' left right
        let inline and' left right = AndConstraintCombinator(left, right)
        let inline (&&&) constraintLeft constraintRight = and' constraintLeft constraintRight
        // add any and all?

        let isValid (constraint':ITypeConstraint<'a>) (value: 'a) = ((LeafExpressionConverter.EvaluateQuotation constraint'.Expression) :?> ('a -> bool)) value

        let explainCustom constraint' value (getMessage : 'a -> string) = 
            // some explanations will be nested
            // lets try to define explain with one official message per constraint type, then I can consider contextual explanation overrides
            ""