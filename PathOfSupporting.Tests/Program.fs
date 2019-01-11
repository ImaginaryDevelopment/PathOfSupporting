// Learn more about F# at http://fsharp.org

open System
open Expecto

let tests =
    test "A simple test"{
        let subject = "Hello World"
        Expect.equal subject "Hello World" "The strings should be equal"
    }
[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let exitCode=Tests.runTestsWithArgs defaultConfig argv tests
    if System.Diagnostics.Debugger.IsAttached then
        Console.ReadLine() |> ignore
    exitCode

