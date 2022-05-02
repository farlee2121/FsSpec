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
open Mono.Cecil

type Max20 = private Max20 of int
module Max20 =
    let create n = 
        if n < 20 then Some (Max20 n)
        else Option.None

//let thenGet name (success:bool, typeReference:TypeReference) =
//    match success with
//    | true -> typeReference.Resolve().NestedTypes |> Seq.find (fun (t:TypeDefinition) -> t.Name = name)
//    | false -> (false, null)

[<Tests>]
let tests = testList "Test parsing arbitrary expressions into constraint data" [
    test "Given an integer < N comparison in a function When I parse constraints Then it recognizes a Max N constraint " {
         let t = Max20.create.GetType()
         let createT = t.DeclaringType.GetNestedType("Max20Module").GetMethod("create")

         let typeNamed (name: string) (t:TypeDefinition) = t.Name = name
         let methodNamed (name: string) (t:MethodDefinition) = t.Name = name
         let testAssembly = ModuleDefinition.ReadModule(@"C:\workspaces\FsSpec\src\CoreTests\bin\Debug\net6.0\CoreTests.dll")
         let success, testModule = testAssembly.TryGetTypeReference("ExpressionInterpreterTests")//. ("Max20Module")
         let max20 = testModule.Resolve().NestedTypes |> Seq.find (typeNamed "Max20Module")
         let createCecil = max20.Methods |> Seq.find (methodNamed "create") 
         let l = createCecil.Body.Instructions
         ignore t.DeclaringType
         //let actual = ConstraintParser.parse Max20.create

         //Expect.equal actual (Some (Constraint<int>.Max 20)) ""
     }
]