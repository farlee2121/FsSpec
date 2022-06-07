module CustomGenerators

open FsCheck
open TreeModel
open FsSpec.CustomTree
open System

    module ConstraintArb =
        let leafOnly<'a> = Arb.generate<ConstraintLeaf<'a>> 
                            |> Gen.map ConstraintLeaf
                            |> Arb.fromGen
        let noLeafs<'a> = 
            Arb.generate<Tree<Combinator<'a>, Combinator<'a>>> 
            |> Gen.map (fun opTree ->
                let reduceLeaf leaf = Combinator (leaf, []) 
                let reduceInternal op children = (Combinator (op, List.ofSeq children))
                let tree = Tree.cata reduceLeaf reduceInternal opTree
                tree)
            |> Arb.fromGen
        let guaranteedLeafs<'a> = 
            let leafGen = leafOnly<'a> |> Arb.toGen

            let internalGen = gen {
                let! op = Arb.generate<Combinator<'a>>
                let! guaranteedLeaves = leafGen.NonEmptyListOf()
                let! otherBranches = Arb.generate<Constraint<'a> list>
                let allChildren = [List.ofSeq guaranteedLeaves; otherBranches] |> List.concat
                return Combinator (op, allChildren)
            }

            Gen.oneof [leafGen; internalGen] |> Arb.fromGen

    type DefaultCustomArb =
        static member IComparableInt() = 
            Arb.generate<int>
            |> Gen.map (fun i -> i :> IComparable<int>)
            |> Arb.fromGen

        static member Regex() =
            Arb.generate<Guid>
            |> Gen.map (string >> System.Text.RegularExpressions.Regex)
            |> Arb.fromGen