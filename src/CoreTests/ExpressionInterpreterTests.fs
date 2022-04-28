module ExpressionInterpreterTests
open Expecto
open FSharp.Quotations
open FsCheck
open Expecto.ExpectoFsCheck
open System.Reflection


module ConstraintParser = 
    type Constraint<'t> =
        | Max of 't 
        | None

    let parse (factory: MethodInfo) : Option<Constraint<'b>> = 
        //factory.GetParameters
        //factory.ReturnType
        Option.None


open ConstraintParser

type Max20 = private Max20 of int
module Max20 =
    let create n = 
        if n < 20 then Some (Max20 n)
        else Option.None

[<Tests>]
let tests = testList "Test parsing arbitrary expressions into constraint data" [
    test "Given an integer < N comparison in a function When I parse constraints Then it recognizes a Max N constraint " {
         let t = Max20.create.GetType()
         let createT = t.DeclaringType.GetNestedType("Max20Module").GetMethod("create")
         ignore t.DeclaringType
         //let actual = ConstraintParser.parse Max20.create

         //Expect.equal actual (Some (Constraint<int>.Max 20)) ""
     }
]