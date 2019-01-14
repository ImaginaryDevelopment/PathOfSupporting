// TODO: path of exile trade api: https://www.reddit.com/r/pathofexiledev/comments/7aiil7/how_to_make_your_own_queries_against_the_official/
// source of some good things to translate perhaps: https://gitlab.com/vtopan/path-of-exile-python-3-sample-tools
// some tooling discussion at https://www.reddit.com/r/pathofexiledev/
// all the mods? https://www.pathofexile.com/api/trade/data/stats
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
