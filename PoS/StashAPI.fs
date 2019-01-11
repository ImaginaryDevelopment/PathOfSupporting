namespace PathOfSupporting.StashAPI // translated from https://github.com/ImaginaryDevelopment/LinqPad/blob/master/LINQPad%20Queries/gamey/PoE%20stash%20tab%20api.linq

open System.Collections.Generic
open Newtonsoft.Json.Linq
open PathOfSupporting.Internal.Helpers
type ItemProperty = {Name:string;Values:string list list;DisplayMode:int}
type Socket = {Group:int;Attr:string;SColour:string}
// there are more uncaptured fields, see: https://github.com/CSharpPoE/PublicStash/blob/master/PublicStash/Model/Items/Item.cs
type Item = {
    Id:string;
    Name:string;NamePrefix:string; TypeLine:string; TypeLinePrefix:string; Verified:bool; Identified:bool; Corrupted:bool; League:string; Icon:string
    FrameType:int;Note:string;ILvl:int;H:int;W:int;X:int;Y:int;InventoryId:string;DescrText:string;SecDescrText:string
    // is this safe? can items come through without categories? if so will it blow up the deserialization?
    Category: Map<string,string list>
    ExplicitMods:string list
    Properties:ItemProperty list
    AdditionalProperties: ItemProperty list
    Requirements:ItemProperty list
    Sockets: Socket list
}

type Stash = {AccountName:string;LastCharacterName:string;Id:string; Stash:string;StashType:string;Public:bool; Items:Item[]}
type ChangeSet = {ChangeId:string;Stashes:Stash list}
type RetryBehavior =
    // allows 0
    | Retries of int
    | Infinite

type FetchArguments = {TargetUrlOverrideOpt:string;StartingChangeIdOpt:string;RetryBehavior:RetryBehavior}
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
                    | Infinite -> Some tra
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

    let fetchOne {StartingChangeIdOpt=changeIdOpt;TargetUrlOverrideOpt=target;RetryBehavior=retryBehavior }=
        let target = target|> Option.ofValueString |> Option.defaultValue stashTabApiUrl
        use client = new System.Net.Http.HttpClient()
        let target = match changeIdOpt with | NonValueString _ -> target | ValueString x -> sprintf "%s?id=%s" target x
        let f () = client.GetStringAsync target |> Async.AwaitTask
        asyncTryRetry {FailureMessage="fetchOneFailed"; RetryType=Rest 1000;Retries=retryBehavior} f
        |> Async.RunSynchronously

    type SequenceState =
    | Start
    | Continue of nextChangeId:string
    | Finished

    let fetchSeq {TargetUrlOverrideOpt=target;StartingChangeIdOpt=startingChangeIdOpt;RetryBehavior=retryBehavior} (fContinue: string -> 'T option * SequenceState)=
        let dprintn x = if Configuration.debug then printfn "%s" x
        let f changeIdOpt =
            let result =
                fetchOne {TargetUrlOverrideOpt=target;StartingChangeIdOpt=changeIdOpt;RetryBehavior=retryBehavior}
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
                | NonValueString _ -> dprintn "getting first item"
                | ValueString changeId -> dprintn <| sprintf "getting first item %s" changeId
                f startingChangeIdOpt
            | SequenceState.Continue changeId ->
                dprintn (sprintf "getting %s" changeId)
                changeId
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
    let deserializeStash = SuperSerial.deserialize<Stash>
    let fetchStashes args =
        let mutable lastChangeId = None
        fetchSeq args
            // was used to have a key to cache results
            //(function | None -> "public-stash-tabs" | Some changeId -> sprintf "public-stash-tabs,%s" changeId)
            (function
                |NonValueString _ -> None,Finished
                |ValueString raw ->
                    let nextChangeId,x = mapChangeSet raw
                    let result = Some (lastChangeId |> Option.defaultValue null,x), Continue nextChangeId
                    lastChangeId <- Some nextChangeId
                    result
            )
        // don't return the same changeset again (in the unlikely event we got the last item on the stream - the changeId would be equal to current)
        |> Seq.changes fst
        |> Seq.rateLimit 1000
        |> Seq.map(fun (changeId,dic:Dictionary<string,JToken>) ->
            let stashContainer =
                dic.["stashes"] :?> JArray
                |> Seq.cast<JObject>
                |> Seq.map(fun jo ->
                    let text = string jo
                    let result = {StashOpt=text |> SuperSerial.deserialize<Stash>;Raw=text}
                    result
                )
            changeId,stashContainer
        )
    ()
    let findItems league args =
        fetchStashes args
        |> Seq.collect snd
        |> Seq.map(fun x -> x.StashOpt)
        |> Seq.choose Option.ofOk
        |> Seq.collect(fun x -> x.Items)
        |> Seq.filter(fun x -> x.League = league)

module Fetch =
    let betrayalStart = "287521566-295313957-282824102-313717386-301060771"

    let fetchStashes args =
        Impl.fetchStashes args
        |> Seq.map(fun (changeId,items) ->
            {ChangeId=changeId; Stashes=items |> Seq.choose (function |{StashOpt=(Ok x)} -> Some x | _ -> None) |> List.ofSeq}
        )



