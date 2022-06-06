module GeneratorExperiment
open FsSpec.CustomTree

module Constraint = 
    let normalizeToDistributedAnd (constraints:Constraint<'a>) = 
        Constraint.Factories.any [Constraint.Factories.all [ConstraintLeaf ConstraintLeaf.None]]

//module Gen = 
//    open FsCheck

    //let fromConstraint (constraints:Constraint<'a>) : Gen<'a> =
    //   Gen.
    //   Arb.generate<'a> |> 
    //   Arb.


