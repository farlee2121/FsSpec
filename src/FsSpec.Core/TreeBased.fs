module TreeBased


type Tree<'LeafData,'INodeData> =
    | LeafNode of 'LeafData
    | InternalNode of 'INodeData * Tree<'LeafData,'INodeData> seq

module Tree = 
    // Someone has to have made a version of this that is properly tail recursive...
    let rec cata fLeaf fNode (tree:Tree<'LeafData,'INodeData>) :'r = 
        let recurse = cata fLeaf fNode  
        match tree with
        | LeafNode leafInfo -> 
            fLeaf leafInfo 
        | InternalNode (nodeInfo,subtrees) -> 
            fNode nodeInfo (subtrees |> Seq.map recurse)

    let rec fold fLeaf fNode acc (tree:Tree<'LeafData,'INodeData>) :'r = 
        let recurse = fold fLeaf fNode  
        match tree with
        | LeafNode leafInfo -> 
            fLeaf acc leafInfo 
        | InternalNode (nodeInfo,subtrees) -> 
            // determine the local accumulator at this level
            let localAccum = fNode acc nodeInfo
            // thread the local accumulator through all the subitems using Seq.fold
            let finalAccum = subtrees |> Seq.fold recurse localAccum 
            // ... and return it
            finalAccum 

module DefaultValidations = 
    let validateMax value max = 
        match value with
        | v when v <= max -> Ok v
        | _ -> Error [$"{value} is greater than the max {max}"]

    let validateMin value min = 
        match value with
        | v when min <= v -> Ok v
        | _ -> Error [$"{value} is less than the min {min}"]

    let validateRegex value regex = 
        let regexTest = System.Text.RegularExpressions.Regex(regex);
        try
            match value :> System.Object with
            | :? System.String as str ->
                match regexTest.IsMatch(str) with
                | true -> Ok value
                | false -> Error [$"{value} didn't match expression {regex}"]
            | _ -> Error ["Invalid "]
        with
        | e -> Error [$"Cast to Object failed with exception: {e.Message}"]


    // these are really just the "and" and "or" operations for a result type. Would probably be better to create parameterized versions
    // then build up my specific case
    let validateAnd childResults = 
        let combine left right =
            match (left, right) with 
            | Ok _, Ok _ -> left
            | Ok _, Error err -> Error err
            | Error err, Ok _ -> Error err
            | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
        childResults |> List.reduce combine

    let validateOr childResults = 
        let combine left right =
            match (left, right) with 
            | Ok _, Ok _ -> left
            | Ok ok, Error _ -> Ok ok
            | Error err, Ok ok -> Ok ok
            | Error errLeft, Error errRight -> Error (List.concat [errLeft; errRight])
        childResults |> List.reduce combine


type Contraints = 
    | Max of int
    | Min of int
    | Regex of string

type Combinators = | And | Or

let max m = LeafNode (Max m)
let min m = LeafNode (Min m)
let matches expr = LeafNode (Regex expr)
let (&&&) left right = InternalNode (And, [left; right])
let (|||) left right = InternalNode (Or, [left; right])

let validate constraintTree value= 
    let fLeaf (op, res) leaf =
        let leafResult = 
            match leaf with // this case is a lot nicer feeling than piping a bunch of functions. Reads better
            | Max max -> DefaultValidations.validateMax value max
            | Min min -> DefaultValidations.validateMin value min
            | Regex expr -> DefaultValidations.validateRegex value expr
        match op with
        | And -> (op, DefaultValidations.validateAnd [res; leafResult])
        | Or -> (op, DefaultValidations.validateOr [res; leafResult])

    let fCombinator (_, res) newOp = (newOp, res)
    let (_, result) = Tree.fold fLeaf fCombinator (And, Ok value) constraintTree
    result

let test = min 1 &&& max 10

let r = validate test 7