namespace PathOfSupporting.StashAPI // translated from https://github.com/ImaginaryDevelopment/LinqPad/blob/master/LINQPad%20Queries/gamey/PoE%20stash%20tab%20api.linq

open System.Collections.Generic
open Newtonsoft.Json.Linq

type Item = {Name:string;NamePrefix:string; TypeLine:string; TypeLinePrefix:string; Verified:bool; Identified:bool; Corrupted:bool; League:string; Icon:string}

type Stash = {AccountName:string;LastCharacterName:string;Id:string; Stash:string;StashType:string;Public:bool; Items:Item[]}

module Impl =
    open PathOfSupporting
    open PathOfSupporting.Configuration
    open PathOfSupporting.Internal.BReusable.StringPatterns
    open PathOfSupporting.Internal.Helpers

    let stashTabApiUrl = "http://www.pathofexile.com/api/public-stash-tabs"
    let deserializeStashApi text = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,JToken>>(text)

    type RetryType=
        | Immediate
        | Rest of milliSeconds:int
    type TryRetryArguments = {RetryType:RetryType;FailureMessage:string; Retries:int}
    let rec asyncTryRetry tra f:Async<PoSResultDI<_>> =
        async{
            try
                let! result = f()
                if debug then printfn "asyncTryRetry worked"
                return Ok result
            with ex ->
                if tra.Retries> 0 then
                    if debug then eprintfn "Failed asyncTry, trying again"
                    match tra.RetryType with
                    | Immediate -> ()
                    | Rest ms -> System.Threading.Thread.Sleep(millisecondsTimeout=ms)
                    return! asyncTryRetry {tra with Retries= tra.Retries - 1} f
                else
                    if debug then eprintfn "Ran out of retries, asyncTry fail"
                    return Result.ExDI tra.FailureMessage ex
        }

    let fetchOne target changeIdOpt =
        let target = defaultArg target stashTabApiUrl
        use client = new System.Net.Http.HttpClient()
        let target = match changeIdOpt with | None -> target | Some x -> sprintf "%s?id=%s" target x
        let f () = client.GetStringAsync target |> Async.AwaitTask
        asyncTryRetry {FailureMessage="fetchOneFailed"; RetryType=Rest 800;Retries=2} f
        |> Async.RunSynchronously

    type SequenceState =
    | Start
    | Continue of nextChangeId:string
    | Finished

    let fetchSeq target (fContinue: string -> 'T option * SequenceState)=
        let dprintn x = if Configuration.debug then printfn "%s" x
        let f changeIdOpt =
            let result =
                fetchOne target changeIdOpt
                |> Result.GetOrRethrow
            dprintn "finished fetch"
            result
            |> fContinue
            |> Some

        SequenceState.Start
        |> Seq.unfold(
            function
            | Start ->
                dprintn "getting first item"
                f None
            | SequenceState.Continue changeId ->
                dprintn (sprintf "getting %s" changeId)
                Some changeId
                |> f
            | SequenceState.Finished ->
                None
        )
        |> Seq.choose id


    [<NoComparison>]
    type FetchDebugResult = {StashOpt:PoSResult<Stash>;Raw:string}
    let fetchStashes targetOverride =
        fetchSeq targetOverride
            // was used to have a key to cache results
            //(function | None -> "public-stash-tabs" | Some changeId -> sprintf "public-stash-tabs,%s" changeId)
            (function
                |NonValueString _ -> None,Finished
                |ValueString raw ->
                    let data = deserializeStashApi raw
                    let nextChangeId = data.["next_change_id"]
                    Some data, Continue(nextChangeId |> string)
            )
        |> Seq.rateLimit 1000
        |> Seq.collect(fun (dic:Dictionary<string,JToken>) ->
            let stashContainer =
                dic.["stashes"] :?> JArray
                |> Seq.cast<JObject>
                |> Seq.map(fun jo -> {StashOpt=jo |> string |> SuperSerial.deserialize<Stash>;Raw=string jo})
            stashContainer
        )
    ()

module Fetch =

    let fetchStashes targetOverride =
        Impl.fetchStashes targetOverride
        |> Seq.choose(function |{StashOpt=(Ok x)} -> Some x | _ -> None)



