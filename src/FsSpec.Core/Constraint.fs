namespace FsSpec
module Constraint =
    let validate constraintTree value = 
        let fLeaf leaf = 
            match leaf with
            | None -> Ok value
            | Max max -> DefaultValidators.validateMax value max
            | Min min -> DefaultValidators.validateMin value min
            | Regex expr -> DefaultValidators.validateRegex value expr
            | Custom(_, pred) -> DefaultValidators.validateCustom value pred
        let fComb comb childResults = 
            match comb with
            | And -> DefaultValidators.validateAnd value childResults
            | Or -> DefaultValidators.validateOr value childResults

        constraintTree |> Constraint.trimEmptyBranches |> Constraint.cata fLeaf fComb

    let isValid constraintTree value =
        match validate constraintTree value with
        | Ok _ -> true
        | Error _ -> false

