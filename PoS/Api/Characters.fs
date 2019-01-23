namespace PathOfSupporting.Api.Characters
open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Schema.Apis

type PassiveSkillsArguments= {AccountName:string;Character:string}
type Item = {   Id:string;Name:string
                League:string;TypeLine:string;Identified:bool;ExplicitMods:string list;DescrText:string
                ILvl:int
                Type:string
                Verified:bool;W:int;H:int;X:int;Y:int;InventoryId:string
                Icon:string
                FrameType:int;Category:Map<string,string list>}

type PassiveSkillsResponse = {Hashes:int list;Items:Item list;JewelSlots: int list}

module Impl =
    open PathOfSupporting.Internal.Helpers

    let getPassiveSkillsUrl {AccountName=an;Character=cn} = sprintf "https://www.pathofexile.com/character-window/get-passive-skills?accountName=%s&character=%s" an cn

    let getTargeting x =
        match x with
        |OverrideUrl url -> url
        |Values x -> getPassiveSkillsUrl x

    let fetch uri =
        Api.fetch uri
        |> Async.map(Result.bind(fun raw ->
                match SuperSerial.deserialize<PassiveSkillsResponse> raw with
                | Ok nr -> Ok (nr,raw)
                | Error e -> Result.ErrAdd "raw" raw e |> Error
        ))

    let getPassiveSkills targeting =
        getTargeting targeting
        |> fetch

module Fetch =

    // gets only the items a single call returns
    let fetchPassiveTree targeting =
        Impl.getPassiveSkills targeting
        |> Async.map Option.ofOk
        |> Async.map (Option.map fst)

