module PathOfSupporting.Internal.Helpers // purpose things that likely would be good in BReusable, but aren't in the canonical file - https://github.com/ImaginaryDevelopment/FsInteractive/blob/master/BReusable.fs
open System
open System.Collections.Generic
open System.Linq
open System.Runtime.ExceptionServices

[<NoComparison>]
type PoSException =
    |Exception of exn
    |Rethrowable of ExceptionDispatchInfo
    |RelevantData of Map<string,obj>
    with
        // for C# consumption
        member x.UnwrapException =
            match x with
            | Exception e -> e
            | Rethrowable r -> r.SourceException
            | RelevantData m ->
                let ex = System.Exception("UnknownException:Any Relevant information in this.Data")
                m
                |> Map.iter(fun k v ->
                    ex.Data.Add(k,v)
                )
                ex
        static member AddData (key:string) (value:obj) =
            function
        //match exOpt with
        //| None -> RelevantData (Map[key,value])
            |  Exception ex->
                ex.Data.Add(key,value)
                Exception ex
            | Rethrowable edi ->
                edi.SourceException.Data.Add(key,value)
                Rethrowable edi
            |  RelevantData m ->
                m.Add(key,value)
                |> RelevantData


type PoSError = string * PoSException option
// type alias definitions aren't perpetuated into tooltips, so the below doc comment helps
/// Result<'t,PoSError>
type PoSResult<'t> = Result<'t,PoSError>

let errMsg msg :PoSResult<'t> = Result.Error(msg,None)
let errMsgEx msg (ex:exn) :PoSResult<'t> = Result.Error(msg,Some (Exception ex))
// could name the extensions Error (but then... what is that helping?
type Result<'t,'tErr> with
    static member TryGetValue x = match x with | Ok x -> Some x | _ -> None
    static member ErrMsg msg = errMsg msg
    static member Ex msg ex = errMsgEx msg ex
    static member ExDI msg ex :PoSResult<_> = Result.Error(msg,Some <| Rethrowable (ExceptionDispatchInfo.Capture ex))
    static member ErrAdd key value ((msg,x):PoSError) :PoSError =
        let x =
            x
            |> Option.map (PoSException.AddData key value)
            |> Option.defaultValue (RelevantData <| Map[key,value])
        (msg,Some x)

module Option =
    let ofOk =
        function
        | Ok x -> Some x
        | _ -> None

module AsyncSeq =
    open FSharp.Control

    let rateLimit rateInMilliseconds (stream:AsyncSeq<_>) =
        asyncSeq{
            let sw  = System.Diagnostics.Stopwatch()
            sw.Start()
            for x in stream do
                yield x
                sw.Stop()
                if sw.Elapsed.Milliseconds < rateInMilliseconds then
                    let remainder = rateInMilliseconds - sw.Elapsed.Milliseconds
                    // printfn "Elapsed %ims. Sleeping for %ims" sw.Elapsed.Milliseconds remainder
                    System.Threading.Thread.Sleep remainder
                    sw.Reset()
                    sw.Start()
        }

module Seq =
    let tryTail items =
        items
        |> Seq.mapi(fun i x -> if i = 0 then None else Some x)
        |> Seq.choose id

    let unflatten fIsHeader fHeader fChild =
        Seq.fold(fun grouped next ->
            if fIsHeader next then
                (fHeader next,[]) :: grouped
            else
                match grouped with
                | (heading,children) :: tail ->
                    (heading, fChild next :: children) :: tail
                | _ -> failwith "no head found"
        ) []
        >> List.map(fun (x,y) -> x, List.rev y)
        >> List.rev

    // return item 1, if item 2's key is different from 1, return it
    // if item 3's key is different from 2 return it
    // and so on
    let changes f =
        let mutable lastKey = None
        Seq.choose(fun x ->
            let key = f x
            match lastKey with
            | Some lastKey when key = lastKey -> None
            | _ ->
                lastKey <- Some key
                Some x
        )
    let rateLimit limit items =
        seq{
            let sw = System.Diagnostics.Stopwatch()
            sw.Start()
            for item in items do
                yield item
                sw.Stop()
                if sw.Elapsed.Milliseconds < limit then
                    let remainder = limit - sw.Elapsed.Milliseconds
                    // printfn "Elapsed %ims. Sleeping for %ims" sw.Elapsed.Milliseconds remainder
                    System.Threading.Thread.Sleep remainder
                    sw.Reset()
                    sw.Start()
        }

