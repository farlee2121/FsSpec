module CustomGenerators

open FsCheck
open FsSpec.Tests.TreeModel
open FsSpec
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

    let impossibleLeafs = 
        let minGreaterThanMax = Arb.generate<int> |> Gen.map (fun i -> Constraint.all [Constraint.min (i+1); Constraint.max i])
        let regex = Gen.constant (ConstraintLeaf (Regex (System.Text.RegularExpressions.Regex("\d"))))
        Gen.oneof [minGreaterThanMax; regex]

    let validLeafForType<'a> = 
        Arb.generate<ConstraintLeaf<'a>> 
        |> Gen.filter FsSpec.FsCheck.Gen.Internal.isLeafValidForType
        |> Gen.filter (function Custom _ -> false | _ -> true)

    let withLeafGen (leafGen:Gen<ConstraintLeaf<'a>>) = 
        let branchOrLeaf = Gen.oneof [
            leafGen |> Gen.map ConstraintLeaf
            Arb.generate<Combinator<'a>> |> Gen.map (fun op -> Combinator (op, []))
        ]
        let maxDepth = 10
        let rec recurse depth parent=
            if depth = maxDepth
            then parent
            else
                match parent with
                | ConstraintLeaf _ as leaf-> leaf
                | Combinator (op, _) -> (Combinator (op, 
                    branchOrLeaf
                    |> Gen.nonEmptyListOf 
                    |> Gen.sample 0 1 |> List.head 
                    |> List.map (recurse (depth+1))
                ))

        branchOrLeaf |> Gen.map (recurse 0)

    let onlyLeafsForType<'a> = 
        withLeafGen validLeafForType<'a> 


type LeaflessConstraintTree<'a> = | LeaflessConstraintTree of Constraint<'a>
    with
        member this.Constraint = match this with | LeaflessConstraintTree c -> c 

type LeafOnly<'a> = | LeafOnly of Constraint<'a>
    with
        member this.Constraint = match this with | LeafOnly c -> c 

type GuaranteedLeafs<'a> = | GuaranteedLeafs of Constraint<'a>
    with
        member this.Constraint = match this with | GuaranteedLeafs c -> c 

type ImpossibleIntConstraint = | ImpossibleIntConstraint of Constraint<int>
    with
        member this.Constraint = match this with | ImpossibleIntConstraint c -> c 

type OnlyLeafsForType<'a> = | OnlyLeafsForType of Constraint<'a>
    with
        member this.Constraint = match this with | OnlyLeafsForType c -> c 

type DefaultConstraintArbs =
    static member IComparable<'a when 'a :> IComparable<'a>>() = 
        Arb.generate<'a>
        |> Gen.map (fun i -> i :> IComparable<'a>)
        |> Arb.fromGen

    static member Regex() =
        Arb.generate<Guid>
        |> Gen.map (string >> System.Text.RegularExpressions.Regex)
        |> Arb.fromGen

    static member LeaflessConstraintTree () = ConstraintGen.noLeafs |> Gen.map LeaflessConstraintTree |> Arb.fromGen
    static member LeafOnly () = ConstraintGen.leafOnly |> Gen.map LeafOnly |> Arb.fromGen
    static member GuaranteedLeafs () = ConstraintGen.guaranteedLeafs |> Gen.map GuaranteedLeafs |> Arb.fromGen
    static member ImpossibleIntConstraint () = ConstraintGen.impossibleLeafs |> Gen.map ImpossibleIntConstraint |> Arb.fromGen    
    static member OnlyLeafsForType () = ConstraintGen.onlyLeafsForType |> Gen.map OnlyLeafsForType |> Arb.fromGen