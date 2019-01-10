namespace PathOfSupporting.StashAPI // translated from https://github.com/ImaginaryDevelopment/LinqPad/blob/master/LINQPad%20Queries/gamey/PoE%20stash%20tab%20api.linq

open System.Collections.Generic
open Newtonsoft.Json.Linq

// there are more uncaptured fields, see: https://github.com/CSharpPoE/PublicStash/blob/master/PublicStash/Model/Items/Item.cs
type Item = {
    Name:string;NamePrefix:string; TypeLine:string; TypeLinePrefix:string; Verified:bool; Identified:bool; Corrupted:bool; League:string; Icon:string
    FrameType:string;Note:string;ILvl:int;H:int;W:int;X:int;Y:int
}

type Stash = {AccountName:string;LastCharacterName:string;Id:string; Stash:string;StashType:string;Public:bool; Items:Item[]}
type ChangeSet = {ChangeId:string;Stashes:Stash list}
type RetryBehavior =
    // allows 0
    | Retries of int
    | Always
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
    type TryRetryArguments = {RetryType:RetryType;FailureMessage:string; Retries:RetryBehavior}
    let rec asyncTryRetry tra f:Async<PoSResultDI<_>> =
        async{
            try
                let! result = f()
                //if debug then printfn "asyncTryRetry worked"
                return Ok result
            with ex ->
                let retryOpt =
                    match tra.Retries with
                    | Always -> Some tra
                    | Retries x when x > 0 ->
                        Some {tra with Retries = Retries (x - 1)}
                    | _ -> None
                match retryOpt with
                |Some tra ->
                    match ex with
                    | :? System.AggregateException as aEx ->
                        if debug then
                            eprintfn "Failed asyncTry, agg '%A'" aEx.InnerException
                            match aEx.InnerExceptions with
                            | null -> ()
                            | items ->
                                items
                                |> Seq.choose Option.ofObj
                                |> Seq.iter(fun x -> eprintfn "  exMsg:%s" x.Message)
                            eprintfn ""
                    | _ -> if debug then eprintfn "Failed asyncTry, trying again after %s" ex.Message
                    match tra.RetryType with
                    | Immediate -> ()
                    | Rest ms ->
                        if debug then eprintfn"rested before next try"
                        System.Threading.Thread.Sleep(millisecondsTimeout=ms)
                        if debug then eprintfn "Rested, retrying now"
                    return! asyncTryRetry tra f
                |None ->
                    if debug then eprintfn "Ran out of retries, asyncTry fail"
                    return Result.ExDI tra.FailureMessage ex
        }

    let fetchOne target changeIdOpt retryBehavior =
        let target = defaultArg target stashTabApiUrl
        use client = new System.Net.Http.HttpClient()
        let target = match changeIdOpt with | None -> target | Some x -> sprintf "%s?id=%s" target x
        let f () = client.GetStringAsync target |> Async.AwaitTask
        asyncTryRetry {FailureMessage="fetchOneFailed"; RetryType=Rest 1000;Retries=retryBehavior} f
        |> Async.RunSynchronously

    type SequenceState =
    | Start
    | Continue of nextChangeId:string
    | Finished

    let fetchSeq target startingChangeIdOpt retryBehavior (fContinue: string -> 'T option * SequenceState)=
        let dprintn x = if Configuration.debug then printfn "%s" x
        let f changeIdOpt =
            let result =
                fetchOne target changeIdOpt retryBehavior 
                |> Result.GetOrRethrow
            //dprintn "finished fetch"
            result
            |> fContinue
            |> Some

        SequenceState.Start
        |> Seq.unfold(
            function
            | Start ->
                match startingChangeIdOpt with
                | None -> dprintn "getting first item"
                | Some changeId -> dprintn <| sprintf "getting first item %s" changeId
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
    let fetchStashes targetOverride startingChangeIdOpt retryBehavior =
        let mutable lastChangeId = None
        fetchSeq targetOverride startingChangeIdOpt retryBehavior
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

    let fetchStashes targetOverride startingChangeIdOpt retryBehavior =
        Impl.fetchStashes targetOverride startingChangeIdOpt retryBehavior
        |> Seq.map(fun (changeId,items) ->
            {ChangeId=changeId; Stashes=items |> Seq.choose (function |{StashOpt=(Ok x)} -> Some x | _ -> None) |> List.ofSeq}
        )



