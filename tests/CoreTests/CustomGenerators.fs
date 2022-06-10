module CustomGenerators

open FsCheck
open FsSpec.Tests.TreeModel
open FsSpec.CustomTree
open System

type AllListsNonEmpty =
        static member List () =
            Arb.generate<NonEmptyArray<'a>> |> Gen.map (fun a -> a.Get |> List.ofArray) |> Arb.fromGen

module ConstraintGen =
    let leafOnly<'a> = Arb.generate<ConstraintLeaf<'a>> 
                        |> Gen.map ConstraintLeaf
    let noLeafs<'a> = 
        Arb.generate<Tree<Combinator<'a>, Combinator<'a>>> 
        |> Gen.map (fun opTree ->
            let reduceLeaf leaf = Combinator (leaf, []) 
            let reduceInternal op children = (Combinator (op, List.ofSeq children))
            let tree = Tree.cata reduceLeaf reduceInternal opTree
            tree)
    let guaranteedLeafs<'a> = 
        let leafGen = leafOnly<'a> 

        let internalGen = gen {
            let! op = Arb.generate<Combinator<'a>>
            let! guaranteedLeaves = leafGen.NonEmptyListOf()
            let! otherBranches = Arb.generate<Constraint<'a> list>
            let allChildren = [List.ofSeq guaranteedLeaves; otherBranches] |> List.concat
            return Combinator (op, allChildren)
        }

        Gen.oneof [leafGen; internalGen]

type LeaflessConstraintTree<'a> = | LeaflessConstraintTree of Constraint<'a>
    with
        member this.Constraint = match this with | LeaflessConstraintTree c -> c 

type LeafOnly<'a> = | LeafOnly of Constraint<'a>
    with
        member this.Constraint = match this with | LeafOnly c -> c 

type GuaranteedLeafs<'a> = | GuaranteedLeafs of Constraint<'a>
    with
        member this.Constraint = match this with | GuaranteedLeafs c -> c 


type DefaultConstraintArbs =
    static member IComparableInt() = 
        Arb.generate<int>
        |> Gen.map (fun i -> i :> IComparable<int>)
        |> Arb.fromGen

    static member Regex() =
        Arb.generate<Guid>
        |> Gen.map (string >> System.Text.RegularExpressions.Regex)
        |> Arb.fromGen

    static member LeaflessConstraintTree () = ConstraintGen.noLeafs |> Gen.map LeaflessConstraintTree |> Arb.fromGen
    static member LeafOnly () = ConstraintGen.leafOnly |> Gen.map LeafOnly |> Arb.fromGen
    static member GuaranteedLeafs () = ConstraintGen.guaranteedLeafs |> Gen.map GuaranteedLeafs |> Arb.fromGen