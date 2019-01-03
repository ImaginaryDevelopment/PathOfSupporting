﻿open PathOfSupporting.Internal.BReusable.StringPatterns
open System

[<EntryPoint>]
let main argv =
    printfn "%A" argv
    match List.ofArray argv with
    | StringEqualsI "pob" :: pobLink :: [] ->
        PathOfSupporting.TreeParsing.PathOfBuildingParsing.Impl.parseCode pobLink
        |> printfn "%A"
        Console.ReadLine() |> ignore
    | _ -> Console.Error.WriteLine "Unknown command"
    0




