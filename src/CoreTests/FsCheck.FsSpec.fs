module GeneratorExperiment
open FsSpec.CustomTree

module Constraint = 
    let normalizeToDistributedAnd (constraints:Constraint<'a>) = 
        let fLeaf leaf = [ConstraintLeaf leaf]
        let fInternal _ children = List.concat children
        let allLeaves = Constraint.cata fLeaf fInternal constraints
        Constraint.Factories.any [Constraint.Factories.all allLeaves]

//module Gen = 
//    open FsCheck

    //let fromConstraint (constraints:Constraint<'a>) : Gen<'a> =
    //   Gen.
    //   Arb.generate<'a> |> 
    //   Arb.


