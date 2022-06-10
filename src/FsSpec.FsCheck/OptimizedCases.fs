namespace FsSpec.FsCheck
open FsSpec
open FsCheck
open System

module OptimizedCases =

    type OptimizedCaseStrategy<'a> = ConstraintLeaf<'a> list -> Gen<'a> option

    let private isMax = (function | Max _ -> true | _ -> false)
    let private isMin = (function | Min _ -> true | _ -> false)
    let private isRegex = (function | Regex _ -> true | _ -> false)

    let private mapObj option = Option.map (fun o -> o :> obj) option
    let private cast<'b> (x:obj):'b =  
        match x with
        | :? 'a as n -> n 
        | _ -> invalidOp "Attempted to create generator from integer bound, but bound value was not an int"
        

    let boundedInt32Gen (leafs: ConstraintLeaf<'a> list) : obj option =
        match leafs :> System.Object with 
        | :? (ConstraintLeaf<int> list) as leafs ->
            match (List.tryFind isMin leafs), (List.tryFind isMax leafs) with
            | Some (Min (min)), Some (Max max) -> Some (Gen.choose (cast<int> min, cast<int> max))
            | Option.None, Some (Max max) -> Some (Gen.choose (Int32.MinValue, cast<int> max))
            | Some (Min min), Option.None -> Some (Gen.choose (cast<int> min, Int32.MaxValue))
            | _ -> Option.None
        | _ -> Option.None
        |> mapObj 

    let regexGen (leafs: ConstraintLeaf<'a> list) : obj option =
        let regexGen pattern = gen {
            let xeger = Fare.Xeger pattern
            return xeger.Generate() 
        }
                    
        match leafs :> System.Object with 
        | :? (ConstraintLeaf<string> list) as leafs ->
            match List.tryFind isRegex leafs with
            | Some (Regex regex)-> Some (regexGen (regex.ToString()))
            | _ -> Option.None
        | _ -> Option.None
        |> mapObj 

    let private strategies<'a> : (ConstraintLeaf<'a> list -> obj option) list = [
        boundedInt32Gen
        regexGen
    ]

    let strategiesInPriorityOrder<'a> ()  = 
        let restoreTyping strat = strat >> (Option.map cast<Gen<'a>>)
        strategies<'a> |> List.map restoreTyping