module Reflection =
    open BReusable
    open Microsoft.FSharp.Reflection

    // we'd like to test if it is a union, but... no clue how
    let getUnionInfo =
        function
        | UnsafeNull -> None
        | x ->
            let t = x.GetType()
            try
                let uInfo,fv = FSharpValue.GetUnionFields(x,t)
                Some (t,uInfo,fv)
            with _ -> None
    let (|Option|_|) (caseInfo:UnionCaseInfo)=
        if caseInfo.Name = "Some" && caseInfo.DeclaringType.IsClass && caseInfo.DeclaringType.IsGenericType then
            if caseInfo.DeclaringType.GetGenericTypeDefinition() = typedefof<Option<_>> then
                Some ()
            else None
        else None


    // Print/display options as 't instead of 't option
    let fDisplay (x:'t) :string =
        let rec fIt x =
            match x with
            | UnsafeNull -> sprintf "%A" x // using whatever is built-in to display null
            | x ->
                getUnionInfo x
                |> Option.map(
                    function
                    | _,uInfo, [| |] -> uInfo.Name
                    // scrape out "Some" from Option displays
                    | _, Option, [| x |] -> fIt x
                    | _, Option, values -> values |> Array.map fIt |> sprintf "%A"
                    | _,uInfo,[| x |] ->
                        sprintf "%s(%s)" uInfo.Name <| fIt x
                    | (_,uInfo,fieldValues) ->
                        sprintf "%A %A" uInfo.Name (fieldValues |> Array.map fIt)
                ) |> Option.defaultValue (sprintf "%A" x)
        fIt x

[<AutoOpen>]
module Utils =
    open System.IO
    let dump titleOpt extensionOverrideOpt (f:_ -> obj) (x:'t) =
        let extension = defaultArg extensionOverrideOpt "json"
        if System.Diagnostics.Debugger.IsAttached then
            let vsCodePath = @"C:\Program Files (x86)\Microsoft VS Code\bin\code.cmd"
            if File.Exists vsCodePath then
                // write the xml out to temp for inspection
                let tmp =
                    titleOpt
                    |> function
                        |Some (title:string) ->
                            if Path.IsPathRooted title then
                                title
                            else Path.Combine(Path.GetTempPath(),sprintf "%s.%s" title extension)
                        | None -> 
                            let fp = Path.GetTempFileName()
                            // we want to control the extension
                            let path =
                                let fn = Path.GetFileNameWithoutExtension fp
                                sprintf "%s.%s"  fn extension
                            Path.Combine(Path.GetDirectoryName fp, path)
                match f x with
                | null -> "null"
                | :? string as txt -> txt
                | x -> Reflection.fDisplay x
                |> fun text ->
                    File.WriteAllText(tmp,text)
                // without full path it seemingly ignored the command
                // might be something else in path that code.cmd pulls in
                if PathOfSupporting.Configuration.allowProcessStart then
                    System.Diagnostics.Process.Start(vsCodePath,tmp)
                    |> ignore

module Async =
    let map f x =
        async{
            let! x = x
            return f x
        }
    let bind f x =
        async{
            let! x = x
            let! x = f x
            return x
        }
    let flatten (seqs:Async<_> seq) =
        async{
            let items = ResizeArray<_>()
            for a in seqs do
                let! x = a
                items.Add x
            return items :> seq<_>
        }


(*
    // the below code works, but was specific to discord, not needed here

    // http://www.fssnip.net/hv/title/Extending-async-with-await-on-tasks
    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(t:System.Threading.Tasks.Task<'t>, f:'t -> Async<'r>) : Async<'r> =
            x.Bind(Async.AwaitTask t,f)
        member x.Bind(t:System.Threading.Tasks.Task, f:unit -> Async<unit>) : Async<unit> =
            x.Bind(Async.AwaitTask t,f)

        // based on https://github.com/RogueException/Discord.Net/blob/ff0fea98a65d907fbce07856f1a9ef4aebb9108b/src/Discord.Net.Core/Extensions/AsyncEnumerableExtensions.cs
        member x.Bind(e:IAsyncEnumerable<IEnumerable<'t>>,f) =
            let t = e.SelectMany(fun y -> y.ToAsyncEnumerable()).ToArray()
            x.Bind(t,f)
        member x.Bind(e:IAsyncEnumerable<IReadOnlyCollection<'t>>,f) =
            let t = e.SelectMany(fun y -> y.ToAsyncEnumerable()).ToArray()
            x.Bind(t,f)

        //member __.For(e:IAsyncEnumerable<IEnumerable<'t>>,f) = e.SelectMany(fun y -> y.ToAsyncEnumerable()).ForEachAsync(Action<_>(f))
        member __.For(e:IAsyncEnumerable<IReadOnlyCollection<'t>>,f) = e.SelectMany(fun y -> y.ToAsyncEnumerable()).ForEach(Action<_>(f))

*)

module SuperSerial =
    open Newtonsoft.Json
    let inline serialize (x:_) = JsonConvert.SerializeObject(value=x)
    let inline deserialize<'t> x :PoSResult<'t> =
        try
            JsonConvert.DeserializeObject<'t>(x)
            |> Result.Ok
        with ex ->
            let msg =sprintf "Error deserialization failed:%s" ex.Message
            System.Diagnostics.Trace.WriteLine msg
            Result.Ex msg ex
    let  inline serializeXmlNodePretty (x:System.Xml.XmlNode) = JsonConvert.SerializeXmlNode(x, Formatting.Indented)


module Storage =
    open System.IO
    open SuperSerial

    let private folderPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Discordantly")
    let private getKeyPath key = Path.Combine(folderPath,sprintf "%s.json" key)

    let private getKeyValue key =
        let keyPath = getKeyPath key
        if Directory.Exists folderPath && File.Exists keyPath then
            File.ReadAllText keyPath |> deserialize
        else Result.ErrMsg <| sprintf "Path not found %s" keyPath
    let private setKeyValue key value =
        if not <| Directory.Exists folderPath then
            Directory.CreateDirectory folderPath |> ignore
        let keyPath = getKeyPath key
        match value with
        | None ->
            if File.Exists keyPath then
                File.Delete keyPath
        | Some value ->
            File.WriteAllText(keyPath,serialize value)

    // allows simple creation of mated pairs of 't option with get,set
    let createGetSet<'t> key:(unit -> PoSResult<'t>)* ('t option -> unit)=
        let getter () =
            match getKeyValue key with
            | Error e ->
                eprintfn "I couldn't find my save file at %s" <| getKeyPath key
                Error e
            | Ok x ->
                Ok x
        let setter vOpt = setKeyValue key vOpt
        getter, setter

module StringPatterns =
    open BReusable
    open System.Text.RegularExpressions

    let (|Trim|) =
        function
        | NonValueString x -> x
        | ValueString x -> x.Trim()

    let (|RMatchGroup|_|) (pattern:string) (i:int) =
        function
        | RMatch pattern m ->
            if m.Groups.Count < i then
                invalidOp <| sprintf "Pattern captured %i groups, but %i was requested" m.Groups.Count i
            else
                Some m.Groups.[i].Value
        | _ -> None
    let (|RGroup|) (i:int) (m:Match) = m.Groups.[i].Value
    let (|RMatchI|_|) p text =
        let m = Regex.Match(text,p,RegexOptions.IgnoreCase)
        if m.Success then
            Some m
        else None
    let (|RMatches|_|) (p:string) (x:string) = 
        let r = Regex.Matches(x,p)
        if r.Count > 0 then
            Some r
        else None

    let (|EndsWithI|_|) d =
        function
        | null | "" -> None
        | _ when String.IsNullOrEmpty d -> invalidOp "Bad delimiter"
        | x when x.EndsWith(d, StringComparison.InvariantCultureIgnoreCase) -> Some ()
        | _ -> None

    let (|EqualsI|_|) d =
        function
        | null when isNull d -> Some()
        | null when not <| isNull d -> None
        | x -> if String.equalsI x d then Some() else None


    // advance/pluck next quoted token
    // expects something surrounded by ' or "
    // passes back token, and remainder if any
    let (|Quoted|_|)=
        function
        | NonValueString _ -> None
        | RMatch @"^\s*'([^']+)'(\s|$)" x
        | RMatch @"^\s*""([^\""]+)\""(\s|$)" x as txt ->
            let token = x.Groups.[1].Value
            let i = x.Index+x.Length
            let rem = if txt.Length > i then txt.[i..] else String.Empty
            Some(token,rem)
        | _ -> None


module Xml =
    open System.Xml.Linq

    let toXName = XName.op_Implicit


    let getAttribValue name (xe:XElement) : string option =
        toXName name
        |> xe.Attribute
        |> Option.ofObj
        |> Option.map(fun x -> x.Value)

    let getAttribValueOrNull name (xe:XElement) : string =
        getAttribValue name xe
        |> Option.defaultValue null

    let getElement name (xe:XElement) = xe.Element(toXName name) |> Option.ofObj
    let getElements name (xe:XElement) = xe.Elements(toXName name)
    let getAllElements (xe:XElement) = xe.Elements()
    let getElementsByName name (xe:XElement) = xe.Elements(toXName name)

    open System.Xml

    // does this kill the root node? - // https://stackoverflow.com/questions/24743916/how-to-convert-xmlnode-into-xelement
    // concerns: https://stackoverflow.com/questions/24743916/how-to-convert-xmlnode-into-xelement
    // if there are issues try also my F# extensions.linq
    let toXmlNode (xe:XElement) =

        let xmlDoc = XmlDocument()
        (
            use xmlReader = xe.CreateReader()
            xmlDoc.Load(xmlReader)
        )
        xmlDoc.FirstChild

    let fromXmlNode (xn:XmlNode) =
        xn.CreateNavigator().ReadSubtree()
        |> XElement.Load

    let serializeXElementPretty (xe:XElement)=
        toXmlNode xe
        |> SuperSerial.serializeXmlNodePretty

    let dumpXE titleOpt extensionOverrideOpt (xe:XElement) = dump titleOpt extensionOverrideOpt (serializeXElementPretty>>box) xe

module Api =
    open PathOfSupporting.Configuration
    open PathOfSupporting.Internal.BReusable

    type RetryType=
    | Immediate
    | Rest of milliSeconds:int
    type RetryBehavior =
        // allows 0
        | Retries of int
        | Infinite

    let fetch (target:string):Async<PoSResult<_>> =
        match target with
        | ValueString _ ->
            async{
                use client = new System.Net.Http.HttpClient()
                let! result = client.GetStringAsync target |> Async.AwaitTask
                return Ok result
            }
        | _ ->
            async{
                return Error("Target was null, empty, or whitespace",None)
            }
        |> Async.Catch
        |> Async.map(function
            |Choice1Of2 x -> x
            |Choice2Of2 ex -> Result.ExDI (sprintf "Exception fetching %s" target) ex
        )

[<RequireQualifiedAccess>]
module Json =
    open Newtonsoft.Json.Linq

    [<NoComparison>]
    type Wrap<'t when 't :> JToken and 't : null >  = private {node:'t}
        with
    //            static member Wrap n = {node=n}
            member x.Value:'t option= Option.ofObj x.node
            static member internal getValue (w:Wrap<_>):'t option = w.Value
            member x.ToDump() = x.Value |> Option.map(fun x -> x.ToString())
    // parent is RequireQualified, allow opening for symbol usage
    module Symbols =
        let (>&>) f1 f2 x = f1 x && f2 x

    open Symbols

    let wrap x = {node=x}
    let wrapOpt x = Option.defaultValue {node=null} x
    let map f =
        Wrap.getValue
        >> Option.map f
    let mapOrDefault f =
        Wrap.getValue
        >> Option.map (f>>wrap)
        >> Option.defaultValue {node=null}
    let mapOrNull f =
        Wrap.getValue
        >> Option.map f
        >> Option.defaultValue null
    let bind f =
        Wrap.getValue
        >> Option.bind f
    
