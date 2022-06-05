module GeneratorExperiment
open FsSpec.CustomTree

module Constraint = 
    let normalizeToDistributedAnd (constraints:Constraint<'a>) = 
        Constraint.Factories.all [constraints]

//module Gen = 
//    open FsCheck

    //let fromConstraint (constraints:Constraint<'a>) : Gen<'a> =
    //   Gen.
    //   Arb.generate<'a> |> 
    //   Arb.


