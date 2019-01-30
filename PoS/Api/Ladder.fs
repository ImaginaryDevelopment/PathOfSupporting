namespace  PathOfSupporting.Api.Ladder // consider using the helper map for the official api: https://app.swaggerhub.com/apis/Chuanhsing/poe/1.0.0

open PathOfSupporting.Internal.BReusable.StringPatterns
open PathOfSupporting.Internal.Helpers

type FetchDetails = {League:string;Limit:int option;Offset:int option;Type:string;AccountNameFilter:string;Difficulty:string;LabyrinthStart:System.DateTime option}
type LadderArguments = |TargetUrlOverrideOpt of string | WithDetails of FetchDetails

type LadderDepth = {Default:int;Solo:int}
type LadderCharacter = {Name:string;Level:int;Class:string;Experience:int64;Id:string;Depth:LadderDepth}
type LadderGuild = {Id:string;Name:string;Tag:string;CreatedAt:string;StatusMessage:string}
type LadderTwitchInfo = {Name:string}
type LadderAccount = {Name:string;LadderGuild:LadderGuild option;Challenges:string*string list;Twitch:LadderTwitchInfo option}
type LadderEntry = {Rank:int;Dead:bool;Online:bool;Character:LadderCharacter}
type LadderResponse = {Total:int;Cached_Since:string;Entries: LadderEntry list}

module Impl =
    open PathOfSupporting.Internal.BReusable.StringHelpers
    open FSharp.Control

    let prependArg title =
        Option.map(sprintf "%s=%s" title)


    let generateUri fd =
            [
                fd.Limit |> Option.map string |> prependArg "limit"
                fd.Offset |> Option.map string |> prependArg "offset"
                fd.Type |> Option.ofValueString |> prependArg "type"
                fd.Difficulty |> Option.ofValueString |> prependArg "difficulty"
                fd.AccountNameFilter |> Option.ofValueString |> prependArg "accountName"
                fd.LabyrinthStart |> Option.map(fun dt -> dt.ToString("yyyy-MM-ddTHH:mm:ssK")) |> prependArg "start"
            ]
            |> List.choose id
            |> delimit "&"
            |> sprintf "http://api.pathofexile.com/ladders/%s?%s" fd.League
    let getTargeting =
        function
        | TargetUrlOverrideOpt x -> x
        | WithDetails fd -> generateUri fd

    let fetch uri =
        Api.fetch uri
        |> Async.map(Result.bind(fun raw ->
                match SuperSerial.deserialize<LadderResponse> raw with
                | Ok nr -> Ok (nr,raw)
                | Error e -> Result.ErrAdd "raw" raw e |> Error
        ))
    let enumerateLadder targeting =
        Some targeting
        |> AsyncSeq.unfoldAsync(fun targeting ->
            match targeting with
            | None -> async{return None}
            |Some targeting ->
                async{
                    match! generateUri targeting |> fetch with
                    | Ok ((lr,_) as x) ->
                        return Some (Ok x,Some {targeting with Offset = (targeting.Offset |> Option.defaultValue 0) + lr.Entries.Length |> Some})
                    | Error x -> return Some (Error x,None)
                }
        )

module Fetch =
    open FSharp.Control

    // gets only the items a single call returns
    let fetchLadder targeting =
        Impl.getTargeting targeting
        |> Impl.fetch
        |> Async.map Option.ofOk
        |> Async.map (Option.map fst)

    // fetch the ladder and all subsequent pages until the end or an error occurs
    let enumerateLadder targeting =
        Impl.enumerateLadder targeting
        |> AsyncSeq.choose Option.ofOk
        |> AsyncSeq.map fst

    let enumerateSynchronously targeting =
        enumerateLadder targeting
        |> AsyncSeq.toBlockingSeq
