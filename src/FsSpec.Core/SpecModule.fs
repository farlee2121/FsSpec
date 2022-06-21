namespace FsSpec

module Explanation = 
    type SpecResult<'a> = Result<'a,'a> 
    module SpecResult = 
        let map f result= 
            match result with
            | SpecResult.Ok s -> Ok (f s)
            | SpecResult.Error s -> Error (f s)

        let get (result:SpecResult<'a>) = 
            match result with
            | SpecResult.Ok s -> s
            | SpecResult.Error s -> s


    type Explanation<'a> =
        | Leaf of SpecResult<SpecLeaf<'a>>
        | Combinator of SpecResult<Combinator<'a>> * (Explanation<'a> list)
    let rec cata fLeaf fNode (spec:Explanation<'a>) :'r = 
        let recurse = cata fLeaf fNode  
        match spec with
        | Explanation.Leaf leafInfo -> 
            fLeaf leafInfo 
        | Explanation.Combinator (nodeInfo,subtrees) -> 
            fNode nodeInfo (subtrees |> List.map recurse)

    let isOk = function
        | Leaf (Ok _) -> true
        | Combinator (Ok _, _) -> true
        | _ -> false

    type ValueExplanation<'a> = {
        Value: 'a
        Explanation: Explanation<'a>
    }

module Spec = 
    let rec cata fLeaf fNode (spec:Spec<'a>) :'r = 
        let recurse = cata fLeaf fNode  
        match spec with
        | Spec.SpecLeaf leafInfo -> 
            fLeaf leafInfo 
        | Spec.Combinator (nodeInfo,subtrees) -> 
            fNode nodeInfo (subtrees |> List.map recurse)

    let rec fold fLeaf fNode acc (spec:Spec<'a>) :'r = 
        let recurse = fold fLeaf fNode  
        match spec with
        | Spec.SpecLeaf leafInfo -> 
            fLeaf acc leafInfo 
        | Spec.Combinator (nodeInfo,subtrees) -> 
            let localAccum = fNode acc nodeInfo
            let finalAccum = subtrees |> List.fold recurse localAccum 
            finalAccum 
    
    
    let max m = Spec.SpecLeaf(Max m)
    let min m = Spec.SpecLeaf (Min m)
    let regex pattern : Spec<string> = Spec.SpecLeaf (Regex (System.Text.RegularExpressions.Regex(pattern)))
    let minLength length : Spec<#System.Collections.IEnumerable>  = 
        if (length < 0) then invalidArg (nameof length) "Lengths must be positive"
        Spec.SpecLeaf (MinLength length)
    let maxLength length : Spec<#System.Collections.IEnumerable> = 
        if (length < 0) then invalidArg (nameof length) "Lengths must be positive"
        Spec.SpecLeaf (MaxLength length)
    let predicate description pred : Spec<'a> = Spec.SpecLeaf (Custom (description, pred))
    let (&&&) left right = Spec.Combinator (And, [left; right])
    let (|||) left right = Spec.Combinator (Or, [left; right])
    let all specs = Spec.Combinator (And, specs)
    let any specs = Spec.Combinator (Or, specs)
    let internal none = Spec.SpecLeaf SpecLeaf.None
    let is<'a> : Spec<'a> = none

    let internal trimEmptyBranches spec =
        let isEmptyCombinator = function
            | Combinator (_, []) -> true
            | _ -> false

        let fLeaf leaf = SpecLeaf leaf
        let fBranch comb children = 
            Combinator (comb, children |> List.filter (not << isEmptyCombinator))
        let trim = cata fLeaf fBranch

        let trimmed = trim spec 
        if isEmptyCombinator trimmed then none else trimmed



    let explain spec value : Explanation.ValueExplanation<'a> = 
        let fLeaf leaf = 
            let isValid =
                match leaf with
                    | None -> (fun _ -> true)
                    | Max max  as c -> DefaultValidators.validateMax max
                    | Min min -> DefaultValidators.validateMin min
                    | Regex expr -> DefaultValidators.validateRegex expr
                    | MinLength minLen -> DefaultValidators.validateMinLength minLen
                    | MaxLength maxLen -> DefaultValidators.validateMaxLength maxLen
                    | Custom(_, pred) as leaf -> pred

            if isValid value then Explanation.Leaf (Ok leaf) else Explanation.Leaf (Error leaf)

        let fComb comb (childResults: Explanation.Explanation<'a> list) = 
            match comb with
            | And -> 
                let passStatus =
                    match childResults with
                    | [] -> Ok 
                    | kids -> if kids |> List.forall Explanation.isOk then Ok else Error
                Explanation.Combinator ((passStatus comb), childResults)
            | Or -> 
                let passStatus =
                    match childResults with
                    | [] -> Ok 
                    | kids -> if kids |> List.exists Explanation.isOk then Ok else Error
                Explanation.Combinator (passStatus comb, childResults)
        
        let expl = spec |> trimEmptyBranches |> cata fLeaf fComb
        { Value = value; Explanation = expl }



    let validate spec value = 
        let { Explanation.Explanation = explanation } = explain spec value

        if Explanation.isOk explanation
        then Result.Ok value
        else Result.Error explanation

    let isValid spec value =
        match validate spec value with
        | Ok _ -> true
        | Error _ -> false

    let depth (specTree:Spec<'a>) =
        let rec recurse subtree = 
            match subtree with
            | Spec.SpecLeaf _ ->  1
            | Spec.Combinator (_, children) as c -> 
                1 + (children |> List.map recurse 
                    |> (function | [] -> 0 | l -> List.max l))
        recurse specTree
            
    module Internal = 
        let isLeafValidForType (leaf:SpecLeaf<'a>) = 
            match leaf with
            | Regex _ as leaf -> 
                typeof<string>.IsAssignableFrom(typeof<'a>)
            | Min _ | Max _ -> 
                typeof<System.IComparable<'a>>.IsAssignableFrom(typeof<'a>)
            | MaxLength _ | MinLength _ -> typeof<System.Collections.IEnumerable>.IsAssignableFrom(typeof<'a>)
            | Custom _ | None -> true
        