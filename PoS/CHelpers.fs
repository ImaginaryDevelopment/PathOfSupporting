/// open this file to get extension methods imported to make use from C# easier for the spots it is rough
[<System.Runtime.CompilerServices.Extension>]
module CHelpers
open System
open System.Runtime.CompilerServices
open PathOfSupporting.Internal.BReusable

// allow nulls for C# consumers
// will work with non-nullables from C# only, this is intended
[<Extension>]
let GetOkOrNull(x:Result<_,_>) =
    match x with
    | Ok v -> v
    | _-> null

[<Extension>]
let GetErrOrDefault(x:Result<_,_>) =
    match x with
    | Error (msg,exOpt) -> (msg, exOpt |> Option.defaultValue null)
    // apparently Tuple is not a class, so we can't return null here
    | _ -> Unchecked.defaultof<_>

// will work with non-nullables from C# only, this is intended
[<Extension>]
let GetOrDefault(x:_ option) =
    match x with
    | Some x -> x
    | None -> null

[<Extension>]
let ProcessResult(result:PathOfSupporting.Internal.Helpers.PoSResult<_>, fOk, fError) =
    match result with
    | Ok x -> Action.invoke1 fOk x
    | Error (m,Some ex) -> Action.invoke2 fError m ex
    | Error (m,None) -> Action.invoke2 fError m null




