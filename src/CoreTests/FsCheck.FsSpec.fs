module GeneratorExperiment
open FsSpec.CustomTree
open Constraint.Factories
open FsCheck


module Gen = 

    let fromConstraint (constraints:Constraint<'a>) : Gen<'a> =
       Arb.generate<'a>
       |> Gen.tryFilter (Constraint.isValid constraints)
       |> Gen.map Option.get

module Arb =
    let fromConstraint (constraints:Constraint<'a>) : Arbitrary<'a> =
       Gen.fromConstraint constraints |> Arb.fromGen


