namespace FsSpec.FsCheck
open FsSpec
open FsCheck
open System


module OptimizedCases =
    
    module Gen = 

        let int16Range (min, max) = 
            let min = Option.defaultValue Int16.MinValue  min
            let max = Option.defaultValue Int16.MaxValue max
            if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

            Gen.choose (int32 min , int32 max)
            |> Gen.map int16

        let intRange (min, max) = 
            let min = Option.defaultValue Int32.MinValue min
            let max = Option.defaultValue Int32.MaxValue max
            if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

            Gen.choose (min, max)

        let int64Range (min,max) = 
            let min = Option.defaultValue Int64.MinValue min
            let max = Option.defaultValue Int64.MaxValue max
            if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"
    
            gen {
                return System.Random.Shared.NextInt64(min,max)
            }    

        let dateTimeRange (min,max) =
            let min = Option.defaultValue DateTime.MinValue min
            let max = Option.defaultValue DateTime.MaxValue max
            if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

            gen { 
                let! dateTimeTicks = int64Range(Some min.Ticks, Some max.Ticks)
                return new DateTime(ticks = dateTimeTicks)
            }

        let dateTimeOffsetRange (min,max) =
            let standardOffset = TimeSpan.Zero
            let normalizeOffset (x:DateTimeOffset) = x.ToOffset(standardOffset)

            let min = Option.defaultValue DateTimeOffset.MinValue min |> normalizeOffset
            let max = Option.defaultValue DateTimeOffset.MaxValue max |> normalizeOffset
            if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

            gen { 
                let! dateTimeTicks = int64Range(Some min.Ticks, Some max.Ticks)
                return DateTimeOffset(ticks = dateTimeTicks, offset = standardOffset)
            }

        let doubleRange (min,max) = 
            let toFinite = function
                | Double.PositiveInfinity -> Double.MaxValue
                | Double.NegativeInfinity -> Double.MinValue
                | f -> f

            let min = Option.defaultValue Double.MinValue min 
            let max = Option.defaultValue Double.MaxValue max
            if min > max then invalidArg "min,max" $"Max must be greater than min, got min: {min}, max: {max}"

            match min,max with 
            | Double.PositiveInfinity, _ -> 
                Gen.constant Double.PositiveInfinity
            | _, Double.NegativeInfinity -> 
                Gen.constant Double.NegativeInfinity
            | _ -> 
                gen {
                    let! n = Arb.generate<int>
                    let proportion = float n / float Int32.MaxValue 
                    let rangeSize = toFinite((min |> toFinite) - (max |> toFinite)) 
                    return (max |> toFinite) - Math.Abs(proportion * rangeSize) 
                }

        let singleRange (min,max) = 
            let min = Option.defaultValue Single.MinValue min 
            let max = Option.defaultValue Single.MaxValue max
            
            doubleRange (Some (double min), Some (double max))
            |> Gen.map single

    type OptimizedCaseStrategy<'a> = SpecLeaf<'a> list -> Gen<'a> option

    let private mapObj option = Option.map (fun o -> o :> obj) option
    let private cast<'b> (x:obj):'b =  
        match x with
        | :? 'a as n -> n 
        | _ -> invalidOp "Attempted to create generator from integer bound, but bound value was not an int"
        

    let tryFindRange leafs = 
        match (List.tryFind SpecLeaf.isMin leafs), (List.tryFind SpecLeaf.isMax leafs) with
        | Some (Min (min)), Some (Max max) -> (Some (cast<'a> min), Some (cast<'a> max))
        | Option.None, Some (Max max) -> (Option.None, Some (cast<'a> max))
        | Some (Min min), Option.None -> (Some (cast<'a> min), Option.None)
        | _ -> (Option.None, Option.None)

    let minMaxToRangedGen (rangeGen:('a option * 'a option -> Gen<'a>)) (leafs: SpecLeaf<'b> list) =
        match leafs :> System.Object with 
        | :? (SpecLeaf<'a> list) as leafs ->
            match tryFindRange leafs with
            | Option.None, Option.None -> Option.None
            | range -> rangeGen range |> Some
        | _ -> Option.None
        |> mapObj

    let regexGen (leafs: SpecLeaf<'a> list) : obj option =
        let regexGen pattern = gen {
            let xeger = Fare.Xeger pattern
            return xeger.Generate() 
        }
                    
        match leafs :> System.Object with 
        | :? (SpecLeaf<string> list) as leafs ->
            match List.tryFind SpecLeaf.isRegex leafs with
            | Some (Regex regex)-> Some (regexGen (regex.ToString()))
            | _ -> Option.None
        | _ -> Option.None
        |> mapObj 

    let private strategies<'a> : (SpecLeaf<'a> list -> obj option) list = [
        minMaxToRangedGen Gen.int16Range
        minMaxToRangedGen Gen.intRange
        minMaxToRangedGen Gen.int64Range
        minMaxToRangedGen Gen.singleRange
        minMaxToRangedGen Gen.doubleRange
        minMaxToRangedGen Gen.dateTimeOffsetRange
        minMaxToRangedGen Gen.dateTimeRange
        regexGen
    ]

    let strategiesInPriorityOrder<'a> ()  = 
        let restoreTyping strat = strat >> (Option.map cast<Gen<'a>>)
        strategies<'a> |> List.map restoreTyping
