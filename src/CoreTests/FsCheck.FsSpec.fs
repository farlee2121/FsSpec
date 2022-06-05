module GeneratorExperiment

module Gen = 
    open FsSpec.CustomTree
    open FsCheck

    let postwalk (constraints:Constraint<'a>) = 
        ()

    let normalizeToDistributedAnd (constraints:Constraint<'a>) = 
        // Really I want a post-order tree walk. This is where a generic tree implementation would be very helpful
        // does cata satisfy this?
        // Hmm, maybe it doesn't need to be post-order. I think I can do this just by recursive fold (or iterate until 1-layer deep or remains)
        let isLeaf = function | ConstraintLeaf _ -> true | _ -> false
        let distributeSingle andList = 
            let (nonCombinators, combinators) = andList |> List.partition isLeaf
            let distributeToCombinator toDistribute childConstr = 
                // something is fishy here. I expect all ands to end up in one layer topped by their nearest or
                // here I end up accounting for non-combinators twice, but I also need to account for the case there are no combinators at all
                // should I be able to end up with only one top-level or? 
                match childConstr with
                | Combinator (And, l) -> Combinator (And, Seq.concat [l; toDistribute])
                | Combinator (Or, l) -> Combinator (Or, l |> Seq.map (fun c -> Constraint.Factories.all (c :: (List.ofSeq toDistribute))))
            
            if List.isEmpty combinators then
                constraints
            else 
                // what do I really want this to look like? I 
                combinators 
                |> Seq.map distributeToCombinator nonCombinators
                |> Combinator
                    
            Constraint.Factories.any (andList |> Seq.map distributeToChild nonCombinators)

        match constraints with
        | Constraint.Combinator (And, children) -> distributeSingle children
        | _ -> constraints


    //let fromConstraint (constraints:Constraint<'a>) : Gen<'a> =
    //   Gen.
    //   Arb.generate<'a> |> 
    //   Arb.

