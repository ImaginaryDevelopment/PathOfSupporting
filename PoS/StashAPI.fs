namespace PathOfSupporting.StashAPI // translated from https://github.com/ImaginaryDevelopment/LinqPad/blob/master/LINQPad%20Queries/gamey/PoE%20stash%20tab%20api.linq

open System.Collections.Generic
open Newtonsoft.Json.Linq

type Item = {Name:string;NamePrefix:string; TypeLine:string; TypeLinePrefix:string; Verified:bool; Identified:bool; Corrupted:bool; League:string; Icon:string}

type Stash = {AccountName:string;LastCharacterName:string;Id:string; Stash:string;StashType:string;Public:bool; Items:Item[]}
type ChangeSet = {ChangeId:string;Stashes:Stash list}

module Impl =
    open PathOfSupporting
    open PathOfSupporting.Configuration
    open PathOfSupporting.Internal.BReusable.StringPatterns
    open PathOfSupporting.Internal.Helpers

    let stashTabApiUrl = "http://www.pathofexile.com/api/public-stash-tabs"
    (* appears we can't look at historic, the api only gives back stashes in their current form : 
       https://www.reddit.com/r/pathofexiledev/comments/8ayz9b/list_of_changeids_and_their_and_the_approximate/
    *)
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

    let fetchSeq target startingChangeIdOpt (fContinue: string -> 'T option * SequenceState)=
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
                f startingChangeIdOpt
            | SequenceState.Continue changeId ->
                dprintn (sprintf "getting %s" changeId)
                Some changeId
                |> f
            | SequenceState.Finished ->
                None
        )
        |> Seq.choose id
    let mapChangeSet raw =
        let data = deserializeStashApi raw
        let nextChangeId = string data.["next_change_id"]
        nextChangeId,data


    [<NoComparison>]
    type FetchDebugResult = {StashOpt:PoSResult<Stash>;Raw:string}
    let fetchStashes targetOverride startingChangeIdOpt =
        let mutable lastChangeId = None
        fetchSeq targetOverride startingChangeIdOpt
            // was used to have a key to cache results
            //(function | None -> "public-stash-tabs" | Some changeId -> sprintf "public-stash-tabs,%s" changeId)
            (function
                |NonValueString _ -> None,Finished
                |ValueString raw ->
                    let (nextChangeId,_) as x = mapChangeSet raw
                    Some x, Continue nextChangeId
            )
        // don't return the same changeset again (in the unlikely event we got the last item on the stream - the changeId would be equal to current)
        |> Seq.changes fst
        |> Seq.rateLimit 1000
        |> Seq.map(fun (changeId,dic:Dictionary<string,JToken>) ->
            let stashContainer =
                dic.["stashes"] :?> JArray
                |> Seq.cast<JObject>
                |> Seq.map(fun jo -> {StashOpt=jo |> string |> SuperSerial.deserialize<Stash>;Raw=string jo})
            changeId,stashContainer
        )
    ()

module Fetch =

    let fetchStashes targetOverride startingChangeIdOpt =
        Impl.fetchStashes targetOverride startingChangeIdOpt
        |> Seq.map(fun (changeId,items) ->
            {ChangeId=changeId; Stashes=items |> Seq.choose (function |{StashOpt=(Ok x)} -> Some x | _ -> None) |> List.ofSeq}
        )



