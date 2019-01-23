namespace PathOfSupporting.Api.Stash // translated from https://github.com/ImaginaryDevelopment/LinqPad/blob/master/LINQPad%20Queries/gamey/PoE%20stash%20tab%20api.linq
open FSharp.Control

open System.Collections.Generic
open Newtonsoft.Json.Linq
open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Internal.Helpers.Api

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
type FetchArguments = {TargetUrlOverrideOpt:string;StartingChangeIdOpt:string}

//[<NoComparison;NoEquality>]
//type FetchResult<'t> = {ItemOpt:PoSResultDI<'t>; Next:Async<FetchResult<'t>>} with
//    static member map f (x:FetchResult<'t>):FetchResult<'tMapped> =
//        {ItemOpt = Result.map f x.ItemOpt; Next=x.Next |> Async.map (FetchResult.map f)}
//    static member toSeq (next:FetchResult<'t>):seq<FetchResult<'t>>=
//        // I'm so grateful for my x
//        let rec thankYou x =
//            seq {
//                yield x
//                yield! Async.RunSynchronously x.Next |> thankYou
//            }
//        thankYou next
//    member x.ThankYou :seq<FetchResult<'t>>=
//        let rec gratefulForMy x =
//            let thankYouNext =
//                x.Next |> Async.map(fun x -> gratefulForMy x)
//                }
//            seq {
//                yield x
//                yield! Async.RunSynchronously x.Next |> gratefulForMy
//            }
//        gratefulForMy x
//    static member changes (x:FetchResult<'t*string>):FetchResult<'t> =
//        let mutable lastChange = x.ItemOpt |> Option.ofOk |> Option.map snd
//        let result=
//            (x,lastChange) |> Seq.unfold(fun state ->
//                Unchecked.defaultof<_>
//            )
//        result

module Impl =
    open PathOfSupporting
    open PathOfSupporting.Internal.BReusable.StringPatterns
    open System.Runtime.ExceptionServices

    let stashTabApiUrl = "http://www.pathofexile.com/api/public-stash-tabs"
    (* appears we can't look at historic, the api only gives back stashes in their current form : 
       https://www.reddit.com/r/pathofexiledev/comments/8ayz9b/list_of_changeids_and_their_and_the_approximate/
    *)
    let deserializeStashApi text = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,JToken>>(text)
    let getTarget (targeting:FetchArguments)=
        let target = targeting.TargetUrlOverrideOpt |> Option.ofValueString |> Option.defaultValue stashTabApiUrl
        let target = match targeting.StartingChangeIdOpt with | NonValueString _ -> target | ValueString x -> sprintf "%s?id=%s" target x
        target

    let fetchOne (targeting:FetchArguments) fMap:Async<PoSResult<_> * FetchArguments> =
        let target = targeting.TargetUrlOverrideOpt |> Option.ofValueString |> Option.defaultValue stashTabApiUrl
        let target = match targeting.StartingChangeIdOpt with | NonValueString _ -> target | ValueString x -> sprintf "%s?id=%s" target x
        async{
            match! Api.fetch target with
            | Ok raw ->
                try
                    let (item,nextKey) = fMap raw
                    return Ok <| (targeting.StartingChangeIdOpt,item), {targeting with StartingChangeIdOpt = nextKey}
                with ex ->
                    let posR = Result.ExDI target ex
                    return (posR, targeting)
            | Error x -> return (Error x, targeting)
        }

    let fetch (targeting:FetchArguments) fMap = // : Async<FetchResult<_>> =
        AsyncSeq.unfoldAsync (fun targeting -> fetchOne targeting fMap |> Async.map Some) targeting

    let mapChangeSet raw =
        let data = deserializeStashApi raw
        let nextChangeId = string data.["next_change_id"]
        nextChangeId,data

    [<NoComparison>]
    type FetchDebugResult = {StashOpt:PoSResult<Stash>;Raw:string}
    let deserializeStash = SuperSerial.deserialize<Stash>
    let fetchStashes args:AsyncSeq<PoSResult<_>>=
        let rawResult =
            fetch args
                // was used to have a key to cache results
                //(function | None -> "public-stash-tabs" | Some changeId -> sprintf "public-stash-tabs,%s" changeId)
                (fun raw ->
                    let nextChangeId,x = mapChangeSet raw
                    let result = x, nextChangeId
                    result
                )
        rawResult
        |> AsyncSeq.rateLimit 1000
        |> AsyncSeq.map(
            Result.map (fun (changeId,dic:Dictionary<string,JToken>) ->
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
        )
    ()
    let findItems league args =
        fetchStashes args
        |> AsyncSeq.choose Option.ofOk
        |> AsyncSeq.map snd
        |> AsyncSeq.collect(fun x ->
                x
                |> Seq.choose(fun x -> x.StashOpt |> Option.ofOk)
                |> Seq.collect(fun x -> x.Items)
                |> Seq.filter(fun x -> x.League = league)
                |> AsyncSeq.ofSeq
        )

module Fetch =
    let betrayalStart = "287521566-295313957-282824102-313717386-301060771"

    let fetchStashes args =
        Impl.fetchStashes args
        |> AsyncSeq.choose Option.ofOk
        |> AsyncSeq.map(fun (changeId,items) ->
            {ChangeId=changeId; Stashes=items |> Seq.choose (function |{StashOpt=(Ok x)} -> Some x | _ -> None) |> List.ofSeq}
        )

    let fetchSynchronously args =
        fetchStashes args
        |> AsyncSeq.toBlockingSeq



