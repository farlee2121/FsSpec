module CustomGenerators

open FsCheck
open FsSpec.Tests.TreeModel
open FsSpec
open System

type AllListsNonEmpty =
    static member List () =
        Arb.generate<NonEmptyArray<'a>> |> Gen.map (fun a -> a.Get |> List.ofArray) |> Arb.fromGen

module SpecGen =
    let leafOnly<'a> = Arb.generate<SpecLeaf<'a>> 
                        |> Gen.map SpecLeaf
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
            let! otherBranches = Arb.generate<Spec<'a> list>
            let allChildren = [List.ofSeq guaranteedLeaves; otherBranches] |> List.concat
            return Combinator (op, allChildren)
        }

        Gen.oneof [leafGen; internalGen]

    let impossibleLeafs = 
        let minGreaterThanMax = Arb.generate<int> |> Gen.map (fun i -> Spec.all [Spec.min (i+1); Spec.max i])
        let regex = Gen.constant (SpecLeaf (Regex (System.Text.RegularExpressions.Regex("\d"))))
        Gen.oneof [minGreaterThanMax; regex]

    let validLeafForType<'a> = 
        Arb.generate<SpecLeaf<'a>> 
        |> Gen.filter FsSpec.FsCheck.Constraint.Internal.isLeafValidForType
        |> Gen.filter (function Custom _ -> false | _ -> true)

    let withLeafGen (leafGen:Gen<SpecLeaf<'a>>) = 
        let branchOrLeaf = Gen.oneof [
            leafGen |> Gen.map SpecLeaf
            Arb.generate<Combinator<'a>> |> Gen.map (fun op -> Combinator (op, []))
        ]
        let maxDepth = 10
        let rec recurse depth parent=
            if depth = maxDepth
            then leafGen |> Gen.map SpecLeaf |> Gen.sample 0 1 |> List.head
            else
                match parent with
                | SpecLeaf _ as leaf-> leaf
                | Combinator (op, _) -> (Combinator (op, 
                    branchOrLeaf
                    |> Gen.nonEmptyListOf 
                    |> Gen.sample 0 1 |> List.head 
                    |> List.map (recurse (depth+1))
                ))

        branchOrLeaf |> Gen.map (recurse 0)

    let onlyLeafsForType<'a> = 
        withLeafGen validLeafForType<'a> 

    let noEmptyBranches<'a> = withLeafGen Arb.generate<SpecLeaf<'a>>


type LeaflessSpecTree<'a> = | LeaflessSpecTree of Spec<'a>
    with
        member this.Spec = match this with | LeaflessSpecTree c -> c 

type LeafOnly<'a> = | LeafOnly of Spec<'a>
    with
        member this.Spec = match this with | LeafOnly c -> c 

type GuaranteedLeafs<'a> = | GuaranteedLeafs of Spec<'a>
    with
        member this.Spec = match this with | GuaranteedLeafs c -> c 

type ImpossibleIntSpec = | ImpossibleIntSpec of Spec<int>
    with
        member this.Spec = match this with | ImpossibleIntSpec c -> c 

type OnlyLeafsForType<'a> = | OnlyLeafsForType of Spec<'a>
    with
        member this.Spec = match this with | OnlyLeafsForType c -> c 

type NoEmptyBranches<'a> = | NoEmptyBranches of Spec<'a>
    with
        member this.Spec = match this with | NoEmptyBranches c -> c 

type DefaultSpecArbs =
    static member IComparable<'a when 'a :> IComparable<'a>>() = 
        Arb.generate<'a>
        |> Gen.map (fun i -> i :> IComparable<'a>)
        |> Arb.fromGen

    static member Regex() =
        Arb.generate<Guid>
        |> Gen.map (string >> System.Text.RegularExpressions.Regex)
        |> Arb.fromGen

    static member LeaflessSpecTree () = SpecGen.noLeafs |> Gen.map LeaflessSpecTree |> Arb.fromGen
    static member LeafOnly () = SpecGen.leafOnly |> Gen.map LeafOnly |> Arb.fromGen
    static member GuaranteedLeafs () = SpecGen.guaranteedLeafs |> Gen.map GuaranteedLeafs |> Arb.fromGen
    static member ImpossibleIntSpec () = SpecGen.impossibleLeafs |> Gen.map ImpossibleIntSpec |> Arb.fromGen    
    static member OnlyLeafsForType () = SpecGen.onlyLeafsForType |> Gen.map OnlyLeafsForType |> Arb.fromGen    
    static member NoEmptyBranches () = SpecGen.noEmptyBranches |> Gen.map NoEmptyBranches |> Arb.fromGen