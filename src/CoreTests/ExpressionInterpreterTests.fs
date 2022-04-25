module ExpressionInterpreterTests
open Expecto
open FSharp.Quotations
open FsCheck
open Expecto.ExpectoFsCheck


module ConstraintParser = 
    type Constraint<'t> =
        | Max of 't 

    let parse (expression: Expr<'t>) : Option<Constraint<'t>> = 
        None
open ConstraintParser



[<Tests>]
let tests = testList "Test parsing arbitrary expressions into constraint data" [

    test "how slow is regex generation" {
        let filter n = (n <> null) && System.Text.RegularExpressions.Regex(@"\d{3}-\d{3}-\d{4}").IsMatch(n)
        
        let gen = Arb.generate<string> |> Gen.filter filter 
        let timer = System.Diagnostics.Stopwatch.StartNew();
        //let sample = gen.Sample(1, 100000)
        let prop = FsCheck.Prop.forAll (Arb.fromGen gen) (fun n -> true)
        prop.VerboseCheck()
        timer.Stop()
        timer.Elapsed
    }
    // test "Given an integer < N comparison in a function When I parse constraints Then it recognizes a Max N constraint " {
    //     let validate n = n < 20

    //     let actual = ConstraintParser.parse <@ validate @>

    //     Expect.equal actual (Some (Constraint<int>.Max 20)) ""
    // }
]