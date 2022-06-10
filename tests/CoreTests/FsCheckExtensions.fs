namespace FsCheck.FSharp

module Gen = 
    let ofType<'a> = ArbMap.defaults |> ArbMap.generate<'a> 
