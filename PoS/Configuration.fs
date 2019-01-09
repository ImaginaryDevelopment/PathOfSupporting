module PathOfSupporting.Configuration

/// global debug flag, code in this lib should listen as to whether or not to console/debug/trace error output
let mutable debug = true
let mutable allowProcessStart = false
/// for non-linqpad consumers: this provides display helpers
/// for linqpad consumption:
/// helpers so that the output in linqpad isn't cluttered by `Some _` table wrapper(s)
module Pad =
    open System.Runtime.CompilerServices

#if LINQPAD
    type Option'<'t>(x:'t option) =
        member __.Value = x
        static member op_Implicit(x:Option'<'t>):Option<'t> = x.Value
        member x.ToDump() = sprintf "%A" x
        override x.ToString() = sprintf "%A" x.Value
       
    let none = Option'(None)
    let some x = Option'(Some x)
    let (|Some'|None'|) (x:Option'<'t>) =
        match x.Value with
        | Some v -> Some' v
        | None -> None'
    let unwrapOpt =
        function
        | Some' x -> Some x
        | None' -> None

#else
    let some x = Some x
    let none = None
    let (|Some'|None'|) =
        function
        | Some x -> Some' x
        | None -> None'
    [<Extension>]
    type Extensions =
        [<Extension>]
        static member Dump (x:obj) = printfn "%A" x
        [<Extension>]
        static member Dump (x:obj,title:string) = printfn "%s:%A" title x
    type Option'<'t> = Option<'t>
    let Dump x = x.Dump(); x
    let unwrapOpt = id

#endif
