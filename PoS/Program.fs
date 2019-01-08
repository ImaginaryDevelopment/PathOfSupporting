#if !EXE
module PathOfSupporting.Program
#endif
open PathOfSupporting.Internal.BReusable.StringPatterns
open System
#if EXE
[<EntryPoint>]
#endif
let main argv =
    printfn "%A" argv
    match List.ofArray argv with
    | StringEqualsI "pob" :: pobLink :: [] ->
        PathOfSupporting.Parsing.PoB.PathOfBuildingParsing.Impl.parseCode pobLink
        |> printfn "%A"
        Console.ReadLine() |> ignore
    | _ -> Console.Error.WriteLine "Unknown command"
    0
