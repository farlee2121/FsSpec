module CustomGenerators

open FsCheck
open FsSpec.Tests.TreeModel
open FsSpec
open System
open FsCheck.FSharp

type AllListsNonEmpty =
        static member List () =
            Gen.ofType<NonEmptyArray<'a>> |> Gen.map (fun a -> a.Get |> List.ofArray) |> Arb.fromGen

module ConstraintGen =
    let leafOnly<'a> = Gen.ofType<ConstraintLeaf<'a>> 
                        |> Gen.map ConstraintLeaf
    let noLeafs<'a> = 
        Gen.ofType<Tree<Combinator<'a>, Combinator<'a>>> 
        |> Gen.map (fun opTree ->
            let reduceLeaf leaf = Combinator (leaf, []) 
            let reduceInternal op children = (Combinator (op, List.ofSeq children))
            let tree = Tree.cata reduceLeaf reduceInternal opTree
            tree)
    let guaranteedLeafs<'a> = 
        let leafGen = leafOnly<'a> 

        let internalGen = gen {
            let! op = Gen.ofType<Combinator<'a>>
            let! guaranteedLeaves =  leafGen |> Gen.nonEmptyListOf
            let! otherBranches = Gen.ofType<Constraint<'a> list>
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
        Gen.ofType<int>
        |> Gen.map (fun i -> i :> IComparable<int>)
        |> Arb.fromGen

    static member Regex() =
        Gen.ofType<Guid>
        |> Gen.map (string >> System.Text.RegularExpressions.Regex)
        |> Arb.fromGen

    static member LeaflessConstraintTree () = ConstraintGen.noLeafs |> Gen.map LeaflessConstraintTree |> Arb.fromGen
    static member LeafOnly () = ConstraintGen.leafOnly |> Gen.map LeafOnly |> Arb.fromGen
    static member GuaranteedLeafs () = ConstraintGen.guaranteedLeafs |> Gen.map GuaranteedLeafs |> Arb.fromGen