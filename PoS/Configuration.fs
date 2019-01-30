module PathOfSupporting.Configuration

/// global debug flag, code in this lib should listen as to whether or not to console/debug/trace error output
let mutable debug = true
let mutable allowProcessStart = false

type Dumper =
    abstract member Dump: 't -> 't
    abstract member Dump: 't * maxDepth:int -> 't
    abstract member Dump: 't * description:string * ?maxDepth:int -> 't

/// for non-linqpad consumers: this provides display helpers
/// for linqpad consumption:
/// helpers so that the output in linqpad isn't cluttered by `Some _` table wrapper(s)
module Pad =
    let mutable dumper =
        {new Dumper with
            member __.Dump(x) =
                printfn "%A" x
                x
            member __.Dump(x, _maxDepth:int) =
                printfn "%A" x
                x
            member __.Dump(x,description:string, ?_maxDepth) =
                printfn "%s:%A" description x
                x
        }
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
    let Dump x = dumper.Dump(x)
    [<Extension>]
    type Extensions =
        [<Extension>]
        static member Dump x = Dump x
        [<Extension>]
        static member Dump<'t> (x:'t,title:string) = dumper.Dump(x,title)
        [<Extension>]
        static member Dump(x:'t, maxDepth:int) = dumper.Dump(x,maxDepth)

    type Option'<'t> = Option<'t>
    let unwrapOpt = id

#endif
