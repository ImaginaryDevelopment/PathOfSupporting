/// open this file to get extension methods imported to make use from C# easier for the spots it is rough
namespace CHelpers
open System
open System.Runtime.CompilerServices
open PathOfSupporting.Internal.BReusable
open FSharp.Control

[<System.Runtime.CompilerServices.Extension>]
// using a type, so we can have C# friendly overloads
type Extensions =
    // allow nulls for C# consumers
    // will work with non-nullables from C# only, this is intended
    [<Extension>]
    static member GetOkOrNull(x:Result<_,_>) =
        match x with
        | Ok v -> v
        | _-> null

    [<Extension>]
    static member GetErrOrDefault(x:Result<_,_>) =
        match x with
        | Error (msg,exOpt) -> (msg, exOpt |> Option.defaultValue null)
        // apparently Tuple is not a class, so we can't return null here
        | _ -> Unchecked.defaultof<_>

    // will work with non-nullables from C# only, this is intended
    [<Extension>]
    static member GetOrDefault(x:_ option) =
        match x with
        | Some x -> x
        | None -> null

    [<Extension>]
    static member ProcessResult(result:PathOfSupporting.Internal.Helpers.PoSResult<_>, fOk, fError) =
        match result with
        | Ok x -> Action.invoke1 fOk x
        | Error (m,Some ex) -> Action.invoke2 fError m (Some ex)
        | Error (m,None) -> Action.invoke2 fError m None

    /// helper such that we don't have to create a new List just to make it a more familiar to C# consumers
    [<Extension>]
    static member toList(x:_ list) = x :> System.Collections.Generic.IReadOnlyList<_>
    [<Extension>]
    static member ToNullable x = Option.toNullable x
    [<Extension>]
    static member GetValueOrDefault x = x |> Option.toNullable |> fun x -> x.GetValueOrDefault()
    [<Extension>]
    static member GetValueOrDefault (x,defaultValue) = x |> Option.toNullable |> fun x -> x.GetValueOrDefault(defaultValue)
    [<Extension>]
    static member ToEnumerable (x:IAsyncEnumerable<_>) = FSharp.Control.AsyncSeq.toBlockingSeq x



