module ExpressionBasedTests
open Expecto
open FsSpec.ExpressionBased
open FsSpec.ExpressionBased.Constraint

[<Tests>]
let tests = testList "Expression-based Constraint tests" [
    test "Numeric Range boolean validation" {
        let constr = min 5 &&& max 10
        Expect.isTrue (isValid constr 7) "7 should be a valid number between 5 and 10"
    }
    test "Numeric Range boolean validation, out of range" {
        let constr = min 5 &&& max 10
        Expect.isFalse (isValid constr 11) "11 should not be a valid number between 5 and 10"
        Expect.isFalse (isValid constr 1) "1 should not be a valid number between 5 and 10"
    }

    test "Regex pass" {
        let pattern = @"^\(\d{3}\)-\d{3}-\d{4}"
        let constr = Constraint.regex pattern
        let value = "(555)-555-5555"
        Expect.isTrue (isValid constr value) $"{value} should pass the regex {pattern}"
    }

    test "Regex fail" {
        let pattern = @"^\(\d{3}\)-\d{3}-\d{4}"
        let constr = Constraint.regex pattern
        let value = "boi"
        Expect.isFalse (isValid constr value) $"{value} should fail the regex {pattern}"
    }
]